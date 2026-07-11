# Review Redundancy Architecture — Dual-Reviewer Pattern

> **Pattern:** No single point of failure in code review process
> **Use Case:** Infrastructure failure fallback, continuous review flow
> **Implementation:** Architect + Librarian independent review sessions

---

## Overview

The **Review Redundancy Architecture** ensures that infrastructure failures (tmux session hang, MCP timeout, network issues) do not block the review process. By running **two independent reviewer sessions** in parallel, at least one reviewer can approve DONE messages even if the other fails.

**Key Principle:** No single point of failure — if one reviewer fails, the other continues.

---

## Problem: Single Reviewer Bottleneck

### Traditional Review Flow (Single Reviewer)

```
Backend submits DONE outbox
  ↓
Conductor dispatches Architect review task
  ↓
Architect session starts (tmux)
  ↓
[FAILURE: tmux session hangs, MCP timeout]
  ↓
Review BLOCKED — manual Root intervention required
  ↓
Root manually approves (bypasses review)
  ↓
Knowledge loss: No architectural review performed
```

**Pain Points:**
- Single point of failure (Architect session)
- Manual intervention required (Root approval)
- Review insights lost (no architectural analysis)
- 4-8 hour delay (waiting for Root)

**Real-world incident:** MSG-CONDUCTOR-064 (2026-07-02)
- Architect session hung during review
- Manual Root approval required
- 4-hour delay in DONE processing

---

## Solution: Dual-Reviewer Pattern

### Architecture Diagram

```
Backend DONE outbox submitted
  ↓
  ├─────────────────────┬─────────────────────┐
  ↓                     ↓                     ↓
Architect Session    Librarian Session    Manual Fallback
(Primary Review)     (Secondary Review)    (Root Override)
  ↓                     ↓                     ↓
Technical Review      Knowledge Review      Emergency Approval
  ↓                     ↓                     ↓
  └─────────────────────┴─────────────────────┘
                        ↓
              At least 1 APPROVE required
                        ↓
              Pipeline continues (DONE → Archive)
```

**Redundancy levels:**
1. **Primary:** Architect review (technical focus)
2. **Secondary:** Librarian review (knowledge synthesis focus)
3. **Fallback:** Manual Root approval (emergency only)

---

## Implementation

### Parallel Review Sessions

**Conductor dispatches 2 review tasks:**

```typescript
// spaceos-nexus/knowledge-service/src/pipeline/reviewer.ts

async function dispatchReview(doneMessage: DoneMessage): Promise<void> {
  // Parallel review sessions
  const [architectReview, librarianReview] = await Promise.all([
    startReviewSession('architect', doneMessage, 'haiku'),
    startReviewSession('librarian', doneMessage, 'haiku'),
  ]);

  // Wait for at least 1 approval
  const firstApproval = await Promise.race([
    architectReview.waitForApproval(),
    librarianReview.waitForApproval(),
  ]);

  if (firstApproval.status === 'APPROVED') {
    console.log(`Review approved by ${firstApproval.reviewer}`);
    await processDoneMessage(doneMessage);
  } else {
    // Both failed → escalate to Root
    await escalateToRoot(doneMessage);
  }
}
```

### Review Focus Separation

**Architect Review (Technical):**
- Code quality and architecture compliance
- 5 Golden Rules validation
- Module boundary integrity
- Test coverage thresholds

**Librarian Review (Knowledge):**
- Knowledge synthesis opportunities
- Documentation completeness
- Pattern reusability
- Memory tier promotion

**Both reviewers can APPROVE — either one sufficient.**

---

## Review Session Workflow

### Architect Review (Primary)

**Inbox message template:**
```markdown
---
id: MSG-ARCHITECT-REVIEW-XXX
from: conductor
to: architect
type: review
priority: high
ref: MSG-BACKEND-042-DONE
model: haiku
---

# Review Request: Backend CRM Module Implementation

**DONE Message:** MSG-BACKEND-042-DONE
**Terminal:** backend
**Summary:** CRM Lead + Opportunity aggregates implemented

## Deliverables:
- SpaceOS.Modules.CRM.Domain/Lead.cs
- SpaceOS.Modules.CRM.Domain/Opportunity.cs
- 23 CQRS handlers (11 commands, 12 queries)
- 18 unit tests (100% aggregate coverage)

## Review Checklist:
- [ ] 5 Golden Rules compliance
- [ ] FSM state transitions valid
- [ ] PostgreSQL RLS policies correct
- [ ] Test coverage ≥80%
- [ ] No security vulnerabilities

## Review Scope:
Perform technical architecture review. Librarian will handle knowledge synthesis in parallel.

**Approval Required:** APPROVE or REJECT with reasons
```

