---
name: DESIGN — project planning modernization
description: Epic/task-kezelés modernizálása sarokkövekkel, konfigurálható szerver-oldali workflow-definíciókkal és koordinátori goal-fókusz támogatással
status: DESIGN (Gábor scope-döntésére vár)
created: 2026-07-12
---

# Projekt-tervezés modernizálása — sarokkövek, workflow-definíciók, goal-fókusz

> Gábor (2026-07-12): "Modernizálni kell a projekt tervezést, az epikek és taskok
> kezelését a sarokkövek meghatározásával. Emellé kell a workflow-meghatározás, amit
> el kell várni a szerveren. Lehessen konfigurálni. A koordinátornak segítenie kell
> a goal-fókuszban."

## Ami MÁR LÉTEZIK (nem építjük újra)

| Komponens | Mit tud | Hol |
|-----------|---------|-----|
| `EPICS.yaml` + epicManager (ADR-053, Mode #4) | Epic {id, status, depends_on, **checkpoints** = sarokkövek {condition, trigger_to}}, progress% = kész checkpointok aránya, next-checkpoint | `conductor/epicManager.ts`, env: `EPICS_PATH` |
| goalStore + MCP toolok | Goal + criteria (watching→triggered→completed), `create_goal`/`check_goal_criteria`/`trigger_goal` | `goalStore.ts` |
| conductorBriefing | Aktív epic + következő checkpoint + blokkolók → prioritás-akciók a koordinátornak | `conductor/conductorBriefing.ts` |
| epicRouter | SQLite projects/epics/task_queue — epic-aware dispatch idle terminálnak | `pipeline/epicRouter.ts` |
| Kanonikus message-model + eval | 6-státusz életciklus-államgép configból + golden path + trajectory-pontozás | `message-model.yaml`, `src/eval/` |

## A HÁROM HIÁNYZÓ DARAB (a modernizálás)

### 1. Lokál projekt-definíciók (Cabinet oldalon ma NINCS EPICS.yaml)
A lokál flottának nincs projekt/epic/sarokkő definíciója — az agentek nem tudják, mi a
cél. Kell: `docs/projects/EPICS.yaml` a valós projektjeinkre (CabinetBilder CAD-motor,
Doorstar műhely-app, nexus-hozzájárulás), sarokkövekkel. A meglévő epicManager-séma
szerint (kompatibilis a VPS-ével), `EPICS_PATH` env-vel a szigetekre kötve.

### 2. Konfigurálható workflow-definíciók a szerveren (ÚJ)
Ma a golden path csak FELVETT referencia (rögzített sikeres futásból). A workflow-
definíció ennek az ELŐÍRÓ párja: configban deklarált elvárt lefolyás task-típusonként.
```yaml
# config/workflows.yaml (knowledge-service, env: WORKFLOWS_CONFIG_PATH)
workflows:
  task:                     # kanonikus type
    expected_trajectory: [read, in_progress, completed]
    max_hours_in: { unread: 24, in_progress: 48 }   # stuck-detektálás alapja
  question:
    expected_trajectory: [read, completed]
```
A szerver ez alapján: (a) az eval a workflow-hoz IS tud pontozni (nem csak felvett
goldenhez), (b) a stuck/watch-pipeline configból tudja, mikor "ragadt be" valami,
(c) minden sziget ugyanazt az elvárást futtatja — konfigurálhatóan.

### 3. Koordinátori goal-fókusz a lokál flottán (bekötés)
A conductorBriefing kész, de a lokál wake-folyamat nem használja. Kell:
- a `fleet.sh wake` prompt kiegészítése az AKTÍV epic + következő sarokkő
  kontextussal (az EPICS.yaml-ból — a goal-externalization minta, drift ellen),
- sarokkő-átbillenéskor (checkpoint done) dense-milestone visszajelzés a
  koordinátornak (a kutatás 6.4%→43% mintája).

## Elvek (a kódolási alapelvárások szerint)
- Minden definíció CONFIG (EPICS.yaml, workflows.yaml), env-átirányítható — nincs hardcode.
- A státusz/haladás a DB-ből (task-message-box + status_history), nem .md-parse.
- Tesztek rögzítik az elvárt működést; README a miértekkel.

## Javasolt sorrend
1. `workflows.yaml` + betöltő domain-modul + eval/stuck integráció (nexus, additív).
2. Lokál `EPICS.yaml` a valós projektjeinkkel (Cabinet, saját).
3. Wake-prompt goal-fókusz bekötés (Cabinet, fleet.sh + prompts/).
4. VPS-egyeztetés: a workflows.yaml séma közös elfogadása (ők is futtatják).
