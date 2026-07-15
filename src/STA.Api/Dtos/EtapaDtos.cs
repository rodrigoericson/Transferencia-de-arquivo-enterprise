using System.ComponentModel.DataAnnotations;

namespace STA.Api.Dtos;

public record EtapaDto(
    int CnEtapa,
    int CnSistema,
    string NmEtapa,
    bool FlAtivo,
    int NrOrdemExecucao,
    string? HrInicioJanela,
    string? HrFimJanela,
    int? NrIntervaloMinutos,
    DateTime DtCriacao,
    DateTime? DtAlteracao,
    int QuantidadeRotas);

public record CreateEtapaDto(
    [Required][StringLength(200)] string NmEtapa,
    [Range(1, 9999)] int NrOrdemExecucao = 1,
    string? HrInicioJanela = null,
    string? HrFimJanela = null,
    int? NrIntervaloMinutos = null);

public record UpdateEtapaDto(
    [Required][StringLength(200)] string NmEtapa,
    bool FlAtivo,
    [Range(1, 9999)] int NrOrdemExecucao,
    string? HrInicioJanela = null,
    string? HrFimJanela = null,
    int? NrIntervaloMinutos = null);
