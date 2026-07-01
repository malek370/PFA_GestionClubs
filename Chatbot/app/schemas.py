import uuid
from datetime import datetime

from pydantic import BaseModel, ConfigDict, Field


# ---- Requests (DTOs in) ----

class ChatAskRequest(BaseModel):
    message: str | None = None
    context: str | None = None


class FAQRequest(BaseModel):
    question: str = Field(min_length=1, max_length=1000)
    answer: str = Field(min_length=1, max_length=5000)
    category: str | None = Field(default=None, max_length=100)


# ---- Responses (DTOs out) ----

class SuggestedAction(BaseModel):
    label: str
    value: str


class ChatAskResponse(BaseModel):
    answer: str
    suggestedActions: list[SuggestedAction]
    escalate: bool


class FAQResponse(BaseModel):
    model_config = ConfigDict(from_attributes=True)

    id: uuid.UUID
    question: str
    answer: str
    category: str | None
    created_at: datetime


class ChatLogResponse(BaseModel):
    model_config = ConfigDict(from_attributes=True)

    id: uuid.UUID
    user_id: str | None
    session_id: str | None
    user_message: str
    bot_answer: str
    escalated: bool
    feedback: str | None
    created_at: datetime
