import json
import logging
import re
import time
import uuid
from concurrent.futures import ThreadPoolExecutor, TimeoutError as FuturesTimeoutError

from sqlalchemy.orm import Session

from app.config import get_settings
from app.models import ChatLog
from app.schemas import ChatAskRequest, ChatAskResponse, SuggestedAction
from app.security import CurrentUser
from app.services.embeddings import get_genai_client
from app.services.faq_indexing_service import RetrievedDoc, faq_indexing_service

log = logging.getLogger(__name__)

# Allow time for transient 503/429 retries inside the chat call.
LLM_TIMEOUT_SECONDS = 45
TOP_K = 5
# Retry settings for transient chat-model errors (503 high demand, 429 quota).
_LLM_MAX_RETRIES = 4
_LLM_BACKOFF_SECONDS = 6.0

SYSTEM_PROMPT = """You are the assistant of the UniClubs platform. You help users with
questions about clubs, their members and roles, events, announcements and how the
platform works.

GROUNDING: Answer ONLY using the CONTEXT provided below (it contains real data
about clubs, members, events and announcements, written in French). If the
context does not contain the answer, politely say you don't know and offer to
escalate to a human (UniClubs support). Never invent clubs, people, dates or facts.

LANGUAGE: Detect the language of the USER QUESTION and ALWAYS reply in that SAME
language. If the question is in French, answer in French. If it is in English,
answer in English. Translate the relevant facts from the context as needed.
Keep answers concise and factual.

You MUST reply STRICTLY as valid JSON, with no surrounding text, using this schema:
{
  "answer": "string - the reply to the user, in the user's language",
  "escalate": boolean - true if you cannot answer or the user asks for a human,
  "suggestedActions": [
    { "label": "short string (max 40 chars)", "value": "string - a follow-up question" }
  ]
}
Provide 2 or 3 relevant suggestedActions, in the same language as the answer."""

USER_TEMPLATE = """CONTEXTE :
{context}

HISTORIQUE (optionnel) :
{history}

QUESTION UTILISATEUR :
{question}"""

_HUMAN_KEYWORDS = (
    "humain", "contact", "support", "conseiller", "agent",
    "human", "advisor", "representative", "real person",
)

_EN_HINTS = (
    " the ", " what ", " who ", " how ", " when ", " where ", " which ",
    " is ", " are ", " club", " event", " member", " president", " tell me",
    " list ", " show ", " give ", " can ", " do ", " does ",
)
_FR_HINTS = (
    " le ", " la ", " les ", " des ", " du ", " qui ", " quoi ", " quel",
    " quelle", " comment", " quand ", " est ", " sont ", " liste", " membre",
    " club", " evenement", " reunion", " annonce", " je ", " une ", " un ",
)

_executor = ThreadPoolExecutor(max_workers=4)


def _is_english(text: str) -> bool:
    """Lightweight heuristic to pick the reply language for fallback messages.

    The LLM itself detects the language for real answers; this only governs the
    canned fallbacks (empty input, no context found, timeouts, errors).
    """
    t = f" {(text or '').lower()} "
    en = sum(1 for w in _EN_HINTS if w in t)
    fr = sum(1 for w in _FR_HINTS if w in t)
    return en > fr


def _default_suggestions(english: bool = False) -> list[SuggestedAction]:
    if english:
        return [
            SuggestedAction(label="List the clubs", value="list the clubs"),
            SuggestedAction(label="How do I join a club?", value="how do I join a club"),
            SuggestedAction(label="Contact support", value="I want to talk to a human"),
        ]
    return [
        SuggestedAction(label="Voir les clubs", value="liste des clubs"),
        SuggestedAction(label="Comment postuler ?", value="comment postuler dans un club"),
        SuggestedAction(label="Contacter le support", value="je veux parler a un humain"),
    ]


def _contains_human_keyword(message: str) -> bool:
    m = message.lower()
    return any(k in m for k in _HUMAN_KEYWORDS)


def _build_context_block(docs: list[RetrievedDoc]) -> str:
    if not docs:
        return "(aucun document pertinent)"
    parts = []
    for i, d in enumerate(docs, start=1):
        parts.append(f"[Doc {i}]\n{d.content}\n")
    return "\n".join(parts)


def _extract_json(raw: str | None) -> str:
    if not raw:
        return "{}"
    s = raw.strip()
    # Strip ```json fences if the model added them
    if s.startswith("```"):
        first_nl = s.find("\n")
        if first_nl > 0:
            s = s[first_nl + 1:]
        if s.endswith("```"):
            s = s[:-3]
    start = s.find("{")
    end = s.rfind("}")
    if start >= 0 and end > start:
        return s[start:end + 1]
    return s


