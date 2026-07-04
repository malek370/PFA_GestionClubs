"""Tests for the Kafka consumer: offset commitment discipline and event handlers."""
from unittest.mock import MagicMock, patch, call

import pytest

from app.services.kafka_consumer import (
    _handle_announcement_created,
    _handle_event_created,
    _handle_user_registered,
    KafkaConsumerService,
)


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _make_club(club_id: int, name: str):
    club = MagicMock()
    club.id = club_id
    club.name = name
    return club


def _make_db(club=None):
    """Return a mock SessionLocal context-manager that yields a fake session."""
    db = MagicMock()
    db.get.return_value = None  # default: entity not found → will add
    db.query.return_value.filter.return_value.first.return_value = club
    db.__enter__ = lambda s: db
    db.__exit__ = MagicMock(return_value=False)
    return db


# ---------------------------------------------------------------------------
# Announcement handler — club lookup
# ---------------------------------------------------------------------------

class TestHandleAnnouncementCreated:
    def test_prefers_club_id_over_name(self):
        """ClubId in payload → db.get() used, name-based query not called."""
        club = _make_club(42, "Club Robotique")
        db = _make_db(club=club)
        db.get.side_effect = lambda model, pk: club if pk == 42 else None

        with (
            patch("app.services.kafka_consumer.SessionLocal", return_value=db),
            patch("app.services.kafka_consumer.knowledge_service") as mock_ks,
        ):
            _handle_announcement_created({
                "AnnouncementId": 1,
                "Title": "Test",
                "Content": "Body",
                "ClubId": 42,
                "ClubName": "Wrong Name",
            })

        db.get.assert_any_call(MagicMock.__class__, 42)
        mock_ks.index_announcement_by_id.assert_called_once_with(1)

    def test_falls_back_to_name_when_no_club_id(self):
        """When ClubId is absent, name-based query is used."""
        club = _make_club(5, "Club IA")
        db = _make_db(club=club)

        with (
            patch("app.services.kafka_consumer.SessionLocal", return_value=db),
            patch("app.services.kafka_consumer.knowledge_service") as mock_ks,
        ):
            _handle_announcement_created({
                "AnnouncementId": 2,
                "Title": "Ann",
                "Content": "Content",
                "ClubName": "Club IA",
            })

        db.query.return_value.filter.return_value.first.assert_called()
        mock_ks.index_announcement_by_id.assert_called_once_with(2)

    def test_skips_when_club_not_found(self):
        """If neither ClubId nor ClubName resolves, message is skipped."""
        db = _make_db(club=None)
        db.get.return_value = None

        with (
            patch("app.services.kafka_consumer.SessionLocal", return_value=db),
            patch("app.services.kafka_consumer.knowledge_service") as mock_ks,
        ):
            _handle_announcement_created({
                "AnnouncementId": 3,
                "Title": "Ann",
                "Content": "Content",
                "ClubId": 99,
            })

        mock_ks.index_announcement_by_id.assert_not_called()

    def test_skips_when_missing_announcement_id(self):
        db = _make_db()
        with patch("app.services.kafka_consumer.SessionLocal", return_value=db):
            _handle_announcement_created({"Title": "No ID"})
        db.commit.assert_not_called()


# ---------------------------------------------------------------------------
# Event handler — complete fields
# ---------------------------------------------------------------------------

class TestHandleEventCreated:
    def test_ingests_is_public_and_tags(self):
        """is_public and tags must be stored on the Event row."""
        club = _make_club(1, "Club Dev")
        db = _make_db(club=club)
        db.get.side_effect = lambda model, pk: club if pk == 1 else None

        captured_event = {}

        def fake_add(obj):
            captured_event.update({
                "is_public": obj.is_public,
                "tags": obj.tags,
            })

        db.add.side_effect = fake_add

        with (
            patch("app.services.kafka_consumer.SessionLocal", return_value=db),
            patch("app.services.kafka_consumer.knowledge_service"),
        ):
            _handle_event_created({
                "EventId": 10,
                "Title": "Workshop",
                "ClubId": 1,
                "IsPublic": False,
                "Tags": ["python", "dev"],
            })

        assert captured_event["is_public"] is False
        assert captured_event["tags"] == ["python", "dev"]

    def test_prefers_club_id_over_name(self):
        club = _make_club(7, "Club Cybersec")
        db = _make_db(club=club)
        db.get.side_effect = lambda model, pk: club if pk == 7 else None

        with (
            patch("app.services.kafka_consumer.SessionLocal", return_value=db),
            patch("app.services.kafka_consumer.knowledge_service") as mock_ks,
        ):
            _handle_event_created({
                "EventId": 20,
                "Title": "CTF",
                "ClubId": 7,
                "ClubName": "Wrong",
            })

        mock_ks.index_event_by_id.assert_called_once_with(20)

    def test_skips_when_club_not_found(self):
        db = _make_db(club=None)
        db.get.return_value = None

        with (
            patch("app.services.kafka_consumer.SessionLocal", return_value=db),
            patch("app.services.kafka_consumer.knowledge_service") as mock_ks,
        ):
            _handle_event_created({
                "EventId": 30,
                "Title": "Hackathon",
                "ClubId": 999,
            })

        mock_ks.index_event_by_id.assert_not_called()


# ---------------------------------------------------------------------------
# Kafka consumer — offset commitment discipline
# ---------------------------------------------------------------------------

class TestKafkaConsumerOffsets:
    """Offset is committed only after a successful handler; errors skip commit."""

    def _make_message(self, topic: str, value: bytes):
        msg = MagicMock()
        msg.topic.return_value = topic
        msg.value.return_value = value
        msg.key.return_value = b"key"
        msg.error.return_value = None
        return msg

    def _run_one_message(self, msg, handler_side_effect=None):
        """Spin up a KafkaConsumerService with a fake Consumer that yields one message."""
        import json
        from confluent_kafka import KafkaError

        fake_consumer = MagicMock()
        # poll() returns the message once, then None to trigger stop_event check
        poll_calls = [msg, None]
        fake_consumer.poll.side_effect = poll_calls

        service = KafkaConsumerService()
        # After the first poll loop iteration, stop the consumer.
        original_is_set = service._stop_event.is_set

        call_count = [0]

        def patched_is_set():
            call_count[0] += 1
            # Stop after 2 iterations (message + None)
            return call_count[0] > 2

        service._stop_event.is_set = patched_is_set

        with (
            patch("app.services.kafka_consumer.Consumer", return_value=fake_consumer),
            patch("app.services.kafka_consumer._HANDLERS", {
                msg.topic(): (
                    MagicMock(side_effect=handler_side_effect)
                    if handler_side_effect
                    else MagicMock()
                )
            }) as patched_handlers,
        ):
            service._run()

        return fake_consumer, patched_handlers

    def test_commits_offset_after_successful_handler(self):
        msg = self._make_message("clubs-topic", b'{"ClubId": 1}')
        consumer, _ = self._run_one_message(msg)
        consumer.commit.assert_called_once_with(message=msg, asynchronous=False)

    def test_does_not_commit_offset_on_handler_failure(self):
        msg = self._make_message("clubs-topic", b'{"ClubId": 1}')
        consumer, _ = self._run_one_message(msg, handler_side_effect=RuntimeError("db error"))
        consumer.commit.assert_not_called()
