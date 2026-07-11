---
name: project-setup
description: "SpaceOS projekt és epic struktúra létrehozása MCP tool-okkal. Használd amikor új projektet kell definiálni milestone-okkal, epic-ekkel és task-okkal. (Átvéve a VPS-ről 2026-07-07, forrás: /opt/spaceos/.claude/skills/project-setup)"
triggers:
  - "új projekt létrehozása"
  - "projekt setup"
  - "epic definiálás"
  - "create project"
  - "projekt struktúra"
author: root
version: "1.0"
created: "2026-07-01"
---

# Project Setup Skill

Teljes projekt struktúra létrehozása a SpaceOS MCP rendszerben.

## Mikor használd

- Új fejlesztési projekt indításakor
- Prototípus → production migráció tervezésekor
- Epic és milestone struktúra definiálásakor

## Workflow

### 1. Projekt létrehozása MCP-vel

```
mcp__spaceos-knowledge__create_project
  slug: "projekt-nev"
  name: "Projekt Teljes Neve"
  description: "Rövid leírás"
  milestones: [
    {id: "M1-XXX", name: "Első mérföldkő"},
    {id: "M2-YYY", name: "Második mérföldkő"}
  ]
```

**Fontos:** ELŐSZÖR hozd létre MCP-vel, UTÁNA szerkeszd a TASKS.yaml-t!

### 2. Epic-ek definiálása EPICS.yaml-ban

Szerkeszd: `docs/projects/EPICS.yaml` (Cabinet lokál megfelelő; VPS-en /opt/spaceos/docs/projects/EPICS.yaml)

Minden epic-hez:
- `id`: EPIC-PREFIX-NAME
- `project`: projekt slug
- `depends_on`: előfeltétel epic-ek
- `parallel_with`: párhuzamosan futtatható epic-ek
- `status`: pending | active | done
- `target_date`: határidő
- `checkpoints`: mérföldkövek trigger-ekkel

### 3. Task-ok rögzítése TASKS.yaml-ban

Szerkeszd: `docs/projects/<projekt>/TASKS.yaml`

Minden task-hoz:
- `id`: JT-XXX-NNN
- `title`: rövid cím
- `terminal`: architect | backend | frontend | designer | explorer
- `priority`: critical | high | medium | low
- `status`: pending | in_progress | completed | blocked
- `depends_on`: előfeltétel task ID-k
- `epic`: kapcsolódó EPIC ID

### 4. Checkpoint subscription-ök ellenőrzése

```
mcp__spaceos-knowledge__get_checkpoint_status
mcp__spaceos-knowledge__refresh_checkpoint_subscriptions
```

### 5. Terminál felelősségek dokumentálása

Hozz létre: `TERMINAL_ROLES.md` a projekt mappában

## Task Template per Epic

Minden epic-hez tipikusan:

1. **Domain Model** (architect) - depends_on: []
2. **Backend API** (backend) - depends_on: [domain model]
3. **UI Design** (designer) - depends_on: [domain model]
4. **Frontend Components** (frontend) - depends_on: [backend, ui design]
5. **Integration** (backend) - depends_on: [frontend]

## Checkpoint Template

```yaml
checkpoints:
  - id: CP-XXX-BACKEND
    name: "Backend API Ready"
    trigger_to: [conductor, architect]
    condition: "API endpoints tested"
    status: pending
  - id: CP-XXX-FRONTEND
    name: "UI Complete"
    trigger_to: [conductor, designer]
    condition: "Components implemented"
    status: pending
```

## Dispatch Prioritás

1. **explorer** - Gap analysis (ha prototípusból migrálsz)
2. **architect** - Domain model design (párhuzamosan több is mehet)
3. **designer** - UI design (domain model után)
4. **backend** - API implementáció (domain model után)
5. **frontend** - UI implementáció (backend + design után)

## Gyakori Hibák

1. **Ne hozd létre manuálisan a projektet** - MCP create_project kell
2. **TASKS.yaml-t MCP után szerkeszd** - különben felülíródik
3. **Checkpoint targetId opcionális** - subscription létrejöhet nélküle is
4. **depends_on task ID-k pontosak legyenek** - a DAG validátor ellenőrzi

## Kapcsolódó MCP Tools

- `create_project` - projekt létrehozás
- `get_project_status` - státusz lekérdezés
- `dispatch_next` - következő task kiadása
- `get_checkpoint_status` - checkpoint-ok listázása
- `subscribe_to_task` - task figyelés
