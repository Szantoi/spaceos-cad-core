# SpaceOS — Growth Strategy
## Cutting Planning · FreeTier · PartnerTier — Unified Roadmap

> **Verzió:** v1.0 — 2026-04-17
> **Státusz:** STRATÉGIAI VÁZ — 3-4 arch-planner session ebből építkezik
> **Döntéshozó:** Gábor (Architect & Founder)
> **Kontextus:** Design session 2026-04-17 — 3 paradigmaváltás + egységes growth roadmap
> **Előzmények:** `SpaceOS_Modules_Cutting_Vision_v1.md` · `SpaceOS_Modules_Cutting_Core_Architecture_v4.md` (DEPLOYED) · `Codebase_Status_20260416.md`
> **Kapcsolódó jövőbeli tervdokumentumok:**
> - `SpaceOS_Modules_Contracts_Architecture_v4_1.md` (next)
> - `SpaceOS_Modules_Cutting_Planning_Architecture_v4.md` (following)
> - `SpaceOS_FreeTier_Architecture_v4.md` (Q3)
> - `SpaceOS_PartnerTier_Architecture_v4.md` (Q4)

---

## 0. Executive Summary

A mai session során a **Cutting Planning** tervezési beszélgetés három paradigmaváltást eredményezett, amelyek együtt **alapvetően átrajzolják a SpaceOS go-to-market stratégiáját**:

| # | Paradigmaváltás | Üzleti impact |
|---|-----------------|---------------|
| **1** | Cutting Planning: **N-day rolling plan + Strategy pattern everywhere** | Tenant-personalizáció, enterprise-ready flexibility |
| **2** | Public nesting endpoint → **FreeTier Anonymous Workspace** (permanent, save/share/return) | CAC validation eszköz + SEO asset + lead generation funnel |
| **3** | FreeTier → **B2B2C PartnerTier Channel Network** (szabászat white-label embedding) | Network effect · brand awareness · csatornapartner bevétel |

**Együttes scope:** ~63 fejlesztői nap 3 fázisban (v1 / v1.5 / v2), ~13 hét naptári idő ha szekvenciálisan.

**Kritikus elv:** _"Nem most implementáljuk, de az architektúra tudja hogy kelleni fog."_ A v1 Cutting Planning tervdokban **extension point-ok** kerülnek be (+2-3 nap), hogy v1.5 és v2 refactor nélkül ráfeküdjön.

---

## 1. Current State — Session indulási helyzet

**Codebase státusz (2026-04-16):**
- Minden tervdok lezárva, minden v4 dokumentum implementálva és DEPLOYED
- E2E: 214/214 passing · Összesített tesztszám: 2289
- Egyetlen nyitott tervdok: Modules.Joinery v2 (QuestPDF Gyártásilap) — BACKLOG, nem tervezési feladat

**Design-pipeline mutató:** `0 READY · 0 sürgős DESIGN` → ideális helyzet stratégiai ötleteléshez.

**Aktivált skillek:** `spaceos-session-kickoff` (kontextus) → ötletelés (nem arch-planner).

**Prerequisite matrix (`SpaceOS_Design_Pipeline_Strategy_v1.md`):** Cutting Planning fázis összes prereq DEPLOYED:
- Cutting Core v4 ✅
- Abstractions Phase A+B ✅
- Joinery v1 + ICuttingProvider ✅
- Inventory Core + Offcut ✅
- Contracts v4 (1.1.0) ✅

---

## 2. Stratégiai kontextus — T-Shape ecosystem

A SpaceOS T-Shape stratégia két dimenziója:

```
                    BREADTH (szélesség)
                    Manufacturer · Supplier · Dealer · Installer · Designer · Client
                         │
                         │
                    DEPTH (mélység)
                    Door module — Doorstar reference vertical
```

A **Cutting Planning** a **DEPTH dimenzió mélyítése**: a Doorstar MRR gate, "first paying customer" production flow.

A **FreeTier** és **PartnerTier** a **BREADTH dimenzió szélesítése**: új actor-típusok (Prospect, Szabászat Partner, End Customer) bekapcsolása a SpaceOS ökoszisztémába.

**Együttes hatás:** ugyanaz a motor (Nesting engine, Guillotine algoritmus, Graph Engine) **3 különböző piaci segmentben** monetizálódik:
1. **Manufacturer tenant** (Doorstar, Cabinet maker) — paid SaaS
2. **Anonymous prospect** (FreeTier) — lead gen + upgrade funnel
3. **Szabászat + End customer** (PartnerTier) — channel partner commission

---

## 3. MaxCut Benchmark — Competitive Positioning

A Gábor kedvenc szabászati alkalmazása, inspirációs forrás. Nézzük a tanulságokat.

### 3.1 MaxCut erősségek — amit megcélzunk vagy átveszünk

| MaxCut feature | Benchmark érték | SpaceOS válasz | Fázis |
|----------------|-----------------|-----------------|-------|
| Yield quality | ~91 alkatrész / 10 tábla (vs. versenytárs 11-12) | Guillotine-cut saját impl — **MaxCut szint vagy jobb** | v1 kötelező |
| Reusable component library | `PartSnippet` entity | Implementálni | v1 |
| Sticky label printing | Label strategy (4 variáns) | Implementálni | v1 |
| Supplier order list | Nesting → Procurement integráció | Integráció (Procurement modul kész) | v1.5 |
| Kerf width customizable | Per CuttingLine mező | Már van | v1 ✅ |
| Grain direction | Erezetirány flag + lock rotate | Implementálni | v1 |
| Offcut management | Offcut-first strategy | Tervezett | v1 |
| Accurate job costing | Anyagár × m² + munkaóra | Implementálni | v1.5 |
| Bulk job item changes | Workspace bulk editing UI | Portal layer | v1 |
| Excel/CAD integration | Import/export | Excel import v1, DXF export v1.5 | v1–v1.5 |

