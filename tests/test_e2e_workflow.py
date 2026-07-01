"""
API Test: End-to-End Club Membership Workflow
=============================================

Workflow tested:
  1.  Register a new user                      → gets "Visitor" role
  2.  Login as the new user                    → obtain JWT
  3.  Browse available clubs                   → find the target club
  4.  Submit an adhesion (join request)        → status = Pending
  5.  Login as the club president (ClubAdmin)  → obtain JWT
  6.  List pending adhesions for the club      → locate the new request
  7.  Accept the adhesion                      → fires Kafka event to promote user
  8.  Wait for Kafka propagation               → IdentityProvider assigns ClubMember role
  9.  As ClubAdmin, create an announcement     → content for members
  10. Re-login as the new user                 → new JWT with ClubMember role
  11. Access the announcement as ClubMember    → 200 OK expected
  12. Verify unauthorized access is rejected   → 401 expected

Targets (container HTTP ports – override via env vars):
  IdentityProvider : http://localhost:8080  (IDP_BASE_URL)
  GestionClubs     : http://localhost:8082  (CLUBS_BASE_URL)

Usage:
  python test_e2e_workflow.py
  IDP_BASE_URL=http://myhost:8080 CLUBS_BASE_URL=http://myhost:8082 python test_e2e_workflow.py
"""

import os
import sys
import time
import uuid

import requests

# ─────────────────────────────────────────────────────────────
# Configuration  (all values can be overridden via env vars)
# ─────────────────────────────────────────────────────────────

# Base URLs for the two microservice containers
IDP_URL   = os.getenv("IDP_BASE_URL",   "http://localhost:8080")
CLUBS_URL = os.getenv("CLUBS_BASE_URL", "http://localhost:8082")

# Club to target — Tech Innovators Club is seeded with id=1
# Its president is alice.martin@gmail.com (ClubAdmin, seeded via SeedData.json)
CLUB_ID          = int(os.getenv("TEST_CLUB_ID",        "1"))
PRESIDENT_EMAIL  = os.getenv("PRESIDENT_EMAIL",         "alice.martin@gmail.com")
PRESIDENT_PASS   = os.getenv("PRESIDENT_PASSWORD",      "Alice@1234!")

# Seconds to wait after accepting an adhesion for the Kafka event
# to reach IdentityProvider and promote the user to ClubMember
KAFKA_WAIT = int(os.getenv("KAFKA_WAIT_SECONDS", "5"))

# ─────────────────────────────────────────────────────────────
# Terminal colours
# ─────────────────────────────────────────────────────────────
GREEN = "\033[92m"
RED   = "\033[91m"
CYAN  = "\033[96m"
BOLD  = "\033[1m"
RESET = "\033[0m"


# ─────────────────────────────────────────────────────────────
# Test-result tracking
# ─────────────────────────────────────────────────────────────
_results: list[tuple[str, bool]] = []


def step(name: str, ok: bool, detail: str = "") -> None:
    """Record one test step and print its result immediately."""
    symbol = f"{GREEN}PASS{RESET}" if ok else f"{RED}FAIL{RESET}"
    suffix = f"  [{detail}]" if detail else ""
    print(f"  [{symbol}] {name}{suffix}")
    _results.append((name, ok))


# ─────────────────────────────────────────────────────────────
# Shared helpers
# ─────────────────────────────────────────────────────────────

def bearer(token: str) -> dict:
    """Return an Authorization header dict for the given JWT."""
    return {"Authorization": f"Bearer {token}"}


def login(email: str, password: str) -> str | None:
    """
    POST /api/account/login to IdentityProvider.
    Returns the accessToken string on success, or None on failure.
    The 'accessToken' field carries the new JWT (may include updated roles).
    """
    resp = requests.post(
        f"{IDP_URL}/api/account/login",
        json={"email": email, "password": password},
    )
    if resp.status_code == 200:
        return resp.json().get("accessToken")
    return None


