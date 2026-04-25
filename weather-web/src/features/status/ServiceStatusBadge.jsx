import { useEffect, useState } from 'react';
import { getServiceStatus } from './statusApi.js';
import { formatDateTime } from '../../lib/date/formatDate.js';

export function ServiceStatusBadge() {
  const [status, setStatus] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    let cancelled = false;
    getServiceStatus()
      .then((data) => {
        if (!cancelled) setStatus(data);
      })
      .catch(() => {
        if (!cancelled) setError('Service unreachable');
      });
    return () => {
      cancelled = true;
    };
  }, []);

  if (error) {
    return <span className="status-badge status-badge-down">{error}</span>;
  }

  if (!status) {
    return <span className="status-badge status-badge-unknown">Checking...</span>;
  }

  const tone =
    status.status === 'Healthy'
      ? 'status-badge-ok'
      : status.databaseAvailable
        ? 'status-badge-warn'
        : 'status-badge-down';

  return (
    <span className={`status-badge ${tone}`} title={`Server time: ${formatDateTime(status.serverTimeUtc)}`}>
      {status.status} - DB {status.databaseAvailable ? 'up' : 'down'}
    </span>
  );
}
