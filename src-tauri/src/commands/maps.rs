use std::path::PathBuf;
use tauri::State;

use crate::device::DeviceState;
use crate::error::AppError;
use crate::maps::omt::{MapInstaller, MapUpdateClient};
use crate::maps::scanner::{self, InstalledMap};
use serde::Serialize;

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

#[tauri::command]
pub async fn check_map_updates(
    unit_id: String,
    state: State<'_, DeviceState>,
) -> Result<Vec<MapUpdateSummary>, AppError> {
    let devices = state.devices.lock().await;
    let device = devices
        .iter()
        .find(|d| d.unit_id == unit_id)
        .ok_or_else(|| AppError::DeviceNotFound(unit_id.clone()))?;

    let client = MapUpdateClient::new()?;
    let serial = client
        .get_unit_serial_number(&device.unit_id, &device.part_number)
        .await?;

    let resp = client
        .get_preloaded_map_updates(&device.unit_id, &serial)
        .await?;
    Ok(resp
        .map_updates
        .into_iter()
        .map(MapUpdateSummary::from_proto)
        .collect())
}

#[derive(Debug, Clone, Serialize)]
pub struct MapUpdatesDebug {
    pub serial: String,
    pub map_updates: Vec<MapUpdateSummary>,
    pub purchasable_products: Vec<String>,
    pub auto_check_enabled: Option<bool>,
}

#[tauri::command]
pub async fn check_map_updates_debug(
    unit_id: String,
    state: State<'_, DeviceState>,
) -> Result<MapUpdatesDebug, AppError> {
    let devices = state.devices.lock().await;
    let device = devices
        .iter()
        .find(|d| d.unit_id == unit_id)
        .ok_or_else(|| AppError::DeviceNotFound(unit_id.clone()))?;

    let client = MapUpdateClient::new()?;
    let serial = client
        .get_unit_serial_number(&device.unit_id, &device.part_number)
        .await?;

    let resp = client
        .get_preloaded_map_updates(&device.unit_id, &serial)
        .await?;

    Ok(MapUpdatesDebug {
        serial,
        map_updates: resp.map_updates.into_iter().map(MapUpdateSummary::from_proto).collect(),
        purchasable_products: resp.purchasable_products,
        auto_check_enabled: resp
            .auto_check_settings
            .as_ref()
            .map(|s| s.is_auto_check_enabled),
    })
}

#[derive(Debug, Clone, Serialize)]
pub struct MapUpdateSummary {
    pub product_group: String,
    pub display_name: String,
    pub version: String,
    pub part_number: String,
    pub update_type: i32,
    pub can_auto_start_download: bool,
}

impl MapUpdateSummary {
    fn from_proto(u: crate::maps::omt::proto::MapUpdateInfo) -> Self {
        Self {
            product_group: u.product_group,
            display_name: u.display_name,
            version: format!("{}.{}", u.major_version, u.minor_version),
            part_number: u.part_number,
            update_type: u.update_type,
            can_auto_start_download: u.can_auto_start_download,
        }
    }
}

#[tauri::command]
pub async fn download_and_install_map_update(
    unit_id: String,
    part_number: String,
    state: State<'_, DeviceState>,
) -> Result<Vec<String>, AppError> {
    let devices = state.devices.lock().await;
    let device = devices
        .iter()
        .find(|d| d.unit_id == unit_id)
        .ok_or_else(|| AppError::DeviceNotFound(unit_id.clone()))?;

    let mount = PathBuf::from(&device.mount_path);

    // Needed to build a DownloadDetailsRequest
    let serial = MapUpdateClient::new()?
        .get_unit_serial_number(&device.unit_id, &device.part_number)
        .await?;

    let details = MapUpdateClient::new()?
        .get_download_details(
            &device.unit_id,
            &serial,
            &device.part_number,
            &device.software_version,
            &part_number,
        )
        .await?;

    drop(devices);

    let installed = MapInstaller::install_download_details_to_device(&details, &mount).await?;
    Ok(installed
        .into_iter()
        .map(|p| p.display().to_string())
        .collect())
}
