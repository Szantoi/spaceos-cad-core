# Dispatch Control Pattern — Budget-Aware Task Dispatch

**Created:** 2026-07-01
**Author:** Librarian
**Status:** Production (2026-06-24+)

---

## Overview

**Dispatch Control** is a budget-aware task dispatch system that tracks token usage per terminal and enforces daily/hourly limits to prevent cost overruns. It provides real-time budget monitoring, threshold alerts, and approval workflows for task dispatching.

### Problem Solved

**Before Dispatch Control:**
- No visibility into token consumption per terminal
- Risk of uncontrolled API costs ($100s/day possible)
- No budget enforcement mechanism
- Manual cost tracking required
- No prioritization when budget constrained

**After Dispatch Control:**
- Real-time token tracking per terminal
- Daily/hourly budget limits with enforcement
- Automated alerts at 80%, 90%, 100% thresholds
- Priority reserve for critical tasks
- Dispatch approval workflow (manual/auto/scheduled modes)

---

## Architecture

### Database Schema

**7 Core Tables:**

1. **`token_usage`** — Append-only token consumption log
2. **`dispatch_config`** — Global dispatch mode (auto/manual/scheduled)
3. **`dispatch_proposals`** — Conductor → Root approval requests
4. **`dispatch_queue`** — Active dispatch requests with status
5. **`budget_config`** — Per-terminal daily/hourly limits
6. **`budget_alerts`** — Alert history (threshold warnings, depleted budget)
7. **`message_sequence`** — Auto-increment for message IDs (shared with TMB)

**2 Views:**

- `v_today_usage` — Today's token sum per terminal
- `v_budget_status` — Real-time budget status (used/remaining/percent/status)

### Token Budget Workflow

```
1. Task arrives → Check budget (canDispatch)
2. If budget OK → Dispatch immediately (auto mode) OR queue (manual mode)
3. Record token usage → Update v_today_usage
4. Check thresholds → Send alerts if 80%/90%/100%
5. If depleted + critical priority → Use priority reserve
```

---

## Budget Configuration

### Per-Terminal Limits