### 3.2 MaxCut gyengeségek — SpaceOS előny vektorai

| MaxCut korlát | SpaceOS differenciáció | Stratégiai érték |
|---------------|------------------------|------------------|
| Desktop only (Windows) | Web-first · platform-független · mobilfriendly | Moderns, Mac/Linux friendly |
| No API | API-first architektúra | Integráció, B2B2B csatorna |
| Dated interface | React 18 + Tailwind + L2 interaktív SVG | Modern brand image |
| No cloud/collaboration | Share token · workspace · team view | Team workflow |
| Community edition has part limits | Free tier: generáció/hó limit (nem alkatrész limit) | Valódi usability free tier-ben |
| No partner network | **PartnerTier B2B2C** | Unique value proposition |
| Pricing: per-user desktop license | SaaS subscription · per-tenant pricing | Modern üzleti modell |

### 3.3 SpaceOS Positioning Statement

> _"MaxCut minősége, ingyenesen a böngészőben,_
> _a szabászatod brandje alatt,_
> _amivel a kuncsaftjaid is boldogok."_

**Kritikus:** Ne kloonozzuk a MaxCut-ot feature-ról feature-ra. Építsünk olyan dolgokat **amit ő nem tud:**
1. **Cloud & collaboration** (workspace, share, team)
2. **Partner network** (B2B2C channel)
3. **White-label & embedding** (szabászat brand)
4. **API & integration** (ERP, e-commerce, CAD)

### 3.4 Nesting Quality Benchmark — Kötelező tesztelési gate

A v1 Cutting Planning **Definition of Done** része:

| Benchmark | Módszer | Pass/fail küszöb |
|-----------|---------|-------------------|
| Yield comparison vs. MaxCut | 5 referencia cutlist (10, 50, 100, 200, 500 darab) | SpaceOS ≥ 95% MaxCut yield |
| Offcut count | Ugyanaz az input | SpaceOS ≤ MaxCut + 5% |
| Compute time | 200 darabig | ≤ 5 másodperc |
| Visual output quality | Subjective review | ≥ MaxCut szint (Guillotine-cut pattern világos) |

Fail esetén: L2 algoritmus finomítása VAGY MaxRects korábbi bekapcsolása.

---

## 4. Cutting Planning — Final Scope (Q1–Q14 döntések)

### 4.1 Core architektúra döntések

| # | Kérdés | Döntés | v1 impl | v2+ placeholder |
|---|---|---|---|---|
| Q1 | Inventory integráció | Saját beépített + külső adapter slot · soft reservation | `IInventoryProvider.Reserve()` (Modules.Inventory) | OptiCut / SAP / WMS adapter |
| Q2 | Nesting algoritmus | Licencmentes · Guillotine v1 · marketing csali | `GuillotineNestingStrategy` | `MaxRectsNestingStrategy` (L3), `ExternalAdapterStrategy` |
| Q3 | FSM állapotok | Draft → Published → Frozen → Closed | Mind a 4 állapot élesben | — |
| Q4 | Priority algoritmus | 2 profile preset · Strategy pattern | `PriorityProfile` entity + Manufacturer/PanelCutter preset | Custom profile + UI hangolás |
| Q5 | Capacity model | 3 model interface · v1 AreaCapacity | `ICapacityModel` + `AreaCapacityModel` | `MachineHourCapacityModel`, `HybridCapacityModel` (placeholder class) |
| Q6 | Offcut küszöb | 400×400mm tenant-config default | Tenant config + default érték | — |
| Q7 | Rework policy | 3 policy interface · v1 WarnAndApply · per-CuttingPlan override | `IReworkPolicy` + `WarnAndApplyPolicy` | `AutoReNestPolicy`, `LockAfterPublishedPolicy` (placeholder) |
| Q8 | Plan horizon | N-day rolling (1..14 default cap, 1..30 config) | DaySlot value object per day | — |
| Q9 | Profile presets v1 | 2 db (Manufacturer + PanelCutter) | `ProfilePresets.cs` | Tenant custom preset library |
| Q10 | Rework scope | Per-CuttingPlan override · profile=default | `CuttingPlan.ReworkPolicyOverride` nullable | — |
| Q11 | Public nesting | **MOST v1.5-ben** FreeTier-ben | — | — |
| Q12 | DaySlot lock | Mindkettő: cron auto-lock + manuális operátor action | `LockDaySlotHandler` + cron worker | — |
| Q13 | Horizon max | 14 nap default, 1..30 tenant-config | Tenant config kulcs | — |
| Q14 | Profile change migráció | C) Draft-only retroaktív + ProfileSnapshotId immutable | Aggregate snapshot publikáláskor | — |

### 4.2 Meta-elvek (architektúra szintű)

| Elv | Kódba transzformálása |
|-----|----------------------|
| **"Nem most implementáljuk, de kelleni fog"** | Strategy pattern interface + v1 impl + placeholder class-ok |
| **Profile = viselkedés-kontrakt** | `PriorityProfile` entity kapcsolja össze a Strategy-ket + config-ot |
| **Immutable publikálás után** | `ProfileSnapshotId` · DaySlot `LocalState` · rework policy snapshot |
| **Granularitás rétegek** | Tenant → Profile → CuttingPlan override → DaySlot lokális állapot |
| **Contract/Implementation szétválasztás** | `ICuttingProvider` · `IInventoryProvider` · `IProcurementProvider` |

