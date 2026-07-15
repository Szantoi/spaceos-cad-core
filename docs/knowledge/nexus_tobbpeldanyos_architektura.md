# Nexus többpéldányos architektúra — kód vs. konfig-példány

> Miért létezik ez a doksi: gyakori kérdés, hogy mi történik, ha ugyanaz a
> nexus-core (tudásbázis + agent-management) fut lokálban ÉS a VPS-en, és hogyan
> viszonyul egymáshoz a két „csapat". Ez rögzíti a mentálmodellt és a szabályokat.

## Kulcs-elv: a KÓD közös, a PÉLDÁNY konfig-vezérelt

A `nexus-core` a **motor** (egy repó = egy igazság). Egy futó „csapat"/**sziget** =
**motor + egy `.env` config-profil**. A `.env` adja a példány identitását és állapotát:

| `.env` mező | Mit dönt el |
|---|---|
| `ISLAND_ID` | a csapat identitása |
| `PORT` | melyik porton hallgat |
| `COLLECTION_NAME` | melyik RAG-tudássiló (ChromaDB collection) |
| `DATA_DIR` | a csapat memóriája — task/üzenet/epic SQLite DB-k |
| `KNOWLEDGE_BASE_PATH` | mit indexel |
| `TERMINALS_PATH` | melyik ügynök-flotta (mailbox-fa) |
| `MCP_AUTH_TOKEN` | a példány saját, rotált titka |

**Ugyanaz a motor, más agy.** A kód azonos (ha egy commiton állnak), de az adat és
identitás külön siló. Két példány csak akkor „ugyanaz", ha SZÁNDÉKOSAN federálod —
az sosem automatikus.

## Következmények

- **Local vs. VPS:** nem azonos csapatok — más tudás, más task-sor, más token.
  Párhuzamosan, izoláltan dolgoznak; a koordináció explicit **federációs üzenet**
  (`/api/federation/*`) vagy a VPS központi relay.
- **Config/token eltér local és VPS közt — és MUSZÁJ is.** A `.env` per-deployment,
  gitignore-olt, nincs a repóban. A token példány-szintű titok.
- **VS Code Remote-SSH:** a Remote-ablakban MINDEN a VPS-en fut; a `localhost:PORT`
  a VPS localhostja (forwardolva), nem a tiéd. Remote-ablak = VPS-fájl; lokális
  ablak = lokális fájl; a kettő csak git push/pull-lal keresztezi egymást.

## N csapat egy gépen — ütközés-szabályok

Több csapat futhat egy gépen (élő példa: CAD 13457 + Doorstar 13458 ugyanabból a
distből). Az ütközésmentességhez KÜLÖN kell: `PORT`, `DATA_DIR`, `COLLECTION_NAME`,
`MCP_AUTH_TOKEN`; és külön `TERMINALS_PATH`, ha külön ügynök-flotta kell.

**Ugyanarra a projektre** két csapat mutathat: közös `KNOWLEDGE_BASE_PATH` (közös
kód/tudás), de **külön task-DB**. Így megy VPS-tükör-reprodukció, A/B, red/blue-team.

> ⚠️ Két processz NE ossza ugyanazt a `DATA_DIR`-t (SQLite write-lock ütközés).
> Közös tudás-collection (read-mostly RAG) rendben; közös task-DB (write-heavy)
> két processz közt nem. „Ugyanaz a projekt" = közös kód/tudás, saját task-DB.

## Higiénia

A runtime DB-k (`data/*.db`) SOHA nem utaznak git-en — minden példány a sajátját
tartja. A `.gitignore` ezt deklarálja; a pre-rule-ból ottmaradt trackelt DB-ket
untrackeltük (nexus-core, 2026-07-13).