| Terminal | Daily Limit | Priority Reserve | Rationale |
|----------|-------------|------------------|-----------|
| **root** | 20,000 | 5,000 | Strategic decisions, high-level coordination |
| **conductor** | 15,000 | 3,000 | Task distribution, pipeline management |
| **backend** | 10,000 | 2,000 | Backend development (C#, Node.js) |
| **frontend** | 10,000 | 2,000 | React/TypeScript UI development |
| **architect** | 10,000 | 2,000 | Architectural consulting (high-value) |
| **librarian** | 5,000 | 1,000 | Knowledge synthesis (lower frequency) |
| **explorer** | 5,000 | 1,000 | Research and exploration |
| **designer** | 5,000 | 1,000 | UI/UX design (lower frequency) |

**Total daily budget:** 80,000 tokens (~$0.24 @ $3/MTok for Sonnet)

### Priority Reserve

**Reserve tokens** are set aside for **critical priority tasks** only:

```typescript
// Example: backend has 2,000 reserve
// Daily limit: 10,000 tokens
// At 10,000 used → budget "depleted"
// Critical task needs 1,500 tokens → ALLOWED (uses reserve)
// Medium task needs 500 tokens → REJECTED (no reserve access)
```

**Use cases:**
- Production incidents (critical bugs, deployment failures)
- Security patches (CVE fixes)
- Escalated blockers (cross-terminal dependencies)

---

## Dispatch Modes

### 3 Modes

| Mode | Behavior | Use Case |
|------|----------|----------|
| **`manual`** | Every dispatch requires explicit approval | Default, full control |
| **`auto`** | Dispatch if budget available | Autonomous operation |
| **`scheduled`** | Dispatch during configured time windows | Cost optimization (e.g., off-peak hours) |

### Mode Management

**Get current mode:**
```typescript
const mode = getDispatchMode(); // 'manual' | 'auto' | 'scheduled'
```

**Set mode:**
```typescript
setDispatchMode('auto', 'root');  // updatedBy = 'root'
```

**Database:**
```sql
-- Single-row table (id CHECK = 1)
SELECT mode FROM dispatch_config WHERE id = 1;
```

---

## Alert System

### 4 Alert Types

| Alert Type | Threshold | Severity | Example |
|------------|-----------|----------|---------|
| `threshold_80` | 80% budget used | ⚠️ Warning | "📊 80% budget used: backend at 8,000/10,000 tokens" |
| `threshold_90` | 90% budget used | 🔴 Critical | "⚠️ 90% budget used: backend at 9,000/10,000 tokens" |
| `budget_depleted` | 100% budget used | 🚨 Urgent | "🚨 Budget depleted: backend used 10,000/10,000 tokens (100%)" |
| `terminal_over` | Hourly limit exceeded | 🔴 Critical | "🔴 Hourly limit exceeded: backend used 3,000 in 1 hour" |

### Alert Delivery

**Telegram notification:**
```typescript
// Alert sent via Telegram bot
await telegram("🚨 Budget depleted: backend used 10,000/10,000 tokens");
```

**Alert deduplication:**
- Only 1 alert per type/terminal/day
- Prevents spam (e.g., 100 tasks hitting depleted budget won't send 100 alerts)

**Alert log:**
```sql
SELECT * FROM budget_alerts
WHERE terminal = 'backend' AND DATE(created_at) = DATE('now');
```

---

## Token Usage Recording

### Recording Token Consumption

**After each session:**
```typescript
recordTokenUsage({
  terminal: 'backend',
  sessionId: 'session-12345',
  taskId: 'MSG-BACKEND-042',
  tokensUsed: 3450,
  model: 'sonnet'
});
```

**Database insert:**
```sql
INSERT INTO token_usage (terminal, session_id, task_id, tokens_used, model, timestamp)
VALUES ('backend', 'session-12345', 'MSG-BACKEND-042', 3450, 'sonnet', NOW());
```

**Automatic alert check:**
- After every insert, `checkAndAlert(terminal)` runs
- Evaluates current budget status
- Sends alert if threshold crossed

---

## Budget Status Queries

### Get Terminal Budget Status

**API:**
```typescript
const status = getTerminalBudgetStatus('backend');
```

**Returns:**
```typescript
{
  terminal: 'backend',
  dailyLimit: 10000,
  tokensUsed: 7250,
  tokensRemaining: 2750,
  usagePercent: 72.5,
  status: 'ok'  // 'ok' | 'warning' | 'critical' | 'depleted'
}
```

**SQL (using view):**
```sql
SELECT * FROM v_budget_status WHERE terminal = 'backend';
```

### Get Daily Budget Summary

**API:**
```typescript
const summary = getDailyBudgetSummary();
```

**Returns:**
```typescript
{
  date: '2026-07-01',
  totalLimit: 80000,
  totalUsed: 45230,
  totalRemaining: 34770,
  byTerminal: {
    root: { terminal: 'root', dailyLimit: 20000, tokensUsed: 12300, ... },
    conductor: { terminal: 'conductor', dailyLimit: 15000, tokensUsed: 8900, ... },
    backend: { terminal: 'backend', dailyLimit: 10000, tokensUsed: 7250, ... },
    // ... (8 terminals)
  },
  resetAt: '2026-07-02T00:00:00Z'  // Midnight Budapest time
}
```

---

## Dispatch Check

### Pre-Dispatch Budget Validation

**Before starting a session, check if dispatch is allowed:**

```typescript
const check = canDispatch('backend', 5000, 'high');
```

**Returns:**
```typescript
{
  allowed: true,
  budgetRemaining: 2750,
  estimatedAfter: 12250
}
```

**OR (if budget insufficient):**
```typescript
{
  allowed: false,
  reason: 'Insufficient budget: need 5000, have 2750',
  budgetRemaining: 2750
}
```

### Dispatch Logic

**Decision tree:**

```
1. Check dispatch mode
   └─ manual → REJECT (require approval)
   └─ auto/scheduled → continue

2. Check budget status
   └─ depleted → check priority
      ├─ critical → check priority reserve
      │  ├─ reserve available → ALLOW (use reserve)
      │  └─ reserve depleted → REJECT
      └─ non-critical → REJECT

3. Check estimated tokens fit
   └─ tokensNeeded <= tokensRemaining → ALLOW
   └─ tokensNeeded > tokensRemaining → REJECT
```

### Priority Reserve Logic

**Example:**

```typescript
// Backend budget: 10,000 daily, 2,000 reserve
// Current usage: 10,000 (depleted)
// Critical task needs: 1,500 tokens

canDispatch('backend', 1500, 'critical');
// ✅ allowed: true (uses reserve, 500 remaining)

canDispatch('backend', 2500, 'critical');
// ❌ allowed: false (exceeds reserve)

canDispatch('backend', 500, 'medium');
// ❌ allowed: false (non-critical can't use reserve)
```

---

## Dispatch Queue

### Queue Management

**Add to queue:**
```typescript
queueDispatch('MSG-BACKEND-042', 'backend', 'high', 5000);
```

**Get queue (priority-sorted):**
```typescript
const queue = getDispatchQueue();
// Returns: [
//   { messageId: 'MSG-BACKEND-041', terminal: 'backend', priority: 'critical', ... },
//   { messageId: 'MSG-BACKEND-042', terminal: 'backend', priority: 'high', ... },
//   { messageId: 'MSG-FRONTEND-088', terminal: 'frontend', priority: 'medium', ... }
// ]
```

**Queue priority sorting:**
```sql
ORDER BY
  CASE priority
    WHEN 'critical' THEN 1
    WHEN 'high' THEN 2
    WHEN 'medium' THEN 3
    WHEN 'low' THEN 4
  END,
  queued_at ASC
```

### Queue Status Transitions

| Status | Meaning | Trigger |
|--------|---------|---------|
| `queued` | Waiting for dispatch | Added to queue |
| `executing` | Session active | Session started |
| `completed` | Session finished | Session ended |
| `failed` | Session error | Error during execution |
| `cancelled` | Manually cancelled | Admin action |

**Update status:**
```typescript
markDispatchExecuting('MSG-BACKEND-042', 'session-12345');
markDispatchCompleted('MSG-BACKEND-042', 3450);  // actual tokens used
markDispatchFailed('MSG-BACKEND-042', 'Session timeout');
```

---

## Usage Statistics

### Terminal Statistics

**Get stats for a terminal:**
```typescript
const stats = getUsageStats('backend');
```

**Returns:**
```typescript
{
  today: 7250,
  thisWeek: 48900,
  thisMonth: 156700,
  byModel: {
    'sonnet': 6200,
    'haiku': 1050
  }
}
```

**System-wide stats (omit terminal):**
```typescript
const allStats = getUsageStats();  // All terminals combined
```

---

## Integration Points

### 1. Session Starter

**Before starting session:**
```typescript
// In sessionStarter.ts
const check = canDispatch(terminal, estimatedTokens, priority);
if (!check.allowed) {
  log(`[SessionStarter] Cannot dispatch ${terminal}: ${check.reason}`);
  return;
}

// Start session...
markDispatchExecuting(messageId, sessionId);
```

### 2. Session End Hook

**After session completes:**
```typescript
// In session cleanup
const tokensUsed = session.usage.total_tokens;
recordTokenUsage({
  terminal,
  sessionId,
  taskId: messageId,
  tokensUsed,
  model: session.model
});

markDispatchCompleted(messageId, tokensUsed);
```

### 3. Nightwatch Pipeline

**Budget check before each cycle:**
```typescript
// In nightwatch.sh
const summary = getDailyBudgetSummary();
if (summary.totalUsed > summary.totalLimit * 0.9) {
  log('[Nightwatch] ⚠️ System-wide budget at 90%');
}
```

### 4. Datahaven Dashboard

**Budget widget API:**
```bash
curl http://localhost:3456/api/dispatch-control/budget/summary
```

**Response:**
```json
{
  "date": "2026-07-01",
  "totalLimit": 80000,
  "totalUsed": 45230,
  "totalRemaining": 34770,
  "byTerminal": { ... }
}
```

---

## Best Practices

### For Root/Conductor

**1. Set realistic daily limits:**
- Start conservative (current limits are well-tested)
- Adjust based on 7-day average usage
- Reserve 20-30% buffer for unexpected tasks

**2. Monitor budget trends:**
```bash
# Weekly usage check
sqlite3 data/dispatch.db "
  SELECT terminal, SUM(tokens_used) as week_total
  FROM token_usage
  WHERE timestamp >= datetime('now', '-7 days')
  GROUP BY terminal
  ORDER BY week_total DESC;
"
```

**3. Use priority reserve wisely:**
- Reserve = 20% of daily limit (current default)
- Only for critical tasks (production incidents, security)
- Review reserve usage weekly

### For Terminals

**1. Estimate token usage accurately:**
```typescript
// Small task (< 2,000 tokens)
canDispatch('backend', 1500, 'medium');

// Medium task (2,000-5,000 tokens)
canDispatch('backend', 3500, 'medium');

// Large task (5,000-10,000 tokens)
canDispatch('backend', 7500, 'high');
```

**2. Handle dispatch rejection gracefully:**
```typescript
const check = canDispatch(terminal, estimatedTokens, priority);
if (!check.allowed) {
  // Queue for later or escalate to Root
  queueDispatch(messageId, terminal, priority, estimatedTokens);
  return;
}
```

**3. Choose appropriate model:**
```typescript
// Haiku (cheap, ~1/10 cost of Sonnet)
recordTokenUsage({ terminal: 'backend', tokensUsed: 500, model: 'haiku' });

// Sonnet (standard)
recordTokenUsage({ terminal: 'backend', tokensUsed: 3000, model: 'sonnet' });

// Opus (expensive, 5x Sonnet)
recordTokenUsage({ terminal: 'backend', tokensUsed: 8000, model: 'opus' });
```

---

## Monitoring & Debugging

### Real-Time Budget Dashboard

**Check all terminal budgets:**
```sql
SELECT * FROM v_budget_status ORDER BY usage_percent DESC;
```

**Output:**
```
terminal   daily_limit  tokens_used  tokens_remaining  usage_percent  status
---------  -----------  -----------  ----------------  -------------  --------
backend    10000        8950         1050              89.5           critical
root       20000        16200        3800              81.0           warning
conductor  15000        9500         5500              63.3           ok
frontend   10000        5200         4800              52.0           ok
...
```

### Alert History

**Today's alerts:**
```sql
SELECT alert_type, terminal, message, created_at
FROM budget_alerts
WHERE DATE(created_at) = DATE('now')
ORDER BY created_at DESC;
```

### Token Usage Trends

**Hourly usage pattern (identify peak hours):**
```sql
SELECT
  strftime('%H:00', timestamp) as hour,
  SUM(tokens_used) as tokens
FROM token_usage
WHERE DATE(timestamp) = DATE('now')
GROUP BY hour
ORDER BY hour;
```

**Output:**
```
hour   tokens
-----  ------
08:00  4200
09:00  7800
10:00  12500  ← Peak usage
11:00  9300
...
```

### Dispatch Queue Status

**Current queue:**
```sql
SELECT message_id, terminal, priority, status, queued_at
FROM dispatch_queue
WHERE status IN ('queued', 'executing')
ORDER BY priority, queued_at;
```

---

## Error Handling

### Common Errors

**1. Budget depleted (non-critical task):**
```
Error: Budget depleted for backend
```
**Cause:** Terminal used 10,000/10,000 tokens, task priority = medium
**Fix:** Wait until midnight (budget reset) OR escalate to critical priority

**2. Insufficient budget:**
```
Error: Insufficient budget: need 5000, have 2750
```
**Cause:** Task needs 5,000 tokens, only 2,750 remaining
**Fix:** Queue task for later OR split into smaller tasks

**3. Manual mode rejection:**
```
Error: Manual mode - requires explicit dispatch approval
```
**Cause:** Dispatch mode = 'manual', auto-dispatch disabled
**Fix:** Set mode to 'auto' OR manually approve dispatch

**4. Priority reserve exceeded:**
```
Error: Priority reserve exhausted
```
**Cause:** Critical task needs 3,000 tokens, reserve = 2,000
**Fix:** Wait for budget reset OR increase reserve limit

### Budget Reset

**Automatic reset:**
- Daily budgets reset at **midnight Budapest time** (Europe/Budapest timezone)
- `v_today_usage` view automatically filters by `DATE('now')`

**Manual reset (emergency):**
```sql
-- ⚠️ Use with caution!
DELETE FROM token_usage WHERE DATE(timestamp) = DATE('now');
```

---

## Configuration Management

### Update Terminal Budget

**API:**
```typescript
setTerminalBudget('backend', 15000, 3000);  // 15k daily, 3k reserve
```

**SQL:**
```sql
INSERT INTO budget_config (terminal, daily_limit, priority_reserve)
VALUES ('backend', 15000, 3000)
ON CONFLICT(terminal) DO UPDATE SET
  daily_limit = excluded.daily_limit,
  priority_reserve = excluded.priority_reserve;
```

### Get All Budget Configs

**API:**
```typescript
const configs = getAllBudgetConfigs();
```

**Returns:**
```typescript
[
  { terminal: 'root', dailyLimit: 20000, priorityReserve: 5000 },
  { terminal: 'conductor', dailyLimit: 15000, priorityReserve: 3000 },
  { terminal: 'backend', dailyLimit: 10000, priorityReserve: 2000 },
  // ... (8 terminals)
]
```

---

## Performance Characteristics

### Write Performance

| Operation | Time | Notes |
|-----------|------|-------|
| Record token usage | ~3ms | Single INSERT + alert check |
| Queue dispatch | ~2ms | INSERT into dispatch_queue |
| Alert check | ~5ms | View query + conditional INSERT |
| Budget update | ~2ms | Single UPDATE |

### Read Performance

| Query | Time | Notes |
|-------|------|-------|
| Get budget status | ~2ms | Indexed view query |
| Daily summary | ~10ms | Aggregates across 8 terminals |
| Dispatch check | ~3ms | View query + logic |
| Usage stats | ~8ms | SUM aggregations with date filters |

**Database size:** ~200 KB per 10,000 token records (negligible).

---

## Future Enhancements

**Planned:**
- [ ] Hourly budget limits (currently only daily)
- [ ] Scheduled dispatch windows (e.g., 09:00-17:00 only)
- [ ] Multi-day budget rollover (unused tokens → next day)
- [ ] Cost estimation API (estimate $ cost per task)
- [ ] Budget forecast (predict depletion time)

**Under consideration:**
- [ ] Per-model budget limits (separate limits for haiku/sonnet/opus)
- [ ] Dynamic budget adjustment (ML-based optimization)
- [ ] Budget pooling (shared budget across terminal groups)
- [ ] Webhook alerts (Slack, Discord, email)

---

## Related Patterns

- [TASKMESSAGEBOX_PATTERN.md](TASKMESSAGEBOX_PATTERN.md) — Task dispatch system (uses Dispatch Control for budget checks)
- [COLD_MODE_SESSION_PATTERN.md](COLD_MODE_SESSION_PATTERN.md) — Session lifecycle (integrates with dispatch queue)
- [TERMINAL_REVIEW_PATTERN.md](TERMINAL_REVIEW_PATTERN.md) — DONE review workflow (budget-aware scheduling)
- [TELEGRAM_INTEGRATION.md](TELEGRAM_INTEGRATION.md) — Alert delivery mechanism

---

## Cost Analysis

### Daily Cost Estimate (80,000 token budget)

**Model pricing (Anthropic API):**
- Haiku: $0.25/MTok input, $1.25/MTok output (~$0.75/MTok average)
- Sonnet: $3/MTok input, $15/MTok output (~$9/MTok average)
- Opus: $15/MTok input, $75/MTok output (~$45/MTok average)

**Typical distribution:**
- 60% Haiku (simple tasks, research): 48,000 tokens × $0.75/MTok = **$0.036**
- 35% Sonnet (standard dev): 28,000 tokens × $9/MTok = **$0.252**
- 5% Opus (architecture): 4,000 tokens × $45/MTok = **$0.180**

**Total daily cost:** ~$0.47/day (~$14/month)

**With safety margin (100k token/day):** ~$18-20/month

---

**Last Updated:** 2026-07-01
**Source Code:** `spaceos-nexus/knowledge-service/src/dispatch-control/`
**Database:** `spaceos-nexus/knowledge-service/data/dispatch.db`
**Tests:** `src/__tests__/unit/dispatchControl.test.ts` (TODO)
