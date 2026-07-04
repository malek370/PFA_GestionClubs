import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import apiClient from '../../lib/apiClient';

export default function RegisterPage() {
  const navigate = useNavigate();

  const [form, setForm] = useState({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    confirmPassword: '',
  });
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [loading, setLoading] = useState(false);

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    setForm((prev) => ({ ...prev, [e.target.name]: e.target.value }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');

    if (form.password !== form.confirmPassword) {
      setError('Passwords do not match.');
      return;
    }

    setLoading(true);
    try {
      await apiClient.post('/identity/api/account/register', form);
      setSuccess('Account created! Redirecting to login…');
      setTimeout(() => navigate('/login'), 1500);
    } catch (err: unknown) {
      const msg =
        (err as { response?: { data?: { message?: string } } })?.response?.data
          ?.message ?? 'Registration failed. Please try again.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light py-4">
      <div className="card shadow-sm" style={{ width: '100%', maxWidth: '460px' }}>
        <div className="card-body p-4">
          <h4 className="card-title text-center mb-1 fw-bold text-primary">
            UniClub
          </h4>
          <p className="text-center text-muted mb-4 small">
            Create your account
          </p>

          {error && (
            <div className="alert alert-danger py-2 small">{error}</div>
          )}
          {success && (
            <div className="alert alert-success py-2 small">{success}</div>
          )}

          <form onSubmit={handleSubmit}>
            <div className="row g-3 mb-3">
              <div className="col-6">
                <label className="form-label">First Name</label>
                <input
                  name="firstName"
                  type="text"
                  className="form-control"
                  value={form.firstName}
                  onChange={handleChange}
                  required
                  autoFocus
                />
              </div>
              <div className="col-6">
                <label className="form-label">Last Name</label>
                <input
                  name="lastName"
                  type="text"
                  className="form-control"
                  value={form.lastName}
                  onChange={handleChange}
                  required
                />
              </div>
            </div>
            <div className="mb-3">
              <label className="form-label">Email</label>
              <input
                name="email"
                type="email"
                className="form-control"
                value={form.email}
                onChange={handleChange}
                required
              />
            </div>
            <div className="mb-3">
              <label className="form-label">Password</label>
              <input
                name="password"
                type="password"
                className="form-control"
                value={form.password}
                onChange={handleChange}
                required
              />
            </div>
            <div className="mb-4">
              <label className="form-label">Confirm Password</label>
              <input
                name="confirmPassword"
                type="password"
                className="form-control"
                value={form.confirmPassword}
                onChange={handleChange}
                required
              />
            </div>
            <button
              type="submit"
              className="btn btn-primary w-100"
              disabled={loading}
            >
              {loading ? (
                <>
                  <span className="spinner-border spinner-border-sm me-2" />
                  Creating account…
                </>
              ) : (
                'Create Account'
              )}
            </button>
          </form>

          <p className="text-center mt-3 mb-0 small text-muted">
            Already have an account? <Link to="/login">Sign in</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
