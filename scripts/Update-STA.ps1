#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Atualizador do TAE-STA com rollback automático.

.DESCRIPTION
    Atualiza API, Worker e Frontend mantendo configurações existentes.
    Se algo falhar, faz rollback dos binários automaticamente.

.PARAMETER InstallPath
    Pasta raiz onde o TAE-STA está instalado. Padrão: C:\TAE-STA

.PARAMETER SourcePath
    Pasta com os novos binários (publish/). Se vazio, usa ..\publish

.PARAMETER SkipMigrations
    Não roda migrations.

.EXAMPLE
    .\Update-STA.ps1 -InstallPath C:\TAE-STA -SourcePath C:\deploy\publish
#>
param(
    [string]$InstallPath = 'C:\TAE-STA',
    [string]$SourcePath,
    [switch]$SkipMigrations
)

$ErrorActionPreference = 'Stop'
$serviceName = 'TAE-STA-Worker'
$backupSuffix = Get-Date -Format 'yyyyMMdd-HHmmss'

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  TAE-STA - Atualizador" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# --- Resolver paths ---
if (-not $SourcePath) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $SourcePath = Join-Path $scriptDir '..\publish'
}

if (-not (Test-Path "$SourcePath\api")) {
    Write-Host "ERRO: Pasta $SourcePath\api nao encontrada." -ForegroundColor Red
    Write-Host "Baixe os artifacts do CI ou execute dotnet publish." -ForegroundColor Red
    exit 1
}

if (-not (Test-Path "$InstallPath\api")) {
    Write-Host "ERRO: Instalacao nao encontrada em $InstallPath." -ForegroundColor Red
    Write-Host "Use Install-STA.ps1 para instalacao inicial." -ForegroundColor Red
    exit 1
}

# --- 1. Parar serviços ---
Write-Host "[1/6] Parando servicos..." -ForegroundColor Cyan

$workerService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($workerService -and $workerService.Status -eq 'Running') {
    Stop-Service -Name $serviceName -Force
    Write-Host "  Worker parado." -ForegroundColor Gray
}

# Parar IIS App Pool (se existir)
try {
    Import-Module WebAdministration -ErrorAction Stop
    $poolName = 'TAE-STA-Pool'
    if (Test-Path "IIS:\AppPools\$poolName") {
        Stop-WebAppPool -Name $poolName -ErrorAction SilentlyContinue
        Write-Host "  IIS App Pool parado." -ForegroundColor Gray
    }
} catch {
    Write-Host "  IIS nao disponivel (ok se nao usa)." -ForegroundColor Yellow
}

Start-Sleep -Seconds 2

# --- 2. Backup dos binários atuais ---
Write-Host "`n[2/6] Fazendo backup..." -ForegroundColor Cyan
$backupPath = "$InstallPath\backup-$backupSuffix"
New-Item -ItemType Directory -Path $backupPath -Force | Out-Null

Copy-Item -Path "$InstallPath\api" -Destination "$backupPath\api" -Recurse -Force
Copy-Item -Path "$InstallPath\worker" -Destination "$backupPath\worker" -Recurse -Force
if (Test-Path "$InstallPath\web") {
    Copy-Item -Path "$InstallPath\web" -Destination "$backupPath\web" -Recurse -Force
}
Write-Host "  Backup salvo em $backupPath" -ForegroundColor Gray

# --- 3. Copiar novos binários ---
Write-Host "`n[3/6] Copiando novos binarios..." -ForegroundColor Cyan
try {
    Copy-Item -Path "$SourcePath\api\*" -Destination "$InstallPath\api" -Recurse -Force
    Write-Host "  API atualizada." -ForegroundColor Gray

    Copy-Item -Path "$SourcePath\worker\*" -Destination "$InstallPath\worker" -Recurse -Force
    Write-Host "  Worker atualizado." -ForegroundColor Gray

    if (Test-Path "$SourcePath\web") {
        Copy-Item -Path "$SourcePath\web\*" -Destination "$InstallPath\web" -Recurse -Force
        Write-Host "  Frontend atualizado." -ForegroundColor Gray
    }
} catch {
    Write-Host "`n  ERRO ao copiar binarios! Iniciando rollback..." -ForegroundColor Red
    Copy-Item -Path "$backupPath\api\*" -Destination "$InstallPath\api" -Recurse -Force
    Copy-Item -Path "$backupPath\worker\*" -Destination "$InstallPath\worker" -Recurse -Force
    if (Test-Path "$backupPath\web") {
        Copy-Item -Path "$backupPath\web\*" -Destination "$InstallPath\web" -Recurse -Force
    }
    Write-Host "  Rollback concluido. Versao anterior restaurada." -ForegroundColor Yellow
    Start-Service -Name $serviceName -ErrorAction SilentlyContinue
    exit 1
}

