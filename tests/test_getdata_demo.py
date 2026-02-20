"""Tests for getdata_demo module."""

from __future__ import annotations

import pytest
import responses

from getdata_demo import (
    SEARCH_URL,
    GetDataApiError,
    _build_search_payload,
    _format_address,
    _format_contact_fields,
    _print_results,
    _run_search,
    main,
)
from infobel_api_auth import GETDATA_TOKEN_URL, InfobelAuthError


# ---------------------------------------------------------------------------
# _build_search_payload
# ---------------------------------------------------------------------------

class TestBuildSearchPayload:
    def test_has_required_keys(self):
        payload = _build_search_payload()
        assert payload["dataType"] == 1
        assert payload["pageSize"] == 10
        assert payload["CountryCodes"] == ["US"]
        assert payload["InternationalCodes"] == ["3674"]

    def test_return_first_page_flag(self):
        payload = _build_search_payload()
        assert payload["returnFirstPage"] == "true"


# ---------------------------------------------------------------------------
# _format_address
# ---------------------------------------------------------------------------

class TestFormatAddress:
    def test_full_address(self):
        record = {
            "addressStreet": "Tech Dr",
            "addressHouseNumber": "10",
            "postCode": "95054",
            "city": "Santa Clara",
            "countryName": "United States",
        }
        result = _format_address(record)
        assert "Tech Dr 10" in result
        assert "95054 Santa Clara" in result
        assert "United States" in result

    def test_empty_record(self):
        assert _format_address({}) == ""

    def test_country_fallback(self):
        record = {"country": "US"}
        assert "US" in _format_address(record)


# ---------------------------------------------------------------------------
# _format_contact_fields
# ---------------------------------------------------------------------------

class TestFormatContactFields:
    def test_all_fields(self):
        record = {"phone": "111", "website": "w.com", "email": "e@e"}
        fields = list(_format_contact_fields(record))
        assert len(fields) == 3

    def test_no_fields(self):
        assert list(_format_contact_fields({})) == []

    def test_mobile_fallback(self):
        record = {"phoneOrMobile": "222"}
        fields = list(_format_contact_fields(record))
        assert "Phone: 222" in fields


# ---------------------------------------------------------------------------
# _run_search
# ---------------------------------------------------------------------------

class TestRunSearch:
    @responses.activate
    def test_success(self):
        body = {"firstPageRecords": [{"companyName": "Intel"}]}
        responses.post(SEARCH_URL, json=body, status=200)

        result = _run_search("tok", {"dataType": 1})
        assert result["firstPageRecords"][0]["companyName"] == "Intel"

    @responses.activate
    def test_http_error(self):
        responses.post(SEARCH_URL, json={"error": "fail"}, status=500)

        with pytest.raises(GetDataApiError, match="GetData search failed"):
            _run_search("tok", {"dataType": 1})


# ---------------------------------------------------------------------------
# _print_results
# ---------------------------------------------------------------------------

class TestPrintResults:
    def test_full_record(self, capsys):
        records = [
            {
                "companyName": "Intel",
                "addressStreet": "Mission",
                "addressHouseNumber": "2200",
                "postCode": "95054",
                "city": "Santa Clara",
                "countryName": "US",
                "uniqueID": "U2",
                "phone": "555-0200",
                "latitude": 37.39,
                "longitude": -121.97,
                "internationalLabel01": "Semiconductors",
            }
        ]
        _print_results(records)
        out = capsys.readouterr().out
        assert "Intel" in out
        assert "UniqueID: U2" in out
        assert "Phone: 555-0200" in out
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
    def test_happy_path(self, monkeypatch, capsys):
        monkeypatch.setenv("INFOBEL_USERNAME", "u")
        monkeypatch.setenv("INFOBEL_PASSWORD", "p")

        responses.post(GETDATA_TOKEN_URL, json={"access_token": "tok"}, status=200)
        responses.post(
            SEARCH_URL,
            json={"firstPageRecords": [{"companyName": "Intel"}]},
            status=200,
        )

        assert main() == 0
        out = capsys.readouterr().out
        assert "Intel" in out

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
        responses.post(GETDATA_TOKEN_URL, json={"access_token": "tok"}, status=200)
        responses.post(SEARCH_URL, json={"error": "boom"}, status=500)

        assert main() == 1
        out = capsys.readouterr().out
        assert "GetData search failed" in out

    @responses.activate
    def test_no_records(self, monkeypatch, capsys):
        monkeypatch.setenv("INFOBEL_USERNAME", "u")
        monkeypatch.setenv("INFOBEL_PASSWORD", "p")
        responses.post(GETDATA_TOKEN_URL, json={"access_token": "tok"}, status=200)
        responses.post(SEARCH_URL, json={"firstPageRecords": []}, status=200)

        assert main() == 0
        out = capsys.readouterr().out
        assert "No records returned" in out
