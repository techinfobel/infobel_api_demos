//! Demo binary that searches the Infobel BizSearch API for a company name,
//! prints the first page of results, then fetches subsequent pages.

use reqwest::blocking::Client;

use infobel_api_demos::auth::{get_infobel_token, ApiType};
use infobel_api_demos::bizsearch::{get_search_page, search_and_get_first_page};
use infobel_api_demos::formatting::print_results;

fn run() -> i32 {
    let token_response = match get_infobel_token(ApiType::BizSearch) {
        Ok(resp) => resp,
        Err(err) => {
            eprintln!("Authentication failed: {err}");
            return 1;
        }
    };

    let access_token = &token_response.access_token;
    let company_name = "Nvidia";
    let client = Client::new();

    // --- Method 1: Initial search to get searchId and first page ---
    println!("--- Searching for '{company_name}' and fetching first page ---");

    let initial_response = match search_and_get_first_page(&client, access_token, company_name) {
        Ok(resp) => resp,
        Err(err) => {
            eprintln!("{err}");
            return 1;
        }
    };

    let search_id = initial_response.get("searchId").and_then(|v| v.as_i64());

    let first_page_records = initial_response
        .get("firstPageRecords")
        .and_then(|v| v.as_array());

    match first_page_records {
        Some(records) if !records.is_empty() => {
            print_results(records);
        }
        _ => {
            println!("No records returned in the first page.");
            return 0;
        }
    }

    let search_id = match search_id {
        Some(id) => id,
        None => {
            println!("No searchId returned, cannot fetch subsequent pages.");
            return 0;
        }
    };

    // --- Method 2: Fetch subsequent pages using searchId ---
    for page in 2..4 {
        println!("--- Fetching page {page} for search ID {search_id} ---");

        let paged_response = match get_search_page(&client, access_token, search_id, page) {
            Ok(resp) => resp,
            Err(err) => {
                eprintln!("{err}");
                continue;
            }
        };

        let records = paged_response.get("records").and_then(|v| v.as_array());

        match records {
            Some(recs) if !recs.is_empty() => {
                print_results(recs);
            }
            _ => {
                println!("No records returned on page {page}.");
                break;
            }
        }
    }

    0
}

fn main() {
    std::process::exit(run());
}
