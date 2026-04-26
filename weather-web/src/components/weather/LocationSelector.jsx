import { useId, useMemo } from 'react';

const ANY_TYPE = 'all';

export function LocationSelector({
  locations,
  value,
  onChange,
  loading,
  error,
  locationType = ANY_TYPE,
  label = 'Location',
  placeholder = 'Select a location'
}) {
  const selectId = useId();

  const options = useMemo(() => {
    const source = locations || [];
    const filtered =
      locationType === ANY_TYPE
        ? source
        : source.filter((location) => location.locationType === locationType);
    return [...filtered].sort((a, b) => a.name.localeCompare(b.name));
  }, [locations, locationType]);

  return (
    <div className="location-selector">
      <label htmlFor={selectId}>{label}</label>
      <select
        id={selectId}
        value={value || ''}
        onChange={(event) => onChange(event.target.value)}
        disabled={loading || Boolean(error) || options.length === 0}
      >
        <option value="" disabled>
          {loading ? 'Loading locations...' : placeholder}
        </option>
        {options.map((location) => (
          <option
            key={`${location.locationType}-${location.name}-${location.stationId ?? ''}`}
            value={location.name}
          >
            {location.name}
          </option>
        ))}
      </select>
      {error && <span className="field-error">{error}</span>}
    </div>
  );
}
