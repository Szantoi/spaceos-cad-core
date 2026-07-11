---
name: orchestrator-dispatching
description: 'Task dispatching and dependency management protocol for the Orchestrator. Use when making dispatch decisions, checking Epic/Task dependencies, or routing work to specialist agents.'
domain: management
last_updated: 2026-02-24
---

# **?? Orchestrator: Feladat-kiosztás és Függõségkezelés**

Ez a skill definiálja, hogyan dönti el az Orchestrator, hogy melyik feladatot (Task) indítsa el, melyik ágenssel (Role), és hogyan validálja a függõségeket a projekt dokumentációja alapján.

## **1\. Függõség-ellenõrzési Protokoll (MANDATORY)**

Mielõtt bármilyen feladatot kiosztana, az Orchestrator-nak kötelezõ lefolytatnia a következõ konzultációkat:

### **A. Epic-szintû függõségek (Architect konzultáció)**

* **Mûvelet**: Az Orchestrator-nak be kell olvasnia az Architect által készített Epic terveket (docs/{project}/epics/{EPIC}/plan.md) és az ADR-eket.
* **Kérdés**: "Van-e az aktuális Epic-nek olyan függõsége más, még le nem zárt Epic-tõl, amely blokkolja az indítást?"
* **Szabály**: Ha az Architect terve szerint az Epic függõségei nem teljesültek, a feladat nem indítható.

### **B. Task-szintû függõségek (Tech Lead konzultáció)**

* **Mûvelet**: Az Orchestrator-nak be kell olvasnia a Tech Lead által készített részletes task terveket (docs/{project}/epics/{EPIC}/tasks/TASK-XX.md).
* **Kérdés**: "Milyen belsõ függõségei vannak a tasknak az Epic-en belül? Létezik-e szekvenciális kényszer (pl. elõbb adatbázis, aztán API)?"
* **Szabály**: Csak olyan task indítható, amelynek minden elõfeltétele (Pre-requisites) "Done" státuszú.

## **2\. Feladat-kiosztási Logika (Dispatching)**

Az Orchestrator a következõ sorrendben hoz döntést:

1. **Scan**: Backlog és State fájlok elemzése (backlog.md, state.md).
2. **Prioritization**:
   * Blokkoló hibák (Bugfix) javítása az elsõ.
   * Tech Lead által meghatározott prioritási sorrend követése.
3. **Quality Gate**:
   * Ha egy terv (Epic/Task Plan) elkészült, de még nincs hozzá Critique Report:
   * INDÍTSA EL az Ördög ügyvédjét (devil_advocate.runbook.md).
   * Csak akkor engedje tovább a Developernek, ha a státusz "ELFOGADVA" vagy "Approved with Suggestions".
4. **Availability**: Van-e szabad "szál" a párhuzamosításhoz?
5. **Role Selection**:
   * **Tervezési feladat** (nincs TASK-XX.md) \-\> tech\_lead\.role.md
   * **Backend fejlesztés** \-\> backend\_developer\.role.md
   * **Frontend fejlesztés** \-\> frontend\_developer\.role.md
   * **Adatbázis / Infra** \-\> backend\_developer\.role.md
   * **Verifikáció / Tesztelés** \-\> qa\_tester\.role.md

## **3\. Indítási Protokoll (Execution Start)**

Amikor az Orchestrator kiválaszt egy taskot és egy szerepkört:

1. **Context Slicing**: A **Knowledge Steward**\-dal kitakaríttatja a memóriát, hogy csak a releváns Epic/Task információ maradjon bent.
2. **Initialization**: Betölti a cél-ágens Runbook-ját.
3. **Task Assignment**: Átadja a Task ID-t és a Tech Lead tervét.
4. **Locking**: Megjelöli az érintett fájlokat a state.md-ben mint "In Progress", hogy elkerülje a párhuzamos írási ütközéseket.

## **4\. Döntési Mátrix Példa**

| Ha a Task típusa... | És a függõség... | Akkor a mûvelet... |
| :---- | :---- | :---- |
| API fejlesztés | DB migráció nincs kész | **WAIT** (Várni a DB taskra) |
| UI Komponens | API endpoint kész | **START** \-\> Frontend Dev |
| Új Feature | Epic stratégia hiányzik | **ESCALATE** \-\> Architect |
| Bármi | Kontextus \> 60% | **STOP** \-\> Knowledge Steward takarítás |

### **? Végrehajtási instrukció az Orchestrator-nak:**

*"Soha ne indíts el taskot anélkül, hogy az Architect tervébõl (Epic függõség) és a Tech Lead leírásából (Task függõség) ne igazoltad volna a futtathatóságot."*
