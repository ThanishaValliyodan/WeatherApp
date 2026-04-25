export function getErrorMessage(error) {
  if (!error) {
    return 'Unknown error.';
  }

  const response = error.response;
  if (response?.data?.error) {
    return response.data.error;
  }

  if (response?.status === 503) {
    return 'weather-service is degraded. Please try again shortly.';
  }

  if (response?.status === 429) {
    return 'Too many requests. Please slow down and try again.';
  }

  if (error.code === 'ECONNABORTED') {
    return 'Request timed out.';
  }

  if (!response) {
    return 'Could not reach weather-service.';
  }

  return error.message || 'Request failed.';
}
