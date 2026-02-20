# Infobel API Demos

Multi-language demo scripts for the Infobel BizSearch and GetData APIs. Each language lives in its own top-level directory (`python/`, `rust/`, etc.).

## Setup & Run (Python)

```bash
cd python
python -m venv venv
source venv/bin/activate
pip install -r requirements.txt
```

Create a `.env` file in `python/` with credentials:

```
INFOBEL_USERNAME="your_username"
INFOBEL_PASSWORD="your_password"
```

Run demos (from inside `python/`):

```bash
python bizsearch_demo.py
python getdata_demo.py
```

## Architecture

### Python (`python/`)

Three source files plus a test suite:

- `python/infobel_api_auth.py` — Shared auth module. OAuth2 password grant via `get_infobel_token(api_type)`. Reads credentials from env vars (`INFOBEL_USERNAME`, `INFOBEL_PASSWORD`).
- `python/bizsearch_demo.py` — BizSearch API demo. Searches by company name ("Nvidia"), demonstrates initial search + pagination via `searchId`.
- `python/getdata_demo.py` — GetData API demo. Queries by SIC code (3674 / semiconductors in US), single-page results.

### Rust (`rust/`)

A Cargo library with two binary targets:

- `rust/src/auth.rs` — Shared auth module. `get_infobel_token(ApiType)` → `Result<TokenResponse, InfobelAuthError>`. Uses `dotenvy` + env vars.
- `rust/src/formatting.rs` — Shared formatting (`format_address`, `format_contact_fields`, `print_results`) using `serde_json::Value`.
- `rust/src/bizsearch.rs` — BizSearch client. `search_and_get_first_page()` + `get_search_page()` with `reqwest::blocking`.
- `rust/src/getdata.rs` — GetData client. `build_search_payload()` + `run_search()`.
- `rust/src/bin/bizsearch_demo.rs` — BizSearch demo binary.
- `rust/src/bin/getdata_demo.rs` — GetData demo binary.

### .NET (`dotnet/`)

A solution with a shared class library and two console app targets:

- `dotnet/src/InfobelApiDemos/Auth/InfobelAuth.cs` — Shared auth module. `GetInfobelTokenAsync(ApiType)` → `Task<JsonObject>`. Uses `DotNetEnv` + env vars.
- `dotnet/src/InfobelApiDemos/Formatting/RecordFormatter.cs` — Shared formatting (`FormatAddress`, `FormatContactFields`, `PrintResults`) using `System.Text.Json.Nodes`.
- `dotnet/src/InfobelApiDemos/BizSearch/BizSearchClient.cs` — BizSearch client. `SearchAndGetFirstPageAsync()` + `GetSearchPageAsync()` with `HttpClient`.
- `dotnet/src/InfobelApiDemos/GetData/GetDataClient.cs` — GetData client. `BuildSearchPayload()` + `RunSearchAsync()`.
- `dotnet/src/BizSearchDemo/Program.cs` — BizSearch demo console app.
- `dotnet/src/GetDataDemo/Program.cs` — GetData demo console app.

## API Details

- BizSearch base: `https://bizsearch.infobelpro.com/api/`
- GetData base: `https://getdata.infobelpro.com/api/`
- Auth: POST `/api/token` with `grant_type=password` + credentials → returns `access_token`
- Search: POST `/api/search` with Bearer token → returns `searchId` + `firstPageRecords`
- Paging (BizSearch): GET `/api/search/{searchId}/records/{page}`

## Setup & Run (Rust)

```bash
cd rust
cargo build
```

Create a `.env` file in `rust/` with credentials (same format as Python).

Run demos (from inside `rust/`):

```bash
cargo run --bin bizsearch_demo
cargo run --bin getdata_demo
```

## Setup & Run (.NET)

```bash
cd dotnet
dotnet build
```

Create a `.env` file in `dotnet/` with credentials (same format as Python).

Run demos (from inside `dotnet/`):

```bash
dotnet run --project src/BizSearchDemo
dotnet run --project src/GetDataDemo
```

## Conventions

### Python
- Type hints throughout (`Dict[str, Any]`, `Literal`, etc.)
- Custom exception per module (`InfobelAuthError`, `BizSearchApiError`, `GetDataApiError`)
- Demo scripts follow a consistent pattern: `main() -> int` with `raise SystemExit(main())`
- Dependencies: `requests`, `python-dotenv`

### Rust
- `thiserror` error enums per module (`InfobelAuthError`, `BizSearchApiError`, `GetDataApiError`)
- `reqwest::blocking` for HTTP, `serde_json::Value` for dynamic API responses
- Two binary targets sharing a common library crate
- Dependencies: `reqwest`, `serde`, `serde_json`, `dotenvy`, `thiserror`

### .NET
- Custom exception per module (`InfobelAuthError`, `BizSearchApiError`, `GetDataApiError`)
- `async/await` throughout with `HttpClient` for HTTP
- `System.Text.Json.Nodes.JsonObject` for dynamic API responses
- Solution with shared class library + two console app projects
- Dependencies: `DotNetEnv`
