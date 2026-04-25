const CHART_METRICS = [
  { type: 'Temperature', label: 'Temp', color: '#146c94' },
  { type: 'RelativeHumidity', label: 'Humidity', color: '#4f7f52' },
  { type: 'Rainfall', label: 'Rainfall', color: '#7a5cbe' },
  { type: 'PSI', label: 'PSI', color: '#9a5b1f' },
  { type: 'PM25', label: 'PM2.5', color: '#9a5b1f' }
];

function buildSeries(records) {
  return CHART_METRICS.map((metric) => {
    const values = records
      .filter((record) => record.metricType === metric.type)
      .slice(-24)
      .map((record) => ({ value: Number(record.metricValue), timestamp: record.timestampUtc }))
      .filter((record) => Number.isFinite(record.value));

    return { ...metric, values };
  }).filter((metric) => metric.values.length > 0);
}

function pointsFor(values, min, max) {
  if (values.length === 1) {
    const y = max === min ? 50 : 100 - ((values[0].value - min) / (max - min)) * 100;
    return `50,${y}`;
  }

  return values
    .map((record, index) => {
      const x = (index / (values.length - 1)) * 100;
      const y = max === min ? 50 : 100 - ((record.value - min) / (max - min)) * 100;
      return `${x},${y}`;
    })
    .join(' ');
}

export function HistoricalWeatherChart({ records }) {
  const series = buildSeries(records || []);
  const allValues = series.flatMap((item) => item.values.map((record) => record.value));

  if (series.length === 0) {
    return <p className="empty">No chartable metrics are available for this range.</p>;
  }

  const min = Math.min(...allValues);
  const max = Math.max(...allValues);

  return (
    <div className="history-chart">
      <div className="chart-legend">
        {series.map((item) => (
          <span key={item.type}>
            <i style={{ backgroundColor: item.color }} />
            {item.label}
          </span>
        ))}
      </div>
      <svg className="line-chart" viewBox="0 0 100 100" preserveAspectRatio="none" role="img" aria-label="Historical weather metrics">
        <line x1="0" y1="100" x2="100" y2="100" className="chart-axis" />
        <line x1="0" y1="0" x2="0" y2="100" className="chart-axis" />
        {series.map((item) => (
          <polyline
            key={item.type}
            points={pointsFor(item.values, min, max)}
            fill="none"
            stroke={item.color}
            strokeWidth="2"
            vectorEffect="non-scaling-stroke"
          />
        ))}
      </svg>
      <div className="chart-scale">
        <span>{max.toLocaleString('en-SG', { maximumFractionDigits: 1 })}</span>
        <span>{min.toLocaleString('en-SG', { maximumFractionDigits: 1 })}</span>
      </div>
    </div>
  );
}
