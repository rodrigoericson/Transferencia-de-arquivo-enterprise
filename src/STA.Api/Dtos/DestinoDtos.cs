using System.ComponentModel.DataAnnotations;

namespace STA.Api.Dtos;

public record DestinoDto(
    int CnRotaDestino,
    int CnRota,
    int NrOrdem,
    string DsDiretorioDestino,
    string? DsDescompactaDestino,
    bool FlAtivo);

public record CreateDestinoDto(
    [Required] int CnRota,
    [Range(1, 9999)] int NrOrdem = 1,
    [Required][StringLength(500)] string DsDiretorioDestino = "",
    [StringLength(10)] string? DsDescompactaDestino = null);

public record UpdateDestinoDto(
    [Range(1, 9999)] int NrOrdem,
    [Required][StringLength(500)] string DsDiretorioDestino,
    [StringLength(10)] string? DsDescompactaDestino = null,
    bool FlAtivo = true);
