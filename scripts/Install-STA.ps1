#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Instalador padronizado do TAE-STA (Transferência de Arquivos Enterprise).

.DESCRIPTION
    Instala API, Worker e Frontend em ambiente Windows.
    Garante paridade entre Dev/HML/Prod.

.PARAMETER Environment
    Ambiente de destino: Dev, Hml ou Prod.

.PARAMETER InstallPath
    Pasta raiz de instalação. Padrão: C:\TAE-STA

.PARAMETER DbHost
    Host do PostgreSQL.

.PARAMETER DbName
    Nome do banco de dados. Padrão: sta

.PARAMETER DbUser
    Usuário do banco.

.PARAMETER DbPassword
    Senha do banco (SecureString).

.PARAMETER JwtSecret
    Chave JWT (mínimo 32 caracteres). Se vazio, gera aleatória.

.PARAMETER AdminPassword
    Senha do usuário admin inicial.

.PARAMETER FrontendUrl
    URL do frontend (pra CORS). Padrão: http://localhost:3000

.PARAMETER SkipIIS
    Não configura IIS.

.PARAMETER SkipMigrations
    Não roda migrations.

.EXAMPLE
    .\Install-STA.ps1 -Environment Prod -DbHost db.empresa.com -DbUser sta_user
#>
param(
    [ValidateSet('Dev','Hml','Prod')]
    [string]$Environment,

    [string]$InstallPath = 'C:\TAE-STA',

    [string]$DbHost,
    [string]$DbName,
    [string]$DbUser,
    [SecureString]$DbPassword,

    [string]$JwtSecret,
    [SecureString]$AdminPassword,
    [string]$FrontendUrl = 'http://localhost:3000',

    [switch]$SkipIIS,
    [switch]$SkipMigrations
)

$ErrorActionPreference = 'Stop'

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "  TAE-STA - Instalador Padronizado" -ForegroundColor Green
Write-Host "  Transferencia de Arquivos Enterprise" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""

# --- 1. Coletar parâmetros faltantes ---
if (-not $Environment) {
    $Environment = Read-Host "Ambiente (Dev/Hml/Prod)"
}

if (-not $DbHost) {
    $DbHost = Read-Host "Host do PostgreSQL (ex: localhost)"
}

if (-not $DbName) {
    $DbName = Read-Host "Nome do banco de dados (ex: sta)"
}

if (-not $DbUser) {
    $DbUser = Read-Host "Usuario do banco (ex: sta_user)"
}

if (-not $DbPassword) {
    $DbPassword = Read-Host "Senha do banco" -AsSecureString
}

if (-not $JwtSecret -or $JwtSecret.Length -lt 32) {
    $JwtSecret = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 40 | ForEach-Object { [char]$_ })
    Write-Host "JWT Secret gerada automaticamente." -ForegroundColor Yellow
}

if (-not $AdminPassword) {
    $AdminPassword = Read-Host "Senha do admin inicial" -AsSecureString
}

$DbPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($DbPassword))
$AdminPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($AdminPassword))

# --- 2. Criar estrutura de pastas ---
Write-Host "`n[1/8] Criando estrutura de pastas..." -ForegroundColor Cyan
$paths = @(
    "$InstallPath\api",
    "$InstallPath\worker",
    "$InstallPath\web",
    "$InstallPath\logs",
    "$InstallPath\config"
)
foreach ($p in $paths) {
    New-Item -ItemType Directory -Path $p -Force | Out-Null
}
Write-Host "  Estrutura criada em $InstallPath" -ForegroundColor Gray

# --- 3. Verificar binários ---
Write-Host "`n[2/8] Verificando binários..." -ForegroundColor Cyan
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceApi = Join-Path $scriptDir '..\publish\api'
$sourceWorker = Join-Path $scriptDir '..\publish\worker'
$sourceWeb = Join-Path $scriptDir '..\publish\web'

if (-not (Test-Path $sourceApi)) {
    Write-Host "  ERRO: Pasta publish/api nao encontrada." -ForegroundColor Red
    Write-Host "  Execute 'dotnet publish' antes ou baixe os artifacts do CI." -ForegroundColor Red
    exit 1
}

