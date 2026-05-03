use std::path::{Path, PathBuf};
use std::time::Duration;

use reqwest::header::HeaderMap;
use reqwest::Client;
use serde::{Deserialize, Serialize};
use std::collections::{HashMap, HashSet};
use prost::Message;
use tracing::warn;
use url::Url;

use crate::device::device_fs;
use crate::device::join_uri_leaf;
use crate::device::resolve_garmin_volume_dir;
use crate::error::AppError;

pub mod proto {
    // `prost` generates request/response types; JSON-first code paths only decode a subset.
    #![allow(dead_code)]
    include!(concat!(env!("OUT_DIR"), "/garmin.omt.mapupdate.rs"));
}

const UNIT_INFO_BASE: &str = "https://omt.garmin.com/api/unit-info/manufacturers/Taiwan";
const PRELOADED_MAP_UPDATES_URL: &str =
    "https://omt.garmin.com/Rce/ProtobufApi/MapUpdateService/GetPreloadedMapUpdates";
const DOWNLOAD_DETAILS_URL: &str =
    "https://omt.garmin.com/Rce/ProtobufApi/MapUpdateService/GetDownloadDetails";
const ACTIVATE_MAP_UPDATE_URL: &str =
    "https://omt.garmin.com/Rce/ProtobufApi/MapUpdateService/ActivateMapUpdate";

const MAP_OTM_DEFAULT_HOST: &str = "https://worldwide.omtmapupdate.garmin.com";
/// Express often resolves relative `rmu/...` against this host (see `DownloadHosts` vs worldwide).
const MAP_OTM_LEGACY_HOST: &str = "https://omtmapupdate.garmin.com";

/// Garmin Express `OmtRestClient` headers — attach **only** to `omt.garmin.com` API calls, not map CDN GETs
/// (some CDNs return 403 if `Garmin-Client-*` is present on `omtmapupdate.garmin.com`).
fn garmin_omt_api_headers(session_id: &str) -> HeaderMap {
    let locale = detect_accept_language();
    let platform_version = std::process::Command::new("uname")
        .arg("-r")
        .output()
        .ok()
        .and_then(|o| String::from_utf8(o.stdout).ok())
        .map(|s| s.trim().to_string())
        .filter(|s| !s.is_empty())
        .unwrap_or_else(|| "0".to_string());

    let mut h = HeaderMap::new();
    h.insert(
        "Garmin-Client-Name",
        reqwest::header::HeaderValue::from_static("express"),
    );
    h.insert(
        "Garmin-Client-Version",
        reqwest::header::HeaderValue::from_static("7.28.0"),
    );
    h.insert(
        "Garmin-Client-Platform",
        reqwest::header::HeaderValue::from_static("Linux"),
    );
    h.insert(
        "Garmin-Client-Platform-Version",
        reqwest::header::HeaderValue::from_str(&platform_version)
            .unwrap_or_else(|_| reqwest::header::HeaderValue::from_static("0")),
    );
    h.insert(
        "Garmin-Client-LocaleCode",
        reqwest::header::HeaderValue::from_str(&locale)
            .unwrap_or_else(|_| reqwest::header::HeaderValue::from_static("en-US")),
    );
    h.insert(
        "Garmin-Client-SessionId",
        reqwest::header::HeaderValue::from_str(session_id).unwrap_or_else(|_| {
            reqwest::header::HeaderValue::from_static("00000000-0000-0000-0000-000000000000")
        }),
    );
    h.insert(
        reqwest::header::ACCEPT_LANGUAGE,
        reqwest::header::HeaderValue::from_str(&locale)
            .unwrap_or_else(|_| reqwest::header::HeaderValue::from_static("en-US")),
    );
    h
}

fn normalize_map_cdn_origin(s: &str) -> String {
    let s = s.trim();
    if s.starts_with("http://") || s.starts_with("https://") {
        s.trim_end_matches('/').to_string()
    } else {
        format!(
            "https://{}",
            s.trim_start_matches('/').trim_end_matches('/')
        )
    }
}

/// Origins to try when rewriting absolute `omtmapupdate` URLs (403 / geo failover).
fn map_cdn_origin_candidates(hosts: Option<&JsonDownloadHosts>) -> Vec<String> {
    let mut v = Vec::new();
    v.push(MAP_OTM_LEGACY_HOST.to_string());
    if let Some(h) = hosts {
        let t = h.foreground_primary_host.trim();
        if !t.is_empty() {
            v.push(normalize_map_cdn_origin(t));
        }
        for f in &h.failover_hosts {
            let t = f.trim();
            if !t.is_empty() {
                v.push(normalize_map_cdn_origin(t));
            }
        }
    }
    v.push(MAP_OTM_DEFAULT_HOST.to_string());
    let mut seen = HashSet::new();
    v.into_iter().filter(|s| seen.insert(s.clone())).collect()
}

