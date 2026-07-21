# AGENTS.md — TAE-STA

Instruções para qualquer agente de código (Claude Code, Cursor, Codex, Copilot, etc.) que trabalhe neste projeto.

---

## Contexto rápido

- **Projeto:** TAE-STA (Transferência de Arquivos Enterprise)
- **Stack:** .NET 10 + ASP.NET Core + EF Core + PostgreSQL + React + Vite + Tailwind + Zustand
- **Arquitetura:** Monorepo com 3 apps (API, Worker, Web) + 1 lib (Core) + testes
- **Banco:** PostgreSQL, schema `sta`, 12 tabelas, code-first migrations
- **Testes:** 108 (xUnit + Moq + EF In-Memory)
- **CI:** GitHub Actions (windows-latest, self-contained win-x64)

---

## Como rodar

```bash
docker compose up -d              # Postgres + Samba AD
dotnet ef database update --project src/STA.Worker --startup-project src/STA.Worker
dotnet run --project src/STA.Api  # porta 5000
dotnet run --project src/STA.Worker
cd src/STA.Web && npm install && npm run dev  # porta 3000
```

---

## Testes

```bash
dotnet test STA.sln               # 108 testes, todos devem passar
cd src/STA.Web && npm run build   # TypeScript + Vite, sem erros
```

**Critério:** nunca commitar se testes falharem.

---

## Convenções

### Commits
- Conventional Commits: `feat:`, `fix:`, `docs:`, `test:`, `refactor:`, `chore:`
- Em blocos lógicos (não big-bang)
- Validar (build + test) antes de cada commit

### Backend (.NET)
- Entidades: `src/STA.Core/Data/Entities/` (POCO)
- Repositórios: `src/STA.Core/Data/Repositories/` (interface + EF)
- Services: `src/STA.Core/Services/` (interface + implementação)
- Controllers: `src/STA.Api/Controllers/` (thin, delega pra services)
- DTOs: `src/STA.Api/Dtos/` (records imutáveis)
- Transports: `src/STA.Core/Services/Transports/` (abstração LOCAL/SFTP)

### Banco de dados
- Schema: `sta`
- Tabelas: `tbl_` + snake_case
- Colunas: snake_case com prefixo (`cn_` PK, `nm_` nome, `ds_` descrição, `fl_` flag, `dt_` data, `id_` identificador, `nr_` número)
- Migrations: `src/STA.Worker/Data/Migrations/`

### Frontend (React)
- TypeScript strict
- Páginas: `src/STA.Web/src/pages/`
- Componentes: `src/STA.Web/src/components/`
- Tipos: `src/STA.Web/src/types/index.ts`
- API client: `src/STA.Web/src/lib/api.ts` (Axios + interceptor)
- Styling: Tailwind CSS (dark theme)

---

## Regras de segurança

- Senhas: BCrypt 12 rounds (banco) ou DPAPI (SFTP)
- Senhas NUNCA retornadas em GET (usar `flPossuiSenha` boolean)
- Senhas NUNCA logadas em plaintext
- JWT em sessionStorage (morre ao fechar aba)
- Credenciais de dev em `appsettings.Development.json` (gitignored)
- `Properties/launchSettings.json` gitignored

---

## Gotchas

- `UseWindowsService()` — só funciona em Windows
- DPAPI — amarra à conta do serviço (se mudar conta, credenciais SFTP quebram)
- `DsPadraoRename` — coluna NÃO segue snake_case (legacy)
- EF In-Memory — não suporta `ExecuteDeleteAsync`
- SSH.NET `PrivateKeyFile` — tenta abrir arquivo no construtor (validar antes)
- Fan-out — all-or-nothing (se 1 destino falha, origem NÃO é apagada)
- Auditoria — best-effort (try/catch, nunca derruba o request)

---

## Checklist antes de commitar

- [ ] `dotnet build` sem erros
- [ ] `dotnet test` — 108+ passando
- [ ] `npm run build` (frontend) sem erros
- [ ] DI registrada pra interfaces novas (`Program.cs`)
- [ ] Dispose chamado em recursos IDisposable
- [ ] Testes existentes não quebraram

---

## Documentação relacionada

| Arquivo | Conteúdo |
|---------|----------|
| `CLAUDE.md` | Regras específicas pro Claude Code (não commitado) |
| `DOCS.md` | Documentação técnica completa |
| `DEPLOY.md` | Manual de instalação/atualização |
| `README.md` | Visão geral + roadmap |
| `config/appsettings.template.json` | Template de configuração |
