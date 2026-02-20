//! Utilities for retrieving an OAuth token from Infobel APIs.

use reqwest::blocking::Client;
use serde::Deserialize;
use thiserror::Error;

const BIZSEARCH_TOKEN_URL: &str = "https://bizsearch.infobelpro.com/api/token";
const GETDATA_TOKEN_URL: &str = "https://getdata.infobelpro.com/api/token";

/// Errors that can occur during the Infobel authentication flow.
#[derive(Debug, Error)]
pub enum InfobelAuthError {
    /// A required environment variable is missing or empty.
    #[error("Environment variable '{0}' is required for Infobel authentication.")]
    MissingEnvVar(String),

    /// An HTTP request to the token endpoint failed.
    #[error("Failed to obtain token: {0}")]
    HttpError(#[from] reqwest::Error),

    /// The token response did not include an `access_token` field.
    #[error("Token response did not include 'access_token'.")]
    MissingAccessToken,
}

/// Selects which Infobel API to authenticate against.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum ApiType {
    /// The BizSearch API at `bizsearch.infobelpro.com`.
    BizSearch,
    /// The GetData API at `getdata.infobelpro.com`.
    GetData,
}

impl ApiType {
    /// Returns the token endpoint URL for this API type.
    fn token_url(self) -> &'static str {
        match self {
            ApiType::BizSearch => BIZSEARCH_TOKEN_URL,
            ApiType::GetData => GETDATA_TOKEN_URL,
        }
    }
}

/// The JSON response returned by the Infobel token endpoint.
#[derive(Debug, Deserialize)]
pub struct TokenResponse {
    /// The OAuth2 bearer token.
    pub access_token: String,
}

fn get_env(name: &str) -> Result<String, InfobelAuthError> {
    let value = std::env::var(name).unwrap_or_default();
    if value.is_empty() {
        return Err(InfobelAuthError::MissingEnvVar(name.to_string()));
    }
    Ok(value)
}

