using Renci.SshNet;
using STA.Core.Data.Entities;

namespace STA.Core.Services.Transports;

public class SftpClientWrapper : ISftpClientWrapper
{
    private readonly SftpClient _client;

    public SftpClientWrapper(SftpClient client)
    {
        _client = client;
    }

    public bool IsConnected => _client.IsConnected;
    public void Connect() => _client.Connect();
    public void Disconnect() => _client.Disconnect();

    public void UploadFile(Stream input, string path)
    {
        _client.UploadFile(input, path, true);
    }

    public void RenameFile(string oldPath, string newPath)
    {
        _client.RenameFile(oldPath, newPath);
    }

    public void DeleteFile(string path)
    {
        _client.DeleteFile(path);
    }

    public bool Exists(string path)
    {
        return _client.Exists(path);
    }

    public bool DirectoryExists(string path)
    {
        return _client.Exists(path) && _client.GetAttributes(path).IsDirectory;
    }

    public IEnumerable<string> ListDirectory(string path)
    {
        return _client.ListDirectory(path)
            .Where(f => f.Name != "." && f.Name != "..")
            .Select(f => f.Name);
    }

    public IEnumerable<SftpRemoteEntry> ListDirectoryDetailed(string path)
    {
        return _client.ListDirectory(path)
            .Where(f => f.Name != "." && f.Name != "..")
            .Select(f => new SftpRemoteEntry(
                f.Name,
                f.FullName,
                f.IsDirectory,
                f.Length,
                f.LastWriteTimeUtc));
    }

    public void DownloadFile(string remotePath, Stream output)
    {
        _client.DownloadFile(remotePath, output);
    }

    public void Dispose()
    {
        if (_client.IsConnected)
            _client.Disconnect();
        _client.Dispose();
    }
}

public class SftpClientFactory : ISftpClientFactory
{
    public ISftpClientWrapper Criar(ConexaoSftp conexao, ICredencialProtector protector)
    {
        SftpClient client;

        if (!string.IsNullOrEmpty(conexao.DsCaminhoChavePrivada))
        {
            if (!File.Exists(conexao.DsCaminhoChavePrivada))
                throw new FileNotFoundException(
                    "Arquivo de chave privada nao encontrado. Verifique a configuracao da conexao ou use autenticacao por senha.");

            var keyFile = new PrivateKeyFile(conexao.DsCaminhoChavePrivada);
            client = new SftpClient(conexao.DsHost, conexao.NrPorta, conexao.DsUsuario, keyFile);
        }
        else if (conexao.DsSenhaCriptografada != null)
        {
            var senha = protector.Recuperar(conexao.DsSenhaCriptografada);
            client = new SftpClient(conexao.DsHost, conexao.NrPorta, conexao.DsUsuario, senha);
        }
        else
        {
            throw new InvalidOperationException($"Conexao SFTP '{conexao.NmConexao}' nao possui senha nem chave privada configurada.");
        }

        client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(120);
        return new SftpClientWrapper(client);
    }
}
