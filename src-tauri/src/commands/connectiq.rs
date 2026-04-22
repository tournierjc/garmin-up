use std::path::PathBuf;
use tauri::State;

use crate::connectiq::manager::{self, InstalledApp};
use crate::device::DeviceState;
use crate::error::AppError;

#[tauri::command]
pub async fn list_iq_apps(
    unit_id: String,
    state: State<'_, DeviceState>,
) -> Result<Vec<InstalledApp>, AppError> {
    let devices = state.devices.lock().await;
    let device = devices.iter()
        .find(|d| d.unit_id == unit_id)
        .ok_or_else(|| AppError::DeviceNotFound(unit_id.clone()))?;

    let mount = PathBuf::from(&device.mount_path);
    drop(devices);

    manager::scan_installed_apps(&mount).await
}

#[tauri::command]
pub async fn install_iq_app(
    unit_id: String,
    source_path: String,
    file_name: String,
    state: State<'_, DeviceState>,
) -> Result<InstalledApp, AppError> {
    let devices = state.devices.lock().await;
    let device = devices.iter()
        .find(|d| d.unit_id == unit_id)
        .ok_or_else(|| AppError::DeviceNotFound(unit_id.clone()))?;

    let mount = PathBuf::from(&device.mount_path);
    drop(devices);

    manager::install_app(&PathBuf::from(source_path), &mount, &file_name).await
}

#[tauri::command]
pub async fn remove_iq_app(
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

    manager::remove_app(&mount, &file_name).await
}
