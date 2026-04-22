use std::path::PathBuf;
use std::sync::Arc;
use tauri::State;
use tokio::sync::Mutex;

use crate::connect::auth::GarminAuth;
use crate::connect::client::GarminClient;
use crate::connect::types::{GarminSession, ActivityUploadResult, DeviceInfo};
use crate::error::AppError;

pub struct ConnectState {
    pub session: Arc<Mutex<Option<GarminSession>>>,
}

impl Default for ConnectState {
    fn default() -> Self {
        Self {
            session: Arc::new(Mutex::new(None)),
        }
    }
}

#[tauri::command]
pub async fn connect_login(
    email: String,
    password: String,
    state: State<'_, ConnectState>,
) -> Result<GarminSession, AppError> {
    let mut auth = GarminAuth::new()?;
    let session = auth.login(&email, &password).await?;
    *state.session.lock().await = Some(session.clone());
    Ok(session)
}

#[tauri::command]
pub async fn connect_logout(
    state: State<'_, ConnectState>,
) -> Result<(), AppError> {
    *state.session.lock().await = None;
    Ok(())
}

#[tauri::command]
pub async fn connect_status(
    state: State<'_, ConnectState>,
) -> Result<Option<String>, AppError> {
    let guard = state.session.lock().await;
    Ok(guard.as_ref().map(|s| s.display_name.clone()))
}

#[tauri::command]
pub async fn connect_upload_activity(
    file_path: String,
    state: State<'_, ConnectState>,
) -> Result<ActivityUploadResult, AppError> {
    let session = get_valid_session(&state).await?;
    let client = GarminClient::new()?;
    client.upload_activity(&session.oauth2, &PathBuf::from(file_path)).await
}

#[tauri::command]
pub async fn connect_list_devices(
    state: State<'_, ConnectState>,
) -> Result<Vec<DeviceInfo>, AppError> {
    let session = get_valid_session(&state).await?;
    let client = GarminClient::new()?;
    client.list_devices(&session.oauth2).await
}

async fn get_valid_session(state: &State<'_, ConnectState>) -> Result<GarminSession, AppError> {
    let mut guard = state.session.lock().await;
    let session = guard.as_mut()
        .ok_or_else(|| AppError::Auth("Not logged in".into()))?;

    if session.oauth2.is_expired() {
        let auth = GarminAuth::new()?;
        session.oauth2 = auth.refresh_token(session).await?;
    }

    Ok(session.clone())
}
