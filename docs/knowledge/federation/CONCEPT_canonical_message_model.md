---
name: CONCEPT — canonical message model
description: A federáció/agent-management egyetlen kanonikus üzenet-fogalma — típus, státusz, mezők — a három jelenlegi szótár (task-message-box, messageRegistry, central Postgres) egyesítésével
status: PROPOSAL (VPS vocab-egyeztetésre vár)
created: 2026-07-12
from: cabinet
---

# Kanonikus üzenet-fogalom (fogalmak rendberakása, nulla adósság)

> Gábor (2026-07-12): "A fogalmakat is rendbe kell rakni. Nem lehet adósság."
> Cél: EGY üzenet-fogalom mindenhol — vége a párhuzamos szótáraknak és a driftnek.

## A probléma: három szótár, ráadásul összekevert dimenziók

| Rendszer | Használat | Státusz-szótár |
|----------|-----------|----------------|
| `messageRegistry.ts` | **10 fogyasztó** (inboxWatcher, összes watch*, taskEscalation, terminalStatus, sessionStarter) — a live pipeline | 12 NAGYBETŰS: UNREAD/INJECTED/READ/PROCESSING/PROCESSED/ARCHIVED/COMPLETED/DONE/SKIPPED/DELEGATED/PENDING/SUPERSEDED |
| `task-message-box` | 2 fogyasztó (federation API, mcp-tools) — újabb, tisztább | 6 kisbetűs: unread/read/in_progress/completed/blocked/archived |
| central Postgres (datahaven) | sziget-közi transport | pending/delivered/ack/failed/expired |

**Fogalmi zavar:** a `done`/`blocked` MINDKÉT rendszerben TÍPUS is — pedig a "kész" és
"elakadt" az a STÁTUSZ (életciklus), nem a purpose. Egy "done report" valójában egy
`response` típusú üzenet `completed` státusszal.

## A kanonikus modell: két FÜGGETLEN dimenzió

### 1. `type` = az üzenet CÉLJA (miért küldték) — 4 érték
`task` | `question` | `response` | `info`
- Megszűnik a `done`/`blocked` mint típus (az státusz). A régi típusok leképezése:
  done/progress/notification/acknowledgment/answer/response → `response`;
  escalation/freeform/message/test → `info` (vagy `task`, ha cselekvést kér).

### 2. `status` = az üzenet ÉLETCIKLUSA — 6 érték (a task-message-box kanonikus)
`unread` → `read` → `in_progress` → `completed` | `blocked` → `archived`

### Leképezések (nulla adósság — minden régi érték egyértelműen megy)
**messageRegistry (12) → kanonikus (6):**
`UNREAD`/`INJECTED`/`PENDING`→`unread`; `READ`→`read`; `PROCESSING`/`DELEGATED`→`in_progress`;
`PROCESSED`/`COMPLETED`/`DONE`→`completed`; `ARCHIVED`/`SKIPPED`/`SUPERSEDED`→`archived`.

**central Postgres ↔ kanonikus (VPS-sel már egyeztetve):**
`pending`↔`unread`; `delivered`↔`read`; `ack`↔`completed`. (`failed`/`expired` = központi-transport-specifikus, nem lokális.)

## Megőrzendő funkciók (messageRegistry-ből a task-message-box-ba)
A konszolidáció NEM veszíthet képességet:
- **content-hash verifikáció** (`stampFileWithHash`/`verifyMessageHash`/`verifyAllMessages`) — integritás.
- **status-history audit** (`message_status_history`, `getStatusHistory`) — ki, mikor, miről mire.
- **filesystem-sync** (`syncWithFilesystem`) — a .md ↔ DB egyeztetés (kivezetés alatt, de amíg kell, megmarad).
- terminal-model tracking, search.

## Migrációs terv (inkrementális, VPS-koordinációval)
1. **Vocab-egyeztetés VPS-sel** (ez a doksi) — a 4 típus + 6 státusz + leképezések elfogadása.
2. A task-message-box kiegészítése a megőrzendő funkciókkal (hash, status_history) — additív, nem tör.
3. A 10 messageRegistry-fogyasztó ÁTKÖTÉSE a task-message-box-ra, egyenként, teszttel.
4. A régi 12/14-es enumok leképezése futásidőben (kompat-réteg az átállás alatt), majd törlés.
5. messageRegistry nyugdíjazása, ha 0 fogyasztó maradt.

## Miért így (a "miértek" — organikus fejlesztés tanulsága)
Két rendszer nőtt párhuzamosan (a régi file-orientált messageRegistry + az újabb DB-first
task-message-box). A drift nem véletlen: a fogalmak sosem voltak egységesítve. Ez a doksi
a fogalmi alap; a kód ezt követi. Egy szótár, két dimenzió, tiszta leképezés → nincs több drift.

_Cabinet root — proposal, 2026-07-12. Vocab-egyeztetésre a VPS-sel a messageRegistry↔task-message-box konszolidációhoz._
