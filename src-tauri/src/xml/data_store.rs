use std::collections::HashMap;
use std::path::Path;

use quick_xml::events::Event;
use quick_xml::Reader;
use serde::{Deserialize, Serialize};

use crate::error::AppError;

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct DeviceDataStore {
    pub unit_id: String,
    pub strings: HashMap<String, String>,
    pub bools: HashMap<String, bool>,
    pub doubles: HashMap<String, f64>,
    pub string_lists: HashMap<String, Vec<String>>,
}

impl DeviceDataStore {
    pub fn friendly_name(&self) -> Option<&str> {
        self.strings.get("FriendlyName").map(|s| s.as_str())
    }

    pub fn registration_email(&self) -> Option<&str> {
        self.strings.get("RegistrationEmail").map(|s| s.as_str())
    }

    pub fn should_always_backup(&self) -> bool {
        self.bools.get("ShouldAlwaysBackup").copied().unwrap_or(false)
    }

    pub fn is_hybrid(&self) -> bool {
        self.bools.get("IsHybrid").copied().unwrap_or(false)
    }
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct DeviceListEntry {
    pub device_type: u32,
    pub manufacturer_id: u32,
    pub pairing_status: String,
    pub device_xml: Option<super::garmin_device::GarminDevice>,
}

pub fn parse_device_data_store(path: &Path) -> Result<Vec<DeviceDataStore>, AppError> {
    let content = std::fs::read_to_string(path)?;
    parse_device_data_store_str(&content)
}

fn parse_device_data_store_str(xml: &str) -> Result<Vec<DeviceDataStore>, AppError> {
    let mut reader = Reader::from_str(xml);
    let mut buf = Vec::new();
    let mut stores = Vec::new();
    let mut current_store: Option<DeviceDataStore> = None;

    #[derive(Debug, Clone, Copy, PartialEq)]
    enum OuterSection {
        None,
        Strings,
        Bools,
        Doubles,
        StringLists,
    }

    #[derive(Debug, PartialEq)]
    enum Field {
        None,
        UnitId,
        Key,
        Value,
    }

    let mut outer = OuterSection::None;
    let mut field = Field::None;
    let mut in_kv = false;
    let mut current_key = String::new();
    let mut current_value = String::new();
    let mut is_nil = false;

    loop {
        match reader.read_event_into(&mut buf) {
            Ok(Event::Start(e)) | Ok(Event::Empty(e)) => {
                let local = local_name(e.name().as_ref());
                match local.as_str() {
                    "DataStoreEntry" => {
                        current_store = Some(DeviceDataStore::default());
                        outer = OuterSection::None;
                    }
                    "UnitId" => field = Field::UnitId,
                    "Strings" => outer = OuterSection::Strings,
                    "Bools" => outer = OuterSection::Bools,
                    "Doubles" => outer = OuterSection::Doubles,
                    "StringLists" => outer = OuterSection::StringLists,
                    tag if tag.starts_with("KeyValuePair") => {
                        in_kv = true;
                        current_key.clear();
                        current_value.clear();
                        is_nil = false;
                    }
                    "key" if in_kv => field = Field::Key,
                    "value" if in_kv => {
                        field = Field::Value;
                        is_nil = e.attributes().any(|a| {
                            a.map(|a| {
                                let key = local_name(a.key.as_ref());
                                key == "nil" && a.value.as_ref() == b"true"
                            })
                            .unwrap_or(false)
                        });
                    }
                    _ => {}
                }
            }
            Ok(Event::Text(e)) => {
                let text = e.unescape().unwrap_or_default().to_string();
                match field {
                    Field::UnitId => {
                        if let Some(ref mut store) = current_store {
                            store.unit_id = text;
                        }
                    }
                    Field::Key => current_key = text,
                    Field::Value => current_value = text,
                    Field::None => {}
                }
            }
            Ok(Event::End(e)) => {
                let local = local_name(e.name().as_ref());
                match local.as_str() {
                    "DataStoreEntry" => {
                        if let Some(store) = current_store.take() {
                            stores.push(store);
                        }
                        outer = OuterSection::None;
                    }
                    "UnitId" | "key" | "value" => {
                        field = Field::None;
                    }
                    tag if tag.starts_with("KeyValuePair") && in_kv => {
                        if let Some(ref mut store) = current_store {
                            if !is_nil && !current_key.is_empty() {
                                match outer {
                                    OuterSection::Strings => {
                                        store
                                            .strings
                                            .insert(current_key.clone(), current_value.clone());
                                    }
                                    OuterSection::Bools => {
                                        store.bools.insert(
                                            current_key.clone(),
                                            current_value == "true",
                                        );
                                    }
                                    OuterSection::Doubles => {
                                        if let Ok(v) = current_value.parse::<f64>() {
                                            store.doubles.insert(current_key.clone(), v);
                                        }
                                    }
                                    _ => {}
                                }
                            }
                        }
                        in_kv = false;
                    }
                    "Strings" | "Bools" | "Doubles" | "StringLists" | "Ints" | "Longs" => {
                        outer = OuterSection::None;
                    }
                    _ => {}
                }
            }
            Ok(Event::Eof) => break,
            Err(e) => return Err(AppError::Other(format!("XML parse error: {}", e))),
            _ => {}
        }
        buf.clear();
    }

    Ok(stores)
}

pub fn parse_devices_list(path: &Path) -> Result<Vec<DeviceListEntry>, AppError> {
    let content = std::fs::read_to_string(path)?;
    parse_devices_list_str(&content)
}

fn parse_devices_list_str(xml: &str) -> Result<Vec<DeviceListEntry>, AppError> {
    let mut reader = Reader::from_str(xml);
    let mut buf = Vec::new();
    let mut entries = Vec::new();
    let mut current: Option<DeviceListEntry> = None;

    enum Field {
        None,
        DeviceType,
        ManufacturerId,
        PairingStatus,
        DeviceXml,
    }

    let mut field = Field::None;

    loop {
        match reader.read_event_into(&mut buf) {
            Ok(Event::Start(e)) => {
                let local = local_name(e.name().as_ref());
                match local.as_str() {
                    "anyType" => {
                        current = Some(DeviceListEntry::default());
                    }
                    "DeviceType" => field = Field::DeviceType,
                    "ManufacturerId" => field = Field::ManufacturerId,
                    "PairingStatus" => field = Field::PairingStatus,
                    "DeviceXml" => field = Field::DeviceXml,
                    _ => {}
                }
            }
            Ok(Event::Text(e)) => {
                let text = e.unescape().unwrap_or_default().to_string();
                if let Some(ref mut entry) = current {
                    match field {
                        Field::DeviceType => {
                            entry.device_type = text.parse().unwrap_or(0);
                        }
                        Field::ManufacturerId => {
                            entry.manufacturer_id = text.parse().unwrap_or(0);
                        }
                        Field::PairingStatus => {
                            entry.pairing_status = text;
                        }
                        Field::DeviceXml => {
                            entry.device_xml =
                                super::garmin_device::parse_from_escaped_xml(&text).ok();
                        }
                        Field::None => {}
                    }
                }
                field = Field::None;
            }
            Ok(Event::End(e)) => {
                let local = local_name(e.name().as_ref());
                if local == "anyType" {
                    if let Some(entry) = current.take() {
                        entries.push(entry);
                    }
                }
            }
            Ok(Event::Eof) => break,
            Err(e) => return Err(AppError::Other(format!("XML parse error: {}", e))),
            _ => {}
        }
        buf.clear();
    }

    Ok(entries)
}

fn local_name(name: &[u8]) -> String {
    let s = std::str::from_utf8(name).unwrap_or("");
    s.rsplit_once(':').map(|(_, local)| local).unwrap_or(s).to_string()
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::path::PathBuf;

    fn data_store_path() -> PathBuf {
        PathBuf::from(env!("HOME"))
            .join("Documents/Garmin/Data/CoreService/device_data_store.xml")
    }

    fn devices_list_path() -> PathBuf {
        PathBuf::from(env!("HOME"))
            .join("Documents/Garmin/Data/CoreService/devices_list.xml")
    }

    #[test]
    fn parse_real_device_data_store() {
        let path = data_store_path();
        if !path.exists() {
            eprintln!("Skipping: test data not found at {}", path.display());
            return;
        }

        let stores = parse_device_data_store(&path).expect("Failed to parse device_data_store.xml");
        assert!(!stores.is_empty(), "Expected at least one data store entry");

        let store = stores.iter().find(|s| s.unit_id == "3429806241").expect("Expected unit 3429806241");
        assert_eq!(store.friendly_name(), Some("fenix 6 Pro"));
        assert_eq!(store.registration_email(), Some("mail@tournierjc.fr"));
        assert!(store.should_always_backup());
        assert!(store.is_hybrid());

        let speed = store.doubles.get("HistoricalTransferSpeed");
        assert!(speed.is_some());
        assert!(*speed.unwrap() > 0.0);
    }

    #[test]
    fn parse_real_devices_list() {
        let path = devices_list_path();
        if !path.exists() {
            eprintln!("Skipping: test data not found at {}", path.display());
            return;
        }

        let entries = parse_devices_list(&path).expect("Failed to parse devices_list.xml");
        assert!(!entries.is_empty(), "Expected at least one device");

        let entry = &entries[0];
        assert_eq!(entry.device_type, 3290);
        assert_eq!(entry.manufacturer_id, 1);
        assert_eq!(entry.pairing_status, "PairingUnknown");

        let device_xml = entry.device_xml.as_ref().expect("Expected embedded DeviceXml");
        assert_eq!(device_xml.model.description, "fenix 6 Pro");
        assert_eq!(device_xml.model.part_number, "006-B3290-00");
        assert_eq!(device_xml.model.software_version, "2802");
        assert_eq!(device_xml.id, "3429806241");
        assert_eq!(device_xml.unlocks.len(), 6);
        assert!(!device_xml.mass_storage_mode.data_types.is_empty());
    }
}
