//! Demo binary that authenticates against the Infobel GetData API and prints
//! semiconductor company records from the US.

use infobel_api_demos::auth::{get_infobel_token, ApiType};
use infobel_api_demos::formatting::print_results;
use infobel_api_demos::getdata::{build_search_payload, run_search};
use reqwest::blocking::Client;

fn main() {
    let token_response = match get_infobel_token(ApiType::GetData) {
        Ok(resp) => resp,
        Err(err) => {
            eprintln!("Authentication failed: {err}");
            std::process::exit(1);
        }
    };

    let client = Client::new();
    let payload = build_search_payload();

    let search_response = match run_search(&client, &token_response.access_token, &payload) {
        Ok(resp) => resp,
        Err(err) => {
            eprintln!("{err}");
            std::process::exit(1);
        }
    };

    let records = search_response
        .get("firstPageRecords")
        .and_then(|v| v.as_array())
        .cloned()
        .unwrap_or_default();

    if records.is_empty() {
        println!("No records returned in the first page.");
        return;
    }

    print_results(&records);
}
