import { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import apiClient from '../../lib/apiClient';
import { useToast } from '../../components/ToastProvider';
import Pagination from '../../components/Pagination';
import type { ClubEvent, PagedResult } from '../../types';

const PAGE_SIZE = 10;

export default function MyEventsPage() {
  const { showToast } = useToast();
  const [events, setEvents] = useState<ClubEvent[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);

  const fetchEvents = useCallback(async () => {
    setLoading(true);
    try {
      const { data } = await apiClient.get<PagedResult<ClubEvent>>(
        '/clubs/api/events/user-events',
        { params: { pageNumber: page, pageSize: PAGE_SIZE } }
      );
      setEvents(data.items);
      setTotalCount(data.totalCount);
    } finally {
      setLoading(false);
    }
  }, [page]);

  useEffect(() => { fetchEvents(); }, [fetchEvents]);

  async function handleLeave(eventId: number) {
    if (!confirm('Leave this event?')) return;
    try {
      await apiClient.put(`/clubs/api/events/${eventId}/leave`);
      showToast('Left event.', 'info');
      fetchEvents();
    } catch {
      showToast('Could not leave event.', 'danger');
    }
  }

  return (
    <div>
      <h2 className="fw-bold mb-4">My Events</h2>

      {loading ? (
        <div className="text-center py-5">
          <span className="spinner-border text-primary" />
        </div>
      ) : events.length === 0 ? (
        <div className="text-center text-muted py-5">
          You haven&apos;t joined any events yet.{' '}
          <Link to="/events">Browse events</Link>
        </div>
      ) : (
        <div className="table-responsive">
          <table className="table table-hover align-middle">
            <thead className="table-light">
              <tr>
                <th>Title</th>
                <th>Club</th>
                <th>Date</th>
                <th>Location</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {events.map((ev) => (
                <tr key={ev.id}>
                  <td>
                    <Link to={`/events/${ev.id}`} className="fw-semibold text-decoration-none">
                      {ev.title}
                    </Link>
                  </td>
                  <td>{ev.clubName}</td>
                  <td>{new Date(ev.startDate).toLocaleDateString()}</td>
                  <td>{ev.location ?? '—'}</td>
                  <td>
                    <button
                      className="btn btn-outline-danger btn-sm"
                      onClick={() => handleLeave(ev.id)}
                    >
                      Leave
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <Pagination
        currentPage={page}
        totalCount={totalCount}
        pageSize={PAGE_SIZE}
        onPageChange={setPage}
      />
    </div>
  );
}
