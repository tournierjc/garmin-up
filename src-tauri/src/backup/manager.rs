use std::path::{Path, PathBuf};

use chrono::Local;
use serde::{Deserialize, Serialize};
use tokio::fs;
use tracing::{debug, info, warn};

use crate::device::device_fs;
use crate::device::DetectedDevice;
use crate::error::AppError;
use crate::xml::garmin_device::TransferDirection;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct BackupProgress {
    pub total_files: usize,
    pub copied_files: usize,
    pub total_bytes: u64,
    pub copied_bytes: u64,
    pub current_file: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct BackupResult {
    pub files_copied: usize,
    pub bytes_copied: u64,
    pub backup_path: PathBuf,
    pub timestamp: String,
}

pub struct BackupManager {
    data_dir: PathBuf,
}

impl BackupManager {
    pub fn new() -> Self {
        Self {
            data_dir: garmin_data_dir(),
        }
    }

    pub fn device_sync_dir(&self, unit_id: &str) -> PathBuf {
        self.data_dir
            .join("CoreService")
            .join("Devices")
            .join(unit_id)
            .join("Sync")
    }

    pub async fn backup_device(
        &self,
        device: &DetectedDevice,
        progress_tx: Option<tokio::sync::mpsc::Sender<BackupProgress>>,
    ) -> Result<BackupResult, AppError> {
        let sync_dir = self.device_sync_dir(&device.unit_id);
        fs::create_dir_all(&sync_dir).await?;

        let files_to_backup = self.collect_backup_files(device).await?;

        let total_files = files_to_backup.len();
        let total_bytes: u64 = files_to_backup.iter().map(|f| f.size).sum();

        info!(
            "Backing up {} files ({} bytes) from {}",
            total_files, total_bytes, device.description
        );

        let mut copied_files = 0usize;
        let mut copied_bytes = 0u64;
        let timestamp = Local::now().format("%Y-%m-%dT%H:%M:%S").to_string();

        for file_entry in &files_to_backup {
            let dest = sync_dir.join(&file_entry.relative_dest);
            if let Some(parent) = dest.parent() {
                fs::create_dir_all(parent).await?;
            }

            let source_kio = device_fs::is_kio_uri(&file_entry.source);
            let should_copy = if source_kio {
                match fs::metadata(&dest).await {
                    Ok(dest_meta) => file_entry.size != dest_meta.len(),
                    Err(_) => true,
                }
            } else {
                let src_meta = match fs::metadata(&file_entry.source).await {
                    Ok(m) => m,
                    Err(e) => {
                        warn!("Skipping {}: {}", file_entry.source.display(), e);
                        continue;
                    }
                };
                match fs::metadata(&dest).await {
                    Ok(dest_meta) => {
                        src_meta.len() != dest_meta.len()
                            || src_meta.modified().ok() != dest_meta.modified().ok()
                    }
                    Err(_) => true,
                }
            };

            if should_copy {
                debug!("Copying {} -> {}", file_entry.source.display(), dest.display());
                if source_kio {
                    let bytes = device_fs::read_bytes(&file_entry.source).await?;
                    fs::write(&dest, &bytes).await?;
                } else {
                    fs::copy(&file_entry.source, &dest).await?;
                }
            } else {
                debug!("Skipping (unchanged) {}", file_entry.source.display());
            }

            copied_files += 1;
            copied_bytes += file_entry.size;

            if let Some(ref tx) = progress_tx {
                let _ = tx
                    .send(BackupProgress {
                        total_files,
                        copied_files,
                        total_bytes,
                        copied_bytes,
                        current_file: file_entry
                            .source
                            .file_name()
                            .unwrap_or_default()
                            .to_string_lossy()
                            .into_owned(),
                    })
                    .await;
            }
        }

        info!(
            "Backup complete: {} files, {} bytes",
            copied_files, copied_bytes
        );

        Ok(BackupResult {
            files_copied: copied_files,
            bytes_copied: copied_bytes,
            backup_path: sync_dir,
            timestamp,
        })
    }

    pub async fn restore_device(
        &self,
        device: &DetectedDevice,
        progress_tx: Option<tokio::sync::mpsc::Sender<BackupProgress>>,
    ) -> Result<BackupResult, AppError> {
        let sync_dir = self.device_sync_dir(&device.unit_id);
        if !sync_dir.is_dir() {
            return Err(AppError::Other(format!(
                "No backup found for device {}",
                device.unit_id
            )));
        }

        let files_to_restore = self.collect_restore_files(device, &sync_dir).await?;

        let total_files = files_to_restore.len();
        let total_bytes: u64 = files_to_restore.iter().map(|f| f.size).sum();

        info!(
            "Restoring {} files ({} bytes) to {}",
            total_files, total_bytes, device.description
        );

        let mut copied_files = 0usize;
        let mut copied_bytes = 0u64;
        let timestamp = Local::now().format("%Y-%m-%dT%H:%M:%S").to_string();

        for file_entry in &files_to_restore {
            let rel = pathbuf_as_posix_slash(&file_entry.relative_dest);
            let dest = device_fs::join_device_relative(&device.mount_path, &rel);

            if device_fs::is_kio_uri(&dest) {
                device_fs::ensure_parent_dirs(&dest).await?;
                debug!("Restoring {} -> {}", file_entry.source.display(), dest.display());
                device_fs::copy_local_to(&file_entry.source, &dest).await?;
            } else {
                if let Some(parent) = dest.parent() {
                    fs::create_dir_all(parent).await?;
                }
                debug!("Restoring {} -> {}", file_entry.source.display(), dest.display());
                fs::copy(&file_entry.source, &dest).await?;
            }

            copied_files += 1;
            copied_bytes += file_entry.size;

            if let Some(ref tx) = progress_tx {
                let _ = tx
                    .send(BackupProgress {
                        total_files,
                        copied_files,
                        total_bytes,
                        copied_bytes,
                        current_file: file_entry
                            .source
                            .file_name()
                            .unwrap_or_default()
                            .to_string_lossy()
                            .into_owned(),
                    })
                    .await;
            }
        }

        info!(
            "Restore complete: {} files, {} bytes",
            copied_files, copied_bytes
        );

        Ok(BackupResult {
            files_copied: copied_files,
            bytes_copied: copied_bytes,
            backup_path: sync_dir,
            timestamp,
        })
    }

    async fn collect_backup_files(
        &self,
        device: &DetectedDevice,
    ) -> Result<Vec<FileEntry>, AppError> {
        let mut entries = Vec::new();
        let kio_mount = device_fs::is_kio_uri(&device.mount_path);

        for data_type in &device.data_types {
            for file_spec in &data_type.files {
                let should_backup = matches!(
                    file_spec.transfer_direction,
                    TransferDirection::OutputFromUnit | TransferDirection::InputOutput
                );
                if !should_backup {
                    continue;
                }

                let device_dir = if kio_mount {
                    device_fs::join_device_relative(&device.mount_path, &file_spec.location.path)
                } else {
                    device.mount_path.join(&file_spec.location.path)
                };

                if kio_mount {
                    if device_fs::read_dir_filenames(&device_dir).await.is_err() {
                        continue;
                    }
                } else if !device_dir.is_dir() {
                    continue;
                }

                let ext = file_spec.location.file_extension.to_lowercase();
                let fit_type_dir = fit_type_dir_name(&data_type.name);

                match &file_spec.location.base_name {
                    Some(base_name) => {
                        let filename = format!("{}.{}", base_name, ext);
                        let src = if kio_mount {
                            device_fs::join_uri_leaf(&device_dir, &filename)
                        } else {
                            device_dir.join(&filename)
                        };
                        if kio_mount {
                            if !device_fs::file_exists_case_insensitive(&device_dir, &filename).await
                            {
                                continue;
                            }
                            let size = device_fs::read_bytes(&src).await.map(|b| b.len() as u64).unwrap_or(0);
                            entries.push(FileEntry {
                                source: src,
                                relative_dest: PathBuf::from(&fit_type_dir).join(&filename),
                                size,
                            });
                        } else if src.is_file() {
                            let size = std::fs::metadata(&src)
                                .map(|m| m.len())
                                .unwrap_or(0);
                            entries.push(FileEntry {
                                source: src,
                                relative_dest: PathBuf::from(&fit_type_dir).join(&filename),
                                size,
                            });
                        }
                    }
                    None => {
                        if kio_mount {
                            for fname in device_fs::read_dir_filenames(&device_dir).await? {
                                let lc = fname.to_lowercase();
                                if !lc.ends_with(&format!(".{ext}")) {
                                    continue;
                                }
                                let src = device_fs::join_uri_leaf(&device_dir, &fname);
                                let size = device_fs::read_bytes(&src).await.map(|b| b.len() as u64).unwrap_or(0);
                                entries.push(FileEntry {
                                    source: src,
                                    relative_dest: PathBuf::from(&fit_type_dir).join(&fname),
                                    size,
                                });
                            }
                        } else if let Ok(mut read_dir) = fs::read_dir(&device_dir).await {
                            while let Ok(Some(entry)) = read_dir.next_entry().await {
                                let path = entry.path();
                                let matches_ext = path
                                    .extension()
                                    .is_some_and(|e| e.to_ascii_lowercase() == ext.as_str());
                                if path.is_file() && matches_ext {
                                    let size = entry.metadata().await.map(|m| m.len()).unwrap_or(0);
                                    let fname = path.file_name().unwrap_or_default().to_owned();
                                    entries.push(FileEntry {
                                        source: path,
                                        relative_dest: PathBuf::from(&fit_type_dir).join(fname),
                                        size,
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }

        Ok(entries)
    }

    async fn collect_restore_files(
        &self,
        device: &DetectedDevice,
        sync_dir: &Path,
    ) -> Result<Vec<FileEntry>, AppError> {
        let mut entries = Vec::new();

        for data_type in &device.data_types {
            for file_spec in &data_type.files {
                let should_restore = matches!(
                    file_spec.transfer_direction,
                    TransferDirection::InputToUnit | TransferDirection::InputOutput
                );
                if !should_restore {
                    continue;
                }

                let fit_type_dir = fit_type_dir_name(&data_type.name);
                let backup_dir = sync_dir.join(&fit_type_dir);
                let device_dest_dir = &file_spec.location.path;

                if !backup_dir.is_dir() {
                    continue;
                }

                if let Ok(mut read_dir) = fs::read_dir(&backup_dir).await {
                    while let Ok(Some(entry)) = read_dir.next_entry().await {
                        let path = entry.path();
                        if path.is_file() {
                            let size = entry.metadata().await.map(|m| m.len()).unwrap_or(0);
                            let fname = path.file_name().unwrap_or_default().to_owned();
                            entries.push(FileEntry {
                                source: path,
                                relative_dest: PathBuf::from(device_dest_dir).join(fname),
                                size,
                            });
                        }
                    }
                }
            }
        }

        Ok(entries)
    }
}

struct FileEntry {
    source: PathBuf,
    relative_dest: PathBuf,
    size: u64,
}

/// Stable `/` form for joining onto `mtp:/…` via [`device_fs::join_device_relative`].
fn pathbuf_as_posix_slash(pb: &Path) -> String {
    pb.components()
        .map(|c| c.as_os_str().to_string_lossy())
        .collect::<Vec<_>>()
        .join("/")
}

fn fit_type_dir_name(data_type_name: &str) -> String {
    if data_type_name.starts_with("FIT") {
        data_type_name.to_string()
    } else {
        format!("OTHER_{}", data_type_name)
    }
}

fn garmin_data_dir() -> PathBuf {
    if cfg!(target_os = "windows") {
        let appdata = std::env::var("PROGRAMDATA").unwrap_or_else(|_| "C:\\ProgramData".into());
        PathBuf::from(appdata).join("Garmin")
    } else {
        std::env::var("HOME")
            .map(PathBuf::from)
            .unwrap_or_else(|_| PathBuf::from("/tmp"))
            .join("Documents")
            .join("Garmin")
            .join("Data")
    }
}