/// Absolute map URLs from `GetDownloadDetails` often point at `worldwide.omtmapupdate…` first.
/// Try alternate CDN origins **before** the original URL so regional / legacy hosts are hit first.
fn map_absolute_download_candidates(original: &str, cdn_origins: &[String]) -> Vec<String> {
    let mut seen = HashSet::<String>::new();
    let mut out = Vec::new();
    for origin in cdn_origins {
        if let Some(rew) = rewrite_map_download_url(original, origin) {
            if seen.insert(rew.clone()) {
                out.push(rew);
            }
        }
    }
    if seen.insert(original.to_string()) {
        out.push(original.to_string());
    }
    out
}

fn rewrite_map_download_url(original: &str, new_origin_base: &str) -> Option<String> {
    let o = Url::parse(original).ok()?;
    let pq = o
        .query()
        .map(|q| format!("?{}", q))
        .unwrap_or_default();
    Some(format!(
        "{}{}{}",
        new_origin_base.trim_end_matches('/'),
        o.path(),
        pq
    ))
}

#[derive(Debug, Clone, Deserialize)]
struct UnitInfoResponse {
    // Observed on the live endpoint as `serialNumber` but Express models it as `SerialNumber`.
    #[serde(rename = "serialNumber", alias = "SerialNumber")]
    serial_number: String,

    // Express uses this to decide whether to include full DeviceXml in map update request.
    #[serde(rename = "ApplicationBehaviors", default)]
    application_behaviors: Option<HashMap<String, String>>,
}

#[derive(Debug, Clone, Serialize)]
pub struct JsonClientInfo {
    #[serde(rename = "LocaleCode")]
    pub locale_code: String,
}

#[derive(Debug, Clone, Serialize)]
pub struct JsonBasicUnitInfo {
    #[serde(rename = "UnitId")]
    pub unit_id: i64,
    #[serde(rename = "FirstFix", skip_serializing_if = "Option::is_none")]
    pub first_fix: Option<i64>,
    #[serde(rename = "SerialNumber", skip_serializing_if = "Option::is_none")]
    pub serial_number: Option<String>,
}

#[derive(Debug, Clone, Serialize)]
pub struct JsonPreloadedMapUpdatesRequest {
    #[serde(rename = "ClientInfo")]
    pub client_info: JsonClientInfo,
    #[serde(rename = "BasicUnitInfo")]
    pub basic_unit_info: JsonBasicUnitInfo,
    #[serde(rename = "IsUserInteractive")]
    pub is_user_interactive: bool,
}

#[derive(Debug, Clone, Serialize)]
pub struct JsonPreloadedMapWithDeviceXmlUpdatesRequest {
    #[serde(rename = "ClientInfo")]
    pub client_info: JsonClientInfo,
    #[serde(rename = "BasicUnitInfo")]
    pub basic_unit_info: JsonBasicUnitInfo,
    #[serde(rename = "IsUserInteractive")]
    pub is_user_interactive: bool,
    #[serde(rename = "DeviceXml")]
    pub device_xml: String,
}

#[derive(Debug, Clone, Deserialize)]
pub struct JsonAutoCheckSettings {
    #[serde(rename = "IsAutoCheckEnabled")]
    pub is_auto_check_enabled: bool,
}

// Shapes mirror Garmin Express JSON; only a subset is used after deserialize.
#[allow(dead_code)]
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct JsonMapUpdateInfo {
    #[serde(rename = "ProductKey", default, skip_serializing_if = "Option::is_none")]
    pub product_key: Option<String>,
    #[serde(rename = "IsReinstall")]
    pub is_reinstall: bool,
    #[serde(rename = "PartNumber", default, skip_serializing_if = "Option::is_none")]
    pub part_number: Option<String>,
    #[serde(rename = "UpdateType")]
    pub update_type: i32,
    #[serde(rename = "MajorVersion")]
    pub major_version: i32,
    #[serde(rename = "MinorVersion")]
    pub minor_version: i32,
    #[serde(rename = "ProductGroup", default, skip_serializing_if = "Option::is_none")]
    pub product_group: Option<String>,
    #[serde(rename = "DisplayName", default, skip_serializing_if = "Option::is_none")]
    pub display_name: Option<String>,
    #[serde(rename = "EulaUrl", default, skip_serializing_if = "Option::is_none")]
    pub eula_url: Option<String>,
    #[serde(rename = "CanAutoStartDownload")]
    pub can_auto_start_download: bool,
    #[serde(rename = "ReleaseNotes", default, skip_serializing_if = "Option::is_none")]
    pub release_notes: Option<String>,
}

fn normalize_map_part_number(s: &str) -> String {
    s.chars()
        .filter(|c| !c.is_whitespace())
        .collect::<String>()
        .to_ascii_lowercase()
}

