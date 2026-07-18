import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import api from '../lib/api';
import type { ApiResponse, PaginatedResponse, Destino } from '../types';
import Header from '../components/layout/Header';

export default function Destinos() {
  const { rotaId } = useParams<{ rotaId: string }>();
  const [destinos, setDestinos] = useState<Destino[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<Destino | null>(null);
  const navigate = useNavigate();

  useEffect(() => { fetchDestinos(); }, [rotaId]);

  const fetchDestinos = async () => {
    try {
      const { data } = await api.get<ApiResponse<PaginatedResponse<Destino>>>(`/destinos?rotaId=${rotaId}&pageSize=50`);
      if (data.success && data.data) setDestinos(data.data.items);
    } catch { /* interceptor */ }
    setLoading(false);
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Remover este destino?')) return;
    await api.delete(`/destinos/${id}`);
    fetchDestinos();
  };

  const handleSave = async (form: DestinoForm) => {
    if (editing) {
      await api.put(`/destinos/${editing.cnRotaDestino}`, form);
    } else {
      await api.post('/destinos', { ...form, cnRota: Number(rotaId) });
    }
    setShowForm(false);
    setEditing(null);
    fetchDestinos();
  };

  if (loading) return <div className="p-8 text-gray-400">Carregando...</div>;

  return (
    <div className="min-h-screen bg-gray-950 text-gray-100">
      <Header />
      <div className="max-w-5xl mx-auto p-8">
        <div className="flex justify-between items-center mb-6">
          <h1 className="text-2xl font-mono text-green-400">Destinos da Rota #{rotaId}</h1>
          <div className="flex gap-3">
            <button onClick={() => navigate(-1)} className="px-3 py-1.5 text-sm bg-gray-800 hover:bg-gray-700 rounded">Voltar</button>
            <button onClick={() => { setEditing(null); setShowForm(true); }} className="px-3 py-1.5 text-sm bg-green-600 hover:bg-green-700 rounded">Novo Destino</button>
          </div>
        </div>

        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-gray-500 border-b border-gray-800">
              <th className="py-2 px-3">#</th>
              <th className="py-2 px-3">Diretório Destino</th>
              <th className="py-2 px-3">Descompacta</th>
              <th className="py-2 px-3">Rename</th>
              <th className="py-2 px-3">Status</th>
              <th className="py-2 px-3">Ações</th>
            </tr>
          </thead>
          <tbody>
            {destinos.map((d) => (
              <tr key={d.cnRotaDestino} className="border-b border-gray-800/50 hover:bg-gray-900/50">
                <td className="py-2 px-3 text-gray-500">{d.nrOrdem}</td>
                <td className="py-2 px-3 font-mono text-xs">{d.dsDiretorioDestino}</td>
                <td className="py-2 px-3 text-xs">{d.dsDescompactaDestino || '-'}</td>
                <td className="py-2 px-3 text-xs font-mono text-yellow-400">{d.dsPadraoRename || '-'}</td>
                <td className="py-2 px-3">
                  <span className={`px-2 py-0.5 rounded text-xs ${d.flAtivo ? 'bg-green-900 text-green-300' : 'bg-red-900 text-red-300'}`}>
                    {d.flAtivo ? 'Ativo' : 'Inativo'}
                  </span>
                </td>
                <td className="py-2 px-3 space-x-2">
                  <button onClick={() => { setEditing(d); setShowForm(true); }} className="text-blue-400 hover:text-blue-300 text-xs">Editar</button>
                  <button onClick={() => handleDelete(d.cnRotaDestino)} className="text-red-400 hover:text-red-300 text-xs">Excluir</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>

        {destinos.length === 0 && <p className="text-gray-500 mt-4">Nenhum destino cadastrado.</p>}

        {showForm && (
          <DestinoFormModal
            initial={editing}
            onSave={handleSave}
            onCancel={() => { setShowForm(false); setEditing(null); }}
          />
        )}
      </div>
    </div>
  );
}

interface DestinoForm {
  nrOrdem: number;
  dsDiretorioDestino: string;
  dsDescompactaDestino: string;
  dsPadraoRename: string;
  flAtivo: boolean;
}

function DestinoFormModal({ initial, onSave, onCancel }: {
  initial: Destino | null;
  onSave: (form: DestinoForm) => void;
  onCancel: () => void;
}) {
  const [form, setForm] = useState<DestinoForm>({
    nrOrdem: initial?.nrOrdem ?? 1,
    dsDiretorioDestino: initial?.dsDiretorioDestino ?? '',
    dsDescompactaDestino: initial?.dsDescompactaDestino ?? '',
    dsPadraoRename: initial?.dsPadraoRename ?? '',
    flAtivo: initial?.flAtivo ?? true,
  });

  const handleSubmit = (e: React.FormEvent) => { e.preventDefault(); onSave(form); };
  const set = (key: keyof DestinoForm, value: string | number | boolean) => setForm({ ...form, [key]: value });

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <form onSubmit={handleSubmit} className="bg-gray-900 p-6 rounded-lg border border-gray-700 w-full max-w-md space-y-4">
        <h2 className="text-lg text-green-400 font-mono">{initial ? 'Editar Destino' : 'Novo Destino'}</h2>

        <div>
          <label className="block text-xs text-gray-400 mb-1">Diretório de Destino</label>
          <input value={form.dsDiretorioDestino} onChange={(e) => set('dsDiretorioDestino', e.target.value)}
            className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm focus:outline-none focus:border-green-500" />
        </div>

        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="block text-xs text-gray-400 mb-1">Descompactar no destino</label>
            <select value={form.dsDescompactaDestino} onChange={(e) => set('dsDescompactaDestino', e.target.value)}
              className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm">
              <option value="">Não</option>
              <option value="SIM">Sim</option>
            </select>
          </div>
          <div>
            <label className="block text-xs text-gray-400 mb-1">Ordem</label>
            <input type="number" value={form.nrOrdem} onChange={(e) => set('nrOrdem', Number(e.target.value))}
              className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm focus:outline-none focus:border-green-500" />
          </div>
        </div>

        <div>
          <label className="block text-xs text-gray-400 mb-1">Padrão de Rename <span className="text-gray-600">(opcional)</span></label>
          <input value={form.dsPadraoRename} onChange={(e) => set('dsPadraoRename', e.target.value)}
            placeholder="Ex: {NAME}_{DATE}{EXT}"
            className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm focus:outline-none focus:border-green-500" />
          <p className="text-xs text-gray-600 mt-1">Placeholders: {'{NAME}'} {'{DATE}'} {'{TIME}'} {'{EXT}'}</p>
        </div>

        {initial && (
          <div className="flex items-center gap-2">
            <input type="checkbox" checked={form.flAtivo} onChange={(e) => set('flAtivo', e.target.checked)} id="ativoDest" />
            <label htmlFor="ativoDest" className="text-sm text-gray-400">Ativo</label>
          </div>
        )}

        <div className="flex gap-3 justify-end pt-2">
          <button type="button" onClick={onCancel} className="px-4 py-2 text-sm bg-gray-700 hover:bg-gray-600 rounded">Cancelar</button>
          <button type="submit" className="px-4 py-2 text-sm bg-green-600 hover:bg-green-700 rounded">Salvar</button>
        </div>
      </form>
    </div>
  );
}
