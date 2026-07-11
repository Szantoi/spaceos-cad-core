# MCP Tools — Usage Examples & Tutorials

> **Real-world scenarios** demonstrating MCP tool usage across terminals.
> **Last Updated:** 2026-07-07
> **Maintainer:** Librarian

---

## Table of Contents

1. [Scenario 1: Conductor Daily Routine](#scenario-1-conductor-daily-routine)
2. [Scenario 2: Frontend Component Development](#scenario-2-frontend-component-development)
3. [Scenario 3: Architect Pattern Recommendation](#scenario-3-architect-pattern-recommendation)
4. [Scenario 4: Explorer Research Handoff](#scenario-4-explorer-research-handoff)
5. [Scenario 5: Session Management (All Terminals)](#scenario-5-session-management-all-terminals)

---

## Scenario 1: Conductor Daily Routine

**Context:** Conductor starts morning session to check terminal status and dispatch tasks.

### Step 1: Session Start (Context Persistence)

```typescript
// 1. Load session context (MANDATORY)
const context = await mcp__spaceos_knowledge__build_session_start_context({
  terminal: "conductor"
});

console.log(`Turn count: ${context.turnCount} (${context.status})`);

if (context.status === "warning") {
  console.log("⚠️ Context saturation warning — refocusing on epic goal");
  console.log(`Epic: ${context.sessionState?.epicId} (${context.sessionState?.epicProgress}%)`);
}

// 2. Check current focus
if (context.statusMd) {
  console.log(`Current focus: ${context.statusMd.current_focus}`);
  console.log("Recent actions:", context.statusMd.recent_actions);
  console.log("Next steps:", context.statusMd.next_steps);
}
```

### Step 2: Terminal Status Check

```typescript
// Check all terminal status
const status = await mcp__spaceos_knowledge__get_terminal_status_aggregate({
  format: "summary"
});

console.log("Terminal Status:");
console.log("Working:", status.summary.working);
console.log("Idle:", status.summary.idle);
console.log("Blocked:", status.summary.blocked);

if (status.summary.warnings.length > 0) {
  console.log("⚠️ Alerts:");
  status.summary.warnings.forEach(w => console.log(`  - ${w}`));
}

// Identify terminals ready for task dispatch
const readyTerminals = status.summary.idle;
console.log(`✅ Ready for dispatch: ${readyTerminals.join(", ")}`);
```

### Step 3: Epic Dependency Resolution

```typescript
// Check dependencies before dispatching Phase 2 tasks
const deps = await mcp__spaceos_knowledge__resolve_dependencies({
  epic_id: "EPIC-CUTTING-Q3",
  check_blockers: true
});

if (deps.blockedBy.length > 0) {
  console.log(`⚠️ Epic blocked by: ${deps.blockedBy.join(", ")}`);
  // Skip dispatch, wait for blocking epics
  return;
}

// Dispatch ready tasks
deps.readyTasks.forEach(task => {
  if (readyTerminals.includes(task.terminal)) {
    console.log(`✅ Dispatching: ${task.id} → ${task.terminal}`);
    // Create inbox message using MCP or manual file write
  }
});

// Track blocked tasks
deps.blockedTasks.forEach(task => {
  console.log(`⏳ ${task.id} blocked by: ${task.blockedBy.join(", ")}`);
});
```

### Step 4: Session End (Context Persistence)

```typescript
// Save session state (MANDATORY)
await mcp__spaceos_knowledge__write_session_state({
  terminal: "conductor",
  epic_id: "EPIC-CUTTING-Q3",
  epic_progress: 50,
  next_checkpoint_id: "CP-INTEGRATION-TEST",
  last_active_task: "MSG-BACKEND-045"
});

// Save STATUS.md (MANDATORY)
await mcp__spaceos_knowledge__write_terminal_status_md({
  terminal: "conductor",
  system_status: "operational",
  current_focus: "JoineryTech Phase 2 dispatch complete",
  recent_actions: [
    "Checked terminal status (3 idle, 2 working, 0 blocked)",
    "Resolved epic dependencies for EPIC-CUTTING-Q3",
    "Dispatched 2 tasks to backend, 1 task to frontend"
  ],
  next_steps: [
    "Wait for backend DONE (MSG-BACKEND-045)",
    "Monitor frontend progress",
    "Prepare Phase 3 tasks"
  ]
});

// Reset turn count (if new session)
await mcp__spaceos_knowledge__reset_turn_count({
  terminal: "conductor"
});

console.log("✅ Session state saved — ready for next session");
```

---

## Scenario 2: Frontend Component Development

**Context:** Frontend terminal receives task to implement cost budget tracking widget.

### Step 1: Session Start

```typescript
// Load session context
const context = await mcp__spaceos_knowledge__build_session_start_context({
  terminal: "frontend"
});

// Check saturation
if (context.turnCount > 30) {
  console.log("⚠️ High turn count — focus on main task");
}
```

### Step 2: Component Scaffolding

```typescript
// Generate React component with tests
const result = await mcp__spaceos_knowledge__scaffold_component({
  component_type: "react_component",
  name: "CostBudgetWidget",
  output_dir: "client/src/components/",
  description: "Cost budget tracking widget with variance analysis",
  with_tests: true,
  with_storybook: true
});

console.log("Created files:", result.filesCreated);
// Output:
// - client/src/components/CostBudgetWidget.tsx
// - client/src/components/CostBudgetWidget.module.css
// - client/src/components/__tests__/CostBudgetWidget.test.tsx
// - client/src/components/CostBudgetWidget.stories.tsx

console.log("Next steps:", result.nextSteps);
// Output:
// - Review generated component
// - Add business logic
// - Run tests: npm test CostBudgetWidget
```

### Step 3: Generate Hook for Data Fetching

```typescript
// Generate React Query hook
const hookResult = await mcp__spaceos_knowledge__scaffold_component({
  component_type: "react_hook",
  name: "useCostBudget",
  api_spec: "openapi.yaml#/components/schemas/CostBudget",
  output_dir: "client/src/hooks/",
  with_tests: true
});

console.log("Hook created:", hookResult.filesCreated);
// Output:
// - client/src/hooks/useCostBudget.ts
// - client/src/hooks/__tests__/useCostBudget.test.ts
```

### Step 4: Integrate & Test

```bash
# Review generated files
cat client/src/components/CostBudgetWidget.tsx
cat client/src/hooks/useCostBudget.ts

# Add business logic (customize as needed)
# ...

# Run tests
npm test CostBudgetWidget
npm test useCostBudget

# Run Storybook (verify UI)
npm run storybook
```

### Step 5: Session End

```typescript
// Save STATUS.md
await mcp__spaceos_knowledge__write_terminal_status_md({
  terminal: "frontend",
  system_status: "in_progress",
  current_focus: "MSG-FRONTEND-060: Cost Budget Widget implementation",
  recent_actions: [
    "Scaffolded CostBudgetWidget component + tests",
    "Generated useCostBudget hook with React Query",
    "Implemented variance analysis logic",
    "Tests passing (12/12)"
  ],
  next_steps: [
    "Integrate with KPI Strip layout",
    "Add responsive CSS for mobile",
    "Create Storybook story for demo"
  ]
});

// Save session state
await mcp__spaceos_knowledge__write_session_state({
  terminal: "frontend",
  epic_id: "EPIC-KONTROLLING-Q3",
  epic_progress: 65,
  next_checkpoint_id: "CP-FRONTEND-COMPLETE",
  last_active_task: "MSG-FRONTEND-060"
});
```

---

## Scenario 3: Architect Pattern Recommendation

**Context:** Architect receives consultation request for new HR attendance feature.

### Step 1: Session Start

```typescript
// Load session context
const context = await mcp__spaceos_knowledge__build_session_start_context({
  terminal: "architect"
});
```

### Step 2: Pattern Matching

```typescript
// Search for existing pattern
const pattern = await mcp__spaceos_knowledge__match_domain_pattern({
  description: "Track employee attendance with shift management and PTO",
  domain: "hr"
});

console.log(`Pattern: ${pattern.pattern} (confidence: ${pattern.confidence * 100}%)`);
// Output: "HR Attendance FSM (confidence: 89%)"

console.log("Recommendations:");
pattern.recommendations.forEach(r => console.log(`  - ${r}`));
// Output:
// - Use AttendanceAggregate pattern (FSM-based)
// - FSM states: Scheduled → CheckedIn → CheckedOut
// - Add PTO event handling (Approved → Canceled)
// - Integrate with shift schedule (read-only dependency)

console.log("References:");
pattern.references.forEach(r => console.log(`  - ${r}`));
// Output:
// - docs/knowledge/patterns/HR_PATTERNS.md
// - spaceos-modules-hr/Domain/Aggregates/AttendanceAggregate.cs

console.log("ADRs:", pattern.adrRefs);
// Output: ["ADR-056"]
```

### Step 3: Review Pattern Details

```bash
# Read pattern documentation
cat docs/knowledge/patterns/HR_PATTERNS.md

# Read ADR
cat docs/architecture/decisions/ADR-056-joinerytech-hr-domain-model.md

# Review example code
cat spaceos-modules-hr/Domain/Aggregates/AttendanceAggregate.cs
```

### Step 4: Provide Recommendation

```typescript
// Prepare recommendation for backend
const recommendation = {
  pattern: "HR Attendance FSM",
  confidence: 0.89,
  fsm_states: ["Scheduled", "CheckedIn", "CheckedOut", "Absent", "OnPTO"],
  events: [
    "AttendanceScheduled",
    "EmployeeCheckedIn",
    "EmployeeCheckedOut",
    "AttendanceMarkedAbsent",
    "PTOApproved"
  ],
  aggregate_root: "AttendanceAggregate",
  references: [
    "docs/knowledge/patterns/HR_PATTERNS.md",
    "spaceos-modules-hr/Domain/Aggregates/AttendanceAggregate.cs",
    "ADR-056"
  ]
};

console.log("Recommendation prepared for backend terminal");
```

### Step 5: Session End

```typescript
// Save session state
await mcp__spaceos_knowledge__write_terminal_status_md({
  terminal: "architect",
  system_status: "operational",
  current_focus: "MSG-ARCHITECT-068: HR Attendance pattern recommendation",
  recent_actions: [
    "Searched HR domain patterns (match: 89% confidence)",
    "Reviewed AttendanceAggregate example code",
    "Prepared FSM recommendation (5 states, 5 events)"
  ],
  next_steps: [
    "Wait for backend implementation",
    "Review backend DONE for compliance"
  ]
});
```

---

## Scenario 4: Explorer Research Handoff

**Context:** Explorer completes JoineryTech research and hands off to Librarian for synthesis.

### Step 1: Session Start

```typescript
// Load session context
const context = await mcp__spaceos_knowledge__build_session_start_context({
  terminal: "explorer"
});
```

### Step 2: Complete Research

```bash
# Research findings saved to outbox
cat terminals/explorer/outbox/2026-07-07_008_joinerytech-research.md
cat terminals/explorer/outbox/2026-07-07_009_task-audit.md
```

### Step 3: Context Transfer to Librarian

```typescript
// Transfer research context to Librarian
const result = await mcp__spaceos_knowledge__transfer_session_context({
  from_terminal: "explorer",
  to_terminal: "librarian",
  context_type: "research_summary",
  include_files: [
    "terminals/explorer/outbox/2026-07-07_008_joinerytech-research.md",
    "terminals/explorer/outbox/2026-07-07_009_task-audit.md"
  ],
  summary: "JoineryTech Phase 1-3 research complete. 3 domain models + 7 workflow patterns identified. Synthesis recommended."
});

console.log("✅ Context transferred:", result.summary);
// Output: "Transferred 2 files (18KB) to librarian"

console.log("Inbox file created:", result.inboxFile);
// Output: "terminals/librarian/inbox/2026-07-07_004_context-transfer-explorer.md"
```

### Step 4: Verify Librarian Inbox

```bash
# Verify inbox message created
ls -la terminals/librarian/inbox/2026-07-07_004_context-transfer-explorer.md

# Check content
cat terminals/librarian/inbox/2026-07-07_004_context-transfer-explorer.md
```

### Step 5: Session End

```typescript
// Save session state
await mcp__spaceos_knowledge__write_terminal_status_md({
  terminal: "explorer",
  system_status: "operational",
  current_focus: "MSG-EXPLORER-008: JoineryTech research complete",
  recent_actions: [
    "Completed JoineryTech research (188 tasks analyzed)",
    "Identified 3 domain models + 7 workflow patterns",
    "Transferred context to Librarian (2 files, 18KB)"
  ],
  next_steps: [
    "Await Librarian synthesis",
    "Review next research task from Conductor"
  ]
});

// Save session state
await mcp__spaceos_knowledge__write_session_state({
  terminal: "explorer",
  epic_id: "EPIC-JOINERYTECH-Q3",
  epic_progress: 100,
  next_checkpoint_id: null,
  last_active_task: "MSG-EXPLORER-008",
  completed_checkpoints: ["CP-RESEARCH-COMPLETE"]
});
```

---

## Scenario 5: Session Management (All Terminals)

**Context:** Universal session ritual for all terminals (MANDATORY).

### Session Start Ritual (MANDATORY)

```typescript
// STEP 1: Load full session context
const context = await mcp__spaceos_knowledge__build_session_start_context({
  terminal: "<your-terminal>"  // conductor, backend, frontend, etc.
});

// STEP 2: Check saturation status
console.log(`Context: ${context.status} (${context.turnCount} turns)`);

if (context.status === "warning") {
  console.log("⚠️ WARNING: Re-focus on epic goal, avoid subtask drift");
}

if (context.status === "critical") {
  console.log("🚨 CRITICAL: Save state and request re-anchoring");
  // Immediate action required!
}

// STEP 3: Review epic goal (if available)
if (context.sessionState) {
  console.log(`Epic: ${context.sessionState.epicId} (${context.sessionState.epicProgress}%)`);
  console.log(`Next checkpoint: ${context.sessionState.nextCheckpointId}`);
  console.log(`Last task: ${context.sessionState.lastActiveTask}`);
}

// STEP 4: Review current focus (if available)
if (context.statusMd) {
  console.log(`Current focus: ${context.statusMd.current_focus}`);
  console.log("Recent actions:", context.statusMd.recent_actions);
  console.log("Next steps:", context.statusMd.next_steps);
}
```

### During Session (Every 10-15 Turns)

```typescript
// STEP 1: Increment turn count
await mcp__spaceos_knowledge__increment_turn_count({
  terminal: "<your-terminal>"
});

// STEP 2: Check saturation
const saturation = await mcp__spaceos_knowledge__get_context_saturation({
  terminal: "<your-terminal>"
});

console.log(`Saturation: ${saturation.status} (${saturation.turnCount} turns)`);

if (saturation.status !== "ok") {
  console.log(`⚠️ ${saturation.recommendation}`);

  // If WARNING: Re-read epic goal
  if (saturation.status === "warning") {
    const state = await mcp__spaceos_knowledge__read_session_state({
      terminal: "<your-terminal>"
    });
    console.log(`Refocusing on: ${state.epicId}`);
  }

  // If CRITICAL: Save state and re-anchor
  if (saturation.status === "critical") {
    // Trigger session state save (see Session End below)
  }
}
```

### Session End Ritual (MANDATORY)

```typescript
// STEP 1: Save STATUS.md snapshot
await mcp__spaceos_knowledge__write_terminal_status_md({
  terminal: "<your-terminal>",
  system_status: "operational",  // "operational" | "in_progress" | "paused" | "blocked"
  current_focus: "MSG-XXX-YYY: Brief task description",
  recent_actions: [
    "Action 1 (specific result)",
    "Action 2 (specific result)",
    "Action 3 (specific result)"
  ],
  next_steps: [
    "Next step 1",
    "Next step 2",
    "Next step 3"
  ]
});

// STEP 2: Save session state
await mcp__spaceos_knowledge__write_session_state({
  terminal: "<your-terminal>",
  epic_id: "EPIC-XXX-QX",
  epic_progress: 45,  // 0-100
  next_checkpoint_id: "CP-XXX",
  last_active_task: "MSG-XXX-YYY",
  completed_checkpoints: ["CP-A", "CP-B"]
});

// STEP 3: Reset turn count (if new session starting)
await mcp__spaceos_knowledge__reset_turn_count({
  terminal: "<your-terminal>"
});

console.log("✅ Session state saved — ready for next session");
```

---

## Quick Reference

### Context Persistence (ALL Terminals)

| Ritual | Tools | When |
|--------|-------|------|
| **Session Start** | `build_session_start_context` | **MANDATORY** (first 3-5 min) |
| **Periodic Check** | `increment_turn_count` + `get_context_saturation` | Every 10-15 turns |
| **Session End** | `write_session_state` + `write_terminal_status_md` + `reset_turn_count` | **MANDATORY** (last 5 min) |

### Phase 1 Infrastructure (Specific Terminals)

| Terminal | Tool | When |
|----------|------|------|
| **Conductor** | `get_terminal_status_aggregate` | Daily morning, before dispatch |
| **Conductor** | `resolve_dependencies` | Epic planning, phase dispatch |
| **Conductor/Explorer** | `transfer_session_context` | Cross-terminal handoff |
| **Frontend** | `scaffold_component` | Component/hook generation |
| **Architect** | `match_domain_pattern` | Pattern search, feature planning |

---

**Last Updated:** 2026-07-07
**Maintainer:** Librarian
**Status:** ✅ Complete (5 scenarios documented)
