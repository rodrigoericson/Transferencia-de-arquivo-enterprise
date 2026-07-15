import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

export default function Login() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const login = useAuth((s) => s.login);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    const success = await login(username, password);
    setLoading(false);

    if (success) {
      navigate('/');
    } else {
      setError('Credenciais inválidas.');
    }
  };

  return (
    <div className="min-h-screen flex" style={{ backgroundColor: '#0a0b12' }}>
      {/* Lado esquerdo — Logo */}
      <div className="hidden md:flex flex-1 items-center justify-center p-12">
        <img src="/sta-logo.png" alt="STA" className="w-full max-w-md" />
      </div>

      {/* Divisor */}
      <div className="hidden md:block w-px bg-gray-800 my-16" />

      {/* Lado direito — Formulário */}
      <div className="flex-1 flex flex-col justify-center p-8 pl-16">
        {/* Logo mobile */}
        <div className="md:hidden mb-8">
          <img src="/sta-logo.png" alt="STA" className="h-28 mx-auto" />
        </div>

        <div className="w-full max-w-sm">
          <h2 className="text-2xl font-semibold text-gray-100 mb-1">Bem-vindo</h2>
          <p className="text-sm text-gray-500 mb-6">Entre com suas credenciais para acessar</p>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-sm text-gray-400 mb-1">Usuário</label>
              <input
                type="text"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                className="w-full px-3 py-2.5 bg-gray-800/80 border border-gray-700 rounded-lg text-gray-100 focus:outline-none focus:border-green-500 focus:ring-1 focus:ring-green-500/30"
                autoFocus
              />
            </div>

            <div>
              <label className="block text-sm text-gray-400 mb-1">Senha</label>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="w-full px-3 py-2.5 bg-gray-800/80 border border-gray-700 rounded-lg text-gray-100 focus:outline-none focus:border-green-500 focus:ring-1 focus:ring-green-500/30"
              />
            </div>

            {error && <p className="text-red-400 text-sm">{error}</p>}

            <button
              type="submit"
              disabled={loading}
              className="w-full py-2.5 bg-green-600 hover:bg-green-500 text-white rounded-lg font-medium disabled:opacity-50 transition-colors mt-2"
            >
              {loading ? 'Entrando...' : 'Entrar'}
            </button>
          </form>

          <p className="text-center text-xs text-gray-700 mt-6">Sistema de Transferência de Arquivos</p>
        </div>
      </div>
    </div>
  );
}