/// Match a map update from `GetPreloadedMapUpdates` to the part number the UI selected.
pub fn find_map_update_by_part_number<'a>(
    updates: &'a [JsonMapUpdateInfo],
    part_number: &str,
) -> Option<&'a JsonMapUpdateInfo> {
    let want = normalize_map_part_number(part_number);
    updates.iter().find(|u| {
        u.part_number
            .as_ref()
            .map(|pn| normalize_map_part_number(pn) == want)
            .unwrap_or(false)
    })
}

#[allow(dead_code)]
#[derive(Debug, Clone, Deserialize)]
pub struct JsonPreloadedMapUpdatesResponse {
    #[serde(rename = "AutoCheckSettings", default)]
    pub auto_check_settings: Option<JsonAutoCheckSettings>,
    #[serde(rename = "MapUpdates", default)]
    pub map_updates: Option<Vec<JsonMapUpdateInfo>>,
    #[serde(rename = "PurchasableProducts", default)]
    pub purchasable_products: Option<Vec<serde_json::Value>>,
}

#[derive(Debug, Clone, Serialize)]
pub struct JsonUnitUpdateFile {
    #[serde(rename = "FileName")]
    pub file_name: String,
    #[serde(rename = "MajorVersion")]
    pub major_version: i32,
    #[serde(rename = "MinorVersion")]
    pub minor_version: i32,
    #[serde(rename = "PartNumber")]
    pub part_number: String,
    #[serde(rename = "Path")]
    pub path: String,
}

#[derive(Debug, Clone, Serialize)]
pub struct JsonUnitDataTypeLocation {
    #[serde(rename = "BaseName")]
    pub base_name: String,
    #[serde(rename = "Extension")]
    pub extension: String,
    #[serde(rename = "Path")]
    pub path: String,
}

#[derive(Debug, Clone, Serialize)]
pub struct JsonUnitDataType {
    #[serde(rename = "Name")]
    pub name: String,
    #[serde(rename = "Locations")]
    pub locations: Vec<JsonUnitDataTypeLocation>,
}

#[derive(Debug, Clone, Serialize)]
pub struct JsonFullUnitInfo {
    #[serde(rename = "UnitId")]
    pub unit_id: i64,
    #[serde(rename = "FirstFix", skip_serializing_if = "Option::is_none")]
    pub first_fix: Option<i64>,
    #[serde(rename = "SerialNumber", skip_serializing_if = "Option::is_none")]
    pub serial_number: Option<String>,
    #[serde(rename = "SoftwarePartNumber")]
    pub software_part_number: String,
    #[serde(rename = "SoftwareVersion")]
    pub software_version: String,
    #[serde(rename = "UpdateFiles")]
    pub update_files: Vec<JsonUnitUpdateFile>,
    #[serde(rename = "DataTypes")]
    pub data_types: Vec<JsonUnitDataType>,
}

#[derive(Debug, Clone, Serialize)]
pub struct JsonDownloadDetailsRequest {
    #[serde(rename = "ClientInfo")]
    pub client_info: JsonClientInfo,
    #[serde(rename = "FullUnitInfo")]
    pub full_unit_info: JsonFullUnitInfo,
    #[serde(rename = "PartNumber")]
    pub part_number: String,
}

#[derive(Debug, Clone, Serialize)]
pub struct JsonActivateMapUpdateRequest {
    #[serde(rename = "ClientInfo")]
    pub client_info: JsonClientInfo,
    #[serde(rename = "FullUnitInfo")]
    pub full_unit_info: JsonFullUnitInfo,
    #[serde(rename = "UpdateInfo")]
    pub update_info: JsonMapUpdateInfo,
    #[serde(rename = "PartNumbersToInstall")]
    pub part_numbers_to_install: Vec<String>,
}

#[allow(dead_code)]
#[derive(Debug, Clone, Deserialize)]
pub struct JsonDownloadHosts {
    #[serde(rename = "ForegroundPrimaryHost", default)]
    pub foreground_primary_host: String,
    #[serde(rename = "BackgroundPrimaryHost")]
    pub background_primary_host: String,
    #[serde(rename = "FailoverHosts", default)]
    pub failover_hosts: Vec<String>,
}

#[allow(dead_code)]
#[derive(Debug, Clone, Deserialize)]
pub struct JsonUrlDto {
    #[serde(rename = "Type", default)]
    pub r#type: i32,
    #[serde(rename = "Url", default)]
    pub url: String,
    #[serde(rename = "Md5", default)]
    pub md5: Option<String>,
    #[serde(rename = "Size", default)]
    pub size: Option<i64>,
    #[serde(rename = "IsRelative", default)]
    pub is_relative: bool,
}

