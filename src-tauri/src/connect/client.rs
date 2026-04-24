use std::path::Path;
use reqwest::{Client, multipart};

use crate::error::AppError;
use super::types::{DeviceInfo, ActivityUploadResult, OAuth2Token};

const BASE_URL: &str = "https://connectapi.garmin.com";
const USER_AGENT_API: &str = "GCM-Android-5.23";

pub struct GarminClient {
    http: Client,
}

impl GarminClient {
    pub fn new() -> Result<Self, AppError> {
        let http = Client::builder()
            .user_agent(USER_AGENT_API)
            .build()?;
        Ok(Self { http })
    }

    pub async fn upload_activity(
        &self,
        token: &OAuth2Token,
        file_path: &Path,
    ) -> Result<ActivityUploadResult, AppError> {
        let file_bytes = tokio::fs::read(file_path).await?;
        let file_name = file_path.file_name()
            .and_then(|n| n.to_str())
            .unwrap_or("activity.fit")
            .to_string();

        let part = multipart::Part::bytes(file_bytes)
            .file_name(file_name)
            .mime_str("application/octet-stream")
            .map_err(|e| AppError::Other(e.to_string()))?;
        let form = multipart::Form::new().part("file", part);

        let url = format!("{}/upload-service/upload", BASE_URL);
        let resp = self.http
            .post(&url)
            .bearer_auth(&token.access_token)
            .header("NK", "NT")
            .multipart(form)
            .send()
            .await?;

        if !resp.status().is_success() {
            let status = resp.status().as_u16();
            let msg = resp.text().await.unwrap_or_default();
            return Err(AppError::Api { status, message: msg });
        }

        let body: serde_json::Value = resp.json().await?;
        let result = &body["detailedImportResult"];

        if let Some(success) = result["successes"].as_array().and_then(|a| a.first()) {
            Ok(ActivityUploadResult {
                activity_id: success["activityId"].as_u64().unwrap_or(0),
                detail_id: success["activityId"].as_u64().unwrap_or(0),
            })
        } else {
            let failures = result["failures"].as_array()
                .and_then(|a| a.first())
                .and_then(|f| f["messages"].as_array())
                .and_then(|m| m.first())
                .and_then(|msg| msg["content"].as_str())
                .unwrap_or("Unknown upload error");
            Err(AppError::Api { status: 200, message: failures.to_string() })
        }
    }

    pub async fn list_devices(&self, token: &OAuth2Token) -> Result<Vec<DeviceInfo>, AppError> {
        let url = format!("{}/device-service/deviceregistration/devices", BASE_URL);
        let resp = self.http
            .get(&url)
            .bearer_auth(&token.access_token)
            .send()
            .await?;

        if !resp.status().is_success() {
            let status = resp.status().as_u16();
            let msg = resp.text().await.unwrap_or_default();
            return Err(AppError::Api { status, message: msg });
        }

        let devices: Vec<DeviceInfo> = resp.json().await?;
        Ok(devices)
    }

    #[allow(dead_code)]
    pub async fn get_device_settings(
        &self,
        token: &OAuth2Token,
        device_id: u64,
    ) -> Result<serde_json::Value, AppError> {
        let url = format!(
            "{}/device-service/deviceservice/device-info/settings/{}",
            BASE_URL, device_id
        );
        let resp = self.http
            .get(&url)
            .bearer_auth(&token.access_token)
            .send()
            .await?;

        if !resp.status().is_success() {
            let status = resp.status().as_u16();
            let msg = resp.text().await.unwrap_or_default();
            return Err(AppError::Api { status, message: msg });
        }

        Ok(resp.json().await?)
    }
}
