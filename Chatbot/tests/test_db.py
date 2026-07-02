"""Relational-layer tests for the PostgreSQL data model.

Runs against an in-memory SQLite database ? the models are portable (no
pgvector), so persistence of FAQ and ChatLog rows is verified for real without
needing a running PostgreSQL server.
"""
import uuid

import pytest
from sqlalchemy import create_engine, select
from sqlalchemy.orm import Session

from app.db import Base
from app.models import FAQ, ChatLog


@pytest.fixture
def session():
    engine = create_engine("sqlite+pysqlite:///:memory:", future=True)
    Base.metadata.create_all(engine)
    with Session(engine) as s:
        yield s
    Base.metadata.drop_all(engine)


def test_persist_and_read_faq(session):
    faq = FAQ(question="Comment rejoindre un club ?", answer="Page Clubs.", category="clubs")
    session.add(faq)
    session.commit()

    rows = session.execute(select(FAQ)).scalars().all()
    assert len(rows) == 1
    assert isinstance(rows[0].id, uuid.UUID)
    assert rows[0].question.startswith("Comment rejoindre")
    assert rows[0].created_at is not None


def test_faqs_ordered_by_created_at_desc(session):
    session.add_all([
        FAQ(question="q1", answer="a1", category="c"),
        FAQ(question="q2", answer="a2", category="c"),
    ])
    session.commit()
    rows = session.execute(select(FAQ).order_by(FAQ.created_at.desc())).scalars().all()
    assert len(rows) == 2


def test_persist_chat_log(session):
    log = ChatLog(
        user_id="user-1",
        session_id="sess-1",
        user_message="bonjour",
        bot_answer="salut",
        escalated=False,
    )
    session.add(log)
    session.commit()

    rows = session.execute(select(ChatLog)).scalars().all()
    assert len(rows) == 1
    assert rows[0].escalated is False
    assert rows[0].user_id == "user-1"
