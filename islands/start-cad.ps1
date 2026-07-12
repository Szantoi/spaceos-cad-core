$ErrorActionPreference = 'Stop'
$IslandDir = Join-Path $PSScriptRoot 'cad'
$NexusDist = 'C:/Users/szant/Documents/Development/nexus-core/src/nexus-core/knowledge-service/dist/server.js'
if (-not (Test-Path $NexusDist)) {
    Write-Error 'nexus-core build not found - run npm run build in the knowledge-service dir first.'
    exit 1
}
Write-Host 'Starting CAD island on port 13457 (collection cabinetbilder-cad) from shared nexus-core dist'
Set-Location $IslandDir
node $NexusDist
