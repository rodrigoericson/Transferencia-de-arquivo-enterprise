namespace STA.Core.Data.Entities;

public class Auditoria
{
    public int CnAuditoria { get; set; }

    public int? CnUsuario { get; set; }

    public required string NmUsuario { get; set; }

    public required string IdEntidade { get; set; }

    public int IdReferencia { get; set; }

    public required string IdAcao { get; set; }

    public DateTime DtAcao { get; set; } = DateTime.UtcNow;

    public string? DsDetalhe { get; set; }
}
