use std::path::Path;
use tokio::fs;

use crate::error::AppError;
use super::checker::{FirmwareChecker, FirmwareInfo};

pub struct FirmwareInstaller;

impl FirmwareInstaller {
    pub async fn install_to_device(
        firmware_url: &str,
        device_mount: &Path,
    ) -> Result<FirmwareInfo, AppError> {
        let checker = FirmwareChecker::new()?;
        let gupdate_dir = device_mount.join("Garmin");
        fs::create_dir_all(&gupdate_dir).await?;

        let dest = gupdate_dir.join("gupdate.gcd");
        checker.download_firmware(firmware_url, &dest).await?;

        Ok(FirmwareInfo {
            current_version: 0,
            latest_version: None,
            latest_version_name: None,
            download_url: Some(firmware_url.to_string()),
            file_size: Some(fs::metadata(&dest).await?.len()),
            update_available: false,
            release_notes: None,
        })
    }
}
