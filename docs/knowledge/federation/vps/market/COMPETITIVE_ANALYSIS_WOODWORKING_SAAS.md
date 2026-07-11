# Competitive Analysis — Woodworking SaaS (2026)

**Készítette:** Librarian (Explorer research alapján)
**Forrás:** MSG-EXPLORER-004-DONE (2026-06-22)
**Utolsó frissítés:** 2026-06-22

---

## Összefoglaló

A faiparos szoftver piac 2026-ban két fő szegmensre oszlik:
1. **Enterprise CAD/CAM** (Cabinet Vision ~$5000+) — nagy gyártók, desktop, teljes 3D CAD
2. **Cut Optimization** (CutList Plus ~$89) — kis műhelyek, desktop, csak anyaglista optimalizálás

**SpaceOS pozíció:** **Freemium SaaS** + **cloud-native** + **end-to-end workflow** → **egyedi piaci pozíció** a magyar KKV szegmensben.

---

## 1. Cabinet Vision (Hexagon)

### 1.1 Árazás

| Tier | Ár | Tartalom |
|------|-----|----------|
| **Perpetual License** | $5,000+ (one-time) | Alap modulok, lifetime használat |
| **Subscription Basic** | $99/hó | Core módlok (engineering) |
| **Subscription Enterprise** | Custom pricing | Teljes modular suite |

**Modular Pricing Model:**
- Core Cabinets / Core Closets (alap engineering)
- Design (sales, renderings, pricing estimates)
- xCountertops, xCRM, xReporting, xOptimizer, xBidding, xShaping, xRendering

**Tipikus teljes ár:** $5,000-$15,000+ (perpetual + modulok)

### 1.2 Fő Funkciók

**Design & CAD:**
- 3D parametrikus szekrénytervező
- Fotorealisztikus rendering (xRendering modul)
- Anyagköltség kalkuláció (pricing estimates)

**Manufacturing:**
- CNC integráció (közvetlen export Homag, Biesse, SCM gépekhez)
- Cut optimization (xOptimizer modul)
- Manufacturing reports (hardware list, cutting list)

**Business:**
- CRM integráció (xCRM modul)
- Bidding/Quotation (xBidding modul)
- Reporting (xReporting modul)

### 1.3 Technológia

- **Platform:** Windows desktop only
- **CAD engine:** Proprietary 3D CAD
- **Deployment:** On-premise installation
- **Cloud:** Nincs cloud verzió (csak desktop sync)

### 1.4 Célpiac

- **Méret:** Közepes-nagy szekrénygyártók (20+ fő)
- **Árbevétel:** $1M+ (affordability threshold)
- **Terület:** USA, Kanada, DACH (német nyelvű piac)
- **Iparág:** Custom cabinetry, closet manufacturers

### 1.5 Erősségek

✅ **Érett CAD/CAM technológia** — 20+ év fejlesztés
✅ **Teljes manufacturing pipeline** — design → CNC export
✅ **3D rendering** — sales presentation quality
✅ **Established brand** — Hexagon backing (enterprise trust)

### 1.6 Gyengeségek

⚠️ **Magas belépési küszöb** — $5000+ small shops számára prohibitív
⚠️ **Desktop only** — nincs mobil/tablet/cloud access
⚠️ **Windows only** — Mac/Linux users kizárva
⚠️ **Steep learning curve** — 40+ hours training ajánlott

---

## 2. CutList Plus

### 2.1 Árazás

| Verzió | Ár | Tartalom |
|--------|-----|----------|
| **Standard** | $89 (one-time) | 1D + 2D cut optimization |
| **FX** | $199 (one-time) | + Advanced features, database |

**Ritka a piacon:** One-time purchase (legtöbb konkurens SaaS subscription)

### 2.2 Fő Funkciók

**Cut Optimization:**
- 1D optimization (lumber, linear cuts)
- 2D optimization (plywood, sheet goods)
- Blade kerf allowance
- Grain direction consideration

**Output:**
- Printable cutting diagrams
- Material waste report
- Cost calculator

