"""Background Kafka consumer for the UniClubs chatbot.

Consumes events published by the GestionClubs and IdentityProvider microservices
and keeps the chatbot's PostgreSQL database and ChromaDB vector store up-to-date
in near-real time.

Subscribed topics
-----------------
user-registered           (IdentityProvider)  → upsert User
clubs-topic               (GestionClubs)      → upsert Club  + reindex ChromaDB
announcements-topic       (GestionClubs)      → upsert Announcement + reindex
events-topic              (GestionClubs)      → upsert Event + reindex
user-promoted-to-club-admin   (GestionClubs)  → set Member.post_in_club = President
user-promoted-to-club-member  (GestionClubs)  → create Member record

Payload fields follow the default System.Text.Json PascalCase serialisation used
by the .NET producers (e.g. {"ClubId": 1, "ClubName": "...", ...}).
"""
import json
import logging
import threading
from datetime import datetime, timezone
from typing import Any

from app.config import get_settings
from app.db import SessionLocal
from app.models import Announcement, Club, Event, Member, User

log = logging.getLogger(__name__)


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _get(payload: dict, *keys: str, default=None):
    """Try multiple key variants (PascalCase then camelCase) for .NET → Python compat."""
    for k in keys:
        if k in payload:
            return payload[k]
    return default


def _parse_dt(value: str | None) -> datetime | None:
    if not value:
        return None
    try:
        return datetime.fromisoformat(value.replace("Z", "+00:00"))
    except (ValueError, AttributeError):
        return None


# ---------------------------------------------------------------------------
# Per-topic event handlers
# ---------------------------------------------------------------------------

def _handle_user_registered(payload: dict[str, Any]) -> None:
    """Upsert the User row (mirrors GestionClubs consumer behaviour)."""
    email = _get(payload, "Email", "email", default="")
    first_name = _get(payload, "FirstName", "firstName", default="")
    last_name = _get(payload, "LastName", "lastName", default="")

    if not email:
        log.warning("user-registered: missing email, skipping")
        return

    with SessionLocal() as db:
        existing = db.query(User).filter(User.email == email).first()
        if not existing:
            db.add(User(email=email, first_name=first_name, last_name=last_name))
            db.commit()
            log.info("Kafka ▶ added User to DB email=%s", email)
        else:
            existing.first_name = first_name
            existing.last_name = last_name
            db.commit()
            log.info("Kafka ▶ updated User in DB email=%s", email)


def _handle_club_created(payload: dict[str, Any]) -> None:
    """Upsert Club row and re-index it in ChromaDB."""
    from app.services.knowledge_service import knowledge_service  # avoid circular at load time

    club_id = _get(payload, "ClubId", "clubId")
    name = _get(payload, "ClubName", "clubName", default="")
    description = _get(payload, "Description", "description", default="")

    if club_id is None:
        log.warning("clubs-topic: missing ClubId, skipping")
        return

    with SessionLocal() as db:
        club = db.get(Club, club_id)
        action = "updated"
        if not club:
            db.add(Club(id=club_id, name=name, description=description))
            action = "added"
        else:
            club.name = name
            club.description = description
        db.commit()

    knowledge_service.index_club_by_id(club_id)
    log.info("Kafka ▶ %s Club in DB id=%s name=%s", action, club_id, name)


def _handle_announcement_created(payload: dict[str, Any]) -> None:
    """Upsert Announcement row and re-index it in ChromaDB."""
    from app.services.knowledge_service import knowledge_service

    ann_id = _get(payload, "AnnouncementId", "announcementId")
    title = _get(payload, "Title", "title", default="")
    content = _get(payload, "Content", "content", default="")
    club_id = _get(payload, "ClubId", "clubId")
    club_name = _get(payload, "ClubName", "clubName", default="")

    if ann_id is None:
        log.warning("announcements-topic: missing AnnouncementId, skipping")
        return

    with SessionLocal() as db:
        # Prefer ClubId from payload; fall back to name-based lookup.
        if club_id is not None:
            club = db.get(Club, club_id)
        else:
            club = db.query(Club).filter(Club.name == club_name).first() if club_name else None

        if not club:
            log.warning(
                "Kafka ▶ announcement: club not found (id=%s name='%s'), skipping",
                club_id, club_name,
            )
            return

        ann = db.get(Announcement, ann_id)
        action = "updated"
        if not ann:
            db.add(Announcement(id=ann_id, club_id=club.id, title=title, content=content))
            action = "added"
        else:
            ann.title = title
            ann.content = content
        db.commit()

    knowledge_service.index_announcement_by_id(ann_id)
    log.info("Kafka ▶ %s Announcement in DB id=%s for club '%s'", action, ann_id, club.name)


