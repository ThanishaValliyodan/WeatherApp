import { apiClient } from '../../lib/api/apiClient.js';

export async function getServiceStatus() {
  const response = await apiClient.get('/api/status', {
    validateStatus: (status) => status === 200 || status === 503
  });
  return response.data;
}
