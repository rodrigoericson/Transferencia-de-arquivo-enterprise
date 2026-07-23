using Microsoft.Extensions.Logging;
using Moq;
using STA.Core.Services.Transports;
using Xunit;

namespace STA.Tests.Services.Transports;

public class SftpTransportTests
{
    private readonly Mock<ISftpClientWrapper> _clientMock;
    private readonly SftpTransport _transport;

    public SftpTransportTests()
    {
        _clientMock = new Mock<ISftpClientWrapper>();
        _clientMock.Setup(c => c.IsConnected).Returns(true);
        _transport = new SftpTransport(_clientMock.Object, Mock.Of<ILogger<SftpTransport>>());
    }

    [Fact]
    public async Task UploadFileAsync_FazUploadAtomico_TmpEntaoRename()
    {
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "conteudo");

        _clientMock.Setup(c => c.Exists("/remote/file.txt")).Returns(false);

        await _transport.UploadFileAsync(tempFile, "/remote/file.txt", false);

        _clientMock.Verify(c => c.UploadFile(It.IsAny<Stream>(), "/remote/file.txt.tmp"), Times.Once);
        _clientMock.Verify(c => c.RenameFile("/remote/file.txt.tmp", "/remote/file.txt"), Times.Once);

        File.Delete(tempFile);
    }

    [Fact]
    public async Task UploadFileAsync_Overwrite_UsaBackupSwapSeguro()
    {
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "novo");

        _clientMock.Setup(c => c.Exists("/remote/file.txt")).Returns(true);
        _clientMock.SetupSequence(c => c.Exists("/remote/file.txt.bak"))
            .Returns(false)
            .Returns(true);

        await _transport.UploadFileAsync(tempFile, "/remote/file.txt", true);

        _clientMock.Verify(c => c.RenameFile("/remote/file.txt", "/remote/file.txt.bak"), Times.Once);
        _clientMock.Verify(c => c.RenameFile("/remote/file.txt.tmp", "/remote/file.txt"), Times.Once);
        _clientMock.Verify(c => c.DeleteFile("/remote/file.txt.bak"), Times.Once);

        File.Delete(tempFile);
    }

    [Fact]
    public async Task UploadFileAsync_QuandoFalha_LimpaTmp()
    {
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "x");

        _clientMock.Setup(c => c.UploadFile(It.IsAny<Stream>(), It.IsAny<string>()))
            .Throws(new Exception("conexao perdida"));
        _clientMock.Setup(c => c.Exists("/remote/file.txt.tmp")).Returns(true);

        await Assert.ThrowsAsync<Exception>(() =>
            _transport.UploadFileAsync(tempFile, "/remote/file.txt", false));

        _clientMock.Verify(c => c.DeleteFile("/remote/file.txt.tmp"), Times.Once);

        File.Delete(tempFile);
    }

    [Fact]
    public async Task TestConnectionAsync_Conecta_RetornaTrue()
    {
        _clientMock.Setup(c => c.IsConnected).Returns(true);

        var result = await _transport.TestConnectionAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task TestConnectionAsync_Falha_RetornaFalse()
    {
        _clientMock.Setup(c => c.IsConnected).Returns(false);
        _clientMock.Setup(c => c.Connect()).Throws(new Exception("timeout"));

        var result = await _transport.TestConnectionAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task ListFilesAsync_RetornaListaDoServidor()
    {
        _clientMock.Setup(c => c.ListDirectory("/remote"))
            .Returns(new[] { "file1.txt", "file2.dat" });

        var files = await _transport.ListFilesAsync("/remote");

        Assert.Equal(2, files.Count);
        Assert.Contains("file1.txt", files);
    }

    [Fact]
    public async Task BrowseDirectoryAsync_RetornaEntriesOrdenadasComDiretoriosPrimeiro()
    {
        var modified = DateTime.UtcNow;
        _clientMock.Setup(c => c.ListDirectoryDetailed("/remote"))
            .Returns(new[]
            {
                new SftpRemoteEntry("arquivo.txt", "/remote/arquivo.txt", false, 123, modified),
                new SftpRemoteEntry("entrada", "/remote/entrada", true, 0, modified)
            });

        var entries = await _transport.BrowseDirectoryAsync("/remote");

        Assert.Equal(2, entries.Count);
        Assert.True(entries[0].IsDirectory);
        Assert.Equal("entrada", entries[0].Name);
        Assert.Equal("arquivo.txt", entries[1].Name);
        Assert.Equal(123, entries[1].SizeBytes);
    }

    [Fact]
    public async Task DeleteFileAsync_ChamaDeleteNoServidor()
    {
        _clientMock.Setup(c => c.Exists("/remote/old.txt")).Returns(true);

        await _transport.DeleteFileAsync("/remote/old.txt");

        _clientMock.Verify(c => c.DeleteFile("/remote/old.txt"), Times.Once);
    }
}
