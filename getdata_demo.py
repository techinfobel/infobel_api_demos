"""Example script that authenticates and queries the Infobel GetData API."""

from __future__ import annotations

import os
from typing import Any, Dict, Iterable, List

import requests
from dotenv import load_dotenv

from infobel_api_auth import InfobelAuthError, get_infobel_token


SEARCH_URL = "https://getdata.infobelpro.com/api/search"


class GetDataApiError(RuntimeError):
    """Raised when a GetData API call fails."""


def _build_search_payload() -> Dict[str, Any]:
    """Create the body for the POST /api/search request."""

    search_payload: Dict[str, Any] = {
        # https://getdata.infobelpro.com/Help/Model/SearchInput
        "dataType": 1,
        "pageSize": 10,
        "displayLanguage": "EN",
        "returnFirstPage": "true",
        "SortingOrder": [5],  # https://bizsearch.infobelpro.com/Help/Model/SortingOrder
        "CountryCodes": ["US"],
        "InternationalCodes": ["3674"],
    }

    return search_payload


def _format_address(record: Dict[str, Any]) -> str:
    """Combine address fields into a single readable string."""

    parts: List[str] = []

    street = record.get("addressStreet")
    number = record.get("addressHouseNumber")
    if street and number:
        parts.append(f"{street} {number}")
    elif street:
        parts.append(street)

    additional = record.get("addressExtra")
    if additional:
        parts.append(str(additional))

    locality_parts = [
        record.get("postCode"),
        record.get("city"),
        record.get("countryName") or record.get("country"),
    ]
    locality = " ".join(filter(None, locality_parts[:2]))
    if locality:
        parts.append(locality)
    if locality_parts[2]:
        parts.append(locality_parts[2])

    return ", ".join(part for part in parts if part)


def _format_contact_fields(record: Dict[str, Any]) -> Iterable[str]:
    phone = record.get("phone") or record.get("phoneOrMobile")
    website = record.get("website")
    email = record.get("email")

    if phone:
        yield f"Phone: {phone}"
    if website:
        yield f"Website: {website}"
    if email:
        yield f"Email: {email}"


def _run_search(access_token: str, payload: Dict[str, Any]) -> Dict[str, Any]:
    """Execute the POST /api/search call and return the parsed JSON."""

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json",
    }

    response = requests.post(SEARCH_URL, json=payload, headers=headers)

    try:
        response.raise_for_status()
    except requests.HTTPError as exc:  # pragma: no cover - passthrough for visibility
        raise GetDataApiError(
            f"GetData search failed: {exc} | Response: {response.text}"
        ) from exc

    return response.json()


def _print_results(records: Iterable[Dict[str, Any]]) -> None:
    for index, record in enumerate(records, start=1):
        company = record.get("companyName") or record.get("businessName") or "<Unknown>"
        address = _format_address(record) or "<No address provided>"
        unique_id = record.get("uniqueID")

        print(f"Result {index}:")
        if unique_id:
            print(f"  UniqueID: {unique_id}")
        print(f"  Company: {company}")
        print(f"  Address: {address}")

        for field in _format_contact_fields(record):
            print(f"  {field}")

        latitude = record.get("latitude")
        longitude = record.get("longitude")
        if latitude and longitude:
            print(f"  Location: {latitude}, {longitude}")
            print(
                "  OpenStreetMap: "
                f"https://www.openstreetmap.org/?mlat={latitude}&mlon={longitude}"
            )

        activity = record.get("internationalLabel01") or record.get("altInternationalLabel01")
        if activity:
            print(f"  Activity: {activity}")

        print()


def main() -> int:
    load_dotenv()

    try:
        token_response = get_infobel_token('getdata')
    except InfobelAuthError as exc:
        print(f"Authentication failed: {exc}")
        return 1

    access_token = token_response["access_token"]

    payload = _build_search_payload()

    try:
        search_response = _run_search(access_token, payload)
    except GetDataApiError as exc:
        print(str(exc))
        return 1

    records = search_response.get("firstPageRecords") or []
    if not records:
        print("No records returned in the first page.")
        return 0

    _print_results(records)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
