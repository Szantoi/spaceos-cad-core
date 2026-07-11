# ADR Decision Template

## Purpose

Provide a **structured template for documenting Architecture Decision Records (ADRs)** that capture why architectural decisions were made, what alternatives were considered, and what consequences resulted. This skill ensures consistent ADR quality across all terminals and preserves decision context for future reference.

**ROI:** 30-minute ADR writing time (vs 2+ hours unstructured), prevents decision re-litigation

---

## When to Use

Trigger this skill when:
- Making a cross-module architectural decision (API contract, event schema, database design)
- Choosing between multiple valid approaches (technology selection, pattern choice)
- Resolving technical debt with long-term impact
- Documenting consensus from multi-terminal debate

**DO NOT use** for:
- Code-level decisions (local variable naming, minor refactoring)
- Temporary workarounds (mark as TODO instead)
- Implementation details (document in code comments)

---

## Prerequisites

**Inputs:**
- Decision trigger (problem statement)
- Alternatives considered (2+ options)
- Decision criteria (performance, cost, complexity, maintainability)
- Stakeholders involved (terminals, roles)

**Tools:**
- ADR numbering (check `docs/knowledge/architecture/ADR_CATALOGUE.md` for next number)
- Markdown editor

---

## Step-by-Step

### Step 1: Choose ADR Number (2 min)

**Check existing ADRs:**
```bash
ls -1 docs/architecture/decisions/ | grep -oP 'ADR-\d+' | sort -V | tail -1
```

**Increment:** If last ADR is `ADR-058`, new ADR is `ADR-059`

**File naming:**
```
docs/architecture/decisions/ADR-<NNN>-<slug>.md
```

Example: `ADR-059-joinerytech-crm-domain-model.md`

### Step 2: Fill Template (20-30 min)

**Template structure:**

```markdown
# ADR-<NNN>: <Title>

**Status:** Proposed | Accepted | Deprecated | Superseded by ADR-XXX
**Date:** YYYY-MM-DD
**Deciders:** [Terminal names or roles]
**Epic:** EPIC-ID (if applicable)

---

## Context

[2-3 paragraphs explaining the problem]

**Problem Statement:**
- What architectural challenge needs to be solved?
- What constraints exist (5 Golden Rules, existing architecture)?
- What's the impact if we don't decide?

**Background:**
- Current state of the system
- Relevant prior decisions (link to ADRs)
- Technical debt or pain points

---

## Decision Drivers

[Prioritized list of factors influencing the decision]

1. **[Driver 1]** — [Why it matters]
2. **[Driver 2]** — [Why it matters]
3. **[Driver 3]** — [Why it matters]

Example:
1. **5 Golden Rules Compliance** — Data → Rules → Geometry must be preserved
2. **Performance** — CRM must handle 1000+ leads without pagination lag
3. **Maintainability** — Domain model must be clear to new developers

---

## Considered Options

### Option 1: [Name]

**Description:**
[1-2 paragraphs explaining this approach]

**Pros:**
- ✅ [Benefit 1]
- ✅ [Benefit 2]

**Cons:**
- ❌ [Drawback 1]
- ❌ [Drawback 2]

**Implementation Cost:** [Time estimate, complexity]

**Example:**
```typescript
// Code snippet showing this approach
```

---

### Option 2: [Name]

[Same structure as Option 1]

---

### Option 3: [Name]

[Same structure as Option 1]

---

## Decision Outcome

**Chosen Option:** Option [N] — [Name]

**Rationale:**
[2-3 paragraphs explaining WHY this option was chosen]

- Meets decision driver #1 by [...]
- Addresses concern X better than Option Y because [...]
- Accepted trade-off: [what we're giving up]

**Decision Date:** YYYY-MM-DD
**Approved By:** [Terminal names or roles]

---

## Consequences

### Positive Consequences

- ✅ [Benefit 1 realized]
- ✅ [Benefit 2 realized]

### Negative Consequences

- ❌ [Trade-off 1 accepted]
- ❌ [Technical debt 2 incurred]

### Neutral Consequences

- ℹ️ [Additional work required]
- ℹ️ [Learning curve for team]

---

## Implementation Plan

**Phase 1: [Name] (Timeline)**
- [ ] Task 1
- [ ] Task 2

**Phase 2: [Name] (Timeline)**
- [ ] Task 3
- [ ] Task 4

**Testing:**
- [ ] Unit tests for [component]
- [ ] Integration tests for [workflow]

**Migration (if applicable):**
- [ ] Migrate existing [data/code]
- [ ] Rollback plan: [how to revert]

---

## Validation

**Success Metrics:**
- [Metric 1]: Target vs Actual
- [Metric 2]: Target vs Actual

**Review Schedule:**
- [Date 1]: Check metric 1
- [Date 2]: Re-evaluate decision if [condition]

---

## References

- ADR-XXX: [Related decision]
- [External resource]: [URL or document]
- EPIC-XXX: [Epic triggering this decision]
- [Code location]: [Path to implementation]

---

**Author:** [Terminal name]
**Last Updated:** YYYY-MM-DD
```

### Step 3: Add to ADR Catalogue (5 min)

**Update `docs/knowledge/architecture/ADR_CATALOGUE.md`:**

