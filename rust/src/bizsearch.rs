//! Client helpers for the Infobel BizSearch API.

use reqwest::blocking::Client;
use serde_json::{json, Value};
use thiserror::Error;

/// Base URL for the BizSearch search endpoint.
pub const SEARCH_URL: &str = "https://bizsearch.infobelpro.com/api/search";

/// Errors that can occur when calling the BizSearch API.
#[derive(Debug, Error)]
pub enum BizSearchApiError {
    /// An HTTP-level error from reqwest.
    #[error("BizSearch HTTP error: {0}")]
    HttpError(#[from] reqwest::Error),

    /// A logical API error (e.g. unexpected response body).
    #[error("BizSearch API error: {0}")]
    ApiError(String),
}

/// Build the JSON payload for a `POST /api/search` request.
///
/// Searches for `company_name` in the US, returning the first page
/// with up to 3 results sorted by relevance.
pub fn build_search_payload(company_name: &str) -> Value {
    json!({
        "dataType": 1,
        "pageSize": 3,
        "displayLanguage": "EN",
        "returnFirstPage": "true",
        "SortingOrder": [5],
        "BusinessName": company_name,
        "CountryCodes": ["US"]
    })
}

fn search_at(
    base_url: &str,
    client: &Client,
    access_token: &str,
    company_name: &str,
) -> Result<Value, BizSearchApiError> {
    let payload = build_search_payload(company_name);
    let response = client
        .post(base_url)
        .bearer_auth(access_token)
        .header("Content-Type", "application/json")
        .json(&payload)
        .send()?
        .error_for_status()?;
    Ok(response.json()?)
}

/// Execute an initial search and return the JSON response containing
/// `searchId` and `firstPageRecords`.
pub fn search_and_get_first_page(
    client: &Client,
    access_token: &str,
    company_name: &str,
) -> Result<Value, BizSearchApiError> {
    search_at(SEARCH_URL, client, access_token, company_name)
}

fn get_page_at(
    base_url: &str,
    client: &Client,
    access_token: &str,
    search_id: i64,
    page: i32,
) -> Result<Value, BizSearchApiError> {
    let url = format!("{base_url}/{search_id}/records/{page}");
    let response = client
        .get(&url)
        .bearer_auth(access_token)
        .send()?
        .error_for_status()?;
    Ok(response.json()?)
}

/// Fetch a specific page of results for a previously issued search.
///
/// Uses `GET /api/search/{search_id}/records/{page}`.
pub fn get_search_page(
    client: &Client,
    access_token: &str,
    search_id: i64,
    page: i32,
) -> Result<Value, BizSearchApiError> {
    get_page_at(SEARCH_URL, client, access_token, search_id, page)
}

#[cfg(test)]
mod tests {
    use super::*;

    // -----------------------------------------------------------------------
    // build_search_payload
    // -----------------------------------------------------------------------

    #[test]
    fn test_build_search_payload_company_name() {
        let payload = build_search_payload("Nvidia");
        assert_eq!(payload["BusinessName"], "Nvidia");
    }

    #[test]
    fn test_build_search_payload_required_keys() {
        let payload = build_search_payload("Test");
        assert_eq!(payload["dataType"], 1);
        assert_eq!(payload["pageSize"], 3);
        assert_eq!(payload["CountryCodes"], json!(["US"]));
        assert_eq!(payload["returnFirstPage"], "true");
    }

    // -----------------------------------------------------------------------
    // search_at (HTTP tests via mockito)
    // -----------------------------------------------------------------------

    #[test]
    fn test_search_and_get_first_page_success() {
        let mut server = mockito::Server::new();
        let body = json!({"searchId": 99, "firstPageRecords": [{"companyName": "Acme"}]});
        let mock = server
            .mock("POST", "/api/search")
            .with_status(200)
            .with_header("content-type", "application/json")
            .with_body(body.to_string())
            .create();

        let client = Client::new();
        let url = format!("{}/api/search", server.url());
        let result = search_at(&url, &client, "tok", "Acme").unwrap();
        assert_eq!(result["searchId"], 99);
        mock.assert();
    }

    #[test]
    fn test_search_and_get_first_page_http_error() {
        let mut server = mockito::Server::new();
        let _mock = server
            .mock("POST", "/api/search")
            .with_status(500)
            .with_body(r#"{"error":"bad"}"#)
            .create();

        let client = Client::new();
        let url = format!("{}/api/search", server.url());
        let err = search_at(&url, &client, "tok", "X").unwrap_err();
        assert!(matches!(err, BizSearchApiError::HttpError(_)));
    }

    // -----------------------------------------------------------------------
    // get_page_at (HTTP tests via mockito)
    // -----------------------------------------------------------------------

    #[test]
    fn test_get_search_page_success() {
        let mut server = mockito::Server::new();
        let body = json!({"records": [{"companyName": "B"}]});
        let mock = server
            .mock("GET", "/api/search/99/records/2")
            .with_status(200)
            .with_header("content-type", "application/json")
            .with_body(body.to_string())
            .create();

        let client = Client::new();
        let base_url = format!("{}/api/search", server.url());
        let result = get_page_at(&base_url, &client, "tok", 99, 2).unwrap();
        assert_eq!(result["records"][0]["companyName"], "B");
        mock.assert();
    }

    #[test]
    fn test_get_search_page_http_error() {
        let mut server = mockito::Server::new();
        let _mock = server
            .mock("GET", "/api/search/99/records/2")
            .with_status(404)
            .with_body(r#"{"error":"fail"}"#)
            .create();

        let client = Client::new();
        let base_url = format!("{}/api/search", server.url());
        let err = get_page_at(&base_url, &client, "tok", 99, 2).unwrap_err();
        assert!(matches!(err, BizSearchApiError::HttpError(_)));
    }
}
