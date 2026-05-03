use reqwest::{Client, header};

use crate::error::AppError;
use super::types::{GarminSession, OAuth1Token, OAuth2Token, OAuthConsumer};

const SSO_LOGIN_PAGE: &str = "https://sso.garmin.com/mobile/sso/en/sign-in";
const SSO_LOGIN_API: &str = "https://sso.garmin.com/mobile/api/login";
#[allow(dead_code)]
const SSO_MFA_VERIFY: &str = "https://sso.garmin.com/mobile/api/mfa/verifyCode";
const OAUTH_CONSUMER_URL: &str = "https://thegarth.s3.amazonaws.com/oauth_consumer.json";
const OAUTH1_PREAUTHORIZED: &str = "https://connectapi.garmin.com/oauth-service/oauth/preauthorized";
const OAUTH2_EXCHANGE: &str = "https://connectapi.garmin.com/oauth-service/oauth/exchange/user/2.0";
const SOCIAL_PROFILE: &str = "https://connectapi.garmin.com/userprofile-service/socialProfile";

// Match python-garminconnect primary mobile strategy: iOS client + Safari UA. The Android
// client (`GCM_ANDROID_DARK` + package UA) is rate-limited / blocked more often by Cloudflare.
const CLIENT_ID: &str = "GCM_IOS_DARK";
const SERVICE_URL: &str = "https://mobile.integration.garmin.com/gcm/ios";
const USER_AGENT: &str = "Mozilla/5.0 (iPhone; CPU iPhone OS 18_7 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148";

pub struct GarminAuth {
    http: Client,
    consumer: Option<OAuthConsumer>,
}

impl GarminAuth {
    pub fn new() -> Result<Self, AppError> {
        let http = Client::builder()
            .cookie_store(true)
            .redirect(reqwest::redirect::Policy::limited(20))
            .user_agent(USER_AGENT)
            .build()?;

        Ok(Self { http, consumer: None })
    }

    pub async fn login(&mut self, email: &str, password: &str) -> Result<GarminSession, AppError> {
        self.fetch_consumer_keys().await?;
        let ticket = self.sso_login(email, password).await?;
        let oauth1 = self.exchange_ticket_for_oauth1(&ticket).await?;
        let oauth2 = self.exchange_oauth1_for_oauth2(&oauth1).await?;
        let display_name = self.fetch_display_name(&oauth2).await.unwrap_or_default();

        Ok(GarminSession {
            display_name,
            oauth1,
            oauth2,
        })
    }

    pub async fn refresh_token(&self, session: &GarminSession) -> Result<OAuth2Token, AppError> {
        let consumer = self.consumer.as_ref()
            .ok_or_else(|| AppError::Auth("Consumer keys not loaded".into()))?;

        let auth_header = build_oauth1_header(
            "POST",
            OAUTH2_EXCHANGE,
            consumer,
            &session.oauth1,
        );

        let resp = self.http
            .post(OAUTH2_EXCHANGE)
            .header(header::AUTHORIZATION, &auth_header)
            .header(header::CONTENT_TYPE, "application/x-www-form-urlencoded")
            .body("audience=GARMIN_CONNECT_MOBILE_ANDROID_DI")
            .send()
            .await?;

        if !resp.status().is_success() {
            return Err(AppError::Auth(format!("Token refresh failed: {}", resp.status())));
        }

        let body: serde_json::Value = resp.json().await?;
        parse_oauth2_response(&body)
    }

    async fn fetch_consumer_keys(&mut self) -> Result<(), AppError> {
        let resp = self.http.get(OAUTH_CONSUMER_URL).send().await?;
        if !resp.status().is_success() {
            return Err(AppError::Auth("Failed to fetch OAuth consumer keys".into()));
        }

        let body: serde_json::Value = resp.json().await?;
        self.consumer = Some(OAuthConsumer {
            consumer_key: body["consumer_key"]
                .as_str()
                .ok_or_else(|| AppError::Auth("Missing consumer_key".into()))?
                .to_string(),
            consumer_secret: body["consumer_secret"]
                .as_str()
                .ok_or_else(|| AppError::Auth("Missing consumer_secret".into()))?
                .to_string(),
        });

        Ok(())
    }

