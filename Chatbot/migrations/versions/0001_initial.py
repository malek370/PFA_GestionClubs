"""initial schema: faqs and chat_logs tables

Migrates the relational data model from MongoDB collections (faqs, chat_logs)
to PostgreSQL tables. FAQ embeddings are NOT stored here ? they live in the
ChromaDB vector database (see app/services/vector_store.py).

Revision ID: 0001_initial
Revises:
Create Date: 2026-06-10

"""
from typing import Sequence, Union

import sqlalchemy as sa
from alembic import op

revision: str = "0001_initial"
down_revision: Union[str, None] = None
branch_labels: Union[str, Sequence[str], None] = None
depends_on: Union[str, Sequence[str], None] = None


def upgrade() -> None:
    op.create_table(
        "faqs",
        sa.Column("id", sa.Uuid(), primary_key=True),
        sa.Column("question", sa.Text(), nullable=False),
        sa.Column("answer", sa.Text(), nullable=False),
        sa.Column("category", sa.String(length=100), nullable=True),
        sa.Column("created_at", sa.DateTime(timezone=True), nullable=False),
    )
    op.create_index("ix_faqs_category", "faqs", ["category"])

    op.create_table(
        "chat_logs",
        sa.Column("id", sa.Uuid(), primary_key=True),
        sa.Column("user_id", sa.String(length=255), nullable=True),
        sa.Column("session_id", sa.String(length=255), nullable=True),
        sa.Column("user_message", sa.Text(), nullable=False),
        sa.Column("bot_answer", sa.Text(), nullable=False),
        sa.Column("escalated", sa.Boolean(), nullable=False, server_default=sa.false()),
        sa.Column("feedback", sa.String(length=10), nullable=True),
        sa.Column("created_at", sa.DateTime(timezone=True), nullable=False),
    )
    op.create_index("ix_chat_logs_user_id", "chat_logs", ["user_id"])
    op.create_index("ix_chat_logs_session_id", "chat_logs", ["session_id"])
    op.create_index("ix_chat_logs_created_at", "chat_logs", ["created_at"])


def downgrade() -> None:
    op.drop_index("ix_chat_logs_created_at", table_name="chat_logs")
    op.drop_index("ix_chat_logs_session_id", table_name="chat_logs")
    op.drop_index("ix_chat_logs_user_id", table_name="chat_logs")
    op.drop_table("chat_logs")

    op.drop_index("ix_faqs_category", table_name="faqs")
    op.drop_table("faqs")
