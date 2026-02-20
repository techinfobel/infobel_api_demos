//! Client helpers for the Infobel GetData API.
//!
//! Provides the search URL, a typed error enum, and functions to build a
//! search payload and execute the search request against the GetData endpoint.

use reqwest::blocking::Client;
use serde_json::Value;
use thiserror::Error;

/// Base URL for the GetData search endpoint.
pub const SEARCH_URL: &str = "https://getdata.infobelpro.com/api/search";

/// Errors that can occur when interacting with the GetData API.
#[derive(Debug, Error)]
pub enum GetDataApiError {
    /// An HTTP-level error (network failure, non-2xx status, etc.).
    #[error("GetData HTTP error: {0}")]
    HttpError(#[from] reqwest::Error),

    /// A logical error returned by the API or encountered while processing.
    #[error("GetData API error: {0}")]
    ApiError(String),
}

/// Build the JSON payload for a GetData search request.
///
/// Searches for US companies with SIC code 3674 (semiconductors),
/// returning the first page of 10 results sorted by relevance.
pub fn build_search_payload() -> Value {
    serde_json::json!({
        "dataType": 1,
        "pageSize": 10,
        "displayLanguage": "EN",
        "returnFirstPage": "true",
        "SortingOrder": [5],
        "CountryCodes": ["US"],
        "InternationalCodes": ["3674"],
    })
}

fn run_search_at(
    url: &str,
    client: &Client,
    access_token: &str,
    payload: &Value,
) -> Result<Value, GetDataApiError> {
    let response = client
        .post(url)
        .bearer_auth(access_token)
        .header("Content-Type", "application/json")
        .json(payload)
        .send()?
        .error_for_status()?;
    Ok(response.json()?)
}

/// Execute a POST search request against the GetData API.
///
/// Sends the given `payload` as JSON to [`SEARCH_URL`] using the provided
/// Bearer `access_token`. Returns the parsed JSON response on success.
pub fn run_search(
    client: &Client,
    access_token: &str,
    payload: &Value,
) -> Result<Value, GetDataApiError> {
    run_search_at(SEARCH_URL, client, access_token, payload)
}

#[cfg(test)]
mod tests {
    use super::*;
    use serde_json::json;

    // -----------------------------------------------------------------------
    // build_search_payload
    // -----------------------------------------------------------------------

    #[test]
    fn test_build_search_payload_required_keys() {
        let payload = build_search_payload();
        assert_eq!(payload["dataType"], 1);
        assert_eq!(payload["pageSize"], 10);
        assert_eq!(payload["CountryCodes"], json!(["US"]));
        assert_eq!(payload["InternationalCodes"], json!(["3674"]));
    }

    #[test]
    fn test_build_search_payload_return_first_page() {
        let payload = build_search_payload();
        assert_eq!(payload["returnFirstPage"], "true");
    }

    // -----------------------------------------------------------------------
    // run_search_at (HTTP tests via mockito)
    // -----------------------------------------------------------------------

    #[test]
    fn test_run_search_success() {
        let mut server = mockito::Server::new();
        let body = json!({"firstPageRecords": [{"companyName": "Intel"}]});
        let mock = server
            .mock("POST", "/api/search")
            .with_status(200)
            .with_header("content-type", "application/json")
            .with_body(body.to_string())
            .create();

        let client = Client::new();
        let url = format!("{}/api/search", server.url());
        let payload = json!({"dataType": 1});
        let result = run_search_at(&url, &client, "tok", &payload).unwrap();
        assert_eq!(result["firstPageRecords"][0]["companyName"], "Intel");
        mock.assert();
    }

    #[test]
    fn test_run_search_http_error() {
        let mut server = mockito::Server::new();
        let _mock = server
            .mock("POST", "/api/search")
            .with_status(500)
            .with_body(r#"{"error":"fail"}"#)
            .create();

        let client = Client::new();
        let url = format!("{}/api/search", server.url());
        let payload = json!({"dataType": 1});
        let err = run_search_at(&url, &client, "tok", &payload).unwrap_err();
        assert!(matches!(err, GetDataApiError::HttpError(_)));
    }
}
