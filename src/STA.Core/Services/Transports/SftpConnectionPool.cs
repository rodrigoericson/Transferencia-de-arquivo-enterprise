using System.Diagnostics;
using Microsoft.Extensions.Logging;
using STA.Core.Data.Entities;
using STA.Core.Data.Repositories;

namespace STA.Core.Services.Transports;

public class SftpConnectionPool : IDisposable
{
    private readonly Dictionary<int, ISftpClientWrapper> _pool = new();
    private readonly object _poolLock = new();
    private readonly List<LogSftp> _pendingLogs = new();
    private readonly ISftpClientFactory _factory;
    private readonly ICredencialProtector _protector;
    private readonly ILogSftpRepository? _logSftpRepository;
    private readonly ILogger<SftpConnectionPool> _logger;

    public SftpConnectionPool(
        ISftpClientFactory factory,
        ICredencialProtector protector,
        ILogger<SftpConnectionPool> logger,
        ILogSftpRepository? logSftpRepository = null)
    {
        _factory = factory;
        _protector = protector;
        _logger = logger;
        _logSftpRepository = logSftpRepository;
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
                EnqueueLog(conexao, "W", "Conexão perdida — reconectando");
                try { existing.Dispose(); } catch { }
                _pool.Remove(conexao.CnConexaoSftp);
            }

            var sw = Stopwatch.StartNew();
            try
            {
                var client = _factory.Criar(conexao, _protector);
                client.Connect();
                sw.Stop();
                _pool[conexao.CnConexaoSftp] = client;

                _logger.LogInformation("Conexao SFTP '{Nome}' ({Host}:{Porta}) aberta em {Ms}ms.",
                    conexao.NmConexao, conexao.DsHost, conexao.NrPorta, sw.ElapsedMilliseconds);
                EnqueueLog(conexao, "S", $"Conectado em {sw.ElapsedMilliseconds}ms — {conexao.DsHost}:{conexao.NrPorta} (usuario: {conexao.DsUsuario})", (int)sw.ElapsedMilliseconds);

                return client;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Falha ao conectar SFTP '{Nome}' ({Host}:{Porta}).",
                    conexao.NmConexao, conexao.DsHost, conexao.NrPorta);
                EnqueueLog(conexao, "E", $"Falha de conexão: {ex.Message} — {conexao.DsHost}:{conexao.NrPorta} (usuario: {conexao.DsUsuario}, tentativa: {sw.ElapsedMilliseconds}ms)", (int)sw.ElapsedMilliseconds);
                throw;
            }
        }
    }

    private void EnqueueLog(ConexaoSftp conexao, string status, string mensagem, int? duracaoMs = null)
    {
        _pendingLogs.Add(new LogSftp
        {
            CnConexaoSftp = conexao.CnConexaoSftp,
            IdTipo = "CONEXAO",
            IdStatus = status,
            NrDuracaoMs = duracaoMs,
            DsMensagem = mensagem,
            DtEvento = DateTime.UtcNow
        });
    }

    public async Task FlushLogsAsync(CancellationToken ct = default)
    {
        if (_logSftpRepository == null || _pendingLogs.Count == 0) return;

        var logs = _pendingLogs.ToList();
        _pendingLogs.Clear();

        foreach (var log in logs)
        {
            try { await _logSftpRepository.InserirAsync(log, ct); }
            catch { }
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
