# MCP Tools — Context Persistence & Goal Drift Prevention

> **Last Updated:** 2026-07-07
> **Status:** Production-Ready (12 tools)
> **Category:** Session Management & Goal Re-Anchoring

This document details the 12 Context Persistence MCP tools designed to prevent goal drift in long-running agent sessions. These tools implement the 6 Solution Patterns from ADR-048.

---

## Table of Contents

1. [Overview](#overview)
2. [The 12 Tools](#the-12-tools)
3. [Session Rituals](#session-rituals)
4. [Troubleshooting](#troubleshooting)

---

## Overview

### The Problem: Goal Drift in Long Sessions

**5 Failure Modes identified:**

1. **Subtask Overfocus** — Agent loses sight of main epic goal
2. **Context Dilution** — Too much information, main goal drowns
3. **Inherited Drift** — DONE outbox messages redirect focus
4. **Long Horizon Loss** — Multi-week epics lose end-state vision
5. **Milestone Blindness** — Agent doesn't recognize completion

### The Solution: 6 Context Persistence Patterns

1. **STATUS.md** — Current state snapshot
2. **.session-state.json** — Cross-session goal recovery
3. **.turn-count** — Context saturation tracking
4. **CHECKPOINTS.md** — Milestone tracking
5. **Goal Re-Anchoring** — Session start context loading
6. **Dense Milestone Feedback** — Explicit epic progress updates

### Context Saturation Thresholds

| Turn Count | Status | Risk | Action Required |
|------------|--------|------|-----------------|
| **0-29** | ✅ OK | Low | Normal operation |
| **30-49** | ⚠️ WARNING | Medium | Re-focus on epic goal |
| **≥50** | 🚨 CRITICAL | High | Re-anchor or reset session |

---

## The 12 Tools

### 1. build_session_start_context

**Purpose:** Load session state + STATUS.md + turn count for goal re-anchoring.

**Usage:** **MANDATORY** at session start (first 3-5 minutes) for ALL terminals.

**Input:**
```typescript
{
  terminal: string  // e.g., "conductor", "backend", "librarian"
}
```

**Output:**
```typescript
{
  terminal: string
  turnCount: number
  status: "ok" | "warning" | "critical"
  thresholds: {
    warning: 30
    critical: 50
  }
  sessionState: {
    epicId: string           // e.g., "EPIC-CUTTING-Q3"
    epicProgress: number     // 0-100
    nextCheckpointId: string // e.g., "CP-KERNEL-FSM"
    lastActiveTask: string   // e.g., "MSG-BACKEND-045"
  } | null
  statusMd: {
    system_status: "operational" | "in_progress" | "paused" | "blocked"
    current_focus: string
    recent_actions: string[]
    next_steps: string[]
  } | null
}
```

**Example:**
```typescript
// Session start ritual (MANDATORY for all terminals)
const context = await mcp__spaceos_knowledge__build_session_start_context({
  terminal: "conductor"
});

console.log(`Turn count: ${context.turnCount} (${context.status})`);

if (context.status === "warning") {
  console.log(`⚠️ Context saturation warning — re-focus on epic goal`);
  console.log(`Epic: ${context.sessionState?.epicId}`);
  console.log(`Progress: ${context.sessionState?.epicProgress}%`);
}

if (context.status === "critical") {
  console.log(`🚨 CRITICAL: Save state and request re-anchoring`);
  // Save current state, reset turn count, or start new session
}
```

**Reference:**
- Implementation: `spaceos-nexus/knowledge-service/src/contextPersistence.ts`
- Pattern: `docs/knowledge/patterns/GOAL_PERSISTENCE_PATTERNS.md` (Solution #5: Goal Re-Anchoring)

---

### 2. get_context_saturation

**Purpose:** Check turn count + threshold status (WARNING >30, CRITICAL >50).

**Usage:** Every 10-15 turns, before major decisions, when goal drift suspected.

**Input:**
```typescript
{
  terminal: string
}
```

**Output:**
```typescript
{
  terminal: string
  turnCount: number
  status: "ok" | "warning" | "critical"
  thresholds: {
    warning: 30
    critical: 50
  }
  recommendation: string  // Context-aware advice
}
```

**Example:**
```typescript
// Periodic saturation check (every 10-15 turns)
const saturation = await mcp__spaceos_knowledge__get_context_saturation({
  terminal: "backend"
});

console.log(`Context: ${saturation.status} (${saturation.turnCount} turns)`);

if (saturation.status === "warning") {
  // Re-read epic goal and STATUS.md
  const state = await mcp__spaceos_knowledge__read_session_state({ terminal: "backend" });
  const status = await mcp__spaceos_knowledge__read_terminal_status_md({ terminal: "backend" });

  console.log(`Refocusing on: ${state.epicId} → ${status.current_focus}`);
}
```

**Reference:**
- Pattern: `docs/knowledge/patterns/GOAL_PERSISTENCE_PATTERNS.md` (Solution #3: Turn Count)

---

### 3. read_session_state

**Purpose:** Read epic ID, progress, checkpoints, last task from `.session-state.json`.

**Usage:** Session start, before major decisions, WARNING saturation.

**Input:**
```typescript
{
  terminal: string
}
```

**Output:**
```typescript
{
  terminal: string
  epicId: string           // e.g., "EPIC-CUTTING-Q3"
  epicName: string         // Human-readable name
  epicProgress: number     // 0-100
  nextCheckpointId: string // e.g., "CP-KERNEL-FSM"
  nextCheckpointName: string
  lastActiveTask: string   // e.g., "MSG-BACKEND-045"
  completedCheckpoints: string[]
} | null
```

**Example:**
```typescript
// Read session state before major decision
const state = await mcp__spaceos_knowledge__read_session_state({
  terminal: "conductor"
});

if (state) {
  console.log(`Epic: ${state.epicName} (${state.epicProgress}% complete)`);
  console.log(`Next checkpoint: ${state.nextCheckpointName}`);
  console.log(`Last task: ${state.lastActiveTask}`);
} else {
  console.log("⚠️ No session state found — starting fresh");
}
```

**Reference:**
- File location: `terminals/<terminal>/.session-state.json`
- Pattern: `docs/knowledge/patterns/GOAL_PERSISTENCE_PATTERNS.md` (Solution #2: Session State)

---

### 4. write_session_state

**Purpose:** Save epic progress to `.session-state.json` for cross-session recovery.

**Usage:** **MANDATORY** at session end, after milestone, before CRITICAL re-anchor.

**Input:**
```typescript
{
  terminal: string
  epic_id?: string
  epic_name?: string
  epic_progress?: number          // 0-100
  next_checkpoint_id?: string
  next_checkpoint_name?: string
  last_active_task?: string
  completed_checkpoints?: string[]
}
```

**Output:**
```typescript
{
  success: boolean
  message: string
}
```

**Example:**
```typescript
// Session end: save state for next session
await mcp__spaceos_knowledge__write_session_state({
  terminal: "backend",
  epic_id: "EPIC-CUTTING-Q3",
  epic_name: "Cutting Module Q3 Delivery",
  epic_progress: 35,
  next_checkpoint_id: "CP-INTEGRATION-TEST",
  next_checkpoint_name: "Integration Test Suite Complete",
  last_active_task: "MSG-BACKEND-045",
  completed_checkpoints: ["CP-KERNEL-FSM", "CP-DOMAIN-MODEL"]
});

console.log("✅ Session state saved for cross-session recovery");
```

**Reference:**
- Pattern: `docs/knowledge/patterns/GOAL_PERSISTENCE_PATTERNS.md` (Solution #2: Session State)

---

### 5. read_terminal_status_md

**Purpose:** Read `STATUS.md` snapshot (current focus, recent actions, next steps).

**Usage:** Session start, before major decisions, when checking focus.

**Input:**
```typescript
{
  terminal: string
}
```

**Output:**
```typescript
{
  terminal: string
  system_status: "operational" | "in_progress" | "paused" | "blocked"
  current_focus: string
  recent_actions: string[]
  next_steps: string[]
  last_updated: string  // ISO 8601 timestamp
} | null
```

**Example:**
```typescript
// Check current focus before new task dispatch
const status = await mcp__spaceos_knowledge__read_terminal_status_md({
  terminal: "frontend"
});

if (status) {
  console.log(`Current focus: ${status.current_focus}`);
  console.log(`Recent actions:`);
  status.recent_actions.forEach(a => console.log(`  - ${a}`));
  console.log(`Next steps:`);
  status.next_steps.forEach(s => console.log(`  - ${s}`));
}
```

**Reference:**
- File location: `terminals/<terminal>/STATUS.md`
- Pattern: `docs/knowledge/patterns/GOAL_PERSISTENCE_PATTERNS.md` (Solution #1: STATUS.md)

---

### 6. write_terminal_status_md

**Purpose:** Update `STATUS.md` snapshot with current state.

**Usage:** **MANDATORY** at session end, after milestone, after major state change.

**Input:**
```typescript
{
  terminal: string
  system_status: "operational" | "in_progress" | "paused" | "blocked"
  current_focus?: string
  recent_actions?: string[]
  next_steps?: string[]
}
```

**Output:**
```typescript
{
  success: boolean
  message: string
}
```

**Example:**
```typescript
// Session end: update STATUS.md snapshot
await mcp__spaceos_knowledge__write_terminal_status_md({
  terminal: "backend",
  system_status: "in_progress",
  current_focus: "MSG-BACKEND-045: Kernel FSM implementation",
  recent_actions: [
    "Completed Kernel FSM state machine (5 states, 12 transitions)",
    "Started integration tests (8/15 passing)",
    "Blocked on Frontend mock API endpoint (/api/orders)"
  ],
  next_steps: [
    "Wait for Frontend mock API completion (MSG-FRONTEND-060)",
    "Continue integration test suite",
    "Review FSM edge cases (invalid transitions)"
  ]
});

console.log("✅ STATUS.md updated for next session");
```

**Reference:**
- Pattern: `docs/knowledge/patterns/GOAL_PERSISTENCE_PATTERNS.md` (Solution #1: STATUS.md)

---

### 7. increment_turn_count

**Purpose:** Increment turn counter for context saturation tracking.

**Usage:** Every 10-15 turns (manual or via hooks).

**Input:**
```typescript
{
  terminal: string
  amount?: number  // Default: 1
}
```

**Output:**
```typescript
{
  success: boolean
  newCount: number
}
```

**Example:**
```typescript
// Increment turn count (typically via hook)
const result = await mcp__spaceos_knowledge__increment_turn_count({
  terminal: "conductor",
  amount: 1
});

console.log(`Turn count: ${result.newCount}`);

// Check saturation after increment
if (result.newCount >= 30) {
  const saturation = await mcp__spaceos_knowledge__get_context_saturation({
    terminal: "conductor"
  });
  console.log(`Saturation: ${saturation.status}`);
}
```

**Reference:**
- File location: `terminals/<terminal>/.turn-count`
- Pattern: `docs/knowledge/patterns/GOAL_PERSISTENCE_PATTERNS.md` (Solution #3: Turn Count)

---

### 8. reset_turn_count

**Purpose:** Reset turn counter to 0 (after session restart or re-anchoring).

**Usage:** Session end (if new session), after CRITICAL re-anchor.

**Input:**
```typescript
{
  terminal: string
}
```

**Output:**
```typescript
{
  success: boolean
  message: string
}
```

**Example:**
```typescript
// Reset turn count after re-anchoring
await mcp__spaceos_knowledge__reset_turn_count({
  terminal: "backend"
});

console.log("✅ Turn count reset to 0 — fresh context window");
```

**Reference:**
- Pattern: `docs/knowledge/patterns/GOAL_PERSISTENCE_PATTERNS.md` (Solution #3: Turn Count)

---

### 9. read_checkpoints_md

**Purpose:** Read `CHECKPOINTS.md` milestone list for epic progress tracking.

**Usage:** Session start, progress check, milestone planning.

**Input:**
```typescript
{
  terminal: string
}
```

**Output:**
```typescript
{
  terminal: string
  checkpoints: Array<{
    date: string
    name: string
    decision: string          // e.g., "GO/NO-GO"
    evaluation_criteria: string[]
    go_actions: string[]
    no_go_actions: string[]
    refs: string[]
    status?: "pending" | "done"
  }>
} | null
```

**Example:**
```typescript
// Read checkpoints at session start
const checkpoints = await mcp__spaceos_knowledge__read_checkpoints_md({
  terminal: "backend"
});

if (checkpoints) {
  console.log("Epic milestones:");
  checkpoints.checkpoints.forEach(cp => {
    const icon = cp.status === "done" ? "✅" : "⏳";
    console.log(`${icon} ${cp.name} (${cp.date})`);
  });
}
```

**Reference:**
- File location: `terminals/<terminal>/CHECKPOINTS.md`
- Pattern: `docs/knowledge/patterns/GOAL_PERSISTENCE_PATTERNS.md` (Solution #4: Checkpoints)

---

### 10. append_checkpoint_to_md

**Purpose:** Add new checkpoint to `CHECKPOINTS.md`.

**Usage:** Milestone planning (Conductor/Root), epic phase transitions.

**Input:**
```typescript
{
  terminal: string
  date: string              // YYYY-MM-DD
  name: string
  decision: string          // e.g., "GO/NO-GO"
  evaluation_criteria: string[]
  go_actions: string[]
  no_go_actions: string[]
  refs?: string[]
}
```

**Output:**
```typescript
{
  success: boolean
  message: string
}
```

**Example:**
```typescript
// Add new checkpoint for milestone
await mcp__spaceos_knowledge__append_checkpoint_to_md({
  terminal: "backend",
  date: "2026-07-10",
  name: "Kernel FSM Complete",
  decision: "GO/NO-GO",
  evaluation_criteria: [
    "All FSM states implemented (5/5)",
    "Unit tests pass (>95% coverage)",
    "Integration with Orchestrator ready (mock API verified)"
  ],
  go_actions: [
    "Proceed to Orchestrator integration (Phase 2)",
    "Dispatch MSG-ORCHESTRATOR-025"
  ],
  no_go_actions: [
    "Fix FSM edge cases (invalid state transitions)",
    "Add missing unit tests (target: 98% coverage)"
  ],
  refs: ["MSG-BACKEND-045", "EPIC-CUTTING-Q3"]
});

console.log("✅ Checkpoint added for milestone tracking");
```

**Reference:**
- Pattern: `docs/knowledge/patterns/GOAL_PERSISTENCE_PATTERNS.md` (Solution #4: Checkpoints)

---

### 11. get_context_files_status

**Purpose:** Get status of all context persistence files for a single terminal.

**Usage:** Diagnostic (Root/Monitor), session health check.

**Input:**
```typescript
{
  terminal: string
}
```

**Output:**
```typescript
{
  terminal: string
  hasStatus: boolean        // STATUS.md exists
  hasSessionState: boolean  // .session-state.json exists
  hasTurnCount: boolean     // .turn-count exists
  hasCheckpoints: boolean   // CHECKPOINTS.md exists
  turnCount: number
  sessionState?: {
    epicId: string
    epicProgress: number
    nextCheckpointId: string
  }
}
```

**Example:**
```typescript
// Check if terminal has proper context setup
const status = await mcp__spaceos_knowledge__get_context_files_status({
  terminal: "conductor"
});

console.log(`Context files for conductor:`);
console.log(`  STATUS.md: ${status.hasStatus ? "✅" : "❌"}`);
console.log(`  .session-state.json: ${status.hasSessionState ? "✅" : "❌"}`);
console.log(`  .turn-count: ${status.hasTurnCount ? "✅" : "❌"}`);
console.log(`  CHECKPOINTS.md: ${status.hasCheckpoints ? "✅" : "❌"}`);

if (!status.hasSessionState) {
  console.warn("⚠️ No session state — goal drift risk!");
}
```

**Reference:**
- Diagnostic tool for Root/Monitor

---

### 12. get_all_context_files_status

**Purpose:** Get status of context persistence files for ALL terminals (system overview).

**Usage:** Root/Monitor diagnostic, system health check.

**Input:** None

**Output:**
```typescript
{
  terminals: Array<{
    terminal: string
    hasStatus: boolean
    hasSessionState: boolean
    hasTurnCount: boolean
    hasCheckpoints: boolean
    turnCount: number
    sessionState?: {
      epicId: string
      epicProgress: number
      nextCheckpointId: string
    }
  }>
}
```

**Example:**
```typescript
// System-wide context persistence health check
const overview = await mcp__spaceos_knowledge__get_all_context_files_status();

console.log("Context persistence status (all terminals):");

overview.terminals.forEach(t => {
  const icon = t.hasSessionState ? "✅" : "⚠️";
  console.log(`${icon} ${t.terminal}: ${t.turnCount} turns`);

  if (t.turnCount > 30) {
    console.warn(`  ⚠️ WARNING: ${t.terminal} approaching saturation (${t.turnCount}/50)`);
  }

  if (!t.hasSessionState) {
    console.warn(`  ⚠️ No session state — goal drift risk`);
  }
});
```

**Reference:**
- System diagnostic for Root/Monitor

---

## Session Rituals

### Session Start (MANDATORY — ALL Terminals)

**Step 1: Load context (first 3-5 minutes)**
```typescript
// 1. Full context load
const context = await mcp__spaceos_knowledge__build_session_start_context({
  terminal: "<your-terminal>"
});

// 2. Check saturation
console.log(`Context: ${context.status} (${context.turnCount} turns)`);

// 3. Review epic goal
if (context.sessionState) {
  console.log(`Epic: ${context.sessionState.epicId} (${context.sessionState.epicProgress}%)`);
  console.log(`Next: ${context.sessionState.nextCheckpointId}`);
}

// 4. Review current focus
if (context.statusMd) {
  console.log(`Focus: ${context.statusMd.current_focus}`);
}
```

**Step 2: Handle saturation**
```typescript
if (context.status === "warning") {
  console.log("⚠️ WARNING: Re-focus on epic goal, avoid subtask drift");
  // Re-read epic + checkpoints
}

if (context.status === "critical") {
  console.log("🚨 CRITICAL: Save state and request re-anchoring");
  // Save state, reset turn count, start new session
}
```

---

### During Session (Every 10-15 Turns)

```typescript
// 1. Increment turn count
await mcp__spaceos_knowledge__increment_turn_count({
  terminal: "<your-terminal>"
});

// 2. Check saturation
const saturation = await mcp__spaceos_knowledge__get_context_saturation({
  terminal: "<your-terminal>"
});

if (saturation.status !== "ok") {
  console.log(`⚠️ ${saturation.status.toUpperCase()}: ${saturation.recommendation}`);
}
```

---

### Session End (MANDATORY — ALL Terminals)

```typescript
// 1. Save STATUS.md
await mcp__spaceos_knowledge__write_terminal_status_md({
  terminal: "<your-terminal>",
  system_status: "in_progress",  // or "operational"
  current_focus: "...",
  recent_actions: [...],
  next_steps: [...]
});

// 2. Save session state
await mcp__spaceos_knowledge__write_session_state({
  terminal: "<your-terminal>",
  epic_id: "...",
  epic_progress: 45,
  next_checkpoint_id: "...",
  last_active_task: "...",
  completed_checkpoints: [...]
});

// 3. Reset turn count (if new session)
await mcp__spaceos_knowledge__reset_turn_count({
  terminal: "<your-terminal>"
});
```

---

## Troubleshooting

### Problem: Goal Drift Detected

**Symptoms:**
- Agent working on subtasks unrelated to epic
- Lost track of main objective
- Optimizing details instead of delivering milestone

**Diagnosis:**
```typescript
const context = await mcp__spaceos_knowledge__build_session_start_context({
  terminal: "<terminal>"
});

// Check turn count
if (context.turnCount > 30) {
  console.log("⚠️ High turn count — goal drift likely");
}

// Check session state
if (!context.sessionState) {
  console.log("⚠️ No session state — no epic anchor");
}
```

**Solution:**
```typescript
// 1. Re-read epic goal
const state = await mcp__spaceos_knowledge__read_session_state({ terminal: "<terminal>" });
console.log(`Main goal: ${state.epicId} → ${state.nextCheckpointId}`);

// 2. Re-read STATUS.md
const status = await mcp__spaceos_knowledge__read_terminal_status_md({ terminal: "<terminal>" });
console.log(`Current focus: ${status.current_focus}`);

// 3. Compare: are they aligned?
// If not → goal drift confirmed → refocus
```

---

### Problem: Context Saturation CRITICAL (≥50 turns)

**Symptoms:**
- Turn count ≥50
- Agent confused, context diluted
- Forgetting earlier decisions

**Solution:**
```typescript
// 1. Save current state
await mcp__spaceos_knowledge__write_session_state({
  terminal: "<terminal>",
  epic_id: state.epicId,
  epic_progress: state.epicProgress,
  next_checkpoint_id: state.nextCheckpointId,
  last_active_task: "<current-task>",
  completed_checkpoints: [...]
});

// 2. Save STATUS.md
await mcp__spaceos_knowledge__write_terminal_status_md({
  terminal: "<terminal>",
  system_status: "in_progress",
  current_focus: "...",
  recent_actions: [...],
  next_steps: [...]
});

// 3. Reset turn count
await mcp__spaceos_knowledge__reset_turn_count({
  terminal: "<terminal>"
});

// 4. Request new session or re-anchoring from Monitor
```

---

### Problem: No Session State Found

**Symptoms:**
- `build_session_start_context` returns `sessionState: null`
- Agent has no epic anchor
- Goal drift risk very high

**Solution:**
```typescript
// 1. Check if .session-state.json exists
const status = await mcp__spaceos_knowledge__get_context_files_status({
  terminal: "<terminal>"
});

if (!status.hasSessionState) {
  console.log("⚠️ No session state file — initializing...");

  // 2. Initialize session state
  await mcp__spaceos_knowledge__write_session_state({
    terminal: "<terminal>",
    epic_id: "<current-epic>",
    epic_progress: 0,
    next_checkpoint_id: "<first-checkpoint>",
    last_active_task: null,
    completed_checkpoints: []
  });
}
```

---

## Performance Metrics

| Tool | Target Response | Actual (Avg) | Status |
|------|----------------|--------------|--------|
| build_session_start_context | <50ms | ~35ms | ✅ Excellent |
| get_context_saturation | <30ms | ~18ms | ✅ Excellent |
| read_session_state | <20ms | ~12ms | ✅ Excellent |
| write_session_state | <30ms | ~22ms | ✅ Excellent |
| read_terminal_status_md | <20ms | ~15ms | ✅ Excellent |
| write_terminal_status_md | <30ms | ~25ms | ✅ Excellent |
| increment_turn_count | <10ms | ~5ms | ✅ Excellent |
| reset_turn_count | <10ms | ~6ms | ✅ Excellent |
| read_checkpoints_md | <20ms | ~14ms | ✅ Excellent |
| append_checkpoint_to_md | <30ms | ~28ms | ✅ Excellent |
| get_context_files_status | <20ms | ~16ms | ✅ Excellent |
| get_all_context_files_status | <50ms | ~42ms | ✅ Excellent |

**Average:** ~20ms (all well under SLA)

---

## References

- **Theory:** `docs/knowledge/patterns/GOAL_PERSISTENCE_PATTERNS.md` — 5 failure modes, 6 solution patterns
- **File Structure:** `docs/knowledge/patterns/TERMINAL_CONTEXT_PERSISTENCE_FILES.md` — Detailed file formats
- **Implementation:** `spaceos-nexus/knowledge-service/src/contextPersistence.ts` — Source code
- **ADR:** ADR-048 — Project Tiered Context & Goal Persistence

---

**Last Updated:** 2026-07-07
**Maintainer:** Librarian
**Status:** ✅ Production-Ready (12 tools)
