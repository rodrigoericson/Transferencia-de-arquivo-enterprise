import { useEffect, useState } from 'react';
import api from '../../lib/api';
import type { ApiResponse, BrowseSftpResult, ConexaoSftp, SftpRemoteEntry } from '../../types';
import { formatBytes, getParentPath } from '../../lib/sftpPath';

interface SftpBrowserModalProps {
  conexao: ConexaoSftp;
  onCancel: () => void;
}

export default function SftpBrowserModal({ conexao, onCancel }: SftpBrowserModalProps) {
  const [path, setPath] = useState('/');
  const [inputPath, setInputPath] = useState('/');
  const [entries, setEntries] = useState<SftpRemoteEntry[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const browse = async (targetPath: string) => {
    setLoading(true);
    setError(null);
    try {
      const { data } = await api.get<ApiResponse<BrowseSftpResult>>(
        `/conexoes-sftp/${conexao.cnConexaoSftp}/browse?path=${encodeURIComponent(targetPath)}`
      );

      if (data.success && data.data) {
        setPath(data.data.currentPath);
        setInputPath(data.data.currentPath);
        setEntries(data.data.entries);
      } else {
        setEntries([]);
        setError(data.message || 'Não foi possível listar o diretório remoto.');
      }
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message || 'Erro ao listar diretório remoto.';
      setEntries([]);
      setError(msg);
    }
    setLoading(false);
  };

  useEffect(() => { browse('/'); }, [conexao.cnConexaoSftp]);

  const openEntry = (entry: SftpRemoteEntry) => {
    if (!entry.isDirectory) return;
    browse(entry.fullPath);
  };

  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50 overflow-auto py-8">
      <div className="bg-gray-900 border border-gray-700 rounded-lg w-full max-w-4xl max-h-[85vh] flex flex-col">
        <div className="p-5 border-b border-gray-800 flex justify-between items-start gap-4">
          <div>
            <h2 className="text-lg text-green-400 font-mono">Explorar SFTP</h2>
            <p className="text-sm text-gray-500 mt-1">{conexao.nmConexao} • {conexao.dsHost}:{conexao.nrPorta}</p>
          </div>
          <button type="button" onClick={onCancel} className="px-3 py-1.5 text-sm bg-gray-800 hover:bg-gray-700 rounded">Fechar</button>
        </div>

        <div className="p-5 border-b border-gray-800 space-y-3">
          <label htmlFor="input-caminho-remoto" className="block text-xs text-gray-400">Caminho remoto</label>
          <div className="flex gap-2">
            <input id="input-caminho-remoto" value={inputPath} onChange={(e) => setInputPath(e.target.value)}
              className="flex-1 px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm font-mono focus:outline-none focus:border-green-500" />
            <button type="button" onClick={() => browse(inputPath)} disabled={loading}
              className="px-3 py-2 text-sm bg-green-600 hover:bg-green-700 disabled:opacity-60 rounded">Ir</button>
            <button type="button" onClick={() => browse(getParentPath(path))} disabled={loading || path === '/'}
              className="px-3 py-2 text-sm bg-gray-700 hover:bg-gray-600 disabled:opacity-40 rounded">Subir</button>
            <button type="button" onClick={() => browse(path)} disabled={loading}
              className="px-3 py-2 text-sm bg-blue-900 text-blue-300 hover:bg-blue-800 disabled:opacity-60 rounded">Atualizar</button>
          </div>
          <p className="text-xs text-gray-500 font-mono">Atual: {path}</p>
          {error && <p className="text-sm text-red-400 bg-red-950/40 border border-red-900 rounded px-3 py-2">{error}</p>}
        </div>

        <div className="overflow-auto flex-1">
          {loading ? (
            <p className="text-gray-500 py-8 text-center">Carregando diretório remoto...</p>
          ) : entries.length === 0 ? (
            <p className="text-gray-500 py-8 text-center">Nenhum arquivo ou pasta encontrado.</p>
          ) : (
            <table className="w-full text-sm">
              <thead className="bg-gray-950 text-gray-500 sticky top-0">
                <tr>
                  <th className="text-left px-4 py-3 font-medium">Nome</th>
                  <th className="text-left px-4 py-3 font-medium">Tipo</th>
                  <th className="text-right px-4 py-3 font-medium">Tamanho</th>
                  <th className="text-left px-4 py-3 font-medium">Modificado</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-800">
                {entries.map((entry) => (
                  <tr key={entry.fullPath} onClick={() => openEntry(entry)}
                    className={`${entry.isDirectory ? 'cursor-pointer hover:bg-gray-800/80' : 'hover:bg-gray-800/40'}`}>
                    <td className="px-4 py-3 font-mono text-gray-100">
                      <span className="mr-2">{entry.isDirectory ? '📁' : '📄'}</span>{entry.name}
                    </td>
                    <td className="px-4 py-3">
                      <span className={`px-2 py-0.5 rounded text-xs ${entry.isDirectory ? 'bg-green-900 text-green-300' : 'bg-gray-800 text-gray-400'}`}>
                        {entry.isDirectory ? 'Pasta' : 'Arquivo'}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-right text-gray-400 font-mono">{entry.isDirectory ? '-' : formatBytes(entry.sizeBytes)}</td>
                    <td className="px-4 py-3 text-gray-400 font-mono">{new Date(entry.lastModifiedUtc).toLocaleString('pt-BR')}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  );
}