### 4.3 Modul struktúra (előzetes)

```
SpaceOS.Modules.Cutting/
├─ Domain/
│  ├─ Aggregates/
│  │  ├─ CuttingSheet.cs              (Core — DEPLOYED)
│  │  ├─ CuttingPlan.cs               ✨ ÚJ — N-day rolling, globalFSM
│  │  └─ PanelReservation.cs          ✨ ÚJ — soft lock
│  ├─ ValueObjects/
│  │  ├─ DaySlot.cs                   ✨ ÚJ — per-day content + localState
│  │  ├─ NestingResult.cs             (Core — DEPLOYED)
│  │  ├─ PanelAssignment.cs           ✨ ÚJ — versioned
│  │  ├─ CapacityBudget.cs            ✨ ÚJ
│  │  └─ PriorityScore.cs             ✨ ÚJ
│  ├─ Services/
│  │  ├─ Nesting/
│  │  │  ├─ INestingStrategy.cs       ✨ ÚJ
│  │  │  ├─ FfdhNestingStrategy.cs    (Core — DEPLOYED, L1)
│  │  │  ├─ GuillotineNestingStrategy.cs  ✨ ÚJ — L2, v1 default
│  │  │  └─ MaxRectsNestingStrategy.cs    ⏳ v2 placeholder
│  │  ├─ Capacity/
│  │  │  ├─ ICapacityModel.cs         ✨ ÚJ
│  │  │  ├─ AreaCapacityModel.cs      ✨ ÚJ — v1
│  │  │  ├─ MachineHourCapacityModel.cs   ⏳ v2 placeholder
│  │  │  └─ HybridCapacityModel.cs    ⏳ v2 placeholder
│  │  ├─ Rework/
│  │  │  ├─ IReworkPolicy.cs          ✨ ÚJ
│  │  │  ├─ WarnAndApplyPolicy.cs     ✨ ÚJ — v1 default
│  │  │  ├─ AutoReNestPolicy.cs       ⏳ v2 placeholder
│  │  │  └─ LockAfterPublishedPolicy.cs   ⏳ v2 placeholder
│  │  ├─ OffcutFirstStrategy.cs       ✨ ÚJ
│  │  └─ PriorityScoringService.cs    ✨ ÚJ — profile-aware
│  └─ Profiles/
│     ├─ PriorityProfile.cs           ✨ ÚJ — entity
│     └─ ProfilePresets.cs            ✨ ÚJ — Manufacturer + PanelCutter
├─ Application/
│  └─ Handlers/
│     ├─ CreateCuttingPlanHandler.cs  ✨ ÚJ
│     ├─ PublishPlanHandler.cs        ✨ ÚJ
│     ├─ FreezePlanHandler.cs         ✨ ÚJ
│     ├─ LockDaySlotHandler.cs        ✨ ÚJ
│     ├─ ReNestHandler.cs             ✨ ÚJ
│     └─ InvalidateOnOrderChange.cs   ✨ ÚJ (event handler)
```

### 4.4 Effort becslés

| Komponens | Nap |
|-----------|-----|
| **Session B: Cutting Planning Core** | |
| CuttingPlan aggregate + DaySlot value object + FSM (global + local) | 3 |
| `INestingStrategy` + Guillotine-cut saját impl | 3 |
| `IInventoryProvider.Reserve()` kliens + soft reservation | 1 |
| **Extension points** (PartnerId, SourceChannel, BrandingContext) | 2 |
| Migration + seed + teszt | 1 |
| **Subtotal Session B** | **~10 nap** |
| **Session C: Profile + Capacity + Rework** | |
| `PriorityProfile` entity + 2 preset + ProfileSnapshotId | 2 |
| `ICapacityModel` + AreaCapacityModel v1 + placeholder | 1 |
| `IReworkPolicy` + WarnAndApplyPolicy v1 + placeholder | 1 |
| `PriorityScoringService` — profile-aware | 1 |
| `OffcutFirstStrategy` + tenant config | 1 |
| DaySlot lock: cron worker + operator endpoint | 1 |
| Per-CuttingPlan rework override | 1 |
| Migration + seed + teszt | 1 |
| **Subtotal Session C** | **~9 nap** |

**Cutting Planning összesen: ~19 nap**

---

## 5. FreeTier Anonymous Workspace — Scope

### 5.1 Paradigmaváltás — nem "public endpoint", hanem "freemium product"

A döntések (permanent tárolás + save/share/return) egy **anonymous-tenant mini-SaaS**-t eredményeztek.

```
ANONYMOUS                       FREE TIER                          FULL TENANT
(public landing)                (email-auth'd workspace)           (paid SpaceOS)
       │                                 │                                │
       │  form kitöltve                  │  email verify                  │
       │  captcha OK                     │  magic link (30 day sliding)   │
       │  nesting számolt                │  saját dashboard               │
       │  eredmény látható 10 min        │  nesting history               │
       │  "ide mentse"? ── email ───────▶│  share token gen               │
       │                                 │  QR label config               │
       │                                 │  3D viz (v1.5+)                │
       │                                 │  PDF/DXF export                │
       │                                 │  upgrade CTA ─────────────────▶│
       │                                                                  │
       └── 10 min result only ────────────────────────────────────────────┘
           (ha nincs email capture)
```

### 5.2 Döntések (PQ1–PQ8 + FT1–FT8)