class ChatbotService:
    """Core RAG pipeline. Faithful port of the Spring ``ChatbotService``."""

    def ask(
        self,
        db: Session,
        request: ChatAskRequest,
        session_id: str | None,
        user: CurrentUser,
    ) -> ChatAskResponse:
        message = (request.message or "").strip()
        english = _is_english(message)
        if not message:
            return ChatAskResponse(
                answer=(
                    "Ask me a question and I'll do my best to help."
                    if english
                    else "Pose-moi une question et je ferai de mon mieux pour y repondre."
                ),
                suggestedActions=_default_suggestions(english),
                escalate=False,
            )

        # 1. Rule-based escalation: explicit request for a human
        escalate_by_keyword = _contains_human_keyword(message)

        # 2. Retrieve relevant documents (FAQs + clubs/events/announcements)
        docs = faq_indexing_service.search(message, TOP_K)
        context_block = _build_context_block(docs)

        if not docs:
            # No context at all -> escalate without burning an LLM call
            response = ChatAskResponse(
                answer=(
                    "I couldn't find information about this in our database. "
                    "Would you like to be connected with a UniClubs support member?"
                    if english
                    else "Je n'ai pas trouve d'information a ce sujet dans notre base. "
                    "Souhaitez-vous etre mis en relation avec un membre du support ?"
                ),
                suggestedActions=_default_suggestions(english),
                escalate=True,
            )
        else:
            response = self._call_llm_with_timeout(message, context_block, request.context)
            if escalate_by_keyword:
                response = ChatAskResponse(
                    answer=response.answer,
                    suggestedActions=response.suggestedActions,
                    escalate=True,
                )

        # 3. Log conversation
        try:
            db.add(
                ChatLog(
                    user_id=user.user_id,
                    session_id=session_id or str(uuid.uuid4()),
                    user_message=message,
                    bot_answer=response.answer,
                    escalated=response.escalate,
                )
            )
            db.commit()
        except Exception as e:  # noqa: BLE001
            db.rollback()
            log.warning("Could not persist chat log: %s", e)

        return response

    def _call_llm_with_timeout(self, question: str, context: str, history: str | None) -> ChatAskResponse:
        english = _is_english(question)
        future = _executor.submit(self._call_llm, question, context, history)
        try:
            return future.result(timeout=LLM_TIMEOUT_SECONDS)
        except FuturesTimeoutError:
            future.cancel()
            log.warning("LLM call timed out after %ss", LLM_TIMEOUT_SECONDS)
            return ChatAskResponse(
                answer=(
                    "The service is a bit slow right now. Would you like to be connected with a human?"
                    if english
                    else "Le service est momentanement lent. Souhaitez-vous etre mis en relation avec un humain ?"
                ),
                suggestedActions=_default_suggestions(english),
                escalate=True,
            )
        except Exception as e:  # noqa: BLE001
            log.error("LLM call failed", exc_info=e)
            return ChatAskResponse(
                answer=(
                    "Something went wrong. Would you like to be connected with a human?"
                    if english
                    else "Une erreur est survenue. Souhaitez-vous etre mis en relation avec un humain ?"
                ),
                suggestedActions=_default_suggestions(english),
                escalate=True,
            )

    def _call_llm(self, question: str, context: str, history: str | None) -> ChatAskResponse:
        from google.genai import types

        settings = get_settings()
        client = get_genai_client()
        user_msg = USER_TEMPLATE.format(
            context=context,
            history=history if history and history.strip() else "(aucun)",
            question=question,
        )
        config = types.GenerateContentConfig(
            system_instruction=SYSTEM_PROMPT,
            temperature=settings.gemini_chat_temperature,
            response_mime_type="application/json",
        )

        models = self._candidate_models(settings)
        last_err: Exception | None = None
        for model_name in models:
            for attempt in range(_LLM_MAX_RETRIES):
                try:
                    response = client.models.generate_content(
                        model=model_name,
                        contents=user_msg,
                        config=config,
                    )
                    return self._parse_llm_json(response.text)
                except Exception as e:  # noqa: BLE001
                    s = str(e)
                    transient = "503" in s or "UNAVAILABLE" in s or "429" in s or "RESOURCE_EXHAUSTED" in s
                    if not transient:
                        raise
                    last_err = e
                    # If overloaded, switch to the next fallback model quickly
                    # instead of waiting out the full backoff on this one.
                    if attempt == 0 and model_name != models[-1]:
                        log.warning("Chat model %s busy; trying fallback model.", model_name)
                        break
                    delay = self._retry_delay(s, attempt)
                    log.warning("Chat model %s busy (attempt %d/%d); retrying in %.0fs",
                                model_name, attempt + 1, _LLM_MAX_RETRIES, delay)
                    time.sleep(delay)
        raise last_err  # type: ignore[misc]

    @staticmethod
    def _candidate_models(settings) -> list[str]:
        models = [settings.gemini_chat_model]
        for name in (settings.gemini_fallback_chat_models or "").split(","):
            name = name.strip()
            if name and name not in models:
                models.append(name)
        return models

    @staticmethod
    def _retry_delay(err_text: str, attempt: int) -> float:
        m = re.search(r"retry in (\d+(?:\.\d+)?)s", err_text)
        if m:
            return float(m.group(1)) + 1.0
        return _LLM_BACKOFF_SECONDS * (attempt + 1)

    def _parse_llm_json(self, raw: str | None) -> ChatAskResponse:
        json_str = _extract_json(raw)
        try:
            node = json.loads(json_str)
            answer = node.get("answer") or "Sorry, I could not formulate an answer."
            escalate = bool(node.get("escalate", False))

            actions: list[SuggestedAction] = []
            for it in node.get("suggestedActions", []) or []:
                label = (it or {}).get("label", "")
                value = (it or {}).get("value", "")
                if label and value:
                    actions.append(SuggestedAction(label=label, value=value))
            if not actions:
                actions = _default_suggestions()
            return ChatAskResponse(answer=answer, suggestedActions=actions, escalate=escalate)
        except Exception:  # noqa: BLE001
            log.warning("Failed to parse LLM JSON, falling back to plain text. raw=%s", raw)
            return ChatAskResponse(
                answer=raw or "",
                suggestedActions=_default_suggestions(),
                escalate=False,
            )


chatbot_service = ChatbotService()
