# TAE-STA — Documentação Técnica

## Índice

1. [Visão Geral](#visão-geral)
2. [Pré-requisitos](#pré-requisitos)
3. [Instalação e Setup](#instalação-e-setup)
4. [Como Funciona](#como-funciona)
5. [Estrutura do Projeto](#estrutura-do-projeto)
6. [API REST](#api-rest)
7. [Frontend](#frontend)
8. [Banco de Dados](#banco-de-dados)
9. [Configuração](#configuração)
10. [Deploy em Produção](#deploy-em-produção)
11. [Roadmap](#roadmap)
12. [Decisões Técnicas](#decisões-técnicas)

---

## Visão Geral

O TAE-STA (Transferência de Arquivos Enterprise) é um serviço que automatiza a movimentação de arquivos entre servidores Windows. Originalmente escrito em VB.NET + Sybase (2019), foi reescrito do zero em .NET 10 + PostgreSQL mantendo o comportamento operacional mas com arquitetura moderna.

**O que ele faz:**
- Transfere arquivos entre diretórios de rede (UNC paths)
- Suporta múltiplos destinos por arquivo (fan-out)
- Compacta arquivos na origem (7-Zip)
- Descompacta arquivos no destino
- Grava log granular por arquivo transferido
- Respeita janela horária configurável
- Ciclo automático a cada N minutos

**Componentes:**
- **Worker** — serviço Windows que executa as transferências em background
- **API REST** — expõe CRUD de configuração + consulta de logs + controle do Worker
- **Frontend** — SPA React para gerenciar o sistema via browser

---

## Pré-requisitos

| Software | Versão | Para quê |
|----------|--------|----------|
| .NET SDK | 10.0+ | Backend (Worker + API) |
| Docker | Latest | PostgreSQL local |
| Node.js | 24.x+ | Frontend (React) |
| Git | Latest | Versionamento |
| 7-Zip | Latest | Compactação de arquivos (Worker) |

---

## Instalação e Setup

### 1. Clonar o repositório

```bash
git clone https://github.com/rodrigoericson/Sistema-de-transferia-de-arquivo.git
cd Sistema-de-transferia-de-arquivo
```

### 2. Subir o PostgreSQL

```bash
docker compose up -d postgres
```

Isso cria o container `sta-postgres` na porta 5432.

### 3. Configurar credenciais locais

Crie o arquivo `src/STA.Worker/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "StaDb": "Host=localhost;Port=5432;Database=sta;Username=postgres;Password=SUA_SENHA"
  },
  "StaSettings": {
    "ArquivoPathsXml": "C:\\caminho\\paths_test.xml"
  }
}
```

Crie também `src/STA.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "StaDb": "Host=localhost;Port=5432;Database=sta;Username=postgres;Password=SUA_SENHA"
  },
  "Jwt": {
    "Secret": "UmaChaveSecretaComNoMinimo32Caracteres!!"
  }
}
```

> Esses arquivos são gitignored e não vão para o repositório.

### 4. Aplicar migrations (criar tabelas)

```bash
cd src/STA.Worker
dotnet ef database update
```

### 5. Rodar o Worker

```bash
dotnet run --project src/STA.Worker
```

### 6. Rodar a API

```bash
dotnet run --project src/STA.Api
```

API disponível em `http://localhost:5000`. Swagger em `http://localhost:5000/swagger`.

### 7. Rodar o Frontend

```bash
cd src/STA.Web
npm install
npm run dev
```

Frontend em `http://localhost:3000`. Login: `admin` / `admin`.

### 8. Rodar testes

```bash
dotnet test STA.sln
```

---

## Como Funciona

### Ciclo do Worker

```
1. Worker inicia e espera 5s (warm-up)
2. A cada ciclo (padrão 5 min):
   a. Busca parâmetros do banco (hora início/fim, intervalo)
   b. Verifica se está dentro da janela horária
   c. Se sim:
      - Abre log de ciclo (status 'R' = rodando)
      - Carrega etapas do banco (configuração de transferência)
      - Para cada etapa → para cada rota → para cada arquivo:
        • Valida máscara, tamanho, lock
        • Compacta (se configurado)
        • Copia para destino(s)
        • Faz backup
        • Descompacta no destino (se configurado)
        • Exclui original
        • Grava log do arquivo (sucesso ou erro)
      - Fecha log de ciclo (status 'O' ou 'W')
   d. Limpa logs antigos (retenção configurável)
3. Dorme N minutos e repete
```

### Configuração via Banco

A configuração de "o que transferir" vive em 3 tabelas:

- **tbl_etapa_transferencia** — agrupamento lógico (ex: "Assessoria GRB")
- **tbl_rota_transferencia** — regra de transferência (origem, máscara, compactação)
- **tbl_rota_destino** — destino(s) de uma rota (suporta fan-out)

Exemplo: 1 etapa com 1 rota que envia para 3 destinos diferentes.

### Fan-out

Um arquivo pode ser enviado para múltiplos destinos. A configuração é:

```
Etapa: "Assessoria GRB"
└── Rota: origem=\\servidor\pasta, mascara=*.REM
    ├── Destino 1: \\servidor-a\pasta
    ├── Destino 2: \\servidor-b\pasta
    └── Destino 3: \\servidor-c\pasta
```

O Worker transfere o arquivo para cada destino sequencialmente.

---

## Estrutura do Projeto

```
STA.sln
├── src/
│   ├── STA.Core/              # Biblioteca compartilhada
│   │   ├── Data/
│   │   │   ├── Entities/      # Mapeamento EF Core das tabelas
│   │   │   ├── Repositories/  # Interfaces e implementações de acesso a dados
│   │   │   └── StaDbContext.cs
│   │   ├── Models/            # Records imutáveis (TransferPath, TransferChain)
│   │   ├── Services/          # Lógica de negócio (FileTransferService, etc)
│   │   └── Settings/          # StaSettings (Options pattern)
│   │
│   ├── STA.Worker/            # Serviço Windows
│   │   ├── Worker.cs          # BackgroundService (orquestrador)
│   │   ├── Program.cs         # DI + configuração
│   │   └── Data/Migrations/   # EF Core migrations
│   │
│   ├── STA.Api/               # Web API REST
│   │   ├── Controllers/       # Etapas, Rotas, Destinos, Logs, Worker, Auth
│   │   ├── Dtos/              # Request/Response DTOs
│   │   ├── Common/            # ApiResponse<T>
│   │   └── Program.cs         # DI + JWT + Swagger
│   │
│   ├── STA.Web/               # Frontend React
│   │   ├── src/pages/         # Login, Dashboard, Etapas, Logs
│   │   ├── src/hooks/         # useAuth (Zustand)
│   │   ├── src/lib/           # API client (Axios)
│   │   └── src/types/         # TypeScript interfaces
│   │
│   └── STA.Database/          # SQL de referência (functions, seeds)
│
├── tests/
│   └── STA.Tests/             # 120 testes (xUnit + Moq)
│
├── docker-compose.yml         # PostgreSQL dev
├── README.md                  # Overview visual
└── DOCS.md                    # Esta documentação
```

---

## API REST

Base URL: `http://localhost:5000/api/v1`

### Autenticação

Todos os endpoints (exceto login e health) exigem JWT Bearer token.

```bash
# Login
curl -X POST /api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"username": "admin", "password": "admin"}'

# Response: { "data": { "token": "eyJ...", "role": "Admin" } }

# Usar em requests subsequentes:
curl -H 'Authorization: Bearer eyJ...' /api/v1/etapas
```

### Endpoints

| Método | Rota | Descrição |
|--------|------|----------|
| POST | `/auth/login` | Gera token JWT |
| GET | `/etapas` | Lista etapas (paginado, filtro por ativo) |
| GET | `/etapas/{id}` | Detalhe de uma etapa |
| POST | `/etapas` | Criar etapa |
| PUT | `/etapas/{id}` | Atualizar etapa |
| DELETE | `/etapas/{id}` | Remover etapa |
| GET | `/rotas` | Lista rotas (filtro por etapaId) |
| GET | `/rotas/{id}` | Detalhe de uma rota |
| POST | `/rotas` | Criar rota |
| PUT | `/rotas/{id}` | Atualizar rota |
| DELETE | `/rotas/{id}` | Remover rota |
| GET | `/destinos` | Lista destinos (filtro por rotaId) |
| GET | `/destinos/{id}` | Detalhe de um destino |
| POST | `/destinos` | Criar destino |
| PUT | `/destinos/{id}` | Atualizar destino |
| DELETE | `/destinos/{id}` | Remover destino |
| GET | `/logs/processos` | Logs de ciclo (filtro por status, data) |
| GET | `/logs/processos/{id}` | Detalhe de um ciclo |
| GET | `/logs/arquivos` | Logs de arquivo (filtro por status, etapa, nome) |
| GET | `/logs/arquivos/{id}` | Detalhe de um arquivo |
| GET | `/worker/status` | Status atual do Worker |
| POST | `/worker/pause` | Pausar o Worker |
| POST | `/worker/resume` | Retomar o Worker |
| GET | `/health` | Health check (público) |

### Response Padrão

```json
{
  "success": true,
  "data": { ... },
  "message": null
}
```

### Paginação

```
GET /api/v1/etapas?page=1&pageSize=20&ativo=true
```

Response inclui: `total`, `page`, `pageSize`, `pageCount`.

---

## Frontend

- **Stack:** React 18 + TypeScript + Vite + Tailwind CSS
- **Porta:** 3000 (dev)
- **Tema:** Dark (fundo escuro, cores verdes)
- **Auth:** JWT armazenado em localStorage

### Páginas

| Rota | Página | Funcionalidade |
|------|--------|---------------|
| `/login` | Login | Formulário de autenticação |
| `/` | Dashboard | Status do Worker, métricas do dia |
| `/etapas` | Etapas | CRUD completo com modal |
| `/logs` | Logs | Tabela paginada com filtros |

### Rodar em desenvolvimento

```bash
cd src/STA.Web
npm run dev
```

O Vite faz proxy automático de `/api` para `localhost:5000` (API backend).

### Build para produção

```bash
npm run build
```

Gera pasta `dist/` (SPA estática) para deploy em IIS ou Nginx.

---

## Banco de Dados

- **SGBD:** PostgreSQL 15
- **Schema:** `sta`
- **ORM:** EF Core 10 (code-first, migrations)

### Tabelas

| Tabela | Propósito |
|--------|----------|
| `tbl_sistema` | Sistemas registrados (STA) |
| `tbl_parametro_sistema` | Parâmetros de execução (janela horária, intervalo) |
| `tbl_etapa_transferencia` | Etapas (agrupamento lógico de transferências) |
| `tbl_rota_transferencia` | Rotas (origem, máscara, compactação, backup) |
| `tbl_rota_destino` | Destinos por rota (fan-out) |
| `tbl_log_processo` | Log por ciclo de execução |
| `tbl_log_arquivo` | Log granular por arquivo transferido |
| `tbl_usuario` | Usuários do sistema (login local/LDAP) |
| `tbl_auditoria` | Audit trail (quem fez o quê) |
| `tbl_conexao_sftp` | Conexões SFTP configuradas (host, credenciais, scheduler) |
| `tbl_log_sftp` | Log dedicado de operações SFTP |
| `tbl_execucao_sftp` | Estado de execução por horário/dia SFTP |

### Aplicar migrations

```bash
cd src/STA.Worker
dotnet ef database update --connection "Host=localhost;Port=5432;Database=sta;Username=postgres;Password=SENHA"
```

### Backup

```bash
docker exec sta-postgres pg_dump -U postgres -d sta > backup_sta.sql
```

---

## Configuração

### Variáveis de Ambiente

| Variável | Descrição | Exemplo |
|----------|-----------|--------|
| `STA_DB_CONN` | Connection string PostgreSQL | `Host=srv;Database=sta;Username=sta_user;Password=xxx` |
| `DOTNET_ENVIRONMENT` | Ambiente (.NET) | `Development` ou `Production` |
| `ASPNETCORE_ENVIRONMENT` | Ambiente (API) | `Development` ou `Production` |

### appsettings.json (seção StaSettings)

| Propriedade | Tipo | Descrição | Padrão |
|-------------|------|-----------|--------|
| `NomeSistema` | string | Alias do sistema | `"STA"` |
| `CnProcesso` | int | ID do processo | `1` |
| `ArquivoPathsXml` | string | Caminho do XML (fallback) | `""` |
| `Arquivo7Zip` | string | Caminho do 7-Zip | `"C:\Program Files\7-Zip\7z.exe"` |
| `TimeoutCompactacaoMs` | int | Timeout compactação (ms) | `1800000` (30 min) |
| `SobreEscreverArquivos` | bool | Sobrescrever no destino | `true` |
| `QtdDiasExcluirLog` | int | Dias de retenção de logs | `5` |
| `GeraLogSucessoBancoDados` | bool | Gravar log quando sucesso | `true` |
| `UseXmlFallback` | bool | Usar XML em vez do banco | `false` |

### JWT (seção Jwt)

| Propriedade | Descrição |
|-------------|----------|
| `Secret` | Chave de assinatura (mín. 32 chars) |
| `Issuer` | Emissor do token |
| `Audience` | Audiência do token |
| `ExpirationHours` | Validade do token em horas |

---

## Deploy em Produção

### Worker (Windows Service)

```powershell
# Publicar
dotnet publish src/STA.Worker -c Release -o C:\STA\worker

# Instalar como serviço
sc.exe create STAWorker binPath= "C:\STA\worker\STA.Worker.exe" start= auto
sc.exe start STAWorker
```

### API (IIS)

```powershell
# Publicar
dotnet publish src/STA.Api -c Release -o C:\STA\api

# Configurar no IIS:
# - Application Pool: No Managed Code
# - Site apontando para C:\STA\api
# - HTTPS habilitado
```

### Frontend (IIS como SPA)

```bash
cd src/STA.Web
npm run build
# Copiar dist/ para C:\STA\web
```

Adicionar URL Rewrite no IIS para SPA (todas rotas → index.html).

---

## Roadmap

| Fase | Status | Descrição |
|------|:------:|----------|
| 1-3 | ✅ | Plumbing, janela horária, transferência core |
| 5.1 | ✅ | STA.Core como biblioteca compartilhada |
| 5.2 | ✅ | Tabelas de configuração via banco |
| 5.3 | ✅ | Log granular por arquivo |
| 6 | ✅ | API REST + JWT + Worker control |
| 7 | ✅ | Frontend React (Login, Dashboard, CRUD, Logs) |
| 8 | ✅ | Segurança: AD/LDAP + BCrypt + Roles + Rate Limiting |
| 9 | 📋 | Notificações (email/Teams em falhas) |
| 10 | 📋 | Audit trail (histórico de alterações) |
| 11 | 📋 | CI/CD pipeline + Docker |

---

## Segurança e Autenticação

### Arquitetura de Auth

```
Usuário digita credenciais
        ↓
API tenta LDAP primeiro (se Ldap:Enabled=true)
        ↓
Se AD responder → role vem do grupo AD → gera JWT
Se AD falhar → fallback banco local (BCrypt) → gera JWT
        ↓
Frontend armazena JWT em sessionStorage (morre ao fechar browser)
```

### Active Directory (LDAP)

**Tecnologia:** Samba AD via Docker (domínio STA.LOCAL, LDAPS porta 636)

**Configuração** (`appsettings.json`):
```json
"Ldap": {
  "Enabled": true,
  "Server": "localhost",
  "BaseDn": "DC=sta,DC=local",
  "Domain": "STA.LOCAL"
}
```

**Criar usuário no AD:**
```bash
# Criar usuário
docker exec sta-samba-ad samba-tool user add nome.usuario SenhaForte123 --given-name=Nome --surname=Sobrenome

# Adicionar ao grupo (define a role)
docker exec sta-samba-ad samba-tool group addmembers STA-Admins nome.usuario
```

**Grupos AD → Roles:**
| Grupo AD | Role | Permissões |
|----------|------|------------|
| STA-Admins | Admin | Tudo: CRUD, Worker control, gestão |
| STA-Operators | Operator | CRUD transferências, ver logs |
| STA-Viewers | Viewer | Apenas leitura: dashboard, logs |

### Banco Local (Fallback)

**Tecnologia:** BCrypt (12 rounds) via pacote `BCrypt.Net-Next`

**Tabela:** `sta.tbl_usuario`
| Campo | Tipo | Descrição |
|-------|------|----------|
| cn_usuario | serial PK | ID |
| nm_usuario | varchar(100) | Login (único) |
| nm_display | varchar(200) | Nome de exibição |
| ds_senha_hash | varchar(500) | Hash BCrypt |
| id_role | varchar(20) | Admin/Operator/Viewer |
| fl_ativo | boolean | Ativo ou desativado |
| nr_tentativas_falhas | int | Contador de erros |
| dt_bloqueado_ate | timestamptz | Bloqueado até |

**Criar usuário local (via SQL):**
```sql
-- Gerar hash com Node.js: node -e "const b=require('bcryptjs');console.log(b.hashSync('senha123',10))"
INSERT INTO sta.tbl_usuario (nm_usuario, nm_display, ds_senha_hash, id_role)
VALUES ('novo.usuario', 'Novo Usuário', '$2a$10$HASH_GERADO', 'Operator');
```

### Bloqueio de Conta

- **5 tentativas erradas** consecutivas → bloqueia por **15 minutos**
- Login correto → reseta contador
- Administrador pode desbloquear via banco:
```sql
UPDATE sta.tbl_usuario SET nr_tentativas_falhas = 0, dt_bloqueado_ate = NULL WHERE nm_usuario = 'usuario';
```

### Roles no Sistema

| Ação | Admin | Operator | Viewer |
|------|:-----:|:--------:|:------:|
| Ver Dashboard/Logs | ✅ | ✅ | ✅ |
| Criar/Editar/Excluir transferências | ✅ | ✅ | ❌ |
| Pausar/Retomar Worker | ✅ | ❌ | ❌ |
| Validar/Criar diretórios | ✅ | ✅ | ❌ |
| Gestão de usuários (futuro) | ✅ | ❌ | ❌ |

**No frontend:** botões de ação ficam ocultos para roles sem permissão.
**Na API:** endpoints retornam 403 Forbidden se role insuficiente.

### Rate Limiting

| Endpoint | Limite | Comportamento |
|----------|--------|---------------|
| POST /auth/login | 5 req/min por IP | Protege contra brute-force |
| API geral | 100 req/min por IP | Protege contra DDoS |
| Excedeu limite | HTTP 429 | Too Many Requests |

### Security Headers

Aplicados em toda response:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy: camera=(), microphone=(), geolocation=()`

### JWT

| Propriedade | Valor |
|-------------|-------|
| Algoritmo | HS256 (HMAC-SHA256) |
| Expiração | 8 horas |
| Armazenamento | sessionStorage (morre ao fechar browser) |
| Claims | Name, Role, Jti (unique ID) |
| Verificação | Frontend decode payload.exp |

### Trocar Senha

```
POST /api/v1/auth/trocar-senha
Body: { "senhaAtual": "...", "novaSenha": "..." }
```
- Exige autenticação (token válido)
- Valida senha atual antes de trocar
- Nova senha mínimo 8 caracteres

---

## Decisões Técnicas

| Decisão | Motivo |
|---------|--------|
| .NET 10 (não Framework) | Moderno, multiplataforma, melhor performance |
| PostgreSQL (não SQL Server) | Open source, Docker-friendly, JSONB |
| EF Core code-first | Migrations versionadas, type-safe |
| Schema `sta` (não `public`) | Isolamento, multi-tenant futuro |
| JWT (não cookies) | SPA-friendly, stateless |
| Tailwind (não Bootstrap) | Utility-first, tree-shaking, dark theme nativo |
| Vite (não CRA) | Build 10x mais rápido, HMR instantâneo |
| Zustand (não Redux) | Simples, sem boilerplate, TypeScript nativo |
| Worker como BackgroundService | Controle total do ciclo, sem Hangfire overhead |
| Config no banco (não XML) | Editável via tela, sem restart necessário |
| Log por arquivo (não só por ciclo) | Rastreabilidade granular, debug facilitado |
| Fan-out sequencial | Previsível, sem race conditions, simples de debugar |
| Compactação Fast (-mx=3) | 5-10x mais rápido que Ultra, suficiente para envio em rede |

---

## Limitações Conhecidas

| Limitação | Motivo | Workaround |
|-----------|--------|------------|
| Arquivo parcial (cópia incompleta na origem) pode ser transferido | Worker valida lock do arquivo, mas se o handle foi liberado (ex: cópia pausada no Explorer) o arquivo aparece como "livre" | Aguarde a cópia terminar antes do próximo ciclo (5 min). Em produção, sistemas geradores mantêm lock durante escrita. |
| Ciclo não é interrompível mid-transfer | Pausa via API só afeta o próximo ciclo, não o atual | Ciclo termina normalmente; arquivo em transferência não é corrompido (File.Copy é atômico). |
| Worker e API são processos separados | Estado de execução real-time depende de consultar o banco | Polling de 5s no frontend; etapa mostrada pode ter delay de até 5s. |
| Countdown pode mostrar "em breve" se Worker não está rodando | Cálculo baseia-se no último ciclo + 5 min | Reiniciar o Worker gera um ciclo imediato e o countdown volta ao normal. |