    async fn sso_login(&self, email: &str, password: &str) -> Result<String, AppError> {
        self.http
            .get(SSO_LOGIN_PAGE)
            .query(&[("clientId", CLIENT_ID)])
            .send()
            .await?;

        // Garmin / Cloudflare treat instant GET→POST as bot-like (python-garminconnect uses a delay).
        tokio::time::sleep(std::time::Duration::from_millis(900)).await;

        let login_body = serde_json::json!({
            "username": email,
            "password": password,
            "rememberMe": true,
            "captchaToken": ""
        });

        let referer = format!("{SSO_LOGIN_PAGE}?clientId={CLIENT_ID}");
        let resp = self.http
            .post(SSO_LOGIN_API)
            .query(&[
                ("clientId", CLIENT_ID),
                ("locale", "en-US"),
                ("service", SERVICE_URL),
            ])
            .header(header::CONTENT_TYPE, "application/json")
            .header(header::ACCEPT, "application/json, text/plain, */*")
            .header(header::ORIGIN, "https://sso.garmin.com")
            .header(header::REFERER, &referer)
            .json(&login_body)
            .send()
            .await?;

        let status = resp.status();
        let text = resp.text().await?;

        if status == reqwest::StatusCode::TOO_MANY_REQUESTS {
            return Err(AppError::Auth(
                "Garmin SSO rate-limited this connection (HTTP 429). Wait a few minutes, try again, or use another network.".into(),
            ));
        }

        let body: serde_json::Value = serde_json::from_str(&text).map_err(|_| {
            let preview: String = text.chars().take(240).collect();
            AppError::Auth(format!(
                "Garmin SSO returned non-JSON (HTTP {}): {preview}",
                status.as_u16()
            ))
        })?;

        // Newer Garmin errors omit `responseStatus` and use top-level `error` (often 429 in JSON).
        if let Some(err) = body.get("error") {
            let code = err
                .get("status-code")
                .and_then(|v| v.as_str())
                .unwrap_or("?");
            let msg = err.get("message").and_then(|v| v.as_str()).unwrap_or("");
            let rid = err.get("request-id").and_then(|v| v.as_str()).unwrap_or("");
            if code == "429" {
                return Err(AppError::Auth(
                    "Garmin SSO rate-limited (429). Wait several minutes or try another network/VPN.".into(),
                ));
            }
            return Err(AppError::Auth(format!(
                "Garmin SSO error ({code}): {msg} (request-id: {rid})"
            )));
        }

        let response_type = body["responseStatus"]["type"].as_str().unwrap_or("");
        let status_detail = body["responseStatus"]["message"]
            .as_str()
            .or_else(|| body["responseStatus"]["details"].as_str())
            .filter(|s| !s.is_empty());

        match response_type {
            "SUCCESSFUL" => body["serviceTicketId"]
                .as_str()
                .map(|s| s.to_string())
                .ok_or_else(|| AppError::Auth("Login succeeded but no ticket returned".into())),
            "MFA_REQUIRED" => Err(AppError::Auth("MFA required — not supported in garmin-up yet. Use Garmin Connect web or app once, then try again if Garmin relaxes MFA for API login.".into())),
            "INVALID_USERNAME_PASSWORD" => Err(AppError::Auth("Invalid email or password.".into())),
            "" => {
                let preview: String = serde_json::to_string(&body)
                    .unwrap_or_else(|_| text.clone())
                    .chars()
                    .take(400)
                    .collect();
                Err(AppError::Auth(format!(
                    "Garmin SSO returned an unexpected JSON shape (no responseStatus.type). Preview: {preview}"
                )))
            }
            other => {
                let suffix = status_detail
                    .map(|d| format!(" — {d}"))
                    .unwrap_or_default();
                Err(AppError::Auth(format!(
                    "SSO login failed: {other}{suffix}"
                )))
            }
        }
    }

