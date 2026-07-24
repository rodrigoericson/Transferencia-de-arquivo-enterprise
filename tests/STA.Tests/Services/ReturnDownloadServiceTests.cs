using Microsoft.Extensions.Logging;
using Moq;
using STA.Core.Data.Entities;
using STA.Core.Data.Repositories;
using STA.Core.Models;
using STA.Core.Services;
using STA.Core.Services.Transports;
using Xunit;

namespace STA.Tests.Services;

public class ReturnDownloadServiceTests
{
    private readonly Mock<IFileMaskMatcher> _maskMock = new();
    private readonly Mock<IFileLockChecker> _lockMock = new();
    private readonly Mock<ILogSftpRepository> _logSftpMock = new();
    private readonly Mock<ILogArquivoRepository> _logArqMock = new();
    private readonly Mock<ISftpClientWrapper> _clientMock = new();
    private readonly Mock<ISftpClientFactory> _factoryMock = new();
    private readonly Mock<ICredencialProtector> _protectorMock = new();
    private readonly ReturnDownloadService _service;

    public ReturnDownloadServiceTests()
    {
        _service = new ReturnDownloadService(
            _maskMock.Object,
            _lockMock.Object,
            _logSftpMock.Object,
            _logArqMock.Object,
            Mock.Of<ILogger<ReturnDownloadService>>());
    }

    private TransferPath MakeConfig(bool habilitado = true) => new(
        Etapa: "Test",
        DiretorioPrincipal: "C:\\Origem",
        DiretorioBackup: "",
        DiasExcluir: 0,
        MascaraArq: "*",
        CompactaOrigemTipo: "",
        DescompactaDestino: "",
        TamanhoInicialArqBytes: 0,
        TamanhoFinalArqBytes: 0,
        CnEtapa: 1,
        CnRota: 1,
        FlExcluirOrigem: true,
        FlHabilitarRetorno: habilitado,
        CnConexaoSftpRetorno: 1,
        DsDiretorioRetorno: "/retorno",
        DsMascaraRetorno: "*.RET",
        DsDiretorioLocalRetorno: Path.Combine(Path.GetTempPath(), $"sta-test-{Guid.NewGuid():N}"));

    private ConexaoSftp MakeConexao() => new()
    {
        CnConexaoSftp = 1,
        NmConexao = "Test",
        DsHost = "localhost",
        NrPorta = 22,
        DsUsuario = "user",
        DsSenhaCriptografada = new byte[] { 1, 2, 3 },
        DsHorariosExecucao = "08:00",
        FlAtivo = true
    };

    private SftpConnectionPool MakePool()
    {
        _factoryMock.Setup(f => f.Criar(It.IsAny<ConexaoSftp>(), It.IsAny<ICredencialProtector>()))
            .Returns(_clientMock.Object);
        _clientMock.Setup(c => c.IsConnected).Returns(true);
        return new SftpConnectionPool(
            _factoryMock.Object,
            _protectorMock.Object,
            Mock.Of<ILogger<SftpConnectionPool>>());
    }

    [Fact]
    public async Task ProcessarRetornoAsync_RetornoDesabilitado_RetornaZero()
    {
        var config = MakeConfig(habilitado: false);
        var pool = MakePool();

        var result = await _service.ProcessarRetornoAsync(config, MakeConexao(), pool, null, false, CancellationToken.None);

        Assert.Equal(0, result.FilesProcessed);
        Assert.Equal(0, result.FilesSucceeded);
    }

    [Fact]
    public async Task ProcessarRetornoAsync_FiltraPorMascara_BaixaSoOsQuePassam()
    {
        var config = MakeConfig();
        Directory.CreateDirectory(config.DsDiretorioLocalRetorno!);
        var pool = MakePool();

        _clientMock.Setup(c => c.ListDirectoryDetailed("/retorno")).Returns(new[]
        {
            new SftpRemoteEntry("RETORNO_001.RET", "/retorno/RETORNO_001.RET", false, 100, DateTime.UtcNow),
            new SftpRemoteEntry("IGNORAR.TXT", "/retorno/IGNORAR.TXT", false, 50, DateTime.UtcNow),
            new SftpRemoteEntry("subpasta", "/retorno/subpasta", true, 0, DateTime.UtcNow)
        });

        _maskMock.Setup(m => m.Match("RETORNO_001.RET", "*.RET")).Returns(true);
        _maskMock.Setup(m => m.Match("IGNORAR.TXT", "*.RET")).Returns(false);
        _lockMock.Setup(l => l.IsFileLocked(It.IsAny<string>())).Returns(false);

        var result = await _service.ProcessarRetornoAsync(config, MakeConexao(), pool, null, false, CancellationToken.None);

        Assert.Equal(1, result.FilesSucceeded);
        Assert.Equal(0, result.FilesFailed);
        _clientMock.Verify(c => c.DownloadFile("/retorno/RETORNO_001.RET", It.IsAny<Stream>()), Times.Once);
        _clientMock.Verify(c => c.DeleteFile("/retorno/RETORNO_001.RET"), Times.Once);

        Directory.Delete(config.DsDiretorioLocalRetorno!, true);
    }

    [Fact]
    public async Task ProcessarRetornoAsync_ArquivoLocalEmUso_SkipComWarning()
    {
        var config = MakeConfig();
        Directory.CreateDirectory(config.DsDiretorioLocalRetorno!);
        var localPath = Path.Combine(config.DsDiretorioLocalRetorno!, "LOCKED.RET");
        File.WriteAllText(localPath, "existing");
        var pool = MakePool();

        _clientMock.Setup(c => c.ListDirectoryDetailed("/retorno")).Returns(new[]
        {
            new SftpRemoteEntry("LOCKED.RET", "/retorno/LOCKED.RET", false, 100, DateTime.UtcNow)
        });
        _maskMock.Setup(m => m.Match("LOCKED.RET", "*.RET")).Returns(true);
        _lockMock.Setup(l => l.IsFileLocked(localPath)).Returns(true);

        var result = await _service.ProcessarRetornoAsync(config, MakeConexao(), pool, null, false, CancellationToken.None);

        Assert.Equal(0, result.FilesSucceeded);
        Assert.Equal(0, result.FilesFailed);
        Assert.Equal(0, result.FilesProcessed);
        _clientMock.Verify(c => c.DownloadFile(It.IsAny<string>(), It.IsAny<Stream>()), Times.Never);

        Directory.Delete(config.DsDiretorioLocalRetorno!, true);
    }

    [Fact]
    public async Task ProcessarRetornoAsync_FalhaAoApagarRemoto_NaoFalhaDownload()
    {
        var config = MakeConfig();
        Directory.CreateDirectory(config.DsDiretorioLocalRetorno!);
        var pool = MakePool();

        _clientMock.Setup(c => c.ListDirectoryDetailed("/retorno")).Returns(new[]
        {
            new SftpRemoteEntry("FILE.RET", "/retorno/FILE.RET", false, 50, DateTime.UtcNow)
        });
        _maskMock.Setup(m => m.Match("FILE.RET", "*.RET")).Returns(true);
        _lockMock.Setup(l => l.IsFileLocked(It.IsAny<string>())).Returns(false);
        _clientMock.Setup(c => c.DeleteFile("/retorno/FILE.RET")).Throws(new Exception("permission denied"));

        var result = await _service.ProcessarRetornoAsync(config, MakeConexao(), pool, null, false, CancellationToken.None);

        Assert.Equal(1, result.FilesSucceeded);
        Assert.Equal(0, result.FilesFailed);

        Directory.Delete(config.DsDiretorioLocalRetorno!, true);
    }
}
