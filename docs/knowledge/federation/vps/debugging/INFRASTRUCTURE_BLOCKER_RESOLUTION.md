# Infrastructure Blocker Resolution Guide

> **Pattern:** Structured diagnosis and resolution for infrastructure blockers
> **MTTR Goal:** Mean Time to Resolution <24 hours
> **Escalation:** VPS operator if >24h timeout

---

## Overview

Infrastructure blockers are issues that prevent development work but are outside the control of the development team:
- Network connectivity (NuGet, npm, Docker Hub timeouts)
- Build environment (dotnet, node, toolchain missing)
- Deployment infrastructure (VPS access, SSH keys, ports)
- External services (Azure, PostgreSQL, Redis down)

**Key Principle:** Distinguish infrastructure issues from code bugs — different resolution paths.

---

## Decision Tree

```
Infrastructure Blocker Detected
  │
  ├─ Network Issue?
  │   ├─ DNS resolution fails → Check /etc/resolv.conf, ping 8.8.8.8
  │   ├─ Firewall blocking → Check iptables, VPS provider firewall
  │   ├─ Timeout on downloads → Check bandwidth, retry with mirror
  │   └─ Proxy/VPN required → Configure proxy settings
  │
  ├─ Build Issue?
  │   ├─ Toolchain missing → Install dotnet/node/cargo/etc.
  │   ├─ Dependencies unavailable → Check package repo status
  │   ├─ Compilation errors → Verify SDK version match
  │   └─ Permission denied → Check file ownership, sudo requirements
  │
  ├─ Deploy Issue?
  │   ├─ SSH access denied → Regenerate keys, check authorized_keys
  │   ├─ Port already in use → Kill process or change port
  │   ├─ Service won't start → Check logs, systemd status
  │   └─ Environment vars missing → Verify .env, systemd config
  │
  └─ External Service Issue?
      ├─ Azure down → Check Azure status page, use fallback
      ├─ PostgreSQL unreachable → Check pg_hba.conf, network
      ├─ Redis timeout → Check Redis server status, memory
      └─ Third-party API down → Implement circuit breaker, retry logic
```

---

## Case Study 1: NuGet Timeout (MSG-ROOT-002)

### Problem

**Symptoms:**
```bash
$ dotnet restore
error NU1301: Unable to load the service index for source https://api.nuget.org/v3/index.json.
error NU1301: The request was aborted: Could not create SSL/TLS secure channel.
```

**Impact:**
- Backend terminal cannot build .NET projects
- Week 2 JoineryTech development blocked
- Frontend unaffected (uses npm, not NuGet)

**Timeline:**
- Day 1 (12:00): Backend detects NuGet timeout, creates BLOCKED outbox
- Day 1 (14:00): Conductor escalates to Root (MSG-ROOT-002)
- Day 1 (16:00): Root diagnoses network issue, contacts VPS operator
- Day 2 (10:00): VPS operator resolves firewall rule
- Day 2 (11:00): Backend unblocked, resumes work

### Diagnostic Steps

**Step 1: Verify NuGet endpoint reachability**
```bash
# Test DNS resolution
nslookup api.nuget.org

# Test HTTP connectivity
curl -I https://api.nuget.org/v3/index.json

# Expected output:
# HTTP/2 200
# content-type: application/json
```

**Result:** If curl fails → Network issue (go to Step 2)

**Step 2: Check firewall and network config**
```bash
# Check iptables rules
sudo iptables -L -n -v | grep nuget

# Check VPS provider firewall (Hetzner Cloud Console)
# Navigate to Firewalls → check if HTTPS (443) outbound allowed

# Check DNS config
cat /etc/resolv.conf
# Expected: nameserver 8.8.8.8 or similar
```

**Result:** If firewall blocks HTTPS → Escalate to VPS operator

**Step 3: Try alternative NuGet source**
```bash
# Add NuGet.org mirror
dotnet nuget add source https://pkgs.dev.azure.com/_packaging/NuGet/nuget/v3/index.json -n AzureNuGet

# Retry restore
dotnet restore --source https://pkgs.dev.azure.com/_packaging/NuGet/nuget/v3/index.json
```

**Result:** If mirror works → Temporary workaround until VPS firewall fixed

**Step 4: Local NuGet cache workaround**
```bash
# Download packages on local machine (working network)
dotnet restore

# Copy NuGet cache to VPS
scp -r ~/.nuget/packages user@vps:/home/spaceos/.nuget/

# Restore offline
dotnet restore --packages ~/.nuget/packages --no-http-cache
```