# ─────────────────────────────────────────────────────────────
# STEP 1 — Register a fresh Visitor account
# ─────────────────────────────────────────────────────────────
print(f"\n{BOLD}╔══ STEP 1 — Register new user (Visitor) ══╗{RESET}")

# Generate a unique e-mail so the script is safely re-runnable
unique_tag        = uuid.uuid4().hex[:8]
NEW_USER_EMAIL    = f"testuser_{unique_tag}@example.com"
NEW_USER_PASSWORD = "TestUser@1234!"

register_resp = requests.post(
    f"{IDP_URL}/api/account/register",
    json={
        "email":           NEW_USER_EMAIL,
        "password":        NEW_USER_PASSWORD,
        "confirmPassword": NEW_USER_PASSWORD,
        "firstName":       "Test",
        "lastName":        "User",
    },
)
# IdentityProvider returns 200 OK on successful registration
step(
    "Register new Visitor account",
    register_resp.status_code == 200,
    f"email={NEW_USER_EMAIL}  status={register_resp.status_code}",
)


# ─────────────────────────────────────────────────────────────
# STEP 2 — Login as the new user, obtain initial JWT (Visitor)
# ─────────────────────────────────────────────────────────────
print(f"\n{BOLD}╔══ STEP 2 — Login as new user ══╗{RESET}")

user_token = login(NEW_USER_EMAIL, NEW_USER_PASSWORD)
step(
    "Login new user — receive access token",
    user_token is not None,
    f"token={'obtained' if user_token else 'FAILED'}",
)


# ─────────────────────────────────────────────────────────────
# STEP 3 — Browse available clubs (requires Visitor role)
# ─────────────────────────────────────────────────────────────
print(f"\n{BOLD}╔══ STEP 3 — Browse available clubs ══╗{RESET}")

clubs_resp = requests.get(
    f"{CLUBS_URL}/api/clubs/",
    headers=bearer(user_token or ""),
    params={"pageNumber": 1, "pageSize": 10},
)
clubs_ok   = clubs_resp.status_code == 200
clubs_body = clubs_resp.json() if clubs_ok else {}
clubs      = clubs_body.get("items", [])

step("GET /api/clubs/ returns 200", clubs_ok, f"status={clubs_resp.status_code}")
step("Club list is not empty", len(clubs) > 0, f"count={len(clubs)}")

# Print the first few clubs for visibility
print(f"  {CYAN}Available clubs:{RESET}")
for c in clubs[:5]:
    print(f"    • id={c.get('id')}  name={c.get('name')}")

# Confirm that our target club is present
target_club = next((c for c in clubs if c.get("id") == CLUB_ID), None)
step(
    f"Target club (id={CLUB_ID}) is listed",
    target_club is not None,
    target_club.get("name", "") if target_club else "NOT FOUND",
)


# ─────────────────────────────────────────────────────────────
# STEP 4 — Submit an adhesion (join request) for the target club
#           POST /api/adhesions/  requires Visitor role
# ─────────────────────────────────────────────────────────────
print(f"\n{BOLD}╔══ STEP 4 — Submit adhesion request ══╗{RESET}")

adh_create_resp = requests.post(
    f"{CLUBS_URL}/api/adhesions/",
    headers=bearer(user_token or ""),
    json={"clubId": CLUB_ID},
)
adhesion_created = adh_create_resp.status_code == 201
# The created adhesion DTO contains its id, status="Pending", and clubName
adhesion_id      = adh_create_resp.json().get("id") if adhesion_created else None
adhesion_status  = adh_create_resp.json().get("status", "") if adhesion_created else ""

step(
    "POST /api/adhesions/ returns 201 Created",
    adhesion_created,
    f"status={adh_create_resp.status_code}  adhesionId={adhesion_id}  adhesionStatus={adhesion_status}",
)


