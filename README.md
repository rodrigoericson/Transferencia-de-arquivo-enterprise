# STA — Sistema de Transferência de Arquivos

<p align="center">
  <img src="./sta-control-center.svg" width="100%">
</p>

<svg xmlns="http://www.w3.org/2000/svg" width="1000" height="380" viewBox="0 0 1000 380">

  <defs>

    <linearGradient id="bg" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0%" stop-color="#101010"/>
      <stop offset="100%" stop-color="#050505"/>
    </linearGradient>

    <filter id="glow">
      <feGaussianBlur stdDeviation="2.5" result="blur"/>
      <feMerge>
        <feMergeNode in="blur"/>
        <feMergeNode in="SourceGraphic"/>
      </feMerge>
    </filter>

    <pattern id="scan" width="4" height="4" patternUnits="userSpaceOnUse">
      <rect width="4" height="2" fill="#ffffff08"/>
      <rect y="2" width="4" height="2" fill="#00000000"/>
    </pattern>

  </defs>

  <!-- Background -->

  <rect width="100%" height="100%" fill="url(#bg)"/>

  <rect width="100%" height="100%" fill="url(#scan)" opacity=".18"/>

  <!-- Moldura -->

  <rect
      x="12"
      y="12"
      width="976"
      height="356"
      rx="8"
      fill="none"
      stroke="#00ff88"
      stroke-width="2"/>

  <!-- Cabeçalho -->

  <text
      x="30"
      y="42"
      fill="#7dffb3"
      font-size="24"
      font-family="JetBrains Mono, Consolas, monospace"
      filter="url(#glow)">

STA CONTROL CENTER
  </text>

  <text
      x="830"
      y="42"
      fill="#55ff55"
      font-size="18"
      font-family="JetBrains Mono, Consolas, monospace">

BUILD 10.0
  </text>

  <line x1="25" y1="58" x2="975" y2="58" stroke="#00ff88"/>

  <!-- Serviços -->

  <g
      fill="#7dffb3"
      font-family="JetBrains Mono, Consolas, monospace"
      font-size="18">

    <text x="40" y="95">● POWER</text>
    <text x="280" y="95" fill="#00ff66">ONLINE</text>

    <text x="40" y="125">● WORKER</text>
    <text x="280" y="125" fill="#00ff66">RUNNING</text>

    <text x="40" y="155">● DATABASE</text>
    <text x="280" y="155" fill="#00ff66">CONNECTED</text>

    <text x="40" y="185">● FILE TRANSFER</text>
    <text x="280" y="185" fill="#00ff66">ACTIVE</text>

    <text x="40" y="215">◐ REST API</text>
    <text x="280" y="215" fill="#ffd54d">PHASE 6</text>

    <text x="40" y="245">○ DASHBOARD</text>
    <text x="280" y="245" fill="#888888">OFFLINE</text>

  </g>

  <!-- Barras -->

  <g
      font-family="JetBrains Mono, Consolas, monospace"
      font-size="17">

    <text x="530" y="95" fill="#7dffb3">CPU</text>

    <rect x="600" y="80" width="250" height="14" fill="#222"/>
    <rect x="600" y="80" width="55" height="14" fill="#00ff66"/>
    <text x="870" y="92" fill="#55ff55">22%</text>

    <text x="530" y="130" fill="#7dffb3">RAM</text>

    <rect x="600" y="115" width="250" height="14" fill="#222"/>
    <rect x="600" y="115" width="95" height="14" fill="#00ff66"/>
    <text x="870" y="127" fill="#55ff55">38%</text>

    <text x="530" y="165" fill="#7dffb3">NETWORK</text>

    <rect x="600" y="150" width="250" height="14" fill="#222"/>
    <rect x="600" y="150" width="180" height="14" fill="#00ff66"/>
    <text x="870" y="162" fill="#55ff55">72%</text>

    <text x="530" y="200" fill="#7dffb3">DISK</text>

    <rect x="600" y="185" width="250" height="14" fill="#222"/>
    <rect x="600" y="185" width="80" height="14" fill="#00ff66"/>
    <text x="870" y="197" fill="#55ff55">32%</text>

  </g>

  <!-- Rodapé -->

  <line x1="25" y1="280" x2="975" y2="280" stroke="#00ff88"/>

  <g
      fill="#7dffb3"
      font-size="18"
      font-family="JetBrains Mono, Consolas, monospace">

    <text x="40" y="315">
LAST TRANSFER
    </text>

    <text x="300" y="315" fill="#00ff66">
SUCCESS
    </text>

    <text x="40" y="345">
NEXT CYCLE
    </text>

    <text x="300" y="345">
00:04:58
    </text>

    <text x="610" y="315">
FILES TODAY
    </text>

    <text x="850" y="315">
1248
    </text>

    <text x="610" y="345">
ERRORS
    </text>

    <text x="850" y="345" fill="#00ff66">
0
    </text>

  </g>

</svg>

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

> **💡 Ambientes:** em *Desenvolvimento* lê `appsettings.Development.json` (credenciais locais, gitignored). Em *Produção* usa env vars (`STA_DB_CONN`, etc).

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

Migração de VB.NET Framework 2.0 → .NET 10 não é trivial. Mas é o tipo de projeto que mostra **disciplina técnica**: refactor incremental, testes crescendo junto com features.
Cada commit conta parte da história. Leia em ordem:

```
Initial commit → ... → Fase 5.3 → ...
```

## 📜 Licença

Privado. Trabalho original.