# Monitor Goal Drift Detection Protocol

> **Terminal:** Monitor
> **Created:** 2026-07-04
> **Priority:** HIGH (Goal Persistence felügyelet)
> **References:** `GOAL_PERSISTENCE_PATTERNS.md`, `MSG-MONITOR-021`

---

## Monitor Felelősség

A Monitor terminál felelős a **Goal Drift** (cél eltérés) felügyeletéért a SpaceOS agent infrastruktúrában. Ez kritikus a hosszú futású agent session-ök során.

---

## 5 Goal Drift Failure Mode — Monitor Detektálás

### 1. Context Dilution (Kontextus Hígulás)

**Mi történik:**
- Korai instrukciók elhalványulnak ahogy a context window telik
- 50+ turn után az agent "elfelejti" az eredeti célt

**Monitor detekció:**
```bash
# Conductor turn count ellenőrzés
TURN_COUNT=$(cat /opt/spaceos/terminals/conductor/.turn-count 2>/dev/null || echo "0")

if [ "$TURN_COUNT" -gt 30 ]; then
  echo "⚠️ WARNING: Conductor turn count >30 (Context dilution risk)"
fi

if [ "$TURN_COUNT" -gt 50 ]; then
  echo "🔴 CRITICAL: Conductor turn count >50 (Auto re-anchor needed)"
fi
```

**NWT Threshold:**
- **15 NWT (30 turn):** WARNING log
- **25 NWT (50 turn):** AUTO RE-ANCHOR trigger

**Ajánlott akció:**
- Ellenőrizd hogy `contextSaturation.ts` auto re-anchor triggerelődött-e
- Ha nem → Conductor inbox warning (context saturation)

---

### 2. Pattern Matching Override (Minta Felülírás)

**Mi történik:**
- Friss kontextus (pl. DONE outbox) felülírja az explicit epic prioritást
- Terminál "eltereljed" az epic céljától

**Monitor detekció:**
```bash
# Epic alignment check
ACTIVE_EPIC=$(grep -A1 "status: active" /opt/spaceos/docs/projects/EPICS.yaml | grep "id:" | head -1 | awk '{print $2}')

# Backend recent DONE-ok epic alignment
RECENT_DONE=$(find /opt/spaceos/terminals/backend/outbox -name "*DONE.md" -mtime -1 -exec grep -l "$ACTIVE_EPIC" {} \; | wc -l)
TOTAL_DONE=$(find /opt/spaceos/terminals/backend/outbox -name "*DONE.md" -mtime -1 | wc -l)

if [ "$RECENT_DONE" -lt "$((TOTAL_DONE / 2))" ]; then
  echo "⚠️ WARNING: Backend <50% DONE messages kapcsolódnak az aktív epic-hez"
fi
```

**Ajánlott akció:**
- Conductor inbox: Epic alignment warning
- Root escalation ha többször ismétlődik

---

### 3. Inherited Drift (Öröklött Eltérítés)

**Mi történik:**
- Subagent (Backend/Frontend) DONE output-ok túl részletesek
- Technikai részletek "szennyezik" a Conductor fókuszát
- Conductor elterjed a high-level koordinációtól

**Monitor detekció:**
```bash
# DONE outbox size check
find /opt/spaceos/terminals/*/outbox -name "*DONE.md" -type f -exec sh -c '
  size=$(wc -c < "$1")
  if [ "$size" -gt 1500 ]; then
    filtered=$(grep -c "Filtered" "$1" || echo "0")
    if [ "$filtered" -eq 0 ]; then
      echo "⚠️ WARNING: $1 (${size} chars, NOT filtered)"
    fi
  fi
' sh {} \;
```

**NWT Threshold:**
- **DONE outbox >1500 char:** Ellenőrizd "Filtered" flag jelenlétét

**Ajánlott akció:**
- Ha nem filtered → Backend/Frontend inbox: Use output filtering
- Ellenőrizd `outputFiltering.ts` működését

---

### 4. Value Conflict Drift (Érték Konfliktus Eltérés)

**Mi történik:**
- Modell saját értékeivel ütköző instrukciók lassan erodálódnak
- Pl. "Gyors implementáció" vs "biztonságos kód" konfliktus
- Agent "megtagadja" vagy "újraértelmezi" a feladatot