**Adatbázis (FX):**
- Material library
- Custom material types

### 2.3 Technológia

- **Platform:** Windows desktop only
- **UI:** Early 2000s style (nem modern)
- **Cloud:** Nincs
- **Mobile:** Nincs
- **Collaboration:** Nincs (single-user only)

### 2.4 Célpiac

- **Méret:** Kis műhelyek (1-5 fő), egyéni vállalkozók
- **Árbevétel:** <$500k (hobbyists → small business)
- **Terület:** USA (angol nyelvű piac)
- **Iparág:** General woodworking (nem csak szekrény)

### 2.5 Erősségek

✅ **Low price point** — $89 affordable hobbi asztalosoknak is
✅ **Perpetual license** — no subscription fatigue
✅ **Simple focus** — cut optimization only (easy to learn)

### 2.6 Gyengeségek

⚠️ **Limited scope** — csak cut list, nincs design/joinery/sales
⚠️ **Outdated UI** — early 2000s Windows forms
⚠️ **No collaboration** — single-user, no cloud sync
⚠️ **Windows only** — Mac users kizárva

---

## 3. SpaceOS Pozicionálás

### 3.1 Differenciáció vs Cabinet Vision

| Aspektus | Cabinet Vision | SpaceOS | Versenyelőny |
|----------|---------------|---------|--------------|
| **Ár** | $5000+ | **Freemium (ingyenes tier)** | ✅ **KRITIKUS** — magyar KKV-k affordability |
| **Platform** | Windows desktop | **Web-based SaaS** | ✅ Mobil/tablet/bárhonnan elérhető |
| **Deployment** | On-premise | **Cloud-native** | ✅ Nincs IT infrastruktúra szükség |
| **3D CAD** | Fotorealisztikus | **Parametrikus konfiguráció** | ⚠️ Gyengébb (MVP: 2D rajz elég) |
| **CNC integráció** | Közvetlen driver | **CSV export** | ⚠️ Gyengébb (de elég legtöbb CNC-hez) |
| **Nyelv** | Angol/Német | **Magyar** | ✅ Magyar piac kizárólagos |
| **Workflow** | Design + CAD/CAM | **Design + Cutting + Joinery + Sales** | ✅ End-to-end (nem csak CAD) |

**Ajánlott üzenet:**
> "Cabinet Vision profi CAD szoftver nagy gyártóknak. SpaceOS **ingyenes** alternatíva kis-közepes műhelyeknek, akiknek **nincs $5000**-juk szoftverre, de **modern cloud megoldást** akarnak."

### 3.2 Differenciáció vs CutList Plus

| Aspektus | CutList Plus | SpaceOS | Versenyelőny |
|----------|-------------|---------|--------------|
| **Ár** | $89 (one-time) | **Freemium** | ✅ Ingyenes tier alacsonyabb küszöb |
| **Scope** | Cut optimization only | **Teljes workflow** | ✅ **KRITIKUS** — design, joinery, procurement, sales |
| **Collaboration** | Single-user | **Team collaboration** | ✅ Multi-user, role-based access |
| **Cloud** | Nincs | **Cloud-native** | ✅ Cross-device sync |
| **Nesting** | 10+ év algoritmus | **Új engine (2026)** | ⚠️ Még nem tesztelt nagy mennyiségen |
| **UI** | Early 2000s | **Modern React UI** | ✅ Modern UX |

**Ajánlott üzenet:**
> "CutList Plus jó cut optimization eszköz, de **csak** anyaglista optimalizálás. SpaceOS **teljes workflow**: design → cutting → joinery → sales."

### 3.3 SpaceOS Egyedi Pozíció (Blue Ocean)

**Pozícionálás:** **Freemium Cloud-Native End-to-End Workflow for Hungarian Woodworking SMBs**

| Dimenzió | SpaceOS Érték |
|----------|---------------|
| **Pricing** | Freemium (ingyenes tier $0, paid tier $29-99/hó) |
| **Deployment** | Cloud-native SaaS (web + mobile) |
| **Workflow** | End-to-end (design, cutting, joinery, procurement, sales) |
| **Language** | Magyar (+ angol Later) |
| **Target** | KKV (5-50 fő, $200k-$5M árbevétel) |
| **Geography** | Magyarország (+ DACH Later) |

