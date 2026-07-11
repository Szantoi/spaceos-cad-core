# SpaceOS — Elavult megközelítések jegyzéke
## Offset-tábla modell → Product Graph Engine migráció

> **Verzió:** v1.0 — 2026-04-09
> **Típus:** Deprecation Notice + Migration Guide
> **Érintett dokumentumok:** Modules.Joinery v4.md, v4.1 Errata, v4.2 Errata

---

## 1. Mi változik és miért

### Az elavult megközelítés (v4 — v4.2)

A Modules.Joinery v4 sorozat **statikus offset-táblákat** használt a méretszámításhoz:

```
DoorTypeRules       → fix offset értékek ajtó típusonként
PartDimensionRules  → fix multiplier + base képletek
CuttingConstants    → fix anyag + vastagság kombinációk
GlobalConstants     → globális konstansok (fúga, ráhagyás)
```

**Ez a módszer működik**, de korlátai vannak:
- Új termék (szekrény, ablak) = teljesen új táblastruktúra
- Felhasználó nem tud új terméket definiálni kód nélkül
- A tábla nem fejezi ki az alkatrészek közötti kapcsolatot
- A CNC és a folyamatszervezés nem deriválható belőle
- Nem illeszthető CAD feature tree-hez

### Az új megközelítés (Product Graph Engine)

A termék = **gráf** (csomópontok + élek), ahol:
- A csomópontok az alkatrészek (ComponentSlot)
- Az élek a csatlakozások (SlotConnection) — egyszerre méretszabály, fizikai illesztés, és CNC művelet
- A gráf topológiai bejárása adja a szabászlistát, CNC programot, és gyártási sorrendet

**Ugyanaz a motor** szolgál ki ajtót, szekrényt, ablakot — csak a gráf konfigurációja más.

---

## 2. Elavult dokumentumok

| Dokumentum | Státusz | Helyette |
|---|---|---|
| `SpaceOS_Modules_Joinery_v4.md` | ⚠️ **DEPRECATED** — DoorTypeRules DDL, PartDimensionRules, CuttingConstants | `SpaceOS_Product_Configuration_Engine_Vision_v1.md` + Abstractions arch doc |
| `SpaceOS_Modules_Joinery_v4_1_Errata.md` | ❌ **TÖRÖLT** — v4.2 felülírta, ami szintén elavult | — |
| `SpaceOS_Modules_Joinery_v4_2_Errata.md` | ⚠️ **DEPRECATED** — irányspecifikus offset-ek, 4 fúga | Product Graph Engine (TemplateParameter) |
| `Doorstar_Seed_Data_Template_v4_2.xlsx` | ⚠️ **DEPRECATED** — offset-tábla formátum | Új template: ProductTemplate import formátum |

### Mi marad érvényes a v4-ből

| Elem | Státusz | Megjegyzés |
|---|---|---|
| DoorOrder aggregate | ✅ Érvényes | Megrendelés kezelés változatlan |
| DoorItem entity | ✅ Érvényes (módosított) | DoorWidth/DoorHeight törlése (számított, nem bemenet) |
| DoorOrderStatus FSM | ✅ Érvényes | Draft → Submitted → InProduction → Completed |
| DoorType enum | ✅ Érvényes | Mint ProductTemplate.Name |
| OpeningDirection enum | ✅ Érvényes | Paraméter a template-ben |
| SurfaceSpec VO | ✅ Érvényes | Felület konfiguráció |
| HardwareSpec VO | ✅ Érvényes (bővített) | + fúga override (4 irány) |
| Security finding-ek (DB-01..SEC-07, BE-01..05) | ✅ Érvényes | RLS, RBAC, schema owner — mind vonatkozik |
| `spaceos_joinery` DB schema | ✅ Érvényes | Marad, de a konstans táblák kiváltódnak |

---

## 3. Mapping: régi tábla → új gráf

### DoorTypeRules → SlotConnection-ök

| DoorTypeRules mező (v4.2) | Graph Engine megfelelő |
|---|---|
| `TokOffsetWidth` | `SlotConnection(Root→TokBM, Axis=Width, Op=Subtract, Operand=10.0)` |
| `TokOffsetHeight` | `SlotConnection(Root→TokBM, Axis=Height, Op=Subtract, Operand=5.0)` |
| `TokFrameLeft` | `SlotConnection(TokBM→TokKM, Axis=Width, Op=Add, Operand=18.0)` |
| `TokFrameRight` | `SlotConnection(TokBM→TokKM, Axis=Width, Op=Add, Operand=18.0)` |
| `TokFrameTop` | `SlotConnection(TokBM→TokKM, Axis=Height, Op=Add, Operand=18.0)` |
| `TokFrameBottom` | `SlotConnection(TokBM→TokKM, Axis=Height, Op=Add, Operand=20.0)` |
| `BbmFixOffsetWidth` | `SlotConnection(TokBM→BbmFix, Axis=Width, Op=Subtract, Operand=3.0)` |
| `BoritasSzelesseg` | `SlotConnection(BbmFix→BkmFix, Axis=Width, Op=Add, Operand=12.0)` |
| `AjtólapCount` | `ComponentSlot("Ajtólap").Quantity = 1` |

### PartDimensionRules → ComponentSlot + SlotConnection

| PartDimensionRules mező | Graph Engine megfelelő |
|---|---|
| `ComponentName` | `ComponentSlot.Name` |
| `ComponentType` | `ComponentSlot.ComponentType` |
| `Material` | `ComponentSlot.DefaultMaterial` |
| `Thickness` | `ComponentSlot.DefaultThickness` |
| `Quantity` | `ComponentSlot.Quantity` |
| `WidthBase` + `WidthMultiplierFactor` + `WidthSourceDim` | `SlotConnection(parentSlot→thisSlot, Axis=Width, Op=Subtract/Add, Operand=WidthBase)` |
| `LengthBase` + `LengthMultiplierFactor` + `LengthSourceDim` | `SlotConnection(parentSlot→thisSlot, Axis=Height, Op=Subtract/Add, Operand=LengthBase)` |

### GlobalConstants → TemplateParameter

| GlobalConstants Key | TemplateParameter Key |
|---|---|
| `CuttingOversize` | `CuttingOversize` (ugyanaz) |
| `CladdingOverhang` | `CladdingOverhang` (ugyanaz) |
| `MatyiWidth` | `MatyiWidth` (ugyanaz) |
| `DefaultFugaTop` | `FugaTop` |
| `DefaultFugaBottom` | `FugaBottom` |
| `DefaultFugaLeft` | `FugaLeft` |
| `DefaultFugaRight` | `FugaRight` |

### CuttingConstants → SlotConnection CNC metadata

| CuttingConstants mező | Graph Engine megfelelő |
|---|---|
| `ComponentSlot` | `SlotConnection.JointType` + `MachiningOp` |
| `FrameMaterialH/V` | `ComponentSlot.DefaultMaterial` |
| `FrameThicknessH/V` | `ComponentSlot.DefaultThickness` |
| `FrameWidthOffsetH/V` | `SlotConnection.Operand` |
| `FrameCountH/V` | `ComponentSlot.Quantity` |

---

## 4. A klasszikus Excel-skin

A felhasználók, akik az offset-táblás módszert ismerik, **ugyanúgy dolgozhatnak**. A portálon egy "Klasszikus nézet" mutatja a megszokott tábla-formátumot, ami a háttérben a gráfot olvassa és írja:

```
Klasszikus nézet                    Háttérben
(amit a felhasználó lát)            (amit a rendszer csinál)

┌────────────────────────┐          ┌─────────────────────────┐
│ Tok offset szélesség:  │          │ SlotConnection          │
│ [ 10.0 ] mm            │    →     │   Root → TokBM          │
│                        │          │   Axis: Width            │
│                        │          │   Operator: Subtract     │
│                        │          │   Operand: 10.0          │
└────────────────────────┘          └─────────────────────────┘
```

**A régi Excel import is működik:** a rendszer a Doorstar Excel template-et beolvassa és ProductTemplate + SlotConnection rekordokat generál belőle. A felhasználónak nem kell tudnia, hogy a háttérben gráf van.

---

## 5. Migráció terv

| Lépés | Mi történik | Mikor |
|---|---|---|
| 1 | Modules.Abstractions architektúra megtervezése (arch-planner pipeline) | **Most** |
| 2 | Graph Engine implementáció (Abstractions Core) | Track 3 — ~15 nap |
| 3 | Manufacturing Derivation (JointType→CNC) | Track 3 — ~8 nap |
| 4 | Doorstar FAF_T ajtó = 1 ProductTemplate a Graph Engine-ben | Track 3 |
| 5 | Doorstar Excel template → ProductTemplate import script | Onboarding fázis |
| 6 | v4.2 offset-táblák végleges törlése | Soft Launch után |
| 7 | Klasszikus nézet (Portal UI) | Portal fejlesztés |

**A v4 tábla-struktúrák NEM implementálódnak.** A Graph Engine közvetlenül váltja ki őket.

---

*SpaceOS — Elavult megközelítések jegyzéke v1.0 · 2026-04-09*
*A Product Configuration Engine Vision dokumentum kísérő anyaga*
