# BLOCKED Message Structure Pattern

**Created:** 2026-06-22 (based on Explorer research + 2 BLOCKED message analysis)

---

## Pattern Overview

**BLOCKED Message** = Terminál nem tud folytatni mert külső függőség vagy döntés hiányzik.

### Two Main Types

| Type | Trigger | Owner | Resolution Time |
|---|---|---|---|
| **Type A: Backend API hiánya** | Frontend ready, backend not | Backend terminál | 2-3 nap |
| **Type B: Architekturális döntés** | Domain gap, unclear design | Architect / Root | 1-2 nap spec + 2-3 nap impl |

---

## Type A: Backend API Hiánya Blokkolja Frontend-et

### Use Case

Frontend terminál megkapja a feature specet, elkezdi a kutatást, de rájön hogy:
- Nincs backend API endpoint
- Nincs domain aggregate/entity
- Nincs DTO/contract definíció

### Example: Bérmunka Partner-oldali Elfogadás (MSG-FRONTEND-003)

```yaml
---
id: MSG-FRONTEND-003-BLOCKED
from: frontend
to: conductor
type: blocked
priority: medium
status: UNREAD
ref: MSG-FRONTEND-002
created: 2026-06-21
---

# BLOCKED: Bérmunka partner-oldali elfogadás — Backend API hiányzik

## Kutatási eredmények

### Backend API audit ❌

Kerestem a következő endpointokat:
```bash
# Procurement domain
grep -r "subcontracts" backend/spaceos-modules-procurement/
grep -r "SubcontractOrder" backend/spaceos-modules-procurement/

# Kernel-wide search
grep -r "/api/procurement/subcontracts" backend/
```

**Eredmény:** Nincs találat

### Domain aggregate audit ❌

Kerestem a következő entitásokat:
- `SubcontractOrder` (aggregate root)
- `SubcontractStatus` (FSM enum)
- `SubcontractLine` (value object)

**Eredmény:** Nincs implementáció

### Mock adat audit 🟡

```typescript
// src/mocks/controlling.ts
export const mockSubcontracts = [
  { id: 'sc-001', name: 'Élzárás bérmunka', status: 'pending' },
  { id: 'sc-002', name: 'Fényes festés', status: 'pending' }
]
```

**Eredmény:** Frontend mock adat létezik, de backend API hiányzik

---

## Blocker részletei

### Backend hiányzik

**Domain:**
- `SubcontractOrder` aggregate
  - Properties: OrderId, SupplierId, Lines, Status, TotalPrice
  - FSM: Draft → Pending → Accepted / Rejected
  - Events: SubcontractCreated, SubcontractAccepted, SubcontractRejected

**Application:**
- Commands: CreateSubcontractOrder, AcceptSubcontract, RejectSubcontract
- Queries: GetSubcontractsBySupplier, GetSubcontractById

**API:**
- `GET /api/procurement/suppliers/{supplierId}/subcontracts`
- `POST /api/procurement/suppliers/{supplierId}/subcontracts/{id}/accept`
- `POST /api/procurement/suppliers/{supplierId}/subcontracts/{id}/reject`

**DTO:**
```csharp
public record SubcontractOrderDto(
    Guid Id,
    Guid OrderId,
    Guid SupplierId,
    List<SubcontractLineDto> Lines,
    string Status,  // "pending" | "accepted" | "rejected"
    decimal TotalPrice
);
```

---

## Frontend előfeltételek

**Komponensek (implementálandók):**
1. `SubcontractAcceptancePanel.tsx` — Partner-oldali elfogadás/elutasítás UI
2. `SubcontractDetailsModal.tsx` — Tétel lista, összeg
3. `useSubcontracts.ts` — API integration hook

**API integráció:**
- Replace mock: `src/mocks/controlling.ts` → real API
- Add endpoints to `src/services/procurementService.ts`

---

## Következő lépések

### Backend Inbox Template (javasolt)

```yaml
---
id: MSG-BACKEND-XXX
from: conductor
to: backend
type: task
priority: high
model: sonnet
ref: MSG-FRONTEND-003-BLOCKED
created: YYYY-MM-DD
---

# Bérmunka Backend API — Subcontract Acceptance

## Feladat

Implementáld a bérmunka partner-oldali elfogadás/elutasítás backend API-ját.

## Domain

**Aggregate:** `SubcontractOrder`
- FSM: Draft → Pending → Accepted / Rejected
- Commands: Create, Accept, Reject

## API Endpoints

- `GET /api/procurement/suppliers/{supplierId}/subcontracts`
- `POST /api/procurement/suppliers/{supplierId}/subcontracts/{id}/accept`
- `POST /api/procurement/suppliers/{supplierId}/subcontracts/{id}/reject`

## DoD

- ✅ Domain aggregate + FSM implementálva
- ✅ Application layer (commands + queries)
- ✅ API controllers (3 endpoints)
- ✅ Unit tests (domain + app layer ≥90%)
- ✅ Integration tests (API endpoints ≥40%)
- ✅ Build 0 error, tests pass

