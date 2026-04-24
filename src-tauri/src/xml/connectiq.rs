#![allow(dead_code)]

use std::path::Path;

use quick_xml::de::from_str;
use serde::{Deserialize, Serialize};

use crate::error::AppError;

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename = "IqAppInfo")]
pub struct IqAppInfo {
    #[serde(rename = "StoreApps")]
    pub store_apps: StoreApps,

    #[serde(rename = "PreloadedApps", default)]
    pub preloaded_apps: Option<PreloadedApps>,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct StoreApps {
    #[serde(rename = "IqAppInfo.StoreAppInfo", default)]
    pub apps: Vec<StoreAppInfo>,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct PreloadedApps {
    #[serde(rename = "IqAppInfo.PreloadedAppInfo", default)]
    pub apps: Vec<PreloadedAppInfo>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct StoreAppInfo {
    #[serde(rename = "AppId")]
    pub app_id: String,

    #[serde(rename = "FileName")]
    pub file_name: String,

    #[serde(rename = "FileSize")]
    pub file_size: u64,

    #[serde(rename = "IsFavorite")]
    pub is_favorite: bool,

    #[serde(rename = "Name")]
    pub name: String,

    #[serde(rename = "Version")]
    pub version: u32,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct PreloadedAppInfo {
    #[serde(rename = "FileName")]
    pub file_name: String,

    #[serde(rename = "FileSize")]
    pub file_size: u64,
}

pub fn parse_file(path: &Path) -> Result<IqAppInfo, AppError> {
    let content = std::fs::read_to_string(path)?;
    parse_str(&content)
}

pub fn parse_str(xml: &str) -> Result<IqAppInfo, AppError> {
    let info: IqAppInfo = from_str(xml)?;
    Ok(info)
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::path::PathBuf;

    fn test_data_path() -> PathBuf {
        PathBuf::from(env!("HOME"))
            .join("Documents/Garmin/Data/CoreService/ConnectIq/3429806241/IQAppInfo.xml")
    }

    #[test]
    fn parse_real_iqappinfo() {
        let path = test_data_path();
        if !path.exists() {
            eprintln!("Skipping: test data not found at {}", path.display());
            return;
        }

        let info = parse_file(&path).expect("Failed to parse IQAppInfo.xml");

        assert!(!info.store_apps.apps.is_empty(), "Expected store apps");
        assert_eq!(info.store_apps.apps.len(), 5);

        let openrunner = &info.store_apps.apps[0];
        assert_eq!(openrunner.name, "OpenRunner");
        assert_eq!(openrunner.app_id, "51f89cb2-03c0-477d-a965-9727dadb7772");
        assert_eq!(openrunner.file_name, "E6984359");
        assert_eq!(openrunner.file_size, 32101);
        assert!(!openrunner.is_favorite);
        assert_eq!(openrunner.version, 43);

        let deezer = info.store_apps.apps.iter().find(|a| a.name == "Deezer").unwrap();
        assert_eq!(deezer.version, 68);
    }
}
