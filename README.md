# Infobel API Demos

Multi-language demonstration scripts for the Infobel BizSearch and GetData APIs.

## Project Structure

```
├── python/          Python demos & tests
├── rust/            Rust demos
├── api-calls.mmd    API call-flow diagram
└── README.md
```

Each language lives in its own top-level directory with its own dependency management and build tooling.

---

## Python

### Prerequisites

- Python 3.8+
- An Infobel Pro account with API access

### Installation

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/techinfobel/infobel_api_demos.git
    cd infobel_api_demos/python
    ```

2.  **Create a virtual environment and install dependencies:**
    ```bash
    python -m venv venv
    source venv/bin/activate  # On Windows, use `venv\Scripts\activate`
    pip install -r requirements.txt
    ```

### Authentication

The scripts use OAuth2 for authentication. Create a `.env` file in the `python/` directory with your credentials:

```env
INFOBEL_USERNAME="your_username"
INFOBEL_PASSWORD="your_password"
```

The authentication logic is handled by `infobel_api_auth.py`, which retrieves an access token from the Infobel token endpoint.

### Demos

#### BizSearch Demo (`python/bizsearch_demo.py`)

Demonstrates how to query the BizSearch API. By default, it searches for "Nvidia" in the United States.

```bash
python bizsearch_demo.py
```

The script will print the company name, address, contact information, activity, and location (latitude/longitude) with a link to OpenStreetMap for the top 5 results.

#### GetData Demo (`python/getdata_demo.py`)

Shows how to use the GetData API. It queries for the top 10 semiconductor companies in the US using the SIC code `3674`.

```bash
python getdata_demo.py
```

The output will include the company name, address, contact details, activity, and a link to the company's location on OpenStreetMap.

### Testing

The project includes a test suite with 53 tests and 98% code coverage. Pure logic functions are tested directly; HTTP transport is intercepted via the [`responses`](https://github.com/getsentry/responses) library.

**Run all tests:**
```bash
pytest
```

**Run with coverage report:**
```bash
pytest --cov=infobel_api_auth --cov=bizsearch_demo --cov=getdata_demo --cov-report=term-missing tests/
```

The test files mirror the source modules:

-   `tests/test_infobel_api_auth.py` — Environment variable handling, token payload construction, OAuth token flow (success, HTTP errors, missing token)
-   `tests/test_bizsearch_demo.py` — CSV splitting, search payload, address/contact formatting, search + pagination HTTP calls, `main()` control-flow branches
-   `tests/test_getdata_demo.py` — Search payload, address/contact formatting, search HTTP calls, `main()` control-flow branches

---

## Rust

### Prerequisites

- Rust 1.70+ (with Cargo)
- An Infobel Pro account with API access

### Installation

```bash
cd infobel_api_demos/rust
cargo build
```

### Authentication

Create a `.env` file in the `rust/` directory with your credentials:

```env
INFOBEL_USERNAME="your_username"
INFOBEL_PASSWORD="your_password"
```

### Demos

#### BizSearch Demo

Searches for "Nvidia" in the US, prints the first page, then fetches pages 2-3 via `searchId`.

```bash
cargo run --bin bizsearch_demo
```

#### GetData Demo

Queries for the top 10 US semiconductor companies (SIC code 3674).

```bash
cargo run --bin getdata_demo
```

### Architecture

The Rust implementation is a Cargo library + two binaries:

-   `src/auth.rs` — Shared auth module. `get_infobel_token(ApiType)` performs an OAuth2 password grant.
-   `src/formatting.rs` — Shared output formatting (`format_address`, `format_contact_fields`, `print_results`).
-   `src/bizsearch.rs` — BizSearch API client (`search_and_get_first_page`, `get_search_page`).
-   `src/getdata.rs` — GetData API client (`build_search_payload`, `run_search`).
-   `src/bin/bizsearch_demo.rs` — BizSearch demo binary.
-   `src/bin/getdata_demo.rs` — GetData demo binary.

Dependencies: `reqwest` (blocking), `serde`/`serde_json`, `dotenvy`, `thiserror`.

---

## Useful Links

-   **BizSearch API Documentation:** [https://bizsearch.infobelpro.com/Help](https://bizsearch.infobelpro.com/Help)
-   **BizSearch API Authentication:** [https://bizsearch.infobelpro.com/Help/Method/POST-api-token](https://bizsearch.infobelpro.com/Help/Method/POST-api-token)
-   **BizSearch API Search:** [https://bizsearch.infobelpro.com/Help/Method/POST-api-search](https://bizsearch.infobelpro.com/Help/Method/POST-api-search)
-   **BizSearch API Inputs:** [https://bizsearch.infobelpro.com/Help/Model/SearchInput](https://bizsearch.infobelpro.com/Help/Model/SearchInput)
-   **BizSearch API Outputs:** [https://bizsearch.infobelpro.com/Help/Model/SearchResult](https://bizsearch.infobelpro.com/Help/Model/SearchResult)
-   **BizSearch API Output records:** [https://bizsearch.infobelpro.com/Help/Model/Record](https://bizsearch.infobelpro.com/Help/Model/Record)
-   **BizSearch API Paging:** [https://bizsearch.infobelpro.com/Help/Method/GET-api-search-searchId-records-page_languageCode_internationalPhoneFormat](https://bizsearch.infobelpro.com/Help/Method/GET-api-search-searchId-records-page_languageCode_internationalPhoneFormat)

-   **GetData API Documentation:** [https://getdata.infobelpro.com/Help](https://getdata.infobelpro.com/Help)
-   **GetData API Authentication:** [https://getdata.infobelpro.com/Help/Method/POST-api-token](https://getdata.infobelpro.com/Help/Method/POST-api-token)
-   **GetData API Search:** [https://getdata.infobelpro.com/Help/Method/POST-api-search](https://getdata.infobelpro.com/Help/Method/POST-api-search)
-   **GetData API Inputs:** [https://getdata.infobelpro.com/Help/Model/SearchInput](https://getdata.infobelpro.com/Help/Model/SearchInput)
-   **GetData API Outputs:** [https://getdata.infobelpro.com/Help/Model/SearchResult](https://getdata.infobelpro.com/Help/Model/SearchResult)
-   **GetData API Output records:** [https://getdata.infobelpro.com/Help/Model/Record](https://getdata.infobelpro.com/Help/Model/Record)
-   **GetData API Paging:** [https://getdata.infobelpro.com/Help/Method/GET-api-search-searchId-records-page_languageCode_internationalPhoneFormat](https://getdata.infobelpro.com/Help/Method/GET-api-search-searchId-records-page_languageCode_internationalPhoneFormat)

-   **Infobel Pro:** [https://www.infobelpro.com/](https://www.infobelpro.com/)