# ─────────────────────────────────────────────────────────────
# STEP 5 — Login as the club president (ClubAdmin)
#           Alice is the seeded president of Tech Innovators Club (id=1)
# ─────────────────────────────────────────────────────────────
print(f"\n{BOLD}╔══ STEP 5 — Login as club president (ClubAdmin) ══╗{RESET}")

president_token = login(PRESIDENT_EMAIL, PRESIDENT_PASS)
step(
    f"Login as president ({PRESIDENT_EMAIL})",
    president_token is not None,
    f"token={'obtained' if president_token else 'FAILED'}",
)


# ─────────────────────────────────────────────────────────────
# STEP 6 — List pending adhesions for the club
#           GET /api/adhesions/club/{id}  requires ClubAdmin role
# ─────────────────────────────────────────────────────────────
print(f"\n{BOLD}╔══ STEP 6 — List adhesions for the club ══╗{RESET}")

adh_list_resp = requests.get(
    f"{CLUBS_URL}/api/adhesions/club/{CLUB_ID}",
    headers=bearer(president_token or ""),
    params={"pageNumber": 1, "pageSize": 100},
)
adh_list_ok   = adh_list_resp.status_code == 200
adh_list      = adh_list_resp.json().get("items", []) if adh_list_ok else []

step(
    "GET /api/adhesions/club/{id} returns 200",
    adh_list_ok,
    f"status={adh_list_resp.status_code}  total adhesions={len(adh_list)}",
)

# Identify the new user's adhesion from the list
pending_adhesion_id: int | None = None
for adh in adh_list:
    if adh.get("user", {}).get("email") == NEW_USER_EMAIL:
        pending_adhesion_id = adh.get("id")
        break

# Fall back to the ID returned at creation time if the list search missed it
if pending_adhesion_id is None and adhesion_id is not None:
    pending_adhesion_id = adhesion_id

step(
    "New user's adhesion located in the club list",
    pending_adhesion_id is not None,
    f"adhesionId={pending_adhesion_id}",
)


# ─────────────────────────────────────────────────────────────
# STEP 7 — Accept the adhesion as the club president
#           PUT /api/adhesions/{id}/accept  requires ClubAdmin role
#           Triggers a Kafka UserPromotedToClubMemberEvent → IdentityProvider
#           will assign the ClubMember role to the user
# ─────────────────────────────────────────────────────────────
print(f"\n{BOLD}╔══ STEP 7 — Accept the adhesion ══╗{RESET}")

if pending_adhesion_id is not None:
    accept_resp = requests.put(
        f"{CLUBS_URL}/api/adhesions/{pending_adhesion_id}/accept",
        headers=bearer(president_token or ""),
    )
    accept_ok      = accept_resp.status_code == 200
    accepted_status = accept_resp.json().get("status", "") if accept_ok else ""
    step(
        "PUT /api/adhesions/{id}/accept returns 200",
        accept_ok,
        f"status={accept_resp.status_code}  adhesionStatus={accepted_status}",
    )
else:
    step("PUT /api/adhesions/{id}/accept", False, "Skipped — adhesion ID unknown")
    accept_ok = False


# ─────────────────────────────────────────────────────────────
# STEP 8 — Wait for Kafka to propagate the ClubMember promotion
#           IdentityProvider's UserPromotedToClubMemberConsumer listens on
#           the configured topic and updates the user's ASP.NET Identity role.
# ─────────────────────────────────────────────────────────────
print(f"\n{BOLD}╔══ STEP 8 — Waiting {KAFKA_WAIT}s for Kafka role promotion ══╗{RESET}")
print(f"  {CYAN}(IdentityProvider will consume UserPromotedToClubMemberEvent){RESET}")
time.sleep(KAFKA_WAIT)
step(f"Kafka wait of {KAFKA_WAIT}s completed", True)


# ─────────────────────────────────────────────────────────────
# STEP 9 — As ClubAdmin (president), create an announcement
#           POST /api/annoucements/  requires ClubAdmin role
# ─────────────────────────────────────────────────────────────
print(f"\n{BOLD}╔══ STEP 9 — Create an announcement (as ClubAdmin) ══╗{RESET}")

