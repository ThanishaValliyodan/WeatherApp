export function ErrorState({ message, onRetry }) {
  return (
    <div className="state state-error" role="alert">
      <p>{message}</p>
      {onRetry && (
        <button type="button" className="ghost-button" onClick={onRetry}>
          Try again
        </button>
      )}
    </div>
  );
}
