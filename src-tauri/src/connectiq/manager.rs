use std::path::{Path, PathBuf};
use serde::Serialize;
use tokio::fs;

use crate::error::AppError;

const IQ_APPS_DIR: &str = "Garmin/Apps";
const IQ_WATCHFACES_DIR: &str = "Garmin/Apps";

#[derive(Debug, Clone, Serialize)]
pub struct InstalledApp {
    pub file_name: String,
    pub path: PathBuf,
    pub size: u64,
    pub app_type: AppType,
}

#[derive(Debug, Clone, Serialize)]
pub enum AppType {
    WatchApp,
    WatchFace,
    DataField,
    Widget,
    Unknown,
}

pub async fn scan_installed_apps(device_mount: &Path) -> Result<Vec<InstalledApp>, AppError> {
    let apps_dir = device_mount.join(IQ_APPS_DIR);
    let mut apps = Vec::new();

    if !apps_dir.exists() {
        return Ok(apps);
    }

    let mut entries = fs::read_dir(&apps_dir).await?;
    while let Some(entry) = entries.next_entry().await? {
        let path = entry.path();
        let ext = path.extension().and_then(|e| e.to_str()).unwrap_or("");

        if ext == "prg" || ext == "PRG" {
            let metadata = entry.metadata().await?;
            let file_name = path.file_name()
                .and_then(|n| n.to_str())
                .unwrap_or("")
                .to_string();

            apps.push(InstalledApp {
                file_name,
                path,
                size: metadata.len(),
                app_type: AppType::Unknown,
            });
        }
    }

    apps.sort_by(|a, b| a.file_name.cmp(&b.file_name));
    Ok(apps)
}

pub async fn remove_app(device_mount: &Path, file_name: &str) -> Result<(), AppError> {
    let path = device_mount.join(IQ_APPS_DIR).join(file_name);
    if path.exists() {
        fs::remove_file(&path).await?;
    }

    let settings_name = file_name.replace(".prg", ".set").replace(".PRG", ".SET");
    let settings_path = device_mount.join("Garmin/Apps/SETTINGS").join(&settings_name);
    if settings_path.exists() {
        fs::remove_file(&settings_path).await?;
    }

    let data_name = file_name.replace(".prg", ".dat").replace(".PRG", ".DAT");
    let data_path = device_mount.join("Garmin/Apps/DATA").join(&data_name);
    if data_path.exists() {
        fs::remove_file(&data_path).await?;
    }

    Ok(())
}

pub async fn install_app(
    source: &Path,
    device_mount: &Path,
    file_name: &str,
) -> Result<InstalledApp, AppError> {
    let dest_dir = device_mount.join(IQ_APPS_DIR);
    fs::create_dir_all(&dest_dir).await?;

    let dest = dest_dir.join(file_name);
    fs::copy(source, &dest).await?;

    let metadata = fs::metadata(&dest).await?;
    Ok(InstalledApp {
        file_name: file_name.to_string(),
        path: dest,
        size: metadata.len(),
        app_type: AppType::Unknown,
    })
}
