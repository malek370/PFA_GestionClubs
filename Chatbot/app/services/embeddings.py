import logging
import re
import time
from functools import lru_cache

from google import genai

from app.config import get_settings

log = logging.getLogger(__name__)

# Each input in a batch counts as one request against the per-minute quota.
# The free tier allows ~100 embed requests/minute, so we pace sub-batches to
# stay safely under that ceiling.
_SUB_BATCH = 20          # items per request
_PER_MINUTE_BUDGET = 90  # leave headroom below the 100/min free-tier limit
_MAX_RETRIES = 8
_DEFAULT_RETRY_DELAY = 30.0


@lru_cache
def get_genai_client() -> genai.Client:
    settings = get_settings()
    return genai.Client(api_key=settings.gemini_api_key)


def _retry_delay_from_error(err: Exception) -> float:
    """Extract the server-suggested retry delay (seconds) from a 429 error."""
    m = re.search(r"retry in (\d+(?:\.\d+)?)s", str(err))
    if m:
        return float(m.group(1)) + 1.0
    return _DEFAULT_RETRY_DELAY


def _is_rate_limit(err: Exception) -> bool:
    s = str(err)
    return "429" in s or "RESOURCE_EXHAUSTED" in s


def _embed_with_retry(client, model: str, contents):
    last_err: Exception | None = None
    for attempt in range(_MAX_RETRIES):
        try:
            return client.models.embed_content(model=model, contents=contents)
        except Exception as e:  # noqa: BLE001
            if not _is_rate_limit(e):
                raise
            last_err = e
            delay = _retry_delay_from_error(e)
            log.warning("Embedding rate-limited (attempt %d/%d); retrying in %.0fs",
                        attempt + 1, _MAX_RETRIES, delay)
            time.sleep(delay)
    raise last_err  # type: ignore[misc]


def embed_text(text: str) -> list[float]:
    """Generate an embedding vector for a single piece of text via Gemini."""
    settings = get_settings()
    client = get_genai_client()
    result = _embed_with_retry(client, settings.gemini_embed_model, text)
    return list(result.embeddings[0].values)


def embed_texts(texts: list[str]) -> list[list[float]]:
    """Batch-embed many texts, pacing requests to respect the free-tier quota."""
    settings = get_settings()
    client = get_genai_client()
    vectors: list[list[float]] = []
    requests_this_window = 0
    window_start = time.monotonic()

    for start in range(0, len(texts), _SUB_BATCH):
        chunk = texts[start:start + _SUB_BATCH]

        # Throttle: if we'd exceed the per-minute budget, wait for the window.
        if requests_this_window + len(chunk) > _PER_MINUTE_BUDGET:
            elapsed = time.monotonic() - window_start
            if elapsed < 60:
                wait = 60 - elapsed + 1
                log.info("Throttling embeddings: sleeping %.0fs to respect quota", wait)
                time.sleep(wait)
            requests_this_window = 0
            window_start = time.monotonic()

        result = _embed_with_retry(client, settings.gemini_embed_model, chunk)
        vectors.extend(list(e.values) for e in result.embeddings)
        requests_this_window += len(chunk)

    return vectors
