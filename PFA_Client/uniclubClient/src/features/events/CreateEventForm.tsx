import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import apiClient from '../../lib/apiClient';
import { useToast } from '../../components/ToastProvider';

export default function CreateEventForm() {
  const { clubId } = useParams<{ clubId: string }>();
  const navigate = useNavigate();
  const { showToast } = useToast();

  const [form, setForm] = useState({
    title: '',
    description: '',
    location: '',
    startDate: '',
    tags: '',
    isPublic: false,
  });
  const [loading, setLoading] = useState(false);

  function handleChange(
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) {
    const target = e.target as HTMLInputElement;
    setForm((prev) => ({
      ...prev,
      [target.name]:
        target.type === 'checkbox' ? target.checked : target.value,
    }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    try {
      await apiClient.post('/clubs/api/events', {
        clubId: Number(clubId),
        title: form.title,
        description: form.description,
        location: form.location || undefined,
        startDate: new Date(form.startDate).toISOString(),
        tags: form.tags
          ? form.tags.split(',').map((t) => t.trim()).filter(Boolean)
          : [],
        isPublic: form.isPublic,
      });
      showToast('Event created!', 'success');
      navigate(`/clubs/${clubId}`);
    } catch {
      showToast('Failed to create event.', 'danger');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="row justify-content-center">
      <div className="col-12 col-md-8 col-lg-6">
        <h2 className="fw-bold mb-4">Create Event</h2>
        <div className="card shadow-sm border-0">
          <div className="card-body p-4">
            <form onSubmit={handleSubmit}>
              <div className="mb-3">
                <label className="form-label">Title</label>
                <input
                  name="title"
                  type="text"
                  className="form-control"
                  value={form.title}
                  onChange={handleChange}
                  required
                />
              </div>
              <div className="mb-3">
                <label className="form-label">Description</label>
                <textarea
                  name="description"
                  className="form-control"
                  value={form.description}
                  onChange={handleChange}
                  required
                  rows={3}
                />
              </div>
              <div className="mb-3">
                <label className="form-label">
                  Location <span className="text-muted small">(optional)</span>
                </label>
                <input
                  name="location"
                  type="text"
                  className="form-control"
                  value={form.location}
                  onChange={handleChange}
                />
              </div>
              <div className="mb-3">
                <label className="form-label">Start Date & Time</label>
                <input
                  name="startDate"
                  type="datetime-local"
                  className="form-control"
                  value={form.startDate}
                  onChange={handleChange}
                  required
                />
              </div>
              <div className="mb-3">
                <label className="form-label">
                  Tags{' '}
                  <span className="text-muted small">(comma-separated, optional)</span>
                </label>
                <input
                  name="tags"
                  type="text"
                  className="form-control"
                  value={form.tags}
                  onChange={handleChange}
                  placeholder="sports, culture, tech"
                />
              </div>
              <div className="form-check mb-4">
                <input
                  id="isPublic"
                  name="isPublic"
                  type="checkbox"
                  className="form-check-input"
                  checked={form.isPublic}
                  onChange={handleChange}
                />
                <label htmlFor="isPublic" className="form-check-label">
                  Public event
                </label>
              </div>
              <div className="d-flex gap-2">
                <button
                  type="submit"
                  className="btn btn-primary"
                  disabled={loading}
                >
                  {loading && (
                    <span className="spinner-border spinner-border-sm me-2" />
                  )}
                  Create Event
                </button>
                <button
                  type="button"
                  className="btn btn-outline-secondary"
                  onClick={() => navigate(`/clubs/${clubId}`)}
                >
                  Cancel
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>
    </div>
  );
}
