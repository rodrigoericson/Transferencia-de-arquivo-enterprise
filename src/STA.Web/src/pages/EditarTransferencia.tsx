import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import api from '../lib/api';
import type { ApiResponse, PaginatedResponse, Rota, Destino } from '../types';
import Header from '../components/layout/Header';
import RenameConfig from '../components/shared/RenameConfig';

export default function EditarTransferencia() {
  const { etapaId } = useParams<{ etapaId: string }>();
  const navigate = useNavigate();
  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [nome, setNome] = useState('');
  const [mascara, setMascara] = useState('*');
  const [origem, setOrigem] = useState('');
  const [backup, setBackup] = useState('');
  const [destinos, setDestinos] = useState<{dir: string, rename: string}[]>([{dir: '', rename: ''}]);
  const [compactar, setCompactar] = useState(false);
  const [retencao, setRetencao] = useState(365);
  const [flAtivo, setFlAtivo] = useState(true);

  // IDs para atualização
  const [cnRota, setCnRota] = useState<number | null>(null);
  const [destinoIds, setDestinoIds] = useState<(number | null)[]>([]);

  useEffect(() => { carregarDados(); }, [etapaId]);

  const carregarDados = async () => {
    try {
      // Carregar etapa
      const etapaRes = await api.get(`/etapas/${etapaId}`);
      const etapa = etapaRes.data.data;
      setNome(etapa.nmEtapa);
      setFlAtivo(etapa.flAtivo);

      // Carregar rotas
      const rotasRes = await api.get<ApiResponse<PaginatedResponse<Rota>>>(`/rotas?etapaId=${etapaId}&pageSize=1`);
      const rota = rotasRes.data.data?.items[0];
      if (rota) {
        setCnRota(rota.cnRota);
        setOrigem(rota.dsDiretorioOrigem);
        setBackup(rota.dsDiretorioBackup || '');
        setMascara(rota.dsMascaraArquivo);
        setCompactar(!!rota.dsCompactaOrigemTipo);
        setRetencao(rota.nrDiasExcluir || 365);

        // Carregar destinos
        const destRes = await api.get<ApiResponse<PaginatedResponse<Destino>>>(`/destinos?rotaId=${rota.cnRota}&pageSize=20`);
        const dests = destRes.data.data?.items ?? [];
        setDestinos(dests.length > 0 ? dests.map(d => ({dir: d.dsDiretorioDestino, rename: d.dsPadraoRename || ''})) : [{dir: '', rename: ''}]);
        setDestinoIds(dests.map(d => d.cnRotaDestino));
      }
    } catch { setError('Erro ao carregar dados.'); }
    setLoading(false);
  };

  const addDestino = () => { setDestinos([...destinos, {dir: '', rename: ''}]); setDestinoIds([...destinoIds, null]); };
  const removeDestino = (i: number) => {
    setDestinos(destinos.filter((_, idx) => idx !== i));
    setDestinoIds(destinoIds.filter((_, idx) => idx !== i));
  };
  const updateDestino = (i: number, field: 'dir' | 'rename', value: string) => {
    const copy = [...destinos];
    copy[i] = { ...copy[i], [field]: value };
    setDestinos(copy);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!nome.trim()) { setError('Nome é obrigatório.'); return; }
    if (!origem.trim()) { setError('Origem é obrigatória.'); return; }
    if (destinos.filter(d => d.dir.trim()).length === 0) { setError('Adicione pelo menos um destino.'); return; }

    setSaving(true);
    try {
      // 1. Atualizar etapa
      await api.put(`/etapas/${etapaId}`, {
        nmEtapa: nome,
        nrOrdemExecucao: 1,
        flAtivo,
      });

      // 2. Atualizar rota
      if (cnRota) {
        await api.put(`/rotas/${cnRota}`, {
          nrOrdem: 1,
          dsDiretorioOrigem: origem.trim(),
          dsDiretorioBackup: backup.trim() || null,
          dsMascaraArquivo: mascara.trim() || '*',
          dsCompactaOrigemTipo: compactar ? '7Z' : null,
          nrDiasExcluir: retencao,
          flAtivo: true,
        });
      }

      // 3. Atualizar destinos (delete existentes + recriar)
      if (cnRota) {
        // Deletar destinos antigos
        for (const id of destinoIds) {
          if (id) await api.delete(`/destinos/${id}`);
        }
        // Criar novos
        const destinosValidos = destinos.filter(d => d.dir.trim());
        for (let i = 0; i < destinosValidos.length; i++) {
          await api.post('/destinos', {
            cnRota,
            nrOrdem: i + 1,
            dsDiretorioDestino: destinosValidos[i].dir.trim(),
            dsPadraoRename: destinosValidos[i].rename.trim() || null,
          });
        }
      }

      alert('Transferência salva com sucesso!');
      navigate('/etapas');
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Erro ao salvar.';
      setError(msg);
    }
    setSaving(false);
  };

  if (loading) return <div className="min-h-screen bg-gray-950"><Header /><div className="p-8 text-gray-400">Carregando...</div></div>;

  return (
    <div className="min-h-screen bg-gray-950 text-gray-100">
      <Header />
      <div className="max-w-2xl mx-auto p-8">
        <div className="flex justify-between items-center mb-6">
          <h1 className="text-2xl font-mono text-green-400">Editar Transferência</h1>
          <button onClick={() => navigate('/etapas')} className="px-3 py-1.5 text-sm bg-gray-800 hover:bg-gray-700 rounded">Voltar</button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">

          {/* Nome + Máscara + Status */}
          <div className="bg-gray-900 p-4 rounded-lg border border-gray-800 space-y-3">
            <div>
              <label className="block text-xs text-gray-400 mb-1">Nome da transferência</label>
              <input value={nome} onChange={(e) => setNome(e.target.value)}
                className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm focus:outline-none focus:border-green-500" />
            </div>
            <div>
              <label className="block text-xs text-gray-400 mb-1">Máscara do arquivo</label>
              <input value={mascara} onChange={(e) => setMascara(e.target.value)}
                className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm focus:outline-none focus:border-green-500" />
            </div>
            <div className="flex items-center gap-2">
              <input type="checkbox" id="flAtivo" checked={flAtivo} onChange={(e) => setFlAtivo(e.target.checked)} className="w-4 h-4" />
              <label htmlFor="flAtivo" className="text-sm text-gray-300">Transferência ativa</label>
            </div>
          </div>

          {/* Origem */}
          <div className="bg-gray-900 p-4 rounded-lg border border-gray-800">
            <h2 className="text-sm text-green-400 font-mono mb-3">📂 ORIGEM</h2>
            <input value={origem} onChange={(e) => setOrigem(e.target.value)}
              placeholder="\\servidor\pasta\onde-arquivo-é-gerado"
              className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm font-mono focus:outline-none focus:border-green-500" />
          </div>

          {/* Backup */}
          <div className="bg-gray-900 p-4 rounded-lg border border-gray-800">
            <h2 className="text-sm text-yellow-400 font-mono mb-3">💾 BACKUP</h2>
            <input value={backup} onChange={(e) => setBackup(e.target.value)}
              placeholder="\\servidor\pasta\backup (opcional)"
              className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm font-mono focus:outline-none focus:border-green-500" />
          </div>

          {/* Destinos */}
          <div className="bg-gray-900 p-4 rounded-lg border border-gray-800">
            <h2 className="text-sm text-blue-400 font-mono mb-3">📤 DESTINOS</h2>
            <div className="space-y-3">
              {destinos.map((d, i) => (
                <div key={i} className="bg-gray-800/50 p-3 rounded border border-gray-700/50 space-y-2">
                  <div className="flex gap-2">
                    <input value={d.dir} onChange={(e) => updateDestino(i, 'dir', e.target.value)}
                      placeholder={`\\servidor\pasta\destino-${i + 1}`}
                      className="flex-1 px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm font-mono focus:outline-none focus:border-green-500" />
                    {destinos.length > 1 && (
                      <button type="button" onClick={() => removeDestino(i)}
                        className="px-2 text-red-400 hover:text-red-300 text-lg">×</button>
                    )}
                  </div>
                  <RenameConfig
                    value={d.rename}
                    onChange={(v) => updateDestino(i, 'rename', v)}
                  />
                </div>
              ))}
            </div>
            <button type="button" onClick={addDestino}
              className="mt-3 px-3 py-1.5 text-sm text-green-400 border border-green-700 hover:bg-green-900/30 rounded">
              + Adicionar outro destino
            </button>
          </div>

          {/* Opções */}
          <div className="bg-gray-900 p-4 rounded-lg border border-gray-800">
            <h2 className="text-sm text-gray-400 font-mono mb-3">⚙️ OPÇÕES</h2>
            <div className="flex items-center gap-3 mb-4">
              <input type="checkbox" id="compactar" checked={compactar} onChange={(e) => setCompactar(e.target.checked)} className="w-4 h-4" />
              <label htmlFor="compactar" className="text-sm text-gray-300">Compactar na origem (7-Zip)</label>
            </div>
            <div>
              <label className="block text-xs text-gray-400 mb-2">Retenção de backup (dias)</label>
              <div className="flex gap-4">
                {[365, 120, 60].map((dias) => (
                  <label key={dias} className="flex items-center gap-1.5 text-sm text-gray-300 cursor-pointer">
                    <input type="radio" name="retencao" value={dias} checked={retencao === dias}
                      onChange={() => setRetencao(dias)} />
                    {dias} dias
                  </label>
                ))}
              </div>
            </div>
          </div>

          {error && <p className="text-red-400 text-sm">{error}</p>}

          <div className="flex gap-3 justify-end">
            <button type="button" onClick={() => navigate('/etapas')}
              className="px-4 py-2 text-sm bg-gray-700 hover:bg-gray-600 rounded">Cancelar</button>
            <button type="submit" disabled={saving}
              className="px-6 py-2 text-sm bg-green-600 hover:bg-green-700 rounded font-medium disabled:opacity-50">
              {saving ? 'Salvando...' : 'Salvar Alterações'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
