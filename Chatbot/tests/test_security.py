"""Tests for stateless JWT auth using IdentityProvider RS256 public key."""
from types import SimpleNamespace

import jwt
import pytest
from fastapi import HTTPException

from app.security import get_current_user, require_admin
from tests.conftest import TEST_PRIVATE_KEY


def _request_with_auth(token: str | None) -> SimpleNamespace:
    headers = {"Authorization": f"Bearer {token}"} if token else {}
    return SimpleNamespace(headers=headers)


def _make_token(**claims) -> str:
    """Sign a test JWT with the test RSA private key (RS256).

    Always includes issuer and audience to match Settings defaults.
    """
    defaults = {"iss": "IdentityProvider", "aud": "myappusers"}
    defaults.update(claims)
    return jwt.encode(defaults, TEST_PRIVATE_KEY, algorithm="RS256")


def test_no_header_is_anonymous():
    user = get_current_user(_request_with_auth(None))
    assert user.user_id == "anonymous"
    assert user.authenticated is False


def test_valid_token_extracts_subject_and_roles():
    # .NET sends roles under the "role" claim (ClaimTypes.Role mapping)
    token = _make_token(sub="user-42", role=["PlatformAdmin", "USER"])
    user = get_current_user(_request_with_auth(token))
    assert user.user_id == "user-42"
    assert user.authenticated is True
    assert user.has_role("PlatformAdmin")
    assert user.has_role("ROLE_USER")


def test_single_role_string_is_accepted():
    """A single role is serialised as a plain string in the JWT, not a list."""
    token = _make_token(sub="user-1", role="PlatformAdmin")
    user = get_current_user(_request_with_auth(token))
    assert user.has_role("PlatformAdmin")


def test_invalid_signature_is_anonymous():
    from cryptography.hazmat.primitives.asymmetric import rsa as _rsa

    other_key = _rsa.generate_private_key(public_exponent=65537, key_size=2048)
    bad = jwt.encode(
        {"sub": "x", "iss": "IdentityProvider", "aud": "myappusers"},
        other_key,
        algorithm="RS256",
    )
    user = get_current_user(_request_with_auth(bad))
    assert user.user_id == "anonymous"
    assert user.authenticated is False


def test_require_admin_allows_admin():
    token = _make_token(sub="boss", role=["PlatformAdmin"])
    user = get_current_user(_request_with_auth(token))
    assert require_admin(user) is user


def test_require_admin_rejects_non_admin():
    token = _make_token(sub="joe", role=["USER"])
    user = get_current_user(_request_with_auth(token))
    with pytest.raises(HTTPException) as exc:
        require_admin(user)
    assert exc.value.status_code == 403