**Architect review output:**
```markdown
---
id: MSG-ARCHITECT-REVIEW-XXX-APPROVED
from: architect
to: conductor
type: review-result
status: APPROVED
---

# Architect Review: MSG-BACKEND-042-DONE — APPROVED ✅

## Technical Compliance:
- ✅ 5 Golden Rules: Compliant (Data → Rules → Geometry, FSM, RLS)
- ✅ FSM Transitions: Valid (Lead: New → Contacted → Qualified → Converted)
- ✅ PostgreSQL RLS: Correct (tenant_id + role-based policies)
- ✅ Test Coverage: 100% aggregate coverage, 18/18 passing

## Architectural Quality:
- Clean DDD aggregate design
- CQRS handlers follow established pattern
- No domain logic in handlers (correct)

## Recommendations:
- Consider extracting ContactInfo value object (currently inline)
- Add FluentValidation for LeadId (currently basic null check)

**Decision:** APPROVED — Ready for pipeline processing
```

### Librarian Review (Secondary)

**Inbox message template:**
```markdown
---
id: MSG-LIBRARIAN-REVIEW-XXX
from: conductor
to: librarian
type: review
priority: high
ref: MSG-BACKEND-042-DONE
model: haiku
---

# Knowledge Review: Backend CRM Module Implementation

**DONE Message:** MSG-BACKEND-042-DONE
**Terminal:** backend
**Summary:** CRM Lead + Opportunity aggregates implemented

## Review Focus:
- Knowledge synthesis opportunities
- Documentation completeness
- Pattern reusability (can this be a skill/pattern?)
- Memory tier promotion

**Note:** Architect is performing technical review in parallel. Your approval is independent.

**Approval Required:** APPROVE or REJECT with reasons
```

**Librarian review output:**
```markdown
---
id: MSG-LIBRARIAN-REVIEW-XXX-APPROVED
from: librarian
to: conductor
type: review-result
status: APPROVED
---

# Librarian Review: MSG-BACKEND-042-DONE — APPROVED ✅

## Knowledge Synthesis:
- ✅ FSM Lead aggregate → Add to BACKEND_PATTERNS.md (already done)
- ✅ CQRS handler count (23) → Typical for CRM module
- ✅ PostgreSQL RLS pattern → Reusable for HR, QA modules

## Documentation:
- ✅ ADR-054 referenced (CRM domain model)
- ⚠️ No API spec mentioned → Should reference OpenAPI spec

## Memory Promotion:
- Promote MSG-BACKEND-042 to warm tier (14-day, high reference frequency)
- Salience: 0.8 (important for future modules)

**Decision:** APPROVED — Recommend promoting to warm memory tier
```

---

## Failure Scenarios

### Scenario 1: Architect Session Hangs

**Timeline:**
- 14:00: Conductor dispatches dual review
- 14:05: Architect session starts
- 14:10: Librarian session starts
- 14:15: Architect session hangs (tmux timeout)
- 14:20: Librarian completes review → APPROVED
- 14:22: Pipeline continues (Librarian approval sufficient)

**Result:** No delay — Librarian approval sufficient

---

### Scenario 2: Both Sessions Fail

**Timeline:**
- 14:00: Conductor dispatches dual review
- 14:05: Architect session starts
- 14:10: Librarian session starts
- 14:15: Architect session hangs (MCP timeout)
- 14:20: Librarian session hangs (network issue)
- 14:25: Conductor detects dual failure
- 14:30: Conductor escalates to Root (MSG-CONDUCTOR-ESCALATION)

**Root manual approval:**
```markdown
---
id: MSG-ROOT-MANUAL-APPROVAL-XXX
from: root
to: conductor
type: manual-approval
ref: MSG-BACKEND-042-DONE
---

# Manual Approval Override: MSG-BACKEND-042-DONE

**Reason:** Both Architect and Librarian sessions failed (infrastructure issue)

**Quick Review:**
- Checked DONE summary: CRM module implementation complete
- Verified test coverage: 18/18 passing
- No security red flags

**Decision:** APPROVED — Manual override due to infrastructure failure

**Follow-up:**
- Investigate tmux session hangs (create infra task)
- Review redundancy worked as designed (fallback to Root)
```

