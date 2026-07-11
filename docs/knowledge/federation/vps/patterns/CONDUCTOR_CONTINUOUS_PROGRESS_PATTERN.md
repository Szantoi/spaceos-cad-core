# Conductor Continuous Progress Pattern

**Created:** 2026-07-02
**Status:** IMPLEMENTED (Monitor-alapú intelligens ellenőrzés)
**Category:** Workflow Automation

---

## Probléma

A Conductor terminál fontos szerepet tölt be a fejlesztési workflow-ban:
- Tervek feldolgozása (queue)
- Outbox DONE review
- Planning pipeline koordináció
- Terminálok közötti koordináció

**Issue:** A Conductor néha idle állapotban marad, miközben feldolgozható munka van a rendszerben.

**Symptoms:**
- Queue tele tervekkel, de nem kerülnek dispatch-elve
- Outbox-okban DONE üzenetek várnak review-ra
- Planning pipeline (ideas/selected/debate) nem halad

---

## Megoldás: Monitor Terminal + Intelligens Ellenőrzés

### Architectural Decision

Használjuk a **Monitor terminált** időnkénti intelligens ellenőrzésre, **nem egyszerű script-based trigger-t**.

**Miért Monitor?**
- ✅ **Intelligens:** LLM-alapú döntéshozatal (nem hardcoded szabályok)
- ✅ **Kontextus-aware:** Látja a teljes rendszer állapotát
- ✅ **Adaptive:** Új helyzetekre is reagálni tud
- ✅ **Audit trail:** Outbox report készül minden ellenőrzésről

**Miért NEM egyszerű script?**
- ❌ **Hardcoded:** Csak előre definiált szabályok alapján működik
- ❌ **Context-blind:** Nem látja a workflow összképét
- ❌ **Fragile:** Új helyzetekre nem tud reagálni
- ❌ **No audit:** Nincs emberi olvasható log

---

## Implementáció

### 1. watchMonitor (10 percenként)

**File:** `spaceos-nexus/knowledge-service/src/pipeline/watchMonitor.ts`

```typescript
// Nightwatch (2 perc) → 5. ciklus (10 perc) → watchMonitor trigger
export async function watchMonitor(): Promise<WatchMonitorResult> {
  cycleCount++;

  if (cycleCount % 5 !== 0) {
    return { triggered: false, reason: `Skipping (cycle ${cycleCount}/5)` };
  }

  // Create Monitor inbox task
  const content = `
# Scheduled Health Check

1. Terminálok status
2. Inbox/Outbox üzenetek
3. Services health
4. **Conductor Progress:**
   - Van feldolgozható munka? (queue, outbox DONE, planning)
   - Idle-e túl sokáig?
   - Ha igen → Conductor inbox üzenet
  `;

  await fs.writeFile(MONITOR_INBOX + filename, content);
}
```

### 2. Monitor Terminál Task

**Minden 10 percben:**
1. Monitor session indul (cold mode)
2. Ellenőrzi a rendszer állapotát
3. **Conductor Progress Check:**
   - `ls docs/planning/queue/` → van-e feldolgozható terv?
   - `grep -r "status: UNREAD" terminals/*/outbox/` → van-e DONE review?
   - `tmux capture-pane -t spaceos-conductor` → idle-e a Conductor?
4. Ha munka van ÉS Conductor idle → **Conductor inbox üzenet**
5. Monitor outbox report (audit trail)
6. Monitor session terminál

### 3. Conductor Inbox Üzenet (ha szükséges)

**File:** `terminals/conductor/inbox/YYYY-MM-DD_NNN_progress-trigger.md`

```markdown
---
id: MSG-CONDUCTOR-NNN
from: monitor
to: conductor
type: task
priority: medium
status: UNREAD
model: sonnet
created: YYYY-MM-DD
---

# Conductor Folytatható Munka Észlelve

A Monitor terminál feldolgozható munkát észlelt:

- **Queue:** 3 terv vár dispatch-re (docs/planning/queue/)
- **Outbox:** 2 DONE üzenet vár review-ra (backend, frontend)
- **Planning:** 5 idea/selected státuszban

**Action:**
1. Dolgozd fel az outbox DONE üzeneteket
2. Dispatch-elj a queue-ból
3. Koordináld a planning pipeline-t
```

