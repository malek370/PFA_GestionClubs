import { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import apiClient from '../../lib/apiClient';
import Pagination from '../../components/Pagination';
import type { ClubEvent, PagedResult } from '../../types';

const PAGE_SIZE = 9;

export default function EventListPage() {
  const [events, setEvents] = useState<ClubEvent[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [tagInput, setTagInput] = useState('');
  const [activeTags, setActiveTags] = useState('');
  const [loading, setLoading] = useState(true);

  const fetchEvents = useCallback(async () => {
    setLoading(true);
    try {
      const { data } = await apiClient.get<PagedResult<ClubEvent>>(
        '/clubs/api/events',
        {
          params: {
            Tags: activeTags || '',
            pageNumber: page,
            pageSize: PAGE_SIZE,
          },
        }
      );
      setEvents(data.items);
      setTotalCount(data.totalCount);
    } finally {
      setLoading(false);
    }
  }, [activeTags, page]);

  useEffect(() => { fetchEvents(); }, [fetchEvents]);

  function handleSearch(e: React.FormEvent) {
    e.preventDefault();
    setActiveTags(tagInput);
    setPage(1);
  }

  return (
    <div>
      <h2 className="fw-bold mb-4">Events</h2>

      <form onSubmit={handleSearch} className="d-flex gap-2 mb-4">
        <input
          type="text"
          className="form-control"
          placeholder="Filter by tags (semicolon-separated)…"
          value={tagInput}
          onChange={(e) => setTagInput(e.target.value)}
        />
        <button type="submit" className="btn btn-outline-primary px-4">
          Filter
        </button>
      </form>

      {loading ? (
        <div className="text-center py-5">
          <span className="spinner-border text-primary" />
        </div>
      ) : events.length === 0 ? (
        <div className="text-center text-muted py-5">No events found.</div>
      ) : (
        <div className="row g-3">
          {events.map((ev) => (
            <div key={ev.id} className="col-12 col-md-6 col-lg-4">
              <div className="card h-100 shadow-sm border-0">
                <div className="card-body d-flex flex-column">
                  <h5 className="card-title fw-semibold">{ev.title}</h5>
                  <p className="card-text text-muted small flex-grow-1">
                    {ev.description}
                  </p>
                  <div className="small text-muted mb-1">
                    <strong>Club:</strong> {ev.clubName}
                  </div>
                  <div className="small text-muted mb-2">
                    <strong>Date:</strong>{' '}
                    {new Date(ev.startDate).toLocaleString()}
                  </div>
                  {ev.location && (
                    <div className="small text-muted mb-2">
                      <strong>Location:</strong> {ev.location}
                    </div>
                  )}
                  <Link
                    to={`/events/${ev.id}`}
                    className="btn btn-outline-primary btn-sm mt-auto"
                  >
                    View
                  </Link>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      <div className="mt-4">
        <Pagination
          currentPage={page}
          totalCount={totalCount}
          pageSize={PAGE_SIZE}
          onPageChange={setPage}
        />
      </div>
    </div>
  );
}
