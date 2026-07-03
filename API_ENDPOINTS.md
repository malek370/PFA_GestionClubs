# API Endpoints Reference

All requests go through the **API Gateway** at `http://localhost:5000`.

**Gateway routing prefixes:**
| Prefix | Target Service |
|--------|---------------|
| `/clubs/...` | GestionClubs (club, member, event, adhesion, announcement management) |
| `/identity/...` | IdentityProvider (auth, JWT, user registration) |
| `/chatbot/...` | Chatbot (NLP chatbot, FAQs) |

---

## Authentication

JWT Bearer token is required for protected endpoints.

**Header:** `Authorization: Bearer <accessToken>`

**Roles:**
- `PlatformAdmin` — platform-level administrator
- `ClubAdmin` — administrator of a specific club
- `ClubMember` — member of at least one club
- `Visitor` (authenticated user with no club role)
- None — no authentication required

---

## Identity Provider

Base gateway path: `http://localhost:5000/identity`

### POST `/identity/api/account/register`

Register a new user account.

**Auth:** None

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "string",
  "confirmPassword": "string",
  "firstName": "string",
  "lastName": "string"
}
```

**Response:** `200 OK`

---

### POST `/identity/api/account/login`

Authenticate and receive JWT tokens.

**Auth:** None

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "string"
}
```

**Response:** `200 OK`
```json
{
  "accessToken": "string",
  "refreshToken": "string",
  "accessTokenExpires": "2025-01-01T00:00:00Z",
  "refreshTokenExpires": "2025-01-01T00:00:00Z"
}
```

---

### POST `/identity/api/account/refresh-token`

Obtain a new access token using a refresh token.

**Auth:** None

**Request Header:** `REFRESH_TOKEN: <refreshToken>`

**Response:** `200 OK`
```json
{
  "accessToken": "string",
  "refreshToken": "string",
  "accessTokenExpires": "2025-01-01T00:00:00Z",
  "refreshTokenExpires": "2025-01-01T00:00:00Z"
}
```

---

### GET `/identity/.well-known/openid-configuration`

OpenID Connect discovery document.

**Auth:** None

**Response:** `200 OK`
```json
{
  "issuer": "string",
  "jwks_uri": "string",
  "token_endpoint": "string"
}
```

---

### GET `/identity/.well-known/jwks`

RSA public key for JWT verification (JWKS format).

**Auth:** None

**Response:** `200 OK`
```json
{
  "keys": [{ "kty": "RSA", "use": "sig", "kid": "string", "n": "string", "e": "string" }]
}
```

---

## Clubs

Base gateway path: `http://localhost:5000/clubs/api/clubs`

### GET `/clubs/api/clubs`

List all clubs with optional filtering and pagination.

**Auth:** `Visitor`

**Query Parameters:**
| Param | Type | Required |
|-------|------|----------|
| `name` | string | No |
| `description` | string | No |
| `pageNumber` | int | No |
| `pageSize` | int | No |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": 1,
      "name": "string",
      "description": "string",
      "presidentMail": "string or null"
    }
  ],
  "totalCount": 0,
  "pageNumber": 1,
  "pageSize": 10
}
```

---

### POST `/clubs/api/clubs`

Create a new club.

**Auth:** `PlatformAdmin`

**Request Body:**
```json
{
  "name": "string (3–100 chars)",
  "description": "string (3–100 chars)",
  "email": "admin@example.com",
  "documents": ["string"]
}
```

**Response:** `201 Created`

---

### GET `/clubs/api/clubs/user-clubs`

Get clubs the authenticated user belongs to.

**Auth:** `ClubMember`

**Query Parameters:**
| Param | Type | Required |
|-------|------|----------|
| `pageNumber` | int | No |
| `pageSize` | int | No |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": 1,
      "name": "string",
      "description": "string",
      "presidentMail": "string or null",
      "userPost": "string or null"
    }
  ],
  "totalCount": 0,
  "pageNumber": 1,
  "pageSize": 10
}
```

---

## Members

Base gateway path: `http://localhost:5000/clubs/api/members`

### GET `/clubs/api/members/club/{clubId}`

Get all members of a specific club.

**Auth:** `ClubAdmin`

**Route Parameters:** `clubId` (int)

