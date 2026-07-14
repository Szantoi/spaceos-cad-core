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
PROMPTS_DIR="$WORKSPACE/islands/prompts"   # externalized agent prompts (inspectable)
TERMINALS_DIR="$WORKSPACE/terminals"
TMUX_SOCKET="/tmp/spaceos.tmux"
SESSION_PREFIX="cab-"        # tmux sessions are cab-<terminal>
DEFAULT_MODEL="sonnet"

TERMINALS="root conductor architect librarian explorer backend frontend designer monitor"
tm() { tmux -S "$TMUX_SOCKET" "$@"; }

# Convert C:/x or C:\x to /c/x for tmux -c (MSYS needs POSIX paths)
posix_path() { echo "$1" | sed -E 's#^([A-Za-z]):[\\/]#/\L\1/#; s#\\#/#g'; }

# ── Islands ───────────────────────────────────────────────────────────────────
island_up() { # $1 = cad|doorstar   $2 = port   $3 = optional island dir override
  local name="$1" port="$2" dir="${3:-$ISLANDS_DIR/$1}"
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
  # Doorstar 2026-07-14 óta a saját repójából fut (adat + profil ott él):
  island_up doorstar 13458 "C:/Users/szant/Documents/Development/doorstar-instance"
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

# ── Goal focus (server-managed, per Gábor: goals live in the DB behind the API,
#    never edited as files — every read/change is served + logged) ──────────────
goal_context() {
  # Compact "hol tartunk" snapshot from GET /api/projects/status for prompt injection.
  local token; token="$(fed_token)"
  node -e '
    const t = process.argv[1];
    fetch("http://localhost:13457/api/projects/status", { headers: { Authorization: "Bearer " + t }})
      .then(r => r.json())
      .then(d => {
        const lines = (d.projects || [])
          .filter(p => p.epic_status === "active")
          .map(p => {
            const next = p.next_checkpoint ? `kovetkezo sarokko: ${p.next_checkpoint.name} (${p.next_checkpoint.condition})` : "minden sarokko kesz";
            return `- ${p.epic_id} [${p.progress.percent}%]: ${next}`;
          });
        console.log(lines.join("\n"));
      })
      .catch(() => {}); // island down → empty context, wake still works
  ' "$token" 2>/dev/null
}

# ── Terminals (tmux-based local wake) ─────────────────────────────────────────
cmd_wake() {
  local t="${1:?usage: wake <terminal> [model]}" model="${2:-$DEFAULT_MODEL}"
  local sess="${SESSION_PREFIX}${t}" wd; wd="$(posix_path "$TERMINALS_DIR/$t")"
  [ -d "$wd" ] || { echo "unknown terminal: $t"; exit 1; }
  if tm has-session -t "$sess" 2>/dev/null; then echo "$t already awake ($sess)"; return; fi
  # Prompts are EXTERNALIZED (not hardcoded) so it's inspectable/traceable which
  # message an agent receives on wake — no "black magic". Edit prompts/wake.txt.
  # The goal context is fetched LIVE from the server API (server-managed goals):
  # the agent gets the current epics + next cornerstones without touching files.
  local wake_prompt_file="$PROMPTS_DIR/wake.txt"
  local wake_prompt goals
  if [ -f "$wake_prompt_file" ]; then wake_prompt="$(cat "$wake_prompt_file")"
  else echo "  WARN: $wake_prompt_file missing — waking without a task prompt."; wake_prompt=""; fi
  goals="$(goal_context)"
  if [ -n "$goals" ]; then
    wake_prompt="AKTIV CELOK (a szerver /api/projects/status-abol, ehhez igazodj):
$goals

$wake_prompt"
  fi
  echo "waking $t (model $model) in $wd ...  [prompt: ${wake_prompt_file} + live goal context]"
  tm new-session -d -s "$sess" -c "$wd"
  sleep 1
  # Pass the wake prompt as a LAUNCH ARGUMENT, not by pasting it into the TUI after
  # start. Pasting a big multi-line prompt raced with Claude Code's UI init on
  # Windows/tmux and garbled the text. Here we write the prompt to a runtime file
  # and start `claude ... "$(cat FILE)"` in one short, single-line command — bash
  # expands the file into a single argument, so there is no send-keys/redraw race.
  if [ -n "$wake_prompt" ]; then
    local runtime_prompt="/tmp/spaceos-wake-${t}.txt"
    printf '%s' "$wake_prompt" > "$runtime_prompt"
    tm send-keys -t "$sess" -l "claude --model $model \"\$(cat '$runtime_prompt')\""
  else
    tm send-keys -t "$sess" -l "claude --model $model"
  fi
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

# ── Federation (ADR-066): the root operates on the DB-driven /api/federation API,
#    not hand-written outbox .md files. Token-optimized, audited, logged. ─────────
FED_BASE="http://localhost:13457/api/federation"   # the CAD island serves the API
fed_token() { grep '^MCP_AUTH_TOKEN=' "$ISLANDS_DIR/cad/.env" | cut -d= -f2-; }
fed_curl()  { curl -s -m 8 -H "Authorization: Bearer $(fed_token)" "$@"; }

cmd_fed_send() { # <to_island> <to_terminal> <subject> <body...>
  local to_island="${1:?usage: fed-send <to_island> <to_terminal> <subject> <body>}"
  local to_terminal="${2:?to_terminal required (usually root)}"; local subject="${3:?subject required}"; shift 3
  local body="$*"
  fed_curl -X POST "$FED_BASE/send" -H "Content-Type: application/json" \
    -d "$(printf '{"from_island":"cabinet","from_terminal":"root","to_island":"%s","to_terminal":"%s","type":"info","priority":"medium","subject":"%s","body":"%s"}' "$to_island" "$to_terminal" "$subject" "$body")"
  echo
}

cmd_fed_inbox() { # [island=cabinet] [status=unread]
  local island="${1:-cabinet}" status="${2:-unread}"
  fed_curl "$FED_BASE/inbox?island=$island&status=$status"; echo
}

cmd_fed_msg() { fed_curl "$FED_BASE/message/${1:?usage: fed-msg <id>}"; echo; }
cmd_fed_ack() { fed_curl -X POST "$FED_BASE/ack" -H "Content-Type: application/json" -d "{\"id\":\"${1:?usage: fed-ack <id>}\"}"; echo; }

# ── Dispatch ──────────────────────────────────────────────────────────────────
case "${1:-}" in
  up)        cmd_up ;;
  down)      cmd_down ;;
  status)    cmd_status ;;
  wake)      shift; cmd_wake "$@" ;;
  send)      shift; cmd_send "$@" ;;
  sleep)     shift; cmd_sleep "$@" ;;
  ls)        cmd_ls ;;
  fed-send)  shift; cmd_fed_send "$@" ;;
  fed-inbox) shift; cmd_fed_inbox "$@" ;;
  fed-msg)   shift; cmd_fed_msg "$@" ;;
  fed-ack)   shift; cmd_fed_ack "$@" ;;
  *) echo "Usage: ./fleet.sh {up|down|status|wake <t> [model]|send <t> <text>|sleep <t>|ls|fed-send <island> <term> <subj> <body>|fed-inbox [island] [status]|fed-msg <id>|fed-ack <id>}"; exit 1 ;;
esac
