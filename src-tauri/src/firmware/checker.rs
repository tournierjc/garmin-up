use std::time::Duration;

use reqwest::Client;
use serde::{Deserialize, Serialize};

use crate::error::AppError;

#[allow(dead_code)]
const UNIT_UPDATE_URL: &str = "https://omt.garmin.com/Rce/ProtobufApi/SoftwareUpdateService/GetAllUnitSoftwareUpdates";
const EXPRESS_UPDATE_URL: &str = "https://www.garmin.com/express/updateCheck";

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct FirmwareInfo {
    pub current_version: u32,
    pub latest_version: Option<u32>,
    pub latest_version_name: Option<String>,
    pub download_url: Option<String>,
    pub file_size: Option<u64>,
    pub update_available: bool,
    pub release_notes: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UpdateCheckRequest {
    pub part_number: String,
    pub current_version: u32,
    pub unit_id: String,
}

pub struct FirmwareChecker {
    http: Client,
}

impl FirmwareChecker {
    pub fn new() -> Result<Self, AppError> {
        let http = Client::builder()
            .user_agent("Garmin Express/7.28.0")
            // Garmin `www` front-ends are flaky over HTTP/2 with some rustls stacks; match Express-style HTTP/1.1.
            .http1_only()
            .connect_timeout(Duration::from_secs(30))
            .timeout(Duration::from_secs(120))
            .gzip(true)
            .build()?;
        Ok(Self { http })
    }

    pub async fn check_for_update(&self, request: &UpdateCheckRequest) -> Result<FirmwareInfo, AppError> {
        let xml_body = format!(
            r#"<?xml version="1.0" encoding="UTF-8"?>
<Requests xmlns="http://www.garmin.com/xmlschemas/PcSoftwareUpdate/v2">
  <Request>
    <PartNumber>{}</PartNumber>
    <Version>
      <VersionMajor>{}</VersionMajor>
      <VersionMinor>0</VersionMinor>
    </Version>
    <LanguageID>0</LanguageID>
  </Request>
</Requests>"#,
            request.part_number,
            request.current_version,
        );

        let lang = std::env::var("LANG")
            .ok()
            .and_then(|l| l.split('.').next().map(|s| s.replace('_', "-")))
            .filter(|s| s.contains('-'))
            .unwrap_or_else(|| "en-US".into());

        let resp = self
            .http
            .post(EXPRESS_UPDATE_URL)
            .header("Content-Type", "application/xml; charset=utf-8")
            .header(reqwest::header::ACCEPT, "application/xml, text/xml, */*;q=0.9")
            .header(reqwest::header::ACCEPT_LANGUAGE, &lang)
            .header(reqwest::header::REFERER, "https://www.garmin.com/express/")
            .header(reqwest::header::ORIGIN, "https://www.garmin.com")
            .body(xml_body)
            .send()
            .await?;

        if !resp.status().is_success() {
            return Ok(FirmwareInfo {
                current_version: request.current_version,
                latest_version: None,
                latest_version_name: None,
                download_url: None,
                file_size: None,
                update_available: false,
                release_notes: None,
            });
        }

        let body = resp.text().await?;
        parse_update_response(&body, request.current_version)
    }

    pub async fn download_firmware(&self, url: &str, dest: &std::path::Path) -> Result<u64, AppError> {
        let resp = self.http.get(url).send().await?;
        if !resp.status().is_success() {
            return Err(AppError::Api {
                status: resp.status().as_u16(),
                message: "Firmware download failed".into(),
            });
        }

        let bytes = resp.bytes().await?;
        let size = bytes.len() as u64;
        tokio::fs::write(dest, &bytes).await?;
        Ok(size)
    }
}

fn parse_update_response(xml: &str, current_version: u32) -> Result<FirmwareInfo, AppError> {
    let version = extract_xml_value(xml, "VersionMajor")
        .and_then(|v| v.parse::<u32>().ok());

    let download_url = extract_xml_value(xml, "Url");
    let file_size = extract_xml_value(xml, "FileSize")
        .and_then(|v| v.parse::<u64>().ok());
    let notes = extract_xml_value(xml, "Description");

    let update_available = version
        .map(|v| v > current_version)
        .unwrap_or(false);

    let version_name = version.map(|v| format!("{:.2}", v as f64 / 100.0));

    Ok(FirmwareInfo {
        current_version,
        latest_version: version,
        latest_version_name: version_name,
        download_url,
        file_size,
        update_available,
        release_notes: notes,
    })
}

fn extract_xml_value(xml: &str, tag: &str) -> Option<String> {
    let open = format!("<{}>", tag);
    let close = format!("</{}>", tag);
    let start = xml.find(&open)? + open.len();
    let end = xml[start..].find(&close)? + start;
    Some(xml[start..end].trim().to_string())
}
