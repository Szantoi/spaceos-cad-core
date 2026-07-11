# SpaceOS Skill Katalógus

> Minden elérhető skill listája, kategorizálva.
> Lokáció: `~/.claude/skills/<name>/SKILL.md`

---

## Skill Használat

```bash
# Skill megtekintése
cat ~/.claude/skills/<name>/SKILL.md

# Skill listázás
ls ~/.claude/skills/
```

---

## Kategóriák

### 🔧 Session & Terminal Management

| Skill | Leírás | Terminálok |
|-------|--------|------------|
| **tmux-session-management** | Session create/kill/inject/capture | root, conductor, backend |
| **cold-start-session** | Cold mode session lifecycle | minden terminál |

### 📬 Mailbox & Communication

| Skill | Leírás | Terminálok |
|-------|--------|------------|
| **inbox-outbox-format** | Üzenet formátum, frontmatter, ID | minden terminál |
| **inbox-outbox-management** | Lifecycle: UNREAD→READ→DONE | minden terminál |

### 🔌 MCP & Integration

| Skill | Leírás | Terminálok |
|-------|--------|------------|
| **mcp-tool-patterns** | MCP JSON-RPC tool hívások | root, conductor, backend |
| **parallel-workers** | ADR-049 worker spawn, költség | conductor, backend |

### ⚙️ Automation & Pipeline

| Skill | Leírás | Terminálok |
|-------|--------|------------|
| **cron-automation** | Nightwatch, planning pipeline | conductor, root |
| **service-management** | Knowledge Service lifecycle | root, backend |
| **script-first-development** | Script vs LLM döntési fa | minden terminál |

### 📊 Monitoring & Review

| Skill | Leírás | Terminálok |
|-------|--------|------------|
| **spaceos-monitor** | Health check, eszkaláció | monitor |
| **terminal-review-workflow** | DONE review Architect+Librarian | architect, librarian |
| **retrospective** | Session retrospective | minden terminál |

### 🛠️ Utility

| Skill | Leírás | Terminálok |
|-------|--------|------------|
| **skill-factory** | Új skill létrehozás | librarian, explorer |
| **skill-management** | Skill list/patch/delete | minden terminál |
| **handoff** | HANDOFF.md context transfer | minden terminál |

### 🤖 Fleet & Agent

| Skill | Leírás | Terminálok |
|-------|--------|------------|
| **ai-fleet-project-execution** | Multi-agent orchestration | root, conductor |
| **fleet-helper** | Python helpers (dashboard API) | backend |
| **telegram-bot-registration** | Telegram bot setup | root |

### 🔀 Git & External

| Skill | Leírás | Terminálok |
|-------|--------|------------|
| **github-pr-rebase-merge** | PR merge stack management | backend, frontend |
| **channel-plugin-duplicate-socket** | Slack/Telegram socket fix | root |

---

## Skill Fejlesztés

Új skill készítéséhez használd a `skill-factory` skill-t, vagy:

```bash
mkdir -p ~/.claude/skills/<new-skill>
cat > ~/.claude/skills/<new-skill>/SKILL.md << 'EOF'
# <Skill Name>

> Rövid leírás

## Mikor használd?
- ...

## Előfeltételek
- ...

## Lépések
1. ...
2. ...

## Gyakori hibák
- ...

## Példák
```bash
# ...
```
EOF
```

---

## Verzió

- **Utolsó frissítés:** 2026-06-30
- **Skill-ek száma:** 22
- **Kategóriák:** 8
