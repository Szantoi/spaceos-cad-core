# Explorer — Daily Codebase Research

> **Start here:** [CUTTING_CONTEXT.md](../context/CUTTING_CONTEXT.md) — Current sprint focus

## 📋 Typical Day Tasks

1. **Codebase discovery** → Check module boundaries & provider interfaces
2. **Pattern analysis** → Document how things work across modules
3. **Onboarding** → Create context docs for new developers
4. **Test coverage** → Identify gaps & improve test strategies

## 🔥 Hot Sprint Areas

- [CUTTING_CONTEXT.md](../context/CUTTING_CONTEXT.md) — Q3 active: nesting, TOP3 assignment
- [NEXUS_CONTEXT.md](../context/NEXUS_CONTEXT.md) — Agent infra: Knowledge Service, Dashboard, Graph
- [EVENT_SOURCING_PATTERNS.md](../patterns/EVENT_SOURCING_PATTERNS.md) — Audit & idempotency patterns

## 🌡️ Reference

- [DOTNET_8_CLEAN_ARCHITECTURE_2026.md](../architecture/DOTNET_8_CLEAN_ARCHITECTURE_2026.md) — 4-layer structure
- [MODULE_BOUNDARIES.md](../architecture/MODULE_BOUNDARIES.md) — Provider interfaces & contracts
- [TESTING_STRATEGIES.md](../patterns/TESTING_STRATEGIES.md) — Coverage targets & patterns

## 🔍 Keresés

```bash
# Find all providers/interfaces
find /opt/spaceos -name "*Provider*.cs" -o -name "*Contract*.cs" | head -20

# Grep pattern search
curl "http://localhost:3456/api/knowledge/search?q=aggregate+root+event+sourcing"
```
