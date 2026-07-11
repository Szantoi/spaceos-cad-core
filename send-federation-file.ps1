# Federation fájlküldő — Cabinet_bilder sziget
#
# Fájlt küld a VPS hubra a federációs hídon át (send_message + base64).
# Konvenció: minden üzenet content-je egy fejléc-sorral kezdődik:
#   [FILE-TRANSFER] name=<fájlnév>; part=<n>/<össz>; sha256=<teljes fájl hash>; encoding=base64
# majd új sorban a base64 szelet. A fogadó oldal a részekből összerakja,
# és a sha256-tal ellenőrzi. Szeletméret: 64 KB nyers adat (~85 KB base64),
# mert a szerver JSON body limitje ~100 KB.
#
# Használat:
#   .\send-federation-file.ps1 -Path .\szabasterv.pdf                # címzett: root (VPS)
#   .\send-federation-file.ps1 -Path .\terv.dxf -To conductor -Note "Jóváhagyásra"

param(
    [Parameter(Mandatory=$true)][string]$Path,
    [string]$To = 'root',
    [string]$Note = '',
    [int]$ChunkBytes = 65536
)

$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$logFile = Join-Path $projectRoot 'logs\federation-poll.log'

$mcpConfig = Get-Content (Join-Path $projectRoot 'terminals\root\.mcp.json') -Raw | ConvertFrom-Json
$vpsUrl  = $mcpConfig.mcpServers.'spaceos-knowledge-vps'.url
$authHdr = $mcpConfig.mcpServers.'spaceos-knowledge-vps'.headers.Authorization

function Write-Log([string]$msg) {
    $line = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') $msg"
    Add-Content -Path $logFile -Value $line -Encoding utf8
    Write-Host $line
}

$file = Get-Item $Path
$bytes = [IO.File]::ReadAllBytes($file.FullName)
$sha256 = (Get-FileHash -Path $file.FullName -Algorithm SHA256).Hash.ToLower()

# A TELJES fajl base64-et daraboljuk (nem a bajtokat!), kulonben a kozbenso
# szeletek vegen padding '=' keletkezne, ami osszefuzeskor ervenytelen.
$b64full = [Convert]::ToBase64String($bytes)
$chunkChars = [Math]::Floor($ChunkBytes * 4 / 3)
$total = [Math]::Ceiling($b64full.Length / $chunkChars)

Write-Log "fajlkuldes indul: $($file.Name) ($($bytes.Length) byte, $total resz, to=$To)"

for ($i = 0; $i -lt $total; $i++) {
    $offset = $i * $chunkChars
    $len = [Math]::Min($chunkChars, $b64full.Length - $offset)
    $b64 = $b64full.Substring($offset, $len)

    $header = "[FILE-TRANSFER] name=$($file.Name); part=$($i+1)/$total; sha256=$sha256; encoding=base64"
    if ($Note -and $i -eq 0) { $header += "; note=$Note" }
    $content = "$header`n$b64"

    $body = @{
        jsonrpc = '2.0'; id = 1; method = 'tools/call'
        params = @{ name = 'send_message'; arguments = @{ to = $To; type = 'info'; content = $content } }
    } | ConvertTo-Json -Depth 6

    $bodyBytes = [Text.Encoding]::UTF8.GetBytes($body)
    $resp = Invoke-RestMethod -Uri $vpsUrl -Method Post -Body $bodyBytes `
        -ContentType 'application/json; charset=utf-8' `
        -Headers @{ Authorization = $authHdr } -TimeoutSec 120
    if ($resp.error) { throw "Bridge hiba (part $($i+1)/$total): $($resp.error.message)" }
    $result = $resp.result.content[0].text | ConvertFrom-Json
    Write-Log "elkuldve: $($file.Name) part $($i+1)/$total -> $($result.id)"
}

Write-Log "fajlkuldes KESZ: $($file.Name) (sha256=$sha256)"
