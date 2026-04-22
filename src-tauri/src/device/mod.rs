pub mod detector;
pub mod monitor;

use serde::{Deserialize, Serialize};
use std::path::PathBuf;
use tokio::sync::Mutex;

use crate::xml::garmin_device::{GarminDevice, DataType};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct DetectedDevice {
    pub mount_path: PathBuf,
    pub unit_id: String,
    pub description: String,
    pub part_number: String,
    pub software_version: String,
    pub data_types: Vec<DataType>,
}

impl DetectedDevice {
    pub fn from_mount(mount_path: PathBuf, device: GarminDevice) -> Self {
        Self {
            mount_path,
            unit_id: device.id.clone(),
            description: device.model.description.clone(),
            part_number: device.model.part_number.clone(),
            software_version: device.model.software_version.clone(),
            data_types: device.mass_storage_mode.data_types,
        }
    }
}

#[derive(Default)]
pub struct DeviceState {
    pub devices: Mutex<Vec<DetectedDevice>>,
}
