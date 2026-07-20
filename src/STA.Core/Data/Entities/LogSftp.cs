namespace STA.Core.Data.Entities;

public class LogSftp
{
    public int CnLogSftp { get; set; }

    public int CnConexaoSftp { get; set; }

    public int? CnRotaDestino { get; set; }

    public required string IdTipo { get; set; }

    public required string IdStatus { get; set; }

    public string? NmArquivo { get; set; }

    public long? NrTamanhoBytes { get; set; }

    public int? NrDuracaoMs { get; set; }

    public string? DsMensagem { get; set; }

    public DateTime DtEvento { get; set; } = DateTime.UtcNow;

    // Navigation
    public ConexaoSftp? ConexaoSftp { get; set; }
    public RotaDestino? RotaDestino { get; set; }
}
