# Knowledge Islands

Both local knowledge islands (CAD-general and Doorstar) run from **one shared
`nexus-core` checkout** — no forked source copies. An island is just an
environment set pointing the shared `dist/server.js` at island-specific paths.

```
nexus-core (Szantoi/nexus-core, release/vps)
  └── src/nexus-core/knowledge-service/dist/server.js   ← the ONE binary both islands run
Cabinet_bilder_scripts/islands/
  ├── cad/       .env · agents.yaml · data/   (gitignored: secrets + state)
  ├── doorstar/  .env · agents.yaml · data/   (gitignored)
  ├── start-cad.ps1
  └── start-doorstar.ps1
```

## What differs per island (env only)

| Var | CAD | Doorstar |
|-----|-----|----------|
| `PORT` | 13457 | 13458 |
| `COLLECTION_NAME` | `cabinetbilder-cad` | `cabinetbilder-doorstar` |
| `KNOWLEDGE_BASE_PATH` | `docs/knowledge` | `docs/knowledge-doorstar` |
| `DATA_DIR` | `islands/cad/data` | `islands/doorstar/data` |
| `AGENTS_CONFIG_PATH` | `islands/cad/agents.yaml` | `islands/doorstar/agents.yaml` |
| `MCP_AUTH_TOKEN` | CAD master token | Doorstar master token |

`TERMINALS_PATH` and `SPACEOS_ROOT` are shared (both islands act over the same
9-terminal fleet workspace).

## Run

```powershell
# once, after pulling a new nexus-core release:
cd C:/Users/szant/Documents/Development/nexus-core/src/nexus-core/knowledge-service
git fetch origin --tags && git checkout <tag>   # e.g. v1.0.0
npm install && npm run build

# then, per island (separate terminals):
cd C:/Users/szant/Documents/Development/Cabinet_bilder_scripts/islands
powershell -File start-cad.ps1
powershell -File start-doorstar.ps1
```

Verify: `curl http://localhost:13457/health` and `:13458/health` — each reports
its own `collectionName` and `knowledgePath`.

## First-time setup

Copy `cad/.env.example` → `cad/.env` (same for `doorstar/`), set `MCP_AUTH_TOKEN`,
and place a per-island `agents.yaml` (token→agent map) beside it. These files are
gitignored — they never go in version control.

## Why this replaces the old forks

Previously `knowledge-service-0.0.01` (CAD) and `knowledge-service-doorstar` were
**full forked copies** of the knowledge-service, each with its own git history —
duplicated code that drifted from upstream and had to be patched twice. Now the
code lives in exactly one place (`nexus-core`); the islands carry only config.
