namespace STA.Worker.Models;

public record TransferPath(
    string Etapa,
    string DiretorioPrincipal,
    string DiretorioBackup,
    int DiasExcluir,
    string MascaraArq,
    string CompactaOrigemTipo,
    string DescompactaDestino,
    long TamanhoInicialArqBytes,
    long TamanhoFinalArqBytes);
