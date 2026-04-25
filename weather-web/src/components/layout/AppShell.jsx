import { Outlet } from 'react-router-dom';
import { Header } from './Header.jsx';

export function AppShell() {
  return (
    <div className="app-shell">
      <Header />
      <main className="app-main">
        <div className="app-container">
          <Outlet />
        </div>
      </main>
      <footer className="app-footer">
        <div className="app-container">
          <p>Data from data.gov.sg. Stored weather records via WeatherApp microservice.</p>
        </div>
      </footer>
    </div>
  );
}
