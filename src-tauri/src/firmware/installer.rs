use std::path::Path;
use tokio::fs;

use crate::device::resolve_garmin_volume_dir;
use crate::error::AppError;
use super::checker::{FirmwareChecker, FirmwareInfo};

pub struct FirmwareInstaller;

impl FirmwareInstaller {
    pub async fn install_to_device(
        firmware_url: &str,
        device_mount: &Path,
    ) -> Result<FirmwareInfo, AppError> {
        let checker = FirmwareChecker::new()?;
        let garmin_vol = resolve_garmin_volume_dir(device_mount).ok_or_else(|| {
            AppError::Other(format!(
                "Device mount has neither GARMIN nor Garmin folder: {}",
                device_mount.display()
            ))
        })?;

        let dest = garmin_vol.join("gupdate.gcd");
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