| # | Kérdés | Döntés |
|---|---|---|
| PQ1 | Input channel | **D) SEO-friendly landing + manuális form v1** · Excel v2 |
| PQ2 | Result visualization | **L2 interaktív SVG** · címke-strategy (4 variáns) · 3D v2 |
| PQ3 | Data retention | **C) Permanent** (email auth után) · 10 min anonymous before |
| PQ4 | Rate limit layers | **Mind az 5** (Nginx + Turnstile + RL + size + timeout) |
| PQ5 | Deployment | **B) Új projekt** `SpaceOS.FreeTier.Api` :5007 |
| PQ6 | Email capture | **Post-result full report** · tárol + share + return |
| PQ7 | PDF tartalom | 6 elem (SVG, cut list, yield, branding, CTA, metadata) |
| PQ8 | Landing URL | **D) `joinerytech.hu/eszkozok/szabaszat-optimalizalo` (HU) + `asztalostech.hu` mirror** |
| FT1 | Free tier auth | **Magic link + 30 nap sliding session** |
| FT2 | Keycloak viszony | **Hibrid**: FreeTier own auth, upgrade → Keycloak user creation |
| FT3 | Share model | **Read-only + export** (editable v2) |
| FT4 | Label strategy | **4 strategy** (None/Manual/Full/QR) · default `FullLabel` |
| FT5 | 3D viz | **v2-ben Three.js** (nem v1) |
| FT6 | DB topológia | **Shared PG + `freetier` schema** · migráció B)-re ha load nő |
| FT7 | Virality tags | **Mind a 4** (PDF footer, share landing, invite, DXF watermark) |
| FT8 | Paywall granularitás | Részletes pricing table FreeTier arch-planner session-ben |

### 5.3 Label Strategy pattern — részletek

A Q2 válasz (címkézés variabilitás) egy **LabelStrategy** Strategy pattern:

| Strategy | Output tartalma | Format | Default |
|----------|-----------------|--------|---------|
| `NoLabelStrategy` | Csak szín/pozíció · manuális referenciához | SVG minimál | — |
| `ManualMarkStrategy` | Alkatrész-ID kicsi · user kézzel ír | SVG + printable template | — |
| `FullLabelStrategy` | Név + méret + anyag | SVG per panel | **✅ Default** |
| `QrCodedLabelStrategy` | QR + alphanumeric ID | SVG + QR (QRCoder NuGet, MIT) | — |

### 5.4 Vendor stack

| Funkció | Vendor | Licensz | Havi költség (kisforgalom) |
|---------|--------|---------|---------------------------|
| Captcha | Cloudflare Turnstile | Free, cookieless | €0 |
| Email (magic link + marketing) | Brevo | Free tier 300 email/nap · €25/hó 20k-ra | €0–25 |
| Analytics | Plausible self-hosted | Free (OSS) | €0 |
| QR generálás | QRCoder NuGet | MIT | €0 |
| PDF | QuestPDF | MIT | €0 |
| 3D (v2) | Three.js | MIT | €0 |

**Total OPEX (v1 kisforgalom):** ~€0–25/hó

### 5.5 GDPR & Legal

**Permanent storage = DPA frissítés szükséges:**

| Item | Action |
|------|--------|
| Privacy Policy új szekció: "FreeTier Anonymous Workspace" | Jogi review ~2 óra |
| DPA template (ha enterprise prospect jön) | Jogi sablon ~3 óra |
| Magic link email — unsubscribe + double opt-in | Implementáció |
| End customer adat: név, email, szabászlista, postal code | Legitimate interest + consent |
| Right to deletion endpoint | Soft delete + 30 day grace |

### 5.6 Effort becslés — FreeTier

| Komponens | Nap |
|-----------|-----|
| `SpaceOS.FreeTier.Api` új projekt :5007 + systemd + nginx | 1 |
| `SpaceOS.Nesting.Algorithms` NuGet extraction | 1 |
| `POST /nest` anonymous + rate limit + Turnstile | 1 |
| FreeTier auth modul (magic link + session + expiry) | 2 |
| FreeTier user + workspace data model + migrations | 1.5 |
| Share token + read-only access + export pre-auth | 1 |
| L2 interaktív SVG visualization (zoom/pan/tooltip/highlight) | 2 |
| 4 LabelStrategy + QRCoder integráció | 1.5 |
| Post-result full report UX (save/share/return CTA) | 1 |
| QuestPDF branded PDF + cut list + SVG embed | 1 |
| `/eszkozok/szabaszat-optimalizalo` landing page (EN + HU) | 2 |
| Plausible analytics + Brevo marketing integration | 1 |
| Privacy policy + GDPR DPA update | 1 |
| Upgrade flow (FreeTier → Keycloak tenant migration) — **stubbed v1** | 1 |
| Tesztek (rate limit, auth, share flow, export) | 2 |
| **Subtotal** | **~19 nap** |

---

## 6. PartnerTier B2B2C Channel Network — Scope

### 6.1 Paradigmaváltás — channel partner modell

```
B2C FreeTier                        B2B2C PartnerTier
───────────────                     ─────────────────
Prospect → SpaceOS landing          End customer → Szabászat site
  │                                   │
  ▼                                   ▼
Calculator SpaceOS brand alatt      Calculator szabászat brand alatt
  │                                   │  (embed / iframe / widget)
  ▼                                   ▼
Email capture → upgrade             Eredmény → kuncsaft email
                                    Adat → szabászat dashboard
                                    SpaceOS brand footer
                                    Szabászat lead-ek gyarapodnak
                                    SpaceOS ismertség nő
```

### 6.2 Három persona

