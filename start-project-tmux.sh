#!/bin/bash

# Generated Tmux Launch Script for cabinet_bilder_scripts
SESSION="cabinet_bilder_scripts"
ENGINE="claude"

# Check if first argument is 'agy'
if [ "$1" == "agy" ]; then
  ENGINE="agy"
fi

echo "=== SpaceOS Project Terminals starting in tmux ==="
echo "Project Path: C:/Users/szant/Documents/Development/Cabinet_bilder_scripts"
echo "Knowledge Service: C:/Users/szant/Documents/Development/knowledge-service-0.0.01"
echo "CLI Engine: $ENGINE"

# Kill existing session if running
tmux kill-session -t "$SESSION" 2>/dev/null

# Create a new session in the background with the first window (root)
tmux new-session -d -s "$SESSION" -n "root" -c "C:/Users/szant/Documents/Development/Cabinet_bilder_scripts/terminals/root"
tmux send-keys -t "$SESSION:root" "echo '=== SpaceOS ROOT Terminal ===' && $ENGINE" C-m

# 2nd window: Conductor Task Loop / Service
tmux new-window -t "$SESSION" -n "conductor" -c "C:/Users/szant/Documents/Development/Cabinet_bilder_scripts/terminals/conductor"
tmux send-keys -t "$SESSION:conductor" "echo '=== SpaceOS Conductor / Knowledge Service ===' && node \"C:/Users/szant/Documents/Development/knowledge-service-0.0.01/dist/server.js\"" C-m

# 3rd window: Backend Developer
tmux new-window -t "$SESSION" -n "backend" -c "C:/Users/szant/Documents/Development/Cabinet_bilder_scripts/terminals/backend"
tmux send-keys -t "$SESSION:backend" "echo '=== SpaceOS Backend Terminal ===' && $ENGINE" C-m

# 4th window: Frontend Developer
tmux new-window -t "$SESSION" -n "frontend" -c "C:/Users/szant/Documents/Development/Cabinet_bilder_scripts/terminals/frontend"
tmux send-keys -t "$SESSION:frontend" "echo '=== SpaceOS Frontend Terminal ===' && $ENGINE" C-m

# 5th window: Designer
tmux new-window -t "$SESSION" -n "designer" -c "C:/Users/szant/Documents/Development/Cabinet_bilder_scripts/terminals/designer"
tmux send-keys -t "$SESSION:designer" "echo '=== SpaceOS Designer Terminal ===' && $ENGINE" C-m

# 6th window: Architect
tmux new-window -t "$SESSION" -n "architect" -c "C:/Users/szant/Documents/Development/Cabinet_bilder_scripts/terminals/architect"
tmux send-keys -t "$SESSION:architect" "echo '=== SpaceOS Architect Terminal ===' && $ENGINE" C-m

# 7th window: Librarian
tmux new-window -t "$SESSION" -n "librarian" -c "C:/Users/szant/Documents/Development/Cabinet_bilder_scripts/terminals/librarian"
tmux send-keys -t "$SESSION:librarian" "echo '=== SpaceOS Librarian Terminal ===' && $ENGINE" C-m

# 8th window: Explorer
tmux new-window -t "$SESSION" -n "explorer" -c "C:/Users/szant/Documents/Development/Cabinet_bilder_scripts/terminals/explorer"
tmux send-keys -t "$SESSION:explorer" "echo '=== SpaceOS Explorer Terminal ===' && $ENGINE" C-m

# 9th window: Monitor
tmux new-window -t "$SESSION" -n "monitor" -c "C:/Users/szant/Documents/Development/Cabinet_bilder_scripts/terminals/monitor"
tmux send-keys -t "$SESSION:monitor" "echo '=== SpaceOS Monitor Terminal ===' && $ENGINE" C-m

# Select the first window
tmux select-window -t "$SESSION:root"

echo "Session started. Attach with: tmux attach -t $SESSION"