```markdown
## ADR-059: JoineryTech CRM Domain Model

**Status:** Accepted
**Date:** 2026-07-04
**Epic:** EPIC-JT-CRM

**Decision:** Use FSM-based aggregates (Lead, Opportunity) with PostgreSQL RLS for tenant isolation.

**Key Drivers:**
- 5 Golden Rules compliance (FSM state transitions)
- Multi-tenant security (RLS policies)
- Domain-Driven Design (aggregate boundaries)

**Alternatives Considered:**
- Anemic domain model with service layer (rejected: violates DDD)
- NoSQL document store (rejected: RLS not supported)

**Consequences:**
- ✅ Clear state transitions prevent invalid data
- ❌ PostgreSQL RLS adds query complexity

**References:**
- Full ADR: [ADR-059-joinerytech-crm-domain-model.md](../architecture/decisions/ADR-059-joinerytech-crm-domain-model.md)
```

### Step 4: Link to Related Documents (5 min)

**Update references in:**
- Task specification (if ADR triggered by epic)
- Knowledge patterns (if ADR introduces new pattern)
- Terminal CONTEXT.md (if ADR affects terminal workflow)

---

## Templates for Common Decision Types

### Technology Selection ADR

**Use when:** Choosing between libraries, frameworks, or tools

**Decision Drivers:**
- Community support (GitHub stars, active maintainers)
- License compatibility (MIT, Apache 2.0)
- Performance benchmarks
- Learning curve for team

**Example:** ADR-034: React Query vs SWR for server state

---

### API Design ADR

**Use when:** Defining REST endpoints, GraphQL schema, or event contracts

**Decision Drivers:**
- 5 Golden Rules compliance (Data → Rules → Geometry)
- OpenAPI 3.1 compatibility (code generation)
- Client needs (Frontend, Orchestrator)
- Versioning strategy

**Example:** ADR-042: Kernel parametric product API contract

---

### Database Schema ADR

**Use when:** Adding tables, changing RLS policies, or migration strategy

**Decision Drivers:**
- Multi-tenant isolation (RLS effectiveness)
- Query performance (index strategy)
- Data integrity (foreign keys, constraints)
- Migration rollback plan

**Example:** ADR-044: Multi-tenant RLS architecture

---

### Pattern Adoption ADR

**Use when:** Introducing CQRS, Event Sourcing, FSM, or architectural pattern

**Decision Drivers:**
- Problem fit (is this pattern appropriate?)
- Team familiarity (learning curve)
- Complexity vs benefit trade-off
- Long-term maintainability

**Example:** ADR-054: JoineryTech CRM domain model (FSM aggregates)

---

## Error Handling

### Common Issues

**1. Too many options (analysis paralysis):**
```
5+ alternatives considered, decision takes 3+ hours
```

**Fix:** Limit to 3 options max, use decision matrix to eliminate quickly

**2. Missing consequences section:**
```
ADR approved but no follow-up tasks created
```

**Fix:** Always fill "Implementation Plan" with concrete tasks

**3. Vague decision drivers:**
```
"Performance" without benchmarks
```

**Fix:** Quantify drivers (e.g., "Response time <200ms for 95th percentile")

**4. No rollback plan:**
```
Decision irreversible, causes blocker later
```

**Fix:** Document rollback in "Migration" section

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **ADR Writing Time** | <30 min | Developer time tracking |
| **Decision Re-litigation** | 0 cases | ADR referenced to prevent re-debate |
| **Consequences Accuracy** | ≥80% | Actual vs predicted outcomes |
| **Implementation Completion** | 100% | Tasks in "Implementation Plan" done |

---

## Real-World Example

### ADR-058: JoineryTech Integration Architecture

**Step-by-step creation:**

**1. Trigger:** EPIC-JT-CRM requires decision on state management, authentication, and API integration

**2. Alternatives Considered:**
- Option 1: localStorage + JWT in localStorage → Security risk
- Option 2: TanStack Query + HttpOnly cookie → Chosen
- Option 3: Redux + sessionStorage → Overcomplicated

**3. Decision Drivers:**
- 5 Golden Rules compliance (server-state authority)
- Security (OWASP Top 10)
- Developer experience (TanStack Query DevTools)

**4. Writing Time:** 25 minutes (using this skill template)

**5. Outcome:**
- Frontend unblocked (clear state management pattern)
- Security approved (HttpOnly cookie)
- 8 gaps identified (contract-first workflow needed)

**6. Follow-up:**
- Created ADR-058-joinerytech-integration-architecture.md
- Added to ADR_CATALOGUE.md
- Linked from EPIC-JT-CRM task spec

---

## Related Skills

- **contract-first-development-workflow:** ADRs for API design decisions
- **checkpoint-coordination-workflow:** ADRs for multi-team epic consensus
- **fsm-aggregate-generator:** ADRs for domain model design

---

## Maintenance Notes

**When to update this skill:**
- New ADR type discovered (add template section)
- Decision drivers changed (e.g., new compliance requirement)
- Template format improved (community best practices)

**ADR versioning:**
- Never delete ADRs (mark as "Superseded by ADR-XXX")
- Link superseding ADR in "Status" field

---

**Skill Owner:** Librarian
**Created:** 2026-07-04
**Status:** ACTIVE — Use for all architectural decisions
