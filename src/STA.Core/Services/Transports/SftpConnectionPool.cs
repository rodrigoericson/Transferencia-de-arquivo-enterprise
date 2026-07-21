using Microsoft.Extensions.Logging;
using STA.Core.Data.Entities;

namespace STA.Core.Services.Transports;

public class SftpConnectionPool : IDisposable
{
    private readonly Dictionary<int, ISftpClientWrapper> _pool = new();
    private readonly object _poolLock = new();
    private readonly ISftpClientFactory _factory;
    private readonly ICredencialProtector _protector;
    private readonly ILogger<SftpConnectionPool> _logger;

    public SftpConnectionPool(
        ISftpClientFactory factory,
        ICredencialProtector protector,
        ILogger<SftpConnectionPool> logger)
    {
        _factory = factory;
        _protector = protector;
        _logger = logger;
    }

    public ISftpClientWrapper GetOrCreate(ConexaoSftp conexao)
    {
        lock (_poolLock)
        {
            if (_pool.TryGetValue(conexao.CnConexaoSftp, out var existing))
            {
                if (existing.IsConnected)
                    return existing;

                _logger.LogWarning("Conexao SFTP '{Nome}' perdida. Reconectando...", conexao.NmConexao);
                try { existing.Dispose(); } catch { }
                _pool.Remove(conexao.CnConexaoSftp);
            }

            var client = _factory.Criar(conexao, _protector);
            client.Connect();
            _pool[conexao.CnConexaoSftp] = client;

            _logger.LogInformation("Conexao SFTP '{Nome}' ({Host}:{Porta}) aberta.",
                conexao.NmConexao, conexao.DsHost, conexao.NrPorta);

            return client;
        }
    }

    public void CloseAll()
    {
        lock (_poolLock)
        {
            foreach (var (id, client) in _pool)
            {
                try
                {
                    if (client.IsConnected)
                        client.Disconnect();
                    client.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao fechar conexao SFTP (id={Id}).", id);
                }
            }
            _pool.Clear();
            _logger.LogDebug("Pool SFTP: todas as conexoes fechadas.");
        }
    }

    public int ActiveConnections => _pool.Count;

    public void Dispose()
    {
        CloseAll();
    }
}
