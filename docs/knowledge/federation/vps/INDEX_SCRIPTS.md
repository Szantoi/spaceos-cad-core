# SpaceOS Script Katalógus

> Újrafelhasználható szkriptek listája.
> Lokáció: `/opt/spaceos/scripts/`
> Alapelv: "Ha 2× csinálod, szkripteld!"

---

## Script Használat

```bash
# Minden script futtatható
/opt/spaceos/scripts/<category>/<script>.sh [args]

# Vagy working directory-ból
cd /opt/spaceos
./scripts/health/check-all.sh
```

---

## Kategóriák

### 📬 mailbox/ — Inbox/Outbox Kezelés

| Script | Leírás | Használat |
|--------|--------|-----------|
| `list-unread.sh` | UNREAD inbox listázás | `./list-unread.sh [terminal\|all]` |
| `send-inbox.sh` | Inbox üzenet küldés | `./send-inbox.sh <terminal> <type> <priority> <model> <title> [content_file]` |
| `mark-read.sh` | READ jelölés | `./mark-read.sh <terminal> <message_file>` |

### 🖥️ session/ — tmux Session Kezelés

| Script | Leírás | Használat |
|--------|--------|-----------|
| `start-terminal.sh` | Session indítás | `./start-terminal.sh <terminal> [model]` |
| `inject-prompt.sh` | Prompt küldés | `./inject-prompt.sh <terminal> <prompt>` |
| `list-sessions.sh` | Session lista | `./list-sessions.sh` |
| `capture-output.sh` | Output capture | `./capture-output.sh <terminal> [lines]` |

### 🏥 health/ — Health Check & Monitoring

| Script | Leírás | Használat |
|--------|--------|-----------|
| `check-all.sh` | Teljes rendszer check | `./check-all.sh` |
| `check-services.sh` | Service health | `./check-services.sh` |
| `check-workers.sh` | ADR-049 worker státusz | `./check-workers.sh [terminal]` |
| `check-sessions.sh` | Stuck session detection | `./check-sessions.sh` |

### 🏗️ build/ — Build & Test

*(Még nem implementált — TODO)*

| Script | Leírás | Használat |
|--------|--------|-----------|
| `build-nexus.sh` | Knowledge Service build | `./build-nexus.sh` |
| `test-all.sh` | Összes teszt futtatás | `./test-all.sh` |
| `lint.sh` | Lint ellenőrzés | `./lint.sh` |

### 🔧 tools/ — Utility Szkriptek

*(Még nem implementált — TODO)*

| Script | Leírás | Használat |
|--------|--------|-----------|
| `json-parse.sh` | JSON feldolgozás | `./json-parse.sh <file> <path>` |
| `yaml-parse.sh` | YAML feldolgozás | `./yaml-parse.sh <file> <path>` |
| `date-format.sh` | Dátum formázás | `./date-format.sh [format]` |

---

## Meglévő Rendszer Szkriptek

### Pipeline & Automation

| Script | Leírás |
|--------|--------|
| `nightwatch.sh` | Fő cron szkript (*/2 perc) |
| `watch-priority.sh` | Priority session (root, conductor) |
| `watch-done.sh` | DONE detection |
| `watch-stuck.sh` | Stuck session nudge |
| `reviewer.sh` | Dual Haiku review |
| `pipeline.sh` | DONE feldolgozás |
| `cold-shutdown.sh` | Cold mode leállítás |

### Planning Pipeline

| Script | Leírás |
|--------|--------|
| `plan-scan.sh` | Idea scan (*/30 perc) |
| `plan-select.sh` | Top 3 szelekció |
| `plan-debate.sh` | A/B debate + konsenzus |

---

## Token Megtakarítás

| Művelet | LLM költség | Script költség | Megtakarítás |
|---------|-------------|----------------|--------------|
| Inbox list | ~500 token | 0 | 100% |
| Health check | ~1000 token | 0 | 100% |
| Session start | ~800 token | 0 | 100% |
| JSON parse | ~300 token | 0 | 100% |

**Napi 50 művelet = ~130K token = ~$2-5 megtakarítás**

---

## Script Fejlesztési Szabályok

1. **Fájlnév:** `<action>-<target>.sh`
2. **Header:** leírás, használat, példa
3. **Strict mode:** `set -euo pipefail`
4. **Output:** strukturált, pipe-olható
5. **Regisztráció:** frissítsd ezt a katalógust

---

## Verzió

- **Utolsó frissítés:** 2026-06-30
- **Szkriptek száma:** 11 (core) + 8 (pipeline)
- **Kategóriák:** 5 (3 implementált)
