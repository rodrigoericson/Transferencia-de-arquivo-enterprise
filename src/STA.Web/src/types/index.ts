export interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  message: string | null;
}

export interface PaginatedResponse<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  pageCount: number;
}

export interface Etapa {
  cnEtapa: number;
  cnSistema: number;
  nmEtapa: string;
  flAtivo: boolean;
  nrOrdemExecucao: number;
  hrInicioJanela: string | null;
  hrFimJanela: string | null;
  nrIntervaloMinutos: number | null;
  dtCriacao: string;
  dtAlteracao: string | null;
  quantidadeRotas: number;
}

export interface Rota {
  cnRota: number;
  cnEtapa: number;
  nrOrdem: number;
  dsDiretorioOrigem: string;
  dsDiretorioBackup: string | null;
  dsMascaraArquivo: string;
  dsCompactaOrigemTipo: string | null;
  nrDiasExcluir: number;
  nrTamanhoInicialBytes: number;
  nrTamanhoFinalBytes: number;
  flExcluirOrigem: boolean;
  flAtivo: boolean;
  quantidadeDestinos: number;
  flHabilitarRetorno: boolean;
  cnConexaoSftpRetorno: number | null;
  dsDiretorioRetorno: string | null;
  dsMascaraRetorno: string;
  dsDiretorioLocalRetorno: string | null;
}

export interface Destino {
  cnRotaDestino: number;
  cnRota: number;
  nrOrdem: number;
  dsDiretorioDestino: string;
  dsDescompactaDestino: string | null;
  dsPadraoRename: string | null;
  idProtocolo: string;
  cnConexaoSftp: number | null;
  flAtivo: boolean;
}

export interface LogProcesso {
  cnLogProcesso: number;
  cnSistema: number;
  cnProcesso: number;
  dtInicio: string;
  dtFimProcesso: string | null;
  idStatusProcesso: string;
  qtRegistrosProcessados: number;
  qtRegistrosErro: number;
  xmlObsProcesso: string | null;
}

export interface LogArquivo {
  cnLogArquivo: number;
  cnLogProcesso: number | null;
  cnEtapa: number | null;
  cnRota: number | null;
  nmArquivo: string;
  dsDiretorioOrigem: string;
  dsDiretorioDestino: string;
  nrTamanhoBytes: number;
  dtInicio: string;
  dtFim: string | null;
  idStatus: string;
  dsMensagem: string | null;
  flCompactado: boolean;
  flDescompactado: boolean;
}

export interface WorkerStatus {
  status: string;
  ultimoCiclo: string | null;
  ultimoCicloStatus: string | null;
  arquivosHoje: number;
  errosHoje: number;
}

export interface LoginResponse {
  token: string;
  expiration: string;
  username: string;
  role: string;
}

export interface Execucao {
  executando: boolean;
  pausado: boolean;
  etapaAtual: string | null;
  cicloIniciadoEm: string | null;
  ultimoCicloFim: string | null;
  proximoCicloEm: string | null;
  duracaoUltimoCicloMs: number | null;
}

export interface ConexaoSftp {
  cnConexaoSftp: number;
  nmConexao: string;
  dsHost: string;
  nrPorta: number;
  dsUsuario: string;
  flPossuiSenha: boolean;
  flPossuiChavePrivada: boolean;
  dsHorariosExecucao: string;
  dsDiasSemana: string;
  flArquivoObrigatorio: boolean;
  nrToleranciaMinutos: number;
  flAtivo: boolean;
  dtCriacao: string;
  dtUltimoUso: string | null;
}

export interface SftpRemoteEntry {
  name: string;
  fullPath: string;
  isDirectory: boolean;
  sizeBytes: number;
  lastModifiedUtc: string;
}

export interface BrowseSftpResult {
  currentPath: string;
  entries: SftpRemoteEntry[];
}

export interface LogSftp {
  cnLogSftp: number;
  cnConexaoSftp: number;
  cnRotaDestino: number | null;
  idTipo: string;
  idStatus: string;
  nmArquivo: string | null;
  nrTamanhoBytes: number | null;
  nrDuracaoMs: number | null;
  dsMensagem: string | null;
  dtEvento: string;
}

export interface Auditoria {
  cnAuditoria: number;
  cnUsuario: number | null;
  nmUsuario: string;
  idEntidade: string;
  idReferencia: number;
  idAcao: string;
  dtAcao: string;
  dsDetalhe: string | null;
}
