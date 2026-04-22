use std::path::{Path, PathBuf};
use serde::Serialize;
use tokio::fs;

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
    let garmin_dir = device_mount.join("Garmin");
    let mut maps = Vec::new();

    if !garmin_dir.exists() {
        return Ok(maps);
    }

    for map_file in MAP_FILES {
        let path = garmin_dir.join(map_file);
        if path.exists() {
            let metadata = fs::metadata(&path).await?;
            let map_type = match *map_file {
                "gmapprom.img" => MapType::Base,
                "gmapsupp.img" => MapType::Supplemental,
                "gmapbmap.img" => MapType::Basemap,
                "gmaptz.img" => MapType::Timezone,
                "gmaptopo.img" => MapType::Topo,
                _ => MapType::Unknown,
            };
            maps.push(InstalledMap {
                file_name: map_file.to_string(),
                path,
                size: metadata.len(),
                map_type,
            });
        }
    }

    let mut entries = fs::read_dir(&garmin_dir).await?;
    while let Some(entry) = entries.next_entry().await? {
        let name = entry.file_name();
        let name_str = name.to_string_lossy().to_lowercase();
        if name_str.ends_with(".img") && !MAP_FILES.contains(&name_str.as_str()) {
            let metadata = entry.metadata().await?;
            maps.push(InstalledMap {
                file_name: name.to_string_lossy().to_string(),
                path: entry.path(),
                size: metadata.len(),
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
    let dest = device_mount.join("Garmin").join(file_name);
    fs::create_dir_all(dest.parent().unwrap()).await?;
    fs::copy(source, &dest).await?;

    let metadata = fs::metadata(&dest).await?;
    Ok(InstalledMap {
        file_name: file_name.to_string(),
        path: dest,
        size: metadata.len(),
        map_type: MapType::Supplemental,
    })
}

pub async fn remove_map(device_mount: &Path, file_name: &str) -> Result<(), AppError> {
    let path = device_mount.join("Garmin").join(file_name);
    if path.exists() {
        fs::remove_file(&path).await?;
    }
    Ok(())
}
