# Goal-definiálási módszertan — átvéve a VPS-ről (2026-07-07)

> Forrás: VPS knowledge-service (list_goals éles példák + patterns/GOAL_PERSISTENCE_PATTERNS.md)
> A VPS-en MOST fejlesztett goal-rendszer sémája, amit a Cabinet flotta is követ.

## A goal-objektum sémája (éles VPS-példák alapján)

```json
{
  "id": "GOAL-YYYY-MM-DD-NNN",
  "created_by": "conductor",
  "epic_id": "EPIC-CB-MCPHOST",
  "goal": {
    "description": "Emberi nyelvű, mérhető cél",
    "checkpoint_id": "CP-XXX-YYY"
  },
  "completion_criteria": [
    {
      "type": "done_outbox",
      "terminal": "backend",
      "message_pattern": "*mcphost*poc*done*"
    }
  ],
  "on_complete": {
    "trigger_terminal": "conductor",
    "next_goal": "A következő cél leírása",
    "prompt": "GOAL TELJESÜLT: {{goal.description}} → következő: {{on_complete.next_goal}}"
  },
  "status": "watching | triggered | completed | expired",
  "expires_at": "ISO-8601 (tipikusan +4 óra)"
}
```

## Kulcselvek

1. **A goalt a conductor hozza létre** (create_goal), a Monitor figyeli (watching → triggered).
2. **Mérhető completion_criteria** — tipikus: `done_outbox` (a terminál DONE üzenete illeszkedik a message_pattern-re). A cél SOSEM szubjektív.
3. **on_complete láncolás** — a teljesült goal automatikusan triggerel egy következő lépést a conductornál (goal-lánc = roadmap).
4. **expires_at** — a lejárt goal "expired" lesz, nem lóg örökké; újra kell értékelni.
5. **Epic-hez kötés** — minden goal egy epic-hez tartozik (epic_id), a checkpoint-ok (CP-*) az epic mérföldkövei.

## Goal Drift — az 5 hibamód, ami ellen a rendszer véd (VPS-kutatás, 2026)

| Hibamód | Lényeg | Védekezés |
|---------|--------|-----------|
| Context Dilution | korai instrukciók elhalványulnak hosszú sessionben | goal-objektum a szerveren él, nem a kontextusban |
| Pattern Matching Override | friss zaj felülírja az explicit direktívát | Monitor a kritériumot nézi, nem a hangulatot |
| Inherited Drift | subagent-outputok eltérítik a fő célt | goal-lánc conductor-szinten, nem terminál-szinten |
| Value Conflict Drift | ütköző instrukciók erodálódnak | explicit prioritás a goal-leírásban |
| Subgoal Displacement | részfeladat-tökéletesítés blokkolja az epicet | expires_at + checkpoint-ok kényszerítik a haladást |

## Kapcsolódó VPS-skillek (lementve: .claude/skills/)

- `project-setup` — projekt/epic/task struktúra MCP-vel (create_project → EPICS.yaml → TASKS.yaml → checkpoint-ok), dispatch-prioritás: explorer → architect → designer → backend → frontend
- `create-implementation-plan` — determinisztikus, gépileg végrehajtható tervdokumentum-sablon (REQ-/TASK-/GOAL- azonosítók, fázisok, DoD)
