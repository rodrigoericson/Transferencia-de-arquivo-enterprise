import { useState, useEffect } from 'react';

interface Props {
  value: string;
  onChange: (padrao: string) => void;
}

export default function RenameConfig({ value, onChange }: Props) {
  const [ativo, setAtivo] = useState(!!value);
  const [nome, setNome] = useState('');
  const [incluirData, setIncluirData] = useState(false);
  const [alterarExt, setAlterarExt] = useState(false);
  const [novaExt, setNovaExt] = useState('');

  // Inicializar a partir do valor existente (edição)
  useEffect(() => {
    if (value) {
      setAtivo(true);
      // Tenta parsear padrão existente
      let remaining = value;
      if (remaining.includes('{DATE}') || remaining.includes('{date}')) {
        setIncluirData(true);
        remaining = remaining.replace(/_{DATE}_{TIME}/gi, '').replace(/_{DATE}/gi, '');
      }
      const dotIdx = remaining.lastIndexOf('.');
      if (dotIdx > 0) {
        const ext = remaining.substring(dotIdx + 1);
        if (ext && !ext.includes('{')) {
          setAlterarExt(true);
          setNovaExt(ext);
          remaining = remaining.substring(0, dotIdx);
        }
      }
      if (remaining.includes('{EXT}') || remaining.includes('{ext}')) {
        remaining = remaining.replace('.{EXT}', '').replace('.{ext}', '');
      }
      setNome(remaining);
    }
  }, []);

  // Gerar padrão a partir dos campos
  const buildAndPropagate = (nAtivo: boolean, nNome: string, nData: boolean, nExt: boolean, nNovaExt: string) => {
    if (!nAtivo || !nNome.trim()) {
      onChange('');
      return;
    }
    let padrao = nNome.trim();
    if (nData) padrao += '_{DATE}{TIME}';
    if (nExt && nNovaExt.trim()) {
      padrao += '.' + nNovaExt.trim().replace(/^\./, '');
    } else {
      padrao += '.{EXT}';
    }
    onChange(padrao);
  };

  // Preview
  const gerarPreview = () => {
    if (!ativo || !nome.trim()) return null;
    const now = new Date();
    let preview = nome.trim();
    if (incluirData) {
      preview += '_' + now.toISOString().slice(0, 10).replace(/-/g, '') + now.toTimeString().slice(0, 8).replace(/:/g, '');
    }
    if (alterarExt && novaExt.trim()) {
      preview += '.' + novaExt.trim().replace(/^\./, '');
    } else {
      preview += '.txt';
    }
    return preview;
  };

  if (!ativo) {
    return (
      <div className="flex items-center gap-2 mt-1">
        <input type="checkbox" id="rename-ativo" checked={false} onChange={() => setAtivo(true)} className="w-3.5 h-3.5" />
        <label htmlFor="rename-ativo" className="text-xs text-gray-500 cursor-pointer">Renomear arquivo neste destino</label>
      </div>
    );
  }

  const preview = gerarPreview();

  return (
    <div className="mt-2 p-3 bg-gray-800/50 rounded border border-gray-700/50 space-y-2">
      <div className="flex items-center gap-2">
        <input type="checkbox" id="rename-ativo" checked={true} onChange={() => { setAtivo(false); setNome(''); onChange(''); }} className="w-3.5 h-3.5" />
        <label htmlFor="rename-ativo" className="text-xs text-gray-400 font-medium">Renomear arquivo</label>
      </div>

      <div>
        <label className="block text-xs text-gray-500 mb-1">Novo nome (obrigatório)</label>
        <input
          value={nome}
          onChange={(e) => { setNome(e.target.value); buildAndPropagate(ativo, e.target.value, incluirData, alterarExt, novaExt); }}
          placeholder="COBREM_SANTANDER"
          className="w-full px-2 py-1.5 bg-gray-800 border border-gray-700 rounded text-gray-100 text-xs font-mono focus:outline-none focus:border-green-500"
        />
      </div>

      <div className="flex items-center gap-2">
        <input type="checkbox" id="incluir-data" checked={incluirData} onChange={(e) => { setIncluirData(e.target.checked); buildAndPropagate(ativo, nome, e.target.checked, alterarExt, novaExt); }} className="w-3.5 h-3.5" />
        <label htmlFor="incluir-data" className="text-xs text-gray-400">Incluir data e hora (YYYYMMDDHHMMSS)</label>
      </div>

      <div className="flex items-center gap-2">
        <input type="checkbox" id="alterar-ext" checked={alterarExt} onChange={(e) => { setAlterarExt(e.target.checked); buildAndPropagate(ativo, nome, incluirData, e.target.checked, novaExt); }} className="w-3.5 h-3.5" />
        <label htmlFor="alterar-ext" className="text-xs text-gray-400">Alterar extensão</label>
        {alterarExt && (
          <input
            value={novaExt}
            onChange={(e) => { setNovaExt(e.target.value); buildAndPropagate(ativo, nome, incluirData, alterarExt, e.target.value); }}
            placeholder="dat"
            className="w-20 px-2 py-1 bg-gray-800 border border-gray-700 rounded text-gray-100 text-xs font-mono focus:outline-none focus:border-green-500"
          />
        )}
      </div>

      {preview && (
        <div className="pt-2 border-t border-gray-700/50">
          <p className="text-xs text-gray-500">Preview:</p>
          <p className="text-xs text-green-400 font-mono">{preview}</p>
        </div>
      )}
    </div>
  );
}
