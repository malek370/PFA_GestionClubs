from typing import Annotated

from fastapi import APIRouter, Depends, Header, status
from sqlalchemy import select
from sqlalchemy.orm import Session

from app.db import get_db
from app.models import ChatLog
from app.schemas import (
    ChatAskRequest,
    ChatAskResponse,
    ChatLogResponse,
    FAQRequest,
    FAQResponse,
)
from app.security import CurrentUser, get_current_user, require_admin, require_authenticated
from app.services.chatbot_service import chatbot_service
from app.services.faq_service import faq_service

router = APIRouter(prefix="/api/chatbot", tags=["chatbot"])

DbSession = Annotated[Session, Depends(get_db)]
CurrentUserDep = Annotated[CurrentUser, Depends(get_current_user)]
AuthenticatedDep = Annotated[CurrentUser, Depends(require_authenticated)]
AdminDep = Annotated[CurrentUser, Depends(require_admin)]
SessionIdHeader = Annotated[str | None, Header(alias="X-Session-Id")]


# 1. Ask a question (authenticated)
@router.post("/ask")
def ask(
    body: ChatAskRequest,
    db: DbSession,
    user: AuthenticatedDep,
    x_session_id: SessionIdHeader = None,
) -> ChatAskResponse:
    return chatbot_service.ask(db, body, x_session_id, user)


# 2. List FAQs (authenticated)
@router.get("/faqs")
def list_faqs(db: DbSession, _: AuthenticatedDep) -> list[FAQResponse]:
    return [FAQResponse.model_validate(f) for f in faq_service.find_all(db)]


# 3. Create FAQ (admin)
@router.post("/faqs", status_code=status.HTTP_201_CREATED)
def create_faq(body: FAQRequest, db: DbSession, _: AdminDep) -> FAQResponse:
    created = faq_service.create(db, body)
    return FAQResponse.model_validate(created)


# 4. List chat logs (admin)
@router.get("/logs")
def list_logs(db: DbSession, _: AdminDep) -> list[ChatLogResponse]:
    logs = db.execute(select(ChatLog).order_by(ChatLog.created_at.desc())).scalars().all()
    return [ChatLogResponse.model_validate(log) for log in logs]
