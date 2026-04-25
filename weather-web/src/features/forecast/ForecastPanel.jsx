import { Card } from '../../components/ui/Card.jsx';
import { LoadingState } from '../../components/ui/LoadingState.jsx';
import { ErrorState } from '../../components/ui/ErrorState.jsx';
import { formatTime, formatDateTime, formatDate } from '../../lib/date/formatDate.js';

function rangeLabel(item) {
  if (item.validFromUtc && item.validToUtc) {
    const sameDate = formatDate(item.validFromUtc) === formatDate(item.validToUtc);
    const start = sameDate ? formatTime(item.validFromUtc) : formatDateTime(item.validFromUtc);
    const end = sameDate ? formatTime(item.validToUtc) : formatDateTime(item.validToUtc);
    return `${start} - ${end}`;
  }
  if (item.forecastTimeUtc) {
    return formatDateTime(item.forecastTimeUtc);
  }
  return '-';
}

function temperatureRange(item) {
  if (item.temperatureLowCelsius == null && item.temperatureHighCelsius == null) {
    return null;
  }
  const low = item.temperatureLowCelsius ?? '-';
  const high = item.temperatureHighCelsius ?? '-';
  return `${low} / ${high} deg C`;
}

function humidityRange(item) {
  if (item.humidityLowPercent == null && item.humidityHighPercent == null) {
    return null;
  }
  const low = item.humidityLowPercent ?? '-';
  const high = item.humidityHighPercent ?? '-';
  return `${low} / ${high}%`;
}

export function ForecastPanel({ title, subtitle, data, loading, error, onRetry, emptyMessage }) {
  return (
    <Card title={title} subtitle={subtitle}>
      {loading && <LoadingState message="Loading forecast..." />}
      {!loading && error && <ErrorState message={error} onRetry={onRetry} />}
      {!loading && !error && data && data.items?.length > 0 && (
        <ul className="forecast-list">
          {data.items.map((item, index) => {
            const tempRange = temperatureRange(item);
            const humidity = humidityRange(item);
            return (
              <li key={`${item.location}-${item.validFromUtc ?? item.forecastTimeUtc}-${index}`} className="forecast-item">
                <div className="forecast-item-header">
                  <strong>{item.location}</strong>
                  <span className="forecast-item-time">{rangeLabel(item)}</span>
                </div>
                <p className="forecast-item-summary">{item.summary || 'No summary.'}</p>
                {(tempRange || humidity || item.windDirection) && (
                  <p className="forecast-item-meta">
                    {tempRange && <span>Temp {tempRange}</span>}
                    {humidity && <span>Humidity {humidity}</span>}
                    {item.windDirection && <span>Wind {item.windDirection}</span>}
                  </p>
                )}
              </li>
            );
          })}
        </ul>
      )}
      {!loading && !error && (!data || (data.items?.length ?? 0) === 0) && (
        <p className="empty">{emptyMessage || 'No forecast available.'}</p>
      )}
    </Card>
  );
}
