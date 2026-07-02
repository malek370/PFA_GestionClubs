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
    idp_verify_ssl: bool = True  # set False when IdP uses a self-signed cert (containers)
    jwt_issuer: str = "IdentityProvider"
    jwt_audience: str = "myappusers"

    # Kafka consumer
    kafka_enabled: bool = True
    kafka_bootstrap_servers: str = "localhost:9092"
    kafka_consumer_group_id: str = "chatbot-group"
    kafka_topics_user_registered: str = "user-registered"
    kafka_topics_clubs: str = "clubs-topic"
    kafka_topics_announcements: str = "announcements-topic"
    kafka_topics_events: str = "events-topic"
    kafka_topics_user_promoted_admin: str = "user-promoted-to-club-admin"
    kafka_topics_user_promoted_member: str = "user-promoted-to-club-member"


@lru_cache
def get_settings() -> Settings:
    return Settings()
