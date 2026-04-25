import { apiClient } from '../../lib/api/apiClient.js';

export async function getCurrentWeather({ location, stationId, latitude, longitude }) {
  const params = {};
  if (location) params.location = location;
  if (stationId) params.stationId = stationId;
  if (latitude !== undefined && latitude !== null) params.latitude = latitude;
  if (longitude !== undefined && longitude !== null) params.longitude = longitude;

  const response = await apiClient.get('/api/weather/current', { params });
  return response.data;
}
