using System.ComponentModel.DataAnnotations;

namespace STA.Api.Dtos;

public record ConexaoSftpDto(
    int CnConexaoSftp,
    string NmConexao,
    string DsHost,
    int NrPorta,
    string DsUsuario,
    bool FlPossuiSenha,
    string? DsCaminhoChavePrivada,
    string DsHorariosExecucao,
    string DsDiasSemana,
    bool FlArquivoObrigatorio,
    int NrToleranciaMinutos,
    bool FlAtivo,
    DateTime DtCriacao,
    DateTime? DtUltimoUso
);

public record CreateConexaoSftpDto(
    [Required][StringLength(100)] string NmConexao,
    [Required][StringLength(255)] string DsHost,
    [Range(1, 65535)] int NrPorta = 22,
    [Required][StringLength(100)] string DsUsuario = "",
    [StringLength(200)] string? DsSenhaPlaintext = null,
    [StringLength(500)] string? DsCaminhoChavePrivada = null,
    [Required][StringLength(200)] string DsHorariosExecucao = "08:00",
    [StringLength(30)] string DsDiasSemana = "seg,ter,qua,qui,sex",
    bool FlArquivoObrigatorio = false,
    [Range(1, 60)] int NrToleranciaMinutos = 10
);

public record UpdateConexaoSftpDto(
    [Required][StringLength(100)] string NmConexao,
    [Required][StringLength(255)] string DsHost,
    [Range(1, 65535)] int NrPorta,
    [Required][StringLength(100)] string DsUsuario,
    [StringLength(200)] string? DsSenhaPlaintext = null,
    [StringLength(500)] string? DsCaminhoChavePrivada = null,
    [Required][StringLength(200)] string DsHorariosExecucao = "08:00",
    [StringLength(30)] string DsDiasSemana = "seg,ter,qua,qui,sex",
    bool FlArquivoObrigatorio = false,
    [Range(1, 60)] int NrToleranciaMinutos = 10,
    bool FlAtivo = true
);

public record TestarConexaoSftpDto(
    [Required][StringLength(255)] string DsHost,
    [Range(1, 65535)] int NrPorta = 22,
    [Required][StringLength(100)] string DsUsuario = "",
    [StringLength(200)] string? DsSenhaPlaintext = null,
    [StringLength(500)] string? DsCaminhoChavePrivada = null
);

public record TestarConexaoResultDto(bool Sucesso, string Mensagem);

public record LogSftpDto(
    int CnLogSftp,
    int CnConexaoSftp,
    int? CnRotaDestino,
    string IdTipo,
    string IdStatus,
    string? NmArquivo,
    long? NrTamanhoBytes,
    int? NrDuracaoMs,
    string? DsMensagem,
    DateTime DtEvento
);
