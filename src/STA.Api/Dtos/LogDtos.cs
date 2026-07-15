namespace STA.Api.Dtos;

public record LogProcessoDto(
    int CnLogProcesso,
    int CnSistema,
    int CnProcesso,
    DateTime DtInicio,
    DateTime? DtFimProcesso,
    string IdStatusProcesso,
    long QtRegistrosProcessados,
    long QtRegistrosErro,
    string? XmlObsProcesso);

public record LogArquivoDto(
    long CnLogArquivo,
    int? CnLogProcesso,
    int? CnEtapa,
    int? CnRota,
    string NmArquivo,
    string DsDiretorioOrigem,
    string DsDiretorioDestino,
    long NrTamanhoBytes,
    DateTime DtInicio,
    DateTime? DtFim,
    string IdStatus,
    string? DsMensagem,
    bool FlCompactado,
    bool FlDescompactado);
