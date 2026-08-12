import { useEffect, useRef } from 'react';
import api from '../../lib/api';

interface Props {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  validacao?: { status: string; mensagem: string; ok: boolean };
  onValidar?: () => void;
}

function getBorderColor(validacao?: Props['validacao']) {
  if (!validacao || validacao.status === 'idle') return 'border-gray-700';
  if (validacao.status === 'validando') return 'border-blue-500';
  if (validacao.ok) return 'border-green-500';
  if (validacao.status === 'nao_existe') return 'border-yellow-500';
  return 'border-red-500';
}

function getIcon(validacao?: Props['validacao']) {
  if (!validacao || validacao.status === 'idle') return null;
  if (validacao.status === 'validando') return <span className="text-blue-400 text-xs">⟳</span>;
  if (validacao.ok) return <span className="text-green-400 text-sm">✓</span>;
  if (validacao.status === 'nao_existe') return <span className="text-yellow-400 text-sm">!</span>;
  return <span className="text-red-400 text-sm">✗</span>;
}

function getMensagemColor(validacao: Props['validacao']) {
  if (validacao?.ok) return 'text-green-500';
  if (validacao?.status === 'nao_existe') return 'text-yellow-400';
  return 'text-red-400';
}

export default function DiretorioInput({ value, onChange, placeholder, validacao, onValidar }: Props) {
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (!onValidar || !value.trim()) return;
    if (timer.current) clearTimeout(timer.current);
    timer.current = setTimeout(() => onValidar(), 800);
    return () => { if (timer.current) clearTimeout(timer.current); };
  }, [value]);

  const borderColor = getBorderColor(validacao);
  const icon = getIcon(validacao);

  const handleCriar = async () => {
    try {
      const { data } = await api.post('/diretorios/criar', { path: value.trim() });
      if (data.success && data.data?.ok && onValidar) {
        onValidar();
      }
    } catch { /* ignore */ }
  };

  return (
    <div>
      <div className="relative">
        <input
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={placeholder}
          className={`w-full px-3 py-2 pr-8 bg-gray-800 border ${borderColor} rounded text-gray-100 text-sm font-mono focus:outline-none transition-colors`}
        />
        {icon && <span className="absolute right-3 top-1/2 -translate-y-1/2">{icon}</span>}
      </div>
      {validacao && validacao.status !== 'idle' && validacao.status !== 'validando' && (
        <div className="flex items-center gap-2 mt-1">
          <p className={`text-xs ${getMensagemColor(validacao)}`}>
            {validacao.mensagem}
          </p>
          {validacao.status === 'nao_existe' && (
            <button type="button" onClick={handleCriar}
              className="text-xs px-2 py-0.5 bg-yellow-700 hover:bg-yellow-600 text-yellow-100 rounded">
              Criar
            </button>
          )}
        </div>
      )}
    </div>
  );
}