**Monitor detekció:**
```bash
# BLOCKED messages with refusal/value conflict pattern
find /opt/spaceos/terminals/*/outbox -name "*.md" -exec grep -l "type: blocked" {} \; -exec grep -l "cannot\|refuse\|conflict\|disagree" {} \; | sort | uniq -d
```

**Ajánlott akció:**
- Root escalation (CRITICAL priority)
- Task refactoring szükséges (konfliktus feloldása)

---

### 5. Subgoal Displacement (Részfeladat Kiszorítás)

**Mi történik:**
- Terminál "túl jól csinálja" egy részfeladatot
- Tökéletesítés blokkolja az epic haladást
- "Side-quest" mentalitás (pl. refactoring instead of delivery)

**Monitor detekció:**
```bash
# Epic progress stagnálás ellenőrzése
# Ha task count növekszik, de checkpoint progress nem
TASK_COUNT=$(grep -c "status: done" /opt/spaceos/terminals/backend/outbox/*.md 2>/dev/null || echo "0")
CHECKPOINT_PROGRESS=$(grep -A5 "id: $ACTIVE_EPIC" /opt/spaceos/docs/projects/EPICS.yaml | grep "progress:" | awk '{print $2}' | tr -d '%')

# Ha task/progress arány rossz (>10 task / 1% progress)
echo "Task count: $TASK_COUNT, Progress: $CHECKPOINT_PROGRESS%"
```

**Ajánlott akció:**
- Conductor inbox: Subgoal displacement warning
- Root review: Epic task breakdown túl granulált?

---

## Goal Persistence Health Check (Integrált Checklist)

Monitor health check során (Cycle N) futtasd:

```markdown
## 🎯 Goal Persistence Check (KRITIKUS!)

### 1. Context Saturation (Conductor)
- [ ] Turn count <50 (check terminals/conductor/.turn-count)
- [ ] Ha >50: re-anchor trigger megtörtént?
- [ ] Session state mentve? (.session-state.json friss?)
- [ ] NWT threshold: 15 WARNING, 25 CRITICAL (auto re-anchor)

**Commands:**
```bash
TURN_COUNT=$(cat /opt/spaceos/terminals/conductor/.turn-count 2>/dev/null || echo "0")
echo "Conductor turn count: $TURN_COUNT"
[ "$TURN_COUNT" -gt 50 ] && echo "🔴 CRITICAL: Auto re-anchor needed"
```

### 2. Epic Alignment
- [ ] Aktív terminálok az EPICS.yaml epic-en dolgoznak?
- [ ] DONE outbox-ok az epic checkpoint-okhoz kapcsolódnak?
- [ ] Nincs "off-topic" munka?

**Commands:**
```bash
ACTIVE_EPIC=$(grep -A1 "status: active" /opt/spaceos/docs/projects/EPICS.yaml | grep "id:" | head -1 | awk '{print $2}')
echo "Active epic: $ACTIVE_EPIC"
grep -r "$ACTIVE_EPIC" /opt/spaceos/terminals/*/outbox/*.md 2>/dev/null | wc -l
```

### 3. Inherited Drift Prevention
- [ ] DONE outbox-ok <1500 char (ha több: filtered flag?)
- [ ] Dense milestone feedback működik? (watchDone.ts)
- [ ] Conductor kapja a progress update-eket?

**Commands:**
```bash
find /opt/spaceos/terminals/*/outbox -name "*DONE.md" -type f -exec wc -c {} + | awk '{if ($1 > 1500) print $2 " - " $1 " chars"}'
```

### 4. Goal Recovery
- [ ] Session restart után goal context injektálva?
- [ ] Conductor briefing tartalmazza az epic progress-t?
- [ ] Cross-session state (.session-state.json) frissül?

**Commands:**
```bash
ls -lh /opt/spaceos/terminals/conductor/.session-state.json 2>/dev/null
cat /opt/spaceos/terminals/conductor/.session-state.json 2>/dev/null | jq '.epicId, .progress'
```
```

---

## Implementált Védelmek — Monitor Ellenőrzési Pontok

