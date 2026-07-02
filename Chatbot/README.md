# UniClubs Chatbot ? Python Edition

A Python (FastAPI) reimplementation of the UniClubs RAG FAQ chatbot. Relational
data is migrated from **MongoDB** to **PostgreSQL**, and embeddings live in a
dedicated **ChromaDB** vector database. The LLM is **Google Gemini Flash**.

This is a faithful port of the original Spring Boot backend described in
`../uniclubs-backend/ARCHITECTURE.md`. Behaviour (RAG pipeline, escalation
rules, JWT security, endpoints) is preserved; only the language and the
persistence/AI layers changed.

## What changed vs. the Java app

| Concern | Java (original) | Python (this project) |
|---|---|---|
| Framework | Spring Boot 3.3 | FastAPI |
| Database | MongoDB | PostgreSQL (relational data) |
| Vector store | In-memory `SimpleVectorStore` | **ChromaDB** (persistent, dedicated vector DB) |
| ORM / data | Spring Data MongoDB | SQLAlchemy 2.0 |
| Migrations | (none) | Alembic |
| AI / LLM | Spring AI + OpenAI | Google Gemini (`google-genai` SDK) |
| Security | Spring Security + JWT | FastAPI dependencies + PyJWT |

Embeddings are stored in a persistent ChromaDB collection, so the vector index
is no longer re-built from scratch on every restart ? a production improvement
called out in the original architecture notes.

## Project layout

```
app/
  config.py            # Settings (env vars), mirrors application.yml
  db.py                # SQLAlchemy engine/session + Base
  models.py            # FAQ, ChatLog tables (relational only)
  schemas.py           # Pydantic request/response models (DTOs)
  security.py          # Stateless JWT auth + role checks
  main.py              # FastAPI app, CORS, startup indexing, /actuator/health
  routers/chatbot.py   # REST endpoints under /api/chatbot
  services/
    embeddings.py            # Gemini embedding client
    vector_store.py          # ChromaDB wrapper (dedicated vector DB)
    faq_indexing_service.py  # ChromaDB indexing + semantic search
    faq_service.py           # FAQ CRUD
    chatbot_service.py       # Core RAG pipeline (Gemini Flash)
migrations/            # Alembic (0001_initial creates tables)
chroma_data/           # Persistent ChromaDB store (gitignored)
docker-compose.yml     # PostgreSQL
```

## Prerequisites

- Python 3.11+
- PostgreSQL 14+ (the bundled `docker-compose.yml` provides one)
- A Google Gemini API key

ChromaDB is embedded (no separate server) and persists to `chroma_data/`.

## Setup

```powershell
# 1. Start Postgres
docker compose up -d

# 2. Create and activate a virtual environment
python -m venv .venv
.\.venv\Scripts\Activate.ps1

# 3. Install dependencies
pip install -r requirements.txt

# 4. Configure environment
Copy-Item .env.example .env
# then edit .env and set GEMINI_API_KEY (and JWT_SECRET for production)

# 5. Run the database migration to PostgreSQL
alembic upgrade head

# 6. Start the API
python -m app.main
# or: uvicorn app.main:app --reload --port 8080
```

The API is served on `http://localhost:8080`. Interactive docs at `/docs`.

## Environment variables

| Variable | Default | Description |
|---|---|---|
| `DATABASE_URL` | `postgresql+psycopg://uniclubs:uniclubs@localhost:5432/uniclubs` | PostgreSQL connection |
| `GEMINI_API_KEY` | *(empty)* | Required for embeddings & chat |
| `GEMINI_CHAT_MODEL` | `gemini-2.5-flash` | Chat model |
| `GEMINI_CHAT_TEMPERATURE` | `0.3` | Sampling temperature |
| `GEMINI_EMBED_MODEL` | `gemini-embedding-001` | Embedding model |
| `CHROMA_PATH` | `./chroma_data` | ChromaDB persistence directory |
| `CHROMA_COLLECTION` | `faqs` | ChromaDB collection name |
| `SERVER_PORT` | `8080` | HTTP port |
| `JWT_SECRET` | `change-me-...` | HS256 secret ? **change in production** |

## REST API

| Endpoint | Method | Access | Description |
|---|---|---|---|
| `/api/chatbot/ask` | POST | Public | Ask the chatbot |
| `/api/chatbot/faqs` | GET | Public | List FAQs (newest first) |
| `/api/chatbot/faqs` | POST | `ROLE_ADMIN` | Create + index a FAQ |
| `/api/chatbot/logs` | GET | `ROLE_ADMIN` | List chat logs (newest first) |
| `/actuator/health` | GET | Public | Health check |

### Example

```bash
curl -X POST http://localhost:8080/api/chatbot/ask \
  -H "Content-Type: application/json" \
  -H "X-Session-Id: 11111111-1111-1111-1111-111111111111" \
  -d '{ "message": "Comment rejoindre un club ?" }'
```

## Migrating existing MongoDB data (optional)

The schema migration (`alembic upgrade head`) creates empty tables. To carry
over existing documents, export each MongoDB collection and insert the rows into
the matching PostgreSQL table (`faqs`, `chat_logs`). Embeddings are not stored in
Postgres ? they are regenerated into ChromaDB automatically at startup by
`faq_indexing_service.index_all` and on FAQ creation.

## JWT format

Tokens are HS256-signed with `JWT_SECRET` and carry:
- `sub` ? user id
- `roles` ? list of roles, e.g. `["ADMIN"]` (normalised to `ROLE_ADMIN`)