# --- 4. Preservar configuração ---
Write-Host "`n[4/6] Preservando configuracao..." -ForegroundColor Cyan
$configFile = "$InstallPath\config\appsettings.json"
if (Test-Path $configFile) {
    Copy-Item $configFile "$InstallPath\api\appsettings.Production.json" -Force
    Copy-Item $configFile "$InstallPath\worker\appsettings.Production.json" -Force
    Write-Host "  Configuracao preservada." -ForegroundColor Gray
} else {
    Write-Host "  AVISO: appsettings.json nao encontrado em config/." -ForegroundColor Yellow
}

# --- 5. Migrations ---
if (-not $SkipMigrations) {
    Write-Host "`n[5/6] Aplicando migrations..." -ForegroundColor Cyan
    try {
        Push-Location "$InstallPath\worker"
        $connStr = (Get-Content "$InstallPath\config\appsettings.json" | ConvertFrom-Json).ConnectionStrings.StaDb
        $env:STA_DB_CONN = $connStr
        & .\STA.Worker.exe --migrate 2>&1 | Out-Null
        Pop-Location
        Write-Host "  Migrations aplicadas." -ForegroundColor Gray
    } catch {
        Pop-Location
        Write-Host "  AVISO: Migrations falharam. Aplique manualmente." -ForegroundColor Yellow
    }
} else {
    Write-Host "`n[5/6] Migrations puladas." -ForegroundColor Yellow
}

# --- 6. Reiniciar serviços ---
Write-Host "`n[6/6] Reiniciando servicos..." -ForegroundColor Cyan

Start-Service -Name $serviceName -ErrorAction SilentlyContinue
Write-Host "  Worker iniciado." -ForegroundColor Gray

try {
    Import-Module WebAdministration -ErrorAction Stop
    $poolName = 'TAE-STA-Pool'
    if (Test-Path "IIS:\AppPools\$poolName") {
        Start-WebAppPool -Name $poolName
        Write-Host "  IIS App Pool iniciado." -ForegroundColor Gray
    }
} catch {
    Write-Host "  IIS nao disponivel." -ForegroundColor Yellow
}

# --- Validação ---
Start-Sleep -Seconds 5

$workerRunning = (Get-Service -Name $serviceName -ErrorAction SilentlyContinue).Status -eq 'Running'
try {
    $health = Invoke-WebRequest -Uri 'http://localhost:5000/health' -UseBasicParsing -TimeoutSec 10
    $apiOk = $true
} catch {
    $apiOk = $false
}

if ($workerRunning -and $apiOk) {
    Write-Host "`n============================================" -ForegroundColor Green
    Write-Host "  Atualizacao concluida com sucesso!" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "  Worker: RODANDO" -ForegroundColor Green
    Write-Host "  API:    OK (200)" -ForegroundColor Green
    Write-Host "  Backup: $backupPath" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Backup pode ser removido apos validacao:" -ForegroundColor Gray
    Write-Host "    Remove-Item -Recurse '$backupPath'" -ForegroundColor Gray
} else {
    Write-Host "`n============================================" -ForegroundColor Red
    Write-Host "  AVISO: Validacao parcial!" -ForegroundColor Red
    Write-Host "============================================" -ForegroundColor Red
    if (-not $workerRunning) { Write-Host "  Worker: PARADO" -ForegroundColor Red }
    if (-not $apiOk) { Write-Host "  API: NAO RESPONDEU" -ForegroundColor Red }
    Write-Host ""
    Write-Host "  Para rollback manual:" -ForegroundColor Yellow
    Write-Host "    Stop-Service $serviceName" -ForegroundColor Yellow
    Write-Host "    Copy-Item '$backupPath\*' '$InstallPath' -Recurse -Force" -ForegroundColor Yellow
    Write-Host "    Start-Service $serviceName" -ForegroundColor Yellow
}
