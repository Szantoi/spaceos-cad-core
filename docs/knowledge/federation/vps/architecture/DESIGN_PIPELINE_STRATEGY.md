# SpaceOS — Design Pipeline Strategy
## Tervezési elvek és döntési sorrend

> **Verzió:** v1.0 — 2026-04-12
> **Döntéshozó:** Gábor (Architect & Founder)
> **Kontextus:** Session kickoff 2026-04-12 — pipeline értékelés után rögzítve

---

## 1. Tervezési szabályok

| # | Szabály | Indoklás |
|---|---------|----------|
| R1 | **Egy session = egy jól behatárolható feladat** | Fókusz > szélesség. A tervdok scope-ja legyen implementálható egységenként. |
| R2 | **Csak akkor mélyítünk, ha a prereq-ek megvannak** | Prereq = DONE vagy DEPLOYED, nem READY vagy CODE_COMPLETE. |
| R3 | **Nem csinálunk strukturális adósságot prezentáció kedvéért** | Ha a feltételek nem állnak fenn, nem tervezünk "előre" csak azért, hogy legyen output. |
| R4 | **A design pipeline a Claude.ai workspace dolga; az implementáció a Claude Code csapaté** | Tiszta felelősségszétválasztás: architektúra itt, kód ott. |

---

## 2. Prereq-mátrix — mi tervezhető mikor

| Tervezendő fázis | Prereq-ek | Prereq státusz (2026-04-12) | Tervezhető? |
|-------------------|-----------|-----------------------------|-------------|
| **Modules.Cutting extraction** | ADR-019 ✅ · Joinery v1 DONE ✅ · Abstractions A+B DONE ✅ | Mind DONE | ✅ IGEN |
| Modules.Spatial extraction | ADR-019 ✅ · Phase 3A DONE ✅ · Cutting design szükséges | Részben | ⏳ Cutting után |
| Cabinet v1 | Ecosystem Actor deployed · Cutting deployed · Abstractions DONE | Ecosystem: READY (nem deployed) | ❌ NEM — prereq hiányzik |
| Window placeholder | Ecosystem Actor deployed · Cutting deployed | Ecosystem: READY (nem deployed) | ❌ NEM |
| Escrow WORM GA | Doorstar pilot live · S3/Azure döntés | Pilot: ops blocker | ❌ NEM |

---

## 3. Aktuális döntési sorrend

```
[MOST] Modules.Cutting extraction — tervezés (prereq-ek DONE)
         │
         ▼
[KÖVETKEZŐ] Modules.Spatial extraction — tervezés (ha Cutting design DONE)
         │
         ▼
[VÁRMÉG] Cabinet v1 — tervezés (ha Ecosystem Actor + Cutting DEPLOYED)
         │
         ▼
[VÁRMÉG] Window placeholder — tervezés (ha Ecosystem Actor + Cutting DEPLOYED)
```

**Párhuzamosan (Claude Code csapat):**
- MSG-057 Stage Registry tesztek
- Ecosystem Actor v4 implementáció
- Joinery v2 implementáció (MSG-057 DoD után)

---

## 4. Frissítési szabály

Ez a dokumentum minden session kickoff-nál felülvizsgálandó:
1. Prereq-mátrix frissítése (státuszok)
2. Ha prereq DEPLOYED-re vált → következő fázis tervezhetővé válik
3. Új sor hozzáadása ha új tervezési igény merül fel

**Nem frissül:** A szabályok (Section 1) — ezek állandóak.

---

*SpaceOS — Design Pipeline Strategy | 2026. április | Architect*
