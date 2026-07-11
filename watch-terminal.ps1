param (
    [string]$TerminalName,
    [int]$MaxRetries = 3
)

# SpaceOS Terminal Watcher v2 (2026-07-07)
# Valtozasok az v1-hez kepest:
#  - Egyszerusitett prompt: a task szovege KOZVETLENUL a promptban van (nem kell fetch_task),
#    a submit_done hivasa hangsulyozott es kotelezo zaras.
#  - Retry-limit: max $MaxRetries probalkozas / uzenet, utana a fajl status: STALLED-ra
#    valt es a watcher nem veszi fel tobbe (nincs vegtelen ujrainditas).
#  - Az agent teljes kimenete logfajlba is kerul: logs/agent-<terminal>-<msgid>-<n>.log

Clear-Host
Write-Host "=================================================="
Write-Host "   SpaceOS Terminal Watcher v2: $TerminalName"
Write-Host "=================================================="
Write-Host "Watching inbox for new UNREAD tasks (max $MaxRetries retries)..."
Write-Host ""

$inboxPath = "inbox"
$logDir = Join-Path (Get-Location) "..\..\logs"
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Force -Path $logDir | Out-Null }
$attempts = @{}

while ($true) {
    if (Test-Path $inboxPath) {
        $unreadFiles = Get-ChildItem -Path $inboxPath -Filter "*.md" -ErrorAction SilentlyContinue | Where-Object {
            try {
                $content = Get-Content $_.FullName -Raw -ErrorAction SilentlyContinue
                $content -match "(?m)^status:\s*UNREAD"
            } catch { $false }
        }

        if ($unreadFiles) {
            $file = $unreadFiles[0]
            $raw = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue

            if ($raw -match "(?m)^id:\s*([^\r\n]+)") {
                $msgId = $Matches[1].Trim()

                if (-not $attempts.ContainsKey($msgId)) { $attempts[$msgId] = 0 }
                $attempts[$msgId]++

                if ($attempts[$msgId] -gt $MaxRetries) {
                    Write-Host "!! $msgId elerte a retry-limitet ($MaxRetries) - STALLED-ra allitom" -ForegroundColor Red
                    $stalled = $raw -replace "(?m)^status:\s*UNREAD", "status: STALLED"
                    Set-Content -Path $file.FullName -Value $stalled -Encoding utf8
                    continue
                }

                $agentLog = Join-Path $logDir ("agent-" + $TerminalName + "-" + ($msgId -replace '[^A-Za-z0-9\-]','_') + "-" + $attempts[$msgId] + ".log")

                Write-Host "--------------------------------------------------"
                Write-Host "NEW TASK: $msgId (probalkozas: $($attempts[$msgId])/$MaxRetries)"
                Write-Host "File: $($file.Name)"
                Write-Host "Agent log: $agentLog"
                Write-Host "--------------------------------------------------"

                # A task torzse (frontmatter utani resz) kozvetlenul a promptba
                $taskBody = $raw -replace "(?s)^---.*?---\s*", ""

                $prompt = @"
Te a(z) '$TerminalName' terminal vagy a SpaceOS flottaban. Az alabbi feladatot kell vegrehajtanod (uzenet-azonosito: $msgId).

=== FELADAT ===
$taskBody
=== FELADAT VEGE ===

KAPCSOLATI SZABALYOK (KOTELEZO, megszeges = azonnali BLOCKED):
- KIZAROLAG a munkakonyvtarad .agents/mcp_config.json fajljaban konfiguralt 'spaceos-knowledge' MCP szervert hasznalhatod (localhost:13457). Ez a LOKAL sziget.
- TILOS: mas portot (kulonosen 3456!) vagy domain-t (datahaven.*) hivnod; sajat scriptet irnod ami HTTP-n MCP-t vagy /api vegpontot hiv; barhol talalt mas tokent kiprobalnod vagy hasznalnod. A 3456-os port egy MASIK, eles rendszer - oda dolgozni szigoruan tilos.
- ELSO LEPES: hivd meg a get_identity MCP toolt. Ha a valasz NEM '$TerminalName', allj le azonnal es zard a feladatot submit_done-nal (summary: "BLOCKED: identitas-elteres, kapott identitas: <nev>").

SZABALYOK:
1. Hajtsd vegre a feladatot pontosan, ne csinalj mast.
2. A LEGVEGEN KOTELEZO meghivnod a submit_done MCP toolt ezekkel a parameterekkel: terminal="$TerminalName", task_id="$msgId", es a summary mezoben rovid osszefoglaloval. ENELKUL A FELADAT NEM SZAMIT KESZNEK.
3. Ha a feladat nem vegezheto el, AKKOR IS hivd meg a submit_done-t, a summary-ban a BLOCKED szoval es az okkal.
"@

                & agy --dangerously-skip-permissions --print $prompt 2>&1 | Tee-Object -FilePath $agentLog

                # Lezaras-ellenorzes (v3, 2026-07-10): a submit_done utan a szerver csak
                # kesleltetve jeloli READ-re az inbox-taskot -> a watcher ujra felvenne
                # (duplikalt futas!). Ezert: ha az outboxban mar van DONE ehhez az
                # uzenethez (ref: msgId), a watcher MAGA allitja READ-re az inbox fajlt.
                $doneFound = Get-ChildItem -Path "outbox" -Filter "*.md" -ErrorAction SilentlyContinue | Where-Object {
                    try {
                        (Get-Content $_.FullName -Raw -ErrorAction SilentlyContinue) -match "(?m)^ref:\s*$([regex]::Escape($msgId))"
                    } catch { $false }
                }
                if ($doneFound) {
                    $rawNow = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
                    if ($rawNow -match "(?m)^status:\s*UNREAD") {
                        Set-Content -Path $file.FullName -Value ($rawNow -replace "(?m)^status:\s*UNREAD", "status: READ") -Encoding utf8
                    }
                    Write-Host "OK: $msgId lezarva (DONE az outboxban), inbox READ-re allitva" -ForegroundColor Green
                } else {
                    Write-Host "!! $msgId nincs lezarva (nincs DONE az outboxban) - retry kovetkezik ($($attempts[$msgId])/$MaxRetries)" -ForegroundColor Yellow
                }

                Write-Host ""
                Write-Host "Agent kilepett. Log: $agentLog"
                Start-Sleep -Seconds 5
            }
        }
    }
    Start-Sleep -Seconds 2
}
