import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../lib/api';
import type { ApiResponse, PaginatedResponse, LogSftp, ConexaoSftp } from '../types';
import Header from '../components/layout/Header';

function todayStr() {
  return new Date().toISOString().split('T')[0];
}

const TIPOS = ['', 'CONEXAO', 'UPLOAD', 'WARNING', 'ERRO', 'RETRY'];

const badgeColor: Record<string, string> = {
  CONEXAO: 'bg-blue-900 text-blue-300',
  UPLOAD: 'bg-green-900 text-green-300',
  WARNING: 'bg-yellow-900 text-yellow-300',
  ERRO: 'bg-red-900 text-red-300',
  RETRY: 'bg-orange-900 text-orange-300',
};

export default function LogsSftpPage() {
  const [logs, setLogs] = useState<LogSftp[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [tipo, setTipo] = useState('');
  const [conexaoId, setConexaoId] = useState('');
  const [de, setDe] = useState(todayStr());
  const [conexoes, setConexoes] = useState<ConexaoSftp[]>([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();
  const pageSize = 20;

  useEffect(() => { fetchConexoes(); }, []);
  useEffect(() => { fetchLogs(); }, [page, tipo, conexaoId, de]);

  const fetchConexoes = async () => {
    try {
      const { data } = await api.get<ApiResponse<PaginatedResponse<ConexaoSftp>>>('/conexoes-sftp?pageSize=50');
      if (data.success && data.data) setConexoes(data.data.items);
    } catch { /* interceptor */ }
  };

  const fetchLogs = async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
      if (tipo) params.set('tipo', tipo);
      if (conexaoId) params.set('conexaoId', conexaoId);
      if (de) {
        params.set('de', `${de}T00:00:00`);
        params.set('ate', `${de}T23:59:59`);
      }

      const { data } = await api.get<ApiResponse<PaginatedResponse<LogSftp>>>(`/logs-sftp?${params}`);
      if (data.success && data.data) {
        setLogs(data.data.items);
        setTotal(data.data.total);
      }
    } catch { /* interceptor */ }
    setLoading(false);
  };

  const pageCount = Math.ceil(total / pageSize) || 1;
  const nomeConexao = (id: number) => conexoes.find(c => c.cnConexaoSftp === id)?.nmConexao ?? `#${id}`;

  return (
    <div className="min-h-screen bg-gray-950 text-gray-100">
      <Header />
      <div className="max-w-6xl mx-auto p-8">
        <div className="flex justify-between items-center mb-6">
          <div>
            <h1 className="text-2xl font-mono text-green-400">Logs SFTP</h1>
            <p className="text-sm text-gray-500 mt-1">Registros de conexão e transferência SFTP</p>
          </div>
          <button onClick={() => navigate('/')} className="px-3 py-1.5 text-sm bg-gray-800 hover:bg-gray-700 rounded">Voltar</button>
        </div>

        {/* Filtros */}
        <div className="flex flex-wrap gap-3 mb-4 items-end">
          <div>
            <label className="block text-xs text-gray-500 mb-1">Dia</label>
            <input type="date" value={de} onChange={(e) => { setDe(e.target.value); setPage(1); }}
              className="px-3 py-1.5 bg-gray-800 border border-gray-700 rounded text-sm text-gray-100 focus:outline-none focus:border-green-500" />
          </div>

          <div>
            <label className="block text-xs text-gray-500 mb-1">Conexão</label>
            <select value={conexaoId} onChange={(e) => { setConexaoId(e.target.value); setPage(1); }}
              className="px-3 py-1.5 bg-gray-800 border border-gray-700 rounded text-sm text-gray-100">
              <option value="">Todas</option>
              {conexoes.map(c => <option key={c.cnConexaoSftp} value={c.cnConexaoSftp}>{c.nmConexao}</option>)}
            </select>
          </div>

          <div>
            <label className="block text-xs text-gray-500 mb-1">Tipo</label>
            <select value={tipo} onChange={(e) => { setTipo(e.target.value); setPage(1); }}
              className="px-3 py-1.5 bg-gray-800 border border-gray-700 rounded text-sm text-gray-100">
              {TIPOS.map(t => <option key={t} value={t}>{t || 'Todos'}</option>)}
            </select>
          </div>

          <div className="ml-auto text-right">
            <p className="text-xs text-gray-600">{total} registro(s)</p>
          </div>
        </div>

        {/* Tabela */}
        {loading ? <p className="text-gray-500 py-8 text-center">Carregando...</p> : logs.length === 0 ? (
          <div className="text-center py-12 text-gray-500">
            <p>Nenhum log SFTP encontrado.</p>
            <p className="text-xs mt-2">Logs aparecem quando o Worker executa transferências SFTP.</p>
          </div>
        ) : (
          <>
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-gray-500 border-b border-gray-800">
                  <th className="py-2 px-2">Hora</th>
                  <th className="py-2 px-2">Conexão</th>
                  <th className="py-2 px-2">Tipo</th>
                  <th className="py-2 px-2">Arquivo</th>
                  <th className="py-2 px-2">Tamanho</th>
                  <th className="py-2 px-2">Duração</th>
                  <th className="py-2 px-2">Mensagem</th>
                </tr>
              </thead>
              <tbody>
                {logs.map((l) => (
                  <tr key={l.cnLogSftp} className="border-b border-gray-800/50 hover:bg-gray-900/50">
                    <td className="py-2 px-2 text-xs text-gray-400">
                      {new Date(l.dtEvento).toLocaleTimeString('pt-BR')}
                    </td>
                    <td className="py-2 px-2 text-xs text-gray-300">{nomeConexao(l.cnConexaoSftp)}</td>
                    <td className="py-2 px-2">
                      <span className={`px-2 py-0.5 rounded text-xs ${badgeColor[l.idTipo] || 'bg-gray-800 text-gray-300'}`}>
                        {l.idTipo}
                      </span>
                    </td>
                    <td className="py-2 px-2 text-xs font-mono text-gray-300 truncate max-w-[150px]" title={l.nmArquivo || ''}>
                      {l.nmArquivo || '-'}
                    </td>
                    <td className="py-2 px-2 text-xs text-gray-400">
                      {l.nrTamanhoBytes != null ? formatBytes(l.nrTamanhoBytes) : '-'}
                    </td>
                    <td className="py-2 px-2 text-xs text-gray-400">
                      {l.nrDuracaoMs != null ? `${l.nrDuracaoMs}ms` : '-'}
                    </td>
                    <td className="py-2 px-2 text-xs text-gray-500 truncate max-w-[200px]" title={l.dsMensagem || ''}>
                      {l.dsMensagem || '-'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            {/* Paginação */}
            <div className="flex justify-between items-center mt-4 text-sm text-gray-500">
              <span>Página {page} de {pageCount}</span>
              <div className="flex gap-2">
                <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page <= 1}
                  className="px-3 py-1 bg-gray-800 hover:bg-gray-700 rounded disabled:opacity-40">Anterior</button>
                <button onClick={() => setPage(p => Math.min(pageCount, p + 1))} disabled={page >= pageCount}
                  className="px-3 py-1 bg-gray-800 hover:bg-gray-700 rounded disabled:opacity-40">Próxima</button>
              </div>
            </div>
          </>
        )}
      </div>
    </div>
  );
}

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}
