use std::path::{Path, PathBuf};

use reqwest::Client;
use serde::Deserialize;
use prost::Message;

use crate::error::AppError;

pub mod proto {
    include!(concat!(env!("OUT_DIR"), "/garmin.omt.mapupdate.rs"));
}

const UNIT_INFO_BASE: &str = "https://omt.garmin.com/api/unit-info/manufacturers/Taiwan";
const PRELOADED_MAP_UPDATES_URL: &str =
    "https://omt.garmin.com/Rce/ProtobufApi/MapUpdateService/GetPreloadedMapUpdates";
const PRELOADED_MAP_UPDATES_VERBOSE_URL: &str =
    "https://omt.garmin.com/Rce/ProtobufApi/MapUpdateService/GetPreloadedMapUpdatesVerbose";
const DOWNLOAD_DETAILS_URL: &str =
    "https://omt.garmin.com/Rce/ProtobufApi/MapUpdateService/GetDownloadDetails";

#[derive(Debug, Clone, Deserialize)]
struct UnitInfoResponse {
    #[serde(rename = "serialNumber")]
    serial_number: String,
}

pub struct MapUpdateClient {
    http: Client,
}

impl MapUpdateClient {
    pub fn new() -> Result<Self, AppError> {
        let http = Client::builder()
            .user_agent("Garmin Express/7.28.0")
            .build()?;
        Ok(Self { http })
    }

    pub async fn get_unit_serial_number(
        &self,
        unit_id: &str,
        firmware_part_number: &str,
    ) -> Result<String, AppError> {
        let url = format!(
            "{UNIT_INFO_BASE}/unitids/{unit_id}?primaryFirmwarePartNumber={firmware_part_number}"
        );

        let resp = self
            .http
            .get(url)
            .header("Accept-Language", detect_accept_language())
            .send()
            .await?;
        if !resp.status().is_success() {
            return Err(AppError::Api {
                status: resp.status().as_u16(),
                message: "Failed to fetch unit-info".into(),
            });
        }

        let info: UnitInfoResponse = resp.json().await?;
        Ok(info.serial_number)
    }

    pub async fn get_preloaded_map_updates(
        &self,
        unit_id: &str,
        serial_number: &str,
    ) -> Result<proto::PreloadedMapUpdatesResponse, AppError> {
        let req = proto::PreloadedMapUpdatesRequest {
            client_info: Some(build_client_info()),
            basic_unit_info: Some(proto::BasicUnitInfo {
                unit_id: parse_unit_id(unit_id),
                serial_number: serial_number.to_string(),
                first_fix: None,
            }),
            is_user_interactive: true,
        };

        let bytes = req.encode_to_vec();

        let resp = self
            .http
            .post(PRELOADED_MAP_UPDATES_URL)
            .header("Content-Type", "application/x-protobuf")
            .header("Accept", "application/x-protobuf")
            .header("Accept-Language", detect_accept_language())
            .body(bytes)
            .send()
            .await?;

        if !resp.status().is_success() {
            return Err(AppError::Api {
                status: resp.status().as_u16(),
                message: resp.text().await.unwrap_or_default(),
            });
        }

        let body = resp.bytes().await?;
        let parsed = proto::PreloadedMapUpdatesResponse::decode(body.as_ref())?;
        Ok(parsed)
    }

    pub async fn get_preloaded_map_updates_verbose(
        &self,
        unit_id: &str,
        serial_number: &str,
    ) -> Result<proto::PreloadedMapUpdatesVerboseResponse, AppError> {
        let req = proto::PreloadedMapUpdatesRequest {
            client_info: Some(build_client_info()),
            basic_unit_info: Some(proto::BasicUnitInfo {
                unit_id: parse_unit_id(unit_id),
                serial_number: serial_number.to_string(),
                first_fix: None,
            }),
            is_user_interactive: true,
        };

        let bytes = req.encode_to_vec();
        let resp = self
            .http
            .post(PRELOADED_MAP_UPDATES_VERBOSE_URL)
            .header("Content-Type", "application/x-protobuf")
            .header("Accept", "application/x-protobuf")
            .header("Accept-Language", detect_accept_language())
            .body(bytes)
            .send()
            .await?;

        if !resp.status().is_success() {
            return Err(AppError::Api {
                status: resp.status().as_u16(),
                message: resp.text().await.unwrap_or_default(),
            });
        }

        let body = resp.bytes().await?;
        let parsed = proto::PreloadedMapUpdatesVerboseResponse::decode(body.as_ref())?;
        Ok(parsed)
    }

