# STA — Sistema de Transferência de Arquivos

> Serviço que move arquivos entre servidores de produção. Roda 24/7, dorme entre ciclos, acorda, transfere, dorme de novo.

<div align="center">

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=.net&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-336791?style=flat-square&logo=postgresql&logoColor=white)
![EF Core](https://img.shields.io/badge/EF%20Core-8.0-512BD4?style=flat-square)
![xUnit](https://img.shields.io/badge/tests-72%2F72-3DDC84?style=flat-square&logo=xunit&logoColor=white)
![Build](https://img.shields.io/badge/build-passing-3DDC84?style=flat-square)
![License](https://img.shields.io/badge/license-private-lightgrey?style=flat-square)

</div>

## O que é

Nasceu como um Windows Service em VB.NET + Sybase (legado corporativo). Reescrito do zero em **.NET 10 + EF Core + PostgreSQL**, mantendo o comportamento essencial (janela horária, multi-origem, fan-out, log por arquivo) mas com configuração 100% via banco e telemetria granular.

A migração inteira está nesse repositório — commit por commit, fase por fase.

## Arquitetura

```
 Worker (ciclo 5min)          PostgreSQL            Web API (Fase 6)
 ──────────────────          ──────────            ───────────────── 
   Carrega config  ────────▶  tbl_etapa     ◀────  CRUD etapas/rotas
   Transfere files ────────▶  tbl_log_arquivo ◀────  GET logs/status
   Grava resultado ────────▶  tbl_log_processo◀────  Pause/Resume
```

- **Worker** — serviço Windows, roda em background, transfere e loga
- **API** — (em construção) expõe CRUD, consulta de logs, controle do Worker
- **Banco** — ponto central: configuração + telemetria + estado

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

## Subindo o ambiente

Você vai precisar de: .NET 10 SDK, Docker e paciência pra primeira migração.

```bash
# 1. Postgres
docker compose up -d postgres

# 2. Schema (Worker cria as tabelas via EF migrations)
cd src/STA.Worker
dotnet ef database update

# 3. Worker rodando
dotnet run

# 4. Validar que tudo funciona
dotnet test STA.sln
```

> **Ambientes:** em *Development* lê `appsettings.Development.json` (credenciais locais, gitignored). Em *Production* usa env vars (`STA_DB_CONN`, etc).

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

## Por que esse repo existe

Migração de VB.NET Framework 2.0 → .NET 10 não é trivial. Mas é o tipo de projeto que mostra **disciplina técnica**: refactor incremental, testes crescendo junto com features, sem big-bang rewrite.

Cada commit conta parte da história. Leia em ordem:
```
Initial commit → ... → Fase 5.3 → ...
```

## Licença

Privado. Trabalho original.

## Licença

Privado. Trabalho original.