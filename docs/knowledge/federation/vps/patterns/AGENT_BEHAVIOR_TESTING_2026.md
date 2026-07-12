# Agent Behavior Testing — Kutatási Összefoglaló (2026)

> **Version:** 1.0
> **Last Updated:** 2026-07-12
> **Source:** WebSearch kutatás + akadémiai források (arXiv)
> **Maintained By:** Root / Librarian

---

## OVERVIEW

Ez a dokumentum összefoglalja a 2026-os kutatási eredményeket arról, hogyan lehet tesztelni, hogy egy **LLM-alapú agent azt csinálja-e, amit szeretnénk**. A fókusz az execution-based verification és a production-ready evaluation framework-ökön van.

**Fő kérdés:** "Azt lehet tesztelni, hogy egy agent azt csinálja amit szeretnénk?"

**Válasz:** Igen, három fő megközelítéssel:
1. **Execution-based verification** — Futtatás + eredmény ellenőrzés
2. **Trajectory analysis** — Lépéssorozat összehasonlítás golden path-tal
3. **Tool correctness** — MCP/tool hívások validálása

---

## EXECUTION-BASED VERIFICATION

### Alapelv

A legmegbízhatóbb módszer: **futtasd az agent-et, és ellenőrizd az eredményt determinisztikusan**.

```
Agent Input → Agent Execution → Output/Side Effects → Deterministic Check
```

### Benchmark Példák

| Benchmark | Mit ellenőriz | Módszer |
|-----------|---------------|---------|
| **SWE-Bench** | Kód működik-e | Test suite futtatás (pass/fail) |
| **tau-bench** | Database művelet helyes-e | Database state ellenőrzés |
| **tau2-bench** | Megbízhatóság | pass^k (k próbálkozásból hány sikeres) |
| **WebArena** | Web task completion | DOM state + URL ellenőrzés |

### SpaceOS Alkalmazás

```
Terminál Agent Input (inbox task)
    ↓
Agent Execution (Claude session)
    ↓
Output (outbox DONE/BLOCKED)
    ↓
Deterministic Check:
  - DONE tartalmazza az elvárt deliverables-t?
  - Build/test pass?
  - Kód módosítások megfelelnek a spec-nek?
```

**Példa ellenőrzés:**
```bash
# 1. Agent outbox validálás
grep -q "status: DONE" terminals/backend/outbox/MSG-BACKEND-*.md

# 2. Deliverables check
grep -q "60 tests passing" terminals/backend/outbox/MSG-BACKEND-*.md

# 3. Build verification
cd /opt/joinerytech && dotnet build && dotnet test
```

---

## HÁROM METRIKA OSZTÁLY

A modern agent evaluation framework-ök (AgentEval, DeepEval, Phoenix) három metrika osztályt használnak:

### 1. TrajectoryAccuracy

**Mit mér:** Agent lépéssorozat vs. "golden path" (elvárt referencia)

**Használat:**
- Rögzíts sikeres session-öket mint "golden path"
- Új session-öket hasonlítsd az elvárthoz
- Eltérés = potenciális hiba vagy alternatív megoldás

**SpaceOS példa:**
```yaml
golden_path:
  - step: "Read inbox task"
  - step: "Run existing tests"
  - step: "Implement feature"
  - step: "Run tests again"
  - step: "Write DONE outbox"

actual_trajectory:
  - step: "Read inbox task"
  - step: "Implement feature"  # ⚠️ SKIPPED: Run existing tests
  - step: "Run tests"
  - step: "Write DONE outbox"

deviation_score: 0.8  # 80% match
```

### 2. ToolCorrectnessJudge

**Mit mér:** Tool/MCP hívások helyessége

**Ellenőrzött szempontok:**
- Helyes tool név
- Helyes paraméterek
- Helyes sorrend
- Graceful failure recovery

**SpaceOS példa:**
```typescript
// Elvárt MCP hívás
expected: {
  tool: "mcp__spaceos-knowledge__create_task",
  params: {
    from: "conductor",
    to: "backend",
    title: "Phase 3 API Endpoints",
    priority: "high"
  }
}

// Aktuális hívás
actual: {
  tool: "mcp__spaceos-knowledge__create_task",
  params: {
    from: "conductor",
    to: "backend",
    title: "Phase 3 API",  // ⚠️ Incomplete title
    priority: "medium"     // ⚠️ Wrong priority
  }
}

tool_correctness_score: 0.7
```

### 3. TaskCompletionJudge

**Mit mér:** Végső cél elérése (bináris vagy kategorikus)

