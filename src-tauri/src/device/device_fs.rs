//! MTP (`mtp:/…`) and mass-storage unified async I/O via KDE kioclient on Linux.
//!
//! **MTP rules (see `kio.rs` + call sites):**
//! - Always build child paths with [`join_uri_leaf`] / [`join_device_relative`] so segments are percent-encoded.
//! - Writes use temp file + [`mtp_copy_local_to_uri`] (remove destination, copy, optional staging + `move`).
//! - [`rename_move`] clears the destination URL before `kioclient move` (overwrite quirks).
//! - [`create_dir`] walks prefix segments on MTP (`mkdir` per level).
//! - Backup/restore never use raw `std::fs` on `mtp:` paths — use this module end-to-end.

use std::path::{Path, PathBuf};

use crate::error::AppError;

/// Sidecar next to `dest` used when MTP rejects in-place overwrites (common for `*_r.xml`).
#[cfg(target_os = "linux")]
fn mtp_staging_uri(dest_normalized: &str) -> String {
    let d = dest_normalized.trim_end_matches('/');
    if let Some((dir, base)) = d.rsplit_once('/') {
        format!("{dir}/{base}.garmin-up-new")
    } else {
        format!("{d}.garmin-up-new")
    }
}

/// Publish a local file onto an MTP URL: try overwrite, then write+rename if the stack blocks overwrite.
#[cfg(target_os = "linux")]
fn mtp_copy_local_to_uri(local_src: &Path, dest_uri: &str) -> Result<(), String> {
    use crate::device::kio::{self, KIO_COPY_TIMEOUT, KIO_FAST_TIMEOUT};

    let dest = kio::normalize_kio_mtp_uri(dest_uri);
    let _ = kio::remove(&dest, KIO_FAST_TIMEOUT);

    if kio::copy_overwrite(local_src, &dest, KIO_COPY_TIMEOUT).is_ok() {
        return Ok(());
    }

    let staging = mtp_staging_uri(&dest);
    let _ = kio::remove(&staging, KIO_FAST_TIMEOUT);
    kio::copy_overwrite(local_src, &staging, KIO_COPY_TIMEOUT)?;
    let _ = kio::remove(&dest, KIO_FAST_TIMEOUT);
    kio::move_url(&staging, &dest, KIO_COPY_TIMEOUT)
        .map_err(|e| format!("{e}. MTP: if this persists, unlock the watch, use file-transfer/MTP mode, and close Dolphin or Garmin Express using the device."))
}

#[cfg(all(test, target_os = "linux"))]
mod mtp_uri_tests {
    use super::mtp_staging_uri;

    #[test]
    fn staging_uri_appends_suffix_before_last_slash() {
        assert_eq!(
            mtp_staging_uri("mtp:/fenix%206%20Pro/Primary/GARMIN/006-d9486-07_r.xml"),
            "mtp:/fenix%206%20Pro/Primary/GARMIN/006-d9486-07_r.xml.garmin-up-new"
        );
    }
}

/// True for KDE MTP URLs stored in [`PathBuf`][std::path::PathBuf].
pub fn is_kio_uri(path: &Path) -> bool {
    path.to_string_lossy().starts_with("mtp:")
}

/// Append a path segment to a device root. For `mtp:/…`, percent-encodes each `/` segment in `relative`
/// (GarminDevice.xml paths like `Garmin/NEWFILES` or `Apps/My App.prg`).
pub fn join_device_relative(device_root: &Path, relative: &str) -> PathBuf {
    let rel = relative.trim().trim_matches('/');
    if rel.is_empty() {
        return device_root.to_path_buf();
    }
    if is_kio_uri(device_root) {
        rel.split('/')
            .filter(|s| !s.is_empty())
            .fold(device_root.to_path_buf(), |acc, seg| join_uri_leaf(&acc, seg))
    } else {
        rel.split('/')
            .filter(|s| !s.is_empty())
            .fold(device_root.to_path_buf(), |acc, seg| acc.join(seg))
    }
}

/// Append a POSIX-style relative segment (`GarminDevice.xml`, `Apps/foo.prg`) to `mtp:/…` safely.
pub fn join_uri_leaf(base: &Path, leaf: impl AsRef<str>) -> PathBuf {
    let b = base.to_string_lossy();
    let clean = leaf.as_ref().trim_matches('/');
    if b.starts_with("mtp:") {
        #[cfg(target_os = "linux")]
        {
            let enc = crate::device::kio::encode_mtp_relative_path(clean);
            PathBuf::from(format!("{}/{}", b.trim_end_matches('/'), enc))
        }
        #[cfg(not(target_os = "linux"))]
        {
            PathBuf::from(format!("{}/{}", b.trim_end_matches('/'), clean))
        }
    } else {
        PathBuf::from(format!("{}/{}", b.trim_end_matches('/'), clean))
    }
}

