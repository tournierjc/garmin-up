use serde::Serialize;
use thiserror::Error;

#[derive(Debug, Error)]
pub enum AppError {
    #[error("XML parse error: {0}")]
    XmlParse(#[from] quick_xml::DeError),

    #[error("IO error: {0}")]
    Io(#[from] std::io::Error),

    #[error("HTTP error: {0}")]
    Http(#[from] reqwest::Error),

    #[error("Device not found: {0}")]
    DeviceNotFound(String),

    #[error("FIT parse error: {0}")]
    #[allow(dead_code)]
    FitParse(String),

    #[error("Auth error: {0}")]
    Auth(String),

    #[error("API error: {status} {message}")]
    Api { status: u16, message: String },

    #[error("{0}")]
    Other(String),
}

impl Serialize for AppError {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: serde::Serializer,
    {
        serializer.serialize_str(&self.to_string())
    }
}
