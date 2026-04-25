import React from 'react';
import { createRoot } from 'react-dom/client';
import axios from 'axios';
import './styles/global.css';

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5228';

function App() {
  const [status, setStatus] = React.useState(null);
  const [error, setError] = React.useState('');
  const [loading, setLoading] = React.useState(false);

  async function loadStatus() {
    setLoading(true);
    setError('');

    try {
      const response = await axios.get(`${apiBaseUrl}/api/status`);
      setStatus(response.data);
    } catch (requestError) {
      const response = requestError.response;
      if (response?.data) {
        setStatus(response.data);
        setError('weather-service responded with a degraded status.');
      } else {
        setStatus(null);
        setError('Could not reach weather-service.');
      }
    } finally {
      setLoading(false);
    }
  }

  React.useEffect(() => {
    loadStatus();
  }, []);

  return (
    <main className="shell">
      <section className="hero">
        <div>
          <p className="eyebrow">WeatherApp</p>
          <h1>weather-web is connected to weather-service</h1>
          <p className="lede">
            This first end-to-end slice checks the API and SQL Server connection
            before the weather features are added.
          </p>
        </div>
        <button onClick={loadStatus} disabled={loading}>
          {loading ? 'Checking...' : 'Refresh'}
        </button>
      </section>

      <section className="status-panel">
        <div className="metric">
          <span>API base URL</span>
          <strong>{apiBaseUrl}</strong>
        </div>
        <div className="metric">
          <span>Service</span>
          <strong>{status?.service || 'Unknown'}</strong>
        </div>
        <div className="metric">
          <span>Service status</span>
          <strong>{status?.status || 'Unknown'}</strong>
        </div>
        <div className="metric">
          <span>Database</span>
          <strong>{status?.databaseAvailable ? 'Connected' : 'Not connected'}</strong>
        </div>
        <div className="metric">
          <span>Server time UTC</span>
          <strong>{status?.serverTimeUtc || 'Unknown'}</strong>
        </div>
      </section>

      {error && <p className="error">{error}</p>}
    </main>
  );
}

createRoot(document.getElementById('root')).render(<App />);
