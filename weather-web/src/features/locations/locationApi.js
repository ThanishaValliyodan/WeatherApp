import { apiClient } from '../../lib/api/apiClient.js';

export async function getLocations(query) {
  const response = await apiClient.get('/api/locations', {
    params: query ? { query } : undefined
  });
  return response.data;
}
