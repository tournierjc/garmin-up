use std::path::PathBuf;
use tauri::State;

use crate::device::DeviceState;
use crate::error::AppError;
use crate::maps::scanner::{self, InstalledMap};

#[tauri::command]
pub async fn list_maps(
    unit_id: String,
    state: State<'_, DeviceState>,
) -> Result<Vec<InstalledMap>, AppError> {
    let devices = state.devices.lock().await;
    let device = devices.iter()
        .find(|d| d.unit_id == unit_id)
        .ok_or_else(|| AppError::DeviceNotFound(unit_id.clone()))?;

    let mount = PathBuf::from(&device.mount_path);
    drop(devices);

    scanner::scan_installed_maps(&mount).await
}

#[tauri::command]
pub async fn install_map(
    unit_id: String,
    source_path: String,
    file_name: String,
    state: State<'_, DeviceState>,
) -> Result<InstalledMap, AppError> {
    let devices = state.devices.lock().await;
    let device = devices.iter()
        .find(|d| d.unit_id == unit_id)
        .ok_or_else(|| AppError::DeviceNotFound(unit_id.clone()))?;

    let mount = PathBuf::from(&device.mount_path);
    drop(devices);

    scanner::install_map(&PathBuf::from(source_path), &mount, &file_name).await
}

#[tauri::command]
pub async fn remove_map(
    unit_id: String,
    file_name: String,
    state: State<'_, DeviceState>,
) -> Result<(), AppError> {
    let devices = state.devices.lock().await;
    let device = devices.iter()
        .find(|d| d.unit_id == unit_id)
        .ok_or_else(|| AppError::DeviceNotFound(unit_id.clone()))?;

    let mount = PathBuf::from(&device.mount_path);
    drop(devices);

    scanner::remove_map(&mount, &file_name).await
}
