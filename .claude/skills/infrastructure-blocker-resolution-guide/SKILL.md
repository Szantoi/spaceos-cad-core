# Infrastructure Blocker Resolution Guide

## Purpose

Execute the **infrastructure blocker diagnosis and resolution workflow** when technical blockers prevent development progress. This skill provides a structured decision tree, escalation paths, and parallel development workarounds to minimize idle time while infrastructure issues are resolved.

**ROI:** 2-4 hour blocker resolution (vs 24+ hours manual escalation)

---

## When to Use

Trigger this skill when:
- Development blocked by infrastructure failure (VPS, network, tooling)
- Technical debt causing repeated failures (NuGet timeout, tmux session hang)
- Multiple terminals experiencing the same issue
- Escalation needed (L1 → L2 → L3 → L4)

**DO NOT use** for:
- Application bugs (domain logic, UI, API)
- Code review feedback (use review workflow)
- Feature design questions (use architect consultation)

---

## Prerequisites

**Tools:**
- SSH access to VPS (`ssh gabor@zold.joinerytech.hu`)
- Datahaven Dashboard access (`https://datahaven.joinerytech.hu`)
- MCP Knowledge Service logs (`/opt/spaceos/logs/`)
- Terminal status API (`curl localhost:3456/api/sessions/all`)

**Team:**
- L1: Conductor (automated detection)
- L2: Root (diagnosis + decision)
- L3: VPS Operator (manual intervention)
- L4: External Vendor (ISP, hosting provider)

---

## Step-by-Step

### Step 1: Blocker Detection (Automated - L1)

**Conductor detects infrastructure blockers:**
```bash
# Stuck session detection (every 2 minutes)
if [ $idle_time -gt 600 ]; then
  echo "Session stuck: $terminal (idle ${idle_time}s)"
  curl -X POST localhost:3456/api/escalation/infra \
    -d '{"terminal":"$terminal","issue":"stuck_session"}'
fi

# NuGet timeout detection (dotnet restore fails 3×)
if [ $nuget_retry_count -ge 3 ]; then
  curl -X POST localhost:3456/api/escalation/infra \
    -d '{"terminal":"backend","issue":"nuget_timeout"}'
fi
```

**Automatic triage:**
- Stuck session → Auto-restart (Enter nudge)
- NuGet timeout → Escalate to Root
- PostgreSQL connection refused → Escalate to Root
- SSH access denied → Escalate to VPS Operator

### Step 2: Root Diagnosis (L2)

**Decision tree:**

```
Infrastructure Blocker
  ├── NuGet Timeout (Azure DevOps 443)
  │   ├─ VPS → Internet connectivity check
  │   ├─ Firewall rules (iptables -L)
  │   └─ DNS resolution (nslookup pkgs.dev.azure.com)
  │
  ├── PostgreSQL Connection Refused
  │   ├─ Service running? (systemctl status postgresql)
  │   ├─ Port open? (ss -tuln | grep 5432)
  │   └─ pg_hba.conf allow? (cat /etc/postgresql/.../pg_hba.conf)
  │
  ├── tmux Session Hang
  │   ├─ Session exists? (tmux ls)
  │   ├─ Pane responsive? (tmux send-keys -t ... Enter)
  │   └─ Memory leak? (top -p $(pgrep -f "tmux"))
  │
  ├── SSH Access Denied
  │   ├─ Key permissions (chmod 600 ~/.ssh/id_rsa)
  │   ├─ Host in known_hosts? (ssh-keyscan zold.joinerytech.hu)
  │   └─ Firewall blocking? (telnet zold.joinerytech.hu 22)
  │
  └── Disk Full
      ├─ Check usage (df -h)
      ├─ Clean logs (journalctl --vacuum-time=7d)
      └─ Clean Docker (docker system prune -af)
```

**Diagnosis commands:**
```bash
# VPS connectivity
ping -c 3 zold.joinerytech.hu
curl -I https://zold.joinerytech.hu

# Service health
systemctl status postgresql
systemctl status spaceos-nexus-knowledge-service

# Disk space
df -h
du -sh /opt/spaceos/logs/* | sort -h

# Process health
ps aux | grep -E "(postgres|node|tmux)"
top -b -n 1 | head -20
```

### Step 3: Parallel Development Workaround (L2)

**While infra issue is being fixed, unblock development:**

| Blocker | Workaround | Terminal Action |
|---------|-----------|-----------------|
| **NuGet timeout** | Use `.nupkg` cache fallback | Backend: Skip restore, use cached packages |
| **PostgreSQL down** | Use Testcontainers local | Backend: `docker run postgres:16-alpine` |
| **VPS SSH denied** | Work locally + git push | Backend/Frontend: Local dev, sync via Git |
| **tmux hang** | Kill + restart session | Conductor: `tmux kill-session -t <terminal>` |

