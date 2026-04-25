export function LoadingState({ message = 'Loading...' }) {
  return (
    <div className="state state-loading" role="status" aria-live="polite">
      <span className="state-dot" />
      <span>{message}</span>
    </div>
  );
}
