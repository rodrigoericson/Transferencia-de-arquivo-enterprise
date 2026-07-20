namespace STA.Core.Data.Entities;

public class ExecucaoSftp
{
    public int CnExecucaoSftp { get; set; }

    public int CnConexaoSftp { get; set; }

    public DateOnly DtDia { get; set; }

    public required string HrHorario { get; set; }

    public required string IdResultado { get; set; }

    public DateTime DtExecutadoEm { get; set; } = DateTime.UtcNow;

    // Navigation
    public ConexaoSftp? ConexaoSftp { get; set; }
}