    pub async fn get_download_details(
        &self,
        unit_id: &str,
        serial_number: &str,
        software_part_number: &str,
        software_version: &str,
        part_number: &str,
    ) -> Result<proto::DownloadDetailsResponse, AppError> {
        let req = proto::DownloadDetailsRequest {
            client_info: Some(build_client_info()),
            full_unit_info: Some(proto::FullUnitInfo {
                unit_id: parse_unit_id(unit_id),
                serial_number: serial_number.to_string(),
                first_fix: None,
                software_part_number: software_part_number.to_string(),
                software_version: software_version.to_string(),
                supported_content_types: vec![],
                current_part_numbers: vec![],
            }),
            part_number: part_number.to_string(),
        };

        let bytes = req.encode_to_vec();
        let resp = self
            .http
            .post(DOWNLOAD_DETAILS_URL)
            .header("Content-Type", "application/x-protobuf")
            .header("Accept", "application/x-protobuf")
            .header("Accept-Language", detect_accept_language())
            .body(bytes)
            .send()
            .await?;

        if !resp.status().is_success() {
            return Err(AppError::Api {
                status: resp.status().as_u16(),
                message: resp.text().await.unwrap_or_default(),
            });
        }

        let body = resp.bytes().await?;
        let parsed = proto::DownloadDetailsResponse::decode(body.as_ref())?;
        Ok(parsed)
    }
}

fn parse_unit_id(unit_id: &str) -> i64 {
    unit_id.parse::<i64>().unwrap_or(0)
}

fn detect_accept_language() -> String {
    // Best-effort: derive from LANG (e.g. fr_FR.UTF-8) -> fr-FR.
    let lang = std::env::var("LANG").unwrap_or_else(|_| "en_US".into());
    let base = lang.split('.').next().unwrap_or("en_US");
    let base = base.replace('_', "-");
    // Garmin endpoints accept standard IETF tags (fr-FR, en-US, ...).
    if base.contains('-') {
        base
    } else {
        "en-US".into()
    }
}

fn build_client_info() -> proto::Client {
    // Matches Garmin.Cartography.Services.Interface.ProtoBufService.Dto.Common.Client (four strings).
    let locale_code = std::env::var("LANG")
        .ok()
        .and_then(|l| l.split('.').next().map(|s| s.to_string()))
        .unwrap_or_else(|| "en_US".into());

    let operating_system_type = match std::env::consts::OS {
        "windows" => "Windows",
        "macos" => "MacOSX",
        _ => "Linux",
    };

    let operating_system_version = std::process::Command::new("uname")
        .arg("-sr")
        .output()
        .ok()
        .and_then(|o| String::from_utf8(o.stdout).ok())
        .map(|s| s.trim().to_string())
        .filter(|s| !s.is_empty())
        .unwrap_or_else(|| {
            format!(
                "{} {}",
                std::env::consts::OS,
                std::env::consts::ARCH
            )
        });

    proto::Client {
        client_type: "GarminExpress".into(),
        locale_code,
        operating_system_type: operating_system_type.into(),
        operating_system_version,
    }
}

pub struct MapInstaller;

impl MapInstaller {
    pub async fn install_download_details_to_device(
        details: &proto::DownloadDetailsResponse,
        device_mount: &Path,
    ) -> Result<Vec<PathBuf>, AppError> {
        let garmin_dir = device_mount.join("GARMIN");
        tokio::fs::create_dir_all(&garmin_dir).await?;

        let host = details
            .download_hosts
            .as_ref()
            .map(|h| h.foreground_primary_host.as_str())
            .unwrap_or("https://worldwide.omtmapupdate.garmin.com");

        let client = Client::builder()
            .user_agent("Garmin Express/7.28.0")
            .build()?;

        let mut installed = Vec::new();

        // Flatten the content tree iteratively (avoid recursive async fn).
        let mut stack: Vec<&proto::DeliverableContent> = details.available_contents.iter().collect();
        while let Some(content) = stack.pop() {
            Self::download_urls(&client, host, &content.urls, &garmin_dir, &mut installed).await?;
            for extra in &content.extra_contents {
                stack.push(extra);
            }
        }

        Ok(installed)
    }

