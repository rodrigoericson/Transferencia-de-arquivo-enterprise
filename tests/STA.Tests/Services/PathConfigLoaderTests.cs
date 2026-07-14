using Microsoft.Extensions.Logging;
using Moq;
using STA.Worker.Services;
using Xunit;

namespace STA.Tests.Services;

public class PathConfigLoaderTests
{
    private readonly PathConfigLoader _loader;

    public PathConfigLoaderTests()
    {
        _loader = new PathConfigLoader(Mock.Of<ILogger<PathConfigLoader>>());
    }

    [Fact]
    public void CarregarCaminhos_XmlValido_RetornaCadeias()
    {
        var xml = @"<NewDataSet>
  <EtapaA>
    <DiretorioPrincipal>C:\origem</DiretorioPrincipal>
    <DiretorioBackup>C:\backup</DiretorioBackup>
    <DiasExcluir>30</DiasExcluir>
    <Etapa>Transferencia A</Etapa>
    <MascaraArq>*.txt</MascaraArq>
    <CompactaOrigemTipo />
    <DescompactaDestino />
    <TamanhoInicialArqBytes>0</TamanhoInicialArqBytes>
    <TamanhoFinalArqBytes>0</TamanhoFinalArqBytes>
  </EtapaA>
  <EtapaA>
    <DiretorioPrincipal>C:\destino</DiretorioPrincipal>
    <DiretorioBackup />
    <DiasExcluir>0</DiasExcluir>
    <Etapa />
    <MascaraArq>*.txt</MascaraArq>
    <CompactaOrigemTipo />
    <DescompactaDestino />
    <TamanhoInicialArqBytes />
    <TamanhoFinalArqBytes />
  </EtapaA>
</NewDataSet>";

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, xml);

        try
        {
            var result = _loader.CarregarCaminhos(tempFile);

            Assert.Single(result);
            Assert.Equal("Transferencia A", result[0].Etapa);
            Assert.Equal(2, result[0].Nodes.Count);
            Assert.Equal("C:\\origem", result[0].Nodes[0].DiretorioPrincipal);
            Assert.Equal("C:\\destino", result[0].Nodes[1].DiretorioPrincipal);
            Assert.Equal("C:\\backup", result[0].Nodes[0].DiretorioBackup);
            Assert.Equal(30, result[0].Nodes[0].DiasExcluir);
            Assert.Equal("*.txt", result[0].Nodes[0].MascaraArq);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void CarregarCaminhos_ComCompactacao_ParseiaCorretamente()
    {
        var xml = @"<NewDataSet>
  <EtapaB>
    <DiretorioPrincipal>C:\src</DiretorioPrincipal>
    <DiretorioBackup>C:\bkp</DiretorioBackup>
    <DiasExcluir>365</DiasExcluir>
    <Etapa>Compactada</Etapa>
    <MascaraArq>*.csv</MascaraArq>
    <CompactaOrigemTipo>7z</CompactaOrigemTipo>
    <DescompactaDestino>SIM</DescompactaDestino>
    <TamanhoInicialArqBytes>1</TamanhoInicialArqBytes>
    <TamanhoFinalArqBytes>1073741824</TamanhoFinalArqBytes>
  </EtapaB>
  <EtapaB>
    <DiretorioPrincipal>C:\dst</DiretorioPrincipal>
    <DiretorioBackup />
    <DiasExcluir>0</DiasExcluir>
    <Etapa />
    <MascaraArq />
    <CompactaOrigemTipo />
    <DescompactaDestino />
    <TamanhoInicialArqBytes />
    <TamanhoFinalArqBytes />
  </EtapaB>
</NewDataSet>";

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, xml);

        try
        {
            var result = _loader.CarregarCaminhos(tempFile);

            Assert.Single(result);
            var firstNode = result[0].Nodes[0];
            Assert.Equal("7z", firstNode.CompactaOrigemTipo);
            Assert.Equal("SIM", firstNode.DescompactaDestino);
            Assert.Equal(1L, firstNode.TamanhoInicialArqBytes);
            Assert.Equal(1073741824L, firstNode.TamanhoFinalArqBytes);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void CarregarCaminhos_ArquivoInexistente_LancaException()
    {
        Assert.Throws<FileNotFoundException>(() =>
            _loader.CarregarCaminhos("C:\\inexistente\\paths.xml"));
    }

    [Fact]
    public void CarregarCaminhos_MultiplasCadeias_RetornaTodas()
    {
        var xml = @"<NewDataSet>
  <CadeiaA>
    <DiretorioPrincipal>C:\a1</DiretorioPrincipal>
    <DiretorioBackup />
    <DiasExcluir>0</DiasExcluir>
    <Etapa>A</Etapa>
    <MascaraArq>*</MascaraArq>
    <CompactaOrigemTipo />
    <DescompactaDestino />
    <TamanhoInicialArqBytes />
    <TamanhoFinalArqBytes />
  </CadeiaA>
  <CadeiaA>
    <DiretorioPrincipal>C:\a2</DiretorioPrincipal>
    <DiretorioBackup />
    <DiasExcluir>0</DiasExcluir>
    <Etapa />
    <MascaraArq />
    <CompactaOrigemTipo />
    <DescompactaDestino />
    <TamanhoInicialArqBytes />
    <TamanhoFinalArqBytes />
  </CadeiaA>
  <CadeiaB>
    <DiretorioPrincipal>C:\b1</DiretorioPrincipal>
    <DiretorioBackup />
    <DiasExcluir>0</DiasExcluir>
    <Etapa>B</Etapa>
    <MascaraArq>*</MascaraArq>
    <CompactaOrigemTipo />
    <DescompactaDestino />
    <TamanhoInicialArqBytes />
    <TamanhoFinalArqBytes />
  </CadeiaB>
  <CadeiaB>
    <DiretorioPrincipal>C:\b2</DiretorioPrincipal>
    <DiretorioBackup />
    <DiasExcluir>0</DiasExcluir>
    <Etapa />
    <MascaraArq />
    <CompactaOrigemTipo />
    <DescompactaDestino />
    <TamanhoInicialArqBytes />
    <TamanhoFinalArqBytes />
  </CadeiaB>
</NewDataSet>";

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, xml);

        try
        {
            var result = _loader.CarregarCaminhos(tempFile);

            Assert.Equal(2, result.Count);
            Assert.Equal("A", result[0].Etapa);
            Assert.Equal("B", result[1].Etapa);
            Assert.Equal(2, result[0].Nodes.Count);
            Assert.Equal(2, result[1].Nodes.Count);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
