import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../lib/api';
import type { ApiResponse, PaginatedResponse, ConexaoSftp } from '../types';
import Header from '../components/layout/Header';

const DIAS = ['seg', 'ter', 'qua', 'qui', 'sex', 'sab', 'dom'];

export default function ConexoesSftpPage() {
  const [conexoes, setConexoes] = useState<ConexaoSftp[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<ConexaoSftp | null>(null);
  const navigate = useNavigate();

  useEffect(() => { fetchConexoes(); }, []);

  const fetchConexoes = async () => {
    setLoading(true);
    try {
      const { data } = await api.get<ApiResponse<PaginatedResponse<ConexaoSftp>>>('/conexoes-sftp?pageSize=50');
      if (data.success && data.data) setConexoes(data.data.items);
    } catch { /* interceptor */ }
    setLoading(false);
  };

  const handleDelete = async (id: number, nome: string) => {
    if (!confirm(`Remover conexão "${nome}"?`)) return;
    try {
      await api.delete(`/conexoes-sftp/${id}`);
      fetchConexoes();
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message || 'Erro ao remover.';
      alert(msg);
    }
  };

  const handleToggleAtivo = async (c: ConexaoSftp) => {
    try {
      await api.put(`/conexoes-sftp/${c.cnConexaoSftp}`, {
        nmConexao: c.nmConexao,
        dsHost: c.dsHost,
        nrPorta: c.nrPorta,
        dsUsuario: c.dsUsuario,
        dsHorariosExecucao: c.dsHorariosExecucao,
        dsDiasSemana: c.dsDiasSemana,
        flArquivoObrigatorio: c.flArquivoObrigatorio,
        nrToleranciaMinutos: c.nrToleranciaMinutos,
        flAtivo: !c.flAtivo,
      });
      fetchConexoes();
    } catch { alert('Erro ao alterar status.'); }
  };

  const handleTestar = async (c: ConexaoSftp) => {
    try {
      const { data } = await api.post<ApiResponse<{ sucesso: boolean; mensagem: string }>>(`/conexoes-sftp/${c.cnConexaoSftp}/testar`);
      if (data.data?.sucesso) alert(`Conexão OK: ${data.data.mensagem}`);
      else alert(`Falha: ${data.data?.mensagem}`);
    } catch { alert('Erro ao testar conexão.'); }
  };

  const handleSave = async (form: ConexaoSftpForm) => {
    try {
      if (editing) {
        await api.put(`/conexoes-sftp/${editing.cnConexaoSftp}`, form);
      } else {
        await api.post('/conexoes-sftp', form);
      }
      setShowForm(false);
      setEditing(null);
      fetchConexoes();
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message || 'Erro ao salvar.';
      alert(msg);
    }
  };

  return (
    <div className="min-h-screen bg-gray-950 text-gray-100">
      <Header />
      <div className="max-w-5xl mx-auto p-8">
        <div className="flex justify-between items-center mb-6">
          <div>
            <h1 className="text-2xl font-mono text-green-400">Conexões SFTP</h1>
            <p className="text-sm text-gray-500 mt-1">Gerenciar conexões SFTP para transferência externa</p>
          </div>
          <div className="flex gap-3">
            <button onClick={() => navigate('/')} className="px-3 py-1.5 text-sm bg-gray-800 hover:bg-gray-700 rounded">Voltar</button>
            <button onClick={() => { setEditing(null); setShowForm(true); }} className="px-3 py-1.5 text-sm bg-green-600 hover:bg-green-700 rounded">Nova Conexão</button>
          </div>
        </div>

        {loading ? <p className="text-gray-500 py-8 text-center">Carregando...</p> : conexoes.length === 0 ? (
          <div className="text-center py-12 text-gray-500">
            <p>Nenhuma conexão SFTP cadastrada.</p>
            <p className="text-xs mt-2">Crie uma nova conexão para enviar arquivos via SFTP.</p>
          </div>
        ) : (
          <div className="space-y-3">
            {conexoes.map((c) => (
              <div key={c.cnConexaoSftp} className="bg-gray-900 p-4 rounded-lg border border-gray-800">
                <div className="flex justify-between items-start">
                  <div>
                    <div className="flex items-center gap-3">
                      <h3 className="font-medium text-gray-100">{c.nmConexao}</h3>
                      <span className={`px-2 py-0.5 rounded text-xs ${c.flAtivo ? 'bg-green-900 text-green-300' : 'bg-red-900 text-red-300'}`}>
                        {c.flAtivo ? 'Ativo' : 'Inativo'}
                      </span>
                    </div>
                    <p className="text-sm text-gray-400 font-mono mt-1">{c.dsHost}:{c.nrPorta} • {c.dsUsuario}</p>
                    <div className="flex gap-4 mt-2 text-xs text-gray-500">
                      <span>⏰ {c.dsHorariosExecucao}</span>
                      <span>📅 {c.dsDiasSemana}</span>
                      {c.flArquivoObrigatorio && <span className="text-yellow-400">⚠ Arquivo obrigatório</span>}
                    </div>
                    {c.flPossuiSenha && <p className="text-xs text-gray-600 mt-1">🔒 Senha configurada</p>}
                    {c.dsCaminhoChavePrivada && <p className="text-xs text-gray-600 mt-1">🔑 Chave: {c.dsCaminhoChavePrivada}</p>}
                  </div>
                  <div className="flex gap-2 flex-wrap">
                    <button onClick={() => handleTestar(c)} className="px-2 py-1 text-xs bg-blue-900 text-blue-300 hover:bg-blue-800 rounded">↻ Testar</button>
                    <button onClick={() => { setEditing(c); setShowForm(true); }} className="px-2 py-1 text-xs bg-gray-700 text-gray-300 hover:bg-gray-600 rounded">Editar</button>
                    <button onClick={() => handleDelete(c.cnConexaoSftp, c.nmConexao)} className="px-2 py-1 text-xs bg-red-900 text-red-300 hover:bg-red-800 rounded">Excluir</button>
                    <button onClick={() => handleToggleAtivo(c)}
                      className={`px-2 py-1 text-xs rounded ${c.flAtivo ? 'bg-yellow-900 text-yellow-300 hover:bg-yellow-800' : 'bg-green-900 text-green-300 hover:bg-green-800'}`}>
                      {c.flAtivo ? 'Desativar' : 'Ativar'}
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}

        {showForm && (
          <ConexaoSftpFormModal
            initial={editing}
            onSave={handleSave}
            onCancel={() => { setShowForm(false); setEditing(null); }}
          />
        )}
      </div>
    </div>
  );
}

interface ConexaoSftpForm {
  nmConexao: string;
  dsHost: string;
  nrPorta: number;
  dsUsuario: string;
  dsSenhaPlaintext: string;
  dsCaminhoChavePrivada: string;
  dsHorariosExecucao: string;
  dsDiasSemana: string;
  flArquivoObrigatorio: boolean;
  nrToleranciaMinutos: number;
  flAtivo: boolean;
}

function ConexaoSftpFormModal({ initial, onSave, onCancel }: {
  initial: ConexaoSftp | null;
  onSave: (form: ConexaoSftpForm) => void;
  onCancel: () => void;
}) {
  const [form, setForm] = useState<ConexaoSftpForm>({
    nmConexao: initial?.nmConexao ?? '',
    dsHost: initial?.dsHost ?? '',
    nrPorta: initial?.nrPorta ?? 22,
    dsUsuario: initial?.dsUsuario ?? '',
    dsSenhaPlaintext: '',
    dsCaminhoChavePrivada: initial?.dsCaminhoChavePrivada ?? '',
    dsHorariosExecucao: initial?.dsHorariosExecucao ?? '08:00',
    dsDiasSemana: initial?.dsDiasSemana ?? 'seg,ter,qua,qui,sex',
    flArquivoObrigatorio: initial?.flArquivoObrigatorio ?? false,
    nrToleranciaMinutos: initial?.nrToleranciaMinutos ?? 10,
    flAtivo: initial?.flAtivo ?? true,
  });

  const [diasSelecionados, setDiasSelecionados] = useState<Set<string>>(
    new Set(form.dsDiasSemana.split(',').map(d => d.trim()).filter(Boolean))
  );

  const toggleDia = (dia: string) => {
    const novo = new Set(diasSelecionados);
    if (novo.has(dia)) novo.delete(dia);
    else novo.add(dia);
    setDiasSelecionados(novo);
    setForm({ ...form, dsDiasSemana: Array.from(novo).join(',') });
  };

  const set = (key: keyof ConexaoSftpForm, value: string | number | boolean) => setForm({ ...form, [key]: value });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.nmConexao.trim()) { alert('Nome é obrigatório.'); return; }
    if (!form.dsHost.trim()) { alert('Host é obrigatório.'); return; }
    if (!form.dsUsuario.trim()) { alert('Usuário é obrigatório.'); return; }
    if (!form.dsSenhaPlaintext && !form.dsCaminhoChavePrivada && !initial?.flPossuiSenha) {
      alert('Informe a senha ou o caminho da chave privada.'); return;
    }
    onSave(form);
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 overflow-auto py-8">
      <form onSubmit={handleSubmit} className="bg-gray-900 p-6 rounded-lg border border-gray-700 w-full max-w-lg space-y-4">
        <h2 className="text-lg text-green-400 font-mono">{initial ? 'Editar Conexão SFTP' : 'Nova Conexão SFTP'}</h2>

        <div>
          <label className="block text-xs text-gray-400 mb-1">Nome da conexão</label>
          <input value={form.nmConexao} onChange={(e) => set('nmConexao', e.target.value)}
            placeholder="SFTP Santander"
            className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm focus:outline-none focus:border-green-500" />
        </div>

        <div className="grid grid-cols-3 gap-3">
          <div className="col-span-2">
            <label className="block text-xs text-gray-400 mb-1">Host</label>
            <input value={form.dsHost} onChange={(e) => set('dsHost', e.target.value)}
              placeholder="sftp.empresa.com"
              className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm font-mono focus:outline-none focus:border-green-500" />
          </div>
          <div>
            <label className="block text-xs text-gray-400 mb-1">Porta</label>
            <input type="number" value={form.nrPorta} onChange={(e) => set('nrPorta', Number(e.target.value))}
              className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm focus:outline-none focus:border-green-500" />
          </div>
        </div>

        <div>
          <label className="block text-xs text-gray-400 mb-1">Usuário</label>
          <input value={form.dsUsuario} onChange={(e) => set('dsUsuario', e.target.value)}
            placeholder="usuario_sftp"
            className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm focus:outline-none focus:border-green-500" />
        </div>

        <div>
          <label className="block text-xs text-gray-400 mb-1">Senha {initial?.flPossuiSenha && '<span class="text-gray-600">(deixe vazio para manter)</span>'}</label>
          <input type="password" value={form.dsSenhaPlaintext} onChange={(e) => set('dsSenhaPlaintext', e.target.value)}
            placeholder={initial?.flPossuiSenha ? '••••••••' : 'Senha SFTP'}
            className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm focus:outline-none focus:border-green-500" />
        </div>

        <div>
          <label className="block text-xs text-gray-400 mb-1">Caminho chave privada <span className="text-gray-600">(alternativo à senha)</span></label>
          <input value={form.dsCaminhoChavePrivada} onChange={(e) => set('dsCaminhoChavePrivada', e.target.value)}
            placeholder="C:\\Keys\\id_rsa"
            className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm font-mono focus:outline-none focus:border-green-500" />
        </div>

        <div>
          <label className="block text-xs text-gray-400 mb-1">Horários de execução <span className="text-gray-600">(separados por vírgula)</span></label>
          <input value={form.dsHorariosExecucao} onChange={(e) => set('dsHorariosExecucao', e.target.value)}
            placeholder="04:00, 10:00, 15:00, 20:00"
            className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm font-mono focus:outline-none focus:border-green-500" />
        </div>

        <div>
          <label className="block text-xs text-gray-400 mb-2">Dias da semana</label>
          <div className="flex gap-2 flex-wrap">
            {DIAS.map(dia => (
              <button key={dia} type="button" onClick={() => toggleDia(dia)}
                className={`px-2.5 py-1 rounded text-xs font-medium transition-colors ${
                  diasSelecionados.has(dia)
                    ? 'bg-green-700 text-green-100'
                    : 'bg-gray-800 text-gray-500 hover:bg-gray-700'
                }`}>
                {dia.charAt(0).toUpperCase() + dia.slice(1)}
              </button>
            ))}
          </div>
        </div>

        <div className="grid grid-cols-2 gap-3">
          <div className="flex items-center gap-2">
            <input type="checkbox" id="flObrigatorio" checked={form.flArquivoObrigatorio}
              onChange={(e) => set('flArquivoObrigatorio', e.target.checked)} className="w-4 h-4" />
            <label htmlFor="flObrigatorio" className="text-xs text-gray-400">Arquivo obrigatório</label>
          </div>
          <div>
            <label className="block text-xs text-gray-400 mb-1">Tolerância (min)</label>
            <input type="number" value={form.nrToleranciaMinutos} onChange={(e) => set('nrToleranciaMinutos', Number(e.target.value))}
              min={1} max={60}
              className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded text-gray-100 text-sm focus:outline-none focus:border-green-500" />
          </div>
        </div>

        {initial && (
          <div className="flex items-center gap-2">
            <input type="checkbox" id="flAtivo" checked={form.flAtivo}
              onChange={(e) => set('flAtivo', e.target.checked)} className="w-4 h-4" />
            <label htmlFor="flAtivo" className="text-sm text-gray-400">Ativo</label>
          </div>
        )}

        <p className="text-xs text-gray-600">
          💡 "Arquivo obrigatório": se nenhum arquivo for transferido em TODOS os horários do dia, gera ERRO no último horário.
        </p>

        <div className="flex gap-3 justify-end pt-2">
          <button type="button" onClick={onCancel} className="px-4 py-2 text-sm bg-gray-700 hover:bg-gray-600 rounded">Cancelar</button>
          <button type="submit" className="px-4 py-2 text-sm bg-green-600 hover:bg-green-700 rounded font-medium">Salvar</button>
        </div>
      </form>
    </div>
  );
}
