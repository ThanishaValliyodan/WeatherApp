import { useCallback, useEffect, useState } from 'react';
import { useLocations } from '../features/locations/useLocations.js';
import { LocationSelector } from '../components/weather/LocationSelector.jsx';
import { CurrentWeatherPanel } from '../features/current-weather/CurrentWeatherPanel.jsx';
import { ForecastPanel } from '../features/forecast/ForecastPanel.jsx';
import { ServiceStatusBadge } from '../features/status/ServiceStatusBadge.jsx';
import { getCurrentWeather } from '../features/current-weather/currentWeatherApi.js';
import { getForecast } from '../features/forecast/forecastApi.js';
import { getErrorMessage } from '../lib/api/apiErrors.js';

const DEFAULT_LOCATION = 'Ang Mo Kio';

export function DashboardPage() {
  const { locations, loading: locationsLoading, error: locationsError } = useLocations();
  const [selectedLocation, setSelectedLocation] = useState(DEFAULT_LOCATION);

  const [currentWeather, setCurrentWeather] = useState(null);
  const [currentLoading, setCurrentLoading] = useState(false);
  const [currentError, setCurrentError] = useState('');

  const [forecast, setForecast] = useState(null);
  const [forecastLoading, setForecastLoading] = useState(false);
  const [forecastError, setForecastError] = useState('');

  const loadCurrent = useCallback(async (location) => {
    if (!location) return;
    setCurrentLoading(true);
    setCurrentError('');
    try {
      const data = await getCurrentWeather({ location });
      setCurrentWeather(data);
    } catch (requestError) {
      setCurrentWeather(null);
      setCurrentError(getErrorMessage(requestError));
    } finally {
      setCurrentLoading(false);
    }
  }, []);

  const loadForecast = useCallback(async (location) => {
    if (!location) return;
    setForecastLoading(true);
    setForecastError('');
    try {
      const data = await getForecast({ type: 'two-hour', location });
      setForecast(data);
    } catch (requestError) {
      setForecast(null);
      setForecastError(getErrorMessage(requestError));
    } finally {
      setForecastLoading(false);
    }
  }, []);

  useEffect(() => {
    loadCurrent(selectedLocation);
    loadForecast(selectedLocation);
  }, [selectedLocation, loadCurrent, loadForecast]);

  return (
    <div className="page page-dashboard">
      <header className="page-header">
        <div>
          <p className="page-eyebrow">Dashboard</p>
          <h2 className="page-title">Latest Singapore weather</h2>
          <p className="page-subtitle">
            Pick a forecast area to see live readings and the next two-hour forecast.
          </p>
        </div>
        <ServiceStatusBadge />
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
        <button
          type="button"
          onClick={() => {
            loadCurrent(selectedLocation);
            loadForecast(selectedLocation);
          }}
          disabled={currentLoading || forecastLoading}
        >
          {currentLoading || forecastLoading ? 'Refreshing...' : 'Refresh'}
        </button>
      </div>

      <div className="grid grid-two">
        <CurrentWeatherPanel
          data={currentWeather}
          loading={currentLoading}
          error={currentError}
          onRetry={() => loadCurrent(selectedLocation)}
          locationLabel={currentWeather?.resolvedLocation?.name || selectedLocation}
        />
        <ForecastPanel
          title="2-hour forecast"
          subtitle={selectedLocation ? `For ${selectedLocation}` : 'Pick a location'}
          data={forecast}
          loading={forecastLoading}
          error={forecastError}
          onRetry={() => loadForecast(selectedLocation)}
          emptyMessage="No 2-hour forecast available for this location yet."
        />
      </div>
    </div>
  );
}
