use tauri::{AppHandle, Emitter, State};
use tokio::sync::mpsc;

use crate::backup::{BackupManager, BackupResult};
use crate::device::DeviceState;
use crate::error::AppError;

#[tauri::command]
pub async fn backup_device(
    app: AppHandle,
    state: State<'_, DeviceState>,
    unit_id: String,
) -> Result<BackupResult, AppError> {
    let device = {
        let devices = state.devices.lock().await;
        devices
            .iter()
            .find(|d| d.unit_id == unit_id)
            .cloned()
            .ok_or_else(|| AppError::DeviceNotFound(unit_id.clone()))?
    };

    let (tx, mut rx) = mpsc::channel(32);

    let app_clone = app.clone();
    tauri::async_runtime::spawn(async move {
        while let Some(progress) = rx.recv().await {
            let _ = app_clone.emit("backup-progress", &progress);
        }
    });

    let manager = BackupManager::new();
    manager.backup_device(&device, Some(tx)).await
}

#[tauri::command]
pub async fn restore_device(
    app: AppHandle,
    state: State<'_, DeviceState>,
    unit_id: String,
) -> Result<BackupResult, AppError> {
    let device = {
        let devices = state.devices.lock().await;
        devices
            .iter()
            .find(|d| d.unit_id == unit_id)
            .cloned()
            .ok_or_else(|| AppError::DeviceNotFound(unit_id.clone()))?
    };

    let (tx, mut rx) = mpsc::channel(32);

    let app_clone = app.clone();
    tauri::async_runtime::spawn(async move {
        while let Some(progress) = rx.recv().await {
            let _ = app_clone.emit("restore-progress", &progress);
        }
    });

    let manager = BackupManager::new();
    manager.restore_device(&device, Some(tx)).await
}