## Kapcsolódó

Frontend task: MSG-FRONTEND-002 (BLOCKED erre a backend-re vár)
```

---

## Javaslat

**Conductor:** Backend terminálnak inbox küldés prioritással (high)

**Becsült feloldási idő:** 2-3 nap backend fejlesztés

**Frontend unblock:** Amint a backend MSG-XXX DONE státuszba kerül, Frontend MSG-002 folytatható

---

## Üzleti impakt

- **Doorstar Soft Launch:** Alacsony (bérmunka nem kritikus Q2-ben)
- **Jövőbeli ügyfelek (beszállítói integráció):** Magas
- **Prioritás:** Medium (backlog, nem blokkol Soft Launch-ot)
```

---

## Type A Message Structure (Checklist)

```markdown
# BLOCKED: [Feature Name] — Backend API hiányzik

## 1. Kutatási eredmények ✅
- Backend API audit (grep results)
- Domain aggregate audit
- Mock adat audit (what exists in frontend)

## 2. Blocker részletei ✅
- **Domain hiányzik:** Aggregate, FSM, Events
- **Application hiányzik:** Commands, Queries
- **API hiányzik:** Endpoints, DTO
- **Példa DTO:** TypeScript or C# snippet

## 3. Frontend előfeltételek ✅
- **Komponensek:** List of UI components to be built
- **API integráció:** Which files need to be modified

## 4. Következő lépések ✅
- **Backend Inbox Template:** Full YAML draft for conductor
- **DoD:** Clear acceptance criteria

## 5. Javaslat ✅
- **Akció:** Conductor sends inbox to backend
- **Becsült idő:** 2-3 nap
- **Unblock path:** When backend DONE, frontend continues
```

---

## Type B: Architekturális Döntés Szükséges

### Use Case

Backend/Frontend terminál megkapja a specet, elkezdi a kutatást, de rájön hogy:
- Domain hiányzik (nem csak API, hanem az egész domain terület)
- Több architekturális döntési pont van
- Nem egyértelmű hogy melyik modulba tartozik

### Example: Beszállítói Reklamáció-válasz (MSG-BACKEND-002)

```yaml
---
id: MSG-BACKEND-002-BLOCKED
from: backend
to: conductor
type: blocked
priority: low
status: UNREAD
ref: MSG-BACKEND-001
created: 2026-06-21
---

# BLOCKED: Beszállítói reklamáció-válasz — Architekturális döntés szükséges

## Kutatási eredmények

### Domain audit

**Keresett entitások:**
- `Complaint` / `Reklamáció` — ❌ Nem található
- `QualityIssue` / `Selejt` — ❌ Nem található
- `SupplierResponse` — ❌ Nem található

**Meglévő domain területek:**
- `Procurement` (Purchase Order, Supplier) ✅
- `Inventory` (Stock, Reservation) ✅
- `Joinery` (Order, Batch) ✅
- `Quality` — ❌ NEM LÉTEZIK (!)

**Következtetés:** Nincs QA/Quality modul, nincs selejt/reklamáció domain terület.

---

## Blokkoló kérdések

Az alábbi architekturális döntések szükségesek implementáció előtt:

### Q1: Melyik modulba kerüljön?

**Opciók:**
- **A) Procurement modul** — Beszállítói kapcsolat része
  - Pro: Supplier entity már létezik
  - Con: Quality concern keveredik procurement-tel
- **B) Quality modul (ÚJ)** — Saját domain terület
  - Pro: Clean separation of concerns
  - Con: Új modul overhead (3-4 nap setup)
- **C) Kernel modul** — Shared concern
  - Pro: Minden modul használhatja
  - Con: Kernel túl generikus (anti-pattern)

**Ajánlás:** Option B (Quality modul) — tiszta domain határ

### Q2: Mi a QA selejt entitás/event?

**Kérdések:**
- Új `QualityDefect` aggregate?
- Event sourcing pattern? (ImmutableQualityEvent)
- FSM: Detected → Reported → Acknowledged → Resolved?
- Photo upload support? (S3 presigned URL, mint EHS modul)

**Ajánlás:** Architect spec kell

### Q3: Beszállítói authentication stratégia?

**Kérdések:**
- Supplier portal külön tenant?
- Supplier user role in Keycloak?
- API key alapú auth? (simplebb mint OAuth)
- RLS policy beszállítói adatokon?

**Ajánlás:** Identity modul bővítés + Architect spec

### Q4: Complaint FSM tervezés?

**Kérdések:**
- States: Draft → Submitted → Supplier_Responded → Accepted/Rejected?
- Transitions: ki mit csinálhat? (Admin, Supplier, QA Manager)
- Email notification? (Supplier értesítés)
- SLA tracking? (pl. 48h válaszidő)

**Ajánlás:** Architect spec kell

---

## Javasolt következő lépések

### Option A: Architect bevonás (ajánlott)

**Conductor:** Architect terminálnak inbox küldés a 4 kérdéssel

**Architect task:**
1. Domain design: Quality modul boundaries
2. FSM design: Complaint lifecycle
3. Authentication stratégia: Supplier portal access
4. API contract: Complaint endpoints + DTO

**Becsült idő:** 1-2 nap Architect spec

**Utána:** Backend implementáció 2-3 nap (spec alapján)

### Option B: MVP megközelítés (gyors, de technical debt)

**Conductor:** Backend terminál kap egyszerűsített spec-et

**MVP scope:**
- Procurement modul-ba kerül (nincs új modul)
- Complaint egyszerű entitás (nem event sourcing)
- Nincs supplier portal auth (csak admin UI)
- Nincs FSM (csak simple status string)

**Becsült idő:** 1 nap backend implementáció

**Utána:** Refactoring szükséges (2-3 nap later amikor Quality modul készül)

---

## Ajánlás

**Prioritás:** Low (Doorstar Soft Launch nem függ tőle)

**Recommended path:** Option A (Architect bevonás)

**Blocker resolution:** Architect spec DONE → Backend implementation DONE

---

## Üzleti impakt

- **Doorstar Soft Launch:** Alacsony (QA flow nem MVP requirement)
- **Supply chain minőség-kezelés:** Magas (későbbi ügyfeleknek kritikus)
- **Technical debt risk:** Magas ha MVP megközelítés (Option B)
```