#[allow(dead_code)]
#[derive(Debug, Clone, Deserialize)]
pub struct JsonFileToRemove {
    #[serde(rename = "Identifier", default)]
    pub identifier: String,
    #[serde(rename = "IsFileName", default)]
    pub is_file_name: bool,
    #[serde(rename = "SizeInBytes", default)]
    pub size_in_bytes: i64,
}

#[allow(dead_code)]
#[derive(Debug, Clone, Deserialize)]
pub struct JsonDeliverableContent {
    #[serde(rename = "AdditionalContent", default)]
    pub additional_content: Vec<JsonDeliverableContent>,
    #[serde(rename = "ContentToReplace", default)]
    pub content_to_replace: Option<JsonFileToRemove>,
    #[serde(rename = "ContentType", default)]
    pub content_type: Option<String>,
    #[serde(rename = "DisplayName", default)]
    pub display_name: Option<String>,
    #[serde(rename = "ExtraContents", default)]
    pub extra_contents: Vec<JsonDeliverableContent>,
    #[serde(rename = "ExtraContentsToReplace", default)]
    pub extra_contents_to_replace: Vec<JsonFileToRemove>,
    #[serde(rename = "Gma", default)]
    pub gma: Option<Vec<u8>>,
    /// Server often omits this on nested `AdditionalContent` / `ExtraContents` nodes.
    #[serde(rename = "Id", default)]
    pub id: i32,
    #[serde(rename = "IsPackage", default)]
    pub is_package: bool,
    #[serde(rename = "IsRecommended", default)]
    pub is_recommended: bool,
    #[serde(rename = "IsReinstallOnly", default)]
    pub is_reinstall_only: bool,
    #[serde(rename = "Locale", default)]
    pub locale: Option<String>,
    #[serde(rename = "PartNumber", default)]
    pub part_number: Option<String>,
    #[serde(rename = "PartNumberToReplace", default)]
    pub part_number_to_replace: Option<String>,
    #[serde(rename = "SmallerContentOptions", default)]
    pub smaller_content_options: Vec<JsonDeliverableContent>,
    #[serde(rename = "UnlockCode", default)]
    pub unlock_code: Option<String>,
    #[serde(rename = "Urls", default)]
    pub urls: Vec<JsonUrlDto>,
}

#[allow(dead_code)]
#[derive(Debug, Clone, Deserialize)]
pub struct JsonDownloadedDetailsResponse {
    #[serde(rename = "FilesToRemove", default)]
    pub files_to_remove: Vec<JsonFileToRemove>,
    #[serde(rename = "AvailableContents", default)]
    pub available_contents: Vec<JsonDeliverableContent>,
    #[serde(rename = "ComputerInstallUrl", default)]
    pub computer_install_url: Option<String>,
    #[serde(rename = "DownloadHosts", default)]
    pub download_hosts: Option<JsonDownloadHosts>,
}

pub struct MapUpdateClient {
    http: Client,
    session_id: String,
}

impl MapUpdateClient {
    pub fn new() -> Result<Self, AppError> {
        // Cookie jar is shared with map CDN GETs; `Garmin-Client-*` is added per OMT API request only
        // (see `garmin_omt_api_headers`) so `omtmapupdate` WAFs do not see Express-only headers.
        let session_id = uuid::Uuid::new_v4().to_string();
        let http = Client::builder()
            .cookie_store(true)
            .user_agent("Garmin Express/7.28.0")
            // Match `FirmwareChecker`: some Garmin / Cloudflare edges misbehave on HTTP/2.
            .http1_only()
            .connect_timeout(Duration::from_secs(30))
            .timeout(Duration::from_secs(900))
            .build()?;
        Ok(Self { http, session_id })
    }

    /// OMT API headers (also used as a **second-pass** fallback on map CDN 403).
    pub fn omt_headers(&self) -> HeaderMap {
        garmin_omt_api_headers(&self.session_id)
    }

    /// Same HTTP client used for OMT JSON APIs and map CDN downloads so `Set-Cookie` from OMT applies.
    /// Map CDN GETs intentionally omit `Garmin-Client-*` (see `download_urls_json`).
    pub fn http(&self) -> &Client {
        &self.http
    }

    #[cfg(test)]
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
            .headers(self.omt_headers())
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

    async fn get_unit_display_info(
        &self,
        unit_id: &str,
        firmware_part_number: &str,
    ) -> Result<UnitInfoResponse, AppError> {
        let url = format!(
            "{UNIT_INFO_BASE}/unitids/{unit_id}?primaryFirmwarePartNumber={firmware_part_number}"
        );

        let resp = self
            .http
            .get(url)
            .headers(self.omt_headers())
            .send()
            .await?;

        if !resp.status().is_success() {
            return Err(AppError::Api {
                status: resp.status().as_u16(),
                message: "Failed to fetch unit-info".into(),
            });
        }

        Ok(resp.json().await?)
    }

    #[cfg(test)]
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
            .headers(self.omt_headers())
            .header("Content-Type", "application/x-protobuf")
            .header("Accept", "application/x-protobuf")
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

    pub async fn get_preloaded_map_updates_json(
        &self,
        unit_id: &str,
        firmware_part_number: &str,
        device_xml: Option<String>,
    ) -> Result<(String, JsonPreloadedMapUpdatesResponse), AppError> {
        let display = self.get_unit_display_info(unit_id, firmware_part_number).await?;
        let include_full_xml = display
            .application_behaviors
            .as_ref()
            .and_then(|m| m.get("GetPreloadedMapUpdatesIncludeFullXML"))
            .map(|v| v.eq_ignore_ascii_case("true"))
            .unwrap_or(false);

        let client_info = JsonClientInfo {
            locale_code: detect_accept_language(),
        };

        let basic = JsonBasicUnitInfo {
            unit_id: parse_unit_id(unit_id),
            first_fix: None,
            serial_number: None,
        };

        let req_body = if include_full_xml {
            let xml = device_xml.unwrap_or_default();
            serde_json::to_value(JsonPreloadedMapWithDeviceXmlUpdatesRequest {
                client_info,
                basic_unit_info: basic,
                is_user_interactive: true,
                device_xml: xml,
            })?
        } else {
            serde_json::to_value(JsonPreloadedMapUpdatesRequest {
                client_info,
                basic_unit_info: basic,
                is_user_interactive: true,
            })?
        };

        let resp = self
            .http
            .post(PRELOADED_MAP_UPDATES_URL)
            .headers(self.omt_headers())
            .header(reqwest::header::ACCEPT, "application/json")
            .json(&req_body)
            .send()
            .await?;

        if !resp.status().is_success() {
            return Err(AppError::Api {
                status: resp.status().as_u16(),
                message: resp.text().await.unwrap_or_default(),
            });
        }

        let body = resp.bytes().await?;
        // Prefer JSON (what Express expects), but fall back to protobuf decode if server responds that way.
        let parsed: JsonPreloadedMapUpdatesResponse = match serde_json::from_slice(&body) {
            Ok(v) => v,
            Err(json_err) => {
                if let Ok(pb) = proto::PreloadedMapUpdatesResponse::decode(body.as_ref()) {
                    JsonPreloadedMapUpdatesResponse {
                        auto_check_settings: pb
                            .auto_check_settings
                            .map(|s| JsonAutoCheckSettings {
                                is_auto_check_enabled: s.is_auto_check_enabled,
                            }),
                        map_updates: Some(
                            pb.map_updates
                                .into_iter()
                                .map(|u| JsonMapUpdateInfo {
                                    product_key: Some(u.product_key),
                                    is_reinstall: u.is_reinstall,
                                    part_number: Some(u.part_number),
                                    update_type: u.update_type,
                                    major_version: u.major_version,
                                    minor_version: u.minor_version,
                                    product_group: Some(u.product_group),
                                    display_name: Some(u.display_name),
                                    eula_url: Some(u.eula_url),
                                    can_auto_start_download: u.can_auto_start_download,
                                    release_notes: Some(u.release_notes),
                                })
                                .collect(),
                        ),
                        purchasable_products: Some(
                            pb.purchasable_products
                                .into_iter()
                                .map(serde_json::Value::String)
                                .collect(),
                        ),
                    }
                } else {
                    let snippet = String::from_utf8_lossy(&body);
                    return Err(AppError::Api {
                        status: 200,
                        message: format!(
                            "error decoding response body (not JSON/protobuf): {json_err}; body starts: {}",
                            snippet.chars().take(400).collect::<String>()
                        ),
                    });
                }
            }
        };
        Ok((display.serial_number, parsed))
    }

    pub async fn get_download_details_json(
        &self,
        full_unit_info: JsonFullUnitInfo,
        part_number: &str,
    ) -> Result<JsonDownloadedDetailsResponse, AppError> {
        let req_body = JsonDownloadDetailsRequest {
            client_info: JsonClientInfo {
                locale_code: detect_accept_language(),
            },
            full_unit_info,
            part_number: part_number.to_string(),
        };

        let resp = self
            .http
            .post(DOWNLOAD_DETAILS_URL)
            .headers(self.omt_headers())
            .header(reqwest::header::ACCEPT, "application/json")
            .json(&req_body)
            .send()
            .await?;

        if !resp.status().is_success() {
            return Err(AppError::Api {
                status: resp.status().as_u16(),
                message: resp.text().await.unwrap_or_default(),
            });
        }

        let body = resp.bytes().await?;
        serde_json::from_slice(&body).map_err(AppError::Json)
    }

