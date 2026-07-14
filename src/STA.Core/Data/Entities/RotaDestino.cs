namespace STA.Core.Data.Entities;

public class RotaDestino
{
    public int CnRotaDestino { get; set; }

    public int CnRota { get; set; }

    public int NrOrdem { get; set; }

    public required string DsDiretorioDestino { get; set; }

    public string? DsDescompactaDestino { get; set; }

    public bool FlAtivo { get; set; } = true;

    // Navigation property
    public RotaTransferencia? Rota { get; set; }
}
