import { useState, useEffect, useRef } from 'react';
import apiClient from '../../lib/apiClient';
import { useAuthStore } from '../../stores/authStore';
import type { ChatbotResponse, Faq } from '../../types';

interface Message {
  from: 'user' | 'bot';
  text: string;
}

export default function ChatbotWidget() {
  const { accessToken } = useAuthStore();
  const [open, setOpen] = useState(false);
  const [tab, setTab] = useState<'chat' | 'faqs'>('chat');
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [faqs, setFaqs] = useState<Faq[]>([]);
  const [faqsLoading, setFaqsLoading] = useState(false);
  const [faqsError, setFaqsError] = useState(false);
  const [openFaqId, setOpenFaqId] = useState<number | null>(null);
  const sessionId = useRef(crypto.randomUUID());
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  function fetchFaqs() {
    setFaqsLoading(true);
    setFaqsError(false);
    apiClient
      .get<Faq[]>('/chatbot/api/chatbot/faqs')
      .then(({ data }) => setFaqs(data))
      .catch(() => setFaqsError(true))
      .finally(() => setFaqsLoading(false));
  }

  useEffect(() => {
    if (tab === 'faqs' && faqs.length === 0 && !faqsError) {
      fetchFaqs();
    }
  }, [tab]);

  if (!accessToken) return null;

  async function sendMessage(text: string) {
    if (!text.trim()) return;
    setMessages((prev) => [...prev, { from: 'user', text }]);
    setInput('');
    setLoading(true);
    try {
      const { data } = await apiClient.post<ChatbotResponse>(
        '/chatbot/api/chatbot/ask',
        { message: text },
        { headers: { 'X-Session-Id': sessionId.current } }
      );
      setMessages((prev) => [...prev, { from: 'bot', text: data.answer }]);

      if (data.suggestedActions?.length) {
        setMessages((prev) => [
          ...prev,
          {
            from: 'bot',
            text: '__SUGGESTIONS__' + JSON.stringify(data.suggestedActions),
          },
        ]);
      }
    } catch {
      setMessages((prev) => [
        ...prev,
        { from: 'bot', text: 'Sorry, something went wrong.' },
      ]);
    } finally {
      setLoading(false);
    }
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    sendMessage(input);
  }

  return (
    <div
      style={{
        position: 'fixed',
        bottom: '1.5rem',
        right: '1.5rem',
        zIndex: 1050,
      }}
    >
      {/* Toggle button */}
      {!open && (
        <button
          className="btn btn-primary rounded-circle shadow"
          style={{ width: 56, height: 56, fontSize: 24 }}
          onClick={() => setOpen(true)}
          aria-label="Open chatbot"
        >
          💬
        </button>
      )}

      {/* Widget */}
      {open && (
        <div
          className="card shadow-lg border-0"
          style={{ width: 340, maxHeight: 520 }}
        >
          {/* Header */}
          <div className="card-header bg-primary text-white d-flex justify-content-between align-items-center py-2">
            <span className="fw-semibold">UniClub Assistant</span>
            <button
              className="btn-close btn-close-white btn-sm"
              onClick={() => setOpen(false)}
            />
          </div>

          {/* Tabs */}
          <ul className="nav nav-tabs nav-fill border-0 bg-light">
            <li className="nav-item">
              <button
                className={`nav-link py-1 ${tab === 'chat' ? 'active' : ''}`}
                onClick={() => setTab('chat')}
              >
                Chat
              </button>
            </li>
            <li className="nav-item">
              <button
                className={`nav-link py-1 ${tab === 'faqs' ? 'active' : ''}`}
                onClick={() => setTab('faqs')}
              >
                FAQs
              </button>
            </li>
          </ul>

          {/* Chat tab */}
          {tab === 'chat' && (
            <>
              <div
                className="card-body p-2 overflow-y-auto"
                style={{ minHeight: 300, maxHeight: 340 }}
              >
                {messages.length === 0 && (
                  <p className="text-muted text-center small mt-3">
                    Ask me anything about UniClub!
                  </p>
                )}
                {messages.map((msg, i) => {
                  if (msg.text.startsWith('__SUGGESTIONS__')) {
                    const actions = JSON.parse(
                      msg.text.replace('__SUGGESTIONS__', '')
                    ) as { label: string; value: string }[];
                    return (
                      <div key={i} className="d-flex flex-wrap gap-1 mb-2">
                        {actions.map((a) => (
                          <button
                            key={a.value}
                            className="btn btn-outline-primary btn-sm"
                            onClick={() => sendMessage(a.value)}
                          >
                            {a.label}
                          </button>
                        ))}
                      </div>
                    );
                  }
                  return (
                    <div
                      key={i}
                      className={`d-flex mb-2 ${
                        msg.from === 'user'
                          ? 'justify-content-end'
                          : 'justify-content-start'
                      }`}
                    >
                      <div
                        className={`px-3 py-2 rounded-3 small ${
                          msg.from === 'user'
                            ? 'bg-primary text-white'
                            : 'bg-light text-dark border'
                        }`}
                        style={{ maxWidth: '80%' }}
                      >
                        {msg.text}
                      </div>
                    </div>
                  );
                })}
                {loading && (
                  <div className="d-flex justify-content-start mb-2">
                    <div className="px-3 py-2 rounded-3 small bg-light border">
                      <span className="spinner-border spinner-border-sm me-1" />
                      Thinking…
                    </div>
                  </div>
                )}
                <div ref={bottomRef} />
              </div>
              <div className="card-footer p-2">
                <form onSubmit={handleSubmit} className="d-flex gap-2">
                  <input
                    type="text"
                    className="form-control form-control-sm"
                    placeholder="Type a message…"
                    value={input}
                    onChange={(e) => setInput(e.target.value)}
                    disabled={loading}
                  />
                  <button
                    type="submit"
                    className="btn btn-primary btn-sm px-3"
                    disabled={loading || !input.trim()}
                  >
                    Send
                  </button>
                </form>
              </div>
            </>
          )}

          {/* FAQs tab */}
          {tab === 'faqs' && (
            <div
              className="card-body p-2 overflow-y-auto"
              style={{ minHeight: 360, maxHeight: 400 }}
            >
              {faqsLoading ? (
                <div className="text-center py-4">
                  <span className="spinner-border spinner-border-sm text-primary" />
                </div>
              ) : faqsError ? (
                <div className="text-center py-4">
                  <p className="text-danger small mb-2">Failed to load FAQs.</p>
                  <button
                    className="btn btn-outline-primary btn-sm"
                    onClick={fetchFaqs}
                  >
                    Retry
                  </button>
                </div>
              ) : faqs.length === 0 ? (
                <p className="text-muted text-center small mt-3">
                  No FAQs available.
                </p>
              ) : (
                <div className="d-flex flex-column gap-2 px-1">
                  {faqs.map((faq) => (
                    <div key={faq.id}>
                      {/* Question bubble – right aligned, acts as a toggle */}
                      <div className="d-flex justify-content-end mb-1">
                        <button
                          type="button"
                          onClick={() =>
                            setOpenFaqId(openFaqId === faq.id ? null : faq.id)
                          }
                          className="border-0 text-start px-3 py-2 rounded-3 small fw-medium"
                          style={{
                            maxWidth: '85%',
                            background: openFaqId === faq.id ? '#0d6efd' : '#e9ecef',
                            color: openFaqId === faq.id ? '#fff' : '#212529',
                            cursor: 'pointer',
                            transition: 'background 0.2s, color 0.2s',
                          }}
                        >
                          {faq.question}
                        </button>
                      </div>

                      {/* Answer bubble – left aligned, visible when open */}
                      {openFaqId === faq.id && (
                        <div className="d-flex justify-content-start mb-1">
                          <div
                            className="px-3 py-2 rounded-3 small border"
                            style={{
                              maxWidth: '85%',
                              background: '#f8f9fa',
                              color: '#212529',
                            }}
                          >
                            {faq.answer}
                            {faq.category && (
                              <span className="badge bg-light text-muted border ms-2 align-middle">
                                {faq.category}
                              </span>
                            )}
                          </div>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
