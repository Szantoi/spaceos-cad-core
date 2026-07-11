# Conductor Session Killer - Root Cause Analysis

**Felfedezve:** 2026-07-02 08:35 UTC
**Explorer Agent:** b107c6d7
**Issue:** Conductor tmux session 20-30 percenként leáll

---

## Probléma Leírás

A `spaceos-conductor` tmux session rendszeresen újraindult/leállt, annak ellenére, hogy:
- `terminals.json`-ban `priority: true` volt beállítva
- `sessionMode: "continuous"` volt definiálva
- AutonomousDev API-ról leállítva lett (`/api/autonomous/stop`)

---

## Root Cause (Gyökérok)

### File: `spaceos-nexus/knowledge-service/src/pipeline/autonomousDev.ts`

**Lines 247-252:**
```typescript
async function coldStartConductor(
  config: AutonomousDevConfig,
  cycleId: number,
  prompt: string,
  tokenCount: number
): Promise<boolean> {
  const session = 'spaceos-conductor';
  const workdir = SESSION_WORKDIR[session] || '/opt/spaceos/terminals/conductor';

  await log(`[AutonomousDev] Cycle ${cycleId}: Cold starting Conductor (prompt tokens: ${tokenCount})`);

  // Kill existing session for clean start
  if (await hasSession(session)) {
    await killSession(session);  // <-- KILÖVI A CONDUCTOR-T!
    await new Promise(resolve => setTimeout(resolve, 2000));
  }

  // Create new session
  await newSession(session, workdir);
  // ... continues to restart conductor
}
```

### Trigger Mechanizmus

1. **Enabled flag:** `.env` fájlban `ENABLE_AUTONOMOUS_DEV=true`
2. **Interval:** `AUTONOMOUS_DEV_INTERVAL_MINUTES=30` (alapértelmezett: 20)
3. **Cold start mode:** `coldStart: true` (mindig törli a session-t)
4. **Scheduler:** `startup.ts:289-296` indítja a scheduler-t

### Miért történik?

Az AutonomousDev célja:
```typescript
/**
 * autonomousDev.ts — Autonóm Fejlesztési Ciklus
 *
 * "Folyamatosan fejleszt a UI terv alapján"
 *
 * Működés:
 * 1. Minden N percben (alapértelmezett: 20) új fejlesztési ciklust indít
 * 2. Conductor hideg indítással indul (tiszta context)
 * 3. Conductor a docs/planning/ vagy design dokumentumok alapján kiválaszt egy feladatot
 * 4. Terminálnak inbox üzenetet küld
 */
```

A "tiszta context" érdekében **mindig kilövi a Conductor-t**, még akkor is ha:
- `priority: true` van beállítva
- `sessionMode: "continuous"` van definiálva
- Manuálisan indított session fut

---

## Evidence (Bizonyítékok)

### Log Patterns

```
2026-07-02 06:10:15 [AutonomousDev] Cycle 4: Cold starting Conductor (prompt tokens: 107)
2026-07-02 06:10:23 [AutonomousDev] Cycle 4: Conductor started with optimized prompt
2026-07-02 06:15:48 [AutonomousDev] Cycle 1: Cold starting Conductor (prompt tokens: 162)
2026-07-02 06:26:42 [AutonomousDev] Cycle 1: Cold starting Conductor (prompt tokens: 162)
```

Minden ~20-30 perc → Conductor kill + restart.

### Config Ignorálás

**File:** `config/terminals.json`
```json
{
  "conductor": {
    "priority": true,
    "sessionMode": "continuous",
    ...
  }
}
```

Az AutonomousDev **nem olvassa** ezt a konfigurációt, hardcoded `killSession()` hívással dolgozik.

### Összehasonlítás: watchIdle.ts (HELYES viselkedés)

**File:** `pipeline/watchIdle.ts:77-79`
```typescript
// Skip priority sessions (root, conductor)
if (isPrioritySession(sessionName)) {
  continue;
}
```

A `watchIdle` **helyesen respektálja** a priority flag-et → nem lő ki priority session-öket.

Az AutonomousDev **nem tartalmaz** ilyen ellenőrzést.

---

## Megoldás

### 1. Gyors Fix (Implemented 2026-07-02 08:36)

**File:** `.env`
```bash
# BEFORE
ENABLE_AUTONOMOUS_DEV=true

# AFTER
ENABLE_AUTONOMOUS_DEV=false
```

**Restart:**
```bash
pkill -f "ts-node src/server.ts"
cd /opt/spaceos/spaceos-nexus/knowledge-service
npm exec ts-node src/server.ts &
```