    /// Garmin Express calls this before pulling `.img` blobs from `omtmapupdate` CDNs (403 otherwise).
    pub async fn activate_map_update_json(
        &self,
        full_unit_info: JsonFullUnitInfo,
        update_info: &JsonMapUpdateInfo,
        part_numbers_to_install: Vec<String>,
    ) -> Result<(), AppError> {
        let req_body = JsonActivateMapUpdateRequest {
            client_info: JsonClientInfo {
                locale_code: detect_accept_language(),
            },
            full_unit_info,
            update_info: update_info.clone(),
            part_numbers_to_install,
        };

        let resp = self
            .http
            .post(ACTIVATE_MAP_UPDATE_URL)
            .headers(self.omt_headers())
            .header(reqwest::header::ACCEPT, "application/json")
            .json(&req_body)
            .send()
            .await?;

        let status = resp.status();
        let message = resp.text().await.unwrap_or_default();
        if !status.is_success() {
            return Err(AppError::Api {
                status: status.as_u16(),
                message,
            });
        }
        tracing::trace!(body = %message, "ActivateMapUpdate");
        Ok(())
    }
}

#[cfg(test)]
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
        .unwrap_or_else(|| format!("{} {}", std::env::consts::OS, std::env::consts::ARCH));

    proto::Client {
        client_type: "GarminExpress".into(),
        locale_code,
        operating_system_type: operating_system_type.into(),
        operating_system_version,
    }
}

fn parse_unit_id(unit_id: &str) -> i64 {
    unit_id.parse::<i64>().unwrap_or(0)
}

/// GET a map payload from `omtmapupdate` / `worldwide.omtmapupdate` CDN.
///
/// Uses the shared client default `User-Agent` (`Garmin Express/7.28.0`) so OMT API calls and
/// CDN GETs present a consistent client to Cloudflare. Optional `omt_auth` adds Garmin session headers.
async fn map_cdn_get(
    http: &Client,
    url: &str,
    omt_auth: Option<&HeaderMap>,
    cookie: Option<&str>,
) -> Result<Vec<u8>, AppError> {
    const REFERER: &str = "https://omt.garmin.com/";
    const ORIGIN: &str = "https://omt.garmin.com";
    let mut req = http.get(url);
    if let Some(h) = omt_auth {
        req = req.headers(h.clone());
    }
    if let Some(c) = cookie {
        let c = c.trim();
        if !c.is_empty() {
            if let Ok(val) = reqwest::header::HeaderValue::from_str(c) {
                req = req.header(reqwest::header::COOKIE, val);
            }
        }
    }
    let resp = req
        .header(reqwest::header::ACCEPT, "*/*")
        .header(reqwest::header::REFERER, REFERER)
        .header(reqwest::header::ORIGIN, ORIGIN)
        .send()
        .await
        .map_err(|e| AppError::Other(format!("GET {url}: {e}")))?;
    let status = resp.status();
    let body = resp
        .bytes()
        .await
        .map_err(|e| AppError::Other(format!("read body {url}: {e}")))?;
    if !status.is_success() {
        let preview = String::from_utf8_lossy(&body[..body.len().min(256)]);
        warn!(%status, %url, preview = %preview, "map CDN GET failed");
        return Err(AppError::Api {
            status: status.as_u16(),
            message: format!("Failed to download {url}"),
        });
    }
    Ok(body.to_vec())
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

pub struct MapInstaller;

impl MapInstaller {
    /// `http`: use [`MapUpdateClient::http`] from the client that fetched `details` so cookies match.
    /// `omt_auth_for_map_cdn`: if set, after plain CDN GETs fail we retry with [`MapUpdateClient::omt_headers`]
    /// (some edges require session headers + cookies together).
    pub async fn install_download_details_json_to_device(
        http: &Client,
        omt_auth_for_map_cdn: Option<HeaderMap>,
        details: &JsonDownloadedDetailsResponse,
        device_mount: &Path,
    ) -> Result<Vec<PathBuf>, AppError> {
        let garmin_dir = resolve_garmin_volume_dir(device_mount).ok_or_else(|| {
            AppError::Other(format!(
                "Device mount has neither GARMIN nor Garmin folder: {}",
                device_mount.display()
            ))
        })?;

        // Safety: quarantine removals before writing new payloads.
        // Garmin Express uses FilesToRemove / *ToReplace hints; we only act on explicit filenames and
        // only within GARMIN/ to avoid destructive behavior.
        Self::quarantine_files_to_remove(details, &garmin_dir).await?;

        let host = details
            .download_hosts
            .as_ref()
            .and_then(|h| {
                let t = h.foreground_primary_host.trim();
                (!t.is_empty()).then_some(t)
            })
            .unwrap_or(MAP_OTM_DEFAULT_HOST);

        let cdn_origins = map_cdn_origin_candidates(details.download_hosts.as_ref());

        let mut installed = Vec::new();
        let mut stack: Vec<&JsonDeliverableContent> = details.available_contents.iter().collect();
        while let Some(content) = stack.pop() {
            Self::download_urls_json(
                http,
                host,
                &cdn_origins,
                omt_auth_for_map_cdn.as_ref(),
                &content.urls,
                &garmin_dir,
                &mut installed,
            )
            .await?;
            for c in &content.additional_content {
                stack.push(c);
            }
            for c in &content.extra_contents {
                stack.push(c);
            }
            for c in &content.smaller_content_options {
                stack.push(c);
            }
        }

        Ok(installed)
    }

    async fn quarantine_files_to_remove(
        details: &JsonDownloadedDetailsResponse,
        garmin_dir: &Path,
    ) -> Result<(), AppError> {
        let mut removals: Vec<&JsonFileToRemove> = Vec::new();
        removals.extend(details.files_to_remove.iter());

        // Some payloads include per-content replacement hints.
        let mut stack: Vec<&JsonDeliverableContent> = details.available_contents.iter().collect();
        while let Some(content) = stack.pop() {
            if let Some(f) = content.content_to_replace.as_ref() {
                removals.push(f);
            }
            removals.extend(content.extra_contents_to_replace.iter());

            for c in &content.additional_content {
                stack.push(c);
            }
            for c in &content.extra_contents {
                stack.push(c);
            }
            for c in &content.smaller_content_options {
                stack.push(c);
            }
        }

        if removals.is_empty() {
            return Ok(());
        }

        let ts = std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap_or_default()
            .as_secs();
        // MTP: `kioclient mkdir` on nested `.garmin-up-trash/<ts>/` often fails; use flat names in GARMIN/.
        let flat_mtp_quarantine = device_fs::is_kio_uri(garmin_dir);
        let quarantine_dir = if flat_mtp_quarantine {
            garmin_dir.to_path_buf()
        } else {
            let trash_root = join_uri_leaf(garmin_dir, ".garmin-up-trash");
            device_fs::create_dir(&trash_root).await?;
            let nested = join_uri_leaf(&trash_root, &ts.to_string());
            device_fs::create_dir(&nested).await?;
            nested
        };

        for r in removals {
            if !r.is_file_name {
                continue;
            }
            let ident = r.identifier.trim();
            if ident.is_empty() {
                continue;
            }

            // Reject any identifier that looks like a path. We only accept plain filenames.
            if ident.contains('/') || ident.contains('\\') || ident.contains("..") {
                warn!("Refusing to remove suspicious identifier: {ident}");
                continue;
            }

            let src = device_fs::join_uri_leaf(garmin_dir, ident);
            let dst = if flat_mtp_quarantine {
                device_fs::join_uri_leaf(garmin_dir, &format!(".garmin-up-trash.{ts}.{ident}"))
            } else {
                device_fs::join_uri_leaf(&quarantine_dir, ident)
            };

            if let Err(err) = device_fs::rename_move(&src, &dst).await {
                warn!("rename/move failed for {} -> {}: {err}", src.display(), dst.display());
                let Ok(bytes) = device_fs::read_bytes(&src).await else {
                    continue;
                };
                if device_fs::write_bytes(&dst, &bytes).await.is_ok() {
                    let _ = device_fs::remove_file(&src).await;
                }
            }
        }

        Ok(())
    }

    async fn download_urls_json(
        http: &Client,
        host: &str,
        cdn_origins: &[String],
        omt_auth_for_map_cdn: Option<&HeaderMap>,
        urls: &[JsonUrlDto],
        garmin_dir: &Path,
        installed: &mut Vec<PathBuf>,
    ) -> Result<(), AppError> {
        for u in urls {
            let mut seen = HashSet::<String>::new();
            let mut candidates: Vec<String> = Vec::new();
            let mut push = |s: String| {
                if seen.insert(s.clone()) {
                    candidates.push(s);
                }
            };

            if u.url.starts_with("https://") || u.url.starts_with("http://") {
                for s in map_absolute_download_candidates(&u.url, cdn_origins) {
                    push(s);
                }
            } else if !u.is_relative {
                continue;
            } else {
                let rel = u.url.trim_start_matches('/');
                push(format!(
                    "{}/{}",
                    host.trim_end_matches('/'),
                    rel
                ));
                for origin in cdn_origins {
                    push(format!(
                        "{}/{}",
                        origin.trim_end_matches('/'),
                        rel
                    ));
                }
            }

            let mut last_err: Option<AppError> = None;
            let mut body: Option<Vec<u8>> = None;

            // Try session headers + cookies first (Express often has both on the wire), then plain GET.
            let mut auth_rounds: Vec<Option<&HeaderMap>> = Vec::new();
            if let Some(h) = omt_auth_for_map_cdn {
                auth_rounds.push(Some(h));
            }
            auth_rounds.push(None);

            'fetched: for auth in auth_rounds {
                for url in &candidates {
                    match map_cdn_get(http, url, auth, None).await {
                        Ok(b) => {
                            body = Some(b);
                            break 'fetched;
                        }
                        Err(e) => last_err = Some(e),
                    }
                }
            }

            let bytes = body.ok_or_else(|| {
                last_err.unwrap_or_else(|| AppError::Other("map download: no candidates succeeded".into()))
            })?;

            let file_name = Path::new(&u.url)
                .file_name()
                .and_then(|n| n.to_str())
                .ok_or_else(|| AppError::Other(format!("Invalid URL filename: {}", u.url)))?;
            let dest = device_fs::join_uri_leaf(garmin_dir, file_name);
            device_fs::write_bytes(&dest, &bytes).await?;
            installed.push(dest);
        }
        Ok(())
    }
}

#[cfg(test)]
mod tests {
    use super::find_map_update_by_part_number;
    use super::map_cdn_get;
    use super::MapUpdateClient;
    use super::JsonMapUpdateInfo;
    use super::proto;
    use prost::Message;

    #[test]
    fn find_map_update_matches_part_case_and_whitespace() {
        let u = JsonMapUpdateInfo {
            product_key: None,
            is_reinstall: false,
            part_number: Some("006-D9486-07".into()),
            update_type: 0,
            major_version: 1,
            minor_version: 0,
            product_group: None,
            display_name: None,
            eula_url: None,
            can_auto_start_download: false,
            release_notes: None,
        };
        assert!(find_map_update_by_part_number(&[u.clone()], "006-d9486-07 ").is_some());
        assert!(find_map_update_by_part_number(&[u], "00 6-D948 6-07").is_some());
    }

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

    #[test]
    fn map_cdn_origin_candidates_legacy_host_first_when_no_download_hosts() {
        let v = super::map_cdn_origin_candidates(None);
        assert_eq!(v.first().map(String::as_str), Some(super::MAP_OTM_LEGACY_HOST));
    }

    #[test]
    fn map_absolute_download_candidates_try_rewrites_before_original_url() {
        let origins = vec![
            "https://omtmapupdate.garmin.com".into(),
            "https://worldwide.omtmapupdate.garmin.com".into(),
        ];
        let abs = "https://worldwide.omtmapupdate.garmin.com/rmu/activeeu/z.img";
        let v = super::map_absolute_download_candidates(abs, &origins);
        assert_eq!(v.last().map(String::as_str), Some(abs));
        assert_eq!(
            v.first().map(String::as_str),
            Some("https://omtmapupdate.garmin.com/rmu/activeeu/z.img")
        );
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

    /// Live map CDN GET (403 without a **fresh** signed URL and often Cloudflare cookies).
    ///
    /// Capture a URL from Windows Garmin Express (`ExpressDetailed_*.log` → `GetDownloadDetails`
    /// `Url` / `IsRelative` lines) immediately before running; URLs expire quickly.
    ///
    /// ```text
    /// export GARMIN_UP_MAP_E2E=1
    /// export GARMIN_UP_MAP_E2E_URL='https://worldwide.omtmapupdate.garmin.com/rmu/.../file.img'
    /// # optional: Cookie from DevTools while logged into Garmin / after hitting omt.garmin.com
    /// export GARMIN_UP_MAP_E2E_COOKIE='__cf_bm=...'
    /// cargo test -p garmin-up live_map_cdn_download_e2e -- --ignored
    /// ```
    ///
    /// Ignored by default so `cargo test` stays green in CI without secrets.
    #[tokio::test]
    #[ignore = "network + fresh URL from Garmin Express; see module rustdoc"]
    async fn live_map_cdn_download_e2e() {
        assert_eq!(
            std::env::var("GARMIN_UP_MAP_E2E").ok().as_deref(),
            Some("1"),
            "set GARMIN_UP_MAP_E2E=1 before running with cargo test -- --ignored"
        );
        let url = std::env::var("GARMIN_UP_MAP_E2E_URL").expect("GARMIN_UP_MAP_E2E_URL");
        assert!(!url.trim().is_empty(), "GARMIN_UP_MAP_E2E_URL is empty");
        let min_bytes: usize = std::env::var("GARMIN_UP_MAP_E2E_MIN_BYTES")
            .ok()
            .and_then(|s| s.parse().ok())
            .unwrap_or(1024);

        let client = MapUpdateClient::new().expect("MapUpdateClient::new");
        let cookie = std::env::var("GARMIN_UP_MAP_E2E_COOKIE").ok();

        let bytes = map_cdn_get(client.http(), &url, None, cookie.as_deref())
            .await
            .unwrap_or_else(|e| panic!("map_cdn_get: {e}"));

        assert!(
            bytes.len() >= min_bytes,
            "expected at least {min_bytes} bytes, got {}",
            bytes.len()
        );
    }
}

