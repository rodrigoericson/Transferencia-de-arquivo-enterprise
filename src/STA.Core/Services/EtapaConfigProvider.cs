using STA.Core.Data.Repositories;
using STA.Core.Models;

namespace STA.Core.Services;

public interface IEtapaConfigProvider
{
    Task<IReadOnlyList<TransferChain>> CarregarEtapasAsync(
        int cnSistema, CancellationToken cancellationToken = default);
}

public class EtapaConfigProvider : IEtapaConfigProvider
{
    private readonly IEtapaRepository _etapaRepository;

    public EtapaConfigProvider(IEtapaRepository etapaRepository)
    {
        _etapaRepository = etapaRepository;
    }

    public async Task<IReadOnlyList<TransferChain>> CarregarEtapasAsync(
        int cnSistema, CancellationToken cancellationToken = default)
    {
        var etapas = await _etapaRepository.BuscarEtapasAtivasAsync(cnSistema, cancellationToken);

        var chains = new List<TransferChain>();

        foreach (var etapa in etapas)
        {
            var nodes = new List<TransferPath>();

            foreach (var rota in etapa.Rotas)
            {
                // Cada rota gera um nó de origem
                nodes.Add(new TransferPath(
                    Etapa: etapa.NmEtapa,
                    DiretorioPrincipal: rota.DsDiretorioOrigem,
                    DiretorioBackup: rota.DsDiretorioBackup ?? string.Empty,
                    DiasExcluir: rota.NrDiasExcluir,
                    MascaraArq: rota.DsMascaraArquivo,
                    CompactaOrigemTipo: rota.DsCompactaOrigemTipo ?? string.Empty,
                    DescompactaDestino: string.Empty,
                    TamanhoInicialArqBytes: rota.NrTamanhoInicialBytes,
                    TamanhoFinalArqBytes: rota.NrTamanhoFinalBytes));

                // Cada destino gera um nó destino
                foreach (var destino in rota.Destinos)
                {
                    nodes.Add(new TransferPath(
                        Etapa: etapa.NmEtapa,
                        DiretorioPrincipal: destino.DsDiretorioDestino,
                        DiretorioBackup: string.Empty,
                        DiasExcluir: 0,
                        MascaraArq: rota.DsMascaraArquivo,
                        CompactaOrigemTipo: string.Empty,
                        DescompactaDestino: destino.DsDescompactaDestino ?? string.Empty,
                        TamanhoInicialArqBytes: 0,
                        TamanhoFinalArqBytes: 0));
                }
            }

            if (nodes.Count > 0)
                chains.Add(new TransferChain(etapa.NmEtapa, nodes));
        }

        return chains;
    }
}
