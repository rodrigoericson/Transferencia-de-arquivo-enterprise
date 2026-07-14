namespace STA.Core.Data.Entities;

public class RotaTransferencia
{
    public int CnRota { get; set; }

    public int CnEtapa { get; set; }

    public int NrOrdem { get; set; }

    public required string DsDiretorioOrigem { get; set; }

    public string? DsDiretorioBackup { get; set; }

    public string DsMascaraArquivo { get; set; } = "*";

    public string? DsCompactaOrigemTipo { get; set; }

    public int NrDiasExcluir { get; set; }

    public long NrTamanhoInicialBytes { get; set; }

    public long NrTamanhoFinalBytes { get; set; }

    public bool FlExcluirOrigem { get; set; } = true;

    public bool FlAtivo { get; set; } = true;

    // Navigation properties
    public EtapaTransferencia? Etapa { get; set; }
    public ICollection<RotaDestino> Destinos { get; set; } = new List<RotaDestino>();
}
