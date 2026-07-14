namespace STA.Worker.Models;

public record TransferChain(
    string Etapa,
    IReadOnlyList<TransferPath> Nodes);
