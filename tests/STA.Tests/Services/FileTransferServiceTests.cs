using Microsoft.Extensions.Logging;
using Moq;
using STA.Worker.Models;
using STA.Worker.Services;
using Xunit;

namespace STA.Tests.Services;

public class FileTransferServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _sourceDir;
    private readonly string _destDir;
    private readonly string _backupDir;
    private readonly FileTransferService _service;

    public FileTransferServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "sta_test_" + Guid.NewGuid().ToString("N")[..8]);
        _sourceDir = Path.Combine(_tempDir, "origem");
        _destDir = Path.Combine(_tempDir, "destino");
        _backupDir = Path.Combine(_tempDir, "backup");

        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_destDir);
        Directory.CreateDirectory(_backupDir);

        var compressorMock = new Mock<IFileCompressor>();
        compressorMock.Setup(c => c.IsCompressionTypeSupported(It.IsAny<string>())).Returns(false);
        compressorMock.Setup(c => c.IsFileCompressed(It.IsAny<string>())).Returns(false);

        _service = new FileTransferService(
            new FileMaskMatcher(),
            new FileSizeValidator(),
            new FileLockChecker(),
            compressorMock.Object,
            Mock.Of<ILogger<FileTransferService>>());
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task TransferAsync_ArquivoSimples_CopiaEExclui()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "data.txt"), "conteudo");

        var config = CreateConfig("*.txt");
        var result = await _service.TransferAsync(config, _sourceDir, _destDir, true, 30000, CancellationToken.None);

        Assert.Equal(1, result.FilesProcessed);
        Assert.Equal(1, result.FilesSucceeded);
        Assert.Equal(0, result.FilesFailed);
        Assert.True(File.Exists(Path.Combine(_destDir, "data.txt")));
        Assert.False(File.Exists(Path.Combine(_sourceDir, "data.txt")));
    }

    [Fact]
    public async Task TransferAsync_ComBackup_CopiaParaBackup()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "report.csv"), "1,2,3");

        var config = CreateConfig("*.csv", backupDir: _backupDir);
        var result = await _service.TransferAsync(config, _sourceDir, _destDir, true, 30000, CancellationToken.None);

        Assert.Equal(1, result.FilesSucceeded);
        Assert.True(File.Exists(Path.Combine(_backupDir, "report.csv")));
    }

    [Fact]
    public async Task TransferAsync_MascaraNaoBate_IgnoraArquivo()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "data.log"), "log");

        var config = CreateConfig("*.txt");
        var result = await _service.TransferAsync(config, _sourceDir, _destDir, true, 30000, CancellationToken.None);

        Assert.Equal(1, result.FilesProcessed);
        Assert.Equal(0, result.FilesSucceeded);
        Assert.False(File.Exists(Path.Combine(_destDir, "data.log")));
        Assert.True(File.Exists(Path.Combine(_sourceDir, "data.log")));
    }

    [Fact]
    public async Task TransferAsync_TamanhoForaDaFaixa_IgnoraArquivo()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "small.txt"), "x");

        var config = new TransferPath(
            Etapa: "Test",
            DiretorioPrincipal: _sourceDir,
            DiretorioBackup: "",
            DiasExcluir: 0,
            MascaraArq: "*.txt",
            CompactaOrigemTipo: "",
            DescompactaDestino: "",
            TamanhoInicialArqBytes: 1000,
            TamanhoFinalArqBytes: 2000);

        var result = await _service.TransferAsync(config, _sourceDir, _destDir, true, 30000, CancellationToken.None);

        Assert.Equal(0, result.FilesSucceeded);
        Assert.True(File.Exists(Path.Combine(_sourceDir, "small.txt")));
    }

    [Fact]
    public async Task TransferAsync_MultiplosArquivos_ProcessaTodos()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "a.txt"), "aaa");
        File.WriteAllText(Path.Combine(_sourceDir, "b.txt"), "bbb");
        File.WriteAllText(Path.Combine(_sourceDir, "c.log"), "ccc");

        var config = CreateConfig("*.txt");
        var result = await _service.TransferAsync(config, _sourceDir, _destDir, true, 30000, CancellationToken.None);

        Assert.Equal(3, result.FilesProcessed);
        Assert.Equal(2, result.FilesSucceeded);
        Assert.True(File.Exists(Path.Combine(_destDir, "a.txt")));
        Assert.True(File.Exists(Path.Combine(_destDir, "b.txt")));
        Assert.False(File.Exists(Path.Combine(_destDir, "c.log")));
    }

    [Fact]
    public async Task TransferAsync_DiretorioOrigemInexistente_RetornaVazio()
    {
        var config = CreateConfig("*");
        var result = await _service.TransferAsync(config, @"C:\inexistente_sta_test", _destDir, true, 30000, CancellationToken.None);

        Assert.Equal(0, result.FilesProcessed);
        Assert.Single(result.ErrorMessages);
    }

    [Fact]
    public async Task TransferAsync_PurgeBackupAntigo_ExcluiArquivosVelhos()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "new.txt"), "new");

        var oldFile = Path.Combine(_backupDir, "old.txt");
        File.WriteAllText(oldFile, "old");
        File.SetLastWriteTime(oldFile, DateTime.Now.AddDays(-60));

        var config = new TransferPath(
            Etapa: "Test",
            DiretorioPrincipal: _sourceDir,
            DiretorioBackup: _backupDir,
            DiasExcluir: 30,
            MascaraArq: "*.txt",
            CompactaOrigemTipo: "",
            DescompactaDestino: "",
            TamanhoInicialArqBytes: 0,
            TamanhoFinalArqBytes: 0);

        await _service.TransferAsync(config, _sourceDir, _destDir, true, 30000, CancellationToken.None);

        Assert.False(File.Exists(oldFile));
    }

    private TransferPath CreateConfig(string mask, string backupDir = "")
    {
        return new TransferPath(
            Etapa: "Test",
            DiretorioPrincipal: _sourceDir,
            DiretorioBackup: backupDir,
            DiasExcluir: 0,
            MascaraArq: mask,
            CompactaOrigemTipo: "",
            DescompactaDestino: "",
            TamanhoInicialArqBytes: 0,
            TamanhoFinalArqBytes: 0);
    }
}
