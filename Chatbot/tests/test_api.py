"""HTTP-level integration tests using FastAPI's TestClient.

The PostgreSQL session and OpenAI/vector-search calls are stubbed so the full
request path (routing, auth, RAG pipeline, response shaping) runs locally
without a database extension or network access.
"""
from types import SimpleNamespace

import jwt
import pytest
from fastapi.testclient import TestClient

from app.db import get_db
from tests.conftest import TEST_PRIVATE_KEY
from app.main import app
from app.schemas import ChatAskResponse, SuggestedAction
from app.services import faq_service as faq_service_mod
from app.services.chatbot_service import chatbot_service
from app.services.faq_indexing_service import RetrievedDoc, faq_indexing_service


class _FakeSession:
    """Minimal stand-in for a SQLAlchemy Session (no real DB)."""

    def add(self, *_args, **_kwargs):
        pass

    def commit(self):
        pass

    def rollback(self):
        pass

    def refresh(self, *_args, **_kwargs):
        pass


def _override_db():
    yield _FakeSession()


@pytest.fixture(autouse=True)
def _wire_db():
    app.dependency_overrides[get_db] = _override_db
    yield
    app.dependency_overrides.clear()


@pytest.fixture
def client():
    return TestClient(app)


def _admin_token() -> str:
    return jwt.encode(
        {"sub": "admin-1", "role": ["PlatformAdmin"], "iss": "IdentityProvider", "aud": "myappusers"},
        TEST_PRIVATE_KEY,
        algorithm="RS256",
    )


def _user_token() -> str:
    return jwt.encode(
        {"sub": "user-1", "role": ["Student"], "iss": "IdentityProvider", "aud": "myappusers"},
        TEST_PRIVATE_KEY,
        algorithm="RS256",
    )


# ---- /ask ----

def test_ask_empty_message_returns_default(client):
    r = client.post(
        "/api/chatbot/ask",
        headers={"Authorization": f"Bearer {_user_token()}"},
        json={"message": "   "},
    )
    assert r.status_code == 200
    body = r.json()
    assert body["escalate"] is False
    assert "Pose-moi une question" in body["answer"]


def test_ask_no_context_escalates_without_llm(client, monkeypatch):
    # Empty search result -> immediate escalation, no LLM call.
    monkeypatch.setattr(faq_indexing_service, "search", lambda q, k: [])
    r = client.post(
        "/api/chatbot/ask",
        headers={"Authorization": f"Bearer {_user_token()}"},
        json={"message": "question inconnue"},
    )
    assert r.status_code == 200
    body = r.json()
    assert body["escalate"] is True


def test_ask_with_context_returns_llm_answer(client, monkeypatch):
    monkeypatch.setattr(
        faq_indexing_service,
        "search",
        lambda q, k: [RetrievedDoc("1", "Q: x\nA: y", "x", "y", "clubs", 0.9)],
    )
    monkeypatch.setattr(
        chatbot_service,
        "_call_llm_with_timeout",
        lambda q, c, h: ChatAskResponse(
            answer="Voici la réponse.",
            suggestedActions=[SuggestedAction(label="Plus", value="en savoir plus")],
            escalate=False,
        ),
    )
    r = client.post(
        "/api/chatbot/ask",
        headers={"Authorization": f"Bearer {_user_token()}"},
        json={"message": "comment rejoindre un club ?"},
    )
    assert r.status_code == 200
    body = r.json()
    assert body["answer"] == "Voici la réponse."
    assert body["escalate"] is False


def test_ask_human_keyword_overrides_escalation(client, monkeypatch):
    monkeypatch.setattr(
        faq_indexing_service,
        "search",
        lambda q, k: [RetrievedDoc("1", "Q: x\nA: y", "x", "y", "clubs", 0.9)],
    )
    monkeypatch.setattr(
        chatbot_service,
        "_call_llm_with_timeout",
        lambda q, c, h: ChatAskResponse(answer="ok", suggestedActions=[], escalate=False),
    )
    r = client.post(
        "/api/chatbot/ask",
        headers={"Authorization": f"Bearer {_user_token()}"},
        json={"message": "je veux un humain"},
    )
    assert r.status_code == 200
    assert r.json()["escalate"] is True


# ---- /faqs ----

def test_list_faqs_public(client, monkeypatch):
    monkeypatch.setattr(faq_service_mod.faq_service, "find_all", lambda db: [])
    r = client.get("/api/chatbot/faqs", headers={"Authorization": f"Bearer {_user_token()}"})
    assert r.status_code == 200
    assert r.json() == []


def test_create_faq_requires_admin(client):
    r = client.post("/api/chatbot/faqs", json={"question": "q?", "answer": "a"})
    assert r.status_code == 401


def test_create_faq_non_admin_forbidden(client):
    r = client.post(
        "/api/chatbot/faqs",
        headers={"Authorization": f"Bearer {_user_token()}"},
        json={"question": "q?", "answer": "a"},
    )
    assert r.status_code == 403


def test_create_faq_validation_error(client):
    # Blank question violates min_length=1 -> 422 before reaching the handler.
    r = client.post(
        "/api/chatbot/faqs",
        headers={"Authorization": f"Bearer {_admin_token()}"},
        json={"question": "", "answer": "a"},
    )
    assert r.status_code == 422


def test_create_faq_admin_ok(client, monkeypatch):
    import uuid
    from datetime import datetime, timezone

    created = SimpleNamespace(
        id=uuid.uuid4(),
        question="Comment créer un club ?",
        answer="Remplissez le formulaire.",
        category="clubs",
        created_at=datetime.now(timezone.utc),
    )
    monkeypatch.setattr(faq_service_mod.faq_service, "create", lambda db, req: created)
    r = client.post(
        "/api/chatbot/faqs",
        headers={"Authorization": f"Bearer {_admin_token()}"},
        json={"question": "Comment créer un club ?", "answer": "Remplissez le formulaire.", "category": "clubs"},
    )
    assert r.status_code == 201
    assert r.json()["question"] == "Comment créer un club ?"


# ---- /logs ----

def test_list_logs_requires_admin(client):
    r = client.get("/api/chatbot/logs")
    assert r.status_code == 401


def test_list_logs_non_admin_forbidden(client):
    r = client.get("/api/chatbot/logs", headers={"Authorization": f"Bearer {_user_token()}"})
    assert r.status_code == 403


# ---- health ----

def test_health(client):
    r = client.get("/actuator/health")
    assert r.status_code == 200
    assert r.json()["status"] == "UP"
