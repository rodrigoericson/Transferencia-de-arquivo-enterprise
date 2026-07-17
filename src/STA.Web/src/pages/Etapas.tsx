import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../lib/api';
import type { ApiResponse, PaginatedResponse, Etapa, Rota, Destino } from '../types';
import Header from '../components/layout/Header';

interface EtapaCompleta extends Etapa {
  rotas: (Rota & { destinos: Destino[] })[];
}

export default function Etapas() {
  const [etapas, setEtapas] = useState<EtapaCompleta[]>([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();
  const canEdit = sessionStorage.getItem('sta_role') === 'Admin' || sessionStorage.getItem('sta_role') === 'Operator';

  useEffect(() => { fetchEtapas(); }, []);

  const fetchEtapas = async () => {
    try {
      const { data } = await api.get<ApiResponse<PaginatedResponse<Etapa>>>('/etapas?pageSize=50');
      if (data.success && data.data) {
        const completas: EtapaCompleta[] = [];
        for (const etapa of data.data.items) {
          const rotasRes = await api.get<ApiResponse<PaginatedResponse<Rota>>>(`/rotas?etapaId=${etapa.cnEtapa}&pageSize=10`);
          const rotas = rotasRes.data.data?.items ?? [];
          const rotasComDestinos = [];
          for (const rota of rotas) {
            const destRes = await api.get<ApiResponse<PaginatedResponse<Destino>>>(`/destinos?rotaId=${rota.cnRota}&pageSize=20`);
            rotasComDestinos.push({ ...rota, destinos: destRes.data.data?.items ?? [] });
          }
          completas.push({ ...etapa, rotas: rotasComDestinos });
        }
        completas.sort((a, b) => a.nmEtapa.localeCompare(b.nmEtapa, 'pt-BR'));
        setEtapas(completas);
      }
    } catch { /* interceptor */ }
    setLoading(false);
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Remover esta transferência e todas suas rotas/destinos?')) return;
    await api.delete(`/etapas/${id}`);
    fetchEtapas();
  };

  const handleToggle = async (etapa: EtapaCompleta) => {
    await api.put(`/etapas/${etapa.cnEtapa}`, {
      nmEtapa: etapa.nmEtapa,
      nrOrdemExecucao: etapa.nrOrdemExecucao,
      flAtivo: !etapa.flAtivo,
    });
    fetchEtapas();
  };

  if (loading) return <div className="min-h-screen bg-gray-950"><Header /><div className="p-8 text-gray-400">Carregando...</div></div>;

  return (
    <div className="min-h-screen bg-gray-950 text-gray-100">
      <Header />
      <div className="max-w-5xl mx-auto p-8">
        <div className="flex justify-between items-center mb-6">
          <div>
            <h1 className="text-2xl font-mono text-green-400">Transferências</h1>
            <p className="text-sm text-gray-500 mt-1">Gerencie suas cadeias de transferência de arquivos</p>
          </div>
          <div className="flex gap-3">
            <button onClick={() => navigate('/')} className="px-4 py-2 text-sm bg-gray-800 hover:bg-gray-700 rounded">Voltar</button>
            {canEdit && <button onClick={() => navigate('/etapas/nova')} className="px-4 py-2 text-sm bg-green-600 hover:bg-green-700 rounded">+ Nova Transferência</button>}
          </div>
        </div>

        {etapas.length === 0 && (
          <div className="text-center py-12 text-gray-500">
            <p className="mb-4">Nenhuma transferência cadastrada.</p>
            <button onClick={() => navigate('/etapas/nova')} className="px-4 py-2 text-sm bg-green-600 hover:bg-green-700 rounded">Criar primeira transferência</button>
          </div>
        )}

        <div className="space-y-4">
          {etapas.map((etapa) => (
            <div key={etapa.cnEtapa} className="bg-gray-900 rounded-lg border border-gray-800 p-5">
              {/* Header da card */}
              <div className="flex justify-between items-start mb-3">
                <div>
                  <div className="flex items-center gap-3">
                    <h3 className="text-lg font-medium text-gray-100">{etapa.nmEtapa}</h3>
                    <span className={`px-2 py-0.5 rounded text-xs ${etapa.flAtivo ? 'bg-green-900 text-green-300' : 'bg-red-900 text-red-300'}`}>
                      {etapa.flAtivo ? 'Ativo' : 'Inativo'}
                    </span>
                  </div>
                  <p className="text-xs text-gray-600 mt-1">Ordem: {etapa.nrOrdemExecucao} • Criado em {new Date(etapa.dtCriacao).toLocaleDateString('pt-BR')}</p>
                </div>
                {canEdit && (
                  <div className="flex gap-2">
                    <button onClick={() => navigate(`/etapas/${etapa.cnEtapa}/editar`)}
                      className="px-2 py-1 text-xs text-blue-400 hover:bg-blue-900/30 rounded">Editar</button>
                    <button onClick={() => handleToggle(etapa)}
                      className={`px-2 py-1 text-xs rounded ${etapa.flAtivo ? 'text-yellow-400 hover:bg-yellow-900/30' : 'text-green-400 hover:bg-green-900/30'}`}>
                      {etapa.flAtivo ? 'Desativar' : 'Ativar'}
                    </button>
                    <button onClick={() => handleDelete(etapa.cnEtapa)}
                      className="px-2 py-1 text-xs text-red-400 hover:bg-red-900/30 rounded">Excluir</button>
                  </div>
                )}
              </div>

              {/* Rotas e destinos */}
              {etapa.rotas.map((rota) => (
                <div key={rota.cnRota} className="mt-3 pl-4 border-l-2 border-gray-700">
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-3 text-sm">
                    <div>
                      <p className="text-xs text-gray-500 uppercase">Origem</p>
                      <p className="font-mono text-xs text-gray-300 truncate" title={rota.dsDiretorioOrigem}>{rota.dsDiretorioOrigem}</p>
                    </div>
                    <div>
                      <p className="text-xs text-gray-500 uppercase">Máscara</p>
                      <p className="font-mono text-xs text-green-400">{rota.dsMascaraArquivo}</p>
                    </div>
                    <div>
                      <p className="text-xs text-gray-500 uppercase">Compactação</p>
                      <p className="text-xs text-gray-300">{rota.dsCompactaOrigemTipo || 'Nenhuma'}</p>
                    </div>
                  </div>

                  {/* Destinos */}
                  <div className="mt-2">
                    <p className="text-xs text-gray-500 uppercase mb-1">Destinos ({rota.destinos.length})</p>
                    {rota.destinos.map((dest) => (
                      <div key={dest.cnRotaDestino} className="flex items-center gap-2 text-xs">
                        <span className="text-green-500">→</span>
                        <span className="font-mono text-gray-400 truncate" title={dest.dsDiretorioDestino}>{dest.dsDiretorioDestino}</span>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
