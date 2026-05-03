use std::collections::HashSet;
use std::path::{Path, PathBuf};
use serde::Serialize;
use tokio::fs;

use crate::device::{device_fs, resolve_garmin_volume_dir};
use crate::error::AppError;

const MAP_FILES: &[&str] = &[
    "gmapprom.img",
    "gmapsupp.img",
    "gmapbmap.img",
    "gmaptz.img",
    "gmaptopo.img",
];

#[derive(Debug, Clone, Serialize)]
pub struct InstalledMap {
    pub file_name: String,
    pub path: PathBuf,
    pub size: u64,
    pub map_type: MapType,
}

#[derive(Debug, Clone, Serialize)]
pub enum MapType {
    Base,
    Supplemental,
    Basemap,
    Timezone,
    Topo,
    Unknown,
}

pub async fn scan_installed_maps(device_mount: &Path) -> Result<Vec<InstalledMap>, AppError> {
    let Some(garmin_dir) = resolve_garmin_volume_dir(device_mount) else {
        return Ok(Vec::new());
    };
    let mut maps = Vec::new();

    let entries = device_fs::read_dir_filenames(&garmin_dir).await?;

    let map_set_lc: HashSet<String> =
        MAP_FILES.iter().map(|s| (*s).to_lowercase()).collect();

    for map_file in MAP_FILES {
        if let Some(found) = entries
            .iter()
            .find(|e| e.eq_ignore_ascii_case(map_file))
        {
            let path = device_fs::join_uri_leaf(&garmin_dir, found);
            let size = if device_fs::is_kio_uri(&path) {
                0
            } else if path.exists() {
                fs::metadata(&path).await?.len()
            } else {
                0
            };

            let map_type = match *map_file {
                "gmapprom.img" => MapType::Base,
                "gmapsupp.img" => MapType::Supplemental,
                "gmapbmap.img" => MapType::Basemap,
                "gmaptz.img" => MapType::Timezone,
                "gmaptopo.img" => MapType::Topo,
                _ => MapType::Unknown,
            };
            maps.push(InstalledMap {
                file_name: found.clone(),
                path,
                size,
                map_type,
            });
        }
    }

    for name in &entries {
        let lc = name.to_lowercase();
        if lc.ends_with(".img") && !map_set_lc.contains(&lc) {
            let path = device_fs::join_uri_leaf(&garmin_dir, name);
            let size = if device_fs::is_kio_uri(&path) {
                0
            } else {
                fs::metadata(&path).await?.len()
            };
            maps.push(InstalledMap {
                file_name: name.clone(),
                path,
                size,
                map_type: MapType::Unknown,
            });
        }
    }

    Ok(maps)
}

pub async fn install_map(
    source: &Path,
    device_mount: &Path,
    file_name: &str,
) -> Result<InstalledMap, AppError> {
    let garmin_dir = resolve_garmin_volume_dir(device_mount).ok_or_else(|| {
        AppError::Other(format!(
            "Device mount has neither GARMIN nor Garmin folder: {}",
            device_mount.display()
        ))
    })?;
    let dest = device_fs::join_uri_leaf(&garmin_dir, file_name);

    device_fs::create_dir(&garmin_dir).await?;
    device_fs::copy_local_to(source, &dest).await?;

    let size = if device_fs::is_kio_uri(&dest) {
        0
    } else {
        fs::metadata(&dest).await?.len()
    };

    Ok(InstalledMap {
        file_name: file_name.to_string(),
        path: dest,
        size,
        map_type: MapType::Supplemental,
    })
}

pub async fn remove_map(device_mount: &Path, file_name: &str) -> Result<(), AppError> {
    let Some(garmin_dir) = resolve_garmin_volume_dir(device_mount) else {
        return Ok(());
    };
    let path = device_fs::join_uri_leaf(&garmin_dir, file_name);

    device_fs::remove_file(&path).await?;
    Ok(())
}
