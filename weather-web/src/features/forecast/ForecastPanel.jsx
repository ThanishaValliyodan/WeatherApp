import { Card } from '../../components/ui/Card.jsx';
import { LoadingState } from '../../components/ui/LoadingState.jsx';
import { ErrorState } from '../../components/ui/ErrorState.jsx';
import { formatTime, formatDateTime, formatDate } from '../../lib/date/formatDate.js';

function capitalize(value) {
  if (!value) {
    return '';
  }

  return value.charAt(0).toUpperCase() + value.slice(1);
}

function compactDateTime(value) {
  if (!value) {
    return '-';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '-';
  }

  return date.toLocaleString('en-SG', {
    timeZone: 'Asia/Singapore',
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit'
  });
}

function twentyFourHourRangeLabel(item) {
  if (!item.validFromUtc || !item.validToUtc) {
    return rangeLabel(item);
  }

  const startDate = formatDate(item.validFromUtc);
  const endDate = formatDate(item.validToUtc);
  const startTime = formatTime(item.validFromUtc);
  const endTime = formatTime(item.validToUtc);

  if (startDate === endDate) {
    return `${startDate}, ${startTime} - ${endTime}`;
  }

  return `${compactDateTime(item.validFromUtc)} - ${compactDateTime(item.validToUtc)}`;
}

function sortForecastItems(items) {
  return [...items].sort((first, second) => {
    const firstDate = new Date(first.validFromUtc || first.forecastTimeUtc || 0).getTime();
    const secondDate = new Date(second.validFromUtc || second.forecastTimeUtc || 0).getTime();
    return firstDate - secondDate;
  });
}

function itemTitle(item, forecastType) {
  if (forecastType === 'twenty-four-hour') {
    if (item.region === 'national') {
      return 'Singapore overall';
    }

    return `${capitalize(item.region || item.location)} region`;
  }

  return item.location;
}

function rangeLabel(item, forecastType) {
  if (forecastType === 'four-day') {
    return formatDate(item.validFromUtc || item.forecastTimeUtc);
  }

  if (forecastType === 'twenty-four-hour' && item.validFromUtc && item.validToUtc) {
    return twentyFourHourRangeLabel(item);
  }

  if (item.validFromUtc && item.validToUtc) {
    if (item.validFromUtc === item.validToUtc) {
      return formatDate(item.validFromUtc);
    }

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

function ForecastMeta({ item }) {
  const tempRange = temperatureRange(item);
  const humidity = humidityRange(item);

  if (!tempRange && !humidity && !item.windDirection) {
    return null;
  }

  return (
    <p className="forecast-item-meta">
      {tempRange && <span>Temp {tempRange}</span>}
      {humidity && <span>Humidity {humidity}</span>}
      {item.windDirection && <span>Wind {item.windDirection}</span>}
    </p>
  );
}

function TwentyFourHourForecastList({ items }) {
  const national = items.find((item) => item.region === 'national');
  const regionalGroups = sortForecastItems(items.filter((item) => item.region && item.region !== 'national'))
    .reduce((groups, item) => {
      const key = `${item.validFromUtc || ''}|${item.validToUtc || ''}`;
      const existing = groups.find((group) => group.key === key);
      if (existing) {
        existing.items.push(item);
        return groups;
      }

      groups.push({ key, item, items: [item] });
      return groups;
    }, []);

  return (
    <ul className="forecast-list">
      {national && (
        <li className="forecast-item">
          <div className="forecast-item-header">
            <strong>Singapore overall</strong>
            <span className="forecast-item-time">{rangeLabel(national, 'twenty-four-hour')}</span>
          </div>
          {national.summary && <p className="forecast-item-summary">{national.summary}</p>}
          <ForecastMeta item={national} />
        </li>
      )}

      {regionalGroups.map((group) => (
        <li key={group.key} className="forecast-item">
          <div className="forecast-item-header">
            <strong>Regional outlook</strong>
            <span className="forecast-item-time">{rangeLabel(group.item, 'twenty-four-hour')}</span>
          </div>
          <div className="forecast-region-grid">
            {group.items.map((item) => (
              <span key={`${item.region}-${item.summary}`}>
                <strong>{capitalize(item.region)}</strong>
                {item.summary}
              </span>
            ))}
          </div>
        </li>
      ))}
    </ul>
  );
}

export function ForecastPanel({ title, subtitle, data, loading, error, onRetry, emptyMessage }) {
  const forecastType = data?.forecastType;
  const items = data?.items ? sortForecastItems(data.items) : [];

  return (
    <Card title={title} subtitle={subtitle}>
      {loading && <LoadingState message="Loading forecast..." />}
      {!loading && error && <ErrorState message={error} onRetry={onRetry} />}
      {!loading && !error && forecastType === 'twenty-four-hour' && items.length > 0 && (
        <TwentyFourHourForecastList items={items} />
      )}
      {!loading && !error && forecastType !== 'twenty-four-hour' && items.length > 0 && (
        <ul className="forecast-list">
          {items.map((item, index) => {
            return (
              <li key={`${item.location}-${item.validFromUtc ?? item.forecastTimeUtc}-${index}`} className="forecast-item">
                <div className="forecast-item-header">
                  <strong>{itemTitle(item, forecastType)}</strong>
                  <span className="forecast-item-time">{rangeLabel(item, forecastType)}</span>
                </div>
                {item.summary && <p className="forecast-item-summary">{item.summary}</p>}
                <ForecastMeta item={item} />
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