**Query Parameters:**
| Param | Type | Required |
|-------|------|----------|
| `pageNumber` | int | No |
| `pageSize` | int | No |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": 1,
      "clubName": "string",
      "postInClub": "string",
      "user": {
        "email": "user@example.com",
        "firstName": "string",
        "lastName": "string"
      }
    }
  ],
  "totalCount": 0,
  "pageNumber": 1,
  "pageSize": 10
}
```

---

### GET `/clubs/api/members/{id}`

Get a single member by ID.

**Auth:** `ClubAdmin`

**Route Parameters:** `id` (int)

**Response:** `200 OK`
```json
{
  "id": 1,
  "clubName": "string",
  "postInClub": "string",
  "user": {
    "email": "user@example.com",
    "firstName": "string",
    "lastName": "string"
  }
}
```

`404 Not Found` if the member does not exist.

---

### PUT `/clubs/api/members/post`

Update a member's post/role within their club.

**Auth:** `ClubAdmin`

**Request Body:**
```json
{
  "memberId": 1,
  "newPost": "President | VicePresident | Treasurer | Secretary | Member"
}
```

**Response:** `200 OK`
```json
{
  "id": 1,
  "clubName": "string",
  "postInClub": "string",
  "user": {
    "email": "user@example.com",
    "firstName": "string",
    "lastName": "string"
  }
}
```

---

### DELETE `/clubs/api/members/{id}`

Remove a member from a club.

**Auth:** `ClubAdmin`

**Route Parameters:** `id` (int)

**Response:** `200 OK` or `404 Not Found`

---

## Events

Base gateway path: `http://localhost:5000/clubs/api/events`

### GET `/clubs/api/events`

List all events with optional tag filtering and pagination.

**Auth:** `Visitor`

**Query Parameters:**
| Param | Type | Required | Notes |
|-------|------|----------|-------|
| `Tags` | string | No | Semicolon-separated list of tags |
| `pageNumber` | int | No | |
| `pageSize` | int | No | |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": 1,
      "title": "string",
      "description": "string",
      "location": "string or null",
      "startDate": "2025-01-01T00:00:00Z",
      "clubName": "string"
    }
  ],
  "totalCount": 0,
  "pageNumber": 1,
  "pageSize": 10
}
```

---

### POST `/clubs/api/events`

Create a new event.

**Auth:** `ClubAdmin`

**Request Body:**
```json
{
  "clubId": 1,
  "title": "string",
  "description": "string",
  "isPublic": false,
  "location": "string (optional)",
  "startDate": "2025-01-01T00:00:00Z",
  "tags": ["string"]
}
```

**Response:** `201 Created`
```json
{
  "id": 1,
  "title": "string",
  "description": "string",
  "location": "string or null",
  "startDate": "2025-01-01T00:00:00Z",
  "clubName": "string"
}
```

---

### GET `/clubs/api/events/{id}`

Get a single event by ID.

**Auth:** `Visitor`

**Route Parameters:** `id` (int)

**Response:** `200 OK`
```json
{
  "id": 1,
  "title": "string",
  "description": "string",
  "location": "string or null",
  "startDate": "2025-01-01T00:00:00Z",
  "clubName": "string"
}
```

`404 Not Found` if the event does not exist.

---

### DELETE `/clubs/api/events/{id}`

Delete an event.

**Auth:** `ClubAdmin`

**Route Parameters:** `id` (int)

**Response:** `204 No Content` or `404 Not Found`

---

### PUT `/clubs/api/events/{id}/join`

Join an event.

**Auth:** `Visitor`

**Route Parameters:** `id` (int)

**Response:** `200 OK`

---

### PUT `/clubs/api/events/{id}/leave`

Leave an event.

**Auth:** `Visitor`

**Route Parameters:** `id` (int)

**Response:** `200 OK`

---

### GET `/clubs/api/events/user-events`

Get events the authenticated user has joined.

**Auth:** `Visitor`

**Query Parameters:**
| Param | Type | Required |
|-------|------|----------|
| `pageNumber` | int | No |
| `pageSize` | int | No |

**Response:** `200 OK` — paginated list of `GetEventDTO`

---

### GET `/clubs/api/events/club-events/{clubId}`

Get all events belonging to a specific club.

**Auth:** `Visitor`

**Route Parameters:** `clubId` (int)

**Query Parameters:**
| Param | Type | Required |
|-------|------|----------|
| `pageNumber` | int | No |
| `pageSize` | int | No |

**Response:** `200 OK` — paginated list of `GetEventDTO`

---

## Adhesions (Membership Requests)

Base gateway path: `http://localhost:5000/clubs/api/adhesions`

### GET `/clubs/api/adhesions/club/{clubId}`

Get all membership requests for a specific club.

**Auth:** `ClubAdmin`

**Route Parameters:** `clubId` (int)

