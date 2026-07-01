"""Tests for stateless JWT auth, mirroring JwtAuthFilter / JwtService."""
from types import SimpleNamespace

import jwt
import pytest
from fastapi import HTTPException

from app.config import get_settings
from app.security import get_current_user, require_admin


def _request_with_auth(token: str | None) -> SimpleNamespace:
    headers = {"Authorization": f"Bearer {token}"} if token else {}
    return SimpleNamespace(headers=headers)


def _make_token(**claims) -> str:
    return jwt.encode(claims, get_settings().jwt_secret, algorithm="HS256")


def test_no_header_is_anonymous():
    user = get_current_user(_request_with_auth(None))
    assert user.user_id == "anonymous"
    assert user.authenticated is False


def test_valid_token_extracts_subject_and_roles():
    token = _make_token(sub="user-42", roles=["ADMIN", "USER"])
    user = get_current_user(_request_with_auth(token))
    assert user.user_id == "user-42"
    assert user.authenticated is True
    assert user.has_role("ADMIN")
    assert user.has_role("ROLE_USER")


def test_invalid_signature_is_anonymous():
    bad = jwt.encode({"sub": "x"}, "a-completely-different-secret-value", algorithm="HS256")
    user = get_current_user(_request_with_auth(bad))
    assert user.user_id == "anonymous"
    assert user.authenticated is False


def test_require_admin_allows_admin():
    token = _make_token(sub="boss", roles=["ADMIN"])
    user = get_current_user(_request_with_auth(token))
    assert require_admin(user) is user


def test_require_admin_rejects_non_admin():
    token = _make_token(sub="joe", roles=["USER"])
    user = get_current_user(_request_with_auth(token))
    with pytest.raises(HTTPException) as exc:
        require_admin(user)
    assert exc.value.status_code == 403
