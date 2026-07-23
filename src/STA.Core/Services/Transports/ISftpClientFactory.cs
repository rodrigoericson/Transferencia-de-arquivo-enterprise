using STA.Core.Data.Entities;

namespace STA.Core.Services.Transports;

public interface ISftpClientWrapper : IDisposable
{
    bool IsConnected { get; }
    void Connect();
    void Disconnect();
    void UploadFile(Stream input, string path);
    void RenameFile(string oldPath, string newPath);
    void DeleteFile(string path);
    bool Exists(string path);
    bool DirectoryExists(string path);
    IEnumerable<string> ListDirectory(string path);
    IEnumerable<SftpRemoteEntry> ListDirectoryDetailed(string path);
}

public interface ISftpClientFactory
{
    ISftpClientWrapper Criar(ConexaoSftp conexao, ICredencialProtector protector);
}