**Kategóriák:**
- **COMPLETE:** Minden deliverable megvan
- **PARTIAL:** Néhány deliverable hiányzik
- **FAILED:** Kritikus hiba, task nem teljesült
- **BLOCKED:** External dependency miatt nem folytatható

**SpaceOS példa:**
```yaml
task: "Implement AI Agent Services (Phase 2)"

expected_deliverables:
  - agentService.ts with 8 operations
  - skillService.ts with 6 operations
  - memoryService.ts with 5 operations
  - 60 unit tests passing
  - Zero TypeScript errors

actual_deliverables:
  - agentService.ts with 8 operations ✅
  - skillService.ts with 6 operations ✅
  - memoryService.ts with 5 operations ✅
  - 60 unit tests passing ✅
  - Zero TypeScript errors ✅

task_completion: COMPLETE
```

---

## SWE-BENCH 2026 ÁLLAPOT

### Benchmark Eredmények

| Modell | SWE-Bench Verified Score | Dátum |
|--------|--------------------------|-------|
| Claude Opus 4.7 | **87.6%** | 2026 április |
| Codex CLI + GPT-5.5 | **88.7%** | 2026 május |
| Claude Sonnet 4 | ~75% | 2026 Q2 |

### Fontos Figyelmeztetések

> ⚠️ **Harness Variance:** Azonos modell 10-20 ponttal eltérő eredményt adhat különböző evaluation harness-ekben!

**Okok:**
1. Task selection a "Verified" subset-en belül
2. Scaffolding és tool access különbségek
3. Retry counting módszer
4. Partial-credit patch kezelés

**Következmény:** Ne hasonlíts össze különböző harness-ből származó számokat!

---

## EVALUATION FRAMEWORKS (2026)

### Phoenix v16.0.0 (2026 május)

**Újdonságok:**
- Sandboxed Code Evaluators (composite scoring)
- LLM-jury implementations (server-side)
- Multi-agent evaluation support

**Használat:**
```python
from phoenix.evals import AgentEvaluator

evaluator = AgentEvaluator(
    metrics=["trajectory_accuracy", "tool_correctness", "task_completion"],
    sandbox=True
)

result = evaluator.evaluate(agent_trace)
```

### DeepEval v4.0.3 (2026 május)

**Újdonságok:**
- Decision Graph Logic (granular simulation control)
- Complete agentic eval harness
- Trace-based evaluation

**Használat:**
```python
from deepeval.agents import AgentTestCase, ToolCorrectnessMetric

test_case = AgentTestCase(
    input="Implement user authentication",
    expected_tools=["read_file", "write_file", "run_tests"],
    expected_output_contains=["JWT", "middleware"]
)

metric = ToolCorrectnessMetric()
result = metric.measure(test_case)
```

### Unified Framework (arXiv 2605.27898)

**Jellemzők:**
- 7 benchmark standardizálva 24 domain-ben
- ReAct-style agent architektúra
- Unified configuration system
- Instruction–tool–environment triplet formátum

---

## SPACEOS TERMINÁL AGENT TESTING

### Javasolt Megközelítés

```
┌─────────────────────────────────────────────────────────────┐
│                    SPACEOS AGENT TESTING                     │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  1. GOLDEN PATH RECORDING                                    │
│     - Sikeres session-ök rögzítése                          │
│     - Inbox → Actions → Outbox trajectory                    │
│     - MCP tool call sequence                                 │
│                                                              │
│  2. EXECUTION-BASED VERIFICATION                             │
│     - Build/test gate (dotnet build && dotnet test)         │
│     - Outbox deliverables check                              │
│     - File modification validation                           │
│                                                              │
│  3. REGRESSION TESTING                                       │
│     - Replay past tasks with new model version              │
│     - Compare trajectory + output                            │
│     - Alert on significant deviation                         │
│                                                              │
│  4. TOOL CORRECTNESS MONITORING                              │
│     - MCP call logging (already in place)                    │
│     - Parameter validation                                   │
│     - Success/failure rate tracking                          │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Implementációs Javaslat

**Phase 1: Golden Path Recording**
```typescript
// knowledge-service/src/pipeline/goldenPathRecorder.ts
interface GoldenPath {
  taskId: string;
  terminal: string;
  trajectory: TrajectoryStep[];
  toolCalls: ToolCall[];
  deliverables: string[];
  success: boolean;
}

