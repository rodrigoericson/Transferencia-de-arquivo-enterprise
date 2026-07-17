import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../lib/api';
import type { ApiResponse, PaginatedResponse, Auditoria } from '../types';
import Header from '../components/layout/Header';

function todayStr() {
  return new Date().toISOString().split('T')[0];
}

const ENTIDADES = ['', 'ETAPA', 'ROTA', 'DESTINO', 'WORKER', 'AUTH'];
const ACOES = ['', 'CREATE', 'UPDATE', 'DELETE', 'LOGIN', 'PASSWORD', 'PAUSE', 'RESUME'];

const badgeColor: Record<string, string> = {
  CREATE: 'bg-green-900 text-green-300',
  UPDATE: 'bg-blue-900 text-blue-300',
  DELETE: 'bg-red-900 text-red-300',
  LOGIN: 'bg-purple-900 text-purple-300',
  PASSWORD: 'bg-yellow-900 text-yellow-300',
  PAUSE: 'bg-orange-900 text-orange-300',
  RESUME: 'bg-teal-900 text-teal-300',
};

export default function AuditoriaPage() {
  const [items, setItems] = useState<Auditoria[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [usuario, setUsuario] = useState('');
  const [entidade, setEntidade] = useState('');
  const [acao, setAcao] = useState('');
  const [de, setDe] = useState(todayStr());
  const [ate, setAte] = useState(todayStr());
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();
  const pageSize = 20;

  useEffect(() => { fetchAuditoria(); }, [page, entidade, acao, de, ate]);

  const fetchAuditoria = async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
      if (usuario) params.set('usuario', usuario);
      if (entidade) params.set('entidade', entidade);
      if (acao) params.set('acao', acao);
      if (de) params.set('de', `${de}T00:00:00`);
      if (ate) params.set('ate', `${ate}T23:59:59`);

      const { data: res } = await api.get<ApiResponse<PaginatedResponse<Auditoria>>>(`/auditoria?${params}`);
      if (res.success && res.data) {
        setItems(res.data.items);
        setTotal(res.data.total);
      }
    } catch { /* interceptor */ }
    setLoading(false);
  };

  const handleSearch = () => { setPage(1); fetchAuditoria(); };
  const pageCount = Math.ceil(total / pageSize) || 1;

  return (
    <div className="min-h-screen bg-gray-950 text-gray-100">
      <Header />
      <div className="max-w-6xl mx-auto p-8">
        <div className="flex justify-between items-center mb-6">
          <div>
            <h1 className="text-2xl font-mono text-green-400">Auditoria</h1>
            <p className="text-sm text-gray-500 mt-1">Histórico de ações no sistema</p>
          </div>
          <button onClick={() => navigate('/')} className="px-3 py-1.5 text-sm bg-gray-800 hover:bg-gray-700 rounded">Voltar</button>
        </div>

        {/* Filtros */}
        <div className="flex flex-wrap gap-3 mb-4 items-end">
          <div>
            <label className="block text-xs text-gray-500 mb-1">De</label>
            <input type="date" value={de} onChange={(e) => { setDe(e.target.value); setPage(1); }}
              className="px-3 py-1.5 bg-gray-800 border border-gray-700 rounded text-sm text-gray-100 focus:outline-none focus:border-green-500" />
          </div>

          <div>
            <label className="block text-xs text-gray-500 mb-1">Até</label>
            <input type="date" value={ate} onChange={(e) => { setAte(e.target.value); setPage(1); }}
              className="px-3 py-1.5 bg-gray-800 border border-gray-700 rounded text-sm text-gray-100 focus:outline-none focus:border-green-500" />
          </div>

          <div>
            <label className="block text-xs text-gray-500 mb-1">Entidade</label>
            <select value={entidade} onChange={(e) => { setEntidade(e.target.value); setPage(1); }}
              className="px-3 py-1.5 bg-gray-800 border border-gray-700 rounded text-sm text-gray-100">
              {ENTIDADES.map(e => <option key={e} value={e}>{e || 'Todas'}</option>)}
            </select>
          </div>

          <div>
            <label className="block text-xs text-gray-500 mb-1">Ação</label>
            <select value={acao} onChange={(e) => { setAcao(e.target.value); setPage(1); }}
              className="px-3 py-1.5 bg-gray-800 border border-gray-700 rounded text-sm text-gray-100">
              {ACOES.map(a => <option key={a} value={a}>{a || 'Todas'}</option>)}
            </select>
          </div>

          <div>
            <label className="block text-xs text-gray-500 mb-1">Usuário</label>
            <div className="flex gap-2">
              <input value={usuario} onChange={(e) => setUsuario(e.target.value)}
                placeholder="Buscar por nome..."
                onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                className="px-3 py-1.5 bg-gray-800 border border-gray-700 rounded text-sm text-gray-100 w-40 focus:outline-none focus:border-green-500" />
              <button onClick={handleSearch} className="px-3 py-1.5 text-sm bg-gray-700 hover:bg-gray-600 rounded">Buscar</button>
            </div>
          </div>

          <div className="ml-auto text-right">
            <p className="text-xs text-gray-600">{total} registro(s)</p>
          </div>
        </div>

        {/* Tabela */}
        {loading ? <p className="text-gray-500 py-8 text-center">Carregando...</p> : items.length === 0 ? (
          <div className="text-center py-12 text-gray-500">
            <p>Nenhum registro de auditoria encontrado.</p>
            <p className="text-xs mt-2">Tente alterar os filtros ou o período.</p>
          </div>
        ) : (
          <>
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-gray-500 border-b border-gray-800">
                  <th className="py-2 px-2">Data/Hora</th>
                  <th className="py-2 px-2">Usuário</th>
                  <th className="py-2 px-2">Entidade</th>
                  <th className="py-2 px-2">Ação</th>
                  <th className="py-2 px-2">Detalhe</th>
                </tr>
              </thead>
              <tbody>
                {items.map((a) => (
                  <tr key={a.cnAuditoria} className="border-b border-gray-800/50 hover:bg-gray-900/50">
                    <td className="py-2 px-2 text-xs text-gray-400">
                      {new Date(a.dtAcao).toLocaleString('pt-BR')}
                    </td>
                    <td className="py-2 px-2 text-xs text-gray-300">{a.nmUsuario}</td>
                    <td className="py-2 px-2 text-xs">
                      <span className="text-gray-300">{a.idEntidade}</span>
                      {a.idReferencia > 0 && <span className="text-gray-600 ml-1">#{a.idReferencia}</span>}
                    </td>
                    <td className="py-2 px-2">
                      <span className={`px-2 py-0.5 rounded text-xs ${badgeColor[a.idAcao] || 'bg-gray-800 text-gray-300'}`}>
                        {a.idAcao}
                      </span>
                    </td>
                    <td className="py-2 px-2 text-xs text-gray-400 truncate max-w-[200px]" title={a.dsDetalhe || ''}>
                      {a.dsDetalhe || '-'}
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