**Query Parameters:**
| Param | Type | Required |
|-------|------|----------|
| `pageNumber` | int | No |
| `pageSize` | int | No |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": 1,
      "status": "Pending | Accepted | Refused",
      "clubName": "string",
      "user": {
        "email": "user@example.com",
        "firstName": "string",
        "lastName": "string"
      }
    }
  ],
  "totalCount": 0,
  "pageNumber": 1,
  "pageSize": 10
}
```

---

### GET `/clubs/api/adhesions/{id}`

Get a single membership request by ID.

**Auth:** `ClubAdmin`

**Route Parameters:** `id` (int)

**Response:** `200 OK`
```json
{
  "id": 1,
  "status": "Pending | Accepted | Refused",
  "clubName": "string",
  "user": {
    "email": "user@example.com",
    "firstName": "string",
    "lastName": "string"
  }
}
```

`404 Not Found` if the adhesion does not exist.

---

### POST `/clubs/api/adhesions`

Submit a membership request to a club.

**Auth:** `Visitor`

**Request Body:**
```json
{
  "clubId": 1
}
```

**Response:** `201 Created`
```json
{
  "id": 1,
  "status": "Pending",
  "clubName": "string",
  "user": {
    "email": "user@example.com",
    "firstName": "string",
    "lastName": "string"
  }
}
```

---

### PUT `/clubs/api/adhesions/{id}/accept`

Accept a membership request.

**Auth:** `ClubAdmin`

**Route Parameters:** `id` (int)

**Response:** `200 OK`
```json
{
  "id": 1,
  "status": "Accepted",
  "clubName": "string",
  "user": { "email": "user@example.com", "firstName": "string", "lastName": "string" }
}
```

---

### PUT `/clubs/api/adhesions/{id}/refuse`

Refuse a membership request.

**Auth:** `ClubAdmin`

**Route Parameters:** `id` (int)

**Response:** `200 OK`
```json
{
  "id": 1,
  "status": "Refused",
  "clubName": "string",
  "user": { "email": "user@example.com", "firstName": "string", "lastName": "string" }
}
```

---

### DELETE `/clubs/api/adhesions/{id}`

Delete a membership request.

**Auth:** `ClubAdmin`

**Route Parameters:** `id` (int)

**Response:** `204 No Content` or `404 Not Found`

---

### GET `/clubs/api/adhesions/myadhesions`

Get the authenticated user's own membership requests.

**Auth:** `Visitor`

**Query Parameters:**
| Param | Type | Required |
|-------|------|----------|
| `pageNumber` | int | No |
| `pageSize` | int | No |

**Response:** `200 OK` — paginated list of `GetAdhesionDTO`

---

## Announcements

Base gateway path: `http://localhost:5000/clubs/api/annoucements`

### GET `/clubs/api/annoucements/club/{clubId}`

Get all announcements for a specific club.

**Auth:** `ClubAdmin`

**Route Parameters:** `clubId` (int)

**Query Parameters:**
| Param | Type | Required |
|-------|------|----------|
| `pageNumber` | int | No |
| `pageSize` | int | No |

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": 1,
      "title": "string",
      "content": "string",
      "clubName": "string"
    }
  ],
  "totalCount": 0,
  "pageNumber": 1,
  "pageSize": 10
}
```

---

### GET `/clubs/api/annoucements/{id}`

Get a single announcement by ID.

**Auth:** `ClubMember`

**Route Parameters:** `id` (int)

**Response:** `200 OK`
```json
{
  "id": 1,
  "title": "string",
  "content": "string",
  "clubName": "string"
}
```

`404 Not Found` if the announcement does not exist.

---

### POST `/clubs/api/annoucements`

Create a new announcement.

**Auth:** `ClubAdmin`

**Request Body:**
```json
{
  "title": "string",
  "content": "string",
  "clubId": 1,
  "isPublic": false
}
```

**Response:** `201 Created`
```json
{
  "id": 1,
  "title": "string",
  "content": "string",
  "clubName": "string"
}
```

---

### DELETE `/clubs/api/annoucements/{id}`

Delete an announcement.

**Auth:** `ClubAdmin`

**Route Parameters:** `id` (int)

**Response:** `204 No Content`

---

## Chatbot

Base gateway path: `http://localhost:5000/chatbot`

### POST `/chatbot/api/chatbot/ask`

Send a message to the chatbot and receive an AI-generated answer.

**Auth:** Authenticated

**Request Body:**
```json
{
  "message": "string (optional)",
  "context": "string (optional)"
}
```

