"""Manual end-to-end check: real Gemini embeddings + ChromaDB + Gemini Flash chat.

Run: python scripts/live_rag_check.py
"""
import uuid

from app.models import FAQ
from app.schemas import ChatAskRequest
from app.security import CurrentUser
from app.services.chatbot_service import chatbot_service
from app.services.faq_indexing_service import faq_indexing_service
from app.services.vector_store import vector_store


class FakeSession:
    def add(self, *a, **k):
        pass

    def commit(self):
        pass

    def rollback(self):
        pass

    def refresh(self, *a, **k):
        pass


faqs = [
    FAQ(
        id=uuid.uuid4(),
        question="Comment rejoindre un club ?",
        answer="Rendez-vous sur la page Clubs, choisissez un club et cliquez sur Rejoindre.",
        category="clubs",
    ),
    FAQ(
        id=uuid.uuid4(),
        question="Comment payer la cotisation ?",
        answer="La cotisation se paie en ligne par carte bancaire depuis votre profil.",
        category="paiement",
    ),
]

for f in faqs:
    faq_indexing_service.index_faq(f)
print("Indexed", len(faqs), "FAQs into ChromaDB; vector count =", vector_store.count())

resp = chatbot_service.ask(
    FakeSession(),
    ChatAskRequest(message="je voudrais rejoindre un club, comment faire ?", context=None),
    "sess-test",
    CurrentUser(),
)
print("\n=== Gemini RAG result ===")
print("ANSWER  :", resp.answer)
print("ESCALATE:", resp.escalate)
print("ACTIONS :", [(a.label, a.value) for a in resp.suggestedActions])
