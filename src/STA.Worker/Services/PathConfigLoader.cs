using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using STA.Worker.Models;

namespace STA.Worker.Services;

public interface IPathConfigLoader
{
    IReadOnlyList<TransferChain> CarregarCaminhos(string caminhoXml);
}

public class PathConfigLoader : IPathConfigLoader
{
    private readonly ILogger<PathConfigLoader> _logger;

    public PathConfigLoader(ILogger<PathConfigLoader> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<TransferChain> CarregarCaminhos(string caminhoXml)
    {
        if (!File.Exists(caminhoXml))
            throw new FileNotFoundException($"Arquivo paths.xml não encontrado: {caminhoXml}");

        var doc = XDocument.Load(caminhoXml);
        var root = doc.Root;

        if (root is null)
            throw new FormatException("XML paths.xml está vazio ou malformado.");

        var groups = root.Elements()
            .GroupBy(e => e.Name.LocalName)
            .ToList();

        var chains = new List<TransferChain>();

        foreach (var group in groups)
        {
            var nodes = new List<TransferPath>();
            string? etapa = null;

            foreach (var element in group)
            {
                var node = ParseNode(element);
                nodes.Add(node);

                if (etapa is null && !string.IsNullOrWhiteSpace(node.Etapa))
                    etapa = node.Etapa;
            }

            if (nodes.Count == 0)
                continue;

            chains.Add(new TransferChain(
                Etapa: etapa ?? group.Key,
                Nodes: nodes));
        }

        _logger.LogInformation("Carregadas {Count} cadeias de transferência de '{Path}'.", chains.Count, caminhoXml);
        return chains;
    }

    private static TransferPath ParseNode(XElement element)
    {
        return new TransferPath(
            Etapa: GetValue(element, "Etapa"),
            DiretorioPrincipal: GetValue(element, "DiretorioPrincipal"),
            DiretorioBackup: GetValue(element, "DiretorioBackup"),
            DiasExcluir: GetInt(element, "DiasExcluir"),
            MascaraArq: GetValue(element, "MascaraArq"),
            CompactaOrigemTipo: GetValue(element, "CompactaOrigemTipo"),
            DescompactaDestino: GetValue(element, "DescompactaDestino"),
            TamanhoInicialArqBytes: GetLong(element, "TamanhoInicialArqBytes"),
            TamanhoFinalArqBytes: GetLong(element, "TamanhoFinalArqBytes"));
    }

    private static string GetValue(XElement parent, string name)
        => parent.Element(name)?.Value?.Trim() ?? string.Empty;

    private static int GetInt(XElement parent, string name)
    {
        var raw = GetValue(parent, name);
        return int.TryParse(raw, out var val) ? val : 0;
    }

    private static long GetLong(XElement parent, string name)
    {
        var raw = GetValue(parent, name);
        return long.TryParse(raw, out var val) ? val : 0;
    }
}
