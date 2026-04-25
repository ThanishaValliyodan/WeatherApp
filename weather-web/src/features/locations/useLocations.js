import { useEffect, useState } from 'react';
import { getLocations } from './locationApi.js';
import { getErrorMessage } from '../../lib/api/apiErrors.js';

export function useLocations() {
  const [locations, setLocations] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError('');

    getLocations()
      .then((data) => {
        if (!cancelled) {
          setLocations(data || []);
        }
      })
      .catch((requestError) => {
        if (!cancelled) {
          setLocations([]);
          setError(getErrorMessage(requestError));
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, []);

  return { locations, loading, error };
}
