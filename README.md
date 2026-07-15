# STA — Sistema de Transferência de Arquivos

> Serviço que move arquivos entre servidores de produção. Roda 24/7, dorme entre ciclos, acorda, transfere, dorme de novo.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=.net)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-336791?logo=postgresql)](https://www.postgresql.org/)
[![EF Core](https://img.shields.io/badge/EF%20Core-8.0-512BD4)](https://learn.microsoft.com/ef/core)
[![Tests](https://img.shields.io/badge/tests-72%2F72-3DDC84?logo=xunit)](https://xunit.net/)
[![Build](https://img.shields.io/badge/build-passing-3DDC84?logo=github-actions)](#)

## O que é

Nasceu como um Windows Service em VB.NET + Sybase (legado corporativo). Reescrito do zero em **.NET 10 + EF Core + PostgreSQL**, mantendo o comportamento essencial (janela horária, multi-origem, fan-out, log por arquivo) mas com configuração 100% via banco e telemetria granular.

A migração inteira está nesse repositório — commit por commit, fase por fase.

## Arquitetura

```
┌─────────────┐       ┌──────────────┐       ┌─────────────┐
│   Worker    │──────▶│  PostgreSQL  │◀──────│   Web API   │
│ (5min ciclo)│       │  (Postgres)  │       │  (Fase 6)   │
└─────────────┘       └──────────────┘       └─────────────┘
       │                      ▲
       │    ┌─────────────────┴──────────────┐
       ▼    │                                │
   Arquivos  │  tbl_etapa_transferencia       │ tbl_log_arquivo
   em UNC    │  tbl_rota_transferencia        │ tbl_log_processo
             │  tbl_rota_destino              │
             └────────────────────────────────┘
```

**Worker** lê configuração do banco → transfere arquivos → grava log por arquivo.
**API** (em construção) expõe CRUD + consultas + controle de pausa.

## Stack

| Camada | Tecnologia | Por quê |
|--------|------------|---------|
| Linguagem | C# / .NET 10 | Stack moderna, async-first, performance |
| ORM | EF Core 8 | Migrations, Include chains, type-safe |
| Banco | PostgreSQL 15 | JSONB, window functions, Docker-friendly |
| Logging | Structured (built-in) | Templates, scopes, sinks plugáveis |
| Testes | xUnit + Moq | Padrão .NET, rápido |
| Service | Windows Service | Integração nativa Windows |

## Roadmap

| Fase | Status | O quê |
|------|:------:|-------|
| 1-3 | ✅ | Plumbing, janela horária, transferência core |
| 5.1 | ✅ | Extrai STA.Core como lib compartilhada |
| 5.2 | ✅ | Tabelas Etapa/Rota/Destino + fallback banco→XML |
| 5.3 | ✅ | Log granular por arquivo (tbl_log_arquivo) |
| 6 | 🚧 | **Web API REST + Worker control (pause/resume)** |
| 7 | 📋 | Frontend React + dashboard |
| 8 | 📋 | Notificações, audit trail, CI/CD, Docker |

## Como rodar localmente

### Pré-requisitos
- .NET 10 SDK
- Docker (ou PostgreSQL 15 local)
- Git

### 1. Subir o Postgres
```bash
docker compose up -d postgres
```

### 2. Aplicar migrations
```bash
cd src/STA.Worker
dotnet ef database update
```

### 3. Rodar o Worker
```bash
dotnet run --project src/STA.Worker
```

Em Development mode, ele lê `appsettings.Development.json` com credenciais locais. Em Production, usa variáveis de ambiente (`STA_DB_CONN`, etc).

### 4. Rodar testes
```bash
dotnet test STA.sln
```

## Estrutura

```
src/
├── STA.Core/          # Domínio, entidades, repos, services, models
├── STA.Worker/        # BackgroundService + migrations + Program.cs
tests/
└── STA.Tests/         # xUnit, 72 testes
docker-compose.yml     # Postgres dev
STA.sln                # Solução
```

Decisões e padrões detalhados em [.claude/skills/](.claude/skills/).

## Por que esse repo existe

Migração de VB.NET Framework 2.0 → .NET 10 não é trivial. Mas é o tipo de projeto que mostra **disciplina técnica**: refactor incremental, testes crescendo junto com features, sem big-bang rewrite.

Cada commit conta parte da história. Leia em ordem:
```
Initial commit → ... → Fase 5.3 → ...
```

## Licença

Privado. Trabalho original.