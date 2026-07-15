using System.ComponentModel.DataAnnotations;

namespace STA.Api.Dtos;

public record RotaDto(
    int CnRota,
    int CnEtapa,
    int NrOrdem,
    string DsDiretorioOrigem,
    string? DsDiretorioBackup,
    string DsMascaraArquivo,
    string? DsCompactaOrigemTipo,
    int NrDiasExcluir,
    long NrTamanhoInicialBytes,
    long NrTamanhoFinalBytes,
    bool FlExcluirOrigem,
    bool FlAtivo,
    int QuantidadeDestinos);

public record CreateRotaDto(
    [Required] int CnEtapa,
    [Range(1, 9999)] int NrOrdem = 1,
    [Required][StringLength(500)] string DsDiretorioOrigem = "",
    [StringLength(500)] string? DsDiretorioBackup = null,
    [StringLength(100)] string DsMascaraArquivo = "*",
    [StringLength(10)] string? DsCompactaOrigemTipo = null,
    [Range(0, 3650)] int NrDiasExcluir = 0,
    long NrTamanhoInicialBytes = 0,
    long NrTamanhoFinalBytes = 0,
    bool FlExcluirOrigem = true);

public record UpdateRotaDto(
    [Range(1, 9999)] int NrOrdem,
    [Required][StringLength(500)] string DsDiretorioOrigem,
    [StringLength(500)] string? DsDiretorioBackup = null,
    [StringLength(100)] string DsMascaraArquivo = "*",
    [StringLength(10)] string? DsCompactaOrigemTipo = null,
    [Range(0, 3650)] int NrDiasExcluir = 0,
    long NrTamanhoInicialBytes = 0,
    long NrTamanhoFinalBytes = 0,
    bool FlExcluirOrigem = true,
    bool FlAtivo = true);