**Verification:**
```bash
curl -s http://localhost:3456/api/autonomous/status
# Returns: {"enabled":false,"running":false,...}
```

### 2. Architectural Fix (TODO)

**Option A: Respect sessionMode**
```typescript
async function coldStartConductor(...) {
  const session = 'spaceos-conductor';

  // Check if continuous mode
  const terminalConfig = getTerminalConfig('conductor');
  if (terminalConfig?.sessionMode === 'continuous') {
    // Inject prompt instead of killing
    await injectPrompt(session, prompt);
    return true;
  }

  // Otherwise cold start
  if (await hasSession(session)) {
    await killSession(session);
  }
  await newSession(session, workdir);
}
```

**Option B: Inbox-based approach**
```typescript
async function autonomousDevCycle() {
  // Don't kill Conductor
  // Instead: create inbox task for Conductor
  await createInboxTask({
    to: 'conductor',
    from: 'system',
    type: 'autonomous-cycle',
    priority: 'low',
    content: generateAutonomousPrompt()
  });
}
```

**Option C: Mode flag**
```typescript
// Check if manual control mode is active
if (isManualControlMode()) {
  console.log('[AutonomousDev] Manual control active, skipping cycle');
  return;
}
```

---

## Related Components

### Files Involved

| File | Role | Respects Priority? |
|------|------|-------------------|
| `autonomousDev.ts` | **Killer** - Unconditionally kills Conductor | ❌ NO |
| `watchIdle.ts` | Idle session cleanup | ✅ YES (skip priority) |
| `sessionStarter.ts` | Session lifecycle management | ✅ YES (checks sessionMode) |
| `terminals.json` | Terminal configuration | - (config file) |
| `.env` | Enable/disable flags | - (config file) |
| `startup.ts` | Bootstraps AutonomousDev scheduler | - (orchestration) |

### API Endpoints

```bash
# Get status
GET /api/autonomous/status

# Start (DANGEROUS - will kill Conductor every 20 min)
POST /api/autonomous/start

# Stop (only stops scheduler, doesn't persist)
POST /api/autonomous/stop

# Trigger manual cycle (one-time)
POST /api/autonomous/trigger
```

---

## Lessons Learned

1. **Configuration ignorálás veszélyes**
   Az AutonomousDev nem olvassa a `terminals.json` konfigurációt → `sessionMode: "continuous"` ignorálva van.

2. **Priority flag nem univerzális**
   A `watchIdle`, `sessionStarter` tiszteletben tartja a priority flag-et, de az AutonomousDev nem.

3. **API stop != .env disable**
   A `/api/autonomous/stop` csak a running state-et állítja le, nem változtatja a `.env` fájlt.
   Service restart után újraindul → **permanens fix:** `.env` módosítás szükséges.

4. **Cold start vs Continuous mode conflict**
   A "clean context" philosophy (cold start) nem kompatibilis a continuous session mode-dal.

---

## Testing

### Verify Fix

```bash
# 1. Check .env
grep ENABLE_AUTONOMOUS_DEV /opt/spaceos/spaceos-nexus/knowledge-service/.env
# Should return: ENABLE_AUTONOMOUS_DEV=false

# 2. Check API status
curl -s http://localhost:3456/api/autonomous/status | grep enabled
# Should return: "enabled":false

# 3. Monitor Conductor session
watch -n 60 'tmux ls | grep spaceos-conductor'
# Should show stable session (not restarting every 20 min)

# 4. Check logs
tail -f /tmp/knowledge-service.log | grep AutonomousDev
# Should NOT show "[AutonomousDev] Cycle" messages
```

### Timeline Verification

1. **Before fix:** Conductor session restarted every 20-30 minutes
2. **After fix:** Conductor session stable for 60+ minutes
3. **Log evidence:** No `[AutonomousDev] Cycle` entries after restart

---

## References

- **Issue:** `.github/issues/2026-07-02_002_autonomous-dev-manual-control-conflict.md`
- **ADR:** N/A (architectural decision pending)
- **Explorer Agent ID:** b107c6d7
- **Memory:** `terminals/root/MEMORY.md` (Kritikus Tanulságok #2)

---

## Future Work

- [ ] Implement `sessionMode` awareness in AutonomousDev
- [ ] Add manual/autonomous control mode flag
- [ ] Refactor AutonomousDev to use inbox-based task dispatch
- [ ] Add session tagging: `startedBy: 'manual' | 'autonomous'`
- [ ] Unit tests for priority session protection
