# SpaceOS Nexus — Biztonsági és Architektúra Audit

**Dátum:** 2026-06-20
**Elemzők:** DevOps Expert, Security Expert, Architect, Devil's Advocate
**Státusz:** PRODUCTION NOT READY

---

## Vélemények Ütköztetése

### Közös Egyetértés (Mind a 4 agent)

| Probléma | DevOps | Security | Architect | Devil's Advocate |
|----------|--------|----------|-----------|------------------|
| **Rate limiting hiányzik** | ✅ HIGH | ✅ HIGH | ✅ HIGH | - |
| **Health check endpoint hiányzik** | ✅ CRITICAL | ✅ MEDIUM | ✅ MEDIUM | ✅ MEDIUM |
| **Input validáció hiányzik** | ✅ HIGH | ✅ HIGH | ✅ HIGH | - |
| **Graceful shutdown nincs** | ✅ MEDIUM | - | ✅ CRITICAL | - |
| **Logging strukturálatlan** | ✅ HIGH | ✅ LOW | ✅ LOW | - |

### Ellentmondások és Viták

#### 1. Command Injection — Mennyire CRITICAL?

**Security Expert:** CRITICAL
> "Shell command injection via exec() — azonnali exploitálható"

**DevOps Expert:** NEM EMLÍTI
> Nem vizsgálta a sessionStarter.ts-t

**Devil's Advocate:**
> "A sessionStarter.ts-ben a terminal whitelist validálja az inputot (TERMINALS objektum). A tényleges injection csak akkor lehetséges, ha a terminal név nem a whitelist-ből jön."

**Verdikt:** HIGH (nem CRITICAL), mert a TERMINALS whitelist véd, DE a path traversal az inbox fájlnál CRITICAL.

---

#### 2. Duplikált Logika (Bash vs TypeScript) — Probléma vagy Feature?

**Devil's Advocate:** CRITICAL ARCHITECTURAL FLAW
> "Két párhuzamos watcher (bash nightwatch.sh cron, TypeScript chokidar real-time) — ez koordinációs anti-pattern. Nem a gap-eket kell foltozni, hanem konszolidálni kellene."

**DevOps Expert:** NEM PROBLÉMA
> "A nightwatch.sh és a TypeScript service különböző felelősségeket viselnek"

**Architect:** MEDIUM
> "Coupling probléma, de működik"

**Verdikt:** A Devil's Advocate-nak IGAZA VAN. Ez architekturális adósság. Hosszú távon:
- Vagy a bash megy el teljesen
- Vagy a TypeScript lesz az egyetlen orchestrátor

---

#### 3. ChromaDB Backup — Mennyire sürgős?

**DevOps Expert:** CRITICAL
> "Összes RAG tudás elveszhet ha a szerver crashel"

**Architect:** NEM EMLÍTI
> A memória-kezelést vizsgálta, nem a perzisztenciát

**Realitás Check:**
- A knowledge base rebuildelése `npm run ingest`-tel ~5 perc
- A source truth a `docs/knowledge/**/*.md` fájlokban van
- ChromaDB backup NICE TO HAVE, nem CRITICAL

**Verdikt:** MEDIUM — a rebuild lehetséges, de backup script kellene a gyors recovery-hez.

---

#### 4. Memory Leak — Hol a tényleges probléma?

**Architect:** CRITICAL
> "Embedding Provider Singleton Memory Leak — ONNX modell ~200-500MB"

**DevOps Expert:** MEDIUM
> "Nincs memory monitoring"

**Realitás Check:**
A jelenlegi Knowledge Service:
- Voyage AI-t használ (távoli API), nem lokális ONNX-et
- A `localProvider` csak fallback, alapból nem aktív
- A tényleges memory leak kockázat: HTTP connection pooling, ChromaDB client

**Verdikt:** MEDIUM — az Architect részben elavult kódot elemzett. A Voyage AI backend esetén nincs ONNX memory leak.

---

### Prioritási Mátrix — Konszolidált

| Prioritás | Probléma | Forrás | Effort | Deadline |
|-----------|----------|--------|--------|----------|
| **P0** | Path traversal (inbox fájl olvasás) | Security | 1h | AZONNAL |
| **P0** | .env 644 permission | Security | 5min | AZONNAL |
| **P0** | Race condition: session creation | Devil's Adv | 2h | 24h |
| **P1** | Rate limiting | Mind | 1h | 3 nap |
| **P1** | Input validation (Zod) | Security, Arch | 2h | 3 nap |
| **P1** | Health check endpoint | Mind | 30min | 24h |
| **P1** | Error message information leak | Security | 1h | 3 nap |
| **P2** | Graceful shutdown | DevOps, Arch | 1h | 1 hét |
| **P2** | Log rotation | DevOps | 30min | 1 hét |
| **P2** | Retry logic (Voyage API) | Arch, D.Adv | 2h | 1 hét |
| **P2** | Startup scan for existing UNREAD | Devil's Adv | 1h | 1 hét |
| **P3** | Bash/TS konszlolidáció | Devil's Adv | 1 nap | 2 hét |
| **P3** | ChromaDB backup script | DevOps | 1h | 2 hét |
| **P3** | Prometheus metrics | DevOps | 2h | 1 hónap |
| **P4** | Unit tesztek | Architect | 1 hét | 1 hónap |

---

## Stratégiai Döntések

### 1. Bash vs TypeScript — Mi legyen a jövő?

**Opciók:**

| Opció | Előny | Hátrány |
|-------|-------|---------|
| **A) Minden TypeScript** | Egy nyelv, egy runtime, tesztelhető | Nagyobb refaktor, cron szkriptek átírása |
| **B) Minden Bash** | Egyszerű, nincs npm dependency | Nem tesztelhető, nehéz komplex logika |
| **C) Hibrid (jelenlegi)** | Működik | Duplikáció, koordinációs problémák |

**Javaslat:** Fokozatos migráció TypeScript-re (Opció A). A nightwatch.sh marad fallback-nek, de az új logika mind TS.

---

### 2. Event-Driven vs Polling — Mi legyen a master?

**Jelenlegi állapot:**
- Chokidar (event-driven) — <1 sec reakcióidő
- Cron nightwatch (polling) — 2 perc

**Javaslat:**
- Chokidar legyen a MASTER
- Nightwatch.sh legyen FALLBACK (ha a TS service crashel)
- Explicit startup scan implementálása a TS-ben

---

### 3. Single Point of Failure — ChromaDB

**Jelenlegi:** ChromaDB Docker, nincs HA

**Opciók:**
| Opció | Komplexitás | Költség |
|-------|-------------|---------|
| Napi backup + gyors rebuild | Alacsony | $0 |
| ChromaDB replica | Magas | $$ |
| Managed vector DB (Pinecone) | Közepes | $$$ |

**Javaslat:** Napi backup script elegendő. A knowledge base 5 perc alatt rebuildelehető.

---

## Akcióterv

### Ma (P0)
```bash
# 1. .env permission fix
chmod 600 /opt/spaceos/spaceos-nexus/knowledge-service/.env

# 2. Path traversal fix — sessionStarter.ts módosítás
# validateInboxPath() implementálása
```

### Holnap (P1)
- Rate limiting middleware hozzáadása
- Health check endpoint: `/health`, `/ready`
- Input validation Zod-dal

### Jövő hét (P2)
- Graceful shutdown SIGTERM handler
- Log rotation konfiguráció
- Retry logic Voyage API hívásokhoz

### 2 hét múlva (P3)
- Startup scan implementálása
- ChromaDB backup script + cron

---

## Megjegyzések a Sub-Agent Használatról

A felhasználó jelezte, hogy a terminálok is használhatnák gyakrabban a sub-agenteket feladataik orchestrálására. Ez feljegyezve a CLAUDE.md-be kerül mint best practice.

**Előnyök:**
- Párhuzamos elemzés (4 szempont egyszerre)
- Különböző nézőpontok ütköztetése
- Gyorsabb átfogó kép

**Javaslat termináloknak:**
- Komplex feladatoknál indítsanak Explore agenteket
- Code review-nál Security + Architect agent párhuzamosan
- Planning-nél Devil's Advocate challenge
