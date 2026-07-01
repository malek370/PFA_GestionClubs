"""Dedicated vector database backed by ChromaDB.

ChromaDB is a persistent, embedded vector database ? it runs locally with no
separate server or database extension required. FAQ embeddings are stored here
(keyed by FAQ id) and queried by cosine similarity, replacing the original
in-memory Spring AI ``SimpleVectorStore`` and the interim pgvector column.
"""
import logging
from dataclasses import dataclass

import chromadb
from chromadb.api.models.Collection import Collection

from app.config import get_settings

log = logging.getLogger(__name__)


@dataclass
class VectorHit:
    """A single nearest-neighbour result from the vector database."""

    id: str
    document: str
    metadata: dict
    similarity: float


class VectorStore:
    """Thin wrapper around a ChromaDB collection using cosine distance."""

    def __init__(self) -> None:
        self._client: chromadb.ClientAPI | None = None
        self._collection: Collection | None = None

    def _coll(self) -> Collection:
        if self._collection is None:
            settings = get_settings()
            self._client = chromadb.PersistentClient(path=settings.chroma_path)
            self._collection = self._client.get_or_create_collection(
                name=settings.chroma_collection,
                metadata={"hnsw:space": "cosine"},
            )
        return self._collection

    def set_collection(self, collection: Collection) -> None:
        """Inject a collection (used by tests with an ephemeral client)."""
        self._collection = collection

    def reset(self) -> None:
        self._client = None
        self._collection = None

    def upsert(self, doc_id: str, document: str, embedding: list[float], metadata: dict) -> None:
        self._coll().upsert(
            ids=[doc_id],
            embeddings=[embedding],
            documents=[document],
            metadatas=[metadata],
        )

    def delete(self, doc_id: str) -> None:
        self._coll().delete(ids=[doc_id])

    def count(self) -> int:
        return self._coll().count()

    def query(self, embedding: list[float], top_k: int) -> list[VectorHit]:
        res = self._coll().query(
            query_embeddings=[embedding],
            n_results=top_k,
            include=["documents", "metadatas", "distances"],
        )
        ids = (res.get("ids") or [[]])[0]
        docs = (res.get("documents") or [[]])[0]
        metas = (res.get("metadatas") or [[]])[0]
        dists = (res.get("distances") or [[]])[0]

        hits: list[VectorHit] = []
        for i, doc_id in enumerate(ids):
            distance = float(dists[i])
            # cosine distance = 1 - cosine similarity (clamped to [0, 1])
            similarity = max(0.0, min(1.0, 1.0 - distance))
            hits.append(
                VectorHit(
                    id=doc_id,
                    document=docs[i],
                    metadata=metas[i] or {},
                    similarity=similarity,
                )
            )
        return hits


vector_store = VectorStore()