    async fn download_urls(
        http: &Client,
        host: &str,
        urls: &[proto::UrlDto],
        garmin_dir: &Path,
        installed: &mut Vec<PathBuf>,
    ) -> Result<(), AppError> {
        for u in urls {
            // We only need the binary payloads; manifests/images are absolute URLs and can be skipped.
            if !u.is_relative {
                continue;
            }

            let url = format!("{host}/{}", u.url.trim_start_matches('/'));
            let resp = http.get(&url).send().await?;
            if !resp.status().is_success() {
                return Err(AppError::Api {
                    status: resp.status().as_u16(),
                    message: format!("Failed to download {url}"),
                });
            }

            let bytes = resp.bytes().await?;
            let file_name = Path::new(&u.url)
                .file_name()
                .and_then(|n| n.to_str())
                .ok_or_else(|| AppError::Other(format!("Invalid URL filename: {}", u.url)))?;
            let dest = garmin_dir.join(file_name);
            tokio::fs::write(&dest, &bytes).await?;
            installed.push(dest);
        }

        Ok(())
    }
}

#[cfg(test)]
mod tests {
    use super::MapUpdateClient;
    use super::proto;
    use prost::Message;

    /// Fixed payload so Rust `prost` wire matches `tools/omt-probe` (Garmin DTO + protobuf-net).
    #[test]
    fn preloaded_map_updates_request_wire_matches_express_layout() {
        let req = proto::PreloadedMapUpdatesRequest {
            client_info: Some(proto::Client {
                client_type: "GarminExpress".into(),
                locale_code: "en_US".into(),
                operating_system_type: "Linux".into(),
                operating_system_version: "Linux test".into(),
            }),
            basic_unit_info: Some(proto::BasicUnitInfo {
                unit_id: 42,
                serial_number: "SN1".into(),
                first_fix: None,
            }),
            is_user_interactive: true,
        };
        let got = req.encode_to_vec();
        // Golden = `prost` encode of this struct; verified with `protoc --decode_raw` (client fields 1–4, basic 1–2, field 3 bool).
        const EXPECTED: &str = "0a290a0d4761726d696e457870726573731205656e5f55531a054c696e7578220a4c696e757820746573741207082a1203534e311801";
        assert_eq!(hex_lower(&got), EXPECTED, "wire snapshot drift");
    }

    fn hex_lower(bytes: &[u8]) -> String {
        bytes.iter().map(|b| format!("{:02x}", b)).collect()
    }

    #[tokio::test]
    async fn can_fetch_preloaded_map_updates_when_env_set() {
        if std::env::var("GARMIN_UP_NETWORK_TESTS").ok().as_deref() != Some("1") {
            eprintln!("Skipping network test (set GARMIN_UP_NETWORK_TESTS=1 to run)");
            return;
        }

        let unit_id = std::env::var("GARMIN_UP_TEST_UNIT_ID").unwrap_or_else(|_| "3429806241".into());
        let fw_part = std::env::var("GARMIN_UP_TEST_FW_PART").unwrap_or_else(|_| "006-B3290-00".into());

        let client = MapUpdateClient::new().expect("client");
        let serial = client
            .get_unit_serial_number(&unit_id, &fw_part)
            .await
            .expect("serial");

        let updates = client
            .get_preloaded_map_updates(&unit_id, &serial)
            .await
            .expect("GetPreloadedMapUpdates HTTP + protobuf decode");

        eprintln!(
            "GetPreloadedMapUpdates: map_updates={}, purchasable_products={}",
            updates.map_updates.len(),
            updates.purchasable_products.len()
        );

        if std::env::var("GARMIN_UP_EXPECT_MAP_UPDATES").ok().as_deref() == Some("1") {
            assert!(
                !updates.map_updates.is_empty(),
                "GARMIN_UP_EXPECT_MAP_UPDATES=1 but map_updates was empty"
            );
        }
    }
}

