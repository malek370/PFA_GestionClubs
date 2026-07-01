"""Shared pytest fixtures for the Chatbot test suite.

Generates a throw-away RSA key pair and auto-mocks the JWKS client so that
every test that exercises JWT auth never makes a real HTTP call to the
IdentityProvider.
"""
from unittest.mock import MagicMock, patch

import pytest
from cryptography.hazmat.primitives.asymmetric import rsa

# One key pair for the whole test session — generated once, fast (in-memory).
TEST_PRIVATE_KEY = rsa.generate_private_key(public_exponent=65537, key_size=2048)
TEST_PUBLIC_KEY = TEST_PRIVATE_KEY.public_key()


@pytest.fixture(autouse=True)
def mock_jwks_client():
    """Replace the real JWKS client with one that uses the test public key.

    Applied automatically to every test so no test accidentally calls the
    live IdentityProvider endpoint.
    """
    mock_signing_key = MagicMock()
    mock_signing_key.key = TEST_PUBLIC_KEY

    mock_client = MagicMock()
    mock_client.get_signing_key_from_jwt.return_value = mock_signing_key

    with patch("app.security._get_jwks_client", return_value=mock_client):
        yield mock_client
