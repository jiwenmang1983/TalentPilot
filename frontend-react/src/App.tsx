import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import LoginPage from './views/LoginPage';
import MainLayout from './components/layout/MainLayout';
import DashboardPage from './views/DashboardPage';
import CandidateListPage from './views/CandidateListPage';
import InterviewListPage from './views/InterviewListPage';
import ReportListPage from './views/ReportListPage';
import SettingsPage from './views/SettingsPage';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/" element={<MainLayout />}>
          <Route index element={<Navigate to="/dashboard" replace />} />
          <Route path="dashboard" element={<DashboardPage />} />
          <Route path="candidates" element={<CandidateListPage />} />
          <Route path="interviews" element={<InterviewListPage />} />
          <Route path="reports" element={<ReportListPage />} />
          <Route path="settings" element={<SettingsPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;
