import logging
from dataclasses import dataclass, field
from functools import lru_cache

import jwt
from jwt import PyJWKClient
from fastapi import Depends, HTTPException, Request, status

from app.config import get_settings

log = logging.getLogger(__name__)


@lru_cache(maxsize=1)
def _get_jwks_client() -> PyJWKClient:
    """Lazy singleton PyJWKClient — fetches and caches IdentityProvider public keys."""
    return PyJWKClient(get_settings().idp_jwks_url)


@dataclass
class CurrentUser:
    """Authentication context extracted from the JWT, mirroring the Spring
    ``SecurityContextHolder`` principal."""

    user_id: str = "anonymous"
    roles: list[str] = field(default_factory=list)
    authenticated: bool = False

    def has_role(self, role: str) -> bool:
        normalized = role if role.startswith("ROLE_") else f"ROLE_{role}"
        return normalized in self.roles


def _normalize_roles(raw: object) -> list[str]:
    # .NET ClaimTypes.Role serialises as a single string when there is one role,
    # or a JSON array when there are several; accept both forms.
    if isinstance(raw, str):
        raw = [raw]
    if not isinstance(raw, list):
        return []
    roles: list[str] = []
    for r in raw:
        s = str(r)
        roles.append(s if s.startswith("ROLE_") else f"ROLE_{s}")
    return roles


def get_current_user(request: Request) -> CurrentUser:
    """Stateless JWT validation using IdentityProvider RSA public key (RS256).
    Absent/invalid tokens yield an anonymous user — the request continues
    unauthenticated, matching the behaviour of ``JwtAuthFilter``."""
    header = request.headers.get("Authorization")
    if not header or not header.startswith("Bearer "):
        return CurrentUser()

    token = header[7:]
    settings = get_settings()
    try:
        signing_key = _get_jwks_client().get_signing_key_from_jwt(token)
        claims = jwt.decode(
            token,
            signing_key.key,
            algorithms=["RS256"],
            issuer=settings.jwt_issuer,
            audience=settings.jwt_audience,
        )
    except Exception as e:  # noqa: BLE001 — any failure (bad sig, expired, JWKS fetch) → anonymous
        log.debug("JWT validation failed: %s", e)
        return CurrentUser()

    user_id = claims.get("sub") or "anonymous"
    # .NET maps ClaimTypes.Role → "role"; accept "roles" for test/legacy tokens
    raw_roles = claims.get("role") or claims.get("roles")
    roles = _normalize_roles(raw_roles)
    return CurrentUser(user_id=user_id, roles=roles, authenticated=True)


def require_admin(user: CurrentUser = Depends(get_current_user)) -> CurrentUser:
    """Dependency enforcing ``ROLE_ADMIN`` for admin endpoints."""
    if not user.has_role("ADMIN"):
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail="Admin role required")
    return user
