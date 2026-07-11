# Windows Terminal Launch Script for cabinet_bilder_scripts
# Auto-generates the 9-terminal woodworking fleet layout in a single Windows Terminal window.

$engine = "agy"
if ($args[0] -eq "claude") {
    $engine = "claude"
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrEmpty($scriptDir)) {
    $scriptDir = Get-Location
}

# Resolve knowledge-service directory dynamically (one level up, next to this directory)
$knowledgeServiceDir = [System.IO.Path]::GetFullPath((Join-Path $scriptDir "..\knowledge-service-0.0.01"))
$serverJs = Join-Path $knowledgeServiceDir "dist\server.js"

Write-Host "=== SpaceOS Project Terminals starting in Windows Terminal ==="
Write-Host "Project Path: $scriptDir"
Write-Host "Knowledge Service: $knowledgeServiceDir"
Write-Host "CLI Engine: $engine"

# Define the terminals and their actions
# Format: @{ Title = "name"; Path = "rel_path"; Cmd = "command" }
$terminals = @(
    @{ Title = "root"; Path = "terminals\root"; Cmd = "powershell -File '$scriptDir\watch-terminal.ps1' -TerminalName 'root'" },
    @{ Title = "conductor"; Path = "terminals\conductor"; Cmd = "echo '=== SpaceOS Conductor / Knowledge Service ===' \; cd '$knowledgeServiceDir' \; node '$serverJs'" },
    @{ Title = "conductor-agent"; Path = "terminals\conductor"; Cmd = "powershell -File '$scriptDir\watch-terminal.ps1' -TerminalName 'conductor'" },
    @{ Title = "backend"; Path = "terminals\backend"; Cmd = "powershell -File '$scriptDir\watch-terminal.ps1' -TerminalName 'backend'" },
    @{ Title = "frontend"; Path = "terminals\frontend"; Cmd = "powershell -File '$scriptDir\watch-terminal.ps1' -TerminalName 'frontend'" },
    @{ Title = "designer"; Path = "terminals\designer"; Cmd = "powershell -File '$scriptDir\watch-terminal.ps1' -TerminalName 'designer'" },
    @{ Title = "architect"; Path = "terminals\architect"; Cmd = "powershell -File '$scriptDir\watch-terminal.ps1' -TerminalName 'architect'" },
    @{ Title = "librarian"; Path = "terminals\librarian"; Cmd = "powershell -File '$scriptDir\watch-terminal.ps1' -TerminalName 'librarian'" },
    @{ Title = "explorer"; Path = "terminals\explorer"; Cmd = "powershell -File '$scriptDir\watch-terminal.ps1' -TerminalName 'explorer'" },
    @{ Title = "monitor"; Path = "terminals\monitor"; Cmd = "powershell -File '$scriptDir\watch-terminal.ps1' -TerminalName 'monitor'" }
)

# Build the 'wt' command line string
$wtArgs = ""
for ($i = 0; $i -lt $terminals.Length; $i++) {
    $t = $terminals[$i]
    $fullPath = Join-Path $scriptDir $t.Path
    $runCmd = $t.Cmd
    
    # We use PowerShell for the terminal profile
    $tabCmd = "powershell -NoExit -Command `"$runCmd`""
    
    if ($i -eq 0) {
        $wtArgs = "new-tab -d `"$fullPath`" --title `"$($t.Title)`" $tabCmd"
    } else {
        $wtArgs += " ; new-tab -d `"$fullPath`" --title `"$($t.Title)`" $tabCmd"
    }
}

# Launch the terminals!
Start-Process wt -ArgumentList $wtArgs
Write-Host "Windows Terminal launched successfully with 9 tabs."

