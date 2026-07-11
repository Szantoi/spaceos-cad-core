---
goal: Ütemezési DAG (CPM) — a 02 Folyamatok modern kiváltása: átfutási idő + kritikus út
version: 1.0
date_created: 2026-07-10
owner: Cabinet root
status: 'Done'
tags: [feature, mcphost, scheduling, cpm, doorstar, modernization]
---

# Bevezetés

A legacy Egység_idő FS/SS függőségeit (Szülő feladat + Feladat Függőség Tipusa + Késleltetés)
modern CPM-ütemezővé alakítjuk: a mért egységidőkből átfutási idő (lead time) + kritikus út +
szabad tartalék (slack) művelet-szinten. Ez a "02 Folyamatok.xlsm" kapacitás/átfutás-tervezésének
modern, tiszta változata. A KÖLTSÉG (mancsóra) már megvan (LaborEstimator); ez az IDŐ dimenzió.

## 1. Requirements & Constraints
- REQ-001: Az Operation bővül függőségekkel: `DependsOn: [{ OnOperationId, Type(FS/SS), LagHours }]`.
- REQ-002: A művelet ütemezési időtartama = ProcessHours (egységidő × illeszkedő darab); a Humánerő KAPACITÁS (crew), nem időtartam-osztó (dokumentálva).
- REQ-003: CPM: forward pass (ES/EF, FS/SS + lag), lead time = max EF; backward pass (LS/LF), slack, kritikus út (slack≈0).
- REQ-004: Ciklus-detektálás (Kahn topo-rendezés) → értelmes hiba.
- REQ-005: `skeleton_production_schedule` tool — ütemezett műveletek + leadTimeHours/Days + kritikus út (webre kész JSON).
- CON-001: 8 órás munkanap a nap-konverzióhoz (leadTimeDays = hours/8). A lag a valós adatban napban van → órára váltva tároljuk.

## 2. Implementation Steps
- TASK-001 ✅: Operation bővítés (OperationDependency + DependencyType FS/SS + DependsOn, default üres → a LaborEstimator tesztek nem törnek).
- TASK-002 ✅: OperationCatalog korpusz-lánc (Szabás→CNC→Élzárás→Csiszolás→Összeállítás; Hátlap párhuzamos → Összeállítás).
- TASK-003 ✅: `Production/Scheduler.cs` — CPM: Kahn topo (ciklus-detektálás) + forward (ES/EF, FS/SS+lag) + backward (LS/LF) + slack + kritikus út. Kihagyott (0-darab) művelet + rá mutató függőség kezelve.
- TASK-004 ✅: `skeleton_production_schedule` tool (leadTimeHours/Days, criticalPath, műveletenként ES/EF/slack/critical).
- TASK-005 ✅: 6 unit teszt (SchedulerTests: FS-lánc, párhuzamos slack, SS+lag, ciklus-hiba, kihagyott művelet, valós korpusz kritikus út) → össz. **63 teszt PASS**; smoke **23 tool** → **PASS**.

**Eredmény:** átfutási idő 1,5336 h (0,192 nap), kritikus út a korpusz-lánc; a hátlap-szabás párhuzamos → tartalékkal, nem kritikus. Fontos különbség: mancsóra 1,72 (teljes munkaráfordítás) vs. átfutás 1,53 (kritikus úti fal-idő) — ez a Folyamatok kapacitás/átfutás dimenziója.

## 3. Risks & Assumptions
- ASSUMPTION-001: időtartam = ProcessHours (szekvenciális, egy crew/művelet); a Humánerő a kapacitás.
- ASSUMPTION-002: a slack≈0 küszöb 1e-6 óra (lebegőpontos tolerancia).