/// Whether `file_name` exists in `dir` (case-insensitive). Works for `mtp:/…` and local dirs.
pub async fn file_exists_case_insensitive(dir: &Path, file_name: &str) -> bool {
    match read_dir_filenames(dir).await {
        Ok(names) => names
            .iter()
            .any(|n| n.eq_ignore_ascii_case(file_name.trim())),
        Err(_) => false,
    }
}

/// Create `path` and every parent directory (`mtp:/…` uses repeated `mkdir`; local uses `create_dir_all`).
pub async fn ensure_parent_dirs(path: &Path) -> Result<(), AppError> {
    let Some(parent) = path.parent() else {
        return Ok(());
    };
    if parent.as_os_str().is_empty() {
        return Ok(());
    }
    if is_kio_uri(path) {
        #[cfg(target_os = "linux")]
        {
            let p = parent.to_string_lossy().into_owned();
            return tokio::task::spawn_blocking(move || {
                mtp_mkdir_all_chain(&p).map_err(AppError::Other)
            })
            .await
            .map_err(|e| AppError::Other(format!("mkdir chain task failed: {e}")))?;
        }
        #[cfg(not(target_os = "linux"))]
        {
            let _ = path;
            return Err(AppError::Other(MTP_KIO_UNAVAILABLE.into()));
        }
    }
    tokio::fs::create_dir_all(parent).await?;
    Ok(())
}

#[cfg(target_os = "linux")]
fn mtp_mkdir_all_chain(normalized_or_raw_uri: &str) -> Result<(), String> {
    use crate::device::kio::{self, KIO_FAST_TIMEOUT};
    let norm = kio::normalize_kio_mtp_uri(normalized_or_raw_uri);
    let trimmed = norm.trim_end_matches('/');
    let body = trimmed.strip_prefix("mtp:/").unwrap_or("");
    let segs: Vec<&str> = body.split('/').filter(|s| !s.is_empty()).collect();
    let mut acc = String::from("mtp:/");
    for (i, seg) in segs.iter().enumerate() {
        if i > 0 {
            acc.push('/');
        }
        acc.push_str(seg);
        let _ = kio::mkdir(&acc, KIO_FAST_TIMEOUT);
    }
    Ok(())
}

#[cfg(not(target_os = "linux"))]
const MTP_KIO_UNAVAILABLE: &str =
    "MTP device paths (mtp:/) are only supported on Linux with kde-cli-tools (kioclient).";

fn ensure_mtp_ls_uri(dir: &Path) -> String {
    let s = dir.to_string_lossy().into_owned();
    let t = s.trim_end_matches('/');
    format!("{t}/")
}

pub async fn read_to_string(path: &Path) -> Result<String, AppError> {
    if is_kio_uri(path) {
        #[cfg(target_os = "linux")]
        {
            let uri = path.to_string_lossy().into_owned();
            return tokio::task::spawn_blocking(move || {
                crate::device::kio::cat_utf8(&uri, crate::device::kio::KIO_FAST_TIMEOUT)
                    .map_err(AppError::Other)
            })
            .await
            .map_err(|e| AppError::Other(format!("read task panicked/join failed: {e}")))?;
        }
        #[cfg(not(target_os = "linux"))]
        {
            let _ = path;
            return Err(AppError::Other(MTP_KIO_UNAVAILABLE.into()));
        }
    }
    Ok(tokio::fs::read_to_string(path).await?)
}

pub async fn read_bytes(path: &Path) -> Result<Vec<u8>, AppError> {
    if is_kio_uri(path) {
        #[cfg(target_os = "linux")]
        {
            let uri = path.to_string_lossy().into_owned();
            return tokio::task::spawn_blocking(move || {
                crate::device::kio::cat_bytes(&uri, crate::device::kio::KIO_COPY_TIMEOUT)
                    .map_err(AppError::Other)
            })
            .await
            .map_err(|e| AppError::Other(format!("read task panicked/join failed: {e}")))?;
        }
        #[cfg(not(target_os = "linux"))]
        {
            let _ = path;
            return Err(AppError::Other(MTP_KIO_UNAVAILABLE.into()));
        }
    }
    Ok(tokio::fs::read(path).await?)
}

pub async fn write_bytes(path: &Path, contents: &[u8]) -> Result<(), AppError> {
    if is_kio_uri(path) {
        #[cfg(target_os = "linux")]
        {
            ensure_parent_dirs(path).await?;
            let dest_uri = path.to_string_lossy().into_owned();
            let bytes = contents.to_vec();
            return tokio::task::spawn_blocking(move || {
                let mut tmp = std::env::temp_dir();
                tmp.push(format!("garmin-up-w-{}.bin", uuid::Uuid::new_v4()));
                std::fs::write(&tmp, &bytes).map_err(|e| AppError::Other(format!("temp write: {e}")))?;
                let res = mtp_copy_local_to_uri(&tmp, &dest_uri).map_err(AppError::Other);
                let _ = std::fs::remove_file(&tmp);
                res
            })
            .await
            .map_err(|e| AppError::Other(format!("write task panicked/join failed: {e}")))?;
        }
        #[cfg(not(target_os = "linux"))]
        {
            let _ = (path, contents);
            return Err(AppError::Other(MTP_KIO_UNAVAILABLE.into()));
        }
    }
    Ok(tokio::fs::write(path, contents).await?)
}

