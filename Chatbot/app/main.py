import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.config import get_settings
from app.db import SessionLocal
from app.routers.chatbot import router as chatbot_router
from app.services.faq_indexing_service import faq_indexing_service
from app.services.kafka_consumer import kafka_consumer_service
from app.services.knowledge_service import knowledge_service
from app.services.vector_store import vector_store

logging.basicConfig(level=logging.INFO)
log = logging.getLogger("uniclubs")


@asynccontextmanager
async def lifespan(app: FastAPI):
    # Index FAQs and domain entities into the vector database at startup
    # (replaces the original @PostConstruct indexing).
    #
    # Skip if the persistent vector store is already populated, to avoid
    # re-embedding everything on every boot (which would burn the embedding
    # API quota). Run scripts/reindex.py to force a rebuild.
    try:
        already_indexed = vector_store.count()
    except Exception:  # noqa: BLE001
        already_indexed = 0

    if already_indexed > 0:
        log.info("Vector store already has %d documents; skipping startup indexing.", already_indexed)
    else:
        db = SessionLocal()
        try:
            faq_indexing_service.index_all(db)
            knowledge_service.index_all(db)
        finally:
            db.close()

    kafka_consumer_service.start()
    yield
    kafka_consumer_service.stop()


app = FastAPI(title="UniClubs Chatbot (Python)", version="1.0.0", lifespan=lifespan)

# CORS: allow all origins with credentials (mirrors SecurityConfig).
app.add_middleware(
    CORSMiddleware,
    allow_origin_regex=".*",
    allow_credentials=True,
    allow_methods=["GET", "POST", "PUT", "DELETE", "OPTIONS"],
    allow_headers=["*"],
)

app.include_router(chatbot_router)


@app.get("/actuator/health", tags=["health"])
def health() -> dict[str, str]:
    return {"status": "UP"}


if __name__ == "__main__":
    import uvicorn

    settings = get_settings()
    uvicorn.run("app.main:app", host=settings.server_host, port=settings.server_port, reload=True)