---

## Type B Message Structure (Checklist)

```markdown
# BLOCKED: [Feature Name] — Architekturális döntés szükséges

## 1. Kutatási eredmények ✅
- Domain audit (what exists, what's missing)
- Module audit (which modules cover related areas)
- **Következtetés:** Gap analysis summary

## 2. Blokkoló kérdések ✅
### Q1: [Question 1]
**Opciók:** A, B, C (with Pros/Cons)
**Ajánlás:** [Recommended option]

### Q2: [Question 2]
**Kérdések:** Specific sub-questions
**Ajánlás:** [Guidance or "Architect spec kell"]

## 3. Javasolt következő lépések ✅
### Option A: Architect bevonás (ajánlott)
- **Task:** [What Architect needs to design]
- **Becsült idő:** X nap spec + Y nap impl

### Option B: MVP megközelítés
- **Scope:** [Simplified requirements]
- **Becsült idő:** X nap impl
- **Risk:** Technical debt, refactoring later

## 4. Ajánlás ✅
- **Prioritás:** High/Medium/Low
- **Recommended path:** Option A/B
- **Blocker resolution:** [What DONE unblocks this]

## 5. Üzleti impakt ✅
- **Doorstar Soft Launch:** High/Medium/Low
- **[Other stakeholder]:** Impact description
```

---

## Decision Matrix: Type A vs Type B

```
Backend API hiányzik?
  ├─── Domain EXISTS, csak API/endpoint hiányzik
  │     → Type A (Backend API hiánya)
  │     → Conductor: Backend inbox (2-3 nap)
  │
  └─── Domain NEM LÉTEZIK, architekturális döntések kellenek
        → Type B (Architekturális döntés)
        → Conductor: Architect inbox (1-2 nap spec + 2-3 nap impl)
```

---

## Statistics (2026-06-21 snapshot)

**Active BLOCKED messages:** 2

| Terminál | Type | Üzleti impakt (Soft Launch) | Prioritás |
|---|---|---|---|
| Frontend | Type A (Backend API hiánya) | Low | Medium |
| Backend | Type B (Architekturális döntés) | Low | Low |

**Pattern:** 0 kritikus blocker (Doorstar Soft Launch szempontjából) — Mindkettő backlog feature.

---

## Best Practices

### 1. Thorough Research Before BLOCKED

**DO:**
- Grep entire codebase (not just expected module)
- Check related domains (Procurement → Quality → Inventory)
- Document negative results ("Nincs találat")

**DON'T:**
- Send BLOCKED after 5 minutes of searching
- Assume domain doesn't exist without evidence

### 2. Provide Actionable Next Steps

**DO:**
- Draft inbox template for backend/architect
- Include specific DoD requirements
- Estimate resolution time

**DON'T:**
- Leave blocker vague ("something is missing")
- No suggestion for who should resolve it

### 3. Business Impact Assessment

**DO:**
- State impact on Doorstar Soft Launch (critical milestone)
- State impact on future customers/features
- Suggest priority (High/Medium/Low)

**DON'T:**
- Mark everything as "High" priority
- Ignore business context

---

## References

- Type A example: `terminals/frontend/outbox/2026-06-21_003_fe-subcontracting-acceptance-blocked.md`
- Type B example: `terminals/backend/outbox/2026-06-21_002_be-supplier-complaint-blocked.md`
- Explorer research: `terminals/explorer/outbox/2026-06-22_002_deep-dive-patterns-research-done.md`
- Active BLOCKED count: 2 (both low priority for Soft Launch)
