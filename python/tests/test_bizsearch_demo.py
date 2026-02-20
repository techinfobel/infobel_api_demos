"""Tests for bizsearch_demo module."""

from __future__ import annotations

import pytest
import responses

from bizsearch_demo import (
    SEARCH_URL,
    BizSearchApiError,
    _build_search_payload,
    _format_address,
    _format_contact_fields,
    _print_results,
    _split_csv,
    get_search_page,
    main,
    search_and_get_first_page,
)
from infobel_api_auth import BIZSEARCH_TOKEN_URL, InfobelAuthError


# ---------------------------------------------------------------------------
# _split_csv
# ---------------------------------------------------------------------------

class TestSplitCsv:
    def test_basic(self):
        assert _split_csv("a, b, c") == ["a", "b", "c"]

    def test_empty_string(self):
        assert _split_csv("") == []

    def test_whitespace_only_items(self):
        assert _split_csv(" , , ") == []

    def test_single_value(self):
        assert _split_csv("hello") == ["hello"]

    def test_strips_whitespace(self):
        assert _split_csv("  foo ,  bar  ") == ["foo", "bar"]


# ---------------------------------------------------------------------------
# _build_search_payload
# ---------------------------------------------------------------------------

class TestBuildSearchPayload:
    def test_contains_company_name(self):
        payload = _build_search_payload("Nvidia")
        assert payload["BusinessName"] == "Nvidia"

    def test_has_required_keys(self):
        payload = _build_search_payload("Test")
        assert payload["dataType"] == 1
        assert payload["pageSize"] == 3
        assert payload["CountryCodes"] == ["US"]
        assert payload["returnFirstPage"] == "true"


# ---------------------------------------------------------------------------
# _format_address
# ---------------------------------------------------------------------------

class TestFormatAddress:
    def test_full_address(self):
        record = {
            "addressStreet": "Main St",
            "addressHouseNumber": "42",
            "postCode": "10001",
            "city": "New York",
            "countryName": "United States",
        }
        result = _format_address(record)
        assert "Main St 42" in result
        assert "10001 New York" in result
        assert "United States" in result

    def test_street_without_number(self):
        record = {"addressStreet": "Broadway", "city": "LA"}
        result = _format_address(record)
        assert "Broadway" in result
        assert "LA" in result

    def test_empty_record(self):
        assert _format_address({}) == ""

    def test_address_extra(self):
        record = {"addressStreet": "Elm", "addressExtra": "Suite 5"}
        result = _format_address(record)
        assert "Suite 5" in result

    def test_country_fallback(self):
        record = {"country": "US"}
        result = _format_address(record)
        assert "US" in result


# ---------------------------------------------------------------------------
# _format_contact_fields
# ---------------------------------------------------------------------------

class TestFormatContactFields:
    def test_all_fields(self):
        record = {"phone": "123", "website": "example.com", "email": "a@b.c"}
        fields = list(_format_contact_fields(record))
        assert "Phone: 123" in fields
        assert "Website: example.com" in fields
        assert "Email: a@b.c" in fields

    def test_phone_fallback_to_mobile(self):
        record = {"phoneOrMobile": "555"}
        fields = list(_format_contact_fields(record))
        assert "Phone: 555" in fields

    def test_no_fields(self):
        assert list(_format_contact_fields({})) == []


# ---------------------------------------------------------------------------
# search_and_get_first_page
# ---------------------------------------------------------------------------

class TestSearchAndGetFirstPage:
    @responses.activate
    def test_success(self):
        body = {"searchId": 99, "firstPageRecords": [{"companyName": "Acme"}]}
        responses.post(SEARCH_URL, json=body, status=200)

        result = search_and_get_first_page("tok", "Acme")
        assert result["searchId"] == 99

    @responses.activate
    def test_http_error(self):
        responses.post(SEARCH_URL, json={"error": "bad"}, status=500)

        with pytest.raises(BizSearchApiError, match="BizSearch search failed"):
            search_and_get_first_page("tok", "X")


# ---------------------------------------------------------------------------
# get_search_page
# ---------------------------------------------------------------------------

class TestGetSearchPage:
    @responses.activate
    def test_success(self):
        url = f"{SEARCH_URL}/99/records/2"
        responses.get(url, json={"records": [{"companyName": "B"}]}, status=200)

        result = get_search_page("tok", 99, 2)
        assert result["records"][0]["companyName"] == "B"

    @responses.activate
    def test_http_error(self):
        url = f"{SEARCH_URL}/99/records/2"
        responses.get(url, json={"error": "fail"}, status=404)

        with pytest.raises(BizSearchApiError, match="get records failed"):
            get_search_page("tok", 99, 2)


# ---------------------------------------------------------------------------
# _print_results
# ---------------------------------------------------------------------------