fn build_token_payload() -> Result<[(&'static str, String); 3], InfobelAuthError> {
    Ok([
        ("grant_type", "password".to_string()),
        ("username", get_env("INFOBEL_USERNAME")?),
        ("password", get_env("INFOBEL_PASSWORD")?),
    ])
}

fn get_token_from_url(url: &str) -> Result<TokenResponse, InfobelAuthError> {
    let payload = build_token_payload()?;
    let client = Client::new();
    let response = client
        .post(url)
        .header("Content-Type", "application/x-www-form-urlencoded")
        .form(&payload)
        .send()?
        .error_for_status()?;

    let data: serde_json::Value = response.json()?;
    serde_json::from_value::<TokenResponse>(data)
        .map_err(|_| InfobelAuthError::MissingAccessToken)
}

/// Authenticate against an Infobel API and return the token response.
///
/// Reads `INFOBEL_USERNAME` and `INFOBEL_PASSWORD` from the environment
/// (loading `.env` via dotenvy first) and performs an OAuth2 password grant
/// against the token endpoint for the given [`ApiType`].
pub fn get_infobel_token(api_type: ApiType) -> Result<TokenResponse, InfobelAuthError> {
    dotenvy::dotenv().ok();
    get_token_from_url(api_type.token_url())
}

#[cfg(test)]
mod tests {
    use super::*;
    use serial_test::serial;

    // -----------------------------------------------------------------------
    // get_env
    // -----------------------------------------------------------------------

    #[test]
    fn test_get_env_returns_value() {
        std::env::set_var("TEST_VAR_RUST_ABC", "hello");
        assert_eq!(get_env("TEST_VAR_RUST_ABC").unwrap(), "hello");
        std::env::remove_var("TEST_VAR_RUST_ABC");
    }

    #[test]
    fn test_get_env_raises_when_missing() {
        std::env::remove_var("MISSING_VAR_RUST_XYZ");
        let err = get_env("MISSING_VAR_RUST_XYZ").unwrap_err();
        assert!(err.to_string().contains("MISSING_VAR_RUST_XYZ"));
    }

    #[test]
    fn test_get_env_raises_when_empty() {
        std::env::set_var("EMPTY_VAR_RUST", "");
        let err = get_env("EMPTY_VAR_RUST").unwrap_err();
        assert!(err.to_string().contains("EMPTY_VAR_RUST"));
        std::env::remove_var("EMPTY_VAR_RUST");
    }

    // -----------------------------------------------------------------------
    // build_token_payload
    // -----------------------------------------------------------------------

    #[test]
    #[serial]
    fn test_build_token_payload_structure() {
        std::env::set_var("INFOBEL_USERNAME", "user1");
        std::env::set_var("INFOBEL_PASSWORD", "pass1");
        let payload = build_token_payload().unwrap();
        assert_eq!(payload[0], ("grant_type", "password".to_string()));
        assert_eq!(payload[1], ("username", "user1".to_string()));
        assert_eq!(payload[2], ("password", "pass1".to_string()));
        std::env::remove_var("INFOBEL_USERNAME");
        std::env::remove_var("INFOBEL_PASSWORD");
    }

    #[test]
    #[serial]
    fn test_build_token_payload_missing_password() {
        std::env::set_var("INFOBEL_USERNAME", "user1");
        std::env::set_var("INFOBEL_PASSWORD", "");
        let err = build_token_payload().unwrap_err();
        assert!(err.to_string().contains("INFOBEL_PASSWORD"));
        std::env::remove_var("INFOBEL_USERNAME");
        std::env::remove_var("INFOBEL_PASSWORD");
    }

    // -----------------------------------------------------------------------
    // ApiType::token_url
    // -----------------------------------------------------------------------

    #[test]
    fn test_bizsearch_token_url() {
        assert_eq!(
            ApiType::BizSearch.token_url(),
            "https://bizsearch.infobelpro.com/api/token"
        );
    }

    #[test]
    fn test_getdata_token_url() {
        assert_eq!(
            ApiType::GetData.token_url(),
            "https://getdata.infobelpro.com/api/token"
        );
    }

    // -----------------------------------------------------------------------
    // get_token_from_url (HTTP tests via mockito)
    // -----------------------------------------------------------------------

    #[test]
    #[serial]
    fn test_token_success() {
        let mut server = mockito::Server::new();
        let mock = server
            .mock("POST", "/api/token")
            .with_status(200)
            .with_header("content-type", "application/json")
            .with_body(r#"{"access_token":"tok123"}"#)
            .create();

        std::env::set_var("INFOBEL_USERNAME", "u");
        std::env::set_var("INFOBEL_PASSWORD", "p");
        let url = format!("{}/api/token", server.url());
        let result = get_token_from_url(&url).unwrap();
        assert_eq!(result.access_token, "tok123");
        mock.assert();
        std::env::remove_var("INFOBEL_USERNAME");
        std::env::remove_var("INFOBEL_PASSWORD");
    }

    #[test]
    #[serial]
    fn test_token_http_error() {
        let mut server = mockito::Server::new();
        let _mock = server
            .mock("POST", "/api/token")
            .with_status(401)
            .with_body(r#"{"error":"unauthorized"}"#)
            .create();

        std::env::set_var("INFOBEL_USERNAME", "u");
        std::env::set_var("INFOBEL_PASSWORD", "p");
        let url = format!("{}/api/token", server.url());
        let err = get_token_from_url(&url).unwrap_err();
        assert!(matches!(err, InfobelAuthError::HttpError(_)));
        std::env::remove_var("INFOBEL_USERNAME");
        std::env::remove_var("INFOBEL_PASSWORD");
    }

    #[test]
    #[serial]
    fn test_missing_access_token() {
        let mut server = mockito::Server::new();
        let _mock = server
            .mock("POST", "/api/token")
            .with_status(200)
            .with_header("content-type", "application/json")
            .with_body(r#"{"token_type":"bearer"}"#)
            .create();

        std::env::set_var("INFOBEL_USERNAME", "u");
        std::env::set_var("INFOBEL_PASSWORD", "p");
        let url = format!("{}/api/token", server.url());
        let err = get_token_from_url(&url).unwrap_err();
        assert!(matches!(err, InfobelAuthError::MissingAccessToken));
        std::env::remove_var("INFOBEL_USERNAME");
        std::env::remove_var("INFOBEL_PASSWORD");
    }
}
