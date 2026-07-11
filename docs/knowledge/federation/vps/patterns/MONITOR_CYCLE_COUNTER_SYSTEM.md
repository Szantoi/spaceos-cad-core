# Monitor Cycle Counter System — "MHz Monitoring"

> **Analógia:** A Monitor cycle counter olyan mint a CPU frekvenciája (MHz) — egy szívverés-rendszer
> ami periodikusan ellenőrzi a SpaceOS infrastruktúra egészségét.

**Created:** 2026-07-04
**Author:** Monitor Terminal
**Status:** Active (Cycle 230+)

---

## Koncepció

A Monitor terminál **persistent hot session** mode-ban fut, és egy **cycle counter** követi
nyomon hogy hányadik health check ciklust futtatja. Ez hasonló a CPU órajelhez:

```
CPU:      1.5 GHz = 1,500,000,000 ciklus/másodperc
Monitor:  Cycle 230 = 230. health check (~60 perc interval)
```

---

## Cycle Counter Működés

### Inicializáció

```bash
# nightwatch.sh (watchMonitor trigger)
CYCLE_FILE="/opt/spaceos/terminals/monitor/.cycle-count"

if [ ! -f "$CYCLE_FILE" ]; then
  echo "0" > "$CYCLE_FILE"
fi

CYCLE=$(cat "$CYCLE_FILE")
CYCLE=$((CYCLE + 1))
echo "$CYCLE" > "$CYCLE_FILE"

echo "2026-07-04 12:35:37 [watchMonitor] Cycle $CYCLE - checking triggers (5th cycle!)"
```

### Session Hook Integration

```typescript
// sessionHooks.ts - buildStartContext()
const cycleCount = await readCycleCount('monitor');

context += `\n## Cycle Information\n`;
context += `- Current cycle: ${cycleCount}\n`;
context += `- Previous cycle: ${cycleCount - 1}\n`;
context += `- Interval: ~60 minutes (configurable)\n`;
```

### Persistent Storage

```
/opt/spaceos/terminals/monitor/.cycle-count
```

- Egyetlen szám: aktuális cycle
- Növekszik minden health check trigger-nél
- Túléli a session restart-okat (persistent)
- Reset: manuálisan vagy telepítéskor

---

## Cycle Frekvencia (Konfigurálható)

| Interval | Ciklusok/nap | Use Case |
|----------|--------------|----------|
| **10 perc** | 144 | Development (debug) |
| **30 perc** | 48 | Active development phase |
| **60 perc** | 24 | Production monitoring ✅ CURRENT |
| **120 perc** | 12 | Stable production |

**Jelenlegi:** 60 perc (ADR-053, Mode #4 optimized)

---

## Cycle-based Triggers

### Intelligent Trigger Logic

```typescript
// watchMonitor.ts
const shouldTrigger = (cycle: number, config: MonitorConfig): boolean => {
  // Phase-based triggers (not time-based!)

  if (hasPhaseTransition()) {
    return true; // Immediate trigger (epic phase >90%)
  }

  if (hasCriticalBlocker()) {
    return true; // Immediate trigger (BLOCKED >24h)
  }

  if (cycle % config.cycleInterval === 0) {
    return true; // Regular interval (e.g., every 5th cycle)
  }

  return false; // Skip cycle (silent monitoring)
};
```

### Trigger Examples (Cycle 230)

- **Cycle 225:** Regular check (5th cycle = 230 % 5 = 0)
- **Cycle 226-229:** Skipped (silent monitoring)
- **Cycle 230:** Regular check (5th cycle = 230 % 5 = 0) ✅ CURRENT
- **Cycle 231-234:** Will skip (unless critical event)
- **Cycle 235:** Next regular check

---

## "MHz" Analógia — Miért Fontos?

### CPU MHz vs Monitor Cycles

| Property | CPU | Monitor |
|----------|-----|---------|
| **Unit** | Hertz (cycles/sec) | Cycle count (health checks) |
| **Frequency** | 1-5 GHz (billions/sec) | 24-144/day (configurable) |
| **Purpose** | Execute instructions | System health verification |
| **Persistence** | Volatile (reset on power off) | Persistent (.cycle-count file) |
| **Optimization** | Higher = faster | Dynamic (phase-based triggers) |

### Miért "MHz" és nem "timer"?

**Timer (naiv megközelítés):**
```bash
# Rossz: minden 60 percben fut, függetlenül az eseményektől
*/60 * * * * /opt/spaceos/scripts/watch-monitor.sh
```

**Cycle-based (intelligens):**
```bash
# Jó: minden 10 percben ELLENŐRIZ, de csak akkor FUTTAT ha kell
*/10 * * * * /opt/spaceos/scripts/watch-monitor.sh  # Cycle count++
```

**Előny:**
- ✅ Phase transition → azonnali trigger (nem vár 60 percet)
- ✅ Conductor idle + work → azonnali ösztönzés
- ✅ BLOCKED >24h → kritikus alert
- ✅ Normál működés → csendes monitoring (token spórolás)

---

## Cycle Persistence Strategy

### File-based Counter

```bash
# /opt/spaceos/terminals/monitor/.cycle-count
230
```

**Miért file, nem DB?**
- ✅ Egyszerű (1 fájl, 1 szám)
- ✅ Gyors olvasás (<1 ms)
- ✅ Crash-safe (atomikus írás)
- ✅ Audit trail (git verziókövetés)

### Reset Conditions

```bash
# Manual reset (deployment)
echo "0" > /opt/spaceos/terminals/monitor/.cycle-count

