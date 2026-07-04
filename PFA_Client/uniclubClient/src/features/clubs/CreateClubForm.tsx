import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import apiClient from '../../lib/apiClient';
import { useToast } from '../../components/ToastProvider';

export default function CreateClubForm() {
  const navigate = useNavigate();
  const { showToast } = useToast();

  const [form, setForm] = useState({
    name: '',
    description: '',
    email: '',
    documents: '',
  });
  const [loading, setLoading] = useState(false);

  function handleChange(
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) {
    setForm((prev) => ({ ...prev, [e.target.name]: e.target.value }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    try {
      await apiClient.post('/clubs/api/clubs', {
        name: form.name,
        description: form.description,
        email: form.email,
        documents: form.documents
          ? form.documents.split(',').map((s) => s.trim()).filter(Boolean)
          : [],
      });
      showToast('Club created successfully!', 'success');
      navigate('/clubs');
    } catch {
      showToast('Failed to create club. Please check your input.', 'danger');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="row justify-content-center">
      <div className="col-12 col-md-8 col-lg-6">
        <h2 className="fw-bold mb-4">Create Club</h2>
        <div className="card shadow-sm border-0">
          <div className="card-body p-4">
            <form onSubmit={handleSubmit}>
              <div className="mb-3">
                <label className="form-label">Club Name</label>
                <input
                  name="name"
                  type="text"
                  className="form-control"
                  value={form.name}
                  onChange={handleChange}
                  required
                  minLength={3}
                  maxLength={100}
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
                  minLength={3}
                  maxLength={100}
                  rows={3}
                />
              </div>
              <div className="mb-3">
                <label className="form-label">Admin Email</label>
                <input
                  name="email"
                  type="email"
                  className="form-control"
                  value={form.email}
                  onChange={handleChange}
                  required
                />
                <div className="form-text">
                  This user will be assigned as the club&apos;s first President
                  and ClubAdmin.
                </div>
              </div>
              <div className="mb-4">
                <label className="form-label">
                  Documents{' '}
                  <span className="text-muted small">(comma-separated, optional)</span>
                </label>
                <input
                  name="documents"
                  type="text"
                  className="form-control"
                  value={form.documents}
                  onChange={handleChange}
                  placeholder="doc1.pdf, doc2.pdf"
                />
              </div>
              <div className="d-flex gap-2">
                <button
                  type="submit"
                  className="btn btn-primary"
                  disabled={loading}
                >
                  {loading ? (
                    <span className="spinner-border spinner-border-sm me-2" />
                  ) : null}
                  Create Club
                </button>
                <button
                  type="button"
                  className="btn btn-outline-secondary"
                  onClick={() => navigate('/clubs')}
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
