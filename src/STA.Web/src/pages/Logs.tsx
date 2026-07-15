import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../lib/api';
import type { ApiResponse, PaginatedResponse, LogArquivo } from '../types';
import Header from '../components/layout/Header';

function todayStr() {
  return new Date().toISOString().split('T')[0];
}

export default function Logs() {
  const [logs, setLogs] = useState<LogArquivo[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState('');
  const [arquivo, setArquivo] = useState('');
  const [data, setData] = useState(todayStr());
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();
  const pageSize = 20;

  useEffect(() => { fetchLogs(); }, [page, status, data]);

  const fetchLogs = async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
      if (status) params.set('status', status);
      if (arquivo) params.set('arquivo', arquivo);
      if (data) {
        params.set('de', `${data}T00:00:00`);
        params.set('ate', `${data}T23:59:59`);
      }

      const { data: res } = await api.get<ApiResponse<PaginatedResponse<LogArquivo>>>(`/logs/arquivos?${params}`);
      if (res.success && res.data) {
        setLogs(res.data.items);
        setTotal(res.data.total);
      }
    } catch { /* interceptor */ }
    setLoading(false);
  };

  const handleSearch = () => { setPage(1); fetchLogs(); };
  const pageCount = Math.ceil(total / pageSize) || 1;

  return (
    <div className="min-h-screen bg-gray-950 text-gray-100">
      <Header />
      <div className="max-w-6xl mx-auto p-8">
        <div className="flex justify-between items-center mb-6">
          <div>
            <h1 className="text-2xl font-mono text-green-400">Logs de Transferência</h1>
            <p className="text-sm text-gray-500 mt-1">Registros de arquivos transferidos por dia</p>
          </div>
          <button onClick={() => navigate('/')} className="px-3 py-1.5 text-sm bg-gray-800 hover:bg-gray-700 rounded">Voltar</button>
        </div>

        {/* Filtros */}
        <div className="flex flex-wrap gap-3 mb-4 items-center">
          <div>
            <label className="block text-xs text-gray-500 mb-1">Dia</label>
            <input type="date" value={data} onChange={(e) => { setData(e.target.value); setPage(1); }}
              className="px-3 py-1.5 bg-gray-800 border border-gray-700 rounded text-sm text-gray-100 focus:outline-none focus:border-green-500" />
          </div>

          <div>
            <label className="block text-xs text-gray-500 mb-1">Status</label>
            <select value={status} onChange={(e) => { setStatus(e.target.value); setPage(1); }}
              className="px-3 py-1.5 bg-gray-800 border border-gray-700 rounded text-sm text-gray-100">
              <option value="">Todos</option>
              <option value="S">Sucesso</option>
              <option value="W">Aviso</option>
              <option value="E">Erro</option>
            </select>
          </div>

          <div>
            <label className="block text-xs text-gray-500 mb-1">Arquivo</label>
            <div className="flex gap-2">
              <input value={arquivo} onChange={(e) => setArquivo(e.target.value)}
                placeholder="Buscar por nome..."
                onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                className="px-3 py-1.5 bg-gray-800 border border-gray-700 rounded text-sm text-gray-100 w-56 focus:outline-none focus:border-green-500" />
              <button onClick={handleSearch} className="px-3 py-1.5 text-sm bg-gray-700 hover:bg-gray-600 rounded">Buscar</button>
            </div>
          </div>

          <div className="ml-auto text-right">
            <p className="text-xs text-gray-600">{total} registro(s) encontrado(s)</p>
          </div>
        </div>

        {/* Tabela */}
        {loading ? <p className="text-gray-500 py-8 text-center">Carregando...</p> : logs.length === 0 ? (
          <div className="text-center py-12 text-gray-500">
            <p>Nenhum log encontrado para {new Date(data + 'T12:00:00').toLocaleDateString('pt-BR')}.</p>
            <p className="text-xs mt-2">Tente outro dia ou remova os filtros.</p>
          </div>
        ) : (
          <>
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-gray-500 border-b border-gray-800">
                  <th className="py-2 px-2">Arquivo</th>
                  <th className="py-2 px-2">Origem</th>
                  <th className="py-2 px-2">Destino</th>
                  <th className="py-2 px-2">Tamanho</th>
                  <th className="py-2 px-2">Status</th>
                  <th className="py-2 px-2">Hora</th>
                </tr>
              </thead>
              <tbody>
                {logs.map((l) => (
                  <tr key={l.cnLogArquivo} className="border-b border-gray-800/50 hover:bg-gray-900/50">
                    <td className="py-2 px-2 font-mono text-xs">{l.nmArquivo}</td>
                    <td className="py-2 px-2 text-xs text-gray-400 truncate max-w-[180px]" title={l.dsDiretorioOrigem}>{l.dsDiretorioOrigem.split('/').pop()}</td>
                    <td className="py-2 px-2 text-xs text-gray-400 truncate max-w-[180px]" title={l.dsDiretorioDestino}>{l.dsDiretorioDestino.split('/').pop()}</td>
                    <td className="py-2 px-2 text-xs">{formatBytes(l.nrTamanhoBytes)}</td>
                    <td className="py-2 px-2">
                      <span className={`px-2 py-0.5 rounded text-xs ${
                        l.idStatus === 'S' ? 'bg-green-900 text-green-300' :
                        l.idStatus === 'W' ? 'bg-yellow-900 text-yellow-300' :
                        'bg-red-900 text-red-300'
                      }`}>
                        {l.idStatus === 'S' ? 'Sucesso' : l.idStatus === 'W' ? 'Aviso' : 'Erro'}
                      </span>
                    </td>
                    <td className="py-2 px-2 text-xs text-gray-400">{new Date(l.dtInicio).toLocaleTimeString('pt-BR')}</td>
                  </tr>
                ))}
              </tbody>
            </table>

            {/* Paginação */}
            <div className="flex justify-between items-center mt-4 text-sm text-gray-500">
              <span>Página {page} de {pageCount}</span>
              <div className="flex gap-2">
                <button disabled={page <= 1} onClick={() => setPage(page - 1)}
                  className="px-3 py-1 bg-gray-800 rounded disabled:opacity-30">← Anterior</button>
                <button disabled={page >= pageCount} onClick={() => setPage(page + 1)}
                  className="px-3 py-1 bg-gray-800 rounded disabled:opacity-30">Próximo →</button>
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