def _handle_event_created(payload: dict[str, Any]) -> None:
    """Upsert Event row and re-index it in ChromaDB."""
    from app.services.knowledge_service import knowledge_service

    event_id = _get(payload, "EventId", "eventId")
    title = _get(payload, "Title", "title", default="")
    description = _get(payload, "Description", "description", default="")
    location = _get(payload, "Location", "location")
    start_date = _parse_dt(_get(payload, "StartDate", "startDate"))
    is_public = bool(_get(payload, "IsPublic", "isPublic", default=True))
    tags_raw = _get(payload, "Tags", "tags", default=[])
    tags = list(tags_raw) if isinstance(tags_raw, (list, tuple)) else []
    club_id = _get(payload, "ClubId", "clubId")
    club_name = _get(payload, "ClubName", "clubName", default="")

    if event_id is None:
        log.warning("events-topic: missing EventId, skipping")
        return

    with SessionLocal() as db:
        # Prefer ClubId from payload; fall back to name-based lookup.
        if club_id is not None:
            club = db.get(Club, club_id)
        else:
            club = db.query(Club).filter(Club.name == club_name).first() if club_name else None

        if not club:
            log.warning(
                "Kafka ▶ event: club not found (id=%s name='%s'), skipping",
                club_id, club_name,
            )
            return

        ev = db.get(Event, event_id)
        action = "updated"
        if not ev:
            db.add(Event(
                id=event_id,
                club_id=club.id,
                title=title,
                description=description,
                location=location,
                start_date=start_date,
                is_public=is_public,
                tags=tags,
            ))
            action = "added"
        else:
            ev.title = title
            ev.description = description
            ev.location = location
            ev.start_date = start_date
            ev.is_public = is_public
            ev.tags = tags
        db.commit()

    knowledge_service.index_event_by_id(event_id)
    log.info("Kafka ▶ %s Event in DB id=%s for club '%s'", action, event_id, club.name)


def _handle_user_promoted_member(payload: dict[str, Any]) -> None:
    """Create a Member row (post = Member) when an adhesion is accepted."""
    from app.services.knowledge_service import knowledge_service

    email = _get(payload, "Email", "email", default="")
    club_id = _get(payload, "ClubId", "clubId")

    if not email or club_id is None:
        log.warning("user-promoted-to-club-member: missing fields, skipping")
        return

    with SessionLocal() as db:
        user = db.query(User).filter(User.email == email).first()
        if not user:
            log.warning("Kafka ▶ promoted-member: user '%s' not found", email)
            return

        if not db.get(Club, club_id):
            log.warning("Kafka ▶ promoted-member: club %s not found", club_id)
            return

        existing = db.query(Member).filter(
            Member.user_id == user.id, Member.club_id == club_id
        ).first()
        if not existing:
            db.add(Member(club_id=club_id, user_id=user.id, post_in_club="Member"))
            db.commit()
            log.info("Kafka ▶ added Member to DB user=%s club=%s", email, club_id)
        else:
            log.info("Kafka ▶ Member already exists in DB user=%s club=%s", email, club_id)

    knowledge_service.index_club_by_id(club_id)


def _handle_user_promoted_admin(payload: dict[str, Any]) -> None:
    """Set member post to President when a user is promoted to club admin."""
    from app.services.knowledge_service import knowledge_service

    email = _get(payload, "Email", "email", default="")
    club_id = _get(payload, "ClubId", "clubId")

    if not email or club_id is None:
        log.warning("user-promoted-to-club-admin: missing fields, skipping")
        return

    with SessionLocal() as db:
        user = db.query(User).filter(User.email == email).first()
        if not user:
            log.warning("Kafka ▶ promoted-admin: user '%s' not found", email)
            return

        member = db.query(Member).filter(
            Member.user_id == user.id, Member.club_id == club_id
        ).first()
        if member:
            member.post_in_club = "President"
            db.commit()
            log.info("Kafka ▶ updated Member in DB user=%s to President in club=%s", email, club_id)
        else:
            # User may not have a member record yet; create it
            if db.get(Club, club_id):
                db.add(Member(club_id=club_id, user_id=user.id, post_in_club="President"))
                db.commit()
                log.info("Kafka ▶ added President Member to DB user=%s club=%s", email, club_id)

    knowledge_service.index_club_by_id(club_id)


