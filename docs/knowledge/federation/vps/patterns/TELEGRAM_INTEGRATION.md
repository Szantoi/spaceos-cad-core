# Telegram & Inbox Integration Pattern

> **Status:** IMPLEMENTED (2026-06-21)
> **Reference:** Marveen project (https://github.com/Szotasz/marveen) - use critically, not blindly

## Problems Solved

1. **Telegram messages**: Arrive via webhook but sessions didn't see them automatically
2. **Inbox messages**: If session was already running, new UNREAD messages got "stuck" - not delivered

## Core Principle (from Marveen)

> **If session NOT running → start it. If session IS running → INJECT the message via tmux send-keys.**

This ensures messages are NEVER stuck waiting for manual intervention.

## Solution: Direct tmux Injection

Instead of just saving messages to inbox files, inject them directly into the running tmux session using `tmux send-keys`.

### Architecture

```
Telegram Bot API
      │
      ▼
Webhook endpoint (/api/telegram/webhook)
      │
      ▼
processWebhookUpdate()
      │
      ├─► Command? (/status, /help, etc.) → Handle & respond
      │
      └─► Message → forwardToRootInbox()
              │
              ├─► Session exists? → injectTelegramMessageToRoot()
              │         │
              │         └─► tmux send-keys -t spaceos-root -l '[TELEGRAM @user] message'
              │         └─► tmux send-keys -t spaceos-root Enter
              │
              └─► Session missing? → Save to inbox file (fallback)
```

### Key Implementation

**File:** `/opt/spaceos/spaceos-nexus/knowledge-service/src/pipeline/telegramBot.ts`

```typescript
import { execSync } from 'child_process';

const ROOT_SESSION = 'spaceos-root';

function sessionExists(sessionName: string): boolean {
  try {
    execSync(`tmux has-session -t ${sessionName} 2>/dev/null`, { stdio: 'ignore' });
    return true;
  } catch {
    return false;
  }
}

function sendPromptToTmuxSession(sessionName: string, text: string): boolean {
  if (!sessionExists(sessionName)) return false;

  try {
    const safeText = text
      .replace(/\r?\n/g, ' ')
      .replace(/'/g, "'\\''");

    // Send in chunks to avoid paste detection issues
    const CHUNK_SIZE = 80;
    for (let i = 0; i < safeText.length; i += CHUNK_SIZE) {
      const chunk = safeText.slice(i, i + CHUNK_SIZE);
      execSync(`tmux send-keys -t ${sessionName} -l '${chunk}'`, { timeout: 5000 });
    }

    execSync(`tmux send-keys -t ${sessionName} Enter`, { timeout: 5000 });
    return true;
  } catch {
    return false;
  }
}

function injectTelegramMessageToRoot(username: string, text: string): boolean {
  const prompt = `[TELEGRAM @${username}] ${text}`;
  return sendPromptToTmuxSession(ROOT_SESSION, prompt);
}
```

### Chunking Strategy

From Marveen's `agent-process.ts`:
- Send text in 80-character chunks using `tmux send-keys -l` (literal mode)
- Avoid paste detection which can cause `[Pasted text #N]` placeholders
- Handle special characters (newlines → spaces, escape single quotes)

### Fallback Behavior

If root session is not running:
1. Save message to `/opt/spaceos/terminals/root/inbox/` as markdown file
2. InboxWatcher will detect it
3. When session starts, it can process the inbox

### Webhook Configuration

```bash
# .env
TELEGRAM_TOKEN=<bot_token>
TELEGRAM_CHAT_ID=<allowed_chat_id>
TELEGRAM_WEBHOOK_URL=https://datahaven.joinerytech.hu/api/telegram/webhook
TELEGRAM_WEBHOOK_SECRET=spaceos-webhook-secret-2026
```

### Commands Supported

| Command | Description |
|---------|-------------|
| `/status` | Fleet status (working/idle terminals) |
| `/inbox` | Unread inbox messages |
| `/queue` | Planning queue |
| `/health` | System health check |
| `/help` | Available commands |

Non-command messages are forwarded to root.

## Marveen Reference Patterns

When implementing agent control features, the Marveen project can serve as reference:

- `src/web/agent-process.ts` - tmux session management, `sendPromptToSession`
- `src/web/telegram.ts` - Telegram API helpers
- `src/pane-state.ts` - detecting if session is idle/busy
- `src/web/inbound-probe.ts` - deafness detection

**IMPORTANT:** Use Marveen as reference critically, not as copy-paste source. Adapt patterns to SpaceOS architecture.

## Testing

1. Send message via Telegram
2. Should see `[TELEGRAM @username] message` appear in root session
3. If session not running, check inbox file created

## Troubleshooting

| Issue | Check |
|-------|-------|
| Message not received | `tail /tmp/knowledge-service.log` |
| Webhook errors | `curl https://datahaven.joinerytech.hu/api/telegram/info` |
| Session not found | `tmux list-sessions` - verify `spaceos-root` exists |
| Injection failed | Check tmux permissions, session name |
| Text stuck in input | Buffer clear working? Check log for warnings |
| Injection appends to existing text | Escape/Ctrl+U should clear - verify session is responsive |

### Full Marveen-Style Injection Flow

The injection system implements the full Marveen pattern with these steps:

**1. Pre-flight buffer clearing:**
- **Escape** - Close any open modal/menu (file picker, tool permission dialog)
- **Ctrl+U** - Clear any text parked in the input buffer
- **0.1s delay** - Let the buffer clear

**2. Chunked sending:**
- Text split into 80-character chunks
- **`-` boundary sliding** - If a chunk boundary lands on `-`, slide up to 8 chars to avoid tmux flag interpretation
- **Space prepend** - If chunk starts with `-`, prepend a space
- **0.03s delay** between chunks to avoid triggering paste detection
- **Enter** - Submit the message

**3. Post-send retry loop (up to 4 attempts):**
- **Capture pane** - Check if prompt was submitted
- **Placeholder detection** - Check for `[Pasted text #N]` stub
  - If placeholder: **Ctrl+C** to discard, then resend chunks
- **Parked text detection** - Check if text visible in input box with idle footer
  - If parked: Send extra **Enter**
- **Done** - If neither condition, prompt was submitted

**4. Placeholder handling:**
- Ctrl+U does NOT clear `[Pasted text #N]` placeholders
- Only **Ctrl+C** works (up to 3 attempts with 0.45s settle time)
- After clearing, resend the entire message

This prevents:
- Text stuck in input line (pre-flight buffer clear)
- Paste placeholder not submitting (Ctrl+C clear + resend)
- Swallowed Enter (post-send retry loop)

---

## Inbox Message Injection

**File:** `/opt/spaceos/spaceos-nexus/knowledge-service/src/sessionStarter.ts`

When InboxWatcher detects a new UNREAD message:

1. **Session NOT running** → Start new session with `tmux new-session` + `claude --model X`
2. **Session IS running** → Inject notification: `[INBOX] Te a TERMINAL terminál vagy. Van UNREAD üzeneted: MSG-ID`

### Implementation

```typescript
/**
 * Clear any stuck text in the input buffer (Marveen-style)
 * - Escape: close any modal/menu
 * - Ctrl+U: clear the input line
 */
function clearInputBuffer(sessionName: string): void {
  try {
    // Send Escape to close any modal (e.g., file picker, menu)
    execSync(`tmux -S /tmp/spaceos.tmux send-keys -t ${sessionName} Escape`, { timeout: 2000 });
    execSync('sleep 0.1');

    // Send Ctrl+U to clear any parked text in input buffer
    execSync(`tmux -S /tmp/spaceos.tmux send-keys -t ${sessionName} C-u`, { timeout: 2000 });
    execSync('sleep 0.1');
  } catch {
    // Non-fatal - continue with injection anyway
  }
}

function injectMessageToSession(sessionName: string, message: string): boolean {
  try {
    // STEP 1: Clear any stuck text in input buffer (Marveen pattern)
    clearInputBuffer(sessionName);

    // STEP 2: Sanitize message
    const safeText = message.replace(/\r?\n/g, ' ').replace(/'/g, "'\\''");

    // STEP 3: Chunk to avoid buffer overflow (80 chars)
    for (let i = 0; i < safeText.length; i += 80) {
      const chunk = safeText.slice(i, i + 80);
      execSync(`tmux -S /tmp/spaceos.tmux send-keys -t ${sessionName} -l '${chunk}'`, { timeout: 5000 });
    }

    // STEP 4: Send Enter to submit
    execSync(`tmux -S /tmp/spaceos.tmux send-keys -t ${sessionName} Enter`, { timeout: 5000 });
    return true;
  } catch {
    return false;
  }
}
```

### Priority Terminals

Root and Conductor are "priority" terminals - managed by watch-priority.sh, not auto-started. BUT if they're running and get inbox, the message IS injected.

### Log Output

```
[InboxWatcher] new_inbox: architect <- MSG-ARCHITECT-001 (high)
[SessionStarter] spaceos-architect already running, injecting inbox notification...
[SessionStarter] ✓ Injected inbox notification to spaceos-architect
```
