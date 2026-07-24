import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import api from '../lib/api';
import type { ApiResponse, PaginatedResponse, Rota } from '../types';
import Header from '../components/layout/Header';

export default function Rotas() {
  const { etapaId } = useParams<{ etapaId: string }>();
  const [rotas, setRotas] = useState<Rota[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<Rota | null>(null);
  const navigate = useNavigate();

  useEffect(() => { fetchRotas(); }, [etapaId]);

  const fetchRotas = async () => {
    try {
      const { data } = await api.get<ApiResponse<PaginatedResponse<Rota>>>(`/rotas?etapaId=${etapaId}&pageSize=50`);
      if (data.success && data.data) setRotas(data.data.items);
    } catch { /* interceptor */ }
    setLoading(false);
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Remover esta rota e seus destinos?')) return;
    await api.delete(`/rotas/${id}`);
    fetchRotas();
  };

  const handleSave = async (form: RotaForm) => {
    if (editing) {
      await api.put(`/rotas/${editing.cnRota}`, form);
    } else {
      await api.post('/rotas', { ...form, cnEtapa: Number(etapaId) });
    }
    setShowForm(false);
    setEditing(null);
    fetchRotas();
  };

  if (loading) return <div className="p-8 text-gray-400">Carregando...</div>;

  return (
    <div className="min-h-screen bg-gray-950 text-gray-100">
      <Header />
      <div className="max-w-6xl mx-auto p-8">
        <div className="flex justify-between items-center mb-6">
          <h1 className="text-2xl font-mono text-green-400">Rotas da Etapa #{etapaId}</h1>
          <div className="flex gap-3">
            <button onClick={() => navigate('/etapas')} className="px-3 py-1.5 text-sm bg-gray-800 hover:bg-gray-700 rounded">Voltar</button>
            <button onClick={() => { setEditing(null); setShowForm(true); }} className="px-3 py-1.5 text-sm bg-green-600 hover:bg-green-700 rounded">Nova Rota</button>
          </div>
        </div>

        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-gray-500 border-b border-gray-800">
              <th className="py-2 px-2">#</th>
              <th className="py-2 px-2">Origem</th>
              <th className="py-2 px-2">Máscara</th>
              <th className="py-2 px-2">Destinos</th>
              <th className="py-2 px-2">Retorno</th>
              <th className="py-2 px-2">Status</th>
              <th className="py-2 px-2">Ações</th>
            </tr>
          </thead>
          <tbody>
            {rotas.map((r) => (
              <tr key={r.cnRota} className="border-b border-gray-800/50 hover:bg-gray-900/50">
                <td className="py-2 px-2 text-gray-500">{r.nrOrdem}</td>
                <td className="py-2 px-2 font-mono text-xs truncate max-w-[300px]" title={r.dsDiretorioOrigem}>{r.dsDiretorioOrigem}</td>
                <td className="py-2 px-2 font-mono text-xs">{r.dsMascaraArquivo}</td>
                <td className="py-2 px-2">{r.quantidadeDestinos}</td>
                <td className="py-2 px-2">
                  {r.flHabilitarRetorno ? (
                    <span className="px-2 py-0.5 rounded text-xs bg-purple-900 text-purple-300">↩ SFTP</span>
                  ) : (
                    <span className="text-xs text-gray-600">—</span>
                  )}
                </td>
                <td className="py-2 px-2">
                  <span className={`px-2 py-0.5 rounded text-xs ${r.flAtivo ? 'bg-green-900 text-green-300' : 'bg-red-900 text-red-300'}`}>
                    {r.flAtivo ? 'Ativo' : 'Inativo'}
                  </span>
                </td>
                <td className="py-2 px-2 space-x-2">
                  <button onClick={() => { setEditing(r); setShowForm(true); }} className="text-blue-400 hover:text-blue-300 text-xs">Editar</button>
                  <button onClick={() => navigate(`/rotas/${r.cnRota}/destinos`)} className="text-gray-400 hover:text-gray-300 text-xs">Destinos</button>
                  <button onClick={() => handleDelete(r.cnRota)} className="text-red-400 hover:text-red-300 text-xs">Excluir</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>

        {showForm && (
          <RotaFormModal
            initial={editing}
            onSave={handleSave}
            onCancel={() => { setShowForm(false); setEditing(null); }}
          />
        )}
      </div>
    </div>
  );
}

interface RotaForm {
  nrOrdem: number;
  dsDiretorioOrigem: string;
  dsDiretorioBackup: string;
  dsMascaraArquivo: string;
  dsCompactaOrigemTipo: string;
  nrDiasExcluir: number;
  flAtivo: boolean;
  flHabilitarRetorno: boolean;
  cnConexaoSftpRetorno: number | null;
  dsDiretorioRetorno: string | null;
  dsMascaraRetorno: string;
  dsDiretorioLocalRetorno: string | null;
}

function RotaFormModal({ initial, onSave, onCancel }: {
  initial: Rota | null;
  onSave: (form: RotaForm) => void;
  onCancel: () => void;
}) {
  const [form, setForm] = useState<RotaForm>({
    nrOrdem: initial?.nrOrdem ?? 1,
    dsDiretorioOrigem: initial?.dsDiretorioOrigem ?? '',
    dsDiretorioBackup: initial?.dsDiretorioBackup ?? '',
    dsMascaraArquivo: initial?.dsMascaraArquivo ?? '*',
    dsCompactaOrigemTipo: initial?.dsCompactaOrigemTipo ?? '',
    nrDiasExcluir: initial?.nrDiasExcluir ?? 0,
    flAtivo: initial?.flAtivo ?? true,
    flHabilitarRetorno: initial?.flHabilitarRetorno ?? false,
    cnConexaoSftpRetorno: initial?.cnConexaoSftpRetorno ?? null,
    dsDiretorioRetorno: initial?.dsDiretorioRetorno ?? null,
    dsMascaraRetorno: initial?.dsMascaraRetorno ?? '*',
    dsDiretorioLocalRetorno: initial?.dsDiretorioLocalRetorno ?? null,
  });

  const handleSubmit = (e: React.FormEvent) => { e.preventDefault(); onSave(form); };
  const set = (key: keyof RotaForm, value: string | number | boolean | null) => setForm({ ...form, [key]: value });

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <form onSubmit={handleSubmit} className="bg-gray-900 p-6 rounded-lg border border-gray-700 w-full max-w-lg space-y-3">
        <h2 className="text-lg text-green-400 font-mono">{initial ? 'Editar Rota' : 'Nova Rota'}</h2>

        <div>
          <label className="block text-xs text-gray-400 mb-1">Diretório de Origem</label>
          <input value={form.dsDiretorioOrigem} onChange={(e) => set('dsDiretorioOrigem', e.target.value)}
            className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm focus:outline-none focus:border-green-500" />
        </div>

        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="block text-xs text-gray-400 mb-1">Máscara</label>
            <input value={form.dsMascaraArquivo} onChange={(e) => set('dsMascaraArquivo', e.target.value)}
              className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm focus:outline-none focus:border-green-500" />
          </div>
          <div>
            <label className="block text-xs text-gray-400 mb-1">Ordem</label>
            <input type="number" value={form.nrOrdem} onChange={(e) => set('nrOrdem', Number(e.target.value))}
              className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm focus:outline-none focus:border-green-500" />
          </div>
        </div>

        <div>
          <label className="block text-xs text-gray-400 mb-1">Diretório de Backup</label>
          <input value={form.dsDiretorioBackup} onChange={(e) => set('dsDiretorioBackup', e.target.value)}
            className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm focus:outline-none focus:border-green-500" />
        </div>

        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="block text-xs text-gray-400 mb-1">Compactação</label>
            <select value={form.dsCompactaOrigemTipo} onChange={(e) => set('dsCompactaOrigemTipo', e.target.value)}
              className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm">
              <option value="">Nenhuma</option>
              <option value="7Z">7-Zip</option>
              <option value="ZIP">ZIP</option>
            </select>
          </div>
          <div>
            <label className="block text-xs text-gray-400 mb-1">Dias para excluir backup</label>
            <input type="number" value={form.nrDiasExcluir} onChange={(e) => set('nrDiasExcluir', Number(e.target.value))}
              className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm focus:outline-none focus:border-green-500" />
          </div>
        </div>

        {initial && (
          <div className="flex items-center gap-2">
            <input type="checkbox" checked={form.flAtivo} onChange={(e) => set('flAtivo', e.target.checked)} id="ativoRota" />
            <label htmlFor="ativoRota" className="text-sm text-gray-400">Ativo</label>
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