    async fn exchange_ticket_for_oauth1(&self, ticket: &str) -> Result<OAuth1Token, AppError> {
        let consumer = self.consumer.as_ref()
            .ok_or_else(|| AppError::Auth("Consumer keys not loaded".into()))?;

        let url_with_params = format!(
            "{}?ticket={}&login-url={}&accepts-mfa-tokens=true",
            OAUTH1_PREAUTHORIZED,
            percent_encode(ticket),
            percent_encode(SERVICE_URL),
        );

        let auth_header = build_oauth1_header_with_params(
            "GET",
            OAUTH1_PREAUTHORIZED,
            consumer,
            None,
            &[
                ("ticket", ticket),
                ("login-url", SERVICE_URL),
                ("accepts-mfa-tokens", "true"),
            ],
        );

        let resp = self.http
            .get(&url_with_params)
            .header(header::AUTHORIZATION, auth_header)
            .send()
            .await?;

        if !resp.status().is_success() {
            return Err(AppError::Auth(format!(
                "OAuth1 preauthorize failed: {}",
                resp.status()
            )));
        }

        let body = resp.text().await?;
        parse_oauth1_response(&body)
    }

    async fn exchange_oauth1_for_oauth2(&self, oauth1: &OAuth1Token) -> Result<OAuth2Token, AppError> {
        let consumer = self.consumer.as_ref()
            .ok_or_else(|| AppError::Auth("Consumer keys not loaded".into()))?;

        let auth_header = build_oauth1_header(
            "POST",
            OAUTH2_EXCHANGE,
            consumer,
            oauth1,
        );

        let resp = self.http
            .post(OAUTH2_EXCHANGE)
            .header(header::AUTHORIZATION, &auth_header)
            .header(header::CONTENT_TYPE, "application/x-www-form-urlencoded")
            .body("audience=GARMIN_CONNECT_MOBILE_ANDROID_DI")
            .send()
            .await?;

        if !resp.status().is_success() {
            return Err(AppError::Auth(format!("OAuth2 exchange failed: {}", resp.status())));
        }

        let body: serde_json::Value = resp.json().await?;
        parse_oauth2_response(&body)
    }

    async fn fetch_display_name(&self, oauth2: &OAuth2Token) -> Result<String, AppError> {
        let resp = self.http
            .get(SOCIAL_PROFILE)
            .bearer_auth(&oauth2.access_token)
            .send()
            .await?;

        if !resp.status().is_success() {
            return Err(AppError::Auth("Failed to fetch display name".into()));
        }

        let body: serde_json::Value = resp.json().await?;
        Ok(body["displayName"].as_str().unwrap_or("User").to_string())
    }
}

fn parse_oauth1_response(body: &str) -> Result<OAuth1Token, AppError> {
    let params: Vec<(String, String)> = url::form_urlencoded::parse(body.as_bytes())
        .into_owned()
        .collect();

    let token = params.iter()
        .find(|(k, _)| k == "oauth_token")
        .map(|(_, v)| v.clone())
        .ok_or_else(|| AppError::Auth("Missing oauth_token in response".into()))?;

    let secret = params.iter()
        .find(|(k, _)| k == "oauth_token_secret")
        .map(|(_, v)| v.clone())
        .ok_or_else(|| AppError::Auth("Missing oauth_token_secret in response".into()))?;

    Ok(OAuth1Token { token, secret })
}

