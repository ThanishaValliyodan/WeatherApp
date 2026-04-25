export function WeatherMetric({ label, value, unit }) {
  return (
    <div className="weather-metric">
      <span className="weather-metric-label">{label}</span>
      <strong className="weather-metric-value">
        {value}
        {unit && value !== '-' && <span className="weather-metric-unit"> {unit}</span>}
      </strong>
    </div>
  );
}
