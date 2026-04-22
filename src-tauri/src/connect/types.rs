use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct OAuth1Token {
    pub token: String,
    pub secret: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct OAuth2Token {
    pub access_token: String,
    pub refresh_token: String,
    pub token_type: String,
    pub expires_at: i64,
}

impl OAuth2Token {
    pub fn is_expired(&self) -> bool {
        let now = chrono::Utc::now().timestamp();
        now >= self.expires_at - 60
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct GarminSession {
    pub display_name: String,
    pub oauth1: OAuth1Token,
    pub oauth2: OAuth2Token,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct OAuthConsumer {
    pub consumer_key: String,
    pub consumer_secret: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ActivityUploadResult {
    pub activity_id: u64,
    pub detail_id: u64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct DeviceInfo {
    pub device_id: Option<u64>,
    pub device_type: Option<String>,
    pub display_name: Option<String>,
    pub part_number: Option<String>,
    pub firmware_version: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct SoftwareUpdateInfo {
    pub version: String,
    pub version_int: u32,
    pub download_url: String,
    pub file_size: u64,
    pub release_notes: Option<String>,
    pub is_major: bool,
}
