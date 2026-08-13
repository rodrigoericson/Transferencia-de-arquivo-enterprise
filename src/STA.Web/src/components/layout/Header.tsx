import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';

export default function Header() {
  const navigate = useNavigate();
  const logout = useAuth((s) => s.logout);
  const username = useAuth((s) => s.username);

  return (
    <header className="border-b border-gray-800 px-8 py-4 flex justify-between items-center">
      <button type="button" onClick={() => navigate('/')} className="flex items-center gap-4 hover:opacity-80 transition-opacity">
        <img src="/sta-logo.png" alt="STA" className="h-10" />
      </button>
      <div className="flex items-center gap-4">
        <span className="text-sm text-gray-500">{username}</span>
        <button type="button" onClick={logout} className="text-sm text-red-400 hover:text-red-300">Sair</button>
      </div>
    </header>
  );
}
