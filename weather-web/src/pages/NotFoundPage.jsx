import { Link } from 'react-router-dom';
import { Card } from '../components/ui/Card.jsx';

export function NotFoundPage() {
  return (
    <div className="page">
      <Card title="Page not found">
        <p>The page you requested does not exist.</p>
        <Link to="/" className="ghost-button">
          Back to dashboard
        </Link>
      </Card>
    </div>
  );
}
