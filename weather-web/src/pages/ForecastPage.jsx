import { useCallback, useEffect, useMemo, useState } from 'react';
import { LocationSelector } from '../components/weather/LocationSelector.jsx';
import { ForecastPanel } from '../features/forecast/ForecastPanel.jsx';
import { useLocations } from '../features/locations/useLocations.js';
import { getForecast } from '../features/forecast/forecastApi.js';
import { getErrorMessage } from '../lib/api/apiErrors.js';

const DEFAULT_LOCATION = 'Ang Mo Kio';
const FALLBACK_REGION = 'central';

export function ForecastPage() {
  const { locations, loading: locationsLoading, error: locationsError } = useLocations();
  const [selectedLocation, setSelectedLocation] = useState(DEFAULT_LOCATION);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [forecasts, setForecasts] = useState({
    twoHour: null,
    twentyFourHour: null,
    fourDay: null
  });

  const selectedLocationDetails = useMemo(() => {
    return locations?.find((location) => location.name === selectedLocation);
  }, [locations, selectedLocation]);

  const selectedRegion = selectedLocationDetails?.region || FALLBACK_REGION;

  const loadForecasts = useCallback(async () => {
    if (!selectedLocation) return;

    setLoading(true);
    setError('');

    try {
      const [twoHour, twentyFourHour, fourDay] = await Promise.all([
        getForecast({ type: 'two-hour', location: selectedLocation }),
        getForecast({ type: 'twenty-four-hour', region: selectedRegion }),
        getForecast({ type: 'four-day' })
      ]);

      setForecasts({ twoHour, twentyFourHour, fourDay });
    } catch (requestError) {
      setForecasts({ twoHour: null, twentyFourHour: null, fourDay: null });
      setError(getErrorMessage(requestError));
    } finally {
      setLoading(false);
    }
  }, [selectedLocation, selectedRegion]);

  useEffect(() => {
    loadForecasts();
  }, [loadForecasts]);

  const locationSubtitle = selectedLocation ? `For ${selectedLocation}` : 'Select a forecast area';

  return (
    <div className="page">
      <header className="page-header">
        <div>
          <p className="page-eyebrow">Forecast</p>
          <h2 className="page-title">Singapore forecasts</h2>
          <p className="page-subtitle">
            Compare area, regional, and national outlooks from data.gov.sg.
          </p>
        </div>
      </header>

      <div className="toolbar">
        <LocationSelector
          locations={locations}
          loading={locationsLoading}
          error={locationsError}
          value={selectedLocation}
          onChange={setSelectedLocation}
          locationType="ForecastArea"
          label="Forecast area"
          placeholder="Select a forecast area"
        />
        <button type="button" onClick={loadForecasts} disabled={loading}>
          {loading ? 'Refreshing...' : 'Refresh'}
        </button>
      </div>

      {error && <p className="page-error">{error}</p>}

      <div className="grid forecast-grid">
        <ForecastPanel
          title="2-hour forecast"
          subtitle={locationSubtitle}
          data={forecasts.twoHour}
          loading={loading}
          error=""
          onRetry={loadForecasts}
          emptyMessage="No 2-hour forecast available for this area."
        />
        <ForecastPanel
          title="24-hour forecast"
          subtitle={locationSubtitle}
          data={forecasts.twentyFourHour}
          loading={loading}
          error=""
          onRetry={loadForecasts}
          emptyMessage="No 24-hour forecast available for this area."
        />
        <ForecastPanel
          title="4-day outlook"
          subtitle="Singapore-wide"
          data={forecasts.fourDay}
          loading={loading}
          error=""
          onRetry={loadForecasts}
          emptyMessage="No 4-day outlook available."
        />
      </div>
    </div>
  );
}
