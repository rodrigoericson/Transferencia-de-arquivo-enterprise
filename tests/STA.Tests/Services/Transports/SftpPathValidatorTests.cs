using STA.Core.Services.Transports;
using Xunit;

namespace STA.Tests.Services.Transports;

public class SftpPathValidatorTests
{
    [Fact]
    public void TryNormalize_Vazio_RetornaRaiz()
    {
        var ok = SftpPathValidator.TryNormalize(" ", out var path, out var erro);

        Assert.True(ok);
        Assert.Equal("/", path);
        Assert.Empty(erro);
    }

    [Fact]
    public void TryNormalize_PathValido_RetornaTrimado()
    {
        var ok = SftpPathValidator.TryNormalize(" /entrada ", out var path, out _);

        Assert.True(ok);
        Assert.Equal("/entrada", path);
    }

    [Fact]
    public void TryNormalize_NomeComPontosValidos_Aceita()
    {
        var ok = SftpPathValidator.TryNormalize("/retorno/v1..2", out var path, out _);

        Assert.True(ok);
        Assert.Equal("/retorno/v1..2", path);
    }

    [Fact]
    public void TryNormalize_SegmentoParent_Rejeita()
    {
        var ok = SftpPathValidator.TryNormalize("/retorno/../saida", out _, out var erro);

        Assert.False(ok);
        Assert.Equal("Caminho remoto não pode conter segmento '..'.", erro);
    }

    [Fact]
    public void TryNormalize_BarraInvertida_Rejeita()
    {
        var ok = SftpPathValidator.TryNormalize("\\retorno", out _, out var erro);

        Assert.False(ok);
        Assert.Equal("Caminho remoto deve usar '/' como separador.", erro);
    }
}
