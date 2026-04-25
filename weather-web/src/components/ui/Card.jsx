export function Card({ title, subtitle, action, children, className = '' }) {
  const composed = ['card', className].filter(Boolean).join(' ');

  return (
    <section className={composed}>
      {(title || action) && (
        <header className="card-header">
          <div>
            {title && <h2 className="card-title">{title}</h2>}
            {subtitle && <p className="card-subtitle">{subtitle}</p>}
          </div>
          {action && <div className="card-action">{action}</div>}
        </header>
      )}
      <div className="card-body">{children}</div>
    </section>
  );
}
