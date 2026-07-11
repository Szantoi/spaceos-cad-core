# Federation tudas-beemelo - Cabinet_bilder sziget
#
# A hidon erkezett, JOVAHAGYOTT fajlokat beemeli a lokalis tudasbazisba
# es ujraindexeli a RAG-ot. A jovahagyas a governance szerint a
# root/conductor dontese - ez a szkript a jovahagyas UTANI lepes.
#
# Folyamat: terminals/root/inbox/files/<fajl>
#        -> <Development>/docs/knowledge/federation/<fajl>
#        -> POST /api/knowledge/index (lokal sziget, 13457)
#
# Hasznalat:
#   .\ingest-federation-knowledge.ps1 -File erp_sema.md      # egy fajl
#   .\ingest-federation-knowledge.ps1 -All                   # minden varakozo fajl

param(
    [string]$File,
    [switch]$All
)

$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$filesDir  = Join-Path $projectRoot 'terminals\root\inbox\files'
# A lokal sziget index-gyokere (knowledge-service .env KNOWLEDGE_BASE_PATH) ezzel azonos!
$knowledgeDir = Join-Path $projectRoot 'docs\knowledge\federation'
$logFile   = Join-Path $projectRoot 'logs\federation-poll.log'

$mcpConfig = Get-Content (Join-Path $projectRoot 'terminals\root\.mcp.json') -Raw | ConvertFrom-Json
$localUrl  = $mcpConfig.mcpServers.'spaceos-knowledge'.url -replace '/mcp/$', ''
$localAuth = $mcpConfig.mcpServers.'spaceos-knowledge'.headers.Authorization

function Write-Log([string]$msg) {
    $line = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') $msg"
    Add-Content -Path $logFile -Value $line -Encoding utf8
    Write-Host $line
}

if (-not $File -and -not $All) {
    Write-Host "Add meg: -File <nev> vagy -All. Varakozo fajlok:"
    if (Test-Path $filesDir) { Get-ChildItem $filesDir | ForEach-Object { Write-Host "  $($_.Name)" } }
    exit 0
}

$targets = @()
if ($All) {
    $targets = @(Get-ChildItem $filesDir -File)
} else {
    $targets = @(Get-Item (Join-Path $filesDir $File))
}

if ($targets.Count -eq 0) {
    Write-Log "ingest: nincs beemelendo fajl"
    exit 0
}

New-Item -ItemType Directory -Force -Path $knowledgeDir | Out-Null
foreach ($t in $targets) {
    $dest = Join-Path $knowledgeDir $t.Name
    Move-Item -Path $t.FullName -Destination $dest -Force
    Write-Log "ingest: $($t.Name) -> $dest"
}

# Ujraindexeles a lokal szigeten
try {
    $resp = Invoke-RestMethod -Uri "$localUrl/api/knowledge/index" -Method Post `
        -Headers @{ Authorization = $localAuth } -ContentType 'application/json; charset=utf-8' `
        -Body '{}' -TimeoutSec 300
    Write-Log "ingest: ujraindexeles kesz ($($targets.Count) uj fajl a tudasbazisban)"
} catch {
    Write-Log "ingest FIGYELEM: ujraindexeles nem sikerult: $($_.Exception.Message) - futtasd kezzel: POST $localUrl/api/knowledge/index"
}