# --- 4. Copiar binários ---
Write-Host "`n[3/8] Copiando binarios..." -ForegroundColor Cyan
Copy-Item -Path "$sourceApi\*" -Destination "$InstallPath\api" -Recurse -Force
Write-Host "  API copiada." -ForegroundColor Gray

Copy-Item -Path "$sourceWorker\*" -Destination "$InstallPath\worker" -Recurse -Force
Write-Host "  Worker copiado." -ForegroundColor Gray

if (Test-Path $sourceWeb) {
    Copy-Item -Path "$sourceWeb\*" -Destination "$InstallPath\web" -Recurse -Force
    Write-Host "  Frontend copiado." -ForegroundColor Gray
}

# --- 5. Gerar configuração ---
Write-Host "`n[4/8] Gerando configuracao ($Environment)..." -ForegroundColor Cyan

$logLevel = switch ($Environment) {
    'Dev'  { 'Debug' }
    'Hml'  { 'Information' }
    'Prod' { 'Warning' }
}

$ldapEnabled = if ($Environment -eq 'Dev') { 'false' } else { 'true' }

$config = @{
    ConnectionStrings = @{
        StaDb = "Host=$DbHost;Port=5432;Database=$DbName;Username=$DbUser;Password=$DbPasswordPlain;Pooling=true;Maximum Pool Size=10;Command Timeout=120"
    }
    Jwt = @{
        Secret = $JwtSecret
        Issuer = 'STA.Api'
        Audience = 'STA.Client'
        ExpirationHours = 8
    }
    Ldap = @{
        Enabled = $ldapEnabled
        Server = ''
        BaseDn = ''
        Domain = ''
    }
    StaSettings = @{
        NomeSistema = 'STA'
        CnProcesso = 1
        Arquivo7Zip = 'C:\Program Files\7-Zip\7z.exe'
        TimeoutCompactacaoMs = 1800000
        SobreEscreverArquivos = $true
        QtdDiasExcluirLog = 5
        GeraLogSucessoBancoDados = $true
        UseXmlFallback = $false
    }
    AllowedOrigins = @($FrontendUrl)
    Logging = @{
        LogLevel = @{
            Default = $logLevel
            'Microsoft.AspNetCore' = 'Warning'
        }
    }
} | ConvertTo-Json -Depth 4

$configPath = "$InstallPath\config\appsettings.json"
$config | Out-File -FilePath $configPath -Encoding utf8
Write-Host "  Configuracao salva em $configPath" -ForegroundColor Gray

# Copiar config pra API e Worker
Copy-Item $configPath "$InstallPath\api\appsettings.Production.json" -Force
Copy-Item $configPath "$InstallPath\worker\appsettings.Production.json" -Force

# --- 6. Aplicar migrations ---
if (-not $SkipMigrations) {
    Write-Host "`n[5/8] Aplicando migrations..." -ForegroundColor Cyan
    $env:STA_DB_CONN = "Host=$DbHost;Port=5432;Database=$DbName;Username=$DbUser;Password=$DbPasswordPlain"
    try {
        Push-Location "$InstallPath\worker"
        & .\STA.Worker.exe --migrate 2>&1 | Out-Null
        # Fallback: usar dotnet ef se disponível
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  Tentando via dotnet ef..." -ForegroundColor Yellow
            dotnet ef database update --connection $env:STA_DB_CONN
        }
        Pop-Location
        Write-Host "  Migrations aplicadas." -ForegroundColor Gray
    } catch {
        Write-Host "  AVISO: Migrations falharam. Aplique manualmente." -ForegroundColor Yellow
        Write-Host "  $($_.Exception.Message)" -ForegroundColor Yellow
        Pop-Location
    }
} else {
    Write-Host "`n[5/8] Migrations puladas (SkipMigrations)." -ForegroundColor Yellow
}

# --- 7. Registrar Worker como Windows Service ---
Write-Host "`n[6/8] Registrando Worker como servico Windows..." -ForegroundColor Cyan
$serviceName = 'TAE-STA-Worker'
$serviceExe = "$InstallPath\worker\STA.Worker.exe"

$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "  Servico ja existe. Parando..." -ForegroundColor Yellow
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    sc.exe delete $serviceName | Out-Null
    Start-Sleep -Seconds 2
}

