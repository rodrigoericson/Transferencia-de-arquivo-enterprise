import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';
import api from '../lib/api';
import Header from '../components/layout/Header';
import DiretorioInput from '../components/shared/DiretorioInput';
import { useValidarDiretorio } from '../hooks/useValidarDiretorio';

export default function NovaTransferencia() {
  const navigate = useNavigate();
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const { resultado, validar } = useValidarDiretorio();

  const [nome, setNome] = useState('');
  const [mascara, setMascara] = useState('*');
  const [origem, setOrigem] = useState('');
  const [backup, setBackup] = useState('');
  const [destinos, setDestinos] = useState<{dir: string, rename: string}[]>([{dir: '', rename: ''}]);
  const [compactar, setCompactar] = useState(false);
  const [retencao, setRetencao] = useState(365);

  const addDestino = () => setDestinos([...destinos, {dir: '', rename: ''}]);
  const removeDestino = (i: number) => setDestinos(destinos.filter((_, idx) => idx !== i));
  const updateDestino = (i: number, field: 'dir' | 'rename', value: string) => {
    const copy = [...destinos];
    copy[i] = { ...copy[i], [field]: value };
    setDestinos(copy);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!nome.trim()) { setError('Nome da transferência é obrigatório.'); return; }
    if (!origem.trim()) { setError('Diretório de origem é obrigatório.'); return; }
    if (destinos.filter(d => d.dir.trim()).length === 0) { setError('Adicione pelo menos um destino.'); return; }

    setSaving(true);
    try {
      // 1. Criar etapa
      const etapaRes = await api.post('/etapas', {
        nmEtapa: nome,
        nrOrdemExecucao: 1,
      });
      const cnEtapa = etapaRes.data.data.cnEtapa;

      // 2. Criar rota
      const rotaRes = await api.post('/rotas', {
        cnEtapa,
        nrOrdem: 1,
        dsDiretorioOrigem: origem.trim(),
        dsDiretorioBackup: backup.trim() || null,
        dsMascaraArquivo: mascara.trim() || '*',
        dsCompactaOrigemTipo: compactar ? '7Z' : null,
        nrDiasExcluir: retencao,
      });
      const cnRota = rotaRes.data.data.cnRota;

      // 3. Criar destinos
      const destinosValidos = destinos.filter(d => d.dir.trim());
      for (let i = 0; i < destinosValidos.length; i++) {
        await api.post('/destinos', {
          cnRota,
          nrOrdem: i + 1,
          dsDiretorioDestino: destinosValidos[i].dir.trim(),
          dsPadraoRename: destinosValidos[i].rename.trim() || null,
        });
      }

      alert('Transferência criada com sucesso!');
      navigate('/etapas');
    } catch (err: unknown) {
      if (axios.isAxiosError(err) && err.response?.data?.message) {
        setError(err.response.data.message);
      } else {
        setError(err instanceof Error ? err.message : 'Erro ao salvar.');
      }
    }
    setSaving(false);
  };

  return (
    <div className="min-h-screen bg-gray-950 text-gray-100">
      <Header />
      <div className="max-w-2xl mx-auto p-8">
        <div className="flex justify-between items-center mb-6">
          <h1 className="text-2xl font-mono text-green-400">Nova Transferência</h1>
          <button onClick={() => navigate('/')} className="px-3 py-1.5 text-sm bg-gray-800 hover:bg-gray-700 rounded">Voltar</button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">

          {/* Nome + Máscara */}
          <div className="bg-gray-900 p-4 rounded-lg border border-gray-800 space-y-3">
            <div>
              <label className="block text-xs text-gray-400 mb-1">Nome da transferência</label>
              <input value={nome} onChange={(e) => setNome(e.target.value)}
                placeholder="Ex: Carga Assessoria Services"
                className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm focus:outline-none focus:border-green-500" />
            </div>
            <div>
              <label className="block text-xs text-gray-400 mb-1">Máscara do arquivo</label>
              <input value={mascara} onChange={(e) => setMascara(e.target.value)}
                placeholder="* ou COBRANCA_PRD* ou *.REM"
                className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm focus:outline-none focus:border-green-500" />
              <p className="text-xs text-gray-600 mt-1">Use * para qualquer arquivo ou parte do nome (ex: CARGA_* pega todos que começam com CARGA_)</p>
            </div>
          </div>

          {/* Origem */}
          <div className="bg-gray-900 p-4 rounded-lg border border-gray-800">
            <h2 className="text-sm text-green-400 font-mono mb-3">📂 ORIGEM</h2>
            <DiretorioInput
              value={origem}
              onChange={setOrigem}
              placeholder="\\servidor\pasta\onde-arquivo-é-gerado"
              validacao={resultado['origem']}
              onValidar={() => validar('origem', origem)}
            />
            <p className="text-xs text-gray-600 mt-1">Diretório onde o arquivo é gerado ou depositado</p>
          </div>

          {/* Backup */}
          <div className="bg-gray-900 p-4 rounded-lg border border-gray-800">
            <h2 className="text-sm text-yellow-400 font-mono mb-3">💾 BACKUP</h2>
            <DiretorioInput
              value={backup}
              onChange={setBackup}
              placeholder="\\servidor\pasta\backup (opcional)"
              validacao={resultado['backup']}
              onValidar={() => validar('backup', backup)}
            />
            <p className="text-xs text-gray-600 mt-1">Cópia de segurança antes de transferir (deixe vazio se não precisar)</p>
          </div>

          {/* Destinos */}
          <div className="bg-gray-900 p-4 rounded-lg border border-gray-800">
            <h2 className="text-sm text-blue-400 font-mono mb-3">📤 DESTINOS</h2>
            <div className="space-y-3">
              {destinos.map((d, i) => (
                <div key={i} className="space-y-1">
                  <div className="flex gap-2 items-start">
                    <div className="flex-1">
                      <DiretorioInput
                        value={d.dir}
                        onChange={(v) => updateDestino(i, 'dir', v)}
                        placeholder={`\\servidor\pasta\destino-${i + 1}`}
                        validacao={resultado[`destino-${i}`]}
                        onValidar={() => validar(`destino-${i}`, d.dir)}
                      />
                    </div>
                    {destinos.length > 1 && (
                      <button type="button" onClick={() => removeDestino(i)}
                        className="px-2 pt-2 text-red-400 hover:text-red-300 text-lg">×</button>
                    )}
                  </div>
                  <input
                    value={d.rename}
                    onChange={(e) => updateDestino(i, 'rename', e.target.value)}
                    placeholder="Renomear para: COBREM_{NAME}_ATIVOS_{DATE}.dat (opcional)"
                    className="w-full ml-0 px-3 py-1.5 bg-gray-800/50 border border-gray-700/50 rounded text-gray-100 text-xs font-mono focus:outline-none focus:border-green-500"
                  />
                </div>
              ))}
            </div>
            <button type="button" onClick={addDestino}
              className="mt-3 px-3 py-1.5 text-sm text-green-400 border border-green-700 hover:bg-green-900/30 rounded">
              + Adicionar outro destino
            </button>
            <p className="text-xs text-gray-600 mt-2">
              Variáveis: <code className="text-green-400">{'{NAME}'}</code>, <code className="text-green-400">{'{EXT}'}</code>, <code className="text-green-400">{'{DATE}'}</code>, <code className="text-green-400">{'{TIME}'}</code>
            </p>
          </div>

          {/* Opções */}
          <div className="bg-gray-900 p-4 rounded-lg border border-gray-800">
            <h2 className="text-sm text-gray-400 font-mono mb-3">⚙️ OPÇÕES</h2>

            <div className="flex items-center gap-3 mb-4">
              <input type="checkbox" id="compactar" checked={compactar} onChange={(e) => setCompactar(e.target.checked)}
                className="w-4 h-4" />
              <label htmlFor="compactar" className="text-sm text-gray-300">Compactar na origem (7-Zip)</label>
            </div>

            <div>
              <label className="block text-xs text-gray-400 mb-2">Retenção de backup (dias para excluir arquivos antigos)</label>
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

          {/* Erro */}
          {error && <p className="text-red-400 text-sm">{error}</p>}

          {/* Botões */}
          <div className="flex gap-3 justify-end">
            <button type="button" onClick={() => navigate('/')}
              className="px-4 py-2 text-sm bg-gray-700 hover:bg-gray-600 rounded">Cancelar</button>
            <button type="submit" disabled={saving}
              className="px-6 py-2 text-sm bg-green-600 hover:bg-green-700 rounded font-medium disabled:opacity-50">
              {saving ? 'Salvando...' : 'Criar Transferência'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
