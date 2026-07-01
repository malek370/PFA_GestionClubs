from functools import lru_cache

from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Application configuration, externalised via environment variables.

    Mirrors the original Spring Boot ``application.yml`` with safe local defaults.
    """

    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8", extra="ignore")

    # PostgreSQL (relational data: FAQs + chat logs)
    database_url: str = "postgresql+psycopg://uniclubs:uniclubs@localhost:5432/uniclubs"

    # Google Gemini
    gemini_api_key: str = ""
    gemini_chat_model: str = "gemini-2.5-flash"
    # Fallback chat models (comma-separated) tried when the primary model is
    # overloaded (HTTP 503) or rate-limited (HTTP 429) on the free tier.
    gemini_fallback_chat_models: str = "gemini-2.0-flash,gemini-2.0-flash-lite"
    gemini_chat_temperature: float = 0.3
    gemini_embed_model: str = "gemini-embedding-001"

    # Vector database (ChromaDB)
    chroma_path: str = "./chroma_data"
    chroma_collection: str = "faqs"

    # Server
    server_host: str = "127.0.0.1"
    server_port: int = 8083

    # Security — RSA/RS256: public key fetched from IdentityProvider JWKS endpoint
    idp_jwks_url: str = "http://localhost:7253/.well-known/jwks"
    jwt_issuer: str = "IdentityProvider"
    jwt_audience: str = "myappusers"


@lru_cache
def get_settings() -> Settings:
    return Settings()
