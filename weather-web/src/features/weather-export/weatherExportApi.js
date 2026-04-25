import { apiClient } from '../../lib/api/apiClient.js';

export async function downloadWeatherCsv({ location, from, to }) {
  const response = await apiClient.get('/api/weather/export', {
    params: { location, from, to },
    responseType: 'blob'
  });

  const blob = new Blob([response.data], { type: 'text/csv;charset=utf-8' });
  const url = window.URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = `weather-${location}-${from}-${to}.csv`.replace(/\s+/g, '-');
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  window.URL.revokeObjectURL(url);
}
