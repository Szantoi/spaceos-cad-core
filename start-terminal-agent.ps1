param(
    [string]$engine = "agy"
)

# 1. Print header
$folderName = (Get-Item .).Name.ToUpper()
Write-Host "=== SpaceOS $folderName Terminal Agent ===" -ForegroundColor Cyan

# 2. Check for UNREAD inbox tasks
$inboxTask = Get-ChildItem -Path "inbox/*.md" -ErrorAction SilentlyContinue | Where-Object {
    (Get-Content $_.FullName -Raw) -match 'status:\s*UNREAD'
} | Select-Object -First 1

if ($inboxTask) {
    Write-Host "Found UNREAD task: $($inboxTask.Name)" -ForegroundColor Yellow
    
    # Get the raw text of the task file
    $taskText = Get-Content $inboxTask.FullName -Raw
    
    # Run the engine in task execution mode
    Write-Host "Starting agent in task execution mode..." -ForegroundColor Green
    & $engine --dangerously-skip-permissions $taskText
} else {
    Write-Host "No pending tasks found. Starting agent in interactive mode..." -ForegroundColor Gray
    & $engine --dangerously-skip-permissions
}
