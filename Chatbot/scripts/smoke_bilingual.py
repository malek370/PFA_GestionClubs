"""Bilingual smoke test against the running RAG chatbot (real Gemini + Chroma).

Run with the server already started:
    $env:PYTHONPATH="."; python scripts/smoke_bilingual.py
"""
import json
import time
import urllib.request

URL = "http://127.0.0.1:8080/api/chatbot/ask"

QUESTIONS = [
    "Quels clubs sont disponibles sur la plateforme ?",
    "Qui est le president du Club Developpement Logiciel ?",
    "Who are the members of the Club Developpement Logiciel and what are their roles?",
    "Quels evenements organise le Club Developpement Logiciel ?",
    "Tell me in English about the hackathon event and its prize.",
    "How much is the membership fee and how do I pay it?",
    "Comment rejoindre un club ?",
]


def ask(message: str) -> dict:
    data = json.dumps({"message": message}).encode("utf-8")
    req = urllib.request.Request(
        URL, data=data,
        headers={"Content-Type": "application/json", "X-Session-Id": "smoke-1"},
    )
    with urllib.request.urlopen(req, timeout=90) as resp:
        return json.loads(resp.read().decode("utf-8"))


def main() -> None:
    for i, q in enumerate(QUESTIONS):
        r = ask(q)
        print("Q:", q)
        print("A:", r["answer"])
        print("escalate:", r["escalate"])
        print("-" * 70)
        if i < len(QUESTIONS) - 1:
            time.sleep(20)  # space out calls to respect the free-tier chat quota


if __name__ == "__main__":
    main()
