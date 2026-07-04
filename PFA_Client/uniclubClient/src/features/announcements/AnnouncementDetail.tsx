import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import apiClient from '../../lib/apiClient';
import type { Announcement } from '../../types';

export default function AnnouncementDetail() {
  const { id } = useParams<{ id: string }>();
  const [announcement, setAnnouncement] = useState<Announcement | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    apiClient
      .get<Announcement>(`/clubs/api/annoucements/${id}`)
      .then(({ data }) => setAnnouncement(data))
      .catch(() => setError('Announcement not found or access denied.'))
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) {
    return (
      <div className="text-center py-5">
        <span className="spinner-border text-primary" />
      </div>
    );
  }

  if (error || !announcement) {
    return <div className="alert alert-warning">{error || 'Not found.'}</div>;
  }

  return (
    <div className="row justify-content-center">
      <div className="col-12 col-lg-8">
        <Link to="/my-clubs" className="text-muted small mb-3 d-block">
          ← Back to My Clubs
        </Link>
        <div className="card shadow-sm border-0">
          <div className="card-body p-4">
            <div className="d-flex justify-content-between align-items-start mb-3">
              <h2 className="fw-bold mb-0">{announcement.title}</h2>
              <span className="badge bg-secondary">{announcement.clubName}</span>
            </div>
            <p className="text-body" style={{ whiteSpace: 'pre-wrap' }}>
              {announcement.content}
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
