//! MTP (`mtp:/…`) and mass-storage unified async I/O via KDE kioclient on Linux.

use std::path::{Path, PathBuf};

use crate::error::AppError;

/// True for KDE MTP URLs stored in [`PathBuf`][std::path::PathBuf].
pub fn is_kio_uri(path: &Path) -> bool {
    path.to_string_lossy().starts_with("mtp:")
}

/// Append a POSIX-style relative segment (`GarminDevice.xml`, `Apps/foo.prg`) to `mtp:/…` safely.
pub fn join_uri_leaf(base: &Path, leaf: impl AsRef<str>) -> PathBuf {
    let b = base.to_string_lossy();
    let clean = leaf.as_ref().trim_matches('/');
    PathBuf::from(format!("{}/{}", b.trim_end_matches('/'), clean))
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
            let dest_uri = path.to_string_lossy().into_owned();
            let bytes = contents.to_vec();
            return tokio::task::spawn_blocking(move || {
                let mut tmp = std::env::temp_dir();
                tmp.push(format!("garmin-up-w-{}.bin", uuid::Uuid::new_v4()));
                std::fs::write(&tmp, &bytes).map_err(|e| AppError::Other(format!("temp write: {e}")))?;
                let res =
                    crate::device::kio::copy_overwrite(&tmp, &dest_uri, crate::device::kio::KIO_COPY_TIMEOUT)
                        .map_err(AppError::Other);
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
                crate::device::kio::mkdir(uri.trim_end_matches('/'), crate::device::kio::KIO_FAST_TIMEOUT)
                    .map_err(AppError::Other)
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
                crate::device::kio::move_url(&a, &b, crate::device::kio::KIO_COPY_TIMEOUT)
                    .map_err(AppError::Other)
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
            let dup = from_local_file.to_path_buf();
            let dest_uri = dest.to_string_lossy().into_owned();
            return tokio::task::spawn_blocking(move || {
                crate::device::kio::copy_overwrite(&dup, &dest_uri, crate::device::kio::KIO_COPY_TIMEOUT)
                    .map_err(AppError::Other)
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