async function recordGoldenPath(session: AgentSession): Promise<GoldenPath> {
  // Record successful session as reference
}
```

**Phase 2: Trajectory Comparison**
```typescript
// knowledge-service/src/pipeline/trajectoryComparator.ts
function compareTrajectory(
  actual: TrajectoryStep[],
  golden: TrajectoryStep[]
): TrajectoryScore {
  // Levenshtein-like distance for step sequences
  // Return similarity score 0.0 - 1.0
}
```

**Phase 3: Automated Regression**
```typescript
// knowledge-service/src/pipeline/agentRegression.ts
async function runRegressionSuite(): Promise<RegressionReport> {
  // Replay historical tasks
  // Compare with golden paths
  // Generate deviation report
}
```

---

## PRODUCTION VS BENCHMARK

### Fontos Különbség

> "Benchmarks like AgentBench, WebArena, and SWE-bench are useful for standardized comparisons, but they do not fully represent your production workflows."

**Mit kell csinálni:**
1. **Custom datasets** — SpaceOS-specifikus task-ok
2. **Trace-based evals** — Saját session log elemzés
3. **Regression tests** — Valós hibákból épített test suite

### SpaceOS Custom Evaluation

```yaml
# spaceos_agent_eval_suite.yaml
tasks:
  - id: "conductor-dispatch"
    input: "Dispatch Phase 3 to Backend"
    expected_tools: ["create_task", "subscribe_to_task"]
    expected_output: "MSG-BACKEND-* created"

  - id: "backend-implement"
    input: "Implement agentService.ts"
    expected_tools: ["read_file", "write_file", "run_tests"]
    expected_deliverables:
      - "agentService.ts exists"
      - "tests pass"
      - "DONE outbox written"

  - id: "librarian-synthesize"
    input: "Create knowledge doc from outbox"
    expected_output: "docs/knowledge/**/*.md created"
```

---

## METRICS DASHBOARD (JAVASLAT)

### Datahaven Integration

```
┌─────────────────────────────────────────────────────────────┐
│                    AGENT HEALTH METRICS                      │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Task Completion Rate    [████████████░░░░] 78%              │
│  Tool Correctness        [██████████████░░] 92%              │
│  Trajectory Adherence    [███████████░░░░░] 71%              │
│  Regression Pass Rate    [████████████████] 100%             │
│                                                              │
│  ─────────────────────────────────────────────────────────  │
│                                                              │
│  Recent Deviations:                                          │
│  • Backend skipped test run before implementation            │
│  • Conductor used wrong priority (medium vs high)            │
│  • Frontend missing TypeScript strict checks                 │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## REFERENCES

### External Sources

- [AI Agent Evaluation (2026): Metrics, Frameworks, and Production Failures](https://www.morphllm.com/ai-agent-evaluation)
- [LLM Agent Evaluation Metrics 2026 - Confident AI](https://www.confident-ai.com/blog/llm-agent-evaluation-complete-guide)
- [AI Agent Eval Frameworks 2026: Testing Guide & Tools](https://www.digitalapplied.com/blog/ai-agent-eval-frameworks-testing-guide-2026)
- [A Survey on Evaluation of LLM-based Agents (arXiv)](https://arxiv.org/html/2503.16416v2)
- [A Unified Framework for LLM Agentic Capabilities (arXiv)](https://arxiv.org/html/2605.27898v1)
- [LLM Evaluation in 2026 - Medium](https://medium.com/@nairmilind3/llm-evaluation-in-2026-e631a78c67dc)

### Related SpaceOS Docs

- `docs/knowledge/patterns/TESTING_STRATEGIES.md` — Unit/Integration/E2E testing
- `docs/knowledge/patterns/AUTONOMOUS_AGENT_FRAMEWORK.md` — Agent architecture
- `docs/knowledge/patterns/TERMINAL_REVIEW_PATTERN.md` — Review workflow
- `docs/architecture/decisions/ADR-053-checkpoint-coordination-workflow.md` — Checkpoint system

---

## NEXT STEPS

### Immediate (Q3 2026)

1. **Golden Path Recording** — Implementáció a knowledge-service-ben
2. **MCP Call Logging** — Már van, de metrics aggregation kell
3. **Regression Test Suite** — 10-20 historical task replay

### Future (Q4 2026)

1. **Datahaven Agent Health Dashboard** — Metrics vizualizáció
2. **Automated Deviation Alerts** — Telegram notification
3. **Phoenix/DeepEval Integration** — External framework adoption

---

**Document Status:** ✅ COMPLETE
**Next Review:** 2026-08-12 (1 month)
**Maintained By:** Root / Librarian
