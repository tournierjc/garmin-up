mod backup;
mod commands;
mod connect;
mod connectiq;
mod device;
mod error;
mod firmware;
mod fit;
mod maps;
mod xml;

use tracing_subscriber::EnvFilter;

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    let _ = rustls::crypto::ring::default_provider().install_default();

    tracing_subscriber::fmt()
        .with_env_filter(EnvFilter::from_default_env())
        .init();

    tauri::Builder::default()
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_dialog::init())
        .manage(device::DeviceState::default())
        .manage(commands::connect::ConnectState::default())
        .setup(|app| {
            let app_handle = app.handle().clone();
            tauri::async_runtime::spawn(async move {
                device::monitor::watch_devices(app_handle).await;
            });
            Ok(())
        })
        .invoke_handler(tauri::generate_handler![
            commands::device::list_devices,
            commands::device::get_device_info,
            commands::device::refresh_devices,
            commands::backup::backup_device,
            commands::backup::restore_device,
            commands::connect::connect_login,
            commands::connect::connect_logout,
            commands::connect::connect_status,
            commands::connect::connect_upload_activity,
            commands::connect::connect_list_devices,
            commands::firmware::check_firmware_update,
            commands::firmware::install_firmware,
            commands::maps::list_maps,
            commands::maps::install_map,
            commands::maps::remove_map,
            commands::maps::check_map_updates,
            commands::maps::check_map_updates_debug,
            commands::maps::download_and_install_map_update,
            commands::music::install_music_files,
            commands::connectiq::list_iq_apps,
            commands::connectiq::install_iq_app,
            commands::connectiq::remove_iq_app,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
