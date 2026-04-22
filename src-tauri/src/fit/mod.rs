use std::path::{Path, PathBuf};
use serde::Serialize;

use crate::error::AppError;

#[derive(Debug, Clone, Serialize)]
pub struct FitFileInfo {
    pub path: PathBuf,
    pub file_name: String,
    pub size: u64,
    pub fit_type: Option<String>,
    pub timestamp: Option<String>,
}

pub async fn scan_fit_files(device_mount: &Path) -> Result<Vec<FitFileInfo>, AppError> {
    let activity_dir = device_mount.join("Garmin").join("Activity");
    let mut files = Vec::new();

    if !activity_dir.exists() {
        return Ok(files);
    }

    let mut entries = tokio::fs::read_dir(&activity_dir).await?;
    while let Some(entry) = entries.next_entry().await? {
        let path = entry.path();
        if path.extension().and_then(|e| e.to_str()) == Some("fit") {
            let metadata = entry.metadata().await?;
            let file_name = path.file_name()
                .and_then(|n| n.to_str())
                .unwrap_or("")
                .to_string();

            files.push(FitFileInfo {
                path: path.clone(),
                file_name,
                size: metadata.len(),
                fit_type: Some("Activity".to_string()),
                timestamp: metadata.modified().ok().map(|t| {
                    let dt: chrono::DateTime<chrono::Utc> = t.into();
                    dt.format("%Y-%m-%d %H:%M:%S").to_string()
                }),
            });
        }
    }

    files.sort_by(|a, b| b.file_name.cmp(&a.file_name));
    Ok(files)
}

pub async fn get_pending_activities(
    device_mount: &Path,
    uploaded_dir: &Path,
) -> Result<Vec<FitFileInfo>, AppError> {
    let all = scan_fit_files(device_mount).await?;

    let uploaded: Vec<String> = if uploaded_dir.exists() {
        let mut names = Vec::new();
        let mut entries = tokio::fs::read_dir(uploaded_dir).await?;
        while let Some(entry) = entries.next_entry().await? {
            if let Some(name) = entry.file_name().to_str() {
                names.push(name.to_string());
            }
        }
        names
    } else {
        Vec::new()
    };

    let pending: Vec<FitFileInfo> = all.into_iter()
        .filter(|f| !uploaded.contains(&f.file_name))
        .collect();

    Ok(pending)
}