pub async fn create_dir(dir: &Path) -> Result<(), AppError> {
    if is_kio_uri(dir) {
        #[cfg(target_os = "linux")]
        {
            let uri = dir.to_string_lossy().into_owned();
            return tokio::task::spawn_blocking(move || {
                mtp_mkdir_all_chain(uri.trim_end_matches('/')).map_err(AppError::Other)
            })
            .await
            .map_err(|e| AppError::Other(format!("mkdir task panicked/join failed: {e}")))?;
        }
        #[cfg(not(target_os = "linux"))]
        {
            let _ = dir;
            return Err(AppError::Other(MTP_KIO_UNAVAILABLE.into()));
        }
    }
    Ok(tokio::fs::create_dir_all(dir).await?)
}

pub async fn remove_file(path: &Path) -> Result<(), AppError> {
    if is_kio_uri(path) {
        #[cfg(target_os = "linux")]
        {
            let uri = path.to_string_lossy().into_owned();
            return tokio::task::spawn_blocking(move || {
                crate::device::kio::remove(&uri, crate::device::kio::KIO_FAST_TIMEOUT)
                    .map_err(AppError::Other)
            })
            .await
            .map_err(|e| AppError::Other(format!("remove task panicked/join failed: {e}")))?;
        }
        #[cfg(not(target_os = "linux"))]
        {
            let _ = path;
            return Err(AppError::Other(MTP_KIO_UNAVAILABLE.into()));
        }
    }
    if path.exists() {
        tokio::fs::remove_file(path).await?;
    }
    Ok(())
}

pub async fn rename_move(from: &Path, to: &Path) -> Result<(), AppError> {
    if is_kio_uri(from) || is_kio_uri(to) {
        #[cfg(target_os = "linux")]
        {
            let a = from.to_string_lossy().into_owned();
            let b = to.to_string_lossy().into_owned();
            return tokio::task::spawn_blocking(move || {
                use crate::device::kio::{self, KIO_COPY_TIMEOUT, KIO_FAST_TIMEOUT};
                let from_n = kio::normalize_kio_mtp_uri(&a);
                let to_n = kio::normalize_kio_mtp_uri(&b);
                // MTP often rejects `move` when the destination already exists (quarantine retries).
                let _ = kio::remove(&to_n, KIO_FAST_TIMEOUT);
                kio::move_url(&from_n, &to_n, KIO_COPY_TIMEOUT).map_err(AppError::Other)
            })
            .await
            .map_err(|e| AppError::Other(format!("move task panicked/join failed: {e}")))?;
        }
        #[cfg(not(target_os = "linux"))]
        {
            return Err(AppError::Other(MTP_KIO_UNAVAILABLE.into()));
        }
    }
    tokio::fs::rename(from, to).await?;
    Ok(())
}

/// Copy **from local file** onto `dest` (`mtp:/…` OK).
pub async fn copy_local_to(from_local_file: &Path, dest: &Path) -> Result<(), AppError> {
    if is_kio_uri(dest) {
        #[cfg(target_os = "linux")]
        {
            ensure_parent_dirs(dest).await?;
            let dup = from_local_file.to_path_buf();
            let dest_uri = dest.to_string_lossy().into_owned();
            return tokio::task::spawn_blocking(move || {
                mtp_copy_local_to_uri(&dup, &dest_uri).map_err(AppError::Other)
            })
            .await
            .map_err(|e| AppError::Other(format!("copy task panicked/join failed: {e}")))?;
        }
        #[cfg(not(target_os = "linux"))]
        {
            let _ = (from_local_file, dest);
            return Err(AppError::Other(MTP_KIO_UNAVAILABLE.into()));
        }
    }
    if let Some(parent) = dest.parent() {
        tokio::fs::create_dir_all(parent).await?;
    }
    tokio::fs::copy(from_local_file, dest).await?;
    Ok(())
}

pub async fn read_dir_filenames(uri_dir: &Path) -> Result<Vec<String>, AppError> {
    if is_kio_uri(uri_dir) {
        #[cfg(target_os = "linux")]
        {
            let ls_uri = ensure_mtp_ls_uri(uri_dir);
            return tokio::task::spawn_blocking(move || {
                crate::device::kio::ls(&ls_uri, crate::device::kio::KIO_FAST_TIMEOUT).map_err(AppError::Other)
            })
            .await
            .map_err(|e| AppError::Other(format!("ls task panicked/join failed: {e}")))?;
        }
        #[cfg(not(target_os = "linux"))]
        {
            let _ = uri_dir;
            return Err(AppError::Other(MTP_KIO_UNAVAILABLE.into()));
        }
    }

    let mut out = Vec::new();
    let mut rd = tokio::fs::read_dir(uri_dir).await?;
    while let Some(e) = rd.next_entry().await? {
        if let Ok(n) = e.file_name().into_string() {
            out.push(n);
        }
    }
    Ok(out)
}
