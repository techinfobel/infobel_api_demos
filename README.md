# Infobel APIs Demo

This project provides demonstration scripts for using the Infobel BizSearch and GetData APIs.

## Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes.

### Prerequisites

- Python 3.8+
- An Infobel Pro account with API access

### Installation

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/techinfobel/api-demos.git
    cd infobel_api_demos
    ```

2.  **Install dependencies:**
    It's recommended to use a virtual environment.
    ```bash
    python -m venv venv
    source venv/bin/activate  # On Windows, use `venv\Scripts\activate`
    ```

    Install the required packages using pip:
    ```bash
    pip install -r requirements.txt
    ```

## Authentication

The scripts use OAuth2 for authentication. You will need to set up a `.env` file in the root of the project to store your credentials.

1.  Create a file named `.env` in the project root.
2.  Add your Infobel username and password to the file:

    ```env
    INFOBEL_USERNAME="your_username"
    INFOBEL_PASSWORD="your_password"
    ```

The authentication logic is handled by `infobel_api_auth.py`, which retrieves an access token from the Infobel token endpoint.

## Demos

The project includes two demo scripts, one for each of the BizSearch and GetData APIs.

### BizSearch Demo (`bizsearch_demo.py`)

This script demonstrates how to query the BizSearch API. By default, it searches for "Nvidia" in the United States.

**Usage:**
```bash
python bizsearch_demo.py
```

The script will print the company name, address, contact information, activity, and location (latitude/longitude) with a link to OpenStreetMap for the top 5 results.

### GetData Demo (`getdata_demo.py`)

This script shows how to use the GetData API. It queries for the top 10 semiconductor companies in the US using the SIC code `3674`.

**Usage:**
```bash
python getdata_demo.py
```

The output will include the company name, address, contact details, activity, and a link to the company's location on OpenStreetMap.

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
