use std::path::{Path, PathBuf};

use sysinfo::Disks;
use tracing::{debug, info};

use super::DetectedDevice;
use super::device_fs::join_uri_leaf;
use crate::xml::garmin_device;

/// Mass-storage Garmin folder at the USB mount root: `GARMIN/` or `Garmin/` depending on device/host,
/// or KDE MTP URIs (`mtp:/…`) probed via `kioclient` (not visible to `std::fs`).
pub fn resolve_garmin_volume_dir(device_mount: &Path) -> Option<PathBuf> {
    #[cfg(target_os = "linux")]
    {
        let root = device_mount.to_string_lossy();
        if root.starts_with("mtp:") {
            let base_pb = PathBuf::from(root.trim_end_matches('/'));
            for name in ["GARMIN", "Garmin"] {
                let garmin_folder = join_uri_leaf(&base_pb, name);
                let probe = join_uri_leaf(&garmin_folder, "GarminDevice.xml");
                if crate::device::kio::cat_utf8(
                    &probe.to_string_lossy(),
                    crate::device::kio::KIO_FAST_TIMEOUT,
                )
                .is_ok()
                {
                    return Some(garmin_folder);
                }
            }
            return None;
        }
    }

    for garmin_dir_name in ["GARMIN", "Garmin"] {
        let garmin_dir = device_mount.join(garmin_dir_name);
        if garmin_dir.is_dir() {
            return Some(garmin_dir);
        }
    }
    None
}

pub fn scan_for_devices() -> Vec<DetectedDevice> {
    let mut devices = Vec::new();
    let disks = Disks::new_with_refreshed_list();

    for disk in disks.list() {
        let mount = disk.mount_point();
        let mut found = false;

        if let Some(garmin_dir) = resolve_garmin_volume_dir(mount) {
            debug!("Found Garmin volume at {}", garmin_dir.display());
            let xml_path = garmin_dir.join("GarminDevice.xml");
            if xml_path.is_file() {
                match garmin_device::parse_file(&xml_path) {
                    Ok(device) => {
                        info!(
                            "Detected device: {} (ID: {}) at {}",
                            device.model.description, device.id, mount.display()
                        );
                        devices.push(DetectedDevice::from_mount(mount.to_path_buf(), device));
                        found = true;
                    }
                    Err(e) => {
                        tracing::warn!(
                            "Failed to parse GarminDevice.xml at {}: {}",
                            xml_path.display(),
                            e
                        );
                    }
                }
            } else {
                debug!("No GarminDevice.xml at {}", xml_path.display());
            }
        }

        if found {
            continue;
        }
    }

    #[cfg(target_os = "linux")]
    scan_kio_mtp_devices(&mut devices);

    scan_garmin_data_dir(&mut devices);

    devices
}

#[cfg(target_os = "linux")]
fn scan_kio_mtp_devices(devices: &mut Vec<DetectedDevice>) {
    let device_names = match crate::device::kio::ls("mtp:/", crate::device::kio::KIO_FAST_TIMEOUT) {
        Ok(names) => names,
        Err(err) => {
            debug!("No KIO MTP devices available: {err}");
            return;
        }
    };

    for device_name in device_names {
        let device_root = format!("mtp:/{device_name}/");
        let mut candidate_roots = vec![device_root.clone()];

        if let Ok(storage_roots) = crate::device::kio::ls(&device_root, crate::device::kio::KIO_FAST_TIMEOUT) {
            candidate_roots.extend(
                storage_roots
                    .into_iter()
                    .map(|storage_root| format!("{device_root}{storage_root}/")),
            );
        }

        for candidate_root in candidate_roots {
            if try_add_kio_device(&candidate_root, devices) {
                break;
            }
        }
    }
}

#[cfg(target_os = "linux")]
fn try_add_kio_device(root_uri: &str, devices: &mut Vec<DetectedDevice>) -> bool {
    let root_pb = PathBuf::from(root_uri.trim_end_matches('/'));
    for garmin_dir_name in ["GARMIN", "Garmin"] {
        let garmin_folder = join_uri_leaf(&root_pb, garmin_dir_name);
        let xml_path = join_uri_leaf(&garmin_folder, "GarminDevice.xml");
        let xml_uri = xml_path.to_string_lossy().into_owned();

        let xml_content = match crate::device::kio::cat_utf8(&xml_uri, crate::device::kio::KIO_FAST_TIMEOUT) {
            Ok(content) => content,
            Err(_) => continue,
        };

        match garmin_device::parse_str(&xml_content) {
            Ok(device) => {
                if devices.iter().any(|existing| existing.unit_id == device.id) {
                    debug!("Skipping duplicate KIO MTP device {}", device.id);
                    return true;
                }

                let mount_path = PathBuf::from(root_uri.trim_end_matches('/'));
                info!(
                    "Detected KIO MTP device: {} (ID: {}) at {}",
                    device.model.description,
                    device.id,
                    mount_path.display()
                );
                devices.push(DetectedDevice::from_mount(mount_path, device));
                return true;
            }
            Err(err) => {
                tracing::warn!("Failed to parse GarminDevice.xml via {xml_uri}: {err}");
            }
        }
    }

    false
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