| Persona | Ki | Mit lát | Mit nyer |
|---------|------|---------|----------|
| **End customer** | Kuncsaft (bútor/ajtó/konyha) | Szabászat honlapján kalkulátor | Gyors árajánlat · PDF · nincs külön szoftver |
| **Szabászat (Partner)** | Kis-közepes szabászműhely | Saját dashboard: leads, conversions | Lead generation · mérhető inbound · ingyenes kalkulátor |
| **SpaceOS** | Mi | Partner network · aggregated insights | Brand awareness · upgrade funnel · piacbecslés |

### 6.3 Integration döntések

| # | Kérdés | Döntés |
|---|---|---|
| M1 | Dual-brand | **B) mindkét brand saját landing, shared backend, brand-szerinti PDF** |
| M2 | Timing | **Doorstar pilot STABIL után** (Q3–Q4 2026) |
| M6 | Integration | **A) iframe + C) subdomain** kombináció · B) widget kerülendő (XSS) · E) API v3 |
| M7 | Onboarding | **B) Application + manuális review** első 20 partner · D) tier-es v2.5 |
| M8 | Nesting quality | **MaxCut benchmark kötelező** — 10% yield különbség kritikus márkaérték |
| M9 | Commission | **10% 12 hónap** ha end customer SpaceOS tenant lesz — network effect hajtó |

### 6.4 Partner tier struktúra (v2.5 revenue layer)

| Tier | Havi díj | Max leads/hó | Branding | Co-branded PDF | Commission |
|------|----------|---------------|----------|----------------|------------|
| **Free Partner** | €0 | 50 lead | "Powered by SpaceOS" | Kötelező | 0% |
| **Pro Partner** | €29 | 500 lead | Custom logo + szín | SpaceOS mini-logo sarokban | 0% |
| **Enterprise Partner** | €99 | Unlimited | Teljes white-label | SpaceOS logo opcionális | 10% SpaceOS tenant upgrade esetén 12 hónapig |

### 6.5 Security hardening (prioritás!)

A Gábor prioritása: **"A biztonság a legfontosabb."**

| Réteg | Követelmény | Implementáció |
|-------|-------------|---------------|
| Partner authentication | `X-Partner-Api-Key` minden request-ben | Key rotation · revocation · audit log |
| CORS allowlist | Csak whitelisted partner origin | Per-partner origin list · wildcard tiltva |
| CSP `frame-ancestors` | Per partner dinamikus CSP | Dinamikus header generálás |
| Rate limit per partner | Tier-alapú (Free 5/min, Pro 50/min, Enterprise custom) | Redis-based counter |
| postMessage validation | Schema-validated iframe↔parent | Strict message protocol |
| End customer data isolation | RLS `partner_id = current_partner()` | PostgreSQL policy |
| Partner admin audit log | Ki mikor mit olvasott (lead data) | Immutable log |
| PII encryption at rest | End customer email/telefon | PostgreSQL pgcrypto |
| GDPR right to deletion | End customer törlés partner oldal felől | Soft delete + 30 day grace |
| Fraud detection | Gyanús pattern (many leads from one IP) | Rate + pattern analysis |
| Abuse reporting | End customer → SpaceOS | Report endpoint · partner suspension |
| **Penetration test** | Külső audit (hackerlab.hu vagy hasonló) | **v1 release előtt kötelező** |

**Extra effort security hardeningre:** **~5 fejlesztői nap**

### 6.6 Effort becslés — PartnerTier

| Komponens | Nap |
|-----------|-----|
| Partner data model (Partners, PartnerApiKeys, PartnerConfigs) | 2 |
| Partner authentication + API key middleware | 1.5 |
| CORS allowlist per partner + dinamikus CSP | 1 |
| iframe embed endpoint (`/embed/{partnerId}`) + sandbox | 2 |
| Subdomain support (`{partner}.spaceos.hu` wildcard DNS + cert) | 2 |
| postMessage iframe↔parent protocol | 1.5 |
| Partner dashboard UI (leads, conversions, analytics) | 3 |
| SpaceOS admin: Partner application review workflow | 2 |
| Commission tracking + upgrade attribution | 1.5 |
| Rate limit per partner (Redis) | 1 |
| Audit log + abuse reporting | 1 |
| DPA template + legal review | 1 |
| **Security hardening subtotal** | |
| RLS enforcement + PII encryption | 1.5 |
| Fraud detection + pattern analysis | 1.5 |
| Penetration test (external) | 2 (coordination) |
| **Subtotal** | **~20 nap** + 5 security hardening |

---

## 7. Extension Points v1-ben (CRITICAL!)

A "tudjuk hogy kelleni fog" elv **csak akkor működik**, ha v1 Cutting Planning tervdokumentum explicit tartalmazza ezeket. Különben v1.5 és v2 refactor igényel.

### 7.1 Data model extensions (v1 migration-ben!)

| Entity | Új mező | Típus | Default | Miért |
|--------|---------|-------|---------|-------|
| `CuttingPlan` | `PartnerId` | `Guid?` nullable | `NULL` | PartnerTier attribúció |
| `CuttingPlan` | `SourceChannel` | `enum` | `Direct` | Funnel analytics |
| `CuttingSheet` | `BrandingContextId` | `Guid?` nullable | `NULL` | Multi-brand rendering (Joinerytech/Asztalostech/Partner) |
| `PanelReservation` | `ReservationSource` | `enum` | `TenantPlan` | FreeTier vs Tenant reservation scope |
| Új tábla: `BrandingContexts` | Logo URL, primary color, custom footer, CSS vars | — | — | FreeTier + PartnerTier brand rendering |

**`SourceChannel` enum értékek (v1-ben deklarálva, de csak `Direct` használva):**

