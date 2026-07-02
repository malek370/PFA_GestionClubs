import logging
from dataclasses import dataclass

from sqlalchemy import select
from sqlalchemy.orm import Session

from app.models import FAQ
from app.services.embeddings import embed_text, embed_texts
from app.services.vector_store import vector_store

log = logging.getLogger(__name__)

# Cosine similarity threshold. Lower than the original 0.5 so that
# cross-lingual matches (English question -> French data) are still retrieved.
SIMILARITY_THRESHOLD = 0.35
DOC_TYPE = "faq"


@dataclass
class RetrievedDoc:
    """A retrieved FAQ document, equivalent to a Spring AI ``Document``."""

    faq_id: str
    content: str
    question: str
    answer: str
    category: str
    similarity: float


class FaqIndexingService:
    """Indexes FAQs into the ChromaDB vector database and runs semantic search.

    Relational FAQ rows live in PostgreSQL; their embeddings live in ChromaDB
    (keyed by FAQ id), replacing the in-memory Spring AI ``SimpleVectorStore``.
    """

    @staticmethod
    def _doc_text(faq: FAQ) -> str:
        return f"Q: {faq.question}\nA: {faq.answer}"

    @staticmethod
    def _metadata(faq: FAQ) -> dict:
        return {
            "type": DOC_TYPE,
            "faqId": str(faq.id),
            "question": faq.question,
            "answer": faq.answer,
            "category": faq.category or "",
        }

    def index_faq(self, faq: FAQ) -> None:
        """Index/refresh a single FAQ in the vector database."""
        try:
            text = self._doc_text(faq)
            embedding = embed_text(text)
            vector_store.upsert(str(faq.id), text, embedding, self._metadata(faq))
        except Exception as e:  # noqa: BLE001
            log.warning("Failed to index FAQ %s: %s", faq.id, e)

    def index_all(self, db: Session) -> None:
        """Push all FAQ rows from PostgreSQL into the vector database (idempotent)."""
        try:
            faqs = db.execute(select(FAQ)).scalars().all()
            if not faqs:
                log.info("No FAQs to index at startup.")
                return
            texts = [self._doc_text(f) for f in faqs]
            embeddings = embed_texts(texts)
            for faq, text, embedding in zip(faqs, texts, embeddings):
                vector_store.upsert(str(faq.id), text, embedding, self._metadata(faq))
            log.info("Indexed %d FAQs into the vector database.", len(faqs))
        except Exception as e:  # noqa: BLE001
            log.warning("FAQ indexing failed (likely missing GEMINI_API_KEY or DB): %s", e)

    def remove_faq(self, faq_id: str) -> None:
        try:
            vector_store.delete(faq_id)
        except Exception as e:  # noqa: BLE001
            log.warning("Failed to remove FAQ %s from vector store: %s", faq_id, e)

    def search(self, query: str, top_k: int) -> list[RetrievedDoc]:
        """Semantic search across FAQ documents using ChromaDB cosine distance."""
        try:
            query_vec = embed_text(query)
            hits = vector_store.query(query_vec, top_k)
            return [
                RetrievedDoc(
                    faq_id=h.id,
                    content=h.document,
                    question=h.metadata.get("question", ""),
                    answer=h.metadata.get("answer", ""),
                    category=h.metadata.get("category", ""),
                    similarity=h.similarity,
                )
                for h in hits
                if h.similarity >= SIMILARITY_THRESHOLD
            ]
        except Exception as e:  # noqa: BLE001
            log.warning("Vector search failed: %s", e)
            return []


faq_indexing_service = FaqIndexingService()
