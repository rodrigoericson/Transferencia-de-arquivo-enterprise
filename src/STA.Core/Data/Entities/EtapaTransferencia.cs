namespace STA.Core.Data.Entities;

public class EtapaTransferencia
{
    public int CnEtapa { get; set; }

    public int CnSistema { get; set; }

    public required string NmEtapa { get; set; }

    public bool FlAtivo { get; set; } = true;

    public int NrOrdemExecucao { get; set; }

    public string? HrInicioJanela { get; set; }

    public string? HrFimJanela { get; set; }

    public int? NrIntervaloMinutos { get; set; }

    public DateTime DtCriacao { get; set; }

    public DateTime? DtAlteracao { get; set; }

    // Navigation properties
    public Sistema? Sistema { get; set; }
    public ICollection<RotaTransferencia> Rotas { get; set; } = new List<RotaTransferencia>();
}
