import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../lib/api';
import type { ApiResponse, PaginatedResponse, Etapa } from '../types';

export default function Etapas() {
  const [etapas, setEtapas] = useState<Etapa[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<Etapa | null>(null);
  const navigate = useNavigate();

  useEffect(() => { fetchEtapas(); }, []);

  const fetchEtapas = async () => {
    try {
      const { data } = await api.get<ApiResponse<PaginatedResponse<Etapa>>>('/etapas?pageSize=50');
      if (data.success && data.data) setEtapas(data.data.items);
    } catch { /* interceptor handles 401 */ }
    setLoading(false);
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Tem certeza que deseja remover esta etapa?')) return;
    await api.delete(`/etapas/${id}`);
    fetchEtapas();
  };

  const handleSave = async (form: { nmEtapa: string; nrOrdemExecucao: number; flAtivo: boolean }) => {
    if (editing) {
      await api.put(`/etapas/${editing.cnEtapa}`, { ...form });
    } else {
      await api.post('/etapas', { nmEtapa: form.nmEtapa, nrOrdemExecucao: form.nrOrdemExecucao });
    }
    setShowForm(false);
    setEditing(null);
    fetchEtapas();
  };

  if (loading) return <div className="p-8 text-gray-400">Carregando...</div>;

  return (
    <div className="min-h-screen bg-gray-950 text-gray-100 p-8">
      <div className="max-w-5xl mx-auto">
        <div className="flex justify-between items-center mb-6">
          <h1 className="text-2xl font-mono text-green-400">Etapas de Transferência</h1>
          <div className="flex gap-3">
            <button onClick={() => navigate('/')} className="px-3 py-1.5 text-sm bg-gray-800 hover:bg-gray-700 rounded">Voltar</button>
            <button onClick={() => navigate('/etapas/nova')} className="px-3 py-1.5 text-sm bg-green-600 hover:bg-green-700 rounded">Nova Transferência</button>
          </div>
        </div>

        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-gray-500 border-b border-gray-800">
              <th className="py-2 px-3">#</th>
              <th className="py-2 px-3">Nome</th>
              <th className="py-2 px-3">Ordem</th>
              <th className="py-2 px-3">Rotas</th>
              <th className="py-2 px-3">Status</th>
              <th className="py-2 px-3">Ações</th>
            </tr>
          </thead>
          <tbody>
            {etapas.map((e) => (
              <tr key={e.cnEtapa} className="border-b border-gray-800/50 hover:bg-gray-900/50">
                <td className="py-2 px-3 text-gray-500">{e.cnEtapa}</td>
                <td className="py-2 px-3">{e.nmEtapa}</td>
                <td className="py-2 px-3">{e.nrOrdemExecucao}</td>
                <td className="py-2 px-3">{e.quantidadeRotas}</td>
                <td className="py-2 px-3">
                  <span className={`px-2 py-0.5 rounded text-xs ${e.flAtivo ? 'bg-green-900 text-green-300' : 'bg-red-900 text-red-300'}`}>
                    {e.flAtivo ? 'Ativo' : 'Inativo'}
                  </span>
                </td>
                <td className="py-2 px-3 space-x-2">
                  <button onClick={() => { setEditing(e); setShowForm(true); }} className="text-blue-400 hover:text-blue-300 text-xs">Editar</button>
                  <button onClick={() => navigate(`/etapas/${e.cnEtapa}/rotas`)} className="text-gray-400 hover:text-gray-300 text-xs">Rotas</button>
                  <button onClick={() => handleDelete(e.cnEtapa)} className="text-red-400 hover:text-red-300 text-xs">Excluir</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>

        {showForm && (
          <EtapaForm
            initial={editing}
            onSave={handleSave}
            onCancel={() => { setShowForm(false); setEditing(null); }}
          />
        )}
      </div>
    </div>
  );
}

function EtapaForm({ initial, onSave, onCancel }: {
  initial: Etapa | null;
  onSave: (form: { nmEtapa: string; nrOrdemExecucao: number; flAtivo: boolean }) => void;
  onCancel: () => void;
}) {
  const [nmEtapa, setNmEtapa] = useState(initial?.nmEtapa ?? '');
  const [nrOrdem, setNrOrdem] = useState(initial?.nrOrdemExecucao ?? 1);
  const [flAtivo, setFlAtivo] = useState(initial?.flAtivo ?? true);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSave({ nmEtapa, nrOrdemExecucao: nrOrdem, flAtivo });
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <form onSubmit={handleSubmit} className="bg-gray-900 p-6 rounded-lg border border-gray-700 w-full max-w-md space-y-4">
        <h2 className="text-lg text-green-400 font-mono">{initial ? 'Editar Etapa' : 'Nova Etapa'}</h2>

        <div>
          <label className="block text-sm text-gray-400 mb-1">Nome</label>
          <input value={nmEtapa} onChange={(e) => setNmEtapa(e.target.value)}
            className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 focus:outline-none focus:border-green-500" />
        </div>

        <div>
          <label className="block text-sm text-gray-400 mb-1">Ordem de Execução</label>
          <input type="number" value={nrOrdem} onChange={(e) => setNrOrdem(Number(e.target.value))}
            className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 focus:outline-none focus:border-green-500" />
        </div>

        {initial && (
          <div className="flex items-center gap-2">
            <input type="checkbox" checked={flAtivo} onChange={(e) => setFlAtivo(e.target.checked)} id="ativo" />
            <label htmlFor="ativo" className="text-sm text-gray-400">Ativo</label>
          </div>
        )}

        <div className="flex gap-3 justify-end">
          <button type="button" onClick={onCancel} className="px-4 py-2 text-sm bg-gray-700 hover:bg-gray-600 rounded">Cancelar</button>
          <button type="submit" className="px-4 py-2 text-sm bg-green-600 hover:bg-green-700 rounded">Salvar</button>
        </div>
      </form>
    </div>
  );
}
