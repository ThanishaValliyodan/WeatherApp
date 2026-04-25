import { apiClient } from '../../lib/api/apiClient.js';

export async function getHistoricalWeather({ location, from, to }) {
  const response = await apiClient.get('/api/weather/history', {
    params: { location, from, to }
  });
  return response.data;
}
