import { useCallback, useEffect, useMemo, useState } from 'react';
import { LocationSelector } from '../components/weather/LocationSelector.jsx';
import { ForecastPanel } from '../features/forecast/ForecastPanel.jsx';
import { useLocations } from '../features/locations/useLocations.js';
import { getForecast } from '../features/forecast/forecastApi.js';
import { getErrorMessage } from '../lib/api/apiErrors.js';

const DEFAULT_LOCATION = 'Ang Mo Kio';
const DEFAULT_REGION = 'west';
const REGIONS = ['central', 'east', 'north', 'south', 'west'];

function RegionSelector({ value, onChange }) {
  return (
    <div className="field">
      <label htmlFor="forecast-region">Region</label>
      <select id="forecast-region" value={value} onChange={(event) => onChange(event.target.value)}>
        {REGIONS.map((region) => (
          <option key={region} value={region}>
            {region[0].toUpperCase() + region.slice(1)}
          </option>
        ))}
      </select>
    </div>
  );
}

export function ForecastPage() {
  const { locations, loading: locationsLoading, error: locationsError } = useLocations();
  const [selectedLocation, setSelectedLocation] = useState(DEFAULT_LOCATION);
  const [selectedRegion, setSelectedRegion] = useState(DEFAULT_REGION);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [forecasts, setForecasts] = useState({
    twoHour: null,
    twentyFourHour: null,
    fourDay: null
  });

  const loadForecasts = useCallback(async () => {
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

  const locationSubtitle = useMemo(() => {
    return selectedLocation ? `For ${selectedLocation}` : 'Select a forecast area';
  }, [selectedLocation]);

  return (
    <div className="page">
      <header className="page-header">
        <div>
          <p className="page-eyebrow">Forecast</p>
          <h2 className="page-title">Singapore forecasts</h2>
          <p className="page-subtitle">
            Compare location, regional, and national outlooks from data.gov.sg.
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
          label="2-hour area"
          placeholder="Select a forecast area"
        />
        <RegionSelector value={selectedRegion} onChange={setSelectedRegion} />
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
          title="24-hour regional forecast"
          subtitle={`For ${selectedRegion}`}
          data={forecasts.twentyFourHour}
          loading={loading}
          error=""
          onRetry={loadForecasts}
          emptyMessage="No 24-hour forecast available for this region."
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