class TestPrintResults:
    def test_full_record(self, capsys):
        records = [
            {
                "companyName": "Nvidia",
                "addressStreet": "Main",
                "addressHouseNumber": "1",
                "postCode": "95050",
                "city": "Santa Clara",
                "countryName": "US",
                "uniqueID": "U1",
                "phone": "555-0100",
                "website": "nvidia.com",
                "email": "info@nvidia.com",
                "latitude": 37.35,
                "longitude": -121.95,
                "internationalLabel01": "Semiconductors",
            }
        ]
        _print_results(records)
        out = capsys.readouterr().out
        assert "Nvidia" in out
        assert "UniqueID: U1" in out
        assert "Phone: 555-0100" in out
        assert "Website: nvidia.com" in out
        assert "Email: info@nvidia.com" in out
        assert "37.35" in out
        assert "OpenStreetMap" in out
        assert "Semiconductors" in out

    def test_minimal_record(self, capsys):
        _print_results([{}])
        out = capsys.readouterr().out
        assert "<Unknown>" in out
        assert "<No address provided>" in out


# ---------------------------------------------------------------------------
# main()
# ---------------------------------------------------------------------------

class TestMain:
    @responses.activate
    def test_happy_path_with_pagination(self, monkeypatch, capsys):
        monkeypatch.setenv("INFOBEL_USERNAME", "u")
        monkeypatch.setenv("INFOBEL_PASSWORD", "p")

        responses.post(BIZSEARCH_TOKEN_URL, json={"access_token": "tok"}, status=200)
        responses.post(
            SEARCH_URL,
            json={
                "searchId": 1,
                "firstPageRecords": [{"companyName": "Nvidia"}],
            },
            status=200,
        )
        responses.get(
            f"{SEARCH_URL}/1/records/2",
            json={"records": [{"companyName": "Page2"}]},
            status=200,
        )
        responses.get(
            f"{SEARCH_URL}/1/records/3",
            json={"records": [{"companyName": "Page3"}]},
            status=200,
        )

        assert main() == 0
        out = capsys.readouterr().out
        assert "Nvidia" in out
        assert "Page2" in out
        assert "Page3" in out

    @responses.activate
    def test_auth_failure(self, monkeypatch, capsys):
        monkeypatch.setenv("INFOBEL_USERNAME", "")
        monkeypatch.setenv("INFOBEL_PASSWORD", "p")

        assert main() == 1
        out = capsys.readouterr().out
        assert "Authentication failed" in out

    @responses.activate
    def test_search_failure(self, monkeypatch, capsys):
        monkeypatch.setenv("INFOBEL_USERNAME", "u")
        monkeypatch.setenv("INFOBEL_PASSWORD", "p")
        responses.post(BIZSEARCH_TOKEN_URL, json={"access_token": "tok"}, status=200)
        responses.post(SEARCH_URL, json={"error": "boom"}, status=500)

        assert main() == 1
        out = capsys.readouterr().out
        assert "BizSearch search failed" in out

    @responses.activate
    def test_no_records(self, monkeypatch, capsys):
        monkeypatch.setenv("INFOBEL_USERNAME", "u")
        monkeypatch.setenv("INFOBEL_PASSWORD", "p")
        responses.post(BIZSEARCH_TOKEN_URL, json={"access_token": "tok"}, status=200)
        responses.post(SEARCH_URL, json={"searchId": 1, "firstPageRecords": []}, status=200)

        assert main() == 0
        out = capsys.readouterr().out
        assert "No records returned" in out

    @responses.activate
    def test_no_search_id(self, monkeypatch, capsys):
        monkeypatch.setenv("INFOBEL_USERNAME", "u")
        monkeypatch.setenv("INFOBEL_PASSWORD", "p")
        responses.post(BIZSEARCH_TOKEN_URL, json={"access_token": "tok"}, status=200)
        responses.post(
            SEARCH_URL,
            json={"firstPageRecords": [{"companyName": "A"}]},
            status=200,
        )

        assert main() == 0
        out = capsys.readouterr().out
        assert "No searchId returned" in out

    @responses.activate
    def test_pagination_error_continues(self, monkeypatch, capsys):
        monkeypatch.setenv("INFOBEL_USERNAME", "u")
        monkeypatch.setenv("INFOBEL_PASSWORD", "p")
        responses.post(BIZSEARCH_TOKEN_URL, json={"access_token": "tok"}, status=200)
        responses.post(
            SEARCH_URL,
            json={"searchId": 1, "firstPageRecords": [{"companyName": "A"}]},
            status=200,
        )
        # Page 2 fails
        responses.get(f"{SEARCH_URL}/1/records/2", json={"error": "fail"}, status=500)
        # Page 3 succeeds
        responses.get(
            f"{SEARCH_URL}/1/records/3",
            json={"records": [{"companyName": "Page3"}]},
            status=200,
        )

        assert main() == 0
        out = capsys.readouterr().out
        assert "get records failed" in out
        assert "Page3" in out
