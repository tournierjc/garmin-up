use std::time::Duration;

use tauri::{AppHandle, Emitter, Manager};
use tracing::{debug, error, info};

use super::detector;

pub async fn watch_devices(app: AppHandle) {
    info!("Starting device monitor");

    let mut last_count = 0usize;

    loop {
        let devices = match tokio::task::spawn_blocking(detector::scan_for_devices).await {
            Ok(devices) => devices,
            Err(err) => {
                error!("Device scan task failed: {err}");
                tokio::time::sleep(Duration::from_secs(3)).await;
                continue;
            }
        };
        let count = devices.len();

        if count != last_count {
            info!("Device count changed: {} -> {}", last_count, count);
            let _ = app.emit("devices-changed", &devices);
            last_count = count;

            if let Some(state) = app.try_state::<super::DeviceState>() {
                let mut stored = state.devices.lock().await;
                *stored = devices;
            }
        } else {
            debug!("Device scan: {} devices (unchanged)", count);
        }

        tokio::time::sleep(Duration::from_secs(3)).await;
    }
}
