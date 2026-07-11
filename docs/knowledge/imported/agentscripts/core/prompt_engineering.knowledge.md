---
name: prompt-engineering-core
description: 'Core Communication Schema and Logic Patterns for multi-agent cognitive setup. Use when agents need shared reasoning patterns (Persona, Chain-of-Thought, Guard Rails) for consistent communication.'
domain: core
last_updated: 2026-02-24
---

# **?? Core Communication Schema (Prompt Engineering)**

Ez a dokumentum definiálja a csapat közös "kognitív nyelvét". Nem csak az Orchestratornak, hanem **minden ágensnek** ismernie kell ezeket a mintákat a hatékony kommunikációhoz és a feladatok végrehajtásához (Cognitive Setup).

## **1. Alapvetõ Mûködési Minták (Core Logic Patterns)**

Ezek a minták határozzák meg, *hogyan gondolkodjon* az ágens.

### **A. Perszóna Minta (Persona Pattern)**

* **Mikor**: Minden új ágens/session indításakor.
* **Cél**: A válasz stílusának, mélységének és szakmai szókincsének beállítása.
* **Utasítás**: "Viselkedj úgy, mint egy [Senior .NET Architect], aki megszállottja a [Clean Architecture]-nek."

### **B. Gondolati Lánc Minta (Chain of Thought Pattern)**

* **Mikor**: Komplex algoritmusok, hibakeresés vagy architekturális döntések esetén.
* **Cél**: Kényszeríteni a modellt, hogy "hangosan gondolkodjon", csökkentve a hallucinációt.
* **Utasítás**: "Mielõtt megírnád a kódot, adj egy lépésrõl-lépésre útmutatót a gondolatmenetedrõl."

### **C. ReACT Minta (Reasoning + Acting)**

* **Mikor**: Ha külsõ eszközöket (keresés, fájl olvasás) kell használni.
* **Cél**: A gondolkodás (Reasoning), cselekvés (Acting) és megfigyelés (Observation) ciklusának fenntartása.
* **Utasítás**: "Gondolkodj végig lépésrõl lépésre. Ha információd hiányzik, használd a [Fájl olvasás] eszközt."

### **D. Kognitív Ellenõrzõ Minta (Cognitive Verifier Pattern)**

* **Mikor**: Ha a feladat leírása kétértelmû.
* **Cél**: A félreértések elkerülése kérdezés által.
* **Utasítás**: "Ha a feladatleírás alapján nem tudsz egyértelmû tervet készíteni, generálj tisztázó kérdéseket."

## **2. Strukturális és Kimenet-vezérlõ Minták**

Ezek a minták határozzák meg, *milyen formában* érkezzen a válasz.

### **E. Formátum / Sablon Minta (Template Pattern)**

* **Mikor**: Implementation Report, QA Sign-off, ADR írásakor.
* **Cél**: Gépi feldolgozásra alkalmas, konzisztens dokumentáció.
* **Utasítás**: "Töltsd ki a sablont a saját tartalmaddal, de õrizd meg a Markdown struktúrát."

### **F. Közönség Minta (Audience Pattern)**

* **Mikor**: Dokumentáció írása különbözõ szintekre.
* **Cél**: A tartalom bonyolultságának hangolása.
* **Utasítás**: "Magyarázd el ezt úgy, mintha [egy Junior fejlesztõnek] írnád."

### **G. Vizualizációt Meghatározó Minta (Visualization Pattern)**

* **Mikor**: Folyamatok, adatmodellek tervezésekor.
* **Cél**: Mermaid diagramok kérése.
* **Utasítás**: "Generálj egy [Mermaid sequence diagramot] az adatfolyamról."

### **H. Tény Összefoglaló Minta (Fact Summary Pattern)**

* **Mikor**: Hosszú elemzések, logok áttekintésekor, Task lezárásakor.
* **Cél**: A lényeg kiemelése a "zajból" (Context Hygiene).
* **Utasítás**: "Állíts össze egy bullet-point listát a legfontosabb tényekrõl a válaszod végére."

## **3. Minõségbiztosítási és Kritikai Minták**

### **I. Tény Ellenõrzõ Minta (Fact Check Pattern)**

* **Mikor**: Code review, Architect plan review.
* **Cél**: Hallucinációk és logikai hibák kiszûrése.
* **Utasítás**: "Ellenõrizd szigorúan a tényeket és a szabályokat helyettem! Sorold fel a potenciális hibákat."

### **J. Reflexiós Minta (Reflection Pattern)**

* **Mikor**: Hibajavítás (Error Recovery) során.
* **Cél**: Az ágens saját megoldásának kritikai felülvizsgálata.
* **Utasítás**: "Magyarázd el az érvelésedet. Miért ezt a megoldást választottad? Mik a korlátai?"

### **K. Alternatív Megközelítõ Minta (Alternative Approach Pattern)**

* **Mikor**: Tervezési fázisban (Architect/Tech Lead).
* **Cél**: A "csõlátás" elkerülése.
* **Utasítás**: "Sorold fel a legjobb 3 megközelítést, hasonlítsd össze õket, és indokold a választást."

## **4. Speciális Interakciós Minták**

### **L. Menü Akció Minta (Menu Action Pattern)**

* **Mikor**: Interaktív session.
* **Példa**: "Ha azt írom 'status', generálj helyzetjelentést."

### **M. Válasz Elhárító Minta (Refusal Pattern)**

* **Mikor**: Ha a kérés Constraints-be ütközik.
* **Utasítás**: "Ha a kérés sérti a szabályokat, utasítsd el, és javasolj helyes alternatívát."

### **N. N-lépéses Minta (N-shot Prompting)**

* **Mikor**: Kódgenerálásnál a stílus illesztéséhez.
* **Utasítás**: "Példa 1: [Kód A]. Példa 2: [Kód B]. Feladat: Írd meg a [Kód C]-t ebben a stílusban."

## **5. Hogyan állíts össze egy Mega-Promptot?**

**Konstrukciós sorrend:**

1. **Perszóna** (Ki vagy?)
2. **Közönség** (Kinek írsz?)
3. **Kontextus** (Bemeneti fájlok, N-shot példák)
4. **Feladat** (Mit kell tenni?)
5. **Logikai Minta** (ReACT vagy CoT vagy Alternatívák)
6. **Korlátok** (Refusal rules, Constraints)
7. **Kimenet** (Formátum/Sablon, Vizualizáció, Tény összefoglaló)
