import logging
import ssl
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
    settings = get_settings()
    ssl_context: ssl.SSLContext | None = None
    if not settings.idp_verify_ssl:
        ssl_context = ssl.create_default_context()
        ssl_context.check_hostname = False
        ssl_context.verify_mode = ssl.CERT_NONE
    return PyJWKClient(settings.idp_jwks_url, ssl_context=ssl_context)


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
    print(f"print Authorization header: {header}")
    log.info("log Authorization header: %s", header)
    if not header or not header.startswith("Bearer "):
        log.info("No valid Authorization header found; returning anonymous user.")
        return CurrentUser()

    token = header[7:]
    log.info("Validating JWT: %s", token)
    settings = get_settings()
    log.info("JWT issuer: %s, audience: %s", settings.jwt_issuer, settings.jwt_audience)
    try:
        log.info("Fetching signing key from JWKS URL: %s", settings.idp_jwks_url)
        signing_key = _get_jwks_client().get_signing_key_from_jwt(token)
        claims = jwt.decode(
            token,
            signing_key.key,
            algorithms=["RS256"],
            issuer=settings.jwt_issuer,
            audience=settings.jwt_audience,
        )
        log.info("JWT validated successfully. Claims: %s", claims)
    except Exception as e:  # noqa: BLE001 — any failure (bad sig, expired, JWKS fetch) → anonymous
        log.info("JWT validation failed: %s", e)
        return CurrentUser()

    user_id = claims.get("sub") or "anonymous"
    # .NET maps ClaimTypes.Role → "role"; accept "roles" for test/legacy tokens
    role_claim_name = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
    raw_roles = claims.get("role") or claims.get("roles") or claims.get(role_claim_name)    
    roles = _normalize_roles(raw_roles)
    print(f"Authenticated user {user_id} with roles {roles}")
    print(f"Claims: {claims}")
    log.info("Authenticated user %s with roles %s", user_id, roles)
    return CurrentUser(user_id=user_id, roles=roles, authenticated=True)


def require_authenticated(user: CurrentUser = Depends(get_current_user)) -> CurrentUser:
    """Dependency enforcing a valid JWT — rejects anonymous/unauthenticated requests."""
    if not user.authenticated:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Authentication required")
    return user


def require_admin(user: CurrentUser = Depends(get_current_user)) -> CurrentUser:
    """Dependency enforcing ``ROLE_PlatformAdmin`` for admin endpoints."""
    log.debug("Checking admin role for user %s with roles %s", user.user_id, user.roles)
    if not user.authenticated:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Authentication required")
    if not user.has_role("PlatformAdmin"):
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail="PlatformAdmin role required")
    return user
