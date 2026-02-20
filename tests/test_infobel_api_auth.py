"""Tests for infobel_api_auth module."""

from __future__ import annotations

import pytest
import responses

from infobel_api_auth import (
    BIZSEARCH_TOKEN_URL,
    GETDATA_TOKEN_URL,
    InfobelAuthError,
    _build_token_payload,
    _get_env,
    get_infobel_token,
)


# ---------------------------------------------------------------------------
# _get_env
# ---------------------------------------------------------------------------

class TestGetEnv:
    def test_returns_value(self, monkeypatch):
        monkeypatch.setenv("TEST_VAR_ABC", "hello")
        assert _get_env("TEST_VAR_ABC") == "hello"

    def test_raises_when_missing(self, monkeypatch):
        monkeypatch.delenv("MISSING_VAR_XYZ", raising=False)
        with pytest.raises(InfobelAuthError, match="MISSING_VAR_XYZ"):
            _get_env("MISSING_VAR_XYZ")

    def test_raises_when_empty(self, monkeypatch):
        monkeypatch.setenv("EMPTY_VAR", "")
        with pytest.raises(InfobelAuthError, match="EMPTY_VAR"):
            _get_env("EMPTY_VAR")


# ---------------------------------------------------------------------------
# _build_token_payload
# ---------------------------------------------------------------------------

class TestBuildTokenPayload:
    def test_returns_correct_structure(self, monkeypatch):
        monkeypatch.setenv("INFOBEL_USERNAME", "user1")
        monkeypatch.setenv("INFOBEL_PASSWORD", "pass1")
        payload = _build_token_payload()
        assert payload == {
            "grant_type": "password",
            "username": "user1",
            "password": "pass1",
        }

    def test_raises_when_password_missing(self, monkeypatch):
        monkeypatch.setenv("INFOBEL_USERNAME", "user1")
        monkeypatch.setenv("INFOBEL_PASSWORD", "")
        with pytest.raises(InfobelAuthError, match="INFOBEL_PASSWORD"):
            _build_token_payload()


# ---------------------------------------------------------------------------
# get_infobel_token
# ---------------------------------------------------------------------------

class TestGetInfobelToken:
    @responses.activate
    def test_bizsearch_success(self, monkeypatch):
        monkeypatch.setenv("INFOBEL_USERNAME", "u")
        monkeypatch.setenv("INFOBEL_PASSWORD", "p")
        responses.post(BIZSEARCH_TOKEN_URL, json={"access_token": "tok123"}, status=200)

        result = get_infobel_token("bizsearch")
        assert result["access_token"] == "tok123"
        assert responses.calls[0].request.url == BIZSEARCH_TOKEN_URL

    @responses.activate
    def test_getdata_success(self, monkeypatch):
        monkeypatch.setenv("INFOBEL_USERNAME", "u")
        monkeypatch.setenv("INFOBEL_PASSWORD", "p")
        responses.post(GETDATA_TOKEN_URL, json={"access_token": "tok456"}, status=200)

        result = get_infobel_token("getdata")
        assert result["access_token"] == "tok456"
        assert responses.calls[0].request.url == GETDATA_TOKEN_URL

    def test_invalid_api_type(self, monkeypatch):
        monkeypatch.setenv("INFOBEL_USERNAME", "u")
        monkeypatch.setenv("INFOBEL_PASSWORD", "p")
        with pytest.raises(ValueError, match="Unsupported api_type"):
            get_infobel_token("invalid")  # type: ignore[arg-type]

    @responses.activate
    def test_http_error_raises(self, monkeypatch):
        monkeypatch.setenv("INFOBEL_USERNAME", "u")
        monkeypatch.setenv("INFOBEL_PASSWORD", "p")
        responses.post(BIZSEARCH_TOKEN_URL, json={"error": "unauthorized"}, status=401)

        with pytest.raises(InfobelAuthError, match="Failed to obtain"):
            get_infobel_token("bizsearch")

    @responses.activate
    def test_missing_access_token_raises(self, monkeypatch):
        monkeypatch.setenv("INFOBEL_USERNAME", "u")
        monkeypatch.setenv("INFOBEL_PASSWORD", "p")
        responses.post(BIZSEARCH_TOKEN_URL, json={"token_type": "bearer"}, status=200)

        with pytest.raises(InfobelAuthError, match="did not include 'access_token'"):
            get_infobel_token("bizsearch")
