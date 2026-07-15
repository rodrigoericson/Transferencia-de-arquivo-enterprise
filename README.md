# STA — Sistema de Transferência de Arquivos

<div align="center">

![Status](https://img.shields.io/badge/status-active-3DDC84?style=flat-square)
![Coverage](https://img.shields.io/badge/coverage-72%2F72%20tests-3DDC84?style=flat-square&logo=xunit&logoColor=white)
![Phase](https://img.shields.io/badge/fase-5.3%20%E2%9C%93%20%E2%86%92%206-FF6B6B?style=flat-square)
![Stack](https://img.shields.io/badge/stack-.NET%2010%20%2B%20EF%20Core%20%2B%20Postgres-512BD4?style=flat-square&logo=.net&logoColor=white)

</div>

<br>

> Serviço que move arquivos entre servidores de produção. Roda 24/7, dorme entre ciclos, acorda, transfere, dorme de novo.

<br>

## 📋 O que é

Nasceu como um Windows Service em **VB.NET + Sybase** (legado corporativo). Reescrito do zero em **.NET 10 + EF Core + PostgreSQL**, mantendo o comportamento essencial (janela horária, multi-origem, fan-out, log por arquivo) mas com configuração 100% via banco e telemetria granular.

A migração inteira está nesse repositório — commit por commit, fase por fase.

## 🏗️ Arquitetura

```
 Worker (ciclo 5min)          PostgreSQL            Web API (Fase 6)
 ──────────────────          ──────────            ─────────────────
   Carrega config  ────────▶  tbl_etapa     ◀────  CRUD etapas/rotas
   Transfere files ────────▶  tbl_log_arquivo ◀───  GET logs/status
   Grava resultado ────────▶  tbl_log_processo ◀── Pause/Resume
```

- ⚙️ **Worker** — serviço Windows, roda em background, transfere e loga
- 🌐 **API** — (em construção) expõe CRUD, consulta de logs, controle do Worker
- 🗄️ **Banco** — ponto central: configuração + telemetria + estado

## 🧰 Stack

| Camada | Tecnologia | Badge |
|--------|------------|-------|
| Linguagem | C# / .NET 10 | ![dotnet](https://img.shields.io/badge/-512BD4?style=flat-square&logo=.net&logoColor=white) |
| ORM | EF Core 8 | ![efcore](https://img.shields.io/badge/-512BD4?style=flat-square&logo=nuget&logoColor=white) |
| Banco | PostgreSQL 15 | ![postgres](https://img.shields.io/badge/-336791?style=flat-square&logo=postgresql&logoColor=white) |
| Logging | Structured (built-in) | ![logs](https://img.shields.io/badge/-F25022?style=flat-square&logo=elasticstack&logoColor=white) |
| Testes | xUnit + Moq | ![xunit](https://img.shields.io/badge/-3DDC84?style=flat-square&logo=xunit&logoColor=white) |
| Service | Windows Service | ![windows](https://img.shields.io/badge/-0078D6?style=flat-square&logo=windows&logoColor=white) |

## 🗺️ Roadmap

| Fase | Status | Descrição | Badge |
|------|:------:|-----------|-------|
| 1-3 | ✅ | Plumbing, janela horária, transferência core | ![done](https://img.shields.io/badge/-3DDC84?style=flat-square) |
| 5.1 | ✅ | Extrai STA.Core como lib compartilhada | ![done](https://img.shields.io/badge/-3DDC84?style=flat-square) |
| 5.2 | ✅ | Tabelas Etapa/Rota/Destino + fallback banco→XML | ![done](https://img.shields.io/badge/-3DDC84?style=flat-square) |
| 5.3 | ✅ | Log granular por arquivo (tbl_log_arquivo) | ![done](https://img.shields.io/badge/-3DDC84?style=flat-square) |
| 6 | 🚧 | Web API REST + Worker control | ![wip](https://img.shields.io/badge/-FFA500?style=flat-square) |
| 7 | 📋 | Frontend React + dashboard | ![todo](https://img.shields.io/badge/-lightgrey?style=flat-square) |
| 8 | 📋 | Notificações, audit trail, CI/CD | ![todo](https://img.shields.io/badge/-lightgrey?style=flat-square) |

## 🚀 Subindo o ambiente

Você vai precisar de:

![dotnet](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=.net&logoColor=white)
![docker](https://img.shields.io/badge/Docker-latest-2496ED?style=flat-square&logo=docker&logoColor=white)
![git](https://img.shields.io/badge/Git-latest-F05032?style=flat-square&logo=git&logoColor=white)

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

> **💡 Ambientes:** em *Development* lê `appsettings.Development.json` (credenciais locais, gitignored). Em *Production* usa env vars (`STA_DB_CONN`, etc).

## 📁 Estrutura

```
src/
├── STA.Core/          # Domínio, entidades, repos, services, models
├── STA.Worker/        # BackgroundService + migrations + Program.cs
tests/
└── STA.Tests/         # xUnit, 72 testes
docker-compose.yml     # Postgres dev
STA.sln                # Solução
```

## 💡 Por que esse repo existe

Migração de VB.NET Framework 2.0 → .NET 10 não é trivial. Mas é o tipo de projeto que mostra **disciplina técnica**: refactor incremental, testes crescendo junto com features, sem big-bang rewrite.

Cada commit conta parte da história. Leia em ordem:

```
Initial commit → ... → Fase 5.3 → ...
```

## 📜 Licença

Privado. Trabalho original.