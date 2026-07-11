---
goal: Naptári + erőforrás-korlátos ütemezés — több projekt közös szakma-kapacitásra
version: 1.0
date_created: 2026-07-10
owner: Cabinet root
status: 'Done'
tags: [feature, mcphost, scheduling, capacity, calendar, modernization]
---

# Bevezetés

A CPM (infinite-resource) után a valós korlátok: (A) munkanap-határok (a munka csak munkanapon,
munkaidőben halad; hétvége kimarad); (B) szakma-kapacitás (egy szakmában véges dolgozó van →
több projekt/művelet verseng ugyanazért az erőforrásért). Ez a "02 Folyamatok" kapacitás-tervezés
lelke: mikor tud pl. a CNC-s egyszerre két projekten dolgozni, és mikor nem.

## 1. Requirements & Constraints
- REQ-001: `WorkCalendar` — munkaóra → naptári dátum leképzés (kezdő dátum, munkanap-órák, munkanapok halmaza; hétvége/ünnep kimarad).
- REQ-002: `ResourceScheduler` — erőforrás-korlátos ütemezés (list scheduling): szakmánként N dolgozó; a művelet a legkorábbi időben indul, amikor (a) az elődei készek ÉS (b) van szabad dolgozó az adott szakmában. Több job (projekt) egyszerre.
- REQ-003: A prioritás CPM-ES szerint (a precedencia így garantált, mert dur>0 → utód ES > előd ES); azonosságnál job-index, opId.
- REQ-004: Az ütemezés munkaóra-térben fut (folytonos munkaidő), majd a WorkCalendar naptárra vetíti — így a hétvége/munkaidő egységesen kezelt.
- REQ-005: `skeleton_schedule_projects` tool — több skeletonId + szakma-kapacitás + kezdő dátum → projektenkénti/műveletenkénti naptári kezdés/vég, makespan-dátum, szakma-kihasználtság.
- CON-001: A C# DateTime determinisztikus (a kezdő dátum input) — nincs "most" függés.

## 2. Implementation Steps
- TASK-001 ✅: `Production/WorkCalendar.cs` — AtWorkHours (munkaóra → DateTime), hétvége-átugrás, kezdet munkanapra igazítva. Determinisztikus.
- TASK-002 ✅: `Production/ResourceScheduler.cs` — list scheduling: job-onként CPM-ES (Scheduler), ES-prioritás, szakmánként azonos dolgozók (a leghamarabb szabaddá válóra); makespan + szakma-foglaltság + kihasználtság.
- TASK-003 ✅: `skeleton_schedule_projects` tool (string[] skeletonIds, asztalos/cnc/összeszerelő kapacitás, startDate ISO, workdayHours) → projektenkénti/műveletenkénti naptári kezdés/vég, makespan-dátum, szakma-kihasználtság %.
- TASK-004 ✅: 10 unit teszt (WorkCalendarTests 5: nap-átfordulás/hétvége-skip/igazítás; ResourceSchedulerTests 5: kapacitás 1 sorbaállít / kapacitás 2 párhuzam / precedencia / 100% kihasználtság / 2 projekt > 1) → össz. **73 teszt PASS**; smoke **25 tool** → **PASS**.

**Eredmény:** 2 projekt közös 1-1 dolgozóval → makespan 0,307 nap (vs 1 projekt 0,192); szakma-kihasználtság Összeszerelő 65,4% (szűk keresztmetszet), Asztalos 31,6%, CNC 31,1%. Ez a válasz a "mikor tud egy szakma egyszerre több projekten dolgozni" kérdésre: a bottleneck látszik, és több dolgozóval csökkenthető.

## 3. Risks & Assumptions
- ASSUMPTION-001: azonos szakmán belül a dolgozók egyformák (identical parallel machines list scheduling — a szabaddá váló dolgozóra tesszük a műveletet).
- ASSUMPTION-002: a munkanap folytonos WorkdayHours; a művelet átnyúlhat napokon (a munkaóra-tér leképzése kezeli).
- ASSUMPTION-003: alap munkanapok hétfő–péntek; a WorkdayHours alap 8.