# Infrastructure upgrade
rm /opt/spaceos/terminals/monitor/.cycle-count
# Next trigger: starts from 1
```

---

## Cycle Monitoring Dashboard

### MEMORY.md Tracking

```markdown
**Last Updated:** 2026-07-04 12:37 CEST
**Session Summary:**
- Cycle 230 (5th cycle interval, persistent session)
- MSG-MONITOR-013 processed (Mode #4 health check)
- MSG-MONITOR-014 outbox written (INFO status — OK)
```

### Outbox Report Format

```markdown
# Health Check — Mode #4 Structured Program

**Timestamp:** 2026-07-04 12:37:15
**Trigger:** MSG-MONITOR-013 (Scheduled, Cycle 230)
**Next health check:** ~60 perc (Cycle 236)
```

### Nightwatch Log Format

```
2026-07-04 12:35:37 [watchMonitor] Cycle 230 - checking triggers (5th cycle!)
2026-07-04 12:35:37 [Monitor] Health check triggered (mode-aware): MSG-MONITOR-013
```

---

## Performance Metrics

| Metric | Target | Actual (Cycle 230) |
|--------|--------|-------------------|
| **Session time** | <60s | ~60s ✅ |
| **Token usage** | <2000 | ~1800 ✅ |
| **Cycle interval** | 60 min | 60 min ✅ |
| **Root spam** | <1/hour | 0 (only OK) ✅ |
| **False positives** | 0 | 0 ✅ |

---

## Cycle-based Intelligence (ADR-053)

### Phase Transition Detection

```typescript
// epicProgressTracker.ts
const detectPhaseTransition = (epic: Epic): boolean => {
  const phases = ['Foundation', 'Core', 'Production', 'Launch'];
  const currentPhase = getCurrentPhase(epic);
  const progress = getPhaseProgress(epic, currentPhase);

  if (progress > 90 && currentPhase !== 'Launch') {
    // Phase >90% complete → next phase ready
    return true; // Trigger encouragement
  }

  return false;
};
```

### Conductor Idle Detection

```typescript
// conductorMonitor.ts
const isConductorIdleWithWork = (): boolean => {
  const idleTime = getConductorIdleMinutes();
  const hasWork = hasQueuedWork() || hasUnreadOutbox();

  if (idleTime > 120 && hasWork) {
    return true; // Trigger encouragement (ösztönzés)
  }

  return false;
};
```

---

## Következő Fejlesztések

### Cycle Analytics (Q3)

```typescript
interface CycleMetrics {
  cycle: number;
  timestamp: Date;
  duration_ms: number;
  token_usage: number;
  critical_findings: number;
  escalations_sent: number;
  status: 'ok' | 'warning' | 'critical';
}

// Goal: Predict optimal cycle interval based on workload patterns
```

### Dynamic Interval Adjustment

```typescript
// Auto-adjust based on activity
const getOptimalInterval = (recentActivity: Activity[]): number => {
  if (isHighActivity(recentActivity)) {
    return 30; // Increase frequency (30 min)
  }

  if (isLowActivity(recentActivity)) {
    return 120; // Decrease frequency (2 hours)
  }

  return 60; // Default (1 hour)
};
```

---

## Összefoglalás

**Monitor Cycle Counter = "MHz for Agent Infrastructure"**

| Feature | Benefit |
|---------|---------|
| **Persistent counting** | Túléli session restart-okat |
| **Phase-based triggers** | Intelligens, nem csak időalapú |
| **Token efficiency** | Csendes monitoring (nincs felesleges spam) |
| **Dashboard visibility** | MEMORY.md + outbox report tracking |
| **Scalable** | 10-120 perc interval (konfigurálható) |

**Jelenlegi állapot:** Cycle 230, 60 perc interval, Mode #4 optimized ✅

---

**Referencia:**
- `CLAUDE.md` (Monitor terminal)
- `MONITOR-CONFIG.yaml` (cycle interval konfigurálása)
- `EPIC_PROGRESS_TRACKER.md` (phase-based triggers)
- `MEMORY.md` (cycle tracking persistence)
