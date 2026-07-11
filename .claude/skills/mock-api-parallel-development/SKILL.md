# Mock API Parallel Development

## Purpose

Enable **Frontend development without Backend dependency** by using Mock Service Worker (MSW) to intercept API requests and return realistic mock responses. This allows Frontend to start work immediately after OpenAPI spec is locked (Week 0), without waiting for Backend API implementation.

**ROI:** Zero Frontend blocker = 2-4 weeks earlier UI delivery

---

## When to Use

Trigger this skill when:
- Backend API implementation delayed (Week 2+)
- Frontend blocked waiting for endpoints
- Contract-first development (OpenAPI spec ready)
- Parallel Backend + Frontend work required

**DO NOT use** for:
- Backend already done (no blocking)
- Prototype work (mock not needed)
- Integration testing (use real API)

---

## Prerequisites

**Tools:**
- MSW (Mock Service Worker): `npm install --save-dev msw`
- OpenAPI spec (locked Week 0)
- Feature flag library (optional but recommended)

**Inputs:**
- OpenAPI spec file (`API_SPEC_PHASE1.yaml`)
- Example responses (200, 400, 401, 422, 500)

---

## Step-by-Step

### Step 1: Install MSW (5 min)

```bash
npm install --save-dev msw
npx msw init public/ --save
```

### Step 2: Create Mock Handlers (20-30 min)

```typescript
// src/mocks/handlers.ts

import { http, HttpResponse } from 'msw';

export const handlers = [
  // Auth endpoints
  http.post('/v1/auth/login', async ({ request }) => {
    const body = await request.json();

    if (!body.email || !body.password) {
      return HttpResponse.json(
        { message: 'Validation failed', code: 'VALIDATION_ERROR' },
        { status: 422 }
      );
    }

    return HttpResponse.json({
      accessToken: 'mock-jwt-token-12345',
      expiresIn: 3600,
      user: {
        id: '550e8400-e29b-41d4-a716-446655440000',
        email: body.email,
        name: 'Mock User',
        role: 'Manager',
      },
    });
  }),

  // Catalog endpoints
  http.get('/v1/catalog', ({ request }) => {
    const url = new URL(request.url);
    const page = parseInt(url.searchParams.get('page') || '1');
    const limit = parseInt(url.searchParams.get('limit') || '10');

    return HttpResponse.json({
      items: Array.from({ length: limit }, (_, i) => ({
        id: `item-${page}-${i}`,
        name: `Product ${(page - 1) * limit + i + 1}`,
        price: Math.floor(Math.random() * 5000) + 1000,
        stock: Math.floor(Math.random() * 100),
      })),
      total: 87,
      page,
      limit,
    });
  }),

  http.get('/v1/catalog/:id', ({ params }) => {
    return HttpResponse.json({
      id: params.id,
      name: `Product ${params.id}`,
      price: 1200,
      description: 'Mock product description',
      stock: 42,
    });
  }),
];
```

### Step 3: Setup MSW Browser Worker (10 min)

```typescript
// src/mocks/browser.ts

import { setupWorker } from 'msw/browser';
import { handlers } from './handlers';

export const worker = setupWorker(...handlers);
```

```typescript
// src/main.tsx

if (import.meta.env.DEV || import.meta.env.VITE_USE_MOCK_API === 'true') {
  const { worker } = await import('./mocks/browser');
  await worker.start();
}

import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
```

### Step 4: Feature Flag for Real API Swap (15 min)

```bash
# .env.development (mock API)
VITE_USE_MOCK_API=true
VITE_API_URL=http://localhost:5000/v1

# .env.production (real API)
VITE_USE_MOCK_API=false
VITE_API_URL=https://api.joinerytech.hu/v1
```

```typescript
// src/api/custom-instance.ts

import axios from 'axios';

const baseURL = import.meta.env.VITE_USE_MOCK_API === 'true'
  ? '' // MSW intercepts all requests
  : import.meta.env.VITE_API_URL;

export const customInstance = axios.create({
  baseURL,
  withCredentials: true,
});
```

### Step 5: Test Mock API (10 min)

```bash
npm run dev
```

Open browser console:
```
[MSW] Mocking enabled.
[MSW] POST /v1/auth/login → 200 (mock)
```

Test login flow → should work with mock data

---

## Error Handling

**Common issues:**

1. **MSW not intercepting requests:**
```
Network tab shows real API calls (404)
```

**Fix:** Check `msw init public/` ran successfully, verify `mockServiceWorker.js` exists

2. **CORS errors with mock API:**
```
Access-Control-Allow-Origin error
```

**Fix:** MSW handles CORS automatically; if error persists, check `baseURL` in axios config

3. **Feature flag not working:**
```
Mock API used in production
```

**Fix:** Verify `.env.production` has `VITE_USE_MOCK_API=false`

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Frontend Unblocked** | Week 1 | UI development starts after spec lock |
| **Mock API Coverage** | 100% endpoints | All OpenAPI endpoints mocked |
| **Integration Swap Time** | <1 hour | Feature flag swap (mock → real) |
| **Mock Data Realism** | ≥80% fields | Realistic examples from spec |

---

## Real-World Example

### EPIC-JT-CRM Frontend (Week 1-4 parallel with Backend)

**Week 0:** OpenAPI spec locked (14 endpoints)
**Week 1:** MSW setup (2 hours), 14 mock handlers created (4 hours)
**Week 2-4:** Frontend UI development (Login, Lead list, Opportunity pipeline)
**Week 4:** Feature flag swap (`VITE_USE_MOCK_API=false`) → Real API integration (30 min)

**Result:** Frontend ready Week 4 (vs Week 7 if waited for Backend)

---

## Related Skills

- **contract-first-development-workflow:** OpenAPI spec provides mock structure
- **fsm-aggregate-generator:** Mock FSM state transitions
- **checkpoint-coordination-workflow:** CP-BACKEND checkpoint triggers real API swap

---

## Maintenance Notes

**When to update mocks:**
- OpenAPI spec changes (new endpoints, fields)
- Realistic data needed (edge cases, error scenarios)

**Mock data sources:**
- Faker.js for realistic data
- Production snapshots (anonymized)

---

**Skill Owner:** Librarian
**Created:** 2026-07-04
**Status:** ACTIVE