sc.exe create $serviceName binPath= "`"$serviceExe`"" start= auto displayname= "TAE-STA Worker - Transferencia de Arquivos" | Out-Null
sc.exe description $serviceName "Servico de transferencia automatica de arquivos (TAE-STA)" | Out-Null

# Configurar variáveis de ambiente pro serviço
$regPath = "HKLM:\SYSTEM\CurrentControlSet\Services\$serviceName"
New-ItemProperty -Path $regPath -Name 'Environment' -PropertyType MultiString -Value @(
    "ASPNETCORE_ENVIRONMENT=$Environment",
    "STA_DB_CONN=Host=$DbHost;Port=5432;Database=$DbName;Username=$DbUser;Password=$DbPasswordPlain"
) -Force | Out-Null

Start-Service -Name $serviceName
Write-Host "  Servico '$serviceName' registrado e iniciado." -ForegroundColor Gray

# --- 8. Configurar IIS ---
if (-not $SkipIIS) {
    Write-Host "`n[7/8] Configurando IIS..." -ForegroundColor Cyan

    Import-Module WebAdministration -ErrorAction SilentlyContinue
    if (-not (Get-Module WebAdministration)) {
        Write-Host "  AVISO: Modulo WebAdministration nao encontrado. Pulando IIS." -ForegroundColor Yellow
        Write-Host "  Instale IIS e o modulo de administracao." -ForegroundColor Yellow
    } else {
        # App Pool
        $poolName = 'TAE-STA-Pool'
        if (-not (Test-Path "IIS:\AppPools\$poolName")) {
            New-WebAppPool -Name $poolName | Out-Null
            Set-ItemProperty "IIS:\AppPools\$poolName" -Name managedRuntimeVersion -Value ''
            Set-ItemProperty "IIS:\AppPools\$poolName" -Name processModel.identityType -Value 'ApplicationPoolIdentity'
        }

        # Site API
        $apiSiteName = 'TAE-STA-API'
        if (-not (Get-Website -Name $apiSiteName -ErrorAction SilentlyContinue)) {
            New-Website -Name $apiSiteName -PhysicalPath "$InstallPath\api" -Port 5000 -ApplicationPool $poolName | Out-Null
        }

        # Site Frontend
        $webSiteName = 'TAE-STA-Web'
        if (-not (Get-Website -Name $webSiteName -ErrorAction SilentlyContinue)) {
            New-Website -Name $webSiteName -PhysicalPath "$InstallPath\web" -Port 3000 -ApplicationPool $poolName | Out-Null
        }

        Write-Host "  IIS configurado: API (porta 5000), Web (porta 3000)." -ForegroundColor Gray
    }
} else {
    Write-Host "`n[7/8] IIS pulado (SkipIIS)." -ForegroundColor Yellow
}

# --- 9. Validação ---
Write-Host "`n[8/8] Validando instalacao..." -ForegroundColor Cyan
Start-Sleep -Seconds 3

$workerRunning = (Get-Service -Name $serviceName -ErrorAction SilentlyContinue).Status -eq 'Running'

if ($workerRunning) {
    Write-Host "  Worker: RODANDO" -ForegroundColor Green
} else {
    Write-Host "  Worker: PARADO (verifique logs em $InstallPath\logs)" -ForegroundColor Red
}

try {
    $health = Invoke-WebRequest -Uri 'http://localhost:5000/health' -UseBasicParsing -TimeoutSec 5
    Write-Host "  API: OK ($($health.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "  API: NAO RESPONDEU (pode demorar alguns segundos pra iniciar)" -ForegroundColor Yellow
}

# --- Resumo ---
Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "  Instalacao concluida!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Ambiente:    $Environment" -ForegroundColor White
Write-Host "  Pasta:       $InstallPath" -ForegroundColor White
Write-Host "  API:         http://localhost:5000" -ForegroundColor White
Write-Host "  Frontend:    $FrontendUrl" -ForegroundColor White
Write-Host "  Worker:      Servico '$serviceName'" -ForegroundColor White
Write-Host "  Config:      $configPath" -ForegroundColor White
Write-Host ""
Write-Host "  Proximos passos:" -ForegroundColor Gray
Write-Host "    1. Configurar LDAP em $configPath (se aplicavel)" -ForegroundColor Gray
Write-Host "    2. Acessar $FrontendUrl e fazer login" -ForegroundColor Gray
Write-Host "    3. Criar primeira transferencia" -ForegroundColor Gray
Write-Host ""
