use std::path::Path;
use tokio::fs;

use crate::device::{device_fs, resolve_garmin_volume_dir};
use crate::error::AppError;
use super::checker::{FirmwareChecker, FirmwareInfo};

use uuid::Uuid;

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
        let tmp = std::env::temp_dir().join(format!("garmin-up-fw-{}.gcd", Uuid::new_v4()));

        checker.download_firmware(firmware_url, &tmp).await?;
        let size = fs::metadata(&tmp).await?.len();
        let cp = device_fs::copy_local_to(&tmp, &dest).await;
        let _ = fs::remove_file(&tmp).await;
        cp?;

        Ok(FirmwareInfo {
            current_version: 0,
            latest_version: None,
            latest_version_name: None,
            download_url: Some(firmware_url.to_string()),
            file_size: Some(size),
            update_available: false,
            release_notes: None,
        })
    }
}
