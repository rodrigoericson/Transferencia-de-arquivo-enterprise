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
}

export interface Destino {
  cnRotaDestino: number;
  cnRota: number;
  nrOrdem: number;
  dsDiretorioDestino: string;
  dsDescompactaDestino: string | null;
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
