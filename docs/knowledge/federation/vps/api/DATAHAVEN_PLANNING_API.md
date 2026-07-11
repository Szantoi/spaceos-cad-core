# Datahaven Planning API Documentation

> **Version:** 1.0
> **Last Updated:** 2026-06-24
> **Base URL:** `https://datahaven.joinerytech.hu`
> **Authentication:** Bearer token required

---

## Overview

The Datahaven Planning API provides endpoints for managing the SpaceOS planning pipeline, including domain focus configuration and epic workflow management.

**Key Features:**
- Domain focus selection (7 predefined domains)
- Planning criteria management (markdown-formatted)
- Epic dependency graph updates
- YAML-backed data storage with Git audit trail

---

## Authentication

All endpoints require authentication via Bearer token.

```http
Authorization: Bearer dev-token-spaceos-dashboard-2026
```

**Error Response (401 Unauthorized):**
```json
{
  "error": "Unauthorized",
  "message": "Missing or invalid authorization token"
}
```

---

## Endpoints

### 1. GET /api/planning/domain-focus

Retrieve the current planning domain and criteria.

**Purpose:**
Load the active planning domain and criteria list for the Focus Area Panel.

**Request:**
```bash
curl -X GET https://datahaven.joinerytech.hu/api/planning/domain-focus \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026"
```

**Response (200 OK):**
```json
{
  "domain": "manufacturing",
  "criteria": "## Focus Criteria\n\n- **Felhasználói érték**: Milyen funkció segíti...\n- **Backend kapcsolhatóság**: Van-e már meglévő...\n- **Iparági minták**: Mi az ami más ERP/MES...",
  "updated_at": "2026-06-24T12:34:56Z"
}
```

**Response Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `domain` | string | Active domain (one of 7 predefined values) |
| `criteria` | string | Markdown-formatted criteria list |
| `updated_at` | ISO 8601 datetime | Last modification timestamp |

**Error Codes:**
| Code | Description |
|------|-------------|
| `401 Unauthorized` | Missing or invalid auth token |
| `500 Internal Server Error` | File read error (check logs) |

**Example:**
```bash
# Simple fetch
curl -s https://datahaven.joinerytech.hu/api/planning/domain-focus \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" | jq .
```

---

### 2. PUT /api/planning/domain-focus

Update the planning domain and/or criteria.

**Purpose:**
Change the active domain or edit the criteria list. Both fields are optional—send only what changed.

**Rate Limit:** 10 writes/minute per IP

**Request:**
```bash
curl -X PUT https://datahaven.joinerytech.hu/api/planning/domain-focus \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "domain": "sales",
    "criteria": "## Updated Criteria\n\n- Focus on CRM module\n- Lead conversion flow"
  }'
```

**Request Body:**
```json
{
  "domain": "sales",     // Optional (if only domain changed)
  "criteria": "..."      // Optional (if only criteria changed)
}
```

**Domain Whitelist:**
The `domain` field must be one of:
- `all` (default)
- `joinery`
- `cutting`
- `manufacturing`
- `ehs`
- `catalog`
- `sales`

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Domain focus updated successfully",
  "domain": "sales",
  "criteria": "## Updated Criteria\n\n- Focus on CRM module\n- Lead conversion flow",
  "updated_at": "2026-06-24T12:35:01Z"
}
```

**Error Codes:**
| Code | Description |
|------|-------------|
| `400 Bad Request` | Invalid domain (not in whitelist) |
| `401 Unauthorized` | Missing or invalid auth token |
| `429 Too Many Requests` | Rate limit exceeded (10 writes/min) |
| `500 Internal Server Error` | File write error |

**Validation Rules:**
- `domain` must be in the predefined whitelist
- `criteria` is sanitized (HTML tags stripped to prevent XSS)
- At least one field (`domain` or `criteria`) must be provided

**Examples:**
```bash
# Change domain only
curl -X PUT https://datahaven.joinerytech.hu/api/planning/domain-focus \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"domain":"cutting"}'

# Update criteria only
curl -X PUT https://datahaven.joinerytech.hu/api/planning/domain-focus \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"criteria":"## New Focus\n\n- Priority 1\n- Priority 2"}'

# Update both
curl -X PUT https://datahaven.joinerytech.hu/api/planning/domain-focus \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"domain":"ehs","criteria":"## EHS Focus\n\n- Risk assessment\n- Safety protocols"}'
```

---

### 3. PUT /api/graph/epics/:id

Update an epic's status, dependencies, or target date.

**Purpose:**
Modify epic metadata in the EPICS.yaml file. Used by the Flow/Workflow Editor.

**Request:**
```bash
curl -X PUT https://datahaven.joinerytech.hu/api/graph/epics/EPIC-CUTTING-Q3 \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "active",
    "depends_on": ["EPIC-KERNEL-STABLE"],
    "target_date": "2026-09-30"
  }'
```

**URL Parameters:**
| Parameter | Description |
|-----------|-------------|
| `:id` | Epic ID (e.g., `EPIC-CUTTING-Q3`) |

**Request Body:**
```json
{
  "status": "active",                     // Optional: pending|active|done|blocked
  "depends_on": ["EPIC-X", "EPIC-Y"],     // Optional: dependency list
  "parallel_with": ["EPIC-Z"],            // Optional: parallel execution list
  "target_date": "2026-12-31"             // Optional: ISO date (YYYY-MM-DD)
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Epic EPIC-CUTTING-Q3 updated successfully",
  "epic": {
    "id": "EPIC-CUTTING-Q3",
    "name": "Cutting Module Q3 Release",
    "status": "active",
    "depends_on": ["EPIC-KERNEL-STABLE"],
    "parallel_with": [],
    "target_date": "2026-09-30",
    "description": "Lapszabász modul: nesting, optimization, CNC integration"
  },
  "validation": {
    "valid": true,
    "cycles": []
  }
}
```

**Error Codes:**
| Code | Description |
|------|-------------|
| `400 Bad Request` | Validation error (cycle detected, invalid status, etc.) |
| `401 Unauthorized` | Missing or invalid auth token |
| `404 Not Found` | Epic not found in EPICS.yaml |
| `500 Internal Server Error` | File write error |

**Validation Rules:**

**Status Transitions:**
Valid state machine transitions:
```
pending → active → done
pending → blocked
active → blocked
blocked → active (retry)
```

Invalid transitions (rejected):
```
done → pending  ❌ (cannot un-complete an epic)
done → active   ❌
```

**Dependency Validation:**
- Dependencies cannot create cycles (A → B → A)
- Cycle detection runs automatically before write
- If a cycle is detected, the request is rejected with a 400 error

**Examples:**
```bash
# Change status
curl -X PUT https://datahaven.joinerytech.hu/api/graph/epics/EPIC-CUTTING-Q3 \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"status":"done"}'

# Add dependency
curl -X PUT https://datahaven.joinerytech.hu/api/graph/epics/EPIC-PORTAL-V2 \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"depends_on":["EPIC-KERNEL-STABLE","EPIC-ORCH-V2"]}'

# Update target date
curl -X PUT https://datahaven.joinerytech.hu/api/graph/epics/EPIC-JOINERY-V2 \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"target_date":"2026-08-15"}'

# Update multiple fields
curl -X PUT https://datahaven.joinerytech.hu/api/graph/epics/EPIC-CUTTING-Q3 \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "active",
    "depends_on": ["EPIC-KERNEL-STABLE"],
    "parallel_with": ["EPIC-JOINERY-V2"],
    "target_date": "2026-09-30"
  }'
```

**Error Response Examples:**

**400 Bad Request (Cycle Detected):**
```json
{
  "success": false,
  "error": "Validation failed",
  "message": "Cycle detected in dependency graph",
  "cycles": [
    ["EPIC-A", "EPIC-B", "EPIC-C", "EPIC-A"]
  ]
}
```

**400 Bad Request (Invalid Status Transition):**
```json
{
  "success": false,
  "error": "Invalid status transition",
  "message": "Cannot transition from 'done' to 'pending'"
}
```

**404 Not Found:**
```json
{
  "success": false,
  "error": "Epic not found",
  "message": "Epic with ID 'EPIC-NONEXISTENT' does not exist in EPICS.yaml"
}
```

---

## Data Models

### DomainFocus

```typescript
interface DomainFocus {
  domain: 'all' | 'joinery' | 'cutting' | 'manufacturing' | 'ehs' | 'catalog' | 'sales';
  criteria: string;  // Markdown-formatted
  updated_at: string; // ISO 8601 datetime
}
```

### Epic

```typescript
interface Epic {
  id: string;
  name: string;
  status: 'pending' | 'active' | 'done' | 'blocked';
  depends_on: string[];      // Epic IDs
  parallel_with?: string[];  // Epic IDs (optional)
  target_date: string;       // YYYY-MM-DD
  description?: string;      // Markdown description (optional)
}
```

---

## Rate Limiting

**Domain Focus Endpoints:**
- `PUT /api/planning/domain-focus`: **10 writes/minute per IP**
- Other endpoints: No limit (read-only)

**Epic Update Endpoints:**
- No rate limit currently enforced (write frequency expected to be low)

**Rate Limit Error (429 Too Many Requests):**
```json
{
  "error": "Too Many Requests",
  "message": "Rate limit exceeded. Max 10 writes per minute.",
  "retry_after": 45  // Seconds until next allowed request
}
```

---

## Security Considerations

**Authentication:**
- All endpoints require valid Bearer token
- Token: `dev-token-spaceos-dashboard-2026` (development only)
- Production token rotation: TBD

**Input Sanitization:**
- **Domain field:** Validated against whitelist (7 values)
- **Criteria field:** HTML tags stripped to prevent XSS
- **Epic fields:** YAML injection protection (all inputs escaped)

**Atomic File Writes:**
- All file writes use atomic temp-file + rename pattern
- No partial writes on error

**Git Audit Trail:**
- All changes to `domain-focus.md` and `EPICS.yaml` are Git-committed
- Commit message format: `Auto-update <file> via UI [timestamp]`

---

## Testing

**API Endpoint Tests:**

```bash
# Test domain focus GET
curl -s https://datahaven.joinerytech.hu/api/planning/domain-focus \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026"

# Test domain focus PUT (valid domain)
curl -X PUT https://datahaven.joinerytech.hu/api/planning/domain-focus \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"domain":"cutting"}'

# Test domain focus PUT (invalid domain - should fail)
curl -X PUT https://datahaven.joinerytech.hu/api/planning/domain-focus \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"domain":"INVALID"}' # Expect 400 error

# Test epic update (valid transition)
curl -X PUT https://datahaven.joinerytech.hu/api/graph/epics/EPIC-CUTTING-Q3 \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"status":"active"}'

# Test epic update (cycle detection - should fail if creates cycle)
curl -X PUT https://datahaven.joinerytech.hu/api/graph/epics/EPIC-CUTTING-Q3 \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"depends_on":["EPIC-PORTAL-V2"]}' # Expect 400 if creates cycle
```

---

## Related Documentation

- **User Guide:** [PLANNING_UI_USER_GUIDE.md](../datahaven/PLANNING_UI_USER_GUIDE.md)
- **Architecture:** [ADR-048-Datahaven-UI-Planning-Components.md](../architecture/ADR-048-Datahaven-UI-Planning-Components.md)
- **Graph Workflow:** [ADR-041-graph-based-workflow-architecture.md](../architecture/ADR-041-graph-based-workflow-architecture.md)

---

## Changelog

### 2026-06-24 — v1.0 (Initial)
- Added `GET /api/planning/domain-focus`
- Added `PUT /api/planning/domain-focus`
- Added `PUT /api/graph/epics/:id`
- Documented validation rules, error codes, rate limits
