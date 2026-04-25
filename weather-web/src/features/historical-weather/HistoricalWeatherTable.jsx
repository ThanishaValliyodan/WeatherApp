import { formatDateTime } from '../../lib/date/formatDate.js';

function formatValue(value) {
  if (value === null || value === undefined) {
    return '-';
  }
  return Number(value).toLocaleString('en-SG', { maximumFractionDigits: 2 });
}

export function HistoricalWeatherTable({ records }) {
  if (!records?.length) {
    return <p className="empty">No stored records found for this date range.</p>;
  }

  return (
    <div className="table-wrap">
      <table className="data-table">
        <thead>
          <tr>
            <th>Time</th>
            <th>Location</th>
            <th>Metric</th>
            <th>Value</th>
            <th>Station</th>
            <th>Dataset</th>
          </tr>
        </thead>
        <tbody>
          {records.map((record, index) => (
            <tr key={`${record.timestampUtc}-${record.metricType}-${index}`}>
              <td>{formatDateTime(record.timestampUtc)}</td>
              <td>{record.location}</td>
              <td>{record.metricType}</td>
              <td>
                {formatValue(record.metricValue)} {record.metricUnit}
              </td>
              <td>{record.stationId || '-'}</td>
              <td>{record.providerDataset}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
