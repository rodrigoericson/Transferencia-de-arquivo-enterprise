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

![Status](https://img.shields.io/badge/status-ativo-3DDC84?style=flat-square)
![Cobertura](https://img.shields.io/badge/cobertura-72%2F72%20testes-3DDC84?style=flat-square&logo=xunit&logoColor=white)
![Fase](https://img.shields.io/badge/fase-5.3%20%E2%9C%93%20%E2%86%92%206-FF6B6B?style=flat-square)
![Stack](https://img.shields.io/badge/stack-.NET%2010%20%2B%20EF%20Core%20%2B%20Postgres-512BD4?style=flat-square&logo=.net&logoColor=white)

</div>

<br>

> Serviço Windows que automatiza transferência de arquivos entre servidores, com janela horária configurável, fan-out multi-destino, compactação 7-Zip e log granular por arquivo.

<br>

<p align="center">
  <img src="./sta-dashboard.svg" width="100%">
</p>

<br>

## 🚀 Subindo o ambiente

```bash
# 1. Banco de dados
docker compose up -d postgres

# 2. Criar tabelas (migrations EF Core)
cd src/STA.Worker
dotnet ef database update

# 3. Rodar o Worker
dotnet run

# 4. Rodar os testes
dotnet test STA.sln
```

> **💡 Ambientes:** em *Desenvolvimento* lê `appsettings.Development.json` (credenciais locais, gitignored). Em *Produção* usa variáveis de ambiente (`STA_DB_CONN`, etc).

## 📁 Estrutura do projeto

```
src/
├── STA.Core/          # Domínio, entidades, repositórios, serviços
├── STA.Worker/        # Serviço Windows + migrations + Program.cs
├── STA.Api/           # API REST (Fase 6)
tests/
└── STA.Tests/         # 72 testes unitários e de integração
docker-compose.yml     # PostgreSQL para desenvolvimento
STA.sln                # Solução
```

## 💡 Sobre o projeto

Migração de VB.NET Framework 2.0 → .NET 10. Refatoração incremental, testes crescendo junto com features, sem reescrita big-bang.

## 📜 Licença

Privado. Trabalho original.
