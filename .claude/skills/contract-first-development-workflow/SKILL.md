# Contract-First Development Workflow

## Purpose

Execute the **Week 0 OpenAPI specification workflow** before Backend and Frontend implementation begins. This skill ensures API contracts are defined, reviewed, and approved before any code is written, enabling parallel development and preventing integration rework.

**ROI:** $4k investment → $11-16k savings (prevents 2 weeks of integration rework)

---

## When to Use

Trigger this skill when:
- Starting a new API module (CRM, Kontrolling, HR, etc.)
- Backend and Frontend need to develop in parallel
- Integration risks are high (multi-team coordination)
- Contract-first development is mandated (JoineryTech standard)

**DO NOT use** for:
- Internal refactoring (no API changes)
- Prototype/spike work (spec not needed)
- Single-developer projects (no coordination overhead)

---

## Prerequisites

**Tools:**
- OpenAPI 3.1 editor (Swagger Editor, Stoplight Studio, or VS Code + OpenAPI extension)
- `curl` (for endpoint testing)
- Orval (Frontend code-gen): `npm install --save-dev orval`
- NSwag (Backend code-gen): `dotnet tool install --global NSwag.ConsoleCore`

**Team:**
- Architect (facilitator)
- Backend terminal (API implementer)
- Frontend terminal (API consumer)

**Inputs:**
- Endpoint inventory (list of all endpoints to document)
- Existing domain model (ADR, FSM definitions)
- 5 Golden Rules compliance checklist

---

## Step-by-Step

### Day 1: Endpoint Inventory (4-6 hours)

**Owner:** Architect + Backend + Frontend

1. **List all Phase 1 endpoints:**
   ```markdown
   | Endpoint | Method | Auth | Request | Response | Owner |
   |----------|--------|------|---------|----------|-------|
   | /auth/login | POST | No | LoginRequest | LoginResponse | Backend |
   | /auth/refresh | POST | Yes | RefreshRequest | LoginResponse | Backend |
   | /catalog | GET | Yes | - | CatalogListResponse | Backend |
   | /catalog/{id} | GET | Yes | - | CatalogItemResponse | Backend |
   ```

2. **Define shared components:**
   - Error response format (400, 401, 422, 500)
   - Pagination schema
   - Common types (UUID, DateTime, Enum)

3. **Identify validation rules:**
   - Required fields
   - Min/max lengths
   - Regex patterns (email, phone, etc.)
   - Format constraints (date-time, email, uri)

**Deliverable:** Endpoint inventory spreadsheet

### Day 2: OpenAPI Schema Definition (6-8 hours)

**Owner:** Backend + Frontend (pair programming recommended)

1. **Create OpenAPI spec file:**
   ```bash
   touch /opt/spaceos/docs/joinerytech/API_SPEC_PHASE1.yaml
   ```

2. **Write schemas for each endpoint:**
   ```yaml
   paths:
     /auth/login:
       post:
         summary: User login
         operationId: login
         tags: [Authentication]
         requestBody:
           required: true
           content:
             application/json:
               schema:
                 type: object
                 required: [email, password]
                 properties:
                   email:
                     type: string
                     format: email
                     example: "user@joinerytech.hu"
                   password:
                     type: string
                     format: password
                     minLength: 8
                     example: "SecurePass123"
         responses:
           '200':
             description: Login successful
             content:
               application/json:
                 schema:
                   $ref: '#/components/schemas/LoginResponse'
           '401':
             $ref: '#/components/responses/Unauthorized'
           '422':
             $ref: '#/components/responses/ValidationError'
           '500':
             $ref: '#/components/responses/InternalServerError'
   ```

3. **Add validation rules:**
   - Use `required`, `minLength`, `maxLength`, `pattern`, `format`
   - Add examples for all fields

4. **Define error responses:**
   ```yaml
   components:
     responses:
       Unauthorized:
         description: Invalid credentials
         content:
           application/json:
             schema:
               type: object
               required: [message, code]
               properties:
                 message:
                   type: string
                   example: "Invalid credentials"
                 code:
                   type: string
                   example: "AUTH_INVALID_CREDENTIALS"
   ```

**Deliverable:** `API_SPEC_PHASE1.yaml` (draft)

### Day 3: Review + Iteration (4-6 hours)

**Owner:** Architect (facilitator), Backend + Frontend (reviewers)

1. **Frontend review:**
   - Are all fields needed for UI present?
   - Are response types correctly structured?
   - Are examples realistic and helpful?

2. **Backend review:**
   - Are validation rules implementable?
   - Are status codes correct (200, 400, 401, 422, 500)?
   - Are error messages consistent?

3. **Architect validation:**
   - Consistent with 5 Golden Rules?
   - No security vulnerabilities (e.g., password in GET params)?
   - RESTful naming conventions followed?

4. **Iterate on feedback:**
   - 2-3 review rounds typical
   - Document decisions in ADR if architectural

**Checklist:**
- [ ] All Phase 1 endpoints documented
- [ ] Error response schemas defined (400, 401, 422, 500)
- [ ] Validation rules specified (required, min/max, regex)
- [ ] Examples provided for all responses
- [ ] Frontend team reviewed and approved
- [ ] Backend team reviewed and approved
- [ ] Architect validated compliance

