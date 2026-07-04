"""Fix adhesions table to match ORM model

Migration 0002 created adhesions with a ``member_id`` foreign key pointing at
``members.id`` and an ``adhesion_type`` column, but the SQLAlchemy ORM defines
the table with a ``user_id`` foreign key pointing at ``users.id`` and no
``adhesion_type``.

This migration drops and recreates the table with the correct schema so that
``alembic upgrade head`` works cleanly on fresh and existing databases.

Revision ID: 0003_fix_adhesions_schema
Revises: 0002_add_domain_entities
Create Date: 2026-07-04

"""
from typing import Sequence, Union

import sqlalchemy as sa
from alembic import op

revision: str = "0003_fix_adhesions_schema"
down_revision: Union[str, None] = "0002_add_domain_entities"
branch_labels: Union[str, Sequence[str], None] = None
depends_on: Union[str, Sequence[str], None] = None


def upgrade() -> None:
    # Drop the incorrectly-defined adhesions table from 0002.
    # Use if_exists=True because the DB may not have these indexes
    # (e.g. if 0002 was partially applied or the table was altered manually).
    op.drop_index("ix_adhesions_member_id", table_name="adhesions", if_exists=True)
    op.drop_index("ix_adhesions_club_id", table_name="adhesions", if_exists=True)
    op.drop_table("adhesions")

    # Recreate with the schema that matches the ORM model.
    op.create_table(
        "adhesions",
        sa.Column("id", sa.Integer(), nullable=False),
        sa.Column("club_id", sa.Integer(), nullable=False),
        sa.Column("user_id", sa.Integer(), nullable=False),
        sa.Column("status", sa.String(length=30), nullable=False, server_default="Pending"),
        sa.Column("created_at", sa.DateTime(timezone=True), nullable=False),
        sa.ForeignKeyConstraint(["club_id"], ["clubs.id"], ondelete="CASCADE"),
        sa.ForeignKeyConstraint(["user_id"], ["users.id"], ondelete="CASCADE"),
        sa.PrimaryKeyConstraint("id"),
    )
    op.create_index("ix_adhesions_club_id", "adhesions", ["club_id"])
    op.create_index("ix_adhesions_user_id", "adhesions", ["user_id"])


def downgrade() -> None:
    op.drop_index("ix_adhesions_user_id", table_name="adhesions")
    op.drop_index("ix_adhesions_club_id", table_name="adhesions")
    op.drop_table("adhesions")

    # Restore the 0002 schema.
    op.create_table(
        "adhesions",
        sa.Column("id", sa.Integer(), nullable=False),
        sa.Column("club_id", sa.Integer(), nullable=False),
        sa.Column("member_id", sa.Integer(), nullable=False),
        sa.Column("adhesion_type", sa.String(length=50), nullable=False),
        sa.Column("status", sa.String(length=50), nullable=False),
        sa.Column("created_at", sa.DateTime(timezone=True), nullable=False),
        sa.ForeignKeyConstraint(["club_id"], ["clubs.id"], ondelete="CASCADE"),
        sa.ForeignKeyConstraint(["member_id"], ["members.id"], ondelete="CASCADE"),
        sa.PrimaryKeyConstraint("id"),
    )
    op.create_index("ix_adhesions_club_id", "adhesions", ["club_id"])
    op.create_index("ix_adhesions_member_id", "adhesions", ["member_id"])
