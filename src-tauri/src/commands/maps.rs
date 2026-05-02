use std::path::PathBuf;
use tauri::State;

use crate::device::{device_fs, resolve_garmin_volume_dir, join_uri_leaf, DeviceState};
use crate::error::AppError;
use crate::maps::omt::{MapInstaller, MapUpdateClient};
use crate::xml::garmin_device;
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

    // Match Garmin Express: JSON request body to /Rce/ProtobufApi/MapUpdateService/GetPreloadedMapUpdates
    let client = MapUpdateClient::new()?;
    let mount = PathBuf::from(&device.mount_path);
    let device_xml = match resolve_garmin_volume_dir(&mount) {
        Some(vol) => {
            let xml_path = join_uri_leaf(&vol, "GarminDevice.xml");
            device_fs::read_to_string(&xml_path).await.ok()
        }
        None => None,
    };

    let resp = client
        .get_preloaded_map_updates_json(&device.unit_id, &device.part_number, device_xml)
        .await?;

    Ok(resp
        .1
        .map_updates
        .unwrap_or_default()
        .into_iter()
        .map(MapUpdateSummary::from_json)
        .collect())
}

#[derive(Debug, Clone, Serialize)]
pub struct MapUpdatesDebug {
    pub serial: String,
    pub map_updates: Vec<MapUpdateSummary>,
    pub purchasable_products: Vec<String>,
    pub auto_check_enabled: Option<bool>,
    pub verbose_details_len: Option<usize>,
    pub verbose_details_sample: Option<Vec<(String, String)>>,
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
    let mount = PathBuf::from(&device.mount_path);
    let device_xml = match resolve_garmin_volume_dir(&mount) {
        Some(vol) => {
            let xml_path = join_uri_leaf(&vol, "GarminDevice.xml");
            device_fs::read_to_string(&xml_path).await.ok()
        }
        None => None,
    };

    let json = client
        .get_preloaded_map_updates_json(&device.unit_id, &device.part_number, device_xml)
        .await?;

    Ok(MapUpdatesDebug {
        serial: json.0,
        map_updates: json
            .1
            .map_updates
            .unwrap_or_default()
            .into_iter()
            .map(MapUpdateSummary::from_json)
            .collect(),
        purchasable_products: vec![],
        auto_check_enabled: json.1.auto_check_settings.map(|s| s.is_auto_check_enabled),
        verbose_details_len: None,
        verbose_details_sample: None,
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
    fn from_json(u: crate::maps::omt::JsonMapUpdateInfo) -> Self {
        Self {
            product_group: u.product_group.unwrap_or_default(),
            display_name: u.display_name.unwrap_or_default(),
            version: format!("{}.{}", u.major_version, u.minor_version),
            part_number: u.part_number.unwrap_or_default(),
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

    // Match Garmin Express: GetDownloadDetails uses JSON body with FullUnitInfo derived from GarminDevice.xml.
    let garmin_vol =
        resolve_garmin_volume_dir(&mount).ok_or_else(|| {
            AppError::Other(format!(
                "Device mount has neither GARMIN nor Garmin folder: {}",
                mount.display()
            ))
        })?;
    let xml_path = join_uri_leaf(&garmin_vol, "GarminDevice.xml");
    let parsed = garmin_device::parse_file(&xml_path)?;

    let full_unit_info = crate::maps::omt::JsonFullUnitInfo {
        unit_id: parsed.id.parse::<i64>().unwrap_or(0),
        first_fix: None,
        serial_number: None,
        software_part_number: parsed.model.part_number.clone(),
        software_version: parsed.model.software_version.clone(),
        update_files: parsed
            .mass_storage_mode
            .update_files
            .into_iter()
            .map(|u| crate::maps::omt::JsonUnitUpdateFile {
                file_name: u.file_name,
                major_version: u.version.major,
                minor_version: u.version.minor,
                part_number: u.part_number,
                path: u.path,
            })
            .collect(),
        data_types: parsed
            .mass_storage_mode
            .data_types
            .into_iter()
            .map(|dt| crate::maps::omt::JsonUnitDataType {
                name: dt.name,
                locations: dt
                    .files
                    .into_iter()
                    .map(|f| crate::maps::omt::JsonUnitDataTypeLocation {
                        base_name: f.location.base_name.unwrap_or_default(),
                        extension: f.location.file_extension,
                        path: f.location.path,
                    })
                    .collect(),
            })
            .collect(),
    };

    let details = MapUpdateClient::new()?
        .get_download_details_json(full_unit_info, &part_number)
        .await?;

    drop(devices);

    let installed = MapInstaller::install_download_details_json_to_device(&details, &mount).await?;
    Ok(installed
        .into_iter()
        .map(|p| p.display().to_string())
        .collect())
}
