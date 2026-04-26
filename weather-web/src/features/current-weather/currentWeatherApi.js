import { apiClient } from '../../lib/api/apiClient.js';

export async function getCurrentWeather({ location }) {
  const params = {};
  if (location) params.location = location;

  const response = await apiClient.get('/api/weather/current', { params });
  return response.data;
}
