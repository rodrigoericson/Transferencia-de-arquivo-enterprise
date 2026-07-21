import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from './hooks/useAuth';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import Etapas from './pages/Etapas';
import Rotas from './pages/Rotas';
import Destinos from './pages/Destinos';
import Logs from './pages/Logs';
import NovaTransferencia from './pages/NovaTransferencia';
import EditarTransferencia from './pages/EditarTransferencia';
import Auditoria from './pages/Auditoria';
import ConexoesSftp from './pages/ConexoesSftp';
import LogsSftp from './pages/LogsSftp';
import ErrorBoundary from './components/ErrorBoundary';

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const isAuthenticated = useAuth((s) => s.isAuthenticated);
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  return <>{children}</>;
}

export default function App() {
  return (
    <ErrorBoundary>
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/" element={<ProtectedRoute><Dashboard /></ProtectedRoute>} />
        <Route path="/etapas" element={<ProtectedRoute><Etapas /></ProtectedRoute>} />
        <Route path="/etapas/nova" element={<ProtectedRoute><NovaTransferencia /></ProtectedRoute>} />
        <Route path="/etapas/:etapaId/editar" element={<ProtectedRoute><EditarTransferencia /></ProtectedRoute>} />
        <Route path="/etapas/:etapaId/rotas" element={<ProtectedRoute><Rotas /></ProtectedRoute>} />
        <Route path="/rotas/:rotaId/destinos" element={<ProtectedRoute><Destinos /></ProtectedRoute>} />
        <Route path="/logs" element={<ProtectedRoute><Logs /></ProtectedRoute>} />
        <Route path="/auditoria" element={<ProtectedRoute><Auditoria /></ProtectedRoute>} />
        <Route path="/conexoes-sftp" element={<ProtectedRoute><ConexoesSftp /></ProtectedRoute>} />
        <Route path="/logs-sftp" element={<ProtectedRoute><LogsSftp /></ProtectedRoute>} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
    </ErrorBoundary>
  );
}