**Blue Ocean Strategy:**
- Cabinet Vision: Enterprise, magas ár → SpaceOS: KKV, freemium
- CutList Plus: Cut only → SpaceOS: Teljes workflow
- Mindkettő: Desktop → SpaceOS: Cloud-native

---

## 4. Kockázatok és Mitigáció

### 4.1 Kockázat: CAD/CAM Érettség Hiánya

**Probléma:**
- Cabinet Vision 20+ év CAD tapasztalat, fotorealisztikus rendering
- SpaceOS: parametrikus konfiguráció, 2D rajz, nincs 3D rendering

**Kockázat:**
- Nagy asztalosok (50+ fő) Cabinet Vision-nél maradnak
- Sales prezentáció gyengébb (nincs fotorealisztikus kép)

**Mitigáció:**
1. **MVP fókusz:** Doorstar (első ügyfél) nem igényel 3D CAD → parametrikus ajtó konfiguráció elég
2. **3D Rendering Later:** Ha igény van (Q4 2026+), 3D rendering modul fejleszthető
3. **Célpiac szűkítés:** 5-50 fős műhelyek, nem 50+ fős enterprise

**Elfogadott trade-off:** SpaceOS **nem** akar Cabinet Vision lenni. Célpiac: kis-közepes műhelyek, akiknek **nincs $5000**-juk.

### 4.2 Kockázat: CNC Integráció Limitáció

**Probléma:**
- Cabinet Vision közvetlen CNC driver (Homag, Biesse, SCM)
- SpaceOS: CSV export (cutting list)

**Kockázat:**
- CNC operátorok manuális CSV import-ot preferálják auto import helyett

**Mitigáció:**
1. **CSV elég a legtöbb CNC-hez:** Homag, Biesse gépek támogatják CSV import-ot
2. **CNC driver Later:** Ha igény van, közvetlen driver fejleszthető (Q4 2026+)
3. **Integration partner:** Homag/Biesse partnership (Later)

**Elfogadott trade-off:** CSV export **elég** MVP-hez. Közvetlen driver **Later** ha enterprise tier kell.

### 4.3 Kockázat: Nesting Algoritmus Érettsége

**Probléma:**
- CutList Plus 10+ év nesting algoritmus fejlesztés
- SpaceOS: új nesting engine (2026 Q2) — **még nem tesztelt** nagy mennyiségen

**Kockázat:**
- Nesting optimalizálás gyengébb (több anyagpazarlás)
- Doorstar visszajelzés: "CutList Plus jobb nesting-et ad"

**Mitigáció:**
1. **Iteratív fejlesztés:** Doorstar feedback alapján algoritmus finomhangolás
2. **Benchmark:** CutList Plus vs SpaceOS nesting összehasonlítás
3. **Manual override:** Operátor módosíthatja a nesting eredményt

**Monitoring:** Nesting waste % tracking (cél: <5% waste, CutList Plus szint)

---

## 5. Go-To-Market Stratégia

### 5.1 Freemium Tier Definition

**Ingyenes tier (Doorstar Soft Launch célpiac):**
- 1-2 user
- 10 order/hó
- Basic cut optimization
- Cloud storage 1 GB

**Paid tier ($29-99/hó):**
- Unlimited users
- Unlimited orders
- Advanced nesting
- Integration (CNC CSV, procurement API)
- Priority support

### 5.2 Pozicionálási Üzenetek

**vs Cabinet Vision:**
> "Cabinet Vision profi CAD szoftver $5000+ áron. SpaceOS **ingyenes** alternatíva magyar kis-közepes műhelyeknek, modern cloud megoldással."

**vs CutList Plus:**
> "CutList Plus csak anyaglista optimalizálás. SpaceOS **teljes workflow**: design, cutting, joinery, sales — egy helyen, cloudban."

**Blue Ocean:**
> "Az első **magyar nyelvű, felhőalapú, teljes körű** workflow management faiparosoknak. Ingyenes indulás, fizetés csak ha kinövöd."

### 5.3 Célpiac Szegmentáció (HU)

| Szegmens | Méret (cég) | Árbevétel | SpaceOS Tier | Penetráció cél |
|----------|-------------|-----------|--------------|----------------|
| **Micro** | 1-5 fő | <$200k | Free | 50% (650 cég) |
| **Small** | 5-20 fő | $200k-$1M | Paid $29/hó | 30% (450 cég) |
| **Medium** | 20-50 fő | $1M-$5M | Paid $99/hó | 10% (100 cég) |
| **Large** | 50+ fő | $5M+ | ❌ Nem célpiac | 0% |

**Total addressable market (HU):** 1,300-2,500 cég
**Target penetration (2027):** 5-10% (65-250 cég)

---

## 6. Versenyhelyzet Monitoring

### 6.1 Figyelendő Metrikák

**Cabinet Vision:**
- Pricing változások (subscription tier árak)
- Cloud SaaS launch (ha bejelentik)
- DACH expansion (német piac)

**CutList Plus:**
- Pricing változások ($89 → SaaS?)
- Cloud version launch
- Collaboration feature

**Új belépők:**
- Magyar startup-ok (faipar SaaS)
- Általános ERP-k (SAP, Odoo woodworking module)

### 6.2 Early Warning Signals

⚠️ **Cabinet Vision cloud launch** → SpaceOS egyedi pozíció gyengül
⚠️ **CutList Plus SaaS pivot** → direct competitor freemium térben
⚠️ **Magyar startup konkurens** → language + local knowledge advantage elvész

**Monitoring frekvencia:** Quarterly review (Q1, Q2, Q3, Q4 2026)

---

## 7. Ajánlások

### 7.1 Azonnal Alkalmazható

✅ **Marketing messaging:**
- "Ingyenes alternatíva $5000+ Cabinet Vision-höz"
- "Teljes workflow, nem csak cut list"
- "Magyar nyelvű, felhőalapú, bárhonnan elérhető"

✅ **Pricing strategy:**
- Freemium tier (1-2 user, 10 order/hó)
- Paid tier $29-99/hó (scalable)

✅ **Célpiac focus:**
- 5-50 fős műhelyek (small-medium SMB)
- Magyar piac (language advantage)

### 7.2 Roadmap Prioritások

**Q3 2026:**
1. Doorstar feedback alapján nesting optimization
2. Freemium tier finomhangolás
3. Magyar marketing anyagok (vs Cabinet Vision comparison)

**Q4 2026:**
1. Competitive benchmark (CutList Plus nesting test)
2. 3D rendering feasibility study (ha Doorstar igényel)
3. DACH expansion planning (német nyelvű piac)

**2027:**
1. Enterprise tier (dedicated DB, CNC driver)
2. 3D CAD modul (ha demand van)
3. Hybrid tiering (Later)

---

## 8. Források

**Cabinet Vision:**
- [CABINET VISION - Pricing, Features, and Details in 2026](https://www.softwaresuggest.com/cabinet-vision)
- [CABINET VISION | Hexagon](https://hexagon.com/products/product-groups/computer-aided-manufacturing-cad-cam-software/cabinet-vision)
- [Cabinet Vision Reviews, Pricing, Features & Alternatives in 2026](https://nerdisa.com/cabinetvision)

**CutList Plus:**
- [CutList Plus Cutting Diagram Software](https://cutlistplus.com/)
- [Best Cut List Optimizer Software 2026](https://cutplan.ai/en/blog/best-cut-list-optimizer-software-2026.html)
- [Cut List Optimizer](https://cutlistoptimizer.com/)

**Explorer Research:**
- MSG-EXPLORER-004-DONE (2026-06-22)

---

## 9. Changelog

| Dátum | Verzió | Változás |
|-------|--------|----------|
| 2026-06-22 | v1.0 | Initial competitive analysis (Librarian synthesis) |

---

**Következő review:** 2026-09-22 (Q3 review)
