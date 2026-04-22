use std::path::PathBuf;
use tauri::State;

use crate::device::DeviceState;
use crate::error::AppError;
use crate::firmware::checker::{FirmwareChecker, FirmwareInfo, UpdateCheckRequest};
use crate::firmware::installer::FirmwareInstaller;

#[tauri::command]
pub async fn check_firmware_update(
    unit_id: String,
    state: State<'_, DeviceState>,
) -> Result<FirmwareInfo, AppError> {
    let devices = state.devices.lock().await;
    let device = devices.iter()
        .find(|d| d.unit_id == unit_id)
        .ok_or_else(|| AppError::DeviceNotFound(unit_id.clone()))?;

    let request = UpdateCheckRequest {
        part_number: device.part_number.clone(),
        current_version: device.software_version.parse::<u32>().unwrap_or(0),
        unit_id: unit_id.clone(),
    };

    drop(devices);

    let checker = FirmwareChecker::new()?;
    checker.check_for_update(&request).await
}

#[tauri::command]
pub async fn install_firmware(
    unit_id: String,
    download_url: String,
    state: State<'_, DeviceState>,
) -> Result<FirmwareInfo, AppError> {
    let devices = state.devices.lock().await;
    let device = devices.iter()
        .find(|d| d.unit_id == unit_id)
        .ok_or_else(|| AppError::DeviceNotFound(unit_id.clone()))?;

    let mount_path = PathBuf::from(&device.mount_path);
    drop(devices);

    FirmwareInstaller::install_to_device(&download_url, &mount_path).await
}