| Védelem | Fájl | Monitor Ellenőrzés |
|---------|------|-------------------|
| **Cross-session Goal Recovery** | `sessionState.ts` | `.session-state.json` frissül? Timestamp <2h? |
| **Auto Context Saturation** | `contextSaturation.ts` | `.turn-count` <50? Ha >50, re-anchor log létezik? |
| **Subagent Output Filtering** | `outputFiltering.ts` | DONE outbox <1500 char VAGY "Filtered" flag? |
| **Dense Milestone Feedback** | `watchDone.ts` | Conductor session log tartalmaz progress update-eket? |

---

## NWT Thresholds (Goal Persistence)

| Metrika | Threshold (NWT) | Emberi idő | Monitor Akció |
|---------|-----------------|------------|---------------|
| **Turn count WARNING** | 15 NWT | 30 min | Outbox log (low priority) |
| **Turn count CRITICAL** | 25 NWT | 50 min | Conductor inbox (high priority) |
| **Session age check** | 60 NWT | 2 óra | Context review ajánlás |
| **DONE outbox size** | - | - | >1500 char → filtered ellenőrzés |
| **Epic progress stall** | 30 NWT | 1 óra | Ha 0% progress change → warning |

---

## Eszkalációs Mátrix

| Failure Mode | Severity | Monitor Akció | Root Eszkaláció? |
|--------------|----------|---------------|------------------|
| **Context Dilution** (>50 turn) | CRITICAL | Conductor inbox (auto re-anchor kérés) | Ha 2× egymás után |
| **Pattern Override** (<50% epic alignment) | HIGH | Conductor inbox (epic realignment) | Ha >3×/nap |
| **Inherited Drift** (>1500 char, not filtered) | MEDIUM | Terminal inbox (use filtering) | Nem |
| **Value Conflict** (refusal BLOCKED) | CRITICAL | Root inbox (task redesign) | Azonnal |
| **Subgoal Displacement** (progress stall) | HIGH | Conductor inbox (task breakdown review) | Ha >2h stall |

---

## Példa Health Check Output (Goal Persistence Integrated)

```markdown
## 🎯 Goal Persistence Check

**Status:** ✅ OK (No drift detected)

### 1. Context Saturation
- ✅ Conductor turn count: 23 (<50 threshold)
- ✅ Session state: Fresh (updated 15 min ago)
- ✅ NWT: 12 (24 min session age, <15 WARNING threshold)

### 2. Epic Alignment
- ✅ Active epic: EPIC-CUTTING-Q3
- ✅ Backend DONE: 8/10 (80%) kapcsolódik epic-hez
- ✅ Frontend DONE: 5/5 (100%) kapcsolódik epic-hez

### 3. Inherited Drift Prevention
- ✅ DONE outbox size: 3 messages, all <1000 chars
- ✅ Output filtering: 2/3 filtered (67%)
- ✅ Dense milestone feedback: Operational

### 4. Goal Recovery
- ✅ .session-state.json: Updated 15 min ago
- ✅ Epic progress: 72% (up from 68%)
- ✅ Next milestone: Phase 3 dispatch

---

**Ajánlás:** Nincs Goal Drift detektálva. Folytatható a normál monitoring.
```

---

## Automatizálási Lehetőségek (Jövő)

### Phase 1: Manual Checklist (DONE — 2026-07-04)
- ✅ MEMORY.md Goal Drift checklist
- ✅ Health check manual ellenőrzés
- ✅ Outbox report Goal Persistence szekcióval

### Phase 2: Automated Detection (Jövő)
- ⏳ Bash script: `check-goal-drift.sh` (auto run minden cycle-ben)
- ⏳ JSON output: Goal Drift metrics
- ⏳ Auto-escalation: Root inbox ha critical drift

### Phase 3: Predictive Monitoring (Jövő)
- ⏳ Trend analysis: Epic progress velocity
- ⏳ Anomaly detection: Unusual task patterns
- ⏳ Proactive re-anchoring: Before >50 turn

---

## Referenciák

- **Kutatás:** `docs/knowledge/patterns/GOAL_PERSISTENCE_PATTERNS.md`
- **Implementáció:** `spaceos-nexus/knowledge-service/src/conductor/*`
- **Monitor CLAUDE.md:** Goal Drift monitoring felelősség
- **Task:** MSG-MONITOR-021 (2026-07-04)

---

**Maintainer:** Monitor Terminal
**Last Updated:** 2026-07-04
**Status:** Active (Production monitoring)