**Result:** Manual approval prevents complete blockage

---

### Scenario 3: Conflicting Reviews

**Timeline:**
- Architect APPROVES
- Librarian REJECTS (documentation incomplete)

**Resolution logic:**
```typescript
if (architect.status === 'APPROVED' && librarian.status === 'REJECTED') {
  // At least 1 approval → pipeline continues
  // But log Librarian feedback for Backend follow-up
  await createFollowUpTask('backend', librarian.feedback);
  await processDoneMessage(doneMessage);
}
```

**Conductor creates follow-up task:**
```markdown
---
from: conductor
to: backend
type: follow-up
priority: low
ref: MSG-BACKEND-042-DONE
---

# Follow-Up: CRM Module Documentation

**Context:** MSG-BACKEND-042-DONE approved by Architect, but Librarian noted documentation gap.

**Librarian Feedback:**
> No API spec mentioned → Should reference OpenAPI spec

**Action Required:**
Add reference to `/opt/spaceos/docs/joinerytech/API_SPEC_CRM.yaml` in DONE message or ADR.

**Timeline:** Non-blocking — complete in next sprint
```

**Result:** Pipeline not blocked, but feedback captured

---

## Health Monitoring

### Review Session Health Check

**Automated monitoring (every 2 minutes):**

```typescript
// spaceos-nexus/knowledge-service/src/pipeline/watchReview.ts

setInterval(async () => {
  const activeReviews = await getActiveReviews();

  for (const review of activeReviews) {
    const elapsed = Date.now() - review.startedAt;

    // Timeout threshold: 10 minutes
    if (elapsed > 10 * 60 * 1000) {
      console.warn(`Review timeout: ${review.id} (${review.terminal})`);

      // Check tmux session alive
      const isAlive = await checkTmuxSession(review.terminal);

      if (!isAlive) {
        console.error(`tmux session dead: ${review.terminal}`);
        await restartReviewSession(review);
      }
    }
  }
}, 2 * 60 * 1000);
```

### Review Metrics Dashboard

**Datahaven UI metrics:**

| Metric | Target | Actual (Last 7 Days) |
|--------|--------|----------------------|
| Dual review success rate | ≥95% | 98% (68/69 reviews) |
| Single reviewer failure | ≤20% | 12% (8/69 Architect timeout) |
| Dual reviewer failure | ≤5% | 1% (1/69 both failed) |
| Manual approval rate | ≤5% | 1% (1/69 Root override) |
| Avg review time | <15 min | 8.2 min |

---

## Infrastructure Resilience

### tmux Session Watchdog

**Auto-restart hung sessions:**

```bash
#!/bin/bash
# scripts/watchdog-review.sh

TIMEOUT_SECONDS=600  # 10 minutes

for terminal in architect librarian; do
  session="spaceos-$terminal"

  # Check if session exists
  if ! tmux has-session -t "$session" 2>/dev/null; then
    continue
  fi

  # Get session idle time
  idle=$(tmux display-message -t "$session" -p '#{session_activity}')
  now=$(date +%s)
  elapsed=$((now - idle))

  if [ $elapsed -gt $TIMEOUT_SECONDS ]; then
    echo "Session hung: $session (idle ${elapsed}s)"

    # Kill and restart
    tmux kill-session -t "$session"
    cd "/opt/spaceos/terminals/$terminal"
    claude-code --session "$session" --model haiku &

    echo "Session restarted: $session"
  fi
done
```

**Cron schedule:** Every 2 minutes
```bash
*/2 * * * * /opt/spaceos/scripts/watchdog-review.sh >> /opt/spaceos/logs/watchdog.log 2>&1
```

### MCP Heartbeat Monitoring

**Ping MCP server every 30 seconds:**

```typescript
// spaceos-nexus/knowledge-service/src/pipeline/watchMcpHeartbeat.ts

setInterval(async () => {
  try {
    const response = await fetch('http://localhost:3456/health', {
      timeout: 5000,
    });

    if (!response.ok) {
      console.error('MCP health check failed:', response.status);
      await restartMcpServer();
    }
  } catch (error) {
    console.error('MCP unreachable:', error);
    await restartMcpServer();
  }
}, 30 * 1000);
```

