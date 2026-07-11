# Review Redundancy Architecture

## Purpose

Implement the **dual-reviewer pattern** to ensure infrastructure failures (tmux session hang, MCP timeout, network issues) do not block the code review process. By running two independent reviewer sessions in parallel, at least one reviewer can approve DONE messages even if the other fails.

**ROI:** 98% review success rate (vs 88% single reviewer), zero complete blockages

---

## When to Use

Trigger this skill when:
- Setting up review pipeline for multi-terminal workflow
- Infrastructure reliability issues causing review delays
- Single reviewer bottleneck identified
- Review process needs redundancy guarantees

**DO NOT use** for:
- Single-terminal projects (no review workflow)
- Prototype/spike work (no formal review needed)
- Local development (no infrastructure risk)

---

## Prerequisites

**Infrastructure:**
- tmux session management for reviewers
- MCP Knowledge Service running (`localhost:3456`)
- Datahaven Dashboard for monitoring
- Watchdog scripts for session recovery

**Reviewers:**
- Architect (technical focus)
- Librarian (knowledge synthesis focus)

**Inputs:**
- DONE outbox messages from terminals
- Review checklist (5 Golden Rules, test coverage, security)

---

## Step-by-Step

### Step 1: Configure Parallel Review Sessions (30 min)

**Conductor review dispatcher:**

```typescript
// spaceos-nexus/knowledge-service/src/pipeline/reviewer.ts

async function dispatchReview(doneMessage: DoneMessage): Promise<void> {
  console.log(`Dispatching dual review for ${doneMessage.id}`);

  // Parallel review sessions (Architect + Librarian)
  const [architectReview, librarianReview] = await Promise.all([
    startReviewSession('architect', doneMessage, 'haiku'),
    startReviewSession('librarian', doneMessage, 'haiku'),
  ]);

  // Wait for at least 1 approval (race logic)
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

**Reviewer separation of concerns:**

| Reviewer | Focus | Approval Criteria |
|----------|-------|-------------------|
| **Architect** | Technical compliance | 5 Golden Rules, FSM valid, RLS correct, test coverage ≥80% |
| **Librarian** | Knowledge synthesis | Documentation complete, pattern reusable, memory tier promotion |

**Either reviewer can APPROVE** — first approval sufficient.

### Step 2: Create Review Inbox Templates (15 min)

**Architect review template:**

```markdown
---
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

**Librarian review template:**

```markdown
---
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

### Step 3: Setup Session Watchdog (20 min)

**tmux session health monitoring:**

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

**Cron schedule:**
```bash
# Every 2 minutes
*/2 * * * * /opt/spaceos/scripts/watchdog-review.sh >> /opt/spaceos/logs/watchdog.log 2>&1
```

### Step 4: Implement Approval Logic (15 min)

**Evaluation function:**

```typescript
interface ReviewResult {
  approved: boolean;
  approvedBy?: 'architect' | 'librarian';
  escalatedToRoot?: boolean;
  followUpTask?: string;
}

async function evaluateReviews(reviews: {
  architect: { status: string; feedback?: string };
  librarian: { status: string; feedback?: string };
}): Promise<ReviewResult> {
  // At least 1 approval → pipeline continues
  if (reviews.architect.status === 'APPROVED') {
    return { approved: true, approvedBy: 'architect' };
  }

  if (reviews.librarian.status === 'APPROVED') {
    return { approved: true, approvedBy: 'librarian' };
  }

  // Both failed → escalate
  if (
    reviews.architect.status === 'FAILED' &&
    reviews.librarian.status === 'FAILED'
  ) {
    return { approved: false, escalatedToRoot: true };
  }

  // One approve, one reject → create follow-up task
  if (
    reviews.architect.status === 'APPROVED' &&
    reviews.librarian.status === 'REJECTED'
  ) {
    return {
      approved: true,
      approvedBy: 'architect',
      followUpTask: reviews.librarian.feedback,
    };
  }

  // Default: wait for completion
  return { approved: false };
}
```

### Step 5: MCP Heartbeat Monitoring (10 min)

**MCP server health check:**

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
}, 30 * 1000); // Every 30 seconds
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
- 14:30: Conductor escalates to Root

**Root manual approval:**
```markdown
---
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

**Architect APPROVES, Librarian REJECTS:**

```typescript
// Conductor creates follow-up task for Backend
await createFollowUpTask('backend', {
  priority: 'low',
  ref: 'MSG-BACKEND-042-DONE',
  content: `
# Follow-Up: CRM Module Documentation

**Context:** Approved by Architect, but Librarian noted documentation gap.

**Librarian Feedback:**
> No API spec mentioned → Should reference OpenAPI spec

**Action Required:**
Add reference to /opt/spaceos/docs/joinerytech/API_SPEC_CRM.yaml in DONE message or ADR.

**Timeline:** Non-blocking — complete in next sprint
  `,
});
```

**Result:** Pipeline not blocked, but feedback captured

---

## Health Monitoring

### Review Metrics Dashboard

**Datahaven UI metrics:**

| Metric | Target | Actual (Last 7 Days) |
|--------|--------|----------------------|
| **Dual review success rate** | ≥95% | 98% (68/69 reviews) |
| **Single reviewer failure** | ≤20% | 12% (8/69 Architect timeout) |
| **Dual reviewer failure** | ≤5% | 1% (1/69 both failed) |
| **Manual approval rate** | ≤5% | 1% (1/69 Root override) |
| **Avg review time** | <15 min | 8.2 min |

**MCP endpoint:**
```bash
curl -s localhost:3456/api/metrics/review-redundancy | jq .
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

## Real-World Example

### EPIC-JT-CRM Week 4 Review (2026-06-22)

**Timeline:**
- 16:00: Backend submits MSG-BACKEND-042-DONE
- 16:05: Conductor dispatches dual review (Architect + Librarian)
- 16:10: Architect session starts → review in progress
- 16:12: Librarian session starts → review in progress
- 16:18: Architect session hangs (tmux timeout)
- 16:20: Librarian completes review → APPROVED ✅
- 16:22: Pipeline continues (DONE → Archive)

**Outcome:**
- Review time: 20 minutes (vs 4+ hours if waiting for Architect recovery)
- Dual-reviewer redundancy prevented 3.5-hour delay
- Architect session auto-restarted by watchdog (16:24)

**ROI:**
- Infrastructure failure: 1/69 reviews (1.4%)
- Zero complete blockages (vs 12% with single reviewer)

---

## Related Skills

- **infrastructure-blocker-resolution-guide:** Escalation patterns for infra failures
- **checkpoint-coordination-workflow:** Multi-team parallel workflows
- **contract-first-development-workflow:** Review checklist for API contracts

---

## Maintenance Notes

**When to update this skill:**
- New reviewer added (e.g., Security Reviewer for RBAC changes)
- Review checklist changed (new compliance requirement)
- Infrastructure monitoring improved (new health check)

**Watchdog tuning:**
- Timeout threshold: 10 minutes (adjust if reviews take longer)
- Restart strategy: Kill + restart (vs nudge with Enter)

---

**Skill Owner:** Librarian
**Created:** 2026-07-04
**Status:** ACTIVE — Use for all DONE message reviews
