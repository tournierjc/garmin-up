use std::path::{Path, PathBuf};

use tauri::State;

use crate::device::DeviceState;
use crate::error::AppError;

fn sanitize_file_name(file_name: &str) -> Option<&str> {
    let name = file_name.trim();
    if name.is_empty() {
        return None;
    }
    if name.contains('/') || name.contains('\\') || name.contains("..") {
        return None;
    }
    Some(name)
}

async fn ensure_music_dir(mount: &Path) -> Result<PathBuf, AppError> {
    // Most Garmin mass-storage devices use GARMIN/MUSIC.
    let garmin_music = mount.join("GARMIN").join("MUSIC");
    tokio::fs::create_dir_all(&garmin_music).await?;
    Ok(garmin_music)
}

#[tauri::command]
pub async fn install_music_files(
    unit_id: String,
    source_paths: Vec<String>,
    state: State<'_, DeviceState>,
) -> Result<Vec<String>, AppError> {
    let devices = state.devices.lock().await;
    let device = devices
        .iter()
        .find(|d| d.unit_id == unit_id)
        .ok_or_else(|| AppError::DeviceNotFound(unit_id.clone()))?;

    let mount = PathBuf::from(&device.mount_path);
    drop(devices);

    let dest_dir = ensure_music_dir(&mount).await?;
    let mut installed = Vec::new();

    for src in source_paths {
        let src_path = PathBuf::from(&src);
        let file_name = src_path
            .file_name()
            .and_then(|n| n.to_str())
            .and_then(sanitize_file_name)
            .ok_or_else(|| AppError::Other(format!("Invalid music file name: {src}")))?;

        let dest = dest_dir.join(file_name);
        tokio::fs::copy(&src_path, &dest).await?;
        installed.push(dest.display().to_string());
    }

    Ok(installed)
}