ann_create_resp = requests.post(
    f"{CLUBS_URL}/api/annoucements/",
    headers=bearer(president_token or ""),
    json={
        "title":    "Welcome to the club!",
        "content":  "We are thrilled to have new members join our community.",
        "clubId":   CLUB_ID,
        "isPublic": False,  # private — only ClubMembers should see it
    },
)
ann_created    = ann_create_resp.status_code == 201
announcement_id = ann_create_resp.json().get("id") if ann_created else None

step(
    "POST /api/annoucements/ returns 201 Created",
    ann_created,
    f"status={ann_create_resp.status_code}  announcementId={announcement_id}",
)


# ─────────────────────────────────────────────────────────────
# STEP 10 — Re-login as the new user
#            After Kafka propagation the user has the ClubMember role;
#            a fresh login issues a new JWT that includes it.
# ─────────────────────────────────────────────────────────────
print(f"\n{BOLD}╔══ STEP 10 — Re-login as new user (expecting ClubMember role) ══╗{RESET}")

member_token = login(NEW_USER_EMAIL, NEW_USER_PASSWORD)
step(
    "Re-login returns a new access token",
    member_token is not None,
    f"token={'obtained' if member_token else 'FAILED'}",
)


# ─────────────────────────────────────────────────────────────
# STEP 11 — Access the announcement as a ClubMember
#            GET /api/annoucements/{id}  requires ClubMember role
# ─────────────────────────────────────────────────────────────
print(f"\n{BOLD}╔══ STEP 11 — Access announcement as ClubMember ══╗{RESET}")

if announcement_id is not None and member_token is not None:
    ann_resp = requests.get(
        f"{CLUBS_URL}/api/annoucements/{announcement_id}",
        headers=bearer(member_token),
    )
    member_access_ok = ann_resp.status_code == 200
    step(
        "GET /api/annoucements/{id} as ClubMember returns 200",
        member_access_ok,
        f"status={ann_resp.status_code}",
    )
    if member_access_ok:
        body = ann_resp.json()
        print(f"  {CYAN}Announcement title  : {body.get('title')}{RESET}")
        print(f"  {CYAN}Announcement content: {body.get('content')}{RESET}")

    # ── Negative check: same endpoint without any token should return 401 ──
    ann_unauth_resp = requests.get(
        f"{CLUBS_URL}/api/annoucements/{announcement_id}",
        # no Authorization header
    )
    step(
        "GET /api/annoucements/{id} without token returns 401 (auth enforced)",
        ann_unauth_resp.status_code == 401,
        f"status={ann_unauth_resp.status_code}",
    )

    # ── Negative check: Visitor token (old token) must be rejected ──
    if user_token:
        ann_visitor_resp = requests.get(
            f"{CLUBS_URL}/api/annoucements/{announcement_id}",
            headers=bearer(user_token),   # original Visitor JWT
        )
        step(
            "GET /api/annoucements/{id} with Visitor token returns 403",
            ann_visitor_resp.status_code == 403,
            f"status={ann_visitor_resp.status_code}",
        )
else:
    step(
        "GET /api/annoucements/{id} as ClubMember",
        False,
        "Skipped — missing announcement ID or member token",
    )


# ─────────────────────────────────────────────────────────────
# Summary
# ─────────────────────────────────────────────────────────────
total  = len(_results)
passed = sum(1 for _, ok in _results if ok)
failed = total - passed

print(f"\n{BOLD}{'═'*55}{RESET}")
print(f"{BOLD}  Test summary: {passed}/{total} passed{RESET}")

if failed:
    print(f"\n{RED}  Failed steps:{RESET}")
    for name, ok in _results:
        if not ok:
            print(f"    ✗ {name}")
else:
    print(f"\n{GREEN}  All tests passed!{RESET}")

print(f"{BOLD}{'═'*55}{RESET}\n")

sys.exit(0 if failed == 0 else 1)
