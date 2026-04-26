import { apiClient } from '../../lib/api/apiClient.js';

export async function createAlertSubscription(subscription) {
  const response = await apiClient.post('/api/alerts/subscriptions', subscription);
  return response.data;
}

export async function getAlertSubscriptions() {
  const response = await apiClient.get('/api/alerts/subscriptions');
  return response.data;
}

export async function deleteAlertSubscription(id) {
  await apiClient.delete(`/api/alerts/subscriptions/${id}`);
}

export async function evaluateAlerts(location) {
  const response = await apiClient.post('/api/alerts/evaluate', null, {
    params: { location }
  });
  return response.data;
}
