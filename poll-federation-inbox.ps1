# Federation inbox poller - Cabinet_bilder sziget
#
# A VPS hubon levo cabinet-bridge inboxot pollozza a hidon at (HTTPS + Bearer),
# az uj (UNREAD) uzeneteket behuzza a lokalis root terminal inboxaba, es
# minden lepest a logs/federation-poll.log fajlba naplo.
#
# [FILE-TRANSFER] uzenetek: base64 szeletek gyujtese, osszerakas,
# sha256 ellenorzes, mentes a terminals/root/inbox/files/ ala.
#
# Hasznalat:
#   .\poll-federation-inbox.ps1 -Once              # egyszeri lekerdezes
#   .\poll-federation-inbox.ps1                    # folyamatos, 120 mp-enkent
#   .\poll-federation-inbox.ps1 -IntervalSeconds 60

param(
    [int]$IntervalSeconds = 120,
    [switch]$Once
)

$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$localInbox  = Join-Path $projectRoot 'terminals\root\inbox'
$logFile     = Join-Path $projectRoot 'logs\federation-poll.log'
$fileStaging = Join-Path $projectRoot 'scratch\fed-files'
$filesDir    = Join-Path $projectRoot 'terminals\root\inbox\files'

# A token es URL egyetlen forrasa a root terminal .mcp.json-ja (spaceos-knowledge-vps)
$mcpConfig = Get-Content (Join-Path $projectRoot 'terminals\root\.mcp.json') -Raw | ConvertFrom-Json
$vpsUrl    = $mcpConfig.mcpServers.'spaceos-knowledge-vps'.url
$authHdr   = $mcpConfig.mcpServers.'spaceos-knowledge-vps'.headers.Authorization

function Write-Log([string]$msg) {
    $line = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') $msg"
    Add-Content -Path $logFile -Value $line -Encoding utf8
    Write-Host $line
}

function Invoke-BridgeTool([string]$tool, [hashtable]$arguments) {
    $body = @{
        jsonrpc = '2.0'
        id      = 1
        method  = 'tools/call'
        params  = @{ name = $tool; arguments = $arguments }
    } | ConvertTo-Json -Depth 6
    # Explicit UTF-8 bajtok, kulonben az ekezetes tartalom serul (VPS jelezte encoding-hibat)
    $bodyBytes = [Text.Encoding]::UTF8.GetBytes($body)
    $resp = Invoke-RestMethod -Uri $vpsUrl -Method Post -Body $bodyBytes `
        -ContentType 'application/json; charset=utf-8' `
        -Headers @{ Authorization = $authHdr } -TimeoutSec 60
    if ($resp.error) { throw "Bridge hiba ($tool): $($resp.error.message)" }
    return $resp.result.content[0].text | ConvertFrom-Json
}

function Process-FileTransfer([string]$content, [string]$msgId) {
    $split = $content -split "`n", 2
    $header = $split[0]
    $b64 = $split[1].Trim()

    $meta = @{}
    $headerBody = $header -replace '^\[FILE-TRANSFER\]\s*', ''
    foreach ($kv in ($headerBody -split ';')) {
        $pair = $kv -split '=', 2
        if ($pair.Count -eq 2) { $meta[$pair[0].Trim()] = $pair[1].Trim() }
    }
    $name = $meta['name']
    $sha = $meta['sha256']
    $parts = $meta['part'] -split '/'
    $partNum = [int]$parts[0]
    $partTotal = [int]$parts[1]

    New-Item -ItemType Directory -Force -Path $fileStaging | Out-Null
    $stagePrefix = Join-Path $fileStaging $sha
    Set-Content -Path ($stagePrefix + '.part' + $partNum) -Value $b64 -Encoding ascii
    Write-Log "fajl-szelet: $name part $partNum/$partTotal ($msgId)"

    $have = @(Get-ChildItem ($stagePrefix + '.part*')).Count
    if ($have -lt $partTotal) { return }

    # Minden szelet megvan: osszerakas sorrendben, dekodolas, hash-ellenorzes
    $allB64 = ''
    for ($p = 1; $p -le $partTotal; $p++) {
        $allB64 += (Get-Content ($stagePrefix + '.part' + $p) -Raw).Trim()
    }
    $bytes = [Convert]::FromBase64String($allB64)
    New-Item -ItemType Directory -Force -Path $filesDir | Out-Null
    $outPath = Join-Path $filesDir $name
    [IO.File]::WriteAllBytes($outPath, $bytes)

    $gotSha = (Get-FileHash -Path $outPath -Algorithm SHA256).Hash.ToLower()
    if ($gotSha -eq $sha) {
        Write-Log "fajl KESZ es HITELES: $outPath ($($bytes.Length) byte, sha256 OK)"
        Remove-Item ($stagePrefix + '.part*') -Force
    } else {
        Write-Log "HIBA: sha256 ELTERES: $name (vart=$sha kapott=$gotSha) - a fajl NEM megbizhato!"
    }
}

function Poll-Once {
    $inbox = Invoke-BridgeTool 'list_inbox' @{ terminal = 'cabinet-bridge'; status = 'UNREAD' }
    if ($inbox.count -eq 0) {
        Write-Log "poll: nincs uj uzenet"
        return
    }
    Write-Log "poll: $($inbox.count) uj uzenet a cabinet-bridge inboxban"
    foreach ($m in $inbox.messages) {
        $msgId = $m.frontmatter.id
        try {
            $full = Invoke-BridgeTool 'read_inbox_message' @{ terminal = 'cabinet-bridge'; message_id = $msgId }
            $msgContent = $full.message.content
            if ($msgContent -and $msgContent.StartsWith('[FILE-TRANSFER]')) {
                Process-FileTransfer $msgContent $msgId
            } else {
                $safeId = $msgId -replace '[^A-Za-z0-9\-]', '_'
                $file = Join-Path $localInbox "$(Get-Date -Format 'yyyy-MM-dd')_FED_$safeId.md"
                $header = "# [FEDERATION] $msgId`n`n> Forras: VPS hub (datahaven.joinerytech.hu), cabinet-bridge inbox`n> Behuzva: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`n`n---`n"
                $header + ($full | ConvertTo-Json -Depth 6) | Out-File -FilePath $file -Encoding utf8
                Write-Log "behuzva: $msgId -> $file"
            }
        } catch {
            Write-Log "HIBA (${msgId}): $($_.Exception.Message)"
        }
    }
}

Write-Log "poller indul (url=$vpsUrl, interval=$IntervalSeconds s, once=$Once)"
if ($Once) {
    Poll-Once
} else {
    while ($true) {
        try { Poll-Once } catch { Write-Log "HIBA (poll): $($_.Exception.Message)" }
        Start-Sleep -Seconds $IntervalSeconds
    }
}
