import logging
from dataclasses import dataclass, field

import jwt
from fastapi import Depends, HTTPException, Request, status

from app.config import get_settings

log = logging.getLogger(__name__)


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
    if not isinstance(raw, list):
        return []
    roles: list[str] = []
    for r in raw:
        s = str(r)
        roles.append(s if s.startswith("ROLE_") else f"ROLE_{s}")
    return roles


def get_current_user(request: Request) -> CurrentUser:
    """Stateless JWT validation. Absent/invalid tokens yield an anonymous user
    (the request continues unauthenticated), matching ``JwtAuthFilter``."""
    header = request.headers.get("Authorization")
    if not header or not header.startswith("Bearer "):
        return CurrentUser()

    token = header[7:]
    try:
        claims = jwt.decode(
            token,
            get_settings().jwt_secret,
            algorithms=["HS256"],
        )
    except jwt.PyJWTError as e:  # noqa: BLE001 - mirror "Invalid JWT" debug log
        log.debug("Invalid JWT: %s", e)
        return CurrentUser()

    user_id = claims.get("sub") or "anonymous"
    roles = _normalize_roles(claims.get("roles"))
    return CurrentUser(user_id=user_id, roles=roles, authenticated=True)


def require_admin(user: CurrentUser = Depends(get_current_user)) -> CurrentUser:
    """Dependency enforcing ``ROLE_ADMIN`` for admin endpoints."""
    if not user.has_role("ADMIN"):
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail="Admin role required")
    return user