```csharp
public enum SourceChannel
{
    Direct = 0,      // Tenant user submits via Portal
    FreeTier = 1,    // Anonymous workspace (v1.5)
    Partner = 2,     // B2B2C embedded flow (v2)
    Api = 3          // Programmatic / ERP integration (v3)
}
```

### 7.2 Interface extensions (v1 kontrakt!)

**`ICuttingProvider` új metódus (opcionális implementáció v1-ben):**

```csharp
public interface ICuttingProvider
{
    // Meglévő
    Task<NestingResult> SubmitCuttingSheet(CuttingSheet sheet, CancellationToken ct);

    // v1 ÚJ — anonymous flow támogatása
    Task<NestingResult> SubmitAnonymousSheet(
        CuttingSheet sheet,
        SourceChannel source,
        Guid? partnerId,
        CancellationToken ct);
}
```

**Implementáció:** v1-ben `SubmitAnonymousSheet` egyszerűen hívja `SubmitCuttingSheet`-et `SourceChannel = Direct`-tel (lefelé kompatibilis). V1.5-ben a FreeTier használja saját implementációval.

### 7.3 NuGet package leválasztás (v1-ben!)

**`SpaceOS.Nesting.Algorithms` standalone NuGet package:**

Tartalma:
- `INestingStrategy` interface
- `FfdhNestingStrategy` (Core-ból kiemelve)
- `GuillotineNestingStrategy` (v1 új)
- `MaxRectsNestingStrategy` (placeholder)
- `NestingResult`, `PanelAssignment` DTO-k

Ez a package használható:
- Modules.Cutting-ben (v1 primary consumer)
- FreeTier.Api-ban (v1.5)
- PartnerTier embedded flow-ban (v2)
- Bármely jövőbeli consumer-ben

**Extra effort v1-ben:** ~1 nap a leválasztásra, NuGet pack config.

### 7.4 Tenant config extensions

Új config kulcsok (v1 seed-ben):

| Kulcs | Típus | Default | Ki használja |
|-------|-------|---------|--------------|
| `cutting.offcut.min_width_mm` | int | 400 | v1 Cutting Planning |
| `cutting.offcut.min_height_mm` | int | 400 | v1 Cutting Planning |
| `cutting.plan.horizon_max_days` | int | 14 | v1 Cutting Planning |
| `cutting.nesting.default_strategy` | string | `"Guillotine"` | v1 Cutting Planning |
| `cutting.capacity.model` | string | `"Area"` | v1 Cutting Planning |
| `branding.primary_color` | string | null | v1.5 FreeTier + v2 PartnerTier |
| `branding.logo_url` | string | null | v1.5 FreeTier + v2 PartnerTier |
| `source.channel` | string | `"Direct"` | v1.5 + v2 attribution |

**Extra effort v1-ben:** ~0.5 nap

---

## 8. Unified Timeline & Phasing

### 8.1 Ütemezés diagram

```
[Q2 2026 — NOW, 6-8 hét]
┌──────────────────────────────────────────────────────────────────┐
│ ARCH-PLANNER SESSION A:                                           │
│   SpaceOS_Modules_Contracts_Architecture_v4_1.md                  │
│   (IInventoryProvider.Reserve · ReleaseReservation · GetReservations) │
│   ~3 nap design · ~5 nap impl                                     │
│                          │                                        │
│                          ▼                                        │
│ ARCH-PLANNER SESSION B:                                           │
│   SpaceOS_Modules_Cutting_Planning_Architecture_v4.md             │
│   (CuttingPlan aggregate + FSM + Nesting Strategy + Ext Points)   │
│   ~2 nap design · ~10 nap impl                                    │
│                          │                                        │
│                          ▼                                        │
│ ARCH-PLANNER SESSION C:                                           │
│   SpaceOS_Modules_Cutting_Planning_Profile_Architecture_v4.md     │
│   (PriorityProfile + Capacity + Rework + DaySlot Lock)            │
│   ~1 nap design · ~9 nap impl                                     │
│                                                                   │
│   ★ Doorstar Soft Launch — reference story ★                      │
└──────────────────────────────────────────────────────────────────┘

[Q3 2026 — Doorstar pilot stabil után, 4-5 hét]
┌──────────────────────────────────────────────────────────────────┐
│ ARCH-PLANNER SESSION D:                                           │
│   SpaceOS_FreeTier_Architecture_v4.md                             │
│   (Magic link auth · workspace · share · L2 viz · PDF · labels)   │
│   ~2 nap design · ~19 nap impl                                    │
│                                                                   │
│   ★ FreeTier LIVE — marketing asset ★                             │
└──────────────────────────────────────────────────────────────────┘

[Q4 2026 — FreeTier validált után, 5-6 hét]
┌──────────────────────────────────────────────────────────────────┐
│ ARCH-PLANNER SESSION E:                                           │
│   SpaceOS_PartnerTier_Architecture_v4.md                          │
│   (iframe embed + subdomain + dashboard + 5 pilot partner)        │
│   ~2 nap design · ~20 nap impl                                    │
│   + 5 nap security hardening (pen-test)                           │
│                                                                   │
│   ★ PartnerTier LIVE — channel network ★                          │
└──────────────────────────────────────────────────────────────────┘
```

### 8.2 Total effort summary

| Fázis | Design napok | Impl napok | Total napok | Hét |
|-------|--------------|------------|-------------|-----|
| Session A — Contracts 1.2.0 | 3 | 5 | 8 | ~2 |
| Session B — Cutting Planning Core + Extension Points | 2 | 12 | 14 | ~3 |
| Session C — Profile + Capacity + Rework | 1 | 9 | 10 | ~2 |
| Session D — FreeTier Anonymous Workspace | 2 | 19 | 21 | ~4-5 |
| Session E — PartnerTier MVP + Security | 2 | 25 | 27 | ~5-6 |
| **Total** | **10** | **70** | **80 nap** | **~16-18 hét** |

