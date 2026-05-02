use std::path::{Path, PathBuf};
use serde::Serialize;
use tokio::fs;

use crate::device::{device_fs, resolve_garmin_volume_dir};
use crate::error::AppError;

#[derive(Debug, Clone, Serialize)]
pub struct InstalledApp {
    pub file_name: String,
    pub path: PathBuf,
    pub size: u64,
    pub app_type: AppType,
}

#[allow(dead_code)]
#[derive(Debug, Clone, Serialize)]
pub enum AppType {
    WatchApp,
    WatchFace,
    DataField,
    Widget,
    Unknown,
}

pub async fn scan_installed_apps(device_mount: &Path) -> Result<Vec<InstalledApp>, AppError> {
    let Some(garmin) = resolve_garmin_volume_dir(device_mount) else {
        return Ok(Vec::new());
    };
    let apps_dir = garmin.join("Apps");
    let mut apps = Vec::new();

    let names = match device_fs::read_dir_filenames(&apps_dir).await {
        Ok(n) => n,
        Err(_) => return Ok(Vec::new()),
    };

    for fnm in names {
        let lc = fnm.to_lowercase();
        if lc.ends_with(".prg") {
            let path = apps_dir.join(&fnm);
            let size = if device_fs::is_kio_uri(&path) {
                0
            } else {
                fs::metadata(&path).await?.len()
            };

            apps.push(InstalledApp {
                file_name: fnm.clone(),
                path,
                size,
                app_type: AppType::Unknown,
            });
        }
    }

    apps.sort_by(|a, b| a.file_name.cmp(&b.file_name));
    Ok(apps)
}

pub async fn remove_app(device_mount: &Path, file_name: &str) -> Result<(), AppError> {
    let Some(garmin) = resolve_garmin_volume_dir(device_mount) else {
        return Ok(());
    };
    let apps = garmin.join("Apps");

    let path = apps.join(file_name);
    device_fs::remove_file(&path).await?;

    let settings_name = file_name.replace(".prg", ".set").replace(".PRG", ".SET");
    let settings_path = apps.join("SETTINGS").join(&settings_name);
    device_fs::remove_file(&settings_path).await?;

    let data_name = file_name.replace(".prg", ".dat").replace(".PRG", ".DAT");
    let data_path = apps.join("DATA").join(&data_name);
    device_fs::remove_file(&data_path).await?;

    Ok(())
}

pub async fn install_app(
    source: &Path,
    device_mount: &Path,
    file_name: &str,
) -> Result<InstalledApp, AppError> {
    let garmin = resolve_garmin_volume_dir(device_mount).ok_or_else(|| {
        AppError::Other(format!(
            "Device mount has neither GARMIN nor Garmin folder: {}",
            device_mount.display()
        ))
    })?;
    let dest_dir = garmin.join("Apps");
    device_fs::create_dir(&dest_dir).await?;

    let dest = dest_dir.join(file_name);
    device_fs::copy_local_to(source, &dest).await?;

    let size = if device_fs::is_kio_uri(&dest) {
        0
    } else {
        fs::metadata(&dest).await?.len()
    };

    Ok(InstalledApp {
        file_name: file_name.to_string(),
        path: dest,
        size,
        app_type: AppType::Unknown,
    })
}
