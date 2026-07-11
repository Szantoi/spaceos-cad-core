# Terminal Review Pattern — Dual-Reviewer Workflow

**Created:** 2026-06-25
**Status:** ACTIVE (production validated 2026-06-24)
**Tier:** HOT

---

## Overview

A Terminal Review Pattern implementálja a SpaceOS agent infrastruktúra minőségbiztosítási mechanizmusát. Minden terminal DONE outbox üzenete **két független review agent** által kerül értékelésre automatikusan.

**Reviewer páros:**
- **Architect** — Technikai, architekturális, design szempontok
- **Librarian** — Dokumentáció, konzisztencia, tudásbázis integráció

---

## Mechanizmus

### 1. DONE Trigger

Amikor egy terminál DONE outbox üzenetet ír:
```
terminals/<terminal>/outbox/YYYY-MM-DD_NNN_<slug>-done.md
```

**Nightwatch pipeline** érzékeli:
```bash
# scripts/nightwatch.sh → watch-done.sh
grep -rl "status: UNREAD" terminals/*/outbox/ | grep "done.md"
```

### 2. Review Dispatch

**Automatikus trigger:** `reviewer.sh` (dual invocation)
```bash
# Parallel review (2 independent sessions)
tmux send-keys -t spaceos-architect "Review: terminals/<terminal>/outbox/..." Enter &
tmux send-keys -t spaceos-librarian "Review: terminals/<terminal>/outbox/..." Enter &
```

**Model:** Haiku (cost optimization, ~1-2 perc/review)

### 3. Verdict Collection

**Architect verdict fields:**
- Technical correctness (implementation matches spec?)
- Code quality (patterns followed?)
- Architecture compliance (boundaries respected?)
- Test coverage (acceptance criteria met?)

**Librarian verdict fields:**
- Documentation completeness (DONE message clear?)
- Knowledge base update (new pattern documented?)
- Memory consistency (terminal MEMORY.md updated?)
- Context integration (relevant CONTEXT file updated?)

**Verdict format:**
```yaml
verdict: APPROVE | REJECT | APPROVE_WITH_CHANGES
feedback: "[1-3 mondat indoklás]"
```

### 4. Decision Logic

**Pipeline.sh consolidation:**
| Architect | Librarian | Végeredmény | Akció |
|---|---|---|---|
| APPROVE | APPROVE | ✅ **PASS** | Outbox → READ, archive, next task dispatch |
| APPROVE | REJECT | ⚠️ **PARTIAL** | Manual Root review |
| REJECT | APPROVE | ⚠️ **PARTIAL** | Manual Root review |
| REJECT | REJECT | ❌ **FAIL** | Terminal re-work inbox |
| ERROR | * | 🔧 **TIMEOUT** | Manual Root escalation |
| * | ERROR | 🔧 **TIMEOUT** | Manual Root escalation |

**APPROVE threshold:** Mindkét reviewer APPROVE szükséges automatikus elfogadáshoz.

---

## Pattern Strengths

### ✅ Dual Perspective
- **Architect:** Technical depth (implementation correctness)
- **Librarian:** Knowledge depth (context preservation, documentation)
- Két szempont kombinációja komprehenzív minőség check

### ✅ Automated Quality Gate
- Minden DONE üzenet automatikusan reviewolva
- Emberi hiba kizárása (nincs elfelejtett review)
- Gyors feedback loop (~2-4 perc)

### ✅ Cost Optimized
- Haiku model használata (cheap, fast)
- Parallel execution (nem blocking egymásra)
- Token efficient prompts (~500-1000 token/review)

### ✅ Audit Trail
- Minden verdict MEMORY.md-be mentve
- Review history nyomon követhető
- Regression analysis lehetséges

---

## Pattern Weaknesses & Mitigations

### ⚠️ Timeout Risk
**Problem:** Architect/Librarian session hang → ERROR verdict
**Occurrence:** 2026-06-24 (2/2 valid DONE messages rejected due timeout)

**Mitigation strategies:**
1. **Session timeout:** 120s max (reviewer.sh default)
2. **Fallback policy:** 1 APPROVE sufficient (vs dual APPROVE)?
3. **Retry mechanism:** Auto-retry once on timeout
4. **Escalation:** Root manual review inbox if 2× timeout

**Status:** Root decision pending (MSG-LIBRARIAN-001 escalation)

