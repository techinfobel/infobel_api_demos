"""Utilities for retrieving an OAuth token from Infobel APIs."""

from __future__ import annotations

import os
from typing import Any, Dict, Literal

import requests
from dotenv import load_dotenv
from requests import Session


BIZSEARCH_TOKEN_URL = "https://bizsearch.infobelpro.com/api/token"
GETDATA_TOKEN_URL = "https://getdata.infobelpro.com/api/token"


class InfobelAuthError(RuntimeError):
    """Raised when the Infobel authentication flow fails."""


def _get_env(name: str) -> str:
    """Fetch a required environment variable."""
    value = os.getenv(name)
    if not value:
        raise InfobelAuthError(
            f"Environment variable '{name}' is required for Infobel authentication."
        )
    return value


def _build_token_payload() -> Dict[str, str]:
    """Construct the payload for the token request."""
    return {
        "grant_type": "password",
        "username": _get_env("INFOBEL_USERNAME"),
        "password": _get_env("INFOBEL_PASSWORD"),
    }


def get_infobel_token(
    api_type: Literal["bizsearch", "getdata"] = "bizsearch",
    *,
    session: Session | None = None,
    timeout: float = 30.0,
) -> Dict[str, Any]:
    """Retrieve an Infobel OAuth token for the specified API.

    Args:
        api_type: The API to authenticate against ("bizsearch" or "getdata").

    Returns the parsed JSON response body as a dictionary.
    """
    load_dotenv()

    if api_type == "bizsearch":
        token_url = BIZSEARCH_TOKEN_URL
    elif api_type == "getdata":
        token_url = GETDATA_TOKEN_URL
    else:
        raise ValueError(f"Unsupported api_type: {api_type}. Must be 'bizsearch' or 'getdata'.")

    payload = _build_token_payload()

    http = session or requests.Session()

    response = http.post(
        token_url,
        data=payload,
        headers={"Content-Type": "application/x-www-form-urlencoded"},
        timeout=timeout,
    )

    try:
        response.raise_for_status()
    except requests.HTTPError as exc:  # pragma: no cover - passthrough for visibility
        raise InfobelAuthError(
            f"Failed to obtain {api_type.capitalize()} token: {exc} | Response: {response.text}"
        ) from exc

    data: Dict[str, Any] = response.json()

    if "access_token" not in data:
        raise InfobelAuthError(
            f"{api_type.capitalize()} token response did not include 'access_token'."
        )

    return data