### Day 4: Lock Spec + Code Generation Setup (4-6 hours)

**Owner:** Backend + Frontend

1. **Lock spec:**
   - No changes without formal review
   - Version control: Commit to `/opt/spaceos/docs/joinerytech/API_SPEC_PHASE1.yaml`
   - Tag: `v1.0.0-phase1-locked`

2. **Frontend: Orval setup:**
   ```bash
   cd /opt/spaceos/datahaven-web/client
   npm install --save-dev orval
   ```

   Create `orval.config.ts`:
   ```typescript
   import { defineConfig } from 'orval';

   export default defineConfig({
     api: {
       input: '../../../docs/joinerytech/API_SPEC_PHASE1.yaml',
       output: {
         mode: 'tags-split',
         target: 'src/api/generated',
         client: 'react-query',
         clean: true,
         override: {
           mutator: {
             path: './src/api/custom-instance.ts',
             name: 'customInstance',
           },
         },
       },
     },
   });
   ```

   Generate:
   ```bash
   npx orval
   ```

3. **Backend: NSwag setup (for Orchestrator TypeScript client):**
   ```bash
   nswag openapi2tsclient \
     /input:docs/joinerytech/API_SPEC_PHASE1.yaml \
     /output:spaceos-nexus/orchestrator/src/clients/joinerytech-client.ts \
     /generateClientClasses:true
   ```

4. **Verify generated clients compile:**
   ```bash
   # Frontend
   npm run build

   # Orchestrator
   cd spaceos-nexus/orchestrator
   npm run build
   ```

**Deliverable:** Locked spec + working code-gen

---

## Error Handling

### Common Issues

**1. Spec validation errors:**
```bash
$ npx orval
Error: Invalid OpenAPI schema - Missing 'operationId' at /auth/login
```

**Fix:** Add `operationId` to all endpoints (required by Orval)

**2. Type mismatch (Frontend vs Backend):**
```typescript
// Generated (Frontend)
interface LoginResponse {
  accessToken: string;
  expiresIn: number;
}

// Backend implementation
{
  "accessToken": "...",
  "expiresIn": "3600"  // ❌ String instead of number
}
```

**Fix:** Update spec `expiresIn` type to `string` or fix Backend response

**3. Missing examples:**
```yaml
# ❌ No example
password:
  type: string
  format: password

# ✅ With example
password:
  type: string
  format: password
  minLength: 8
  example: "SecurePass123"
```

**Fix:** Add examples for all fields (required by review checklist)

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Spec Approval Time** | 3-4 days | Calendar days (Week 0) |
| **Integration Rework** | 0 days | Developer time tracking |
| **Code Generation Time** | <5 seconds | `npx orval` execution time |
| **Frontend Unblocked** | Week 1 | MSW mock API ready |
| **Backend Unblocked** | Week 1 | Contract tests CI/CD |
| **Spec Changes** | ≤3 per phase | Git commits to spec file |

---

## Real-World Example

### EPIC-JT-CRM Week 0 (3 days)

**Day 1 (June 30):**
- Architect facilitated endpoint inventory session
- Backend proposed Auth API structure
- Frontend requested `user` object in login response
- Result: 14 endpoints identified

**Day 2 (July 1):**
- Backend wrote OpenAPI schemas
- Frontend paired on review
- 8 schemas defined (LoginRequest, LoginResponse, Lead, Opportunity, etc.)
- Result: `API_SPEC_CRM.yaml` draft

**Day 3 (July 2):**
- Architect validated 5 Golden Rules compliance
- Frontend requested `phoneNumber` field (missing)
- Backend adjusted validation rules (email regex)
- Result: Spec locked, v1.0.0

**Day 4 (July 3):**
- Frontend: Orval code-gen setup (5 React Query hooks generated)
- Orchestrator: NSwag TypeScript client generated
- Backend: Contract tests setup (Dredd)
- Result: All teams unblocked, parallel dev starts Week 1

**ROI:**
- Investment: $4k (3 days × 3 FTE)
- Savings: $14k (prevented 2 weeks of integration rework discovered in original timeline)
- Total ROI: 250% return

---

## Related Skills

- **mock-api-parallel-development:** Frontend uses MSW while Backend implements
- **fsm-aggregate-generator:** Domain model → OpenAPI schema mapping
- **checkpoint-coordination-workflow:** Multi-team epic orchestration
- **infrastructure-blocker-resolution-guide:** Network/build issues during spec writing

---

## Maintenance Notes

**When to update spec:**
- New endpoints added (Phase 2+)
- Breaking changes (major version bump)
- Security vulnerabilities discovered

**Versioning:**
- Semantic versioning: `v1.0.0` (major.minor.patch)
- Lock major versions per phase (Phase 1 = v1.x.x, Phase 2 = v2.x.x)

**Deprecation:**
- Mark deprecated endpoints in spec
- Provide migration guide
- Maintain old version for 2 releases

**Code-gen updates:**
- Re-run `npx orval` after spec changes
- Re-run `nswag` after spec changes
- Commit generated code to version control (optional, but recommended for reproducibility)

---

**Skill Owner:** Librarian
**Created:** 2026-07-04
**Status:** ACTIVE — Use for all multi-team API projects
