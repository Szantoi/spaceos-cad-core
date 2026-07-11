# Checkpoint Coordination Workflow

## Purpose

Orchestrate **multi-team epic development** with automated checkpoint triggers. Enables Backend and Frontend to work in parallel by defining intermediate milestones (checkpoints) that automatically notify downstream teams when ready.

**ROI:** 8 weeks → 5 weeks (37.5% faster delivery)

---

## When to Use

Trigger this skill when:
- Epic spans multiple terminals (Backend → Frontend → Integration)
- Sequential workflow causes idle time (Frontend waiting for Backend)
- Cross-team dependencies need automation
- Parallel development possible (with checkpoints)

**DO NOT use** for:
- Single-terminal epics (no coordination needed)
- Fully independent work (no dependencies)
- Prototypes (no formal checkpoints)

---

## Prerequisites

**EPICS.yaml structure:**
```yaml
epics:
  - id: EPIC-JT-CRM
    checkpoints:
      - id: CP-CRM-BACKEND
        owner: backend
        triggers: [...]
```

**MCP tools:**
- `subscribe_to_checkpoint` (event subscription)
- `get_checkpoint_status` (status query)

---

## Step-by-Step

### Step 1: Define Checkpoints in EPICS.yaml (30 min)

```yaml
epics:
  - id: EPIC-JT-CRM
    name: "CRM Module"
    checkpoints:
      - id: CP-CRM-BACKEND
        name: "Backend API Ready"
        owner: backend
        acceptance:
          - "OpenAPI spec finalized"
          - "Contract tests passing (Dredd 100%)"
          - "Auth API deployed to dev"
        triggers:
          - terminal: frontend
            action: start_ui_development
            inbox_template: |
              # CRM Frontend Development - Backend Ready
              Backend checkpoint reached. OpenAPI spec: `/docs/API_SPEC_CRM.yaml`

      - id: CP-CRM-FRONTEND
        name: "Frontend UI Ready"
        owner: frontend
        acceptance:
          - "Lead/Opportunity screens complete"
          - "Component tests passing (≥80%)"
        triggers:
          - terminal: architect
            action: integration_review
```

### Step 2: Subscribe to Checkpoint Notifications (5 min)

```typescript
await mcp__spaceos-knowledge__subscribe_to_checkpoint({
  epic_id: 'EPIC-JT-CRM',
  checkpoint_id: 'CP-CRM-BACKEND',
  terminal: 'frontend',
  events: ['checkpoint_reached'],
  delivery_method: 'inbox',
});
```

### Step 3: Backend Reaches Checkpoint (automated)

When Backend completes acceptance criteria:
```bash
# Contract tests pass
dredd docs/API_SPEC_CRM.yaml http://localhost:5000/v1

# Deploy to dev
kubectl apply -f k8s/dev/crm-api.yaml

# Checkpoint auto-detected → Inbox created for Frontend
```

### Step 4: Frontend Receives Notification (automated)

Inbox message automatically created:
```markdown
---
from: conductor
to: frontend
type: checkpoint-reached
priority: high
---

# CRM Frontend Development - Backend Ready

Backend checkpoint reached: CP-CRM-BACKEND

OpenAPI spec: `/opt/spaceos/docs/joinerytech/API_SPEC_CRM.yaml`
Mock API setup: Use MSW
Backend integration: Available Week 4
```

---

## Error Handling

**Checkpoint never reached:**
- Check acceptance criteria (are they automated/testable?)
- Check Backend DONE outbox (did task complete?)

**Duplicate notifications:**
- Subscription manager deduplicates (one notification per checkpoint)

---

## Success Metrics

| Metric | Target |
|--------|--------|
| **Parallel Dev Time** | 8 weeks → 5 weeks |
| **Coordination Overhead** | 2-3 days → 5 minutes |
| **Idle Time** | 0 weeks (Frontend starts Week 1.5) |

---

## Real-World Example

**EPIC-JT-CRM:** 3 checkpoints (Backend, Frontend, Integration)
- Week 1.5: CP-BACKEND → Frontend starts
- Week 4: CP-FRONTEND + CP-INTEGRATION → Architect review
- Total: 5 weeks (vs 8 sequential)

---

## Related Skills

- **contract-first-development-workflow:** Week 0 spec enables checkpoints
- **mock-api-parallel-development:** Frontend uses mock until CP-BACKEND

---

**Skill Owner:** Librarian
**Created:** 2026-07-04
**Status:** ACTIVE
