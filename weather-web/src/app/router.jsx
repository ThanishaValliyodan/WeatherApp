import { createBrowserRouter } from 'react-router-dom';
import { AppShell } from '../components/layout/AppShell.jsx';
import { DashboardPage } from '../pages/DashboardPage.jsx';
import { ForecastPage } from '../pages/ForecastPage.jsx';
import { HistoryPage } from '../pages/HistoryPage.jsx';
import { AlertsPage } from '../pages/AlertsPage.jsx';
import { NotFoundPage } from '../pages/NotFoundPage.jsx';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppShell />,
    children: [
      { index: true, element: <DashboardPage /> },
      { path: 'forecast', element: <ForecastPage /> },
      { path: 'history', element: <HistoryPage /> },
      { path: 'alerts', element: <AlertsPage /> },
      { path: '*', element: <NotFoundPage /> }
    ]
  }
]);
