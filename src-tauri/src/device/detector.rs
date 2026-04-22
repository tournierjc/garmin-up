use std::path::PathBuf;

use sysinfo::Disks;
use tracing::{debug, info};

use super::DetectedDevice;
use crate::xml::garmin_device;

pub fn scan_for_devices() -> Vec<DetectedDevice> {
    let mut devices = Vec::new();
    let disks = Disks::new_with_refreshed_list();

    for disk in disks.list() {
        let mount = disk.mount_point();
        let garmin_dir = mount.join("GARMIN");

        if !garmin_dir.is_dir() {
            continue;
        }

        debug!("Found GARMIN directory at {}", mount.display());

        let xml_path = garmin_dir.join("GarminDevice.xml");
        if !xml_path.is_file() {
            debug!("No GarminDevice.xml at {}", xml_path.display());
            continue;
        }

        match garmin_device::parse_file(&xml_path) {
            Ok(device) => {
                info!(
                    "Detected device: {} (ID: {}) at {}",
                    device.model.description, device.id, mount.display()
                );
                devices.push(DetectedDevice::from_mount(mount.to_path_buf(), device));
            }
            Err(e) => {
                tracing::warn!(
                    "Failed to parse GarminDevice.xml at {}: {}",
                    xml_path.display(),
                    e
                );
            }
        }
    }

    scan_garmin_data_dir(&mut devices);

    devices
}

fn scan_garmin_data_dir(_devices: &mut Vec<DetectedDevice>) {
    let data_dir = dirs_garmin_data();
    if !data_dir.is_dir() {
        return;
    }

    let devices_list_path = data_dir.join("CoreService").join("devices_list.xml");
    if devices_list_path.is_file() {
        debug!("Found Garmin Express data at {}", data_dir.display());
    }
}

fn dirs_garmin_data() -> PathBuf {
    if cfg!(target_os = "windows") {
        let appdata = std::env::var("PROGRAMDATA").unwrap_or_else(|_| "C:\\ProgramData".into());
        PathBuf::from(appdata).join("Garmin")
    } else {
        dirs_home().join("Documents").join("Garmin")
    }
}

fn dirs_home() -> PathBuf {
    std::env::var("HOME")
        .map(PathBuf::from)
        .unwrap_or_else(|_| PathBuf::from("/tmp"))
}
