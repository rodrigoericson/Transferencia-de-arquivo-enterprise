import { useEffect, useState } from 'react';
import api from '../lib/api';
import type { ApiResponse, WorkerStatus } from '../types';
import { useAuth } from '../hooks/useAuth';
import { useNavigate } from 'react-router-dom';

export default function Dashboard() {
  const [status, setStatus] = useState<WorkerStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const logout = useAuth((s) => s.logout);
  const username = useAuth((s) => s.username);
  const navigate = useNavigate();

  useEffect(() => {
    fetchStatus();
    const interval = setInterval(fetchStatus, 30000);
    return () => clearInterval(interval);
  }, []);

  const fetchStatus = async () => {
    try {
      const { data } = await api.get<ApiResponse<WorkerStatus>>('/worker/status');
      if (data.success && data.data) setStatus(data.data);
    } catch { /* handled by interceptor */ }
    setLoading(false);
  };

  const handlePause = async () => {
    await api.post('/worker/pause');
    fetchStatus();
  };

  const handleResume = async () => {
    await api.post('/worker/resume');
    fetchStatus();
  };

  if (loading) return <div className="p-8 text-gray-400">Carregando...</div>;

  return (
    <div className="min-h-screen bg-gray-950 text-gray-100">
      {/* Header */}
      <header className="border-b border-gray-800 px-8 py-4 flex justify-between items-center">
        <div className="flex items-center gap-4">
          <img src="/sta-logo.png" alt="STA" className="h-10" />
        </div>
        <div className="flex items-center gap-4">
          <span className="text-sm text-gray-500">{username}</span>
          <button onClick={logout} className="text-sm text-red-400 hover:text-red-300">Sair</button>
        </div>
      </header>

      {/* Content */}
      <div className="max-w-5xl mx-auto p-8">
        {/* Status Cards */}
        {status && (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
            <StatCard
              label="Status"
              value={status.status}
              color={status.status === 'rodando' ? 'green' : 'yellow'}
            />
            <StatCard
              label="Arquivos Hoje"
              value={String(status.arquivosHoje)}
              color="blue"
            />
            <StatCard
              label="Erros Hoje"
              value={String(status.errosHoje)}
              color={status.errosHoje > 0 ? 'red' : 'green'}
            />
            <StatCard
              label="Último Ciclo"
              value={status.ultimoCicloStatus === 'O' ? 'Sucesso' : status.ultimoCicloStatus ?? '-'}
              color={status.ultimoCicloStatus === 'O' ? 'green' : 'yellow'}
            />
          </div>
        )}

        {/* Worker Control */}
        <div className="flex gap-3 mb-8">
          {status?.status === 'rodando' ? (
            <button onClick={handlePause} className="px-4 py-2 text-sm bg-yellow-600 hover:bg-yellow-700 rounded">Pausar Worker</button>
          ) : (
            <button onClick={handleResume} className="px-4 py-2 text-sm bg-green-600 hover:bg-green-700 rounded">Retomar Worker</button>
          )}
        </div>

        {/* Navigation */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <NavCard title="Transferências" description="Gerenciar etapas, rotas e destinos" onClick={() => navigate('/etapas')} />
          <NavCard title="Logs" description="Consultar logs de transferência" onClick={() => navigate('/logs')} />
          <NavCard title="Nova Transferência" description="Criar nova cadeia de transferência" onClick={() => navigate('/etapas/nova')} accent />
        </div>
      </div>
    </div>
  );
}

function StatCard({ label, value, color }: { label: string; value: string; color: string }) {
  const colors: Record<string, string> = {
    green: 'border-green-500/50 text-green-400',
    yellow: 'border-yellow-500/50 text-yellow-400',
    red: 'border-red-500/50 text-red-400',
    blue: 'border-blue-500/50 text-blue-400',
  };

  return (
    <div className={`p-4 bg-gray-900 rounded-lg border-l-4 ${colors[color]}`}>
      <p className="text-xs text-gray-500 uppercase">{label}</p>
      <p className="text-xl font-mono mt-1">{value}</p>
    </div>
  );
}

function NavCard({ title, description, onClick, accent }: { title: string; description: string; onClick: () => void; accent?: boolean }) {
  return (
    <button onClick={onClick}
      className={`text-left p-5 rounded-lg border transition-colors ${
        accent
          ? 'bg-green-900/20 border-green-700 hover:bg-green-900/40'
          : 'bg-gray-900 border-gray-800 hover:border-gray-700 hover:bg-gray-800/50'
      }`}>
      <h3 className={`font-medium mb-1 ${accent ? 'text-green-400' : 'text-gray-100'}`}>{title}</h3>
      <p className="text-sm text-gray-500">{description}</p>
    </button>
  );
}