**Response:** `200 OK`
```json
{
  "answer": "string",
  "suggestedActions": [
    { "label": "string", "value": "string" }
  ],
  "escalate": false
}
```

---

### GET `/chatbot/api/chatbot/faqs`

List all FAQ entries.

**Auth:** Authenticated

**Response:** `200 OK`
```json
[
  {
    "id": "uuid",
    "question": "string",
    "answer": "string",
    "category": "string or null",
    "created_at": "2025-01-01T00:00:00Z"
  }
]
```

---

### POST `/chatbot/api/chatbot/faqs`

Create a new FAQ entry.

**Auth:** Admin

**Request Body:**
```json
{
  "question": "string (1–1000 chars)",
  "answer": "string (1–5000 chars)",
  "category": "string (optional, max 100 chars)"
}
```

**Response:** `201 Created`
```json
{
  "id": "uuid",
  "question": "string",
  "answer": "string",
  "category": "string or null",
  "created_at": "2025-01-01T00:00:00Z"
}
```

---

### GET `/chatbot/api/chatbot/logs`

List all chatbot conversation logs.

**Auth:** Admin

**Response:** `200 OK`
```json
[
  {
    "id": "uuid",
    "user_id": "string or null",
    "session_id": "string or null",
    "user_message": "string",
    "bot_answer": "string",
    "escalated": false,
    "feedback": "string or null",
    "created_at": "2025-01-01T00:00:00Z"
  }
]
```

---

## Quick Reference

| Method | Gateway URL | Auth | Description |
|--------|------------|------|-------------|
| POST | `/identity/api/account/register` | None | Register user |
| POST | `/identity/api/account/login` | None | Login, get tokens |
| POST | `/identity/api/account/refresh-token` | None | Refresh access token |
| GET | `/identity/.well-known/openid-configuration` | None | OIDC discovery |
| GET | `/identity/.well-known/jwks` | None | JWKS public keys |
| GET | `/clubs/api/clubs` | Visitor | List clubs |
| POST | `/clubs/api/clubs` | PlatformAdmin | Create club |
| GET | `/clubs/api/clubs/user-clubs` | ClubMember | My clubs |
| GET | `/clubs/api/members/club/{clubId}` | ClubAdmin | List club members |
| GET | `/clubs/api/members/{id}` | ClubAdmin | Get member |
| PUT | `/clubs/api/members/post` | ClubAdmin | Update member role |
| DELETE | `/clubs/api/members/{id}` | ClubAdmin | Remove member |
| GET | `/clubs/api/events` | Visitor | List events |
| POST | `/clubs/api/events` | ClubAdmin | Create event |
| GET | `/clubs/api/events/{id}` | Visitor | Get event |
| DELETE | `/clubs/api/events/{id}` | ClubAdmin | Delete event |
| PUT | `/clubs/api/events/{id}/join` | Visitor | Join event |
| PUT | `/clubs/api/events/{id}/leave` | Visitor | Leave event |
| GET | `/clubs/api/events/user-events` | Visitor | My events |
| GET | `/clubs/api/events/club-events/{clubId}` | Visitor | Club events |
| GET | `/clubs/api/adhesions/club/{clubId}` | ClubAdmin | Club adhesion requests |
| GET | `/clubs/api/adhesions/{id}` | ClubAdmin | Get adhesion |
| POST | `/clubs/api/adhesions` | Visitor | Request membership |
| PUT | `/clubs/api/adhesions/{id}/accept` | ClubAdmin | Accept request |
| PUT | `/clubs/api/adhesions/{id}/refuse` | ClubAdmin | Refuse request |
| DELETE | `/clubs/api/adhesions/{id}` | ClubAdmin | Delete adhesion |
| GET | `/clubs/api/adhesions/myadhesions` | Visitor | My adhesions |
| GET | `/clubs/api/annoucements/club/{clubId}` | ClubAdmin | Club announcements |
| GET | `/clubs/api/annoucements/{id}` | ClubMember | Get announcement |
| POST | `/clubs/api/annoucements` | ClubAdmin | Create announcement |
| DELETE | `/clubs/api/annoucements/{id}` | ClubAdmin | Delete announcement |
| POST | `/chatbot/api/chatbot/ask` | Authenticated | Ask chatbot |
| GET | `/chatbot/api/chatbot/faqs` | Authenticated | List FAQs |
| POST | `/chatbot/api/chatbot/faqs` | Admin | Create FAQ |
| GET | `/chatbot/api/chatbot/logs` | Admin | Chat logs |