**Example: NuGet timeout workaround**
```bash
# Backend terminal inbox
---
from: root
to: backend
type: task
priority: high
---

# Workaround: NuGet Timeout

VPS → Azure DevOps connectivity issue detected. Use local package cache:

1. Skip `dotnet restore` for now
2. Use cached `.nupkg` files from `~/.nuget/packages/`
3. Build with `dotnet build --no-restore`

Root is investigating the VPS → Internet connectivity issue.
```

### Step 4: L3 Escalation (VPS Operator)

**When to escalate to VPS Operator:**
- Firewall rule changes needed
- PostgreSQL configuration changes
- Disk cleanup required (>80% usage)
- Service restart with privilege escalation

**Escalation message format:**
```markdown
---
from: root
to: vps-operator
type: escalation
priority: critical
---

# Escalation: NuGet Timeout (VPS → Azure DevOps)

## Issue
Backend terminal cannot reach `pkgs.dev.azure.com:443` for NuGet package restore.

## Diagnosis
- VPS → Internet connectivity: OK (ping 1.1.1.1)
- DNS resolution: FAILED (nslookup pkgs.dev.azure.com → timeout)
- Firewall rules: Unknown (need sudo)

## Requested Action
1. Check iptables: `sudo iptables -L -n | grep 443`
2. Check DNS config: `cat /etc/resolv.conf`
3. Test Azure DevOps connectivity: `curl -I https://pkgs.dev.azure.com`

## Workaround Active
Backend using local package cache (no-restore build).

## Urgency
High — blocking 3 terminals (Backend, Orchestrator, Kernel)
```

### Step 5: L4 Escalation (External Vendor)

**When to escalate to external vendor:**
- ISP routing issue (persistent packet loss)
- Hosting provider maintenance (unannounced downtime)
- DNS provider failure (zold.joinerytech.hu unreachable)

**Timeline:**
- L4 escalation: 4-24 hour response time
- Parallel development: MUST continue with workarounds

---

## Error Handling

### Common Issues

**1. False positive stuck session:**
```bash
# Session idle but waiting for user input (AskUserQuestion)
```

**Fix:** Check terminal inbox for `type: question` messages

**2. NuGet timeout intermittent:**
```bash
# Succeeds 2/3 times, fails 1/3
```

**Fix:** Retry with exponential backoff (1s, 2s, 4s)

**3. PostgreSQL connection pool exhausted:**
```bash
# "too many connections" error
```

**Fix:** Increase `max_connections` in `postgresql.conf`

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Detection → Escalation Time** | <5 min | Conductor auto-triage |
| **L2 Diagnosis Time** | <30 min | Root investigation |
| **Workaround Deployment** | <1 hour | Terminal unblocked |
| **L3 Resolution Time** | <4 hours | VPS Operator intervention |
| **L4 Resolution Time** | <24 hours | External vendor SLA |

---

## Real-World Example

### Case Study 1: NuGet Timeout (2026-06-20)

**Timeline:**
- 14:00: Backend BLOCKED — `dotnet restore` timeout
- 14:05: Conductor auto-escalates to Root
- 14:10: Root diagnoses VPS → Azure DevOps connectivity issue
- 14:15: Workaround deployed (local package cache)
- 14:20: Backend UNBLOCKED — continues with `--no-restore`
- 15:30: VPS Operator identifies firewall rule blocking HTTPS
- 16:00: Firewall rule fixed — full NuGet restore working

**Outcome:**
- Backend idle time: 20 minutes (vs 4+ hours without workaround)
- Root cost: $2.50 (15 min diagnosis + workaround)
- VPS Operator cost: $40 (1 hour investigation + fix)

### Case Study 2: tmux Session Hang (2026-06-22)

**Timeline:**
- 10:00: Architect session hung during review
- 10:02: Conductor detects 10-minute idle time
- 10:03: Auto-restart (Enter nudge) → no response
- 10:05: Escalate to Root
- 10:10: Root diagnoses memory leak (tmux process 4GB RAM)
- 10:15: Workaround: Kill session + restart
- 10:17: Architect session restarted — continues review

**Outcome:**
- Architect idle time: 17 minutes
- Dual-reviewer redundancy saved 0 minutes (Librarian approved in parallel)

---

## Related Skills

- **checkpoint-coordination-workflow:** Multi-team epic coordination with blocker escalation
- **contract-first-development-workflow:** Infrastructure dependencies (VPS, Docker, PostgreSQL)
- **mock-api-parallel-development:** Workaround for Backend API blocker

---

## Maintenance Notes

**When to update this guide:**
- New infrastructure blocker category discovered
- Workaround proven effective (add to table)
- Escalation path changed (VPS Operator contact update)

**Decision tree updates:**
- Add new leaf nodes for new blockers
- Update diagnostic commands for tool changes

---

**Skill Owner:** Librarian
**Created:** 2026-07-04
**Status:** ACTIVE — Use for all infrastructure blockers
