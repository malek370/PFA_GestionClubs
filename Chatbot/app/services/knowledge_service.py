"""Indexes UniClubs domain entities (clubs, members, events, announcements)
into the ChromaDB vector database so the chatbot can ground answers on real data.

Each entity becomes one searchable document, stored in the same Chroma
collection as the FAQs but with a type-prefixed id ("club:1", "event:3",
"announcement:2"). Embeddings are multilingual, so French data is retrievable
from both French and English questions.
"""
import logging

from sqlalchemy import select
from sqlalchemy.orm import Session

from app.models import Announcement, Club, Event
from app.services.embeddings import embed_texts
from app.services.vector_store import vector_store

log = logging.getLogger(__name__)


def _fmt_date(value) -> str:
    if not value:
        return "date non precisee"
    try:
        return value.strftime("%Y-%m-%d %H:%M")
    except Exception:  # noqa: BLE001
        return str(value)


class KnowledgeService:
    """Builds and indexes documents for the clubs domain."""

    @staticmethod
    def _club_document(club: Club) -> str:
        members = list(club.members)
        roster = ", ".join(
            f"{m.user.first_name} {m.user.last_name} ({m.post_in_club})" for m in members if m.user
        )
        leaders = [m for m in members if m.post_in_club and m.post_in_club.lower() != "member"]
        leaders_txt = ", ".join(
            f"{m.user.first_name} {m.user.last_name} = {m.post_in_club}" for m in leaders if m.user
        )
        event_titles = ", ".join(e.title for e in club.events) or "aucun"
        announce_titles = ", ".join(a.title for a in club.announcements if a.is_public) or "aucune"
        docs = ", ".join(club.documents or []) or "aucun"
        lines = [
            f"[CLUB #{club.id}] {club.name}",
            f"Description: {club.description or ''}",
            f"Nombre de membres: {len(members)}",
            f"Bureau / roles: {leaders_txt or 'non precise'}",
            f"Tous les membres: {roster or 'aucun'}",
            f"Documents officiels: {docs}",
            f"Evenements du club: {event_titles}",
            f"Annonces publiques: {announce_titles}",
        ]
        return "\n".join(lines)

    @staticmethod
    def _event_document(event: Event, club_name: str) -> str:
        participants = ", ".join(
            f"{u.first_name} {u.last_name}" for u in event.participants
        ) or "aucun inscrit"
        tags = ", ".join(event.tags or []) or "aucun"
        visibility = "public" if event.is_public else "prive (reserve aux membres)"
        lines = [
            f"[EVENEMENT #{event.id}] {event.title}",
            f"Club organisateur: {club_name}",
            f"Description: {event.description or ''}",
            f"Date: {_fmt_date(event.start_date)}",
            f"Lieu: {event.location or 'non precise'}",
            f"Visibilite: {visibility}",
            f"Tags: {tags}",
            f"Participants ({len(event.participants)}): {participants}",
        ]
        return "\n".join(lines)

    @staticmethod
    def _announcement_document(ann: Announcement, club_name: str) -> str:
        visibility = "publique" if ann.is_public else "privee (reserve aux membres)"
        lines = [
            f"[ANNONCE #{ann.id}] {ann.title}",
            f"Club: {club_name}",
            f"Publiee le: {_fmt_date(ann.created_at)} ({visibility})",
            f"Contenu: {ann.content}",
        ]
        return "\n".join(lines)

    def index_all(self, db: Session) -> int:
        """(Re)index every club, event and announcement. Returns docs indexed."""
        try:
            clubs = db.execute(select(Club)).scalars().all()
            if not clubs:
                log.info("No clubs to index.")
                return 0

            ids: list[str] = []
            documents: list[str] = []
            metadatas: list[dict] = []

            # A directory document so "list all clubs" style queries retrieve an
            # aggregate overview (individual club docs alone don't match well).
            directory_lines = [f"[ANNUAIRE DES CLUBS] La plateforme UniClubs compte {len(clubs)} clubs:"]
            for c in clubs:
                directory_lines.append(
                    f"- {c.name} ({len(list(c.members))} membres) : {(c.description or '')[:120]}"
                )
            ids.append("catalog:clubs")
            documents.append("\n".join(directory_lines))
            metadatas.append({"type": "catalog", "title": "Annuaire des clubs"})

            for club in clubs:
                ids.append(f"club:{club.id}")
                documents.append(self._club_document(club))
                metadatas.append({
                    "type": "club",
                    "entityId": str(club.id),
                    "name": club.name,
                    "title": club.name,
                })

                for ev in club.events:
                    ids.append(f"event:{ev.id}")
                    documents.append(self._event_document(ev, club.name))
                    metadatas.append({
                        "type": "event",
                        "entityId": str(ev.id),
                        "clubId": str(club.id),
                        "title": ev.title,
                    })

                for ann in club.announcements:
                    ids.append(f"announcement:{ann.id}")
                    documents.append(self._announcement_document(ann, club.name))
                    metadatas.append({
                        "type": "announcement",
                        "entityId": str(ann.id),
                        "clubId": str(club.id),
                        "title": ann.title,
                    })

            embeddings = embed_texts(documents)
            for doc_id, doc, emb, meta in zip(ids, documents, embeddings, metadatas):
                vector_store.upsert(doc_id, doc, emb, meta)

            log.info("Indexed %d domain documents into the vector database.", len(ids))
            return len(ids)
        except Exception as e:  # noqa: BLE001
            log.warning("Domain indexing failed (likely missing GEMINI_API_KEY or DB): %s", e)
            return 0


knowledge_service = KnowledgeService()
