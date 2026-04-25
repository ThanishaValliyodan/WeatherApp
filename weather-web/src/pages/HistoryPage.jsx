import { useCallback, useMemo, useState } from 'react';
import { Card } from '../components/ui/Card.jsx';
import { ErrorState } from '../components/ui/ErrorState.jsx';
import { LoadingState } from '../components/ui/LoadingState.jsx';
import { LocationSelector } from '../components/weather/LocationSelector.jsx';
import { HistoricalWeatherChart } from '../features/historical-weather/HistoricalWeatherChart.jsx';
import { HistoricalWeatherTable } from '../features/historical-weather/HistoricalWeatherTable.jsx';
import { getHistoricalWeather } from '../features/historical-weather/historicalWeatherApi.js';
import { useLocations } from '../features/locations/useLocations.js';
import { downloadWeatherCsv } from '../features/weather-export/weatherExportApi.js';
import { getErrorMessage } from '../lib/api/apiErrors.js';
import { toInputDate } from '../lib/date/formatDate.js';

const DEFAULT_LOCATION = 'Ang Mo Kio';

function daysAgo(days) {
  const date = new Date();
  date.setDate(date.getDate() - days);
  return toInputDate(date);
}

export function HistoryPage() {
  const { locations, loading: locationsLoading, error: locationsError } = useLocations();
  const [location, setLocation] = useState(DEFAULT_LOCATION);
  const [from, setFrom] = useState(daysAgo(7));
  const [to, setTo] = useState(toInputDate(new Date()));
  const [history, setHistory] = useState(null);
  const [loading, setLoading] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [error, setError] = useState('');
  const [exportError, setExportError] = useState('');

  const records = history?.records || [];

  const summary = useMemo(() => {
    const metricTypes = new Set(records.map((record) => record.metricType));
    return {
      records: records.length,
      metricTypes: metricTypes.size,
      from,
      to
    };
  }, [records, from, to]);

  const loadHistory = useCallback(async () => {
    if (!location || !from || !to) return;
    setLoading(true);
    setError('');

    try {
      const data = await getHistoricalWeather({ location, from, to });
      setHistory(data);
    } catch (requestError) {
      setHistory(null);
      setError(getErrorMessage(requestError));
    } finally {
      setLoading(false);
    }
  }, [location, from, to]);

  const exportCsv = useCallback(async () => {
    if (!location || !from || !to) return;
    setExporting(true);
    setExportError('');

    try {
      await downloadWeatherCsv({ location, from, to });
    } catch (requestError) {
      setExportError(getErrorMessage(requestError));
    } finally {
      setExporting(false);
    }
  }, [location, from, to]);

  return (
    <div className="page">
      <header className="page-header">
        <div>
          <p className="page-eyebrow">History</p>
          <h2 className="page-title">Historical weather</h2>
          <p className="page-subtitle">
            Query stored readings by location and date range, then export the same result set as CSV.
          </p>
        </div>
      </header>

      <div className="toolbar history-toolbar">
        <LocationSelector
          locations={locations}
          loading={locationsLoading}
          error={locationsError}
          value={location}
          onChange={setLocation}
          locationType="ForecastArea"
          label="Location"
          placeholder="Select a location"
        />
        <div className="field">
          <label htmlFor="history-from">From</label>
          <input id="history-from" type="date" value={from} onChange={(event) => setFrom(event.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="history-to">To</label>
          <input id="history-to" type="date" value={to} onChange={(event) => setTo(event.target.value)} />
        </div>
        <div className="button-group">
          <button type="button" onClick={loadHistory} disabled={loading}>
            {loading ? 'Loading...' : 'Search'}
          </button>
          <button type="button" className="ghost-action" onClick={exportCsv} disabled={exporting}>
            {exporting ? 'Exporting...' : 'Export CSV'}
          </button>
        </div>
      </div>

      {exportError && <p className="page-error">{exportError}</p>}

      <div className="summary-strip">
        <div>
          <span>Records</span>
          <strong>{summary.records}</strong>
        </div>
        <div>
          <span>Metric types</span>
          <strong>{summary.metricTypes}</strong>
        </div>
        <div>
          <span>Range</span>
          <strong>
            {summary.from} to {summary.to}
          </strong>
        </div>
      </div>

      <div className="grid grid-two">
        <Card title="Metric trend" subtitle="Latest 24 values per metric">
          {loading && <LoadingState message="Loading history..." />}
          {!loading && error && <ErrorState message={error} onRetry={loadHistory} />}
          {!loading && !error && <HistoricalWeatherChart records={records} />}
        </Card>
        <Card title="Stored records" subtitle={location}>
          {loading && <LoadingState message="Loading records..." />}
          {!loading && error && <ErrorState message={error} onRetry={loadHistory} />}
          {!loading && !error && <HistoricalWeatherTable records={records} />}
        </Card>
      </div>
    </div>
  );
}
