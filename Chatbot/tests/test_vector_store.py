"""Real vector-database tests against ChromaDB (ephemeral, no network).

Embeddings are injected deterministically so we exercise ChromaDB's actual
cosine-similarity search and the indexing service end-to-end without calling
Gemini.
"""
import uuid

import chromadb
import pytest

from app.models import FAQ
from app.services import faq_indexing_service as fis_mod
from app.services.faq_indexing_service import faq_indexing_service
from app.services.vector_store import vector_store


# Toy embedding space: each text maps to a 3-dim vector based on keywords.
# "club" -> x axis, "paiement"/"cotisation" -> y axis, otherwise z axis.
def _toy_embed(text: str) -> list[float]:
    t = text.lower()
    if "club" in t or "rejoindre" in t:
        return [1.0, 0.0, 0.0]
    if "paiement" in t or "cotisation" in t or "payer" in t:
        return [0.0, 1.0, 0.0]
    return [0.0, 0.0, 1.0]


@pytest.fixture(autouse=True)
def _wire_chroma_and_embeddings(monkeypatch):
    # Fresh in-memory Chroma collection per test (unique name for isolation).
    chroma_client = chromadb.EphemeralClient()
    collection = chroma_client.get_or_create_collection(
        name=f"test_faqs_{uuid.uuid4().hex}", metadata={"hnsw:space": "cosine"}
    )
    vector_store.set_collection(collection)

    # Deterministic embeddings (no Gemini).
    monkeypatch.setattr(fis_mod, "embed_text", _toy_embed)
    monkeypatch.setattr(fis_mod, "embed_texts", lambda texts: [_toy_embed(t) for t in texts])

    yield
    vector_store.reset()


def _faq(question: str, answer: str, category: str = "general") -> FAQ:
    return FAQ(id=uuid.uuid4(), question=question, answer=answer, category=category)


def test_index_and_count():
    faq_indexing_service.index_faq(_faq("Comment rejoindre un club ?", "Va sur la page Clubs.", "clubs"))
    faq_indexing_service.index_faq(_faq("Comment payer la cotisation ?", "Par carte.", "paiement"))
    assert vector_store.count() == 2


def test_search_returns_nearest_neighbour():
    faq_indexing_service.index_faq(_faq("Comment rejoindre un club ?", "Va sur la page Clubs.", "clubs"))
    faq_indexing_service.index_faq(_faq("Comment payer la cotisation ?", "Par carte.", "paiement"))

    results = faq_indexing_service.search("je veux rejoindre un club", top_k=4)
    assert results, "expected at least one hit"
    assert "club" in results[0].content.lower()
    assert results[0].similarity >= 0.5


def test_search_threshold_filters_unrelated():
    faq_indexing_service.index_faq(_faq("Comment rejoindre un club ?", "Va sur la page Clubs.", "clubs"))
    # Query maps to the orthogonal z-axis -> cosine similarity 0 -> filtered out.
    results = faq_indexing_service.search("question totalement hors sujet", top_k=4)
    assert results == []


def test_upsert_is_idempotent_on_same_id():
    faq = _faq("Comment rejoindre un club ?", "Réponse v1", "clubs")
    faq_indexing_service.index_faq(faq)
    faq_indexing_service.index_faq(faq)  # same id -> upsert, not duplicate
    assert vector_store.count() == 1


def test_remove_faq():
    faq = _faq("Comment rejoindre un club ?", "Va sur la page Clubs.", "clubs")
    faq_indexing_service.index_faq(faq)
    assert vector_store.count() == 1
    faq_indexing_service.remove_faq(str(faq.id))
    assert vector_store.count() == 0