fn parse_oauth2_response(body: &serde_json::Value) -> Result<OAuth2Token, AppError> {
    let access_token = body["access_token"]
        .as_str()
        .ok_or_else(|| AppError::Auth("Missing access_token".into()))?
        .to_string();
    let refresh_token = body["refresh_token"]
        .as_str()
        .ok_or_else(|| AppError::Auth("Missing refresh_token".into()))?
        .to_string();
    let token_type = body["token_type"]
        .as_str()
        .unwrap_or("Bearer")
        .to_string();
    let expires_in = body["expires_in"].as_i64().unwrap_or(3600);
    let expires_at = chrono::Utc::now().timestamp() + expires_in;

    Ok(OAuth2Token {
        access_token,
        refresh_token,
        token_type,
        expires_at,
    })
}

fn build_oauth1_header(
    method: &str,
    url_str: &str,
    consumer: &OAuthConsumer,
    token: &OAuth1Token,
) -> String {
    build_oauth1_header_with_params(method, url_str, consumer, Some(token), &[])
}

fn build_oauth1_header_with_params(
    method: &str,
    url_str: &str,
    consumer: &OAuthConsumer,
    token: Option<&OAuth1Token>,
    extra_params: &[(&str, &str)],
) -> String {
    use hmac::{Hmac, Mac};
    use sha1::Sha1;
    use base64::Engine;

    let timestamp = chrono::Utc::now().timestamp().to_string();
    let nonce = uuid::Uuid::new_v4().to_string().replace("-", "");

    let mut params: Vec<(&str, String)> = vec![
        ("oauth_consumer_key", consumer.consumer_key.clone()),
        ("oauth_nonce", nonce.clone()),
        ("oauth_signature_method", "HMAC-SHA1".to_string()),
        ("oauth_timestamp", timestamp.clone()),
        ("oauth_version", "1.0".to_string()),
    ];

    if let Some(t) = token {
        params.push(("oauth_token", t.token.clone()));
    }

    for (k, v) in extra_params {
        params.push((k, v.to_string()));
    }

    params.sort_by(|a, b| a.0.cmp(&b.0));

    let param_string: String = params.iter()
        .map(|(k, v)| format!("{}={}", percent_encode(k), percent_encode(v)))
        .collect::<Vec<_>>()
        .join("&");

    let base_string = format!(
        "{}&{}&{}",
        method.to_uppercase(),
        percent_encode(url_str),
        percent_encode(&param_string)
    );

    let token_secret = token.map(|t| t.secret.as_str()).unwrap_or("");
    let signing_key = format!(
        "{}&{}",
        percent_encode(&consumer.consumer_secret),
        percent_encode(token_secret)
    );

    let mut mac = Hmac::<Sha1>::new_from_slice(signing_key.as_bytes())
        .expect("HMAC accepts any key length");
    mac.update(base_string.as_bytes());
    let signature = base64::engine::general_purpose::STANDARD.encode(mac.finalize().into_bytes());

    let mut header_parts = vec![
        format!(r#"oauth_consumer_key="{}""#, percent_encode(&consumer.consumer_key)),
        format!(r#"oauth_nonce="{}""#, percent_encode(&nonce)),
        format!(r#"oauth_signature="{}""#, percent_encode(&signature)),
        format!(r#"oauth_signature_method="HMAC-SHA1""#),
        format!(r#"oauth_timestamp="{}""#, percent_encode(&timestamp)),
    ];

    if let Some(t) = token {
        header_parts.push(format!(r#"oauth_token="{}""#, percent_encode(&t.token)));
    }

    header_parts.push(r#"oauth_version="1.0""#.to_string());

    format!("OAuth {}", header_parts.join(","))
}

fn percent_encode(input: &str) -> String {
    let mut result = String::with_capacity(input.len() * 2);
    for byte in input.bytes() {
        match byte {
            b'A'..=b'Z' | b'a'..=b'z' | b'0'..=b'9' | b'-' | b'.' | b'_' | b'~' => {
                result.push(byte as char);
            }
            _ => {
                result.push_str(&format!("%{:02X}", byte));
            }
        }
    }
    result
}