**Megjegyzés:** Nem mind SpaceOS Kernel work — a Session D és E jelentős része külön service-ben (`FreeTier.Api`, `PartnerTier.Api`).

---

## 9. Risk Register

### 9.1 Scope / Complexity risks

| Risk | Súly | Mitigation |
|------|------|-----------|
| **Scope eszkalálás** (3 paradigmaváltás egy sprint-ben) | Magas | 3-fázisos ütemezés · Doorstar-first · FreeTier csak pilot után |
| **Extension point teherbíráskapacitás** (v1-ben +2-3 nap, v2-ben 0 refactor) | Közepes | Explicit dokumentálva v1 tervdokban · DoD ellenőrzi |
| **FreeTier scope gyorsan "second product"-tá nő** | Magas | Feature freeze v1.5-re · v2 backlog szigorúan |
| **PartnerTier go-to-market complexity** (legal, commission, tiers) | Magas | Manual review első 20 partner · tier-es system v2.5-ben |

### 9.2 Technical risks

| Risk | Súly | Mitigation |
|------|------|-----------|
| **Contracts 1.2.0 bump bottleneck** (blokkolja Session B-t) | Magas | Session A első · kritikus útvonal |
| **Nesting quality < MaxCut** | Magas | Benchmark kötelező v1 DoD-ban · L3 MaxRects placeholder |
| **Public endpoint compute DDoS** | Magas | 5-layer rate limit (Nginx, Turnstile, RL, size, timeout) |
| **FreeTier load shared PG-n** | Közepes | Metric alert · migráció plan B-re ha threshold túllépve |
| **Magic link email reputation** | Közepes | DKIM/SPF/DMARC · dedikált sender (free@) |
| **PartnerTier iframe XSS / postMessage attack** | Magas | Schema-validated protocol · strict CSP · pen-test |
| **Guillotine output != OptiCut output reputation damage** | Közepes | Transzparens disclaimer · benchmark dokumentálva |

### 9.3 Business risks

| Risk | Súly | Mitigation |
|------|------|-----------|
| **Doorstar pilot nem stabil Q3-ra → FreeTier timing csúszik** | Magas | Timing flexibilitás · pilot success criteria előre definiálva |
| **FreeTier conversion < 5%** (lead → paying tenant) | Magas | A/B testing · funnel optimization · CTA iteration |
| **Szabászat partner network nem épül fel** (<5 partner Q4-re) | Közepes | Manual outreach · reference story · free tier rewards |
| **GDPR compliance audit finding** | Magas | Legal review minden fázis előtt · DPA template · Privacy Policy update |
| **MaxCut vs SpaceOS feature war** | Alacsony | Platform vs desktop pozicionálás · ne kloonozzunk · unique features (cloud, collaboration, partner) |

### 9.4 Dependencies / blockers

| Risk | Súly | Mitigation |
|------|------|-----------|
| Modules.Inventory Reservation API kompatibilis | Magas | Session A első · Inventory team sync |
| Keycloak production load (FreeTier upgrade flow) | Közepes | Keycloak Hibrid model (FreeTier own auth + upgrade trigger) |
| QRCoder / Three.js / QuestPDF licensz audit | Alacsony | MIT mindegyik ✅ · dokumentált Contracts-ban |
| DACH expansion legal requirements | Közepes | CAC validation előbb · DE/AT jog audit később |

---

## 10. Decision Log — Master Reference

A mai session során hozott döntések egyetlen táblázatban, Claude Code és jövőbeli arch-planner sessionek számára.

| Téma | # | Kérdés | Döntés |
|------|---|---------|--------|
| **Cutting Planning** | Q1 | Inventory integráció | Saját + külső adapter · soft reservation |
| | Q2 | Nesting algo | Licencmentes · Guillotine v1 · marketing csali |
| | Q3 | FSM | Draft → Published → Frozen → Closed |
| | Q4 | Priority | 2 profile preset · Strategy pattern |
| | Q5 | Capacity | 3 model interface · v1 Area |
| | Q6 | Offcut küszöb | 400×400mm tenant-config default |
| | Q7 | Rework policy | 3 interface · v1 WarnAndApply · per-CuttingPlan override |
| | Q8 | Horizon | N-day rolling (1..14 default, 1..30 config) |
| | Q9 | Profile presets | 2 (Manufacturer + PanelCutter) |
| | Q10 | Rework scope | Per-CuttingPlan override · profile=default |
| | Q11 | Public nesting | v1.5-ben FreeTier-ben (nem Cutting Planning-ban) |
| | Q12 | DaySlot lock | Cron auto + manuális operator |
| | Q13 | Horizon max | 14 nap default |
| | Q14 | Profile change | Draft-only retroaktív + ProfileSnapshotId immutable |
| **FreeTier** | PQ1 | Input | D) SEO landing + manuális form v1 |
| | PQ2 | Viz | L2 interaktív SVG · 4 label strategy · 3D v2 |
| | PQ3 | Retention | Permanent (email auth után) · 10 min anonymous |
| | PQ4 | Rate limit | Mind az 5 layer |
| | PQ5 | Deployment | Új projekt `SpaceOS.FreeTier.Api` :5007 |
| | PQ6 | Email capture | Post-result full report · save/share/return |
| | PQ7 | PDF | 6 elem (SVG, cut list, yield, branding, CTA, meta) |
| | PQ8 | Landing URL | `/eszkozok/szabaszat-optimalizalo` HU + mirror EN |
| | FT1 | Auth | Magic link + 30 nap sliding |
| | FT2 | Keycloak | Hibrid: own auth, upgrade → Keycloak |
| | FT3 | Share | Read-only + export · editable v2 |
| | FT4 | Labels | 4 strategy · default FullLabel |
| | FT5 | 3D | v2-ben Three.js |
| | FT6 | DB | Shared PG `freetier` schema |
| | FT7 | Virality | Mind a 4 tag |
| | FT8 | Paywall | Részletes pricing arch-planner Session D-ben |
| **PartnerTier** | M1 | Dual-brand | Mindkét brand saját landing · shared backend |
| | M2 | Timing | Doorstar stable után (Q3 FreeTier · Q4 Partner) |
| | M3 | 3-fázisos ütemezés | IGEN |
| | M4 | v1 extension points | IGEN (+2-3 nap) |
| | M5 | FreeTier timing | Doorstar stabil után |
| | M6 | Integration | iframe + subdomain kombináció |
| | M7 | Onboarding | Application + review első 20 · tier v2.5 |
| | M8 | Nesting quality | MaxCut benchmark kötelező |
| | M9 | Commission | 10% 12 hónap · network effect hajtó |
| | M10 | Next step | Growth Strategy doc generálás + Session A arch-planner |

