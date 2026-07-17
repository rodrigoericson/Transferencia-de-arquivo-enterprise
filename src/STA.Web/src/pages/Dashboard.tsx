import { useEffect, useState } from 'react';
import api from '../lib/api';
import type { ApiResponse, PaginatedResponse, WorkerStatus, LogArquivo, Execucao } from '../types';
import { useNavigate } from 'react-router-dom';
import Header from '../components/layout/Header';

export default function Dashboard() {
  const [status, setStatus] = useState<WorkerStatus | null>(null);
  const [errosRecentes, setErrosRecentes] = useState<LogArquivo[]>([]);
  const [execucao, setExecucao] = useState<Execucao | null>(null);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    fetchStatus();
    fetchErrosRecentes();
    fetchExecucao();
    const interval = setInterval(() => { fetchStatus(); fetchErrosRecentes(); fetchExecucao(); }, 5000);
    return () => clearInterval(interval);
  }, []);

  const fetchStatus = async () => {
    try {
      const { data } = await api.get<ApiResponse<WorkerStatus>>('/worker/status');
      if (data.success && data.data) setStatus(data.data);
    } catch { /* handled by interceptor */ }
    setLoading(false);
  };

  const fetchExecucao = async () => {
    try {
      const { data } = await api.get<ApiResponse<Execucao>>('/worker/execucao');
      if (data.success && data.data) setExecucao(data.data);
    } catch { /* interceptor */ }
  };

  const fetchErrosRecentes = async () => {
    try {
      const seteDiasAtras = new Date();
      seteDiasAtras.setDate(seteDiasAtras.getDate() - 7);
      const de = seteDiasAtras.toISOString().split('T')[0] + 'T00:00:00';

      const erros = await api.get<ApiResponse<PaginatedResponse<LogArquivo>>>(`/logs/arquivos?status=E&pageSize=10&de=${de}`);
      const warnings = await api.get<ApiResponse<PaginatedResponse<LogArquivo>>>(`/logs/arquivos?status=W&pageSize=10&de=${de}`);
      const items = [
        ...(erros.data.data?.items ?? []),
        ...(warnings.data.data?.items ?? []),
      ].sort((a, b) => new Date(b.dtInicio).getTime() - new Date(a.dtInicio).getTime());
      setErrosRecentes(items);
    } catch { /* handled by interceptor */ }
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
      <Header />

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

        {/* Box de Execução */}
        {execucao && (
          <ExecucaoBox execucao={execucao} />
        )}

        {/* Warnings / Erros Recentes */}
        {errosRecentes.length > 0 && (
          <div className="mb-8 p-4 bg-red-950/30 border border-red-900/50 rounded-lg">
            <div className="flex justify-between items-center mb-3">
              <h3 className="text-sm font-medium text-red-400">⚠️ Erros e Avisos (últimos 7 dias)</h3>
              <button onClick={() => navigate('/logs')} className="text-xs text-red-400 hover:text-red-300">Ver todos →</button>
            </div>
            <div className="space-y-2 max-h-48 overflow-y-auto">
              {errosRecentes.map((log) => (
                <div key={log.cnLogArquivo} className={`rounded p-2 ${log.idStatus === 'E' ? 'bg-red-950/40' : 'bg-yellow-950/40'}`}>
                  <div className="flex justify-between items-start">
                    <div className="flex items-center gap-2">
                      <span className={`text-xs ${log.idStatus === 'E' ? 'text-red-400' : 'text-yellow-400'}`}>{log.idStatus === 'E' ? '●' : '◐'}</span>
                      <span className={`px-1.5 py-0.5 rounded text-[10px] ${log.idStatus === 'E' ? 'bg-red-900 text-red-300' : 'bg-yellow-900 text-yellow-300'}`}>{log.idStatus === 'E' ? 'ERRO' : 'AVISO'}</span>
                      <span className="font-mono text-xs text-gray-200">{log.nmArquivo}</span>
                    </div>
                    <span className="text-xs text-gray-600 whitespace-nowrap">{new Date(log.dtInicio).toLocaleString('pt-BR')}</span>
                  </div>
                  <p className={`text-xs mt-1 pl-5 ${log.idStatus === 'E' ? 'text-red-300/70' : 'text-yellow-300/70'}`}>{log.dsMensagem || 'Verifique permissões ou disponibilidade do diretório'}</p>
                </div>
              ))}
            </div>
            <p className="text-xs text-gray-600 mt-2">{errosRecentes.length} ocorrência(s) nos últimos 7 dias</p>
          </div>
        )}

        {/* Worker Control (Admin only) */}
        {sessionStorage.getItem('sta_role') === 'Admin' && (
          <div className="flex gap-3 mb-8">
            {status?.status === 'rodando' ? (
              <button onClick={handlePause} className="px-4 py-2 text-sm bg-yellow-600 hover:bg-yellow-700 rounded">Pausar Worker</button>
            ) : (
              <button onClick={handleResume} className="px-4 py-2 text-sm bg-green-600 hover:bg-green-700 rounded">Retomar Worker</button>
            )}
          </div>
        )}

        {/* Navigation */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {sessionStorage.getItem('sta_role') !== 'Viewer' && (
            <NavCard title="Nova Transferência" description="Criar nova cadeia de transferência" onClick={() => navigate('/etapas/nova')} accent />
          )}
          <NavCard title="Transferências" description="Ver etapas, rotas e destinos" onClick={() => navigate('/etapas')} />
          <NavCard title="Logs" description="Ver registros de transferência de hoje" onClick={() => navigate('/logs')} />
          {sessionStorage.getItem('sta_role') === 'Admin' && (
            <NavCard title="Auditoria" description="Ver histórico de ações no sistema" onClick={() => navigate('/auditoria')} />
          )}
        </div>

        {/* Última atualização */}
        <p className="text-xs text-gray-700 text-center mt-8">Última atualização: {new Date().toLocaleTimeString('pt-BR')} (atualiza a cada 30s)</p>
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

function ExecucaoBox({ execucao }: { execucao: Execucao }) {
  const [countdown, setCountdown] = useState('');

  useEffect(() => {
    if (!execucao.proximoCicloEm || execucao.executando || execucao.pausado) {
      setCountdown('');
      return;
    }
    const update = () => {
      const diff = new Date(execucao.proximoCicloEm!).getTime() - Date.now();
      if (diff <= 0) { setCountdown('em breve'); return; }
      const min = Math.floor(diff / 60000);
      const sec = Math.floor((diff % 60000) / 1000);
      setCountdown(`${String(min).padStart(2, '0')}:${String(sec).padStart(2, '0')}`);
    };
    update();
    const timer = setInterval(update, 1000);
    return () => clearInterval(timer);
  }, [execucao.proximoCicloEm, execucao.executando, execucao.pausado]);

  if (execucao.pausado) {
    return (
      <div className="mb-6 p-4 bg-yellow-950/20 border border-yellow-800/50 rounded-lg flex items-center gap-3">
        <span className="text-yellow-400 text-lg">⏸</span>
        <span className="text-sm text-yellow-300">Worker pausado — aguardando retomada</span>
      </div>
    );
  }

  if (execucao.executando) {
    return (
      <div className="mb-6 p-4 bg-green-950/20 border border-green-800/50 rounded-lg flex items-center gap-3">
        <span className="text-green-400 text-lg animate-pulse">⏳</span>
        <span className="text-sm text-green-300">{execucao.etapaAtual || 'Processando...'}</span>
      </div>
    );
  }

  const duracaoStr = execucao.duracaoUltimoCicloMs != null
    ? execucao.duracaoUltimoCicloMs < 1000
      ? `${execucao.duracaoUltimoCicloMs}ms`
      : `${(execucao.duracaoUltimoCicloMs / 1000).toFixed(1)}s`
    : null;

  return (
    <div className="mb-6 p-4 bg-gray-900 border border-gray-800 rounded-lg flex items-center gap-3">
      <span className="text-green-400 text-lg">✓</span>
      <span className="text-sm text-gray-300">
        Ciclo concluído{duracaoStr && <span className="text-gray-600"> {' '}em {duracaoStr}</span>}
      </span>
      <span className="text-sm text-gray-500 ml-auto font-mono">
        {countdown ? `Próximo em ${countdown}` : 'Aguardando...'}
      </span>
    </div>
  );
}
