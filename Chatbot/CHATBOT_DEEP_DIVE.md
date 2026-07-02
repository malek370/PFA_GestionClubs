# UniClubs Chatbot Microservice — Deep Dive

A complete guide to understand every layer of the service: from zero to fully productive.

---

## Table of Contents

1. [What Is This Service?](#1-what-is-this-service)
2. [Technology Stack](#2-technology-stack)
3. [Project Layout](#3-project-layout)
4. [Configuration & Environment Variables](#4-configuration--environment-variables)
5. [Data Storage — Two Databases](#5-data-storage--two-databases)
   - 5.1 [PostgreSQL (relational)](#51-postgresql-relational)
   - 5.2 [ChromaDB (vector database)](#52-chromadb-vector-database)
   - 5.3 [How the two stores relate](#53-how-the-two-stores-relate)
6. [SQLAlchemy Models](#6-sqlalchemy-models)
7. [Database Migrations (Alembic)](#7-database-migrations-alembic)
8. [Authentication & Authorization](#8-authentication--authorization)
9. [API Endpoints](#9-api-endpoints)
10. [The RAG Pipeline — How the Chatbot Answers](#10-the-rag-pipeline--how-the-chatbot-answers)
    - 10.1 [Embeddings](#101-embeddings)
    - 10.2 [Vector Store (ChromaDB wrapper)](#102-vector-store-chromadb-wrapper)
    - 10.3 [FAQ Indexing Service](#103-faq-indexing-service)
    - 10.4 [Knowledge Service (domain entities)](#104-knowledge-service-domain-entities)
    - 10.5 [Chatbot Service (orchestrator)](#105-chatbot-service-orchestrator)
11. [Application Startup (lifespan)](#11-application-startup-lifespan)
12. [Seeding the Database](#12-seeding-the-database)
13. [Reindexing the Vector Store](#13-reindexing-the-vector-store)
14. [Running Locally](#14-running-locally)
15. [Tests](#15-tests)
16. [Data Flow — End-to-End Diagram](#16-data-flow--end-to-end-diagram)

---

## 1. What Is This Service?

The Chatbot microservice is a **FastAPI** application that gives users a conversational interface to query university club data. It uses a **Retrieval-Augmented Generation (RAG)** pattern:

1. User asks a question.
2. The question is embedded into a vector.
3. The closest documents (FAQs + club/event/announcement records) are fetched from ChromaDB.
4. Those documents are injected as context into a Google Gemini prompt.
5. Gemini generates a grounded, factual answer.
6. The conversation is logged to PostgreSQL.

It is one of three microservices in the PFA platform:

| Service | Role |
|---|---|
| **GestionClubs** (.NET 9) | Club/member/event management |
| **IdentityProvider** (.NET 9) | Auth: JWT issuance & JWKS |
| **Chatbot** (Python/FastAPI) | This service |

---

## 2. Technology Stack

| Concern | Library / Tool |
|---|---|
| Web framework | FastAPI 0.115 |
| ASGI server | Uvicorn |
| Relational DB | PostgreSQL 16 (via psycopg v3) |
| ORM | SQLAlchemy 2.0 |
| Migrations | Alembic 1.14 |
| Schema validation | Pydantic v2 + pydantic-settings |
| Vector DB | ChromaDB ≥ 0.5 (embedded, on-disk) |
| LLM + embeddings | Google Gemini (`google-genai`) |
| JWT validation | PyJWT 2.10 + cryptography |

---

## 3. Project Layout

```
Chatbot/
├── app/
│   ├── main.py            # FastAPI app + lifespan startup logic
│   ├── config.py          # Settings (pydantic-settings, reads .env)
│   ├── db.py              # SQLAlchemy engine, SessionLocal, Base, get_db()
│   ├── models.py          # All ORM models (FAQ, ChatLog, User, Club, …)
│   ├── schemas.py         # Pydantic request/response DTOs
│   ├── security.py        # JWT validation dependency, CurrentUser, require_admin
│   ├── routers/
│   │   └── chatbot.py     # All HTTP endpoints
│   └── services/
│       ├── chatbot_service.py       # RAG orchestrator
│       ├── faq_service.py           # FAQ CRUD + auto-reindex
│       ├── faq_indexing_service.py  # Embed FAQs → ChromaDB
│       ├── knowledge_service.py     # Embed clubs/events/announcements → ChromaDB
│       ├── embeddings.py            # Gemini embedding calls with rate-limit handling
│       └── vector_store.py          # Thin ChromaDB wrapper
├── migrations/
│   ├── env.py             # Alembic runtime config
│   └── versions/
│       └── 0001_initial.py  # Creates faqs + chat_logs tables
├── scripts/
│   ├── seed.py            # Drop & recreate tables, insert French demo data, reindex
│   └── reindex.py         # Rebuild ChromaDB index from existing Postgres data
├── tests/                 # pytest suite (unit + integration)
├── chroma_data/           # Persistent ChromaDB files (gitignored in prod)
├── .env.example           # Template for environment variables
├── alembic.ini            # Alembic config
├── docker-compose.yml     # Postgres-only compose (for local dev)
└── requirements.txt       # Python dependencies
```

---

## 4. Configuration & Environment Variables

All settings live in `app/config.py` as a `pydantic-settings` `BaseSettings` class. It reads from a `.env` file and falls back to safe defaults for local development.

```python
# app/config.py (simplified)
class Settings(BaseSettings):
    database_url: str = "postgresql+psycopg://uniclubs:uniclubs@localhost:5432/uniclubs"
    gemini_api_key: str = ""
    gemini_chat_model: str = "gemini-2.5-flash"
    gemini_fallback_chat_models: str = "gemini-2.0-flash,gemini-2.0-flash-lite"
    gemini_chat_temperature: float = 0.3
    gemini_embed_model: str = "gemini-embedding-001"
    chroma_path: str = "./chroma_data"
    chroma_collection: str = "faqs"
    server_host: str = "127.0.0.1"
    server_port: int = 8083
    idp_jwks_url: str = "http://localhost:7253/.well-known/jwks"
    jwt_issuer: str = "IdentityProvider"
    jwt_audience: str = "myappusers"
```

Copy `.env.example` to `.env` and fill in `GEMINI_API_KEY` and point `IDP_JWKS_URL` to the running IdentityProvider.

The singleton is cached with `@lru_cache` so it is read once per process.

---

## 5. Data Storage — Two Databases

### 5.1 PostgreSQL (relational)

Holds **structured, mutable data**:

| Table | Purpose |
|---|---|
| `faqs` | FAQ entries (question, answer, category) |
| `chat_logs` | Every user↔bot conversation turn |
| `users` | Users mirrored from GestionClubs |
| `clubs` | Club records |
| `members` | Club membership + role (President, Secretary, …) |
| `announcements` | Club announcements |
| `events` | Club events |
| `adhesions` | Membership applications |
| `event_participants` | Many-to-many: users ↔ events |

Connection is configured via `DATABASE_URL` and managed through SQLAlchemy's `sessionmaker`. Each HTTP request gets a dedicated session via the `get_db()` FastAPI dependency:

```python
def get_db() -> Generator[Session, None, None]:
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()
```

### 5.2 ChromaDB (vector database)

Holds **embedding vectors** for semantic search. ChromaDB runs **embedded** (no separate server) and persists data to the `chroma_data/` directory on disk.

- One collection named `faqs` (configured via `CHROMA_COLLECTION`).
- Uses **cosine distance** (`hnsw:space: cosine`).
- Documents stored: FAQ text, club summaries, event summaries, announcement summaries.
- Each document has a string `id` and a `metadata` dict with a `type` field.

Document ID naming convention:

| Type | Example ID |
|---|---|
| FAQ | `<uuid>` (FAQ's UUID primary key) |
| Club | `club:1` |
| Event | `event:3` |
| Announcement | `announcement:7` |
| Club directory | `catalog:clubs` |

### 5.3 How the two stores relate

```
PostgreSQL                        ChromaDB
──────────────                    ────────────────────────
faqs.id (UUID) ──────────────────► vector doc id = str(faq.id)
clubs.id (int) ──────────────────► vector doc id = "club:{id}"
events.id (int) ─────────────────► vector doc id = "event:{id}"
announcements.id ────────────────► vector doc id = "announcement:{id}"
```

The relational store is the **source of truth**. ChromaDB is a **derived index** that can always be rebuilt by running `scripts/reindex.py`.

---

## 6. SQLAlchemy Models

All models inherit from `Base = DeclarativeBase()` defined in `app/db.py`.

### `FAQ`
```
id          UUID  PK
question    Text
answer      Text
category    String(100) nullable
created_at  DateTime(tz)
```
Indexed on `category`.

### `ChatLog`
```
id           UUID  PK
user_id      String(255) nullable   ← JWT sub claim
session_id   String(255) nullable   ← X-Session-Id header
user_message Text
bot_answer   Text
escalated    Boolean (default False)
feedback     String(10) nullable    ← "POSITIVE" | "NEGATIVE"
created_at   DateTime(tz)
```
Indexed on `user_id`, `session_id`, `created_at`.

### `User`, `Club`, `Member`, `Announcement`, `Event`, `Adhesion`

These mirror the domain entities managed by the GestionClubs service. They exist here so the chatbot can read them for grounding without making cross-service HTTP calls.

**Relationships:**
- `Club` → many `Member`, many `Announcement`, many `Event`, many `Adhesion`
- `Member` → belongs to `User` and `Club`
- `Event` ↔ many `User` via `event_participants` (M2M association table)

---

## 7. Database Migrations (Alembic)

Alembic manages schema evolution. Configuration lives in `alembic.ini`; the runtime wiring is in `migrations/env.py`.

`env.py` auto-injects the `DATABASE_URL` from `Settings` at runtime so you never hardcode the URL in `alembic.ini`.

**Run migrations:**
```bash
cd Chatbot
alembic upgrade head
```

**Current migrations:**

| Revision | Description |
|---|---|
| `0001_initial` | Creates `faqs` and `chat_logs` tables (with all indexes) |

The domain tables (`clubs`, `users`, etc.) are created directly by `scripts/seed.py` via `Base.metadata.create_all(engine)` for the dev workflow.

---

## 8. Authentication & Authorization

### JWT Validation (`app/security.py`)

The service is **stateless** — it validates JWTs signed by the IdentityProvider using **RS256** (asymmetric RSA). It never stores session state.

**Flow:**

```
Request
  │
  ├─ Authorization: Bearer <token>   (optional)
  │
  ▼
get_current_user()
  ├─ No header?  → CurrentUser(authenticated=False, user_id="anonymous")
  ├─ Bad token?  → CurrentUser(authenticated=False)
  └─ Valid token → CurrentUser(user_id=sub, roles=[...], authenticated=True)
```

The JWKS public key is fetched **lazily** from the IdentityProvider's `/.well-known/jwks` endpoint and cached via `PyJWKClient` + `@lru_cache`.

```python
# JWT claims extracted:
user_id = claims["sub"]
roles   = claims["role"]   # or "roles" for legacy tokens
```

Role normalization: raw roles like `"ADMIN"` are stored as `"ROLE_ADMIN"` internally.

### Authorization Levels

| Level | Dependency | Behaviour |
|---|---|---|
| Public | `Depends(get_current_user)` | Any caller, identified or anonymous |
| Admin only | `Depends(require_admin)` | 403 if not `ROLE_ADMIN` |

### Test Isolation

`tests/conftest.py` generates a **throwaway RSA key pair** at test-session startup and patches `_get_jwks_client` so no test ever calls the live IdentityProvider.

---

## 9. API Endpoints

Base prefix: `/api/chatbot`  
Interactive docs: `GET /docs` (Swagger UI) or `/scalar/v1` if configured.  
Health: `GET /actuator/health` → `{"status": "UP"}`

---

### `POST /api/chatbot/ask` — Ask a question

**Auth:** Public (anonymous or authenticated)  
**Request body:**
```json
{
  "message": "Qui est le président du club de robotique ?",
  "context": "(optional) previous conversation history"
}
```
**Header (optional):** `X-Session-Id: <uuid>` — groups turns into a session.

**Response:**
```json
{
  "answer": "Le président est Pierre Dubois.",
  "escalate": false,
  "suggestedActions": [
    { "label": "Voir les clubs", "value": "liste des clubs" },
    { "label": "Comment postuler ?", "value": "comment postuler dans un club" }
  ]
}
```

- `escalate: true` if the question can't be answered or the user asked for a human.
- Every call is persisted to `chat_logs`.

---

### `GET /api/chatbot/faqs` — List FAQs

**Auth:** Public  
**Response:** `FAQResponse[]`

```json
[
  {
    "id": "550e8400-...",
    "question": "Comment rejoindre un club ?",
    "answer": "Remplissez le formulaire d'adhésion...",
    "category": "adhesion",
    "created_at": "2024-01-15T10:00:00Z"
  }
]
```

---

### `POST /api/chatbot/faqs` — Create a FAQ *(admin)*

**Auth:** `ROLE_ADMIN` required (403 otherwise)  
**Request body:**
```json
{
  "question": "Comment créer un club ?",
  "answer": "Contactez l'administration...",
  "category": "clubs"
}
```
**Response (201):** The created `FAQResponse`.

Creating a FAQ automatically triggers `faq_indexing_service.index_faq()` to update the vector store in real time.

---

### `GET /api/chatbot/logs` — List chat logs *(admin)*

**Auth:** `ROLE_ADMIN` required  
**Response:** `ChatLogResponse[]` ordered by `created_at DESC`.

---

## 10. The RAG Pipeline — How the Chatbot Answers

### 10.1 Embeddings

`app/services/embeddings.py` wraps the Gemini embedding API.

- Model: `gemini-embedding-001`
- `embed_text(text)` → single vector `list[float]`
- `embed_texts(texts)` → batch of vectors, with **rate-limit handling**:
  - Sub-batches of 20 items per API call.
  - Budget cap: 90 requests/minute (below the free-tier limit of 100).
  - 429 / `RESOURCE_EXHAUSTED` errors are retried with exponential back-off (up to 8 attempts).
  - Retry delay is parsed from the server's error message when available.

### 10.2 Vector Store (ChromaDB wrapper)

`app/services/vector_store.py` is a thin singleton wrapper:

```python
vector_store.upsert(doc_id, document_text, embedding, metadata)
vector_store.delete(doc_id)
vector_store.query(embedding, top_k)   # returns list[VectorHit]
vector_store.count()
```

`VectorHit` carries `id`, `document`, `metadata`, and a `similarity` score (0→1).  
Distance is converted: `similarity = 1 - cosine_distance`.

The underlying ChromaDB collection is created lazily on first access.

### 10.3 FAQ Indexing Service

`app/services/faq_indexing_service.py` — bridges PostgreSQL FAQs ↔ ChromaDB.

Each FAQ becomes one document:
```
Q: <question>
A: <answer>
```
Metadata stored alongside: `type`, `faqId`, `question`, `answer`, `category`.

Key methods:

| Method | When called |
|---|---|
| `index_all(db)` | Startup (if vector store empty) or `scripts/reindex.py` |
| `index_faq(faq)` | Immediately after `POST /faqs` creates a row |
| `remove_faq(faq_id)` | (available for future delete endpoint) |
| `search(query, top_k)` | During every `/ask` request |

Similarity threshold: **0.35** (intentionally low to handle cross-lingual queries — French data retrieved via English question).

### 10.4 Knowledge Service (domain entities)

`app/services/knowledge_service.py` — indexes clubs, events, and announcements.

It builds **human-readable French text documents** from ORM objects:

**Club document example:**
```
[CLUB #1] Club Developpement Logiciel
Description: Un club dédié aux passionnés de développement...
Nombre de membres: 7
Bureau / roles: Alice Martin = President, Pierre Dubois = VicePresident
Tous les membres: Alice Martin (President), Pierre Dubois (VicePresident), ...
Evenements du club: Workshop Git & GitHub Avance, Coding Dojo...
Annonces publiques: Nouvelle session .NET 9, Hackathon Mars 2024...
```

It also creates a **catalog document** (`catalog:clubs`) listing all clubs, so "list the clubs" queries retrieve a single aggregated overview.

### 10.5 Chatbot Service (orchestrator)

`app/services/chatbot_service.py` — the main RAG loop.

```
User message
     │
     ▼
1. Empty message check
     │
     ▼
2. Keyword escalation check
   (contains "human", "support", "humain", etc.)
     │
     ▼
3. Semantic search (faq_indexing_service.search)
   Top-5 docs, cosine similarity ≥ 0.35
     │
     ├── No docs found → immediate escalation response (no LLM call)
     │
     └── Docs found
           │
           ▼
        4. Build context block
           [Doc 1]\n<text>\n[Doc 2]\n<text>...
           │
           ▼
        5. Call Gemini (with timeout: 45s)
           System prompt: grounding rules + JSON schema instruction
           User prompt:   CONTEXTE + HISTORIQUE + QUESTION
           │
           ├── Fallback model chain on 503/429
           │   primary → gemini-2.0-flash → gemini-2.0-flash-lite
           │
           ▼
        6. Parse JSON response
           { answer, escalate, suggestedActions }
           │
           ▼
7. Force escalate=true if keyword matched in step 2
     │
     ▼
8. Persist ChatLog to PostgreSQL
     │
     ▼
9. Return ChatAskResponse
```

**LLM system prompt key rules:**
- Answer **only** from the provided context.
- Detect user's language (FR/EN) and reply in the same language.
- Always respond as strict JSON.
- Provide 2–3 `suggestedActions`.

**Fallback model chain:** `gemini-2.5-flash` → `gemini-2.0-flash` → `gemini-2.0-flash-lite`. On 503 (overloaded) or 429 (rate limit), the service tries the next model immediately before falling back to retry+backoff.

---

## 11. Application Startup (lifespan)

`app/main.py` uses FastAPI's `@asynccontextmanager lifespan` to run startup logic **before** the server accepts requests:

```python
@asynccontextmanager
async def lifespan(app: FastAPI):
    already_indexed = vector_store.count()
    if already_indexed > 0:
        log.info("Vector store already has %d documents; skipping startup indexing.", already_indexed)
    else:
        db = SessionLocal()
        try:
            faq_indexing_service.index_all(db)   # FAQs → ChromaDB
            knowledge_service.index_all(db)       # Clubs/Events/Announcements → ChromaDB
        finally:
            db.close()
    yield  # server runs here
```

**Key behaviour:** indexing is **skipped** if ChromaDB already has documents. This avoids burning the embedding API quota on every restart. To force a full rebuild, run `scripts/reindex.py`.

---

## 12. Seeding the Database

`scripts/seed.py` is a **development-only** script. It:

1. **Drops and recreates all tables** (`Base.metadata.drop_all` then `create_all`).
2. Inserts 50 French users (realistic first/last name combinations).
3. Inserts 10 clubs: one detailed "Club Développement Logiciel" plus 9 themed clubs (Robotique, IA, Cybersécurité, Jeux Vidéo, Photographie, …).
4. For each club, inserts members (with roles like President, VicePresident, Secretary), events, and announcements.
5. Inserts a set of French FAQs covering common questions about clubs, membership, events.
6. Calls `faq_indexing_service.index_all(db)` and `knowledge_service.index_all(db)` to embed everything into ChromaDB.

**How to run:**
```powershell
cd Chatbot
$env:PYTHONPATH = "."
python scripts/seed.py
```

> **Warning:** This is destructive. It drops all data first.

---

## 13. Reindexing the Vector Store

`scripts/reindex.py` rebuilds the ChromaDB index from whatever is already in PostgreSQL, without touching the relational data.

Use this when:
- The embedding job failed mid-way (e.g., hit the Gemini rate limit).
- You changed the document format in `knowledge_service.py`.
- The `chroma_data/` directory was deleted.

```powershell
cd Chatbot
$env:PYTHONPATH = "."
python scripts/reindex.py
```

---

## 14. Running Locally

### 1. Start PostgreSQL
```bash
docker compose up -d    # starts uniclubs-postgres on port 5432
```

### 2. Create virtual environment & install dependencies
```powershell
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
```

### 3. Configure environment
```powershell
copy .env.example .env
# Edit .env: set GEMINI_API_KEY and IDP_JWKS_URL
```

### 4. Run migrations
```bash
alembic upgrade head
```

### 5. Seed demo data (optional)
```powershell
$env:PYTHONPATH = "."
python scripts/seed.py
```

### 6. Start the server
```powershell
$env:PYTHONPATH = "."
python app/main.py
# or: uvicorn app.main:app --reload
```

Server runs at `http://127.0.0.1:8083`. Swagger UI at `http://127.0.0.1:8083/docs`.

---

## 15. Tests

```bash
pytest tests/
```

| File | What it covers |
|---|---|
| `test_api.py` | HTTP endpoints (TestClient), happy path + error cases |
| `test_db.py` | SQLAlchemy model round-trips against a real (in-memory or test) DB |
| `test_pipeline_logic.py` | RAG logic: context building, JSON parsing, language detection |
| `test_security.py` | JWT validation, anonymous fallback, role checking |
| `test_vector_store.py` | ChromaDB wrapper: upsert, query, delete, similarity |

**Test isolation strategy:**
- `conftest.py` generates a throwaway RSA key pair and auto-patches `_get_jwks_client` for every test (no live IdentityProvider calls).
- Vector store tests use an ephemeral in-memory ChromaDB client via `vector_store.set_collection(...)`.

---

## 16. Data Flow — End-to-End Diagram

```mermaid
sequenceDiagram
    participant Client
    participant FastAPI
    participant JWTValidator
    participant ChatbotService
    participant FaqIndexingService
    participant ChromaDB
    participant Gemini
    participant PostgreSQL

    Client->>FastAPI: POST /api/chatbot/ask\n{message: "Qui est le président?"}
    FastAPI->>JWTValidator: validate Bearer token\n(JWKS from IdentityProvider)
    JWTValidator-->>FastAPI: CurrentUser(user_id, roles)
    
    FastAPI->>ChatbotService: ask(db, request, session_id, user)
    
    ChatbotService->>FaqIndexingService: search("Qui est le président?", top_k=5)
    FaqIndexingService->>Gemini: embed_text(question) → vector
    FaqIndexingService->>ChromaDB: query(vector, top_k=5)
    ChromaDB-->>FaqIndexingService: [VectorHit(doc, similarity), ...]
    FaqIndexingService-->>ChatbotService: [RetrievedDoc(content, similarity≥0.35)]
    
    ChatbotService->>Gemini: generate_content(\n  system=SYSTEM_PROMPT,\n  user=CONTEXT+HISTORY+QUESTION\n)
    Gemini-->>ChatbotService: JSON {answer, escalate, suggestedActions}
    
    ChatbotService->>PostgreSQL: INSERT INTO chat_logs ...
    ChatbotService-->>FastAPI: ChatAskResponse
    FastAPI-->>Client: {answer, escalate, suggestedActions}
```

### Admin creates a FAQ

```mermaid
sequenceDiagram
    participant Admin
    participant FastAPI
    participant FaqService
    participant PostgreSQL
    participant FaqIndexingService
    participant Gemini
    participant ChromaDB

    Admin->>FastAPI: POST /api/chatbot/faqs\n(Bearer token with ROLE_ADMIN)
    FastAPI->>PostgreSQL: INSERT INTO faqs ...
    PostgreSQL-->>FastAPI: FAQ row (with UUID)
    FastAPI->>FaqIndexingService: index_faq(faq)
    FaqIndexingService->>Gemini: embed_text("Q: ... A: ...")
    FaqIndexingService->>ChromaDB: upsert(faq.id, text, vector, metadata)
    FastAPI-->>Admin: 201 FAQResponse
```