---

## 11. Kimaradt döntések — jövőbeli arch-planner sessionek során eldöntendő

A jelen dokumentum **nem** tartalmazza:

| Téma | Hol kerül eldöntésre |
|------|---------------------|
| Pricing table (Free/Pro/Enterprise Partner) részletes | Session E (PartnerTier) |
| FreeTier paywall granularitás (mely feature-ök Free) | Session D (FreeTier) |
| MaxCut benchmark konkrét tesztesetek | Session B (Cutting Planning) DoD |
| Partner dashboard UI design | Session E (PartnerTier) + Portal design session |
| End customer email template-ek | Session D + E (kommunikációs asset) |
| Privacy Policy új szekciók szövege | Jogi review Session D előtt |
| DPA template konkrét klauzulák | Jogi review Session E előtt |
| Subdomain wildcard cert setup | Session E infra design |
| Redis deployment (rate limit backing store) | Infra upgrade Session A körül |
| DACH expansion nyelvi localizáció | Separate i18n arch session |
| Fraud detection algoritmus részletek | Session E security hardening |

---

## 12. Action Items — Következő session

**Azonnali:**

1. ✅ Growth Strategy v1 dokumentum mentve a projektbe
2. ⏳ Aktivált skill: `/spaceos-arch-planner` Contracts 1.2.0 tervdokumentumra
3. ⏳ Session A kimenete: `SpaceOS_Modules_Contracts_Architecture_v4_1.md`

**Session A scope (előzetes):**

- `IInventoryProvider.Reserve(correlationId, items, ttl)` — új metódus
- `IInventoryProvider.ReleaseReservation(correlationId)` — cleanup API
- `IInventoryProvider.GetReservations(filter)` — query API
- Modules.Inventory implementáció: új `Reservation` aggregate + TTL worker
- Contracts NuGet package version bump: 1.1.0 → 1.2.0
- DoD: Cutting modul sikeres `Reserve()` hívás · Inventory TTL worker unit teszt · E2E happy path

**Session A prerequisite check:**

| Prereq | Státusz |
|--------|---------|
| Modules.Inventory Core DEPLOYED | ✅ (2026-04-16) |
| Modules.Contracts 1.1.0 DEPLOYED | ✅ (2026-04-16) |
| Modules.Cutting Core DEPLOYED | ✅ (2026-04-16) |
| ICuttingProvider consumer (Joinery) DEPLOYED | ✅ (2026-04-16) |

**Mind DEPLOYED → Session A indítható azonnal.**

---

## 13. Záró elvek — "Ki ezt olvassa"

Ez a dokumentum **4 különböző olvasóközönségnek** szól:

| Olvasó | Mit keres itt | Relevantáns szekció |
|--------|---------------|---------------------|
| **Gábor (Architect)** | Stratégiai áttekintés, döntések archívum | Section 0, 8, 10 |
| **Jövőbeli Claude (arch-planner)** | Scope, döntések, extension points | Section 4, 5, 6, 7, 12 |
| **Jövőbeli Claude Code** | Implementációs kontextus | Section 7 (extension points), 10 (decision log) |
| **Non-technical stakeholder (pl. befektető, tanácsadó)** | Business context, timeline | Section 0, 2, 3, 8 |

**Egyetlen source of truth** a growth stratégiára vonatkozóan — ha bármely döntés frissül, **ez a dokumentum frissítendő először**, aztán a downstream arch-planner tervdokok.

---

## 14. Sign-off

| Role | Name | Status |
|------|------|--------|
| Architect & Founder | Gábor | ✅ Elfogadva 2026-04-17 |
| Design Pipeline | Claude (ai.claude.com) | ✅ Generált 2026-04-17 |
| Implementation Team | Claude Code | ⏳ Session A elindításra vár |

**Next action:** `/spaceos-arch-planner` Session A — Contracts 1.2.0.

---

*SpaceOS — Growth Strategy v1.0 · 2026. április 17. · Architect session*
*3 paradigmaváltás rögzítve · 4 jövőbeli arch-planner session alapja*
*Ez a dokumentum Claude Code, Claude arch-planner és Gábor számára egyaránt forrásdokumentum.*
