# Frontend Analysis — University Club Management Platform

> This document is a precise technical summary of the backend for the purpose of building a React frontend. It describes all features, the role system, business rules, data models, and API contracts.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Authentication & Token Flow](#authentication--token-flow)
3. [Role System](#role-system)
4. [Data Models](#data-models)
5. [Features by Role](#features-by-role)
6. [API Reference Summary](#api-reference-summary)
7. [Key Business Rules](#key-business-rules)
8. [Suggested React UI Structure](#suggested-react-ui-structure)

---

## Architecture Overview

The platform is made of three backend microservices behind a single **API Gateway** at `http://localhost:5000`:

| Gateway Prefix | Service | Purpose |
|---------------|---------|---------|
| `/identity/...` | IdentityProvider (.NET 9) | User registration, login, JWT issuance |
| `/clubs/...` | GestionClubs (.NET 9) | Clubs, members, events, adhesions, announcements |
| `/chatbot/...` | Chatbot (Python FastAPI) | AI-powered chatbot, FAQ management |

All API calls from the React frontend go to `http://localhost:5000`.

---

## Authentication & Token Flow

### How it works

1. User registers via `POST /identity/api/account/register` — no token required.
2. User logs in via `POST /identity/api/account/login` — receives `accessToken` + `refreshToken`.
3. Every protected request must include the header:
   ```
   Authorization: Bearer <accessToken>
   ```
4. When the `accessToken` expires, call `POST /identity/api/account/refresh-token` with header:
   ```
   REFRESH_TOKEN: <refreshToken>
   ```
   to get a new pair of tokens.

### Token response shape

```json
{
  "accessToken": "string",
  "refreshToken": "string",
  "accessTokenExpires": "2025-01-01T00:00:00Z",
  "refreshTokenExpires": "2025-01-01T00:00:00Z"
}
```

### JWT contents

The JWT claims include:
- `email` — user's email address
- `role` — one or more roles (see Role System below)

> **Frontend tip:** Decode the JWT on the client (e.g., with `jwt-decode`) to read the user's role(s) and conditionally render UI elements. Do NOT trust the decoded role for security — the server enforces authorization.

---

## Role System

There are **four platform-level roles**, embedded in the JWT:

| Role | Who | Level |
|------|-----|-------|
| `PlatformAdmin` | Platform-wide administrator | Highest |
| `ClubAdmin` | Administrator of at least one club | Club-scoped |
| `ClubMember` | Member of at least one club | Club-scoped |
| `Visitor` | Any authenticated user with no club role | Lowest authenticated |

### Role hierarchy (for authorization policies)

```
PlatformAdmin  →  full platform access
ClubAdmin      →  can do everything a ClubMember or Visitor can + admin actions
ClubMember     →  can do everything a Visitor can + member-only content
Visitor        →  any authenticated user
(none)         →  public, no login required
```

The backend policy definitions are:
- `Visitor` policy: accepts `Visitor`, `ClubMember`, or `ClubAdmin`
- `ClubMember` policy: accepts `ClubMember` or `ClubAdmin`
- `ClubAdmin` policy: accepts `ClubAdmin` only
- `PlatformAdmin` policy: accepts `PlatformAdmin` only

### How roles are assigned

1. **Initial role:** A newly registered user starts with no club role (effectively `Visitor` level once logged in — the `Visitor` role is assigned by the Identity Provider after registration).
2. **ClubAdmin via club creation:** When a `PlatformAdmin` creates a club, the designated email is promoted to `ClubAdmin` via a Kafka event (`UserPromotedToClubAdminEvent`). The IdentityProvider consumes this event and adds the `ClubAdmin` role to that user's account.
3. **ClubAdmin via post change:** When a `ClubAdmin` promotes a member to `President` post inside a club, that member is also promoted to the `ClubAdmin` role in the Identity Provider (via the same Kafka event).
4. **ClubMember:** When an adhesion (membership request) is accepted, the user gains the `ClubMember` role.

### Club-level admin check (critical for frontend)

Having the `ClubAdmin` JWT role **does not mean** the admin can manage any club. The backend performs a **secondary database check**: it verifies that the requesting user is a member of the **specific club** being managed with a post that is NOT `Member` (i.e., `President`, `VicePresident`, `Treasurer`, `Secretary`, or `HeadOfDepartment`).

**Consequence for the frontend:** Even if a user has the `ClubAdmin` role, they should only be shown admin controls for clubs where they hold a non-Member post. Fetch `/clubs/api/clubs/user-clubs` to determine which clubs the user belongs to and what post they hold, then use this to control UI visibility.

---

## Data Models

### User

```ts
interface User {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
}
```

### Club

```ts
interface Club {
  id: number;
  name: string;
  description: string;
  presidentMail: string | null;  // list endpoint
}

interface UserClub extends Club {
  userPost: string | null;  // user-clubs endpoint — the current user's post in this club
}
```

### Member

```ts
interface Member {
  id: number;
  clubName: string;
  postInClub: PostInClub;
  user: { email: string; firstName: string; lastName: string };
}

type PostInClub =
  | "Member"
  | "HeadOfDepartment"
  | "President"
  | "Secretary"
  | "Treasurer"
  | "VicePresident";
```

### Adhesion (Membership Request)

```ts
interface Adhesion {
  id: number;
  status: "Pending" | "Accepted" | "Refused";
  clubName: string;
  user: { email: string; firstName: string; lastName: string };
}
```

### Event

```ts
interface ClubEvent {
  id: number;
  title: string;
  description: string;
  location: string | null;
  startDate: string; // ISO 8601
  clubName: string;
}
```

### Announcement

```ts
interface Announcement {
  id: number;
  title: string;
  content: string;
  clubName: string;
}
```

### Paginated Response

All list endpoints return:

```ts
interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}
```

Query params: `pageNumber` (default 1), `pageSize` (default 10).

---

## Features by Role

### Unauthenticated user (no token)

| Feature | Available |
|---------|-----------|
| Register | Yes |
| Login | Yes |
| Browse clubs / events | **No** — requires at least `Visitor` role |

> Registration is the only action available without a token.

---

### Visitor (any authenticated user)

| Feature | Endpoint |
|---------|----------|
| Browse all clubs (with search/filter) | `GET /clubs/api/clubs` |
| Browse all public events (with tag filter) | `GET /clubs/api/events` |
| View a single event | `GET /clubs/api/events/{id}` |
| View events for a specific club | `GET /clubs/api/events/club-events/{clubId}` |
| View their own joined events | `GET /clubs/api/events/user-events` |
| Join an event | `PUT /clubs/api/events/{id}/join` |
| Leave an event | `PUT /clubs/api/events/{id}/leave` |
| Submit a membership request to a club | `POST /clubs/api/adhesions` |
| View their own membership requests | `GET /clubs/api/adhesions/myadhesions` |
| Use the chatbot | `POST /chatbot/api/chatbot/ask` |
| Browse FAQs | `GET /chatbot/api/chatbot/faqs` |

---

### ClubMember (accepted member of at least one club)

Inherits all `Visitor` permissions, plus:

| Feature | Endpoint |
|---------|----------|
| View a specific announcement | `GET /clubs/api/annoucements/{id}` |
| View their clubs (with their post) | `GET /clubs/api/clubs/user-clubs` |

---

### ClubAdmin (admin of at least one club)

Inherits all `ClubMember` permissions, plus:

> All admin actions are **club-scoped**: the backend checks that the requesting user is a non-Member post holder **in that specific club**.

| Feature | Endpoint |
|---------|----------|
| **Members** | |
| View all members of their club | `GET /clubs/api/members/club/{clubId}` |
| View a single member | `GET /clubs/api/members/{id}` |
| Change a member's post | `PUT /clubs/api/members/post` |
| Remove a member | `DELETE /clubs/api/members/{id}` |
| **Adhesions** | |
| View all membership requests for their club | `GET /clubs/api/adhesions/club/{clubId}` |
| View a single adhesion | `GET /clubs/api/adhesions/{id}` |
| Accept a membership request | `PUT /clubs/api/adhesions/{id}/accept` |
| Refuse a membership request | `PUT /clubs/api/adhesions/{id}/refuse` |
| Delete a membership request | `DELETE /clubs/api/adhesions/{id}` |
| **Events** | |
| Create an event | `POST /clubs/api/events` |
| Delete an event | `DELETE /clubs/api/events/{id}` |
| **Announcements** | |
| View announcements list for their club | `GET /clubs/api/annoucements/club/{clubId}` |
| Create an announcement | `POST /clubs/api/annoucements` |
| Delete an announcement | `DELETE /clubs/api/annoucements/{id}` |

---

### PlatformAdmin

| Feature | Endpoint |
|---------|----------|
| Create a new club (and assign its first admin) | `POST /clubs/api/clubs` |

> When a club is created, the `email` field in the request body designates who becomes the club's first admin. That user is automatically given the `ClubAdmin` role in the Identity Provider via Kafka.

---

## API Reference Summary

### Base URL: `http://localhost:5000`

#### Auth (IdentityProvider)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/identity/api/account/register` | None | Register new user |
| POST | `/identity/api/account/login` | None | Login, get tokens |
| POST | `/identity/api/account/refresh-token` | `REFRESH_TOKEN` header | Refresh access token |

#### Clubs

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/clubs/api/clubs` | Visitor | List all clubs |
| POST | `/clubs/api/clubs` | PlatformAdmin | Create a club |
| GET | `/clubs/api/clubs/user-clubs` | ClubMember | List user's clubs with their post |

#### Members

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/clubs/api/members/club/{clubId}` | ClubAdmin | List members of a club |
| GET | `/clubs/api/members/{id}` | ClubAdmin | Get single member |
| PUT | `/clubs/api/members/post` | ClubAdmin | Change member's post |
| DELETE | `/clubs/api/members/{id}` | ClubAdmin | Remove member from club |

#### Events

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/clubs/api/events` | Visitor | List all events (tag filter supported) |
| GET | `/clubs/api/events/{id}` | Visitor | Get single event |
| GET | `/clubs/api/events/user-events` | Visitor | Events the user joined |
| GET | `/clubs/api/events/club-events/{clubId}` | Visitor | Events of a specific club |
| POST | `/clubs/api/events` | ClubAdmin | Create event |
| DELETE | `/clubs/api/events/{id}` | ClubAdmin | Delete event |
| PUT | `/clubs/api/events/{id}/join` | Visitor | Join event |
| PUT | `/clubs/api/events/{id}/leave` | Visitor | Leave event |

#### Adhesions

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/clubs/api/adhesions/club/{clubId}` | ClubAdmin | List membership requests for a club |
| GET | `/clubs/api/adhesions/{id}` | ClubAdmin | Get single adhesion |
| GET | `/clubs/api/adhesions/myadhesions` | Visitor | User's own requests |
| POST | `/clubs/api/adhesions` | Visitor | Submit membership request |
| PUT | `/clubs/api/adhesions/{id}/accept` | ClubAdmin | Accept request |
| PUT | `/clubs/api/adhesions/{id}/refuse` | ClubAdmin | Refuse request |
| DELETE | `/clubs/api/adhesions/{id}` | ClubAdmin | Delete request |

#### Announcements

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/clubs/api/annoucements/club/{clubId}` | ClubAdmin | List club's announcements |
| GET | `/clubs/api/annoucements/{id}` | ClubMember | Get single announcement |
| POST | `/clubs/api/annoucements` | ClubAdmin | Create announcement |
| DELETE | `/clubs/api/annoucements/{id}` | ClubAdmin | Delete announcement |

#### Chatbot

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/chatbot/api/chatbot/ask` | Authenticated | Send message to chatbot |
| GET | `/chatbot/api/chatbot/faqs` | Authenticated | List FAQs |

---

## Key Business Rules

1. **Duplicate adhesion prevention:** A user cannot request to join a club they have already requested (even if refused). The backend throws an error. The frontend should check `myadhesions` before showing the "Join" button.

2. **Already-a-member check:** A user cannot request to join a club they are already a member of.

3. **Club-scoped admin access:** The `ClubAdmin` JWT role alone is not enough to manage a club. The user must hold a non-`Member` post in that specific club. This check happens server-side, so the frontend will receive a `401 Unauthorized` if violated.

4. **President promotion triggers ClubAdmin role:** Changing a member's post to `President` via `PUT /clubs/api/members/post` triggers a Kafka event that grants that user the `ClubAdmin` role in the Identity Provider. The user must re-login (or refresh token) to receive the updated role in their JWT.

5. **Club creation assigns first admin:** The `email` provided in `POST /clubs/api/clubs` receives the `ClubAdmin` role via Kafka. This user is automatically created as the club's first member with the `President` post.

6. **Announcements visibility:** Listing announcements for a club (`GET /clubs/api/annoucements/club/{clubId}`) requires `ClubAdmin`. Reading a single announcement (`GET /clubs/api/annoucements/{id}`) requires `ClubMember`. Non-members cannot read announcements.

7. **Events are public-browsable:** All authenticated users (`Visitor`+) can browse, join, and leave events regardless of club membership.

8. **Pagination is universal:** All list endpoints accept `pageNumber` and `pageSize` query parameters. Always handle paginated responses in the frontend.

---

## Suggested React UI Structure

```
src/
├── features/
│   ├── auth/
│   │   ├── LoginPage.tsx          # POST /identity/api/account/login
│   │   ├── RegisterPage.tsx       # POST /identity/api/account/register
│   │   └── authSlice.ts           # Store accessToken, refreshToken, decoded role
│   │
│   ├── clubs/
│   │   ├── ClubListPage.tsx       # GET /clubs/api/clubs  (Visitor+)
│   │   ├── ClubDetailPage.tsx     # Shows events, announcements for a club
│   │   ├── MyClubsPage.tsx        # GET /clubs/api/clubs/user-clubs  (ClubMember+)
│   │   └── CreateClubForm.tsx     # POST /clubs/api/clubs  (PlatformAdmin only)
│   │
│   ├── members/
│   │   └── ClubMembersPage.tsx    # GET /clubs/api/members/club/{clubId}  (ClubAdmin only)
│   │                              # Includes: change post, remove member
│   │
│   ├── adhesions/
│   │   ├── MyAdhesionsPage.tsx    # GET /clubs/api/adhesions/myadhesions  (Visitor+)
│   │   └── ClubAdhesionsPage.tsx  # GET /clubs/api/adhesions/club/{clubId}  (ClubAdmin only)
│   │                              # Includes: accept, refuse, delete
│   │
│   ├── events/
│   │   ├── EventListPage.tsx      # GET /clubs/api/events  (Visitor+)
│   │   ├── EventDetailPage.tsx    # GET /clubs/api/events/{id} + join/leave
│   │   ├── MyEventsPage.tsx       # GET /clubs/api/events/user-events
│   │   └── CreateEventForm.tsx    # POST /clubs/api/events  (ClubAdmin only)
│   │
│   ├── announcements/
│   │   ├── AnnouncementDetail.tsx # GET /clubs/api/annoucements/{id}  (ClubMember+)
│   │   └── ClubAnnouncementsPage.tsx # GET /clubs/api/annoucements/club/{clubId}  (ClubAdmin)
│   │                              # Includes: create, delete
│   │
│   └── chatbot/
│       └── ChatbotWidget.tsx      # POST /chatbot/api/chatbot/ask  (Authenticated)
│
├── components/
│   ├── ProtectedRoute.tsx         # Redirect if no token or insufficient role
│   └── RoleGuard.tsx              # Show/hide UI elements based on role
│
└── lib/
    ├── apiClient.ts               # Axios instance with token injection + refresh logic
    └── auth.ts                    # JWT decode helper (jwt-decode)
```

### Role-based route protection

```tsx
// Example: ClubAdmin-only route
<Route
  path="/clubs/:clubId/members"
  element={
    <ProtectedRoute requiredRole="ClubAdmin">
      <ClubMembersPage />
    </ProtectedRoute>
  }
/>
```

### Token management

- Store `accessToken` and `refreshToken` in `localStorage` or an in-memory store.
- Set up an Axios interceptor to automatically attach the `Authorization: Bearer` header.
- On 401 responses, attempt a token refresh with `POST /identity/api/account/refresh-token` using the `REFRESH_TOKEN` header, then retry the original request.
- After logout, clear both tokens.

### Determining which clubs a user can administer

```ts
// After login, fetch user's clubs
const { items } = await api.get('/clubs/api/clubs/user-clubs');

// Admin for a club = member with a post that is NOT "Member"
const adminClubIds = items
  .filter(c => c.userPost && c.userPost !== 'Member')
  .map(c => c.id);
```

Use `adminClubIds` to conditionally show admin panels (member management, adhesion review, announcements, event creation) per club.
