import { Card } from '../../components/ui/Card.jsx';
import { LoadingState } from '../../components/ui/LoadingState.jsx';
import { ErrorState } from '../../components/ui/ErrorState.jsx';
import { WeatherMetric } from '../../components/weather/WeatherMetric.jsx';

function formatNumber(value, fractionDigits = 1) {
  if (value === null || value === undefined) {
    return '-';
  }
  return Number(value).toFixed(fractionDigits);
}

export function CurrentWeatherPanel({ data, loading, error, onRetry, locationLabel }) {
  return (
    <Card
      title="Current weather"
      subtitle={locationLabel ? `For ${locationLabel}` : 'Pick a location to begin'}
    >
      {loading && <LoadingState message="Fetching latest readings..." />}
      {!loading && error && <ErrorState message={error} onRetry={onRetry} />}
      {!loading && !error && data && (
        <>
          <div className="metric-grid">
            <WeatherMetric label="Temperature" value={formatNumber(data.temperatureCelsius)} unit="deg C" />
            <WeatherMetric label="Humidity" value={formatNumber(data.humidityPercent, 0)} unit="%" />
            <WeatherMetric label="Rainfall" value={formatNumber(data.rainfallMm)} unit="mm" />
            <WeatherMetric label="Wind speed" value={formatNumber(data.windSpeed)} unit="km/h" />
            <WeatherMetric label="Wind direction" value={formatNumber(data.windDirectionDegrees, 0)} unit="deg" />
            <WeatherMetric label="PM2.5" value={formatNumber(data.pm25, 0)} unit="ug/m3" />
            <WeatherMetric label="PSI" value={formatNumber(data.psi, 0)} unit="index" />
            <WeatherMetric label="UV index" value={formatNumber(data.uvIndex, 0)} unit="index" />
          </div>
          {data.sources?.length > 0 && (
            <p className="card-footnote">
              Sources: {data.sources.map((source) => source.providerDataset).join(', ')}
            </p>
          )}
        </>
      )}
      {!loading && !error && !data && <p className="empty">No readings yet. Pick a location.</p>}
    </Card>
  );
}
