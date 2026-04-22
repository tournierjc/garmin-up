use tauri::State;

use crate::device::{DetectedDevice, DeviceState};
use crate::error::AppError;

#[tauri::command]
pub async fn list_devices(state: State<'_, DeviceState>) -> Result<Vec<DetectedDevice>, AppError> {
    let devices = state.devices.lock().await;
    Ok(devices.clone())
}

#[tauri::command]
pub async fn get_device_info(
    state: State<'_, DeviceState>,
    unit_id: String,
) -> Result<DetectedDevice, AppError> {
    let devices = state.devices.lock().await;
    devices
        .iter()
        .find(|d| d.unit_id == unit_id)
        .cloned()
        .ok_or_else(|| AppError::DeviceNotFound(unit_id))
}

#[tauri::command]
pub async fn refresh_devices(state: State<'_, DeviceState>) -> Result<Vec<DetectedDevice>, AppError> {
    let detected = crate::device::detector::scan_for_devices();
    let mut devices = state.devices.lock().await;
    *devices = detected.clone();
    Ok(detected)
}
