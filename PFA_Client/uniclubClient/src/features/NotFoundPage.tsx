import { Link } from 'react-router-dom';

export default function NotFoundPage() {
  return (
    <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
      <div className="text-center">
        <h1 className="display-1 fw-bold text-primary">404</h1>
        <h2 className="mb-3">Page Not Found</h2>
        <p className="text-muted mb-4">
          The page you are looking for does not exist.
        </p>
        <Link to="/" className="btn btn-primary">
          Go Home
        </Link>
      </div>
    </div>
  );
}
