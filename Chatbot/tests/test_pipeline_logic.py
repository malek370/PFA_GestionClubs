"""Unit tests for the pure RAG-pipeline helpers and JSON parsing.

These exercise the same logic as the original Spring ``ChatbotService`` without
touching PostgreSQL or OpenAI.
"""
from app.schemas import ChatAskResponse
from app.services import chatbot_service as cs
from app.services.chatbot_service import ChatbotService
from app.services.faq_indexing_service import RetrievedDoc


def test_contains_human_keyword():
    assert cs._contains_human_keyword("je veux parler à un humain")
    assert cs._contains_human_keyword("Contactez le SUPPORT svp")
    assert cs._contains_human_keyword("besoin d'un conseiller")
    assert not cs._contains_human_keyword("comment rejoindre un club ?")


def test_default_suggestions_shape():
    sugg = cs._default_suggestions()
    assert len(sugg) == 3
    assert all(s.label and s.value for s in sugg)


def test_build_context_block_empty():
    assert cs._build_context_block([]) == "(aucun document pertinent)"


def test_build_context_block_numbered():
    docs = [
        RetrievedDoc("1", "Q: a\nA: b", "a", "b", "cat", 0.9),
        RetrievedDoc("2", "Q: c\nA: d", "c", "d", "cat", 0.8),
    ]
    block = cs._build_context_block(docs)
    assert "[Doc 1]" in block
    assert "[Doc 2]" in block
    assert "Q: a" in block and "Q: c" in block


def test_extract_json_plain():
    assert cs._extract_json('{"answer": "x"}') == '{"answer": "x"}'


def test_extract_json_with_markdown_fence():
    raw = '```json\n{"answer": "x", "escalate": false}\n```'
    out = cs._extract_json(raw)
    assert out.startswith("{") and out.endswith("}")
    assert '"answer"' in out


def test_extract_json_embedded_text():
    raw = 'Voici la réponse: {"answer": "ok"} merci'
    assert cs._extract_json(raw) == '{"answer": "ok"}'


def test_extract_json_none():
    assert cs._extract_json(None) == "{}"


def test_parse_llm_json_valid():
    svc = ChatbotService()
    raw = (
        '{"answer": "Pour rejoindre un club...", "escalate": false,'
        ' "suggestedActions": [{"label": "Voir clubs", "value": "liste clubs"}]}'
    )
    resp = svc._parse_llm_json(raw)
    assert isinstance(resp, ChatAskResponse)
    assert resp.answer.startswith("Pour rejoindre")
    assert resp.escalate is False
    assert resp.suggestedActions[0].label == "Voir clubs"


def test_parse_llm_json_filters_incomplete_actions_and_falls_back():
    svc = ChatbotService()
    raw = '{"answer": "ok", "suggestedActions": [{"label": "", "value": "x"}]}'
    resp = svc._parse_llm_json(raw)
    # Incomplete action dropped -> default suggestions used
    assert len(resp.suggestedActions) == 3


def test_parse_llm_json_malformed_falls_back_to_plain_text():
    svc = ChatbotService()
    raw = "ceci n'est pas du JSON"
    resp = svc._parse_llm_json(raw)
    assert resp.answer == raw
    assert resp.escalate is False
    assert len(resp.suggestedActions) == 3
