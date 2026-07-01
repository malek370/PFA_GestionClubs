"""(Re)index existing Postgres data into ChromaDB without touching the database.

Use this after the tables are already seeded (see scripts/seed.py) when only the
vector index needs to be rebuilt -- e.g. after hitting an embedding rate limit.

Run:
    cd uniclubs-chatbot-python
    $env:PYTHONPATH="."; python scripts/reindex.py
"""
from app.db import SessionLocal
from app.services.faq_indexing_service import faq_indexing_service
from app.services.knowledge_service import knowledge_service
from app.services.vector_store import vector_store


def reindex() -> None:
    db = SessionLocal()
    try:
        print("Indexing FAQs into ChromaDB...")
        faq_indexing_service.index_all(db)
        print("Indexing domain entities (clubs/events/announcements)...")
        n = knowledge_service.index_all(db)
        print(f"Domain documents indexed: {n}")
        print(f"Total vectors in ChromaDB: {vector_store.count()}")
    finally:
        db.close()


if __name__ == "__main__":
    reindex()
