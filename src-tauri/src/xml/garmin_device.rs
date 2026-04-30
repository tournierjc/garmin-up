use std::path::Path;

use quick_xml::de::from_str;
use serde::{Deserialize, Serialize};

use crate::error::AppError;

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename = "Device")]
pub struct GarminDevice {
    #[serde(rename = "Model")]
    pub model: Model,

    #[serde(rename = "Id")]
    pub id: String,

    #[serde(rename = "Unlock", default)]
    pub unlocks: Vec<Unlock>,

    #[serde(rename = "MassStorageMode")]
    pub mass_storage_mode: MassStorageMode,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Model {
    #[serde(rename = "PartNumber")]
    pub part_number: String,

    #[serde(rename = "SoftwareVersion")]
    pub software_version: String,

    #[serde(rename = "Description")]
    pub description: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Unlock {
    #[serde(rename = "Code")]
    pub code: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct MassStorageMode {
    #[serde(alias = "DataType", default)]
    pub data_types: Vec<DataType>,

    // Present on many devices; used by Garmin Express for map update requests.
    #[serde(alias = "UpdateFile", default)]
    pub update_files: Vec<UpdateFile>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UpdateFile {
    #[serde(alias = "FileName")]
    pub file_name: String,

    #[serde(alias = "PartNumber")]
    pub part_number: String,

    #[serde(alias = "Path")]
    pub path: String,

    #[serde(alias = "Version")]
    pub version: UpdateFileVersion,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UpdateFileVersion {
    #[serde(alias = "Major")]
    pub major: i32,

    #[serde(alias = "Minor")]
    pub minor: i32,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct DataType {
    #[serde(alias = "Name")]
    pub name: String,

    #[serde(alias = "File", default)]
    pub files: Vec<DataFile>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct DataFile {
    #[serde(alias = "Specification")]
    pub specification: FileSpecification,

    #[serde(alias = "Location")]
    pub location: FileLocation,

    #[serde(alias = "TransferDirection")]
    pub transfer_direction: TransferDirection,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct FileSpecification {
    #[serde(alias = "Identifier")]
    pub identifier: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct FileLocation {
    #[serde(alias = "Path")]
    pub path: String,

    #[serde(alias = "BaseName", default)]
    pub base_name: Option<String>,

    #[serde(alias = "FileExtension")]
    pub file_extension: String,
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub enum TransferDirection {
    InputToUnit,
    OutputFromUnit,
    InputOutput,
}

pub fn parse_file(path: &Path) -> Result<GarminDevice, AppError> {
    let content = std::fs::read_to_string(path)?;
    parse_str(&content)
}

pub fn parse_str(xml: &str) -> Result<GarminDevice, AppError> {
    let device: GarminDevice = from_str(xml)?;
    Ok(device)
}

#[allow(dead_code)]
pub fn parse_from_escaped_xml(escaped: &str) -> Result<GarminDevice, AppError> {
    let unescaped = escaped
        .replace("&lt;", "<")
        .replace("&gt;", ">")
        .replace("&amp;", "&")
        .replace("&#xD;\n", "\n")
        .replace("&#xD;", "\r");
    parse_str(&unescaped)
}
