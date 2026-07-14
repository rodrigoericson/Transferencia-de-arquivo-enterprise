namespace STA.Core.Data.Entities;

public class LogArquivo
{
    public long CnLogArquivo { get; set; }

    public int? CnLogProcesso { get; set; }

    public int? CnEtapa { get; set; }

    public int? CnRota { get; set; }

    public required string NmArquivo { get; set; }

    public required string DsDiretorioOrigem { get; set; }

    public required string DsDiretorioDestino { get; set; }

    public long NrTamanhoBytes { get; set; }

    public DateTime DtInicio { get; set; }

    public DateTime? DtFim { get; set; }

    public required string IdStatus { get; set; }

    public string? DsMensagem { get; set; }

    public bool FlCompactado { get; set; }

    public bool FlDescompactado { get; set; }

    // Navigation properties
    public LogProcesso? LogProcesso { get; set; }
    public EtapaTransferencia? Etapa { get; set; }
    public RotaTransferencia? Rota { get; set; }
}