# ---------------------------------------------------------------------------
# Topic → handler map (built at module load, after all functions are defined)
# ---------------------------------------------------------------------------

_HANDLERS: dict[str, Any] = {
    "user-registered": _handle_user_registered,
    "clubs-topic": _handle_club_created,
    "announcements-topic": _handle_announcement_created,
    "events-topic": _handle_event_created,
    "user-promoted-to-club-admin": _handle_user_promoted_admin,
    "user-promoted-to-club-member": _handle_user_promoted_member,
}


# ---------------------------------------------------------------------------
# Consumer service (daemon thread)
# ---------------------------------------------------------------------------

class KafkaConsumerService:
    """Wraps a Confluent Kafka consumer in a daemon thread.

    Start via ``kafka_consumer_service.start()`` during app lifespan;
    stop via ``kafka_consumer_service.stop()`` on shutdown.
    """

    def __init__(self) -> None:
        self._thread: threading.Thread | None = None
        self._stop_event = threading.Event()

    def start(self) -> None:
        settings = get_settings()
        if not settings.kafka_enabled:
            log.info("Kafka consumer disabled (KAFKA_ENABLED=false); skipping.")
            return

        self._stop_event.clear()
        self._thread = threading.Thread(target=self._run, daemon=True, name="kafka-consumer")
        self._thread.start()
        log.info("Kafka consumer thread started (bootstrap=%s).", settings.kafka_bootstrap_servers)

    def stop(self) -> None:
        self._stop_event.set()
        if self._thread and self._thread.is_alive():
            self._thread.join(timeout=10)
        log.info("Kafka consumer thread stopped.")

    def _run(self) -> None:
        try:
            from confluent_kafka import Consumer, KafkaError
        except ImportError:
            log.error("confluent-kafka not installed; Kafka consumer disabled.")
            return

        settings = get_settings()
        consumer = Consumer({
            "bootstrap.servers": settings.kafka_bootstrap_servers,
            "group.id": settings.kafka_consumer_group_id,
            "auto.offset.reset": "earliest",
            "enable.auto.commit": False,
            "session.timeout.ms": 10000,
            "heartbeat.interval.ms": 3000,
        })

        topics = [
            settings.kafka_topics_user_registered,
            settings.kafka_topics_clubs,
            settings.kafka_topics_announcements,
            settings.kafka_topics_events,
            settings.kafka_topics_user_promoted_admin,
            settings.kafka_topics_user_promoted_member,
        ]
        consumer.subscribe(topics)
        log.info("Kafka consumer subscribed to topics: %s", topics)

        try:
            while not self._stop_event.is_set():
                msg = consumer.poll(timeout=1.0)
                if msg is None:
                    continue
                if msg.error():
                    if msg.error().code() == KafkaError._PARTITION_EOF:
                        continue
                    log.error("Kafka consumer error: %s", msg.error())
                    continue

                topic = msg.topic()
                raw = msg.value()
                if not raw:
                    continue

                try:
                    payload = json.loads(raw.decode("utf-8"))
                except json.JSONDecodeError as exc:
                    log.error("Cannot parse message on topic '%s': %s", topic, exc)
                    continue

                log.info("Kafka ▶ read message from topic='%s' key=%s", topic, msg.key())

                handler = _HANDLERS.get(topic)
                if handler:
                    try:
                        handler(payload)
                        # Commit only after successful handler execution.
                        consumer.commit(message=msg, asynchronous=False)
                    except Exception as exc:  # noqa: BLE001
                        log.error(
                            "Handler error for topic '%s' key=%s: %s — offset NOT committed",
                            topic,
                            msg.key(),
                            exc,
                            exc_info=True,
                        )
                else:
                    # No handler; commit to avoid reprocessing on restart.
                    consumer.commit(message=msg, asynchronous=False)
                    log.debug("No handler registered for topic '%s'", topic)
        finally:
            consumer.close()
            log.info("Kafka consumer closed.")


kafka_consumer_service = KafkaConsumerService()