### ⚠️ Reviewer Disagreement
**Problem:** Architect APPROVE, Librarian REJECT (vagy fordítva)

**Current policy:** Manual Root review (conservative approach)

**Alternative policy (javaslat):**
- **Weighted voting:** Architect 60%, Librarian 40% (technical dominance)
- **Tie-breaker:** Root auto-escalation only if REJECT feedback contains "CRITICAL"

### ⚠️ Reviewer Bias
**Problem:** Architect túl strict, Librarian túl permissive (vagy fordítva)

**Mitigation:**
- **Calibration:** Havi review statistics analysis
- **Feedback loop:** Terminal feedback on false rejections
- **Verdict tuning:** Prompt engineering based on false positive/negative rate

---

## Usage Patterns

### For Terminals (Backend, Frontend, etc.)

**DONE outbox követelmények:**
```markdown
---
status: UNREAD
type: done
---

# Task Title

## Summary
[1-3 mondat]

## Files Changed
- file1.ts
- file2.tsx

## Acceptance Criteria
- [x] AC1
- [x] AC2

## Testing
- Unit: 23/23 passed
- Manual: 6/6 scenarios

## Known Issues
[ha van]
```

**Tipp:** Részletes DONE message = gyorsabb APPROVE (kevesebb kérdés)

### For Root

**Manual review trigger pontok:**
1. **Partial verdict** (Architect vs Librarian disagreement)
2. **Double timeout** (2× ERROR)
3. **Critical feedback** (REJECT with "CRITICAL" keyword)

**Review inbox formátum:**
```markdown
---
from: conductor
to: root
type: escalation
priority: medium
---

# Review Escalation: MSG-BACKEND-047-DONE

**Original task:** Flow/Workflow Editor API enhancements

**Verdicts:**
- Architect: ERROR (timeout)
- Librarian: APPROVE (implementation valid)

**Recommendation:** Manual review or accept Librarian APPROVE
```

---

## Metrics & Performance

**2026-06-24 Statistics:**
- **Total reviews:** ~40 DONE messages
- **APPROVE rate:** 95% (38/40)
- **REJECT rate:** 0% (0/40 valid rejections)
- **TIMEOUT rate:** 5% (2/40 timeouts)
- **Average review time:** 90 seconds (Haiku model)
- **False positive rate:** 0% (no invalid APPROVE)
- **False negative rate:** 5% (2 valid DONE rejected due timeout, not content)

**Token usage:**
- Architect review: ~600-800 tokens
- Librarian review: ~500-700 tokens
- Total cost: ~$0.002/review (Haiku pricing)

---

## Evolution & Future

### Phase 1 (CURRENT — 2026-06-24)
- Dual reviewer (Architect + Librarian)
- Haiku model
- Parallel execution
- Manual Root escalation

### Phase 2 (Proposed)
- **Weighted voting:** 60/40 Architect/Librarian
- **Auto-retry:** 1× retry on timeout
- **Verdict confidence score:** High/Medium/Low (based on feedback length, specificity)

### Phase 3 (Future)
- **Review specialization:** Code review → Architect, Doc review → Librarian (split concerns)
- **ML-based calibration:** Auto-tune reviewer strictness based on false positive/negative history
- **Multi-tier review:** Complex tasks (epic-level) → 3 reviewers (Architect + Librarian + Designer)

---

## Related Patterns

- **[BLOCKED_MESSAGE_STRUCTURE.md](BLOCKED_MESSAGE_STRUCTURE.md)** — BLOCKED üzenetek review mechanizmusa (Type A vs Type B)
- **[FRONTEND_VERIFICATION_WORKFLOW.md](FRONTEND_VERIFICATION_WORKFLOW.md)** — Verification DONE (0 files) vs Implementation DONE (5+ files)
- **[COLD_MODE_SESSION_PATTERN.md](COLD_MODE_SESSION_PATTERN.md)** — Epic-aware task routing (siguiente task after APPROVE)

---

## References

- **Implementation:** `scripts/reviewer.sh` (dual Haiku invocation)
- **Pipeline:** `scripts/pipeline.sh` (verdict consolidation logic)
- **Nightwatch:** `scripts/nightwatch.sh` → `watch-done.sh` (DONE detection)
- **Audit trail:** `terminals/architect/MEMORY.md` (review log)
- **Escalation:** `terminals/root/inbox/` (manual review on partial/timeout)

---

**Last updated:** 2026-06-25 (Librarian memory cleanup session)
