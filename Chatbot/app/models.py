import uuid
from datetime import datetime, timezone

from sqlalchemy import (
    Boolean,
    Column,
    DateTime,
    ForeignKey,
    Index,
    Integer,
    JSON,
    String,
    Table,
    Text,
    Uuid,
)
from sqlalchemy.orm import Mapped, mapped_column, relationship

from app.db import Base


def _utcnow() -> datetime:
    return datetime.now(timezone.utc)


class FAQ(Base):
    """FAQ entry (relational). Replaces the MongoDB ``faqs`` collection.

    Embeddings for semantic search live in the ChromaDB vector database
    (see ``app.services.vector_store``), keyed by this row's id.
    """

    __tablename__ = "faqs"

    id: Mapped[uuid.UUID] = mapped_column(Uuid, primary_key=True, default=uuid.uuid4)
    question: Mapped[str] = mapped_column(Text, nullable=False)
    answer: Mapped[str] = mapped_column(Text, nullable=False)
    category: Mapped[str | None] = mapped_column(String(100), nullable=True)
    created_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), default=_utcnow, nullable=False)

    __table_args__ = (Index("ix_faqs_category", "category"),)


class ChatLog(Base):
    """Conversation log. Replaces the MongoDB ``chat_logs`` collection."""

    __tablename__ = "chat_logs"

    id: Mapped[uuid.UUID] = mapped_column(Uuid, primary_key=True, default=uuid.uuid4)
    user_id: Mapped[str | None] = mapped_column(String(255), nullable=True)
    session_id: Mapped[str | None] = mapped_column(String(255), nullable=True)
    user_message: Mapped[str] = mapped_column(Text, nullable=False)
    bot_answer: Mapped[str] = mapped_column(Text, nullable=False)
    escalated: Mapped[bool] = mapped_column(Boolean, default=False, nullable=False)
    feedback: Mapped[str | None] = mapped_column(String(10), nullable=True)  # POSITIVE / NEGATIVE
    created_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), default=_utcnow, nullable=False)

    __table_args__ = (
        Index("ix_chat_logs_user_id", "user_id"),
        Index("ix_chat_logs_session_id", "session_id"),
        Index("ix_chat_logs_created_at", "created_at"),
    )


# ---------------------------------------------------------------------------
# UniClubs domain entities (clubs, members, events, announcements, adhesions).
# These hold the real business data the chatbot grounds its answers on.
# ---------------------------------------------------------------------------

# Many-to-many: which users participate in which events.
event_participants = Table(
    "event_participants",
    Base.metadata,
    Column("event_id", Integer, ForeignKey("events.id", ondelete="CASCADE"), primary_key=True),
    Column("user_id", Integer, ForeignKey("users.id", ondelete="CASCADE"), primary_key=True),
)


class User(Base):
    __tablename__ = "users"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    email: Mapped[str] = mapped_column(String(255), nullable=False, unique=True)
    first_name: Mapped[str] = mapped_column(String(120), nullable=False)
    last_name: Mapped[str] = mapped_column(String(120), nullable=False)
    created_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), default=_utcnow, nullable=False)

    memberships: Mapped[list["Member"]] = relationship(back_populates="user")


class Club(Base):
    __tablename__ = "clubs"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    name: Mapped[str] = mapped_column(String(255), nullable=False)
    description: Mapped[str | None] = mapped_column(Text, nullable=True)
    documents: Mapped[list] = mapped_column(JSON, default=list, nullable=False)
    created_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), default=_utcnow, nullable=False)

    members: Mapped[list["Member"]] = relationship(back_populates="club", cascade="all, delete-orphan")
    announcements: Mapped[list["Announcement"]] = relationship(back_populates="club", cascade="all, delete-orphan")
    events: Mapped[list["Event"]] = relationship(back_populates="club", cascade="all, delete-orphan")
    adhesions: Mapped[list["Adhesion"]] = relationship(back_populates="club", cascade="all, delete-orphan")

    __table_args__ = (Index("ix_clubs_name", "name"),)


class Member(Base):
    __tablename__ = "members"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    club_id: Mapped[int] = mapped_column(ForeignKey("clubs.id", ondelete="CASCADE"), nullable=False)
    user_id: Mapped[int] = mapped_column(ForeignKey("users.id", ondelete="CASCADE"), nullable=False)
    post_in_club: Mapped[str] = mapped_column(String(50), nullable=False)  # President, Secretary, Member, ...
    created_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), default=_utcnow, nullable=False)

    club: Mapped["Club"] = relationship(back_populates="members")
    user: Mapped["User"] = relationship(back_populates="memberships")

    __table_args__ = (Index("ix_members_club_id", "club_id"), Index("ix_members_user_id", "user_id"))


class Announcement(Base):
    __tablename__ = "announcements"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    club_id: Mapped[int] = mapped_column(ForeignKey("clubs.id", ondelete="CASCADE"), nullable=False)
    title: Mapped[str] = mapped_column(String(255), nullable=False)
    content: Mapped[str] = mapped_column(Text, nullable=False)
    is_public: Mapped[bool] = mapped_column(Boolean, default=True, nullable=False)
    created_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), default=_utcnow, nullable=False)

    club: Mapped["Club"] = relationship(back_populates="announcements")

    __table_args__ = (Index("ix_announcements_club_id", "club_id"),)


class Event(Base):
    __tablename__ = "events"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    club_id: Mapped[int] = mapped_column(ForeignKey("clubs.id", ondelete="CASCADE"), nullable=False)
    title: Mapped[str] = mapped_column(String(255), nullable=False)
    description: Mapped[str | None] = mapped_column(Text, nullable=True)
    is_public: Mapped[bool] = mapped_column(Boolean, default=True, nullable=False)
    location: Mapped[str | None] = mapped_column(String(255), nullable=True)
    start_date: Mapped[datetime | None] = mapped_column(DateTime(timezone=True), nullable=True)
    tags: Mapped[list] = mapped_column(JSON, default=list, nullable=False)
    created_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), default=_utcnow, nullable=False)

    club: Mapped["Club"] = relationship(back_populates="events")
    participants: Mapped[list["User"]] = relationship(secondary=event_participants)

    __table_args__ = (Index("ix_events_club_id", "club_id"), Index("ix_events_start_date", "start_date"))


class Adhesion(Base):
    __tablename__ = "adhesions"

    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    club_id: Mapped[int] = mapped_column(ForeignKey("clubs.id", ondelete="CASCADE"), nullable=False)
    user_id: Mapped[int] = mapped_column(ForeignKey("users.id", ondelete="CASCADE"), nullable=False)
    status: Mapped[str] = mapped_column(String(30), default="Pending", nullable=False)  # Approved, Pending, Rejected
    created_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), default=_utcnow, nullable=False)

    club: Mapped["Club"] = relationship(back_populates="adhesions")

    __table_args__ = (Index("ix_adhesions_club_id", "club_id"), Index("ix_adhesions_user_id", "user_id"))
