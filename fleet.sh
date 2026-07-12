#!/usr/bin/env bash
# fleet.sh — unified control surface for the local Cabinet fleet.
#
# ONE entry point replacing the scattered scripts (start-cad.ps1, start-doorstar.ps1,
# federation-watcher.sh, manual tmux commands). Runs from Git Bash, where tmux + the
# `claude` CLI are available.
#
# Usage:
#   ./fleet.sh up                 # start both knowledge islands (CAD 13457, Doorstar 13458)
#   ./fleet.sh down               # stop islands + kill all fleet tmux sessions
#   ./fleet.sh status             # island /health + live terminal sessions
#   ./fleet.sh wake <terminal>    # spawn a claude agent session for a terminal, tell it to work its inbox
#   ./fleet.sh send <terminal> <text...>   # send a line to a terminal's session
#   ./fleet.sh sleep <terminal>   # kill a terminal's session
#   ./fleet.sh ls                 # list terminals
set -uo pipefail

# ── Config ────────────────────────────────────────────────────────────────────
WORKSPACE="/c/Users/szant/Documents/Development/Cabinet_bilder_scripts"
NEXUS_DIST="/c/Users/szant/Documents/Development/nexus-core/src/nexus-core/knowledge-service/dist/server.js"
ISLANDS_DIR="$WORKSPACE/islands"
TERMINALS_DIR="$WORKSPACE/terminals"
TMUX_SOCKET="/tmp/spaceos.tmux"
SESSION_PREFIX="cab-"        # tmux sessions are cab-<terminal>
DEFAULT_MODEL="sonnet"

TERMINALS="root conductor architect librarian explorer backend frontend designer monitor"
tm() { tmux -S "$TMUX_SOCKET" "$@"; }

# Convert C:/x or C:\x to /c/x for tmux -c (MSYS needs POSIX paths)
posix_path() { echo "$1" | sed -E 's#^([A-Za-z]):[\\/]#/\L\1/#; s#\\#/#g'; }

# ── Islands ───────────────────────────────────────────────────────────────────
island_up() { # $1 = cad|doorstar   $2 = port
  local name="$1" port="$2" dir="$ISLANDS_DIR/$1"
  if curl -s -m 2 -o /dev/null "http://localhost:$port/health" 2>/dev/null; then
    echo "  island $name already up on $port"; return
  fi
  echo "  starting island $name (port $port) from shared nexus dist..."
  ( cd "$dir" && nohup node "$NEXUS_DIST" > "$dir/island.log" 2>&1 & )
}

cmd_up() {
  [ -f "$NEXUS_DIST" ] || { echo "ERROR: nexus build missing at $NEXUS_DIST — run 'npm run build' in knowledge-service first."; exit 1; }
  echo "Bringing fleet islands up:"
  island_up cad 13457
  island_up doorstar 13458
  echo "Islands launching (indexing ~1min on first boot). Check: ./fleet.sh status"
}

cmd_down() {
  echo "Stopping islands + fleet sessions..."
  for port in 13457 13458; do
    local pid
    pid=$(netstat -ano 2>/dev/null | grep -E "LISTENING" | grep ":$port " | awk '{print $NF}' | head -1)
    [ -n "${pid:-}" ] && { taskkill //PID "$pid" //F >/dev/null 2>&1 && echo "  stopped island on $port (PID $pid)"; }
  done
  for t in $TERMINALS; do tm kill-session -t "${SESSION_PREFIX}${t}" 2>/dev/null && echo "  killed session $t"; done
  echo "done."
}

# ── Terminals (tmux-based local wake) ─────────────────────────────────────────
cmd_wake() {
  local t="${1:?usage: wake <terminal> [model]}" model="${2:-$DEFAULT_MODEL}"
  local sess="${SESSION_PREFIX}${t}" wd; wd="$(posix_path "$TERMINALS_DIR/$t")"
  [ -d "$wd" ] || { echo "unknown terminal: $t"; exit 1; }
  if tm has-session -t "$sess" 2>/dev/null; then echo "$t already awake ($sess)"; return; fi
  echo "waking $t (model $model) in $wd ..."
  tm new-session -d -s "$sess" -c "$wd"
  sleep 1
  tm send-keys -t "$sess" -l "claude --model $model"
  tm send-keys -t "$sess" Enter
  sleep 4   # let claude initialize
  tm send-keys -t "$sess" -l "Olvasd el az inbox/ mappát ebben a könyvtárban, és dolgozd fel a beérkezett feladatokat a legmagasabb prioritással kezdve. A kész munkát az outbox/-ba tedd."
  tm send-keys -t "$sess" Enter
  echo "  $t awake. Attach with: tmux -S $TMUX_SOCKET attach -t $sess"
}

cmd_send() {
  local t="${1:?usage: send <terminal> <text>}"; shift
  local sess="${SESSION_PREFIX}${t}"
  tm has-session -t "$sess" 2>/dev/null || { echo "$t is asleep — ./fleet.sh wake $t first"; exit 1; }
  tm send-keys -t "$sess" -l "$*"; tm send-keys -t "$sess" Enter
  echo "sent to $t."
}

cmd_sleep() {
  local t="${1:?usage: sleep <terminal>}"; tm kill-session -t "${SESSION_PREFIX}${t}" 2>/dev/null && echo "$t asleep." || echo "$t was not awake."
}

# ── Status ────────────────────────────────────────────────────────────────────
cmd_status() {
  echo "── Islands ──"
  for pair in "CAD:13457" "Doorstar:13458"; do
    local nm="${pair%%:*}" port="${pair##*:}"
    local h; h=$(curl -s -m 3 "http://localhost:$port/health" 2>/dev/null)
    if [ -n "$h" ]; then
      echo "  $nm ($port): $(echo "$h" | grep -oE '"collectionName":"[^"]*"|"documents":[0-9]+' | tr '\n' ' ')"
    else
      echo "  $nm ($port): offline"
    fi
  done
  echo "── Awake terminals ──"
  local awake; awake=$(tm list-sessions -F '#{session_name}' 2>/dev/null | grep "^${SESSION_PREFIX}" | sed "s/^${SESSION_PREFIX}//" | tr '\n' ' ')
  echo "  ${awake:-（none）}"
}

cmd_ls() { echo "Terminals: $TERMINALS"; }

# ── Dispatch ──────────────────────────────────────────────────────────────────
case "${1:-}" in
  up)     cmd_up ;;
  down)   cmd_down ;;
  status) cmd_status ;;
  wake)   shift; cmd_wake "$@" ;;
  send)   shift; cmd_send "$@" ;;
  sleep)  shift; cmd_sleep "$@" ;;
  ls)     cmd_ls ;;
  *) echo "Usage: ./fleet.sh {up|down|status|wake <t> [model]|send <t> <text>|sleep <t>|ls}"; exit 1 ;;
esac
