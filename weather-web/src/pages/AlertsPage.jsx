import { useCallback, useEffect, useState } from 'react';
import { Card } from '../components/ui/Card.jsx';
import { ErrorState } from '../components/ui/ErrorState.jsx';
import { LoadingState } from '../components/ui/LoadingState.jsx';
import { LocationSelector } from '../components/weather/LocationSelector.jsx';
import { createAlertSubscription, deleteAlertSubscription, evaluateAlerts, getAlertSubscriptions } from '../features/alerts/alertsApi.js';
import { useLocations } from '../features/locations/useLocations.js';
import { getErrorMessage } from '../lib/api/apiErrors.js';
import { formatDateTime } from '../lib/date/formatDate.js';

const DEFAULT_LOCATION = 'Ang Mo Kio';
const ALERT_TYPES = [
  { value: 'HighTemperature', label: 'High temperature', unit: 'deg C', defaultThreshold: 33 },
  { value: 'HeavyRain', label: 'Heavy rain', unit: 'mm', defaultThreshold: 10 },
  { value: 'HighHumidity', label: 'High humidity', unit: '%', defaultThreshold: 85 },
  { value: 'StrongWind', label: 'Strong wind', unit: 'km/h', defaultThreshold: 30 }
];

function labelForAlertType(alertType) {
  return ALERT_TYPES.find((type) => type.value === alertType)?.label || alertType;
}

function unitForAlertType(alertType) {
  return ALERT_TYPES.find((type) => type.value === alertType)?.unit || '';
}

export function AlertsPage() {
  const { locations, loading: locationsLoading, error: locationsError } = useLocations();
  const [email, setEmail] = useState('');
  const [location, setLocation] = useState(DEFAULT_LOCATION);
  const [alertType, setAlertType] = useState(ALERT_TYPES[0].value);
  const [thresholdValue, setThresholdValue] = useState(ALERT_TYPES[0].defaultThreshold);
  const [subscriptions, setSubscriptions] = useState([]);
  const [evaluation, setEvaluation] = useState(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [evaluating, setEvaluating] = useState(false);
  const [error, setError] = useState('');
  const [formError, setFormError] = useState('');

  const loadSubscriptions = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const data = await getAlertSubscriptions();
      setSubscriptions(data);
    } catch (requestError) {
      setError(getErrorMessage(requestError));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadSubscriptions();
  }, [loadSubscriptions]);

  function updateAlertType(nextAlertType) {
    setAlertType(nextAlertType);
    setThresholdValue(ALERT_TYPES.find((type) => type.value === nextAlertType)?.defaultThreshold || 1);
  }

  async function submitSubscription(event) {
    event.preventDefault();
    setSaving(true);
    setFormError('');

    try {
      await createAlertSubscription({
        email,
        location,
        alertType,
        thresholdValue: Number(thresholdValue)
      });
      setEmail('');
      await loadSubscriptions();
    } catch (requestError) {
      setFormError(getErrorMessage(requestError));
    } finally {
      setSaving(false);
    }
  }

  async function removeSubscription(id) {
    await deleteAlertSubscription(id);
    await loadSubscriptions();
  }

  async function checkAlerts() {
    setEvaluating(true);
    setFormError('');
    try {
      const data = await evaluateAlerts(location);
      setEvaluation(data);
    } catch (requestError) {
      setFormError(getErrorMessage(requestError));
    } finally {
      setEvaluating(false);
    }
  }

  return (
    <div className="page">
      <header className="page-header">
        <div>
          <p className="page-eyebrow">Alerts</p>
          <h2 className="page-title">Weather alert subscriptions</h2>
          <p className="page-subtitle">
            Store alert rules and check whether current readings meet the selected threshold.
          </p>
        </div>
      </header>

      <div className="grid grid-two">
        <Card title="Create subscription" subtitle="Stored alert rule">
          <form className="alert-form" onSubmit={submitSubscription}>
            <div className="field">
              <label htmlFor="alert-email">Email</label>
              <input
                id="alert-email"
                type="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                placeholder="name@example.com"
                required
              />
            </div>
            <LocationSelector
              locations={locations}
              loading={locationsLoading}
              error={locationsError}
              value={location}
              onChange={setLocation}
              locationType="ForecastArea"
              label="Location"
              placeholder="Select a location"
            />
            <div className="field">
              <label htmlFor="alert-type">Alert type</label>
              <select id="alert-type" value={alertType} onChange={(event) => updateAlertType(event.target.value)}>
                {ALERT_TYPES.map((type) => (
                  <option key={type.value} value={type.value}>
                    {type.label}
                  </option>
                ))}
              </select>
            </div>
            <div className="field">
              <label htmlFor="alert-threshold">Threshold ({unitForAlertType(alertType)})</label>
              <input
                id="alert-threshold"
                type="number"
                min="0"
                step="0.1"
                value={thresholdValue}
                onChange={(event) => setThresholdValue(event.target.value)}
                required
              />
            </div>
            {formError && <p className="page-error">{formError}</p>}
            <div className="button-group button-group-left">
              <button type="submit" disabled={saving}>
                {saving ? 'Saving...' : 'Subscribe'}
              </button>
              <button type="button" className="ghost-action" onClick={checkAlerts} disabled={evaluating}>
                {evaluating ? 'Checking...' : 'Check now'}
              </button>
            </div>
          </form>
        </Card>

        <Card title="Triggered now" subtitle={evaluation ? formatDateTime(evaluation.evaluatedAtUtc) : location}>
          {!evaluation && <p className="empty">Run a check to see active subscriptions that match current readings.</p>}
          {evaluation && evaluation.triggeredAlerts.length === 0 && (
            <p className="empty">No active subscriptions are triggered for {evaluation.location}.</p>
          )}
          {evaluation && evaluation.triggeredAlerts.length > 0 && (
            <ul className="alert-list">
              {evaluation.triggeredAlerts.map((alert) => (
                <li key={alert.subscriptionId} className="alert-item alert-item-triggered">
                  <strong>{labelForAlertType(alert.alertType)}</strong>
                  <span>{alert.message}</span>
                  <small>{alert.email}</small>
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>

      <Card title="Active subscriptions" subtitle={`${subscriptions.length} active`}>
        {loading && <LoadingState message="Loading subscriptions..." />}
        {!loading && error && <ErrorState message={error} onRetry={loadSubscriptions} />}
        {!loading && !error && subscriptions.length === 0 && (
          <p className="empty">No active alert subscriptions yet.</p>
        )}
        {!loading && !error && subscriptions.length > 0 && (
          <ul className="alert-list">
            {subscriptions.map((subscription) => (
              <li key={subscription.id} className="alert-item">
                <div>
                  <strong>{labelForAlertType(subscription.alertType)}</strong>
                  <span>
                    {subscription.location} at {subscription.thresholdValue} {unitForAlertType(subscription.alertType)}
                  </span>
                  <small>{subscription.email}</small>
                </div>
                <button type="button" className="ghost-action" onClick={() => removeSubscription(subscription.id)}>
                  Deactivate
                </button>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  );
}