---

## Workflow

```
Nightwatch (2 perc)
  ├── watchPriority → Conductor mindig fut
  ├── watchInbox → Nudge ha UNREAD inbox
  └── watchMonitor (10 perc) → Monitor health check
        ↓
Monitor Terminál (cold mode)
  ├── 1. System health check
  ├── 2. Conductor progress check
  │     ├── Queue files count
  │     ├── Outbox UNREAD count
  │     └── Conductor idle check
  └── 3. If (work available + Conductor idle)
          → Create Conductor inbox task
```

---

## Configuration

### Timing

| Component | Interval | Reason |
|-----------|----------|--------|
| Nightwatch | 2 perc | Base monitoring cycle |
| watchMonitor | 10 perc (5× Nightwatch) | Intelligens ellenőrzés (nem túl gyakori) |
| Monitor session | Cold mode (~2-3 perc) | Gyors check, outbox report, terminál |
| Conductor nudge | Max 1× / 10 perc | Monitor által trigger-elve, ha szükséges |

### Thresholds

- **Conductor idle threshold:** 30 perc (nincs aktivitás)
- **Work threshold:** >0 queue OR >0 outbox DONE OR >3 planning items
- **Monitor health check:** 10 perc (5× Nightwatch)

---

## Monitor Terminal CLAUDE.md Snippet

A Monitor terminál CLAUDE.md-jébe be kell építeni a Conductor progress check logikát:

```markdown
## Conductor Progress Check

**Priority:** MEDIUM (minden health check-kor kötelező)

### Ellenőrzési Lépések

1. **Queue check:**
   ```bash
   ls docs/planning/queue/*.md | wc -l
   ```

2. **Outbox DONE check:**
   ```bash
   grep -rl "status: UNREAD" terminals/*/outbox/ | grep -E "done|DONE" | wc -l
   ```

3. **Planning pipeline check:**
   ```bash
   ls docs/planning/ideas/*.md docs/planning/selected/*.md docs/planning/debate/*.md | wc -l
   ```

4. **Conductor idle check:**
   ```bash
   tmux capture-pane -t spaceos-conductor -p | tail -20
   # Elemzés: van-e prompt? Vár-e input-ra? Idle-e?
   ```

### Döntési Logika (Intelligens Priorizálás)

Monitor NEM csak számlál, hanem **értelmezi a projektek állapotát**:

1. **Olvassa a projekt dokumentációt:**
   - `docs/projects/EPICS.yaml` → milyen epic-ek vannak, dependency graph
   - `docs/planning/domain-focus.md` → aktuális fókusz területek
   - `docs/tasks/README.md` → aktív feladatok státusza

2. **Elemzi a folyamatokat:**
   - Queue items: milyen priority, melyik epic-hez tartozik
   - Outbox DONE: melyik terminál, milyen feladat (critical path?)
   - Blocked items: van-e blokkolt munka ami feloldható

3. **Prioritizál:**
   - **CRITICAL:** Blocking epic (dependency graph)
   - **HIGH:** Domain focus területen van munka
   - **MEDIUM:** Queue tele, outbox review szükséges
   - **LOW:** Planning pipeline haladás

4. **Döntés:**

**IF** priority CRITICAL OR HIGH **AND** Conductor idle > 30 perc:
  → **CREATE** Conductor inbox task (priority: critical/high)
  → **INCLUDE:** Mit kell csinálni, miért fontos, melyik epic/projekt

**ELSE IF** priority MEDIUM **AND** Conductor idle > 60 perc:
  → **CREATE** Conductor inbox task (priority: medium)

**ELSE:**
  → **SKIP** (Conductor aktívan dolgozik, vagy work nem sürgős)

### Inbox Üzenet Template (Intelligens Priorizálással)

```markdown
---
id: MSG-CONDUCTOR-{NNN}
from: monitor
to: conductor
type: task
priority: {critical|high|medium}
status: UNREAD
model: sonnet
created: {DATE}
ref: {EPIC-ID vagy task-id}
---

# Conductor Folytatható Munka Észlelve — {Priority Level}

A Monitor terminál **intelligens elemzést végzett** és prioritizált munkát talált:

## 🔴 Critical Path Blokkolt (példa)

**Epic:** EPIC-CUTTING-Q3 (Szabászat modul)
**Status:** BLOCKED - Frontend DONE vár review-ra
**Impact:** Backend nem tud haladni (dependency)

**Files:**
- `terminals/frontend/outbox/2026-07-02_065_cutting-ui-phase1-done.md`

**Action:**
1. ✅ Review frontend DONE → APPROVE/REJECT
2. 📨 Feedback/next steps frontend-nek
3. 🚀 Unblock backend ha approved

---

## 📊 Projekt Kontextus

**Domain Focus:** Cutting, JoineryTech Phase 3
**Active Epics:** 3 (CUTTING, JOINERY, DATAHAVEN)
**Queue:** 2 terv vár dispatch-re
**Outbox DONE:** 1 critical (frontend), 2 medium (backend, designer)

---

## 📋 Recommended Workflow

1. **Critical first:** Frontend Cutting UI review
2. **High priority:** JoineryTech Backend architecture DONE (MSG-BACKEND-105)
3. **Medium:** Queue dispatch (2 planning items)

---

**Estimated time:** 30-45 perc (review + dispatch)
**Blocker resolution:** 1 critical, 0 high
```
```

---

## Benefits

1. **Intelligens döntéshozatal:** Monitor LLM látja a kontextust, nem hardcoded szabályok
2. **Audit trail:** Monitor outbox report minden ellenőrzésről
3. **Adaptive:** Új helyzetekre is reagál
4. **Low overhead:** 10 percenként cold mode session (2-3 perc)
5. **Preventive:** Megelőzi hogy a Conductor hosszú ideig idle maradjon

---

## Testing

### Verify Setup

```bash
# 1. Monitor watcher enabled
grep "watchMonitor" /opt/spaceos/spaceos-nexus/knowledge-service/src/pipeline/nightwatch.ts

# 2. Monitor inbox directory exists
ls /opt/spaceos/terminals/monitor/inbox/

# 3. Monitor CLAUDE.md contains Conductor check logic
grep -A 5 "Conductor Progress" /opt/spaceos/terminals/monitor/CLAUDE.md
```

### Simulate Scenario

```bash
# 1. Create queue item
echo "# Test Plan" > docs/planning/queue/2026-07-02_test.md

# 2. Create outbox DONE
echo "status: UNREAD" > terminals/backend/outbox/2026-07-02_999_test-done.md

# 3. Wait for Monitor cycle (10 min) or trigger manually
curl -X POST http://localhost:3456/api/monitor/trigger

# 4. Check Monitor outbox for report
ls terminals/monitor/outbox/ | tail -1

# 5. Check Conductor inbox for trigger message
grep -l "progress-trigger" terminals/conductor/inbox/*.md
```

---

## Related

- [AUTONOMOUS_AGENT_FRAMEWORK.md](AUTONOMOUS_AGENT_FRAMEWORK.md) — Agent coordination patterns
- [COLD_MODE_SESSION_PATTERN.md](COLD_MODE_SESSION_PATTERN.md) — Cold mode vs continuous sessions
- [TERMINAL_REVIEW_PATTERN.md](TERMINAL_REVIEW_PATTERN.md) — DONE review workflow
- [DISPATCH_CONTROL_PATTERN.md](DISPATCH_CONTROL_PATTERN.md) — Budget-aware dispatch

---

## Future Enhancements

- [ ] **Adaptive interval:** Monitor gyakoribb check ha sok munka van
- [ ] **Priority scoring:** Conductor prioritizáljon fontos queue items alapján
- [ ] **Escalation:** Ha Conductor 60+ perc idle + work → Root escalation
- [ ] **Metrics:** Track Conductor utilization % (active vs idle time)