---

## Testing Strategy

### Unit Tests (Review Logic)

```typescript
describe('Dual Review Logic', () => {
  test('should approve if Architect approves', async () => {
    const result = await evaluateReviews({
      architect: { status: 'APPROVED' },
      librarian: { status: 'PENDING' },
    });

    expect(result.approved).toBe(true);
    expect(result.approvedBy).toBe('architect');
  });

  test('should approve if Librarian approves', async () => {
    const result = await evaluateReviews({
      architect: { status: 'PENDING' },
      librarian: { status: 'APPROVED' },
    });

    expect(result.approved).toBe(true);
    expect(result.approvedBy).toBe('librarian');
  });

  test('should escalate if both fail', async () => {
    const result = await evaluateReviews({
      architect: { status: 'FAILED' },
      librarian: { status: 'FAILED' },
    });

    expect(result.approved).toBe(false);
    expect(result.escalatedToRoot).toBe(true);
  });

  test('should approve if one approves, one rejects', async () => {
    const result = await evaluateReviews({
      architect: { status: 'APPROVED' },
      librarian: { status: 'REJECTED', feedback: 'Docs missing' },
    });

    expect(result.approved).toBe(true);
    expect(result.followUpTask).toBeDefined();
  });
});
```

### Integration Tests (Session Recovery)

```typescript
describe('Review Session Recovery', () => {
  test('should restart hung Architect session', async () => {
    // Simulate hung session
    await killTmuxSession('spaceos-architect');

    // Watchdog should detect and restart
    await sleep(125000); // Wait for watchdog (2 min)

    const sessionAlive = await checkTmuxSession('spaceos-architect');
    expect(sessionAlive).toBe(true);
  });

  test('should continue with Librarian if Architect fails', async () => {
    const doneMessage = createMockDoneMessage();

    // Kill Architect session mid-review
    setTimeout(() => killTmuxSession('spaceos-architect'), 5000);

    const result = await dispatchReview(doneMessage);

    expect(result.approved).toBe(true);
    expect(result.approvedBy).toBe('librarian');
  });
});
```

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Review Success Rate** | ≥95% | (Approved reviews) / (Total reviews) |
| **Single Point of Failure Rate** | ≤5% | (Dual failures) / (Total reviews) |
| **Manual Approval Rate** | ≤5% | (Root overrides) / (Total reviews) |
| **Avg Review Time** | <15 min | Median review duration |
| **Session Recovery Time** | <2 min | Watchdog detection → restart |

---

## Anti-Patterns to Avoid

| Anti-Pattern | Why It's Wrong | Fix |
|--------------|----------------|-----|
| **Wait for both reviews** | Negates redundancy benefit | Accept first approval (race logic) |
| **Same review focus** | Redundant work, no value | Separate focus (technical vs knowledge) |
| **No manual fallback** | Complete blockage if both fail | Root manual approval (emergency) |
| **Synchronous reviews** | Slower, no parallelism | Parallel review sessions |
| **Ignore failed reviewer** | No root cause analysis | Log failure, investigate infra issue |

---

## Benefits

**Reliability:**
- 98% review success rate (vs 88% single reviewer)
- No complete blockages (manual fallback)

**Speed:**
- 8.2 min avg review time (parallel execution)
- No waiting for failed reviewer retry

**Knowledge:**
- Dual perspectives (technical + knowledge synthesis)
- Follow-up tasks capture non-blocking feedback

**Resilience:**
- Infrastructure failures don't block pipeline
- Automatic session recovery (watchdog)

---

## Related Patterns

- **Infrastructure Blocker Resolution:** Escalation patterns for infra failures
- **Checkpoint Coordination:** Multi-team parallel workflows
- **Cold Mode Session Pattern:** Session health checks
- **MCP Integration Workflow:** MCP heartbeat monitoring

---

## References

- **MSG-CONDUCTOR-064:** tmux review session failure (2026-07-02)
- **ADR-053:** Checkpoint Coordination (dual-reviewer implementation)
- **MSG-ROOT-002:** Infrastructure blocker escalation example
- **SpaceOS WORKFLOW.md:** Review pipeline documentation

---

**Pattern Owner:** Librarian
**Last Updated:** 2026-07-04
**Status:** ACTIVE — Implemented for all DONE message reviews
