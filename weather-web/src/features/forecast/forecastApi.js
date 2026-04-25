import { apiClient } from '../../lib/api/apiClient.js';

export async function getForecast({ type, location, region }) {
  const params = { type };
  if (location) params.location = location;
  if (region) params.region = region;

  const response = await apiClient.get('/api/weather/forecast', { params });
  return response.data;
}