**Result:** Offline restore works → Unblocks development temporarily

### Resolution

**Root Cause:** VPS provider firewall blocking HTTPS outbound to `api.nuget.org`

**Fix Applied:**
1. VPS operator added firewall rule: Allow HTTPS (443) outbound
2. Verified with `curl -I https://api.nuget.org/v3/index.json`
3. Backend terminal verified with `dotnet restore`

**Prevention:**
- Infrastructure checklist: Verify NuGet/npm/Docker Hub connectivity before terminal assignment
- Smoke test script: `scripts/smoke-test-network.sh` (runs on VPS provisioning)

---

## Case Study 2: tmux Review Session Failure (MSG-CONDUCTOR-064)

### Problem

**Symptoms:**
- Architect terminal session hangs during review
- `tmux send-keys` does not inject prompt
- Manual review approval required

**Impact:**
- Automated review pipeline blocked
- Manual Root intervention needed (MSG-CONDUCTOR-064)
- 4-hour delay in DONE message processing

**Timeline:**
- Day 1 (14:00): Architect review session hangs
- Day 1 (16:00): Conductor detects stuck session, creates BLOCKED outbox
- Day 1 (17:00): Root manually approves DONE, bypasses review
- Day 2 (10:00): Librarian investigates root cause
- Day 2 (14:00): Fix implemented (review redundancy pattern)

### Diagnostic Steps

**Step 1: Check tmux session status**
```bash
# List tmux sessions
tmux ls

# Expected output:
# spaceos-architect: 1 windows (created Thu Jul  4 14:23:15 2026)

# Attach to session
tmux attach -t spaceos-architect

# Check if session is responsive
# Press Ctrl+C, Enter — does prompt appear?
```

**Result:** If session frozen → Terminate and restart (Step 2)

**Step 2: Check Claude Code process**
```bash
# Find Claude Code process
ps aux | grep claude

# Kill if hung
kill -9 <PID>

# Restart session
cd /opt/spaceos/terminals/architect
claude-code --session spaceos-architect --model sonnet
```

**Result:** If process hung → Restart resolves issue temporarily

**Step 3: Check MCP server health**
```bash
# Check knowledge service status
curl -s http://localhost:3456/health | jq

# Expected:
# { "status": "ok", "uptime": 3600 }

# Check MCP logs
tail -50 /opt/spaceos/spaceos-nexus/knowledge-service/logs/mcp.log
```

**Result:** If MCP down → Restart `npm run dev`

**Step 4: Implement review redundancy**
```bash
# Dual-reviewer pattern (Architect + Librarian)
# If Architect hangs, Librarian session continues independently
# Manual approval fallback path
```

**Result:** Redundancy prevents single point of failure

### Resolution

**Root Cause:** tmux session process hang (likely MCP connection timeout)

**Fix Applied:**
1. Implemented dual-reviewer pattern (Architect + Librarian independent sessions)
2. Manual approval fallback path (Conductor → Root escalation)
3. MCP health check in review startup script

**Prevention:**
- Review redundancy architecture (ADR-053)
- Watchdog script: Auto-restart hung sessions after 5 minutes idle
- MCP heartbeat monitoring (ping every 30 seconds)

---

## Parallel Development Workaround

### Scenario: Backend Blocked by Build Issues

**Problem:**
- NuGet timeout prevents Backend from building/testing code
- Week 3-4 planned work blocked
- Frontend and Architect unaffected

**Workaround: Design-Only Mode**

**Backend continues development without building:**

```markdown
Week 3 Plan (Build blocked):
- ✅ Design CRM module structure (DDD/FSM)
- ✅ Write ADR for CRM integration
- ✅ OpenAPI spec writing (contract-first)
- ✅ Create CQRS handler pseudocode
- ❌ Build/test (blocked by NuGet)

Deliverables:
- docs/architecture/decisions/ADR-CRM.md
- docs/joinerytech/API_SPEC_CRM.yaml
- docs/planning/CRM_IMPLEMENTATION_PLAN.md

Exit Criteria:
- ADR reviewed and approved
- OpenAPI spec locked
- Implementation plan ready (awaiting build fix)
```

**Benefits:**
- Backend not idle (design work valuable)
- Frontend unblocked (uses OpenAPI spec for mock API)
- Implementation ready when build issue resolved

**Risk Mitigation:**
- Design-only work does not validate buildability
- Technical debt: Must verify designs when build restored
- Time limit: Max 1 week design-only, then escalate

---

## Priority Escalation Pattern

### Escalation Levels

| Level | Who Resolves | Escalation Criteria | Response Time |
|-------|--------------|---------------------|---------------|
| **L1: Terminal Self-Resolution** | Terminal (Backend, Frontend, etc.) | Network issue with known fix (e.g., retry NuGet restore) | <1 hour |
| **L2: Conductor Coordination** | Conductor | Affects multiple terminals, workaround available | <4 hours |
| **L3: Root Strategic Decision** | Root | Blocks critical path, no workaround | <24 hours |
| **L4: VPS Operator Intervention** | VPS Operator | Infrastructure change required (firewall, SSH, etc.) | <24 hours |

### Escalation Decision Matrix

| Blocker Type | Self-Resolve | Conductor | Root | VPS Operator |
|--------------|--------------|-----------|------|--------------|
| NuGet timeout (first occurrence) | ✅ Retry | - | - | - |
| NuGet timeout (3+ retries) | - | ✅ Workaround | - | - |
| NuGet timeout (>24h) | - | - | ✅ Escalate | ✅ Fix firewall |
| SSH access denied | - | - | ✅ Regenerate keys | ✅ Fix authorized_keys |
| Port 5000 in use | ✅ Kill process | - | - | - |
| PostgreSQL down | - | ✅ Check service | ✅ Escalate if critical | - |
| Azure service down | - | - | ✅ Implement fallback | - |

### Escalation Message Template

**BLOCKED Outbox (L2 → Conductor):**
```markdown
---
id: MSG-BACKEND-XXX-BLOCKED
from: backend
to: conductor
type: blocked
priority: high
blocked_reason: "NuGet timeout (3+ retries, >2 hours)"
created: 2026-07-04
---

# Backend Blocked: NuGet Timeout

**Blocker:** Cannot restore NuGet packages

## Symptoms:
- `dotnet restore` fails with NU1301 error
- 3 retries over 2 hours, all failed
- Last attempt: 2026-07-04 12:45 UTC

## Diagnostics Performed:
- ✅ DNS resolution OK (nslookup api.nuget.org)
- ❌ HTTPS connectivity FAIL (curl timeout after 30s)
- ✅ Alternative mirror tried (Azure NuGet) — also fails

## Impact:
- Week 3 CRM module implementation blocked
- Cannot build/test code
- Frontend unaffected (uses npm)

## Workaround Attempted:
- Offline NuGet cache restore (SUCCESS)
- Continuing with design-only work

## Escalation Request:
**To Conductor:** Please escalate to Root for VPS operator intervention.
**Blocker Duration:** >2 hours
**Expected Resolution:** Firewall rule change
```

**Escalation to Root (L3):**
```markdown
---
id: MSG-CONDUCTOR-064
from: conductor
to: root
type: escalation
priority: critical
ref: MSG-BACKEND-XXX-BLOCKED
---

# Infrastructure Blocker: NuGet Firewall Issue

**Summary:** Backend terminal blocked by NuGet timeout >2 hours

**Root Cause:** VPS firewall blocking HTTPS to api.nuget.org

**Workaround:** Offline NuGet cache (temporary)

**Request:** Contact VPS operator to allow HTTPS (443) outbound

**Impact:** Critical path blocked (Week 3 CRM module)

**SLA:** 24-hour escalation threshold approaching
```

---

## Infrastructure Checklist (Prevention)

### Pre-Deployment (VPS Provisioning)

```bash
# Network connectivity smoke test
curl -I https://api.nuget.org/v3/index.json || echo "FAIL: NuGet"
curl -I https://registry.npmjs.org/ || echo "FAIL: npm"
curl -I https://index.docker.io/v2/ || echo "FAIL: Docker Hub"

# DNS resolution
nslookup github.com || echo "FAIL: DNS"

# Firewall rules
sudo iptables -L -n -v | grep ACCEPT || echo "WARN: Check firewall"

# SSH keys
ssh-keygen -t ed25519 -f ~/.ssh/spaceos-vps -N ""
cat ~/.ssh/spaceos-vps.pub >> ~/.ssh/authorized_keys
```

### Weekly Health Check (Automated)

```bash
# scripts/health/infrastructure-check.sh

#!/bin/bash
set -e

echo "=== Infrastructure Health Check ==="

# NuGet
dotnet restore --no-cache /opt/spaceos/spaceos-kernel/ || exit 1

# npm
npm install --prefix /opt/spaceos/datahaven-web/client || exit 1

# PostgreSQL
psql -U spaceos -h localhost -d spaceos -c "SELECT 1" || exit 1

# Redis
redis-cli ping || exit 1

# MCP Server
curl -f http://localhost:3456/health || exit 1

echo "✅ All infrastructure checks passed"
```

**Cron schedule:** Every Sunday 02:00 UTC
```bash
0 2 * * 0 /opt/spaceos/scripts/health/infrastructure-check.sh >> /opt/spaceos/logs/health/infrastructure.log 2>&1
```

---

## Common Infrastructure Blockers

### 1. NuGet Package Restore Timeout

**Symptoms:** `dotnet restore` fails with NU1301

**Diagnosis:**
```bash
curl -I https://api.nuget.org/v3/index.json
```

**Fix:**
- Verify HTTPS (443) outbound allowed in firewall
- Try alternative mirror (Azure NuGet)
- Offline cache workaround (temporary)

**Escalation:** VPS operator if >24h

---

### 2. npm Package Install Timeout

**Symptoms:** `npm install` hangs or times out

**Diagnosis:**
```bash
curl -I https://registry.npmjs.org/
npm config get registry
```

**Fix:**
- Increase npm timeout: `npm config set fetch-timeout 60000`
- Try npm mirror: `npm config set registry https://registry.npmmirror.com`
- Clear cache: `npm cache clean --force`

**Escalation:** Conductor if affects critical path

---

### 3. PostgreSQL Connection Refused

**Symptoms:** `psql: could not connect to server: Connection refused`

**Diagnosis:**
```bash
sudo systemctl status postgresql
sudo netstat -tulpn | grep 5432
cat /etc/postgresql/*/main/pg_hba.conf | grep -v "^#"
```

**Fix:**
- Start service: `sudo systemctl start postgresql`
- Check port: Default 5432, change if conflict
- Allow localhost: Add `host all all 127.0.0.1/32 trust` to pg_hba.conf

**Escalation:** Root if systemctl fails

---

### 4. Port Already in Use

**Symptoms:** `Error: listen EADDRINUSE: address already in use :::5000`

**Diagnosis:**
```bash
sudo lsof -i :5000
```

**Fix:**
- Kill process: `sudo kill -9 <PID>`
- Change port in code/config
- Use different port range (5001-5010)

**Escalation:** None (self-resolve)

---

### 5. SSH Access Denied

**Symptoms:** `Permission denied (publickey)`

**Diagnosis:**
```bash
ssh -v user@vps.example.com
cat ~/.ssh/authorized_keys
```

**Fix:**
- Regenerate keys: `ssh-keygen -t ed25519 -f ~/.ssh/new-key`
- Add to authorized_keys on VPS
- Verify permissions: `chmod 700 ~/.ssh && chmod 600 ~/.ssh/authorized_keys`

**Escalation:** VPS operator if no console access

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **MTTR (Mean Time to Resolution)** | <24 hours | Blocker timestamp → resolution timestamp |
| **Self-Resolution Rate** | ≥70% | L1 resolved / total blockers |
| **Escalation Rate** | ≤20% | L4 escalations / total blockers |
| **Preventable Blockers** | ≤10% | Blockers caught by health check / total |

---

## Anti-Patterns to Avoid

| Anti-Pattern | Why It's Wrong | Fix |
|--------------|----------------|-----|
| **"Works on my machine"** | Ignores VPS environment differences | Test on VPS, not local |
| **Immediate escalation** | Overwhelms Root, delays self-resolution | Follow L1 → L2 → L3 → L4 escalation path |
| **No workaround attempted** | Idle waiting wastes time | Try offline cache, alternative mirror, design-only mode |
| **Single retry** | Transient network issues common | Retry 3× with exponential backoff before escalating |
| **No diagnostic log** | Root cannot debug without context | Include diagnostics in BLOCKED outbox |

---

## Related Patterns

- **ADR-053:** Checkpoint Coordination (parallel dev workaround)
- **Review Redundancy Architecture:** Dual-reviewer fallback
- **Cold Mode Session Pattern:** Session startup health checks
- **MCP Integration Workflow:** MCP health monitoring

---

## References

- **MSG-ROOT-002:** NuGet timeout escalation (2026-07-02)
- **MSG-CONDUCTOR-064:** tmux review session failure (2026-07-02)
- **ADR-058:** JoineryTech Integration (Week 0 contract-first workaround)
- **SpaceOS Codebase Status:** `/opt/spaceos/docs/Codebase_Status.md`

---

**Pattern Owner:** Librarian
**Last Updated:** 2026-07-04
**Status:** ACTIVE — Reference for all infrastructure blockers
