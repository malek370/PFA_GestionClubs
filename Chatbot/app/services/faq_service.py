from sqlalchemy import select
from sqlalchemy.orm import Session

from app.models import FAQ
from app.schemas import FAQRequest
from app.services.faq_indexing_service import faq_indexing_service


class FaqService:
    """CRUD for FAQs, with automatic re-indexing on create."""

    def find_all(self, db: Session) -> list[FAQ]:
        return list(
            db.execute(select(FAQ).order_by(FAQ.created_at.desc())).scalars().all()
        )

    def create(self, db: Session, req: FAQRequest) -> FAQ:
        faq = FAQ(question=req.question, answer=req.answer, category=req.category)
        db.add(faq)
        db.commit()
        db.refresh(faq)
        faq_indexing_service.index_faq(faq)
        return faq


faq_service = FaqService()
