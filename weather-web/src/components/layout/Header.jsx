import { NavLink } from 'react-router-dom';

const links = [
  { to: '/', label: 'Dashboard', end: true },
  { to: '/forecast', label: 'Forecast' },
  { to: '/history', label: 'History' },
  { to: '/alerts', label: 'Alerts' }
];

export function Header() {
  return (
    <header className="app-header">
      <div className="app-header-inner">
        <div className="brand">
          <span className="brand-mark" aria-hidden="true">
            W
          </span>
          <div>
            <p className="brand-eyebrow">WeatherApp</p>
            <h1 className="brand-title">Singapore Weather</h1>
          </div>
        </div>
        <nav className="app-nav" aria-label="Primary">
          {links.map((link) => (
            <NavLink
              key={link.to}
              to={link.to}
              end={link.end}
              className={({ isActive }) =>
                isActive ? 'app-nav-link active' : 'app-nav-link'
              }
            >
              {link.label}
            </NavLink>
          ))}
        </nav>
      </div>
    </header>
  );
}
