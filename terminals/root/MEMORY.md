# Root Terminal Session Memory (LOKÁL — Cabinet_bilder_scripts)

> Ez a LOKÁLIS root terminál memóriája. A VPS-es root memória: `spaceos_VPS/terminals/root/MEMORY.md`.
> Séma: a VPS-es MEMORY.md konvencióit követi (session-szekciók, táblázatok, Lessons, TODO).

---

## 📋 SESSION 2026-07-12 — Nexus konszolidáció: egy repó = egy igazság + biztonsági incidens

**Status:** ✅ Lokál nexus-fejlesztés feloldva · ✅ CAD+Doorstar egy közös checkout-ból fut · ⚠️ nexus-core token-szivárgás rotálásra vár (VPS) · ⏳ #15 Cabinet-motor duplikáció válaszra vár

### 🔑 KÖZPONTOSÍTÁS: nexus-core = a knowledge-service egyetlen forrása
- **Repóstruktúra tisztázva:** `Szantoi/nexus-core` (VPS: `/opt/nexus`) = knowledge-service FŐREPÓ (`src/nexus-core/knowledge-service/`). `Szantoi/spaceos-core` (VPS: `/opt/spaceos`) = KOORDINÁCIÓ only (root/conductor terminálok), a benne lévő `spaceos-nexus/knowledge-service/` üres (csak .gitignore).
- **Release-modell:** `release/vps` branch + `v1.x.x` tag a nexus-core-ban. Sync workflow: `git fetch origin --tags` → `git checkout <tag>` → `npm install && npm run build` → smoke test `/health`.
- **Lokál klón:** `Development/nexus-core` (release/vps branch). Az `upstream` remote is beállítva (github nexus-core).

### 6 hardening-fix commitolva+pusholva a nexus-core release/vps-re (98c4019, eb68d83, 8b370f3)
1. `COLLECTION_NAME` env-driven — `vectorStore.ts` ÉS `hybridSearch.ts` (utóbbi plusz találat, második hardcode volt).
2. `/health` javítás: `KNOWLEDGE_BASE_PATH`-ot olvas (nem a nemlétező `KNOWLEDGE_PATH`-ot), + `collectionName` a válaszban.
3. `TERMINALS_ROOT` env-driven (`TERMINALS_PATH`).
4. `bin/stdio-bridge.js` hardcode fallback token ELTÁVOLÍTVA (kötelező `MCP_AUTH_TOKEN`).
5. 23 trackelt SQLite/state fájl kivéve a git indexből (`.gitignore` csak új commitot állít meg, a régit `git rm --cached` kell).
6. **Rate-limit bug:** `indexer.ts` minden fájl után 40mp Voyage-delay-t alkalmazott FELTÉTEL NÉLKÜL, helyi ingyenes embeddingnél is → 150 fájl ~100 perc helyett ~45mp. Most csak `useVoyage()` esetén vár.
7. Multi-island izoláció: `DATA_DIR`/`AGENTS_CONFIG_PATH` env-vezérelt, a `*_DATA_DIR` vál-k `DATA_DIR`-re esnek vissza — egyetlen `DATA_DIR` izolál egy instance-t.

### ✅ CAD+Doorstar szigetek → egy közös nexus-core checkout (a 2 fork-repó kiváltása)
- Új harness: `Cabinet_bilder_scripts/islands/{cad,doorstar}/` — mindkét sziget a KÖZÖS `nexus-core/.../dist/server.js`-t futtatja, csak env különbözik (PORT, COLLECTION_NAME, KNOWLEDGE_BASE_PATH, DATA_DIR, AGENTS_CONFIG_PATH, MCP_AUTH_TOKEN).
- Élőben verifikálva: mindkét sziget egyszerre fut (CAD 13457/cabinetbilder-cad/3912 dok, Doorstar 13458/cabinetbilder-doorstar/87 dok), külön `data/`-ba ír, nulla ütközés.
- Git-trackelt: start scriptek, `.env.example`-ök, README. Gitignore-olt: `islands/*/.env`, `islands/*/agents.yaml`, `islands/*/data/`. Push: spaceos-cad-core `12a76eb`.
- A régi fork-repók (`spaceos-cad-nexus`, `spaceos-cad-doorstar`) mostantól redundánsak — nyugdíjazhatók, ha a modell bevált a napi használatban.

### ⚠️ BIZTONSÁGI INCIDENS (nexus-core, VPS-oldali): teljes token-készlet szivárgás
- A **publikus** `nexus-core` repó `config/agents.yaml`-je tartalmazta a `master_token`-t (ROOT hozzáférés) + mind a 8 SpaceOS terminál tokent + `cabinet-bridge` tokent — mind egyezik a lokál szigetünk élő tokenjeivel. Plusz a `stdio-bridge.js` hardcode fallback token.
- **Konténment (kérdés nélkül, védelmi):** `nexus-core` repó priváttá állítva (`gh repo edit --visibility private`). Verifikálva: private.
- **VPS-nek kell (koordináció, ŐK validálják):** master + összes terminál + cabinet-bridge token rotálása, `config/agents.yaml` gitignore-olása a nexus-core-ban, git history scrub (token a `v1.0.0` tag-ben is). → #16 task, még nyitva.
- Megfigyelés: a Doorstar sziget master tokenje MÁR eltér a CAD-étól (`pul1x5Od...` vs `IoUpLUgr...`), tehát részben már rotált.

### JoineryTech submodule-ok
- A `.gitmodules` most már pusholva (VPS), de: (a) `joinerytech-keycloak-theme` bejegyzés HIÁNYZIK belőle, (b) a submodule URL-ek SSH-sak (`git@github.com`), ehhez GitHub SSH kulcs kell. Generáltam egy dedikált kulcsot (`~/.ssh/github_key`, `~/.ssh/config`-ba bekötve github.com-ra), a public részt Gábornak kézzel kell hozzáadnia a GitHub fiókhoz.

### ⚠️ Párhuzamos-fejlesztés ütközés + helyreállítás (git koordináció)
- A VPS **force-pusholt** a release/vps-re (`19ce42b` token-rotáció) egy RÉGI bázisról → **letörölte mind a 6+1 hardening-fixemet** ÉS visszahozta a hardcode-olt tokent. Majd `615d0fc` — párhuzamosan implementálta ugyanazt a multi-island izolációt egy TISZTÁBB `src/config/paths.ts` modullal (ISLAND_ID, DATA_DIR, COLLECTION_NAME, AGENTS_CONFIG_PATH, per-DB exportok).
- **Helyreállítás (nem pingpong):** eldobtam a saját DATA_DIR-megközelítésemet, `git reset --hard origin/release/vps` a te `paths.ts` bázisodra, majd a hiányzó fixjeimet ÚJRA a `paths.ts`-hez kötve alkalmaztam (13 knowledge-service fájl importál most paths-ból). Normál fast-forward push (`615d0fc..8cfcfdf`), nincs force, nincs elveszett munka.
- **Token még mindig hardcode-olva volt 8 fájlban** (nem csak stdio-bridge): 4 datahaven-web JS route + config.yaml/docs/teszt. A 4 JS-t kiszedtem; a config/docs/teszt string-scrubot (BFG --replace-text) igényel.
- **TANULSÁG:** megosztott repóban SOHA force-push egyeztetés nélkül; párhuzamos munka előtt egyeztetni ki mit csinál. A `paths.ts` (VPS) jobb volt a sajátomnál — érdemes az upstream megoldásra állni, nem ráerőltetni a sajátot.

### Federáció réteg-modell + Windows-ébresztés + egységes fleet vezérlő (2026-07-12 folyt.)
- **Réteg-modell (ADR-proposal):** federáció CSAK sziget-határon át (`to_island != from_island`). Minden más HELYI: a `task-message-box`-ban nincs is `island` mező. A helyi tudás = sziget saját RAG-collection; helyi feladat = terminál↔terminál; helyi ébresztés = epicRouter/watch*/spawn_work_session (MÁR kész a nexusban). Rootok = federáció gateway-ei (két-szintű hub-and-spoke: központ köti a rootokat, root a saját termináljainak hubja). Root→root = federációs üzenet `to_terminal: root` mindkét oldalon. Proposal: `docs/knowledge/federation/ADR-PROPOSAL_scalable_task_message_federation.md`, elküldve VPS-nek (outbox, mert datahaven-web 502).
- **Windows terminál-ébresztés MŰKÖDIK:** a nexus tmux-alapú (`common.ts`: `tmux -S /tmp/spaceos.tmux new-session` + `send-keys`). Ezen a gépen VAN tmux 3.6a + `claude` CLI (Git Bash). Az EGYETLEN Windows-fix: a `tmux new-session -c` workdir POSIX-út legyen (`/c/Users/...`), NEM `C:/...` — utóbbinál a pane shellje el sem indul. Bizonyítva: `send-keys -l "<cmd>"` + `Enter` lefut, ha a cwd POSIX. A szigetet futtató node-nak látnia kell a tmux-ot a PATH-ján.
- **Egységes fleet vezérlő:** `fleet.sh` (Git Bash) — EGY belépési pont a szétszórt szkriptek helyett: `up|down|status|wake <t> [model]|send <t> <text>|sleep <t>|ls`. Kiváltja start-cad/doorstar.ps1 + federation-watcher.sh + kézi tmux. Session-nevek: `cab-<terminál>` a `/tmp/spaceos.tmux` socketen. `wake` VALÓDI claude agentet indít (token + autonóm) → csak explicit jóváhagyással.

### ✅ ADR-066 Federation API — Cabinet referencia-implementáció (folyamatban)
- **Döntések (Gábor):** kanonikus store = `task-message-box`; a Cabinet építi a referenciát, VPS review-zza, a többi sziget átveszi. Foundation-lelet: KÉT üzenet-DB volt inkonzisztens státusz-szótárral (`task-message-box` 6 kisbetűs vs `messageRegistry` 16 NAGYBETŰS, COMPLETED+DONE is) — a task-message-box a kanonikus, a messageRegistry-t rá kell képezni (VPS-sel egyeztetve).
- **Increment 1 (5911e88):** task-message-box séma + migráció nullable `from_island`/`to_island` mezőkkel; `sendFederationMessage()` (content_hash dedup), `getFederationInbox()` (TOKEN-OPTIMÁLT: csak metaadat-sorok, nincs body). Federációs = `to_island` kitöltve; helyi üzenet null-lal marad, sosem szivárog federációs inboxba. 5/5 unit teszt.
- **Increment 2 (ecdd2b2):** `/api/federation/*` HTTP endpointok minden szigeten: `POST send`, `GET inbox` (metaadat), `GET message/:id` (full, on-demand), `POST ack` (unread→read). Bearer-token auth (meglévő MCP auth = sziget-sziget). DB-vezérelt végig, nincs .md parse. 4/4 supertest.
- **2 Windows-bug is javítva közben:** a `.md` render `created_at.split('T')`-je a kettőspontos időbélyeget tette a fájlnévbe (Windows-illegális) → `split(/[ T]/)`; és a `.md` render best-effort lett (DB-first: a DB-írás áll, ha a render hibázik).
- **Élesítés + fleet CLI (kész):** a szigetek újraindítva az új dist-tel (a valódi meglévő DB-k migrálódtak — de kellett egy séma-fix `023fabc`: a `to_island` index a migrációba, nem a SCHEMA-ba, különben meglévő DB-n `no such column: to_island` crash). Élő end-to-end teszt valós tokennel PASS (401/403 auth, send→inbox→message→ack). A `fleet.sh` federációs alparancsai (`fed-send/fed-inbox/fed-msg/fed-ack`) élőben működnek — a root DB-vezérelt API-n federál, nem outbox-fájlon.
- **⚠️ KÉT federációs API-réteg (tisztázandó):** a VPS deployolta a SAJÁT `/api/messages/*`-át (PostgreSQL, datahaven, KÖZPONTI hub, statusz: pending/delivered/ack), NEM az én `/api/federation/*` referenciámat (SQLite task-message-box, SZIGET-szintű, statusz: unread/read/completed). Külön endpoint/store/státusz-szótár. Javaslat (elküldve nexusnak, id 644f79ef): KOMPLEMENTER két réteg — `/api/messages` = központi transport/wire, `/api/federation` = sziget-lokális munka-tár (agentek olvassák); híd = a sziget pollozza a központit és beírja a lokális task-message-boxba. Státusz-szótár összehangolása kell. VPS-válaszra vár.
- **✅ datahaven `/api/messages` ÚJRA ÉL** (502 megoldva). Kétirányú központi csatorna igazolva: bejövő nexus-ping ACK-olva + kimenő reconciliation-üzenet elküldve.
- **Következő kör (VPS-válasz után):** könyvtáros-harvest (döntések→RAG); a Cabinet-oldali híd (poll központi → lokális task-message-box); projekt/goal/mód-4 bevezetés a szigeteknél ([[sziget-projekt-goal-mode4-teendo]] jellegű).

### Lessons
- `.gitignore` NEM távolítja el a már trackelt fájlokat — `git rm --cached` kell hozzá (kétszer is beleütköztem: nexus-core data/, és a merge-kísérlet fájlzár-hibája).
- Windows-on futó node folyamat zárolja a SQLite `data/*.db`-t → `git reset/rebase` "Invalid argument" unlink hibával elakad. Meg kell keresni és leállítani a zároló `dist/server.js` folyamatot (magányos smoke-test példányok is!) a git-művelet előtt.
- MSYS/Git Bash tmux: a `-c` start-dir POSIX-út kell (`/c/...`), Windows-út (`C:/...`) esetén a pane shell némán nem indul → send-keys nem hajtódik végre.
- Új repóból induló smoke test előtt `npm install` kell (friss klón nincs node_modules) — a build-hibák nem a mi kódunk, csak hiányzó függőségek.
- PowerShell start scriptekben kerülni a díszítő Unicode/`...` karaktereket — parser-hibát okozott, egyszerű ASCII-val ment.

---

## 📋 SESSION 2026-07-10 — Governance-csomag INGEST + kanonikus tudás-gyökér átállás

**Status:** ✅ KÉSZ — a VPS governance-tudás a lokál RAG-ban, kereshetően · ⏳ VPS OpenAPI draft még mindig nem érkezett

### Kontextus
- Híd ellenőrizve: **nincs új üzenet 07-08 22:36 (MSG-021) óta** — a Doorstar Production-modul OpenAPI draftja késik (1-2 napot ígértek 07-08-án).
- Lokál sziget nem futott → elindítva (`start-local-island.ps1`, 13457). Lesson: a Xenova-modell betöltés miatt az indulás ~15-30 mp, a health-check várjon.

### 🔑 KANONIKUS TUDÁS-GYÖKÉR ÁTÁLLÁS (strukturális döntés)
- **Felfedezés:** a lokál index eddig a `JoineryTech.AgentScripts/database/knowledge`-ből épült (.env KNOWLEDGE_BASE_PATH; 22 fájl → 450 chunk) — a projekt saját `docs/knowledge` doksijai (doorstar séma, faipari RAG-ok!) NEM voltak indexelve. Az ingest-szkript célmappája ráadásul egy harmadik, nem létező hely volt (`Development\docs\knowledge`).
- **Döntés (VPS-szabvány követése):** kanonikus gyökér = `Cabinet_bilder_scripts/docs/knowledge` (projekt-gyökér, mint a VPS-en a SPACEOS_ROOT/docs/knowledge). `.env` átállítva, `ingest-federation-knowledge.ps1` célmappája javítva (`$projectRoot\docs\knowledge\federation`).
- Az AgentScripts-korpusz NEM veszett el: másolat `docs/knowledge/imported/agentscripts/` alatt (az eredeti a helyén marad).

### Governance-csomag ingest (a 07-08-án letöltött 3 tar.gz feldolgozása)
- sha256 mindhármon újra-ellenőrizve ✅, kicsomagolás staging-be, majd szelektíven:
  - **knowledge-base-full** → `docs/knowledge/federation/vps/<kategória>` (api, architecture, by-role, context, datahaven, debugging, deployment, engineering, graph, market, patterns, security, snippets + INDEX/KNOWLEDGE_BASE/MEMORY_INDEX). Kihagyva: reading-list, synthesis (dátumos pillanatképek), test-reindex.md.
  - **code-design-strategy** → `federation/vps/design/` (md-k) + `federation/vps/joinerytech/` (CRM/HR/Maintenance/QA/**DMS** domain modellek + domain kód). A design HTML/zip assetek NEM kerültek be (nem-md, az inbox/files őrzi).
  - **architect-skills (9 db)** → `.claude/skills/` (adr-decision-template, checkpoint-coordination, contract-first, fsm-aggregate-generator, infrastructure-blocker-resolution, knowledge-pattern-documentation, mock-api-parallel-development, multi-module-delivery-roadmap, review-redundancy-architecture) — a harness be is töltötte őket ✅
  - Provenance: `federation/vps/README.md` (forrás, sha256, dátum, kihagyások, DMS-séma kiemelve).
- **Reindex eredmény: 154 md fájl → 3987 chunk** (korábban 450). Keresés-teszt PASS mindhárom rétegre: saját (doorstar_dokumentacios_sema 0.517), VPS (HR_DOMAIN_MODEL 0.767 FSM-kérdésre), imported (project_documentation_structure 0.526).
- ⚠️ Apró: a /health `knowledgePath: "(default)"`-ot mutat, pedig env-ből jön — kozmetikai bug a health-reporterben, javítandó alkalomadtán.
- A REST search paramétere `q` (NEM `query`) — 400-at ad különben.

## 📋 SESSION 2026-07-10 (13) — Naptári + erőforrás-korlátos ütemezés: több projekt közös szakma-kapacitásra

**Status:** ✅ build 0 error, **73 unit teszt PASS**, smoke PASS (**25 tool**). Terv: feature-calendar-resource-schedule-1.md (Done).

### Amit hozzáadtam (a valós korlátok: munkanap + kapacitás)
- `Production/WorkCalendar.cs` — munkaóra → naptári dátum (kezdő dátum + napi óra + munkanapok); a hétvége kimarad, a kezdet munkanapra igazítva. Determinisztikus (a dátum input, nincs "most").
- `Production/ResourceScheduler.cs` — **erőforrás-korlátos list scheduling TÖBB projektre**: szakmánként N azonos dolgozó; a művelet a legkorábbi időben indul, amikor az elődei készek ÉS van szabad dolgozó. Prioritás CPM-ES szerint (precedencia-helyes). Munkaóra-térben fut, a WorkCalendar naptárra vetíti.
- `skeleton_schedule_projects` tool: több skeletonId + szakma-kapacitás (asztalos/cnc/összeszerelő) + kezdő dátum → projektenkénti/műveletenkénti naptári kezdés-vég, makespan-dátum, **szakma-kihasználtság %**.

### A "mikor tud egy szakma több projekten dolgozni" — megválaszolva
Smoke: 2 szekrény közös 1-1 dolgozóval → makespan 0,307 nap (1 projekt: 0,192). **Összeszerelő 65,4% (szűk keresztmetszet)**, Asztalos 31,6%, CNC 31,1%. A bottleneck látszik; ha az Összeszerelőből 2 lenne, gyorsulna. A 2-projekt makespan < 2× (mert eltérő szakmák átfednek projektek közt) — helyes erőforrás-korlátos viselkedés.

### Tesztek: 73 PASS (63 + 5 WorkCalendar + 5 ResourceScheduler) · MCPHost: 25 tool
A modernizáció 3 dimenziója kész: KÖLTSÉG (mancsóra→kalkuláció), IDŐ (CPM átfutás), KAPACITÁS (több projekt/naptár). Az Excel 02 Folyamatok kapacitás-tervezésének magja modern, tesztelt formában megvan.

### Következő modernizációs lehetőségek
- Naptári ütemezés kiterjesztése: ünnepnapok, dolgozónkénti naptár, szakma-alapú órabérek a kalkulációba.
- Teljes Egység_idő taxonómia + Skeleton front/fiók/tok komponensek.
- Gantt/idővonal webes megjelenítés (a schedule_projects tasks már dátumozott — kész az adat).

## 📋 SESSION 2026-07-10 (12) — Ütemezési DAG (CPM): a 02 Folyamatok modern kiváltása

**Status:** ✅ build 0 error, **63 unit teszt PASS**, smoke PASS (**23 tool**). Terv: feature-production-schedule-dag-1.md (Done).

### Amit hozzáadtam (az IDŐ dimenzió — a mancsóra után az átfutás)
- Operation bővítve FS/SS függőségekkel (`OperationDependency{OnOperationId, Type, LagHours}`, default üres). OperationCatalog korpusz-lánc: Szabás→CNC-furat→Élzárás→Csiszolás→Összeállítás; a Hátlap-szabás párhuzamos ág, az Összeállításnál csatlakozik (mint a woodwork_domain §11 DAG).
- `Production/Scheduler.cs` — **kritikus út módszer (CPM)**: Kahn topo-rendezés (ciklus-detektálással), forward pass (ES/EF, FS ÉS SS + lag), backward pass (LS/LF), slack, kritikus út. A 0-darabszámú (kihagyott) műveletek és a rájuk mutató függőségek kezelve.
- `skeleton_production_schedule` tool: leadTimeHours/Days, criticalPath, műveletenként kezdés/befejezés/tartalék/kritikus.
- Smoke: **átfutás 1,5336 h (0,192 nap)**, kritikus út = SZABAS→CNC_FURAT→ELZARAS→CSISZOLAS→OSSZEALLITAS; a hátlap-szabás párhuzamos → nem kritikus, van tartaléka.

### Kulcs-megkülönböztetés (két dimenzió, két szám)
- **Mancsóra 1,72** (LaborEstimator, humánerővel) = a KÖLTSÉG alapja (bérköltség).
- **Átfutás 1,53 h** (Scheduler, kritikus út) = az IDŐ (kapacitás/határidő). A párhuzamos hátlap nem nyújtja.
- Az időtartam = ProcessHours; a Humánerő KAPACITÁS (crew), nem időtartam-osztó (dokumentálva).

### Tesztek: 63 PASS (57 + 6 SchedulerTests) · MCPHost: 23 tool
A modernizáció második lépcsője kész: az Egység_idő legacy FS/SS függőségei modern CPM-ütemezővé váltak. A 02 Folyamatok kapacitás/átfutás-tervezésének magja megvan.

### Következő modernizációs lépcsők (változatlan)
- Teljes Egység_idő taxonómia (Ajtólap/Tok/Borítás/Falpanel) + Skeleton front/fiók/tok komponensek.
- Szakma-alapú órabérek + naptári ütemezés (munkanap-határok, erőforrás-kapacitás).
- Gyártási Naplók DATA nyers mérés becsatornázása.

## 📋 SESSION 2026-07-10 (11) — Egység_idő MODERNIZÁLÁS: mért folyamatmodell → auto munkaidő a kalkulációba

**Status:** ✅ build 0 error, **57 unit teszt PASS**, smoke PASS (**22 tool**). Tudás: `docs/knowledge/doorstar_egysegido_folyamatmodell.md`.

### Gábor direktívája: a legacy fájlt NE másoljuk — MODERNIZÁLJUK
- A `10 - Adatok/Egység_idő.xlsx` (`Feladat_Egység_idő`, 30 oszlop) **munkanaplóval mért**, de kezdetleges/legacy séma. Az ÉRTÉK (mért egységidők, folyamat→alkatrész kötés, FS/SS függőségek) az arany; a séma nem.
- Kinyerés a valós fájlból (scratchpad/read_xlsx.py): egységidő Excel-nap-törtben (`4.1667e-2`=1 óra → ×24 tiszta órára); Humánerő; szakma (Asztalos/CNC/Összeszerelő/Fóliázó); függőség FS/SS + késleltetés; **alkatrész-illesztés** a `Keresési Oszlop/Feltétel`-lel (pl. Fóliázás csak `[Felület Tipus]="Fóliás"`, Festőnél `="Festett"` +4 nap).

### Modern szelet (megvalósítva)
- `Production/Operation.cs` — tiszta record: Id, Name, Role, **UnitTimeHours (tiszta óra)**, Headcount, Match{Category?,Surface?}, PerCabinet. `OperationCatalog.CarcassOperations` a korpusz-műveletekkel, a mért Doorstar-értékekből órára váltva (Szabás 0,0375h; CNC-furat 0,0953h; Csiszolás 0,0756h; Összeállítás 0,5h/szekrény).
- `Production/LaborEstimator.cs` — BOM → mancsóra (UnitTimeHours × Headcount × illeszkedő darab), műveletenként + **szakmánként**. Az ütemezési DAG (FS/SS) KÜLÖN réteg lesz (átfutás/kapacitás) — most csak a munkaidő-aggregáció.
- `skeleton_labor_estimate` tool + **a cost_calculation auto-labor opciója**: ha `laborHours < 0`, a folyamat-modell mancsóráját használja a bérköltséghez (nem kézi input).
- Smoke: 6 művelet, össz 1,72 mancsóra (Asztalos 0,54 / CNC 0,38 / Összeszerelő 0,80); az auto-labor kalkuláció ezt használja (nettó 28000).

### Tesztek: 57 PASS (53 + 4 LaborEstimatorTests) · MCPHost: 22 tool

### A modernizáció további lépcsői (dokumentálva a tudás-doksiban)
- Teljes Egység_idő taxonómia (Ajtólap/Tok/Borítás/Falpanel) — a Skeleton front/fiók/tok bővítésével.
- Ütemezési DAG (FS/SS + késleltetés) modern változata → átfutás/kapacitás (a 02 Folyamatok.xlsm kiváltása).
- Szakma-alapú órabérek (most 1 órabér); a Gyártási Naplók DATA nyers mérés → egységidő-finomítás.

## 📋 SESSION 2026-07-10 (10) — Power Query kinyerés + projekt-export a VALÓS Doorstar-sémával

**Status:** ✅ build 0 error, **53 unit teszt PASS**, smoke PASS (**20 tool**). Tervek: feature-project-export-1.md (Done). Tudás: `docs/knowledge/doorstar_power_query_semak.md`.

### 🔑 Power Query M-kód kinyerve a valós projekt-.xlsm-ekből (Gábor kérdésére: IGEN, olvasható)
- **Módszer:** a PQ a `customXml/item3.xml` `<DataMashup>` (UTF-16!) base64-ében; a dekódolt blob **MS-QDEFF**: `[4B verzió][4B packageLen][ZIP → Formulas/Section1.m]`. Szkript: `scratchpad/extract_pq.py`.
- **A lánc lelke:** Gyartasmegrendelő 1 PQ-t tartalmaz (`SEGÉD`) → SharePointról húzza a `Data-05.0.01.xlsm` `GYM változok`-ot. **Az auth a SharePoint** (ez a valós identity!). A Kalkulátor a `Gyartasmegrendelse_Data`-ból szűr kategóriánként (Ajtó/Vasalat/Üveg/Anyag/…).
- **🎯 A Szabászat-tábla VALÓS oszlopsémája** (a szabásjegyzék célformátuma): `DSMR;Sorszám;Hosszúság;Szélesség;Darab;Név;Megjegyzés;Tipus;Alkatrész Megnevezése;Anyag;Vastagság;Felület tipus;Szín;Minta`. Anyag Szükséglet: `DSMR;Sorszám;Alkatrész Megnevezése;Anyag;Vastagság;Szélesség;Hosszúság;Darab;Szín`. A **Felület tipus** oszlop = a mi surface-attribútumunk!

### Projekt-export (`skeleton_export_project`)
- `Export/ProjectExporter.cs` (BuildFiles pure + WriteAll IO) + `Catalog/MaterialAttributes.cs` (Szín a color-ból). 5 fájl: **Szabaszat.csv + Mennyisegek.csv a VALÓS PQ-fejlécekkel**, Kalkulacio.csv (11 lépés), Muszaki-Leiras.md, export.json. HU-konvenció: `;` elválasztó, **tizedesvessző** (2,5 nem 2.5), UTF-8 BOM, CRLF. Alkatrész angol→magyar (Side Left→Bal oldal…).
- Smoke-bizonyíték: a kiírt Szabaszat.csv fejléce szó szerint egyezik a PQ-sémával; sorok: `26144;1;740;580;1;Side Left;;Bútorlap;Bal oldal;Sonoma tölgy...;18;laminált;Sonoma;`. A Doorstar PQ-lánc `Table.Combine`/`Excel.CurrentWorkbook` mintája be tudja húzni.
- **A datahaven-first elv gyakorlatban:** a lelet előbb a tudásbázisba (doorstar_power_query_semak.md), aztán az implementáció rá.

### Tesztek: 53 PASS (46 + 7 ProjectExporterTests) · MCPHost: 20 tool
Az export a lánc ELEJÉRE táplál (a séma-doksi következtetése: a CabinetBilder értéke ott van; a Kalkulátor→Folyamatok→Kiíró Excel-lánc viszi tovább). NEM írunk .xlsm-et (makró/plugin a Doorstar oldalon).

### Nyitott
- A `Data-05.0.01.xlsm` `GYM változok` teljes oszloplistája (SharePoint-forrás séma) — kinyerhető.
- Anyag/Szín/Felület értékkészlet leképzés a Doorstar törzsadatra.

## 📋 SESSION 2026-07-10 (9) — Műszaki leírás generálás → a DOORSTAR DOKUMENTUM-NÉGYES TELJES 🎉

**Status:** ✅✅✅ build 0 error, **46 unit teszt PASS**, smoke PASS (**16 tool**). Terv: `plan/feature-technical-description-1.md` (Done).
**A Doorstar/CLAUDE.md 4 kötelező dokumentuma MIND generálható a parametrikus modellből:**
| Dokumentum | Tool |
|---|---|
| Műszaki leírás | `skeleton_technical_description` ✅ ÚJ |
| Anyagszükséglet | `skeleton_material_summary` (+élzáró) ✅ |
| Szabásterv | `skeleton_cutting_plan` (+`cutting_sheet`/`submit`) ✅ |
| Árkalkuláció | `skeleton_cost_calculation` (11 lépés) ✅ |

### Műszaki leírás (tankönyvi séma: faipari_muszaki_dokumentacio_rag.md §2.1 — a RAG-ból!)
- `Docs/TechnicalDescriptionGenerator.cs` (pure static): név + **befoglaló méret korpusz-konvencióval** (Szélesség × Magasság × Mélység) + felhasznált anyagok szerepenként (Korpusz/Hátlap/Élzáró, segédanyag NÉLKÜL — tankönyv) + szerkezeti felépítés (Rebuild-logika szövegesen: átmenő oldalak, fedél/fenék közéjük, hátlap BackOffset beütéssel, élzárás) + felületkezelés (felület-attribútumból: laminált→nem igényel; festett/fóliás→gyártás része).
- **REQ-008 kör bizonyítva:** a create/apply/record_design_intent-tel gyűjtött szándékok (smoke: 3 db) megjelennek a dokumentumban — a tervezés közbeni tudás a dokumentációba folyik.
- Kimenet: strukturált JSON + kész magyar `markdown` mező (webre/Excelbe tovább-renderelhető).

### Tesztek: 46 PASS (39 + 7 TechnicalDescriptionTests) · Smoke: 16 tool PASS
Smoke-bizonyíték: méret "800 × 720 × 560" (a Width=800 módosítás UTÁNI állapot — élő modellből); anyagok Korpusz=LAM18_SONOMA/Hátlap=HDF3_WHITE/Élzáró=ABS2_SONOMA (a set_material hívások eredménye); 3 szándék; markdown 987 kar.

### Következő lehetséges irányok (Gáborral egyeztetendő)
- Excel/xlsm-integráció: a 4 dokumentum betáplálása a valós Doorstar láncba (Gyartasmegrendelő→Kalkulátor→Folyamatok→Kiíró) — a doorstar_dokumentacios_sema.md szerint a lánc ELEJÉN van az értékünk
- VPS OpenAPI draft (Doorstar Production-modul) — még mindig várunk; MSG-ROOT-024 nyitott
- Drilling/CNC réteg (DrillingService már létezik a Core-ban), front/fiók komponensek a Skeletonba

## 📋 SESSION 2026-07-10 (8) — Élzárás + 11 lépéses árkalkuláció + VPS-beküldés outboxba (MIND KÉSZ)

**Status:** ✅✅ build 0 error, **39 unit teszt PASS**, smoke PASS (**15 tool**). Terv: `plan/feature-edging-costing-submit-1.md` (Done). A Doorstar 4 dokumentumból 3 megvan: Anyagszükséglet ✅, Szabásterv ✅, **Árkalkuláció ✅** — hátra: Műszaki leírás.

### (A) Élzárás
- Core: Skeleton `EdgingId` param (default ABS2_WHITE); ComputeBom a korpusz-sorokra (MaterialId==CarcassMaterialId) teszi, hátlapra null.
- Katalógus v2: +ABS2_WHITE/ABS2_SONOMA (ár FM-alapú, BodyJson unit=fm). **CatalogSeeder v2: hiányzó interim kód esetén is seedel** — kellett, mert a meglévő client.db-ben már bent volt a régi 5 anyag!
- `Edging/EdgingCalculator.cs`: élhossz = max(L,W)*db (PoC: 1 hosszú él/panel — a valós élzárás-térkép későbbi finomítás); élzárónkénti fm+költség. material_summary kapott `edging` blokkot (totalEstimatedCost-ba beszámít); set_material target='edging'.

### (B) Árkalkuláció — a tankönyvi 11 lépéses séma
- Forrás: `docs/knowledge/woodwork_domain.md` §10 (fig-2.21) — a RAG-ból! `Costing/CostCalculator.cs`: anyag→bér(óra×órabér)→járulék%→egyéb→közvetlen→általános%→önköltség→nyereség%→kalkulált→nettó (1000-re LEFELÉ)→bruttó (áfa%). **MINDEN % paraméter** (tankönyv: ne drótozzuk be). Lépésenként egész Ft-ra kerekítés — így a **tankönyvi példa SZÁMRA reprodukálódik** (teszt: 157986/189583/218020/276860 ✓).
- `skeleton_cost_calculation` tool: anyagköltség automatikus (lap+élzáró a BOM-ból), laborHours kötelező input, defaults: órabér 5000, szocho 13, rezsi 20, nyereség 15, áfa 27.

### (C) VPS-beküldés — outbox-minta
- A HttpSpaceOsClient még stub (Success HTTP nélkül!) → NEM közvetlen hívás, hanem **EnqueueOutboxAsync(OutboxOperation.SubmitCuttingSheet)**: a payload tartósan a lokál SQLite outboxban (Pending), az éles HTTP-kliens + OutboxWorker élesedésekor magától kimegy. `skeleton_submit_cutting_sheet` tool: outboxEntryId + outboxPending + payloadSha256. A payload-builder kiemelve (cutting_sheet és submit közös, tuple sha256-tal).

### Tesztek: 39 PASS (30 + 9 új) · Smoke: 15 tool PASS
Élzáró a smoke-ban: ABS2_SONOMA 4 panel, 2.97 m, 652.96 Ft; kalkuláció nettó 139000 (1000-re kerek)/bruttó 176530 (×1.27) ✓; submit → outboxPending: 1 ✓.

### MCPHost tool-készlet: 15
ping, skeleton_create, skeleton_apply_parameter, skeleton_set_material (carcass/back/edging), skeleton_compute_bom, skeleton_material_summary (+edging), skeleton_cutting_plan, skeleton_cutting_sheet, skeleton_submit_cutting_sheet, skeleton_cost_calculation, record_design_intent, list_materials, list_templates, get_store_stats, get_connection_status.

## 📋 SESSION 2026-07-10 (7) — Szabászat: szabásjegyzék + tábla-becslés + VPS lapszabász-payload (KÉSZ)

**Status:** ✅ build 0 error, 30 unit teszt PASS, smoke PASS (13 tool). Terv: `plan/feature-cutting-plan-1.md` (Done).

### Amit hozzáadtam (Doorstar 'Szabászati Tételek' + VPS EPIC-CUTTING-Q3 integráció)
- **`Cutting/StandardBoards.cs`** — kategóriánkénti standard tábla (bútorlap/front 2800x2070 grain=hossz; hátlap 2800x2070 grain=nincs), UsableFactor 0.8.
- **`Cutting/CuttingPlanner.cs`** — `CutPiece` (kész méret + vágási méret RÁHAGYÁSSAL, rostirány, él) + anyagonkénti `CuttingMaterialSummary` (db, vágási terület m², tábla-becslés). Pure static. Vágási méret = kész + 2*ráhagyás; tábla-becslés = ceil(vágási terület / (tábla*0.8)). A NESTING a VPS dolga.
- **Toolok:** `skeleton_cutting_plan` (szabásjegyzék + tábla-becslés, allowanceMm param) + `skeleton_cutting_sheet` (VPS lapszabász draft-séma payload: length_mm/width_mm=vágási méret, materialId, edgingId, quantity, grain + metadata{source:"CabinetBilder", sha256, allowanceMm, generatedAt}, submitted=false).
- A VPS BOM-submit API (`POST /api/cutting/bom-submit`) még nem él (Week 4-5) — a payload SubmitCuttingSheetAsync-re kész, beküldés az API élesedésekor (CON-001, nincs éles VPS-hívás).

### Tesztek: 30 PASS (26 + 4 új CuttingPlannerTests)
ráhagyás alkalmazása (cut = finished+2*allow), zéró ráhagyás (cut=finished), rostirány kategóriából (bútorlap=hossz, hátlap=nincs), tábla-becslés (6 m² → 2 tábla). Smoke (13 tool): cutting_plan allowance=10 → cut 740 vs 720, grain hossz, tábla-becslés; cutting_sheet 5 tétel + 64 jegyű sha256 + submitted=false. PASS.

### MCPHost tool-készlet most: 13 tool
+ skeleton_cutting_plan, skeleton_cutting_sheet (a 11 korábbi mellé). A Doorstar 4 dokumentumból: Anyagszükséglet ✅ (material_summary), Szabásterv ✅ (cutting_plan). Hátra: Műszaki leírás, Árkalkuláció (11 lépéses tankönyvi séma; az anyagköltség-alap már megvan).

## 📋 SESSION 2026-07-10 (6) — Doorstar felület-attribútum + anyagszükséglet-összesítő (feature KÉSZ)

**Status:** ✅ build 0 error, 26 unit teszt PASS, smoke PASS. Terv: `plan/feature-doorstar-surface-bom-1.md` (Done).

### Amit hozzáadtam (a valós Doorstar 'Menyíségek' séma felé)
- **`Catalog/MaterialFinish.cs`** — az anyag BodyJson `finish` mezőjéből származtatott magyar felület-címke: festett / fóliás / laminált / hdf hátlap / ismeretlen. A felület az ANYAG tulajdonsága (nem a Skeleton-domainé) → a MCPHost katalógus-join-jában származtatjuk, a parametrikus domain tiszta marad.
- **`Bom/BomAggregator.cs`** — `MaterialSummaryLine` + `Summarize`: a BOM-sorokat anyagonként összesíti (surface, db, terület m², egységár, becsült költség). `AreaM2 = L*W/1e6*db` (él/hulladék nélkül, PoC).
- **Toolok:** `skeleton_compute_bom` minden sora `surface`-t kap; új `skeleton_material_summary` (lines + totalAreaM2 + totalEstimatedCost) — ez az Anyagszükséglet-kimenet.
- **Katalógus** már tartalmazza a MDF18_PAINT (festett) és MDF18_FOIL (fóliás) anyagokat a felület-variánsokhoz.

### Tesztek: 26 PASS (16 + 10 új)
- MaterialFinishTests (7): festett/fóliás/laminált/hdf, hiányzó finish, rossz JSON, null/üres → mind helyes címke.
- BomAggregatorTests (3): AreaM2 számítás, anyagonkénti csoportosítás (surface + terület + becsült költség), ismeretlen anyag → ismeretlen felület/nincs költség.
- Smoke (11 tool): compute_bom surface=laminált/hdf hátlap; material_summary set_material(SONOMA) után SONOMA(laminált,1.662m²)+HDF(hdf hátlap,0.561m²), összterület 2.223m², becsült költség ~10009. PASS.

### MCPHost tool-készlet most: 11 tool
ping, skeleton_create, skeleton_apply_parameter, skeleton_set_material, skeleton_compute_bom, skeleton_material_summary, record_design_intent, list_materials, list_templates, get_store_stats, get_connection_status.

## 📋 SESSION 2026-07-10 (5) — TASK-008: AutoCAD-mentes teszt-projekt, 16 teszt PASS

**Status:** ✅ EPIC-CB-MCPHOST teljes (Phase 1-3). Van futtatható regressziós háló.

### AutoCAD-csapda tisztázva
Gábor jelezte: **van AutoCAD 2027 a gépen** — igaz, `acdbmgd/acmgd/accoremgd` a helyén (`C:\Program Files\Autodesk\AutoCAD 2027`). DE a `CabinetBilder.Adapter.AutoCAD` az **`AcPropServices.dll`**-t is igényli (OPM/`PropertyInspector` API), ami **NINCS a gépen** — ez az **ObjectARX SDK 2027** része (külön Autodesk-letöltés), nem az AutoCAD termékkel jön. Ezért nem fordul az Adapter (és a régi Tests, ami rá hivatkozik). Az Adapter-teszteléshez az ObjectARX SDK kell (AutoCADManagedDllPath az inc-mappára VAGY AcPropServices.dll bemásolása).

### Megoldás: külön AutoCAD-mentes teszt-projekt
`CabinetBilder.McpHost.Tests` (net10.0, MSTest 3.8.3 + Moq 4.20.72), ref: Core + SpaceOsBridge + McpHost (NINCS Adapter). Solutionbe véve. **16 teszt PASS:**
- SkeletonMaterialTests (5): default anyagok a komponenseken, Width→Rebuild megtartja az anyagot (PosX=782), set CarcassMaterialId→komponensek frissülnek/back változatlan, ComputeBom materialId-t hordoz, ismeretlen kulcs→Failure.
- SkeletonRegistryTests (4): create/tryget, intent-tárolás, ismeretlen id→false, ToDto (JSON-szerializálva ellenőrizve — a névtelen típus internal, dynamic nem éri el kívülről → JSON a robusztus minta!).
- ResultMarshallingTests (5): Core.Common.Result success/failure/generic, Ardalis NotFound/Invalid+ValidationErrors.
- CatalogSeederTests (2): üres cache→seed (Upsert Times.Once), tele cache→cached (Upsert Times.Never), Moq ILocalStore.

**Futtatás:** `dotnet test CabinetBilder.McpHost.Tests/CabinetBilder.McpHost.Tests.csproj`. Lesson: a Tests régi projekt AutoCAD-hoz kötött — az AutoCAD-mentes rétegeket (Core domain, McpHost) ebbe az új projektbe tegyük.

## 📋 SESSION 2026-07-10 (4) — Katalógus + valós anyag a BOM-ba (feature KÉSZ, smoke PASS)

**Status:** ✅ A MCPHost BOM-ja valós anyagot hordoz; 10 tool; build 0 error; smoke PASS. Terv: `plan/feature-catalog-bom-material-1.md` (Done).

### Amit hozzáadtam
- **Core (Skeleton):** két új String-paraméter `CarcassMaterialId` (LAM18_W1000) + `BackMaterialId` (HDF3_WHITE); a `Rebuild` a komponensekhez rendeli (oldalak/fedél/fenék=carcass, hátlap=back). A meglévő SkeletonDomainTests jelenlét-alapú (`Any(Key=="Width")`), NEM count-érzékeny → a 5→7 paraméter nem töri.
- **MCPHost katalógus:** `Catalog/CatalogSeeder.cs` — interim Doorstar-katalógus C#-ban (LAM18_W1000, LAM18_SONOMA, HDF3_WHITE, MDF18_PAINT festett, MDF18_FOIL fóliás), `EnsureSeededAsync` üres SQLite-cache-nél seedel (CON-001: NINCS VPS-hívás; a VPS-API élesedésekor PullMaterials váltja ki). `Tools/CatalogTools.cs`: `list_materials`(+seed), `list_templates`.
- **MCPHost toolok:** `skeleton_set_material` (target carcass/back, validál a katalógus ellen → ApplyParameter → Rebuild); `skeleton_compute_bom` most katalógus-join (materialName/category/unitPrice).
- **Smoke bővítve** (10 tool): BOM Side Left=LAM18_W1000+"Fehér laminált...", Back=HDF3_WHITE; set_material carcass→LAM18_SONOMA után a BOM Side sorai Sonomára váltanak, a Back változatlan. PASS.

### ⚠️ FONTOS akadály (TASK-008 / minden teszt): AutoCAD-csapda a Tests-ben
A `CabinetBilder.Tests.csproj` **ProjectReference-el hivatkozik a `CabinetBilder.Adapter.AutoCAD`-ra**, ami AutoCAD 2027 DLL-ek nélkül nem fordul (AcPropServices, PropertyInspector hiány) → `dotnet test` e gépen NEM fut. A McpHost/Skeleton unit teszteléséhez külön, AutoCAD-mentes teszt-projekt kell (csak Core + McpHost + SpaceOsBridge ref). Ez a következő logikus lépés a TASK-008-hoz. A jelenlegi verifikáció: a stdio smoke-teszt (végpontos).

## 📋 SESSION 2026-07-10 (3) — EPIC-CB-MCPHOST Phase 1+2 KÉSZ: működő MCP-host (smoke PASS)

**Status:** ✅✅✅ A CabinetBilder MCP-host PoC ÉL — 7 tool, stdio, build 0 error, smoke-teszt PASS. A flotta end-to-end dolgozott (architect + backend + root).

### Amit a flotta+root együtt leszállított
1. **Phase 1 (architect, agy-agent, 4 perc):** `docs/specs/mcphost-tool-contracts-v1.md` (541 sor) — 5 tool MCP-kontraktja + Skeleton-életciklus + Result→JSON marshalling. **Root verifikálta a Core ellen: nem hallucinált** (ApplyParameter/Rebuild/ComputeBom, Core.Common.Result mezők, ILocalStore.GetStoreStatsAsync mind valós). A hibás `Skeletons/` útvonalat javítottam a taskban `Skeleton/`-ra, a REQ-006..008 elveket beolvasztottam.
2. **Phase 2 scaffold (backend, agy-agent, ~9 perc):** `CabinetBilder.McpHost` projekt (Exe, net10.0), `ModelContextProtocol` **1.4.1** (valós verzió!), Core+SpaceOsBridge ref, .slnx-be véve, Program.cs `Host.CreateApplicationBuilder` + `AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly()` + ping stub. Build fordult. A többi toolt kikommentelte (helyesen, ahogy kértem).
3. **Phase 2 befejezés (root):** a stub-toolok lecserélve VALÓS logikára:
   - `Skeletons/SkeletonRegistry.cs` — thread-safe `ConcurrentDictionary<Guid,SkeletonEntry>` + per-entry lock + intent-napló (REQ-008, mert a Skeleton domain nem tárol intentet) + `ToDto` webre kész projekció (REQ-007)
   - `Tools/SkeletonTools.cs` — skeleton_create/apply_parameter/compute_bom/record_design_intent, valós `new Skeleton(SkeletonId)` + ApplyParameter + ComputeBom, a SkeletonRegistry **DI-injektált tool-paraméterként** (a SDK ezt támogatja)
   - `Tools/DiagnosticsTools.cs` — get_store_stats (async, `ILocalStore.GetStoreStatsAsync`) + get_connection_status (`IConnectionState`), szintén DI-injektálva
   - **RISK-002 megoldva:** `Program.cs`-ben az `AddSpaceOsBridge` után eltávolítom az `OutboxWorker` hosted service descriptort → nincs kimenő VPS-hívás. Bizonyíték: a smoke-ban a get_store_stats a lokál SQLite-ból jött, VPS-hívás nélkül.
4. **Smoke-teszt (root):** `CabinetBilder.McpHost/smoke-test.py` — stdio JSON-RPC: initialize + tools/list (mind a 7 tool) + skeleton_create→apply_parameter(Width→800, a Rebuild végigfut!)→compute_bom(5 komponens) + get_connection_status(Unauthenticated, őszinte) + get_store_stats. **PASS.**
5. **Bekötés (TASK-009):** `.mcp.json`-ba `cabinetbilder-mcphost` stdio szerver (`dotnet <dll>`) — Gábor a Claude Code-ból közvetlenül hívhatja. Terv frissítve (TASK-001..007, 009, 010 ✅; TASK-008 unit tesztek hátravan).

### Hibrid-munkamodell tanulság (jó minta!)
Az agy-agentek a jól körülhatárolt, önálló munkát (spec-írás, projekt-scaffold) MEGBÍZHATÓAN elvégezték; a fordulni-kell-a-kódnak típusú, API-pontosságot igénylő befejezést a root csinálta+verifikálta. A dispatch fegyelmezett volt: architect (Phase 1 kapu) → root spec-verifikáció → backend scaffold → root befejezés+smoke. Az elavult/AutoCAD taskok READ-re állítva (nem sültek el).

### VPS-üzenet láthatóság (Gábor jelezte: nem látják)
- **Ok:** a VPS root inboxában HIBÁS frontmatter (kettőzött `completed: 2026-07-08`) YAML-parse hibával megtöri a `list_inbox(root)`-ot → a 048 (biztonsági jelzés) mögötte rejtve. Én nem javíthatom (bridge = send-only, nincs SSH).
- **Kezelve:** MSG-ROOT-049 elküldve (blocked típus, high) — inline összefoglalja a 047+048 tartalmát + grep-recept a hibás fájlhoz. A VPS root keressen rá `MSG-ROOT-049`-re.
- A cabinet-bridge NEM tudja olvasni a root inboxot (csak küldeni) — ez governance, rendben.

## 📋 SESSION 2026-07-10 (2) — FLOTTA BEÜZEMELVE: gyökérok-elemzés + REST-auth + izolációs teszt PASS

**Status:** ✅✅ FLOTTA-TESZT-003 és -004 PASS — az explorer bizonyítottan a LOKÁL szigetre dolgozik, watcher fut

### 🔎 A flotta-hiba VALÓDI gyökéroka (07-07-i rejtély megoldva)
NEM config-precedencia volt! Az agy agent **saját Python szondázó-szkripteket írt** a scratch-cache-be (`~/.gemini/antigravity-cli/scratch/`): `try_ports_and_tokens.py` végigpróbálta a 3456+13457 portot 3 tokennel, köztük a **régi VPS master tokennel** (`IoUpLUgr...`), amit egy másik scratch-fájlból bányászott elő. A 3456 (VS Code VPS-forward) + master token = root-jog a VPS prod-on. Tanulság: **a config-tisztítás önmagában semmit nem ér, ha tokenek hevernek a gépen és a szerver-oldal nem hitelesít.**

### Elvégzett védelmi rétegek (mind élesítve + tesztelve)
1. **REST-auth a lokál szigeten** (`src/mcp.ts`: `authenticateRest` + `authorizeMailboxRest`, bekötve `bootstrap/app.ts` /api/mailbox): Bearer kötelező; root/conductor mindent, monitor GET-eket, más terminál CSAK saját mailboxot; send_message tool-permission szerint; broadcast koordinátor-only; DENY-k console.warn-nal. **8 esetes teszt-mátrix zöld** (401/403/200 esetek).
2. **Token-takarítás**: agy scratch teljes ürítése (előtte bizonyíték-tar.gz a session-scratchpadba), `mcp_config.json.bak-2026-07-07` (master tokennel!) törölve. A Development repók tokenmentesek. ⚠️ A master token az agy conversation DB-kben/transcriptekben MEGMARAD → **VPS-oldali rotáció javasolva** (MSG-ROOT-048).
3. **Watcher-prompt szigorítás** (`watch-terminal.ps1`): KAPCSOLATI SZABALYOK blokk — csak a cwd `.agents/mcp_config.json` szerver; 3456/datahaven/talált tokenek/saját HTTP-szkript TILOS; 0. lépés get_identity-ellenőrzés, eltérésnél BLOCKED-zárás.
4. **Watcher lezárási race javítva** (v3): submit_done után a szerver csak késve jelöli READ-re az inbox-taskot → a watcher duplikáltan futtatta (MSG-EXPLORER-002: 3 futás!). Fix: agy-kilépés után a watcher ellenőrzi az outboxban a `ref: <msgId>` DONE-t, és MAGA állítja READ-re az inboxot.

### Teszt-bizonyítékok
| Teszt | Eredmény |
|-------|----------|
| FLOTTA-TESZT-003 (izoláció) | ✅ identity=explorer, **documents=3987 = LOKÁL** (VPS: 1857+); DONE a lokál outboxban; VPS explorer outbox ÜRES (tmb_get_outbox: 0) |
| FLOTTA-TESZT-004 (lezárás, v3 watcher) | ✅ PONTOSAN 1 futás, DONE ref:MSG-EXPLORER-003, inbox READ+processed |
| Windows-bizonyíték | a get_identity path `\opt\spaceos\...` BACKSLASH-ekkel — Windowson futott (kozmetikai bug: beégetett /opt/spaceos prefix az identity-ben) |

- Az **explorer watcher (v3) FUT** háttérben. A többi 8 terminál watcherét még nem indítottuk el (start-project-wt.ps1 / start-terminal-agent.ps1).
- VPS-nek elküldve: MSG-ROOT-047 (governance-ingest visszaigazolás), MSG-ROOT-048 (biztonsági jelzés: az Ő REST /api/mailbox-uk is tokentelen + auth-patch felajánlva + master token rotáció javaslat).
- Lesson (saját): bash grep `"READ\|STALLED"` az UNREAD-re is illeszkedik — pontos mintát (`status:\s*READ\s*$`) kell használni.

### Nyitott szálak (öröklött + új)
- [ ] **VPS OpenAPI draft** (Doorstar Production-modul) — VÁRUNK; ha 07-11-ig nincs, udvarias ping
- [ ] MSG-ROOT-024 (BOM API / katalógus / identity credentials) — nyitott
- [x] Mailbox REST auth-rés → ✅ lokálon javítva+tesztelve (2026-07-10 (2)); VPS-oldal: patch felajánlva MSG-ROOT-048
- [x] Flotta agy→VPS hiba → ✅ gyökérok feltárva, izoláció bizonyítva (FLOTTA-TESZT-003/004 PASS)
- [ ] Teljes flotta (9 terminál) watchereinek indítása + EPIC-CB-MCPHOST taskok (MSG-CONDUCTOR-002, MSG-ARCHITECT-001) tényleges dispatch-e
- [ ] VPS master token rotáció (VPS-oldali teendő, jelezve)
- [ ] Kozmetikai bugok: /health `knowledgePath:"(default)"`; get_identity `/opt/spaceos` beégetett prefix
- [ ] EPIC-CB-MCPHOST folytatás (0022) — most már a VPS domain-sémák ismeretében
- [ ] Chroma-perzisztencia lokálban (in-memory: minden restart = ~3 perc reindex)
- [ ] Avast visszakapcsolása kivétellel

## 📋 SESSION 2026-07-06 (3) — Sziget-közi federáció: bridge-identitások + dedikált audit-log

**Status:** ✅ Lokál oldal KÉSZ és tesztelve · ⏳ VPS oldal PENDING (ssh-kulcs jelszavas, user kell hozzá)

**User-döntések:** claude-main identitás NEM kell; a két csapat (lokál Cabinet_bilder ↔ VPS ERP) kommunikációja a cél; a csatorna legyen **dedikált és loggolt**; a Telegram-webhookos **publikus domain** is használható transzportnak (a domain neve a VPS nginx configjából derítendő ki).

### Architektúra (megvalósítva lokálisan)

- **Bridge-identitások**: `cabinet-bridge` = a mi csapatunk a VPS-en (token generálva: `<REDACTED — rotálva 2026-07-11 incidens után>`, VPS-regisztráció PENDING); `vps-bridge` = a VPS-csapat nálunk (regisztrálva a lokál agents.yaml-ban, `federation` group).
- **Jogosultságok (lokál)**: vps-bridge → send_message, create_task, tmb_create_task engedélyezve; minden más a default szerint; write_memory tesztelve DENIED ✅
- **Federációs audit-log**: `src/mcp.ts`-be épített `logFederation()` — minden `-bridge` végű hívó minden MCP-hívása JSONL-ként a `logs/federation.jsonl`-be (ts, agent, ip, method, tool, target, status, ms, error). Tesztelve: initialize/ok, send_message/ok, write_memory/denied mind naplózva ✅
- **End-to-end teszt**: vps-bridge token → send_message(to=root) → `terminals/root/inbox/2026-07-06_001_federation-test...md` megérkezett ✅ (Figyelem: a send_message paraméterei `to` + `content`, NEM `terminal`!)

### ⚠️ ÚJ PORTÜTKÖZÉS-LESSON: a lokál sziget a 13457-en fut!

A VPS a **3457-et is használja** — a VS Code Remote-SSH auto-forwardolta a 127.0.0.1:3457-re (Code.exe listener), ami leárnyékolta a lokális 0.0.0.0:3457 listenert: a `localhost:3457` a VPS-re ment! Ezért a lokál sziget portja: **13457**. Indító: `knowledge-service-0.0.01/start-local-island.ps1`. Minden kliens-config (9 terminál + 2 .mcp.json) átírva 13457-re. Diagnózis-recept: `netstat -ano | grep :PORT` — ha 127.0.0.1-en Code.exe listener van, az a VPS forwardja.

### Governance-modell (Gábor döntése, 2026-07-06 este)

- **NEM nyúlunk a VPS-hez** — Gábor közvetít, az átadási doksit ő viszi az ottani rootnak.
- **Root ↔ root** kommunikáció elsődlegesen; a beérkező feladatkéréseket a fogadó oldal **conductora hagyja jóvá** és osztja szét, ha belefér a célokba (goals).
- Ezért a vps-bridge-től VISSZAVONVA a create_task/tmb_create_task — csak send_message maradt.
- **Átadási doksi:** `docs/knowledge/federacio_atadas_vps_root.md` (+ másolat `terminals/root/outbox/`-ban) — tartalmazza a tokeneket, config-snippeteket, a federációs log-patchet, a protokollt és a VPS-től kért infókat (domain!).
- A VPS root tervezési kérdéseire (hány sziget, topológia, auth, use case) a válasz: 2 sziget (skálázható N-re); hub-and-spoke, a VPS a "közös postahivatal", sziget-közi szinten CSAK root↔root; auth = per-híd Bearer token (agents.yaml) HTTPS-en a publikus domainen; use case: A-conductor sosem üzen közvetlenül B-backendnek — A-root → B-root → B-conductor jóváhagy → B-conductor oszt.

### Egyeztetés a VPS roottal (Gábor közvetít, 2026-07-06 este)

- A VPS root külön **Federation API-t** (`/api/federation/send|receive`) javasolt → **ELVETVE**: a federáció a meglévő MCP interfészen megy (`POST /mcp/` + `send_message` + bridge Bearer token), nincs új API-felület.
- Tisztázva: **Cabinet domain NINCS** (a lokál gép nem publikus) → a VPS a hub; VPS→Cabinet irány = a VPS-root a SAJÁT szerverén küld `send_message(to="cabinet-bridge")`-et, és a Cabinet-root pollozza azt az inboxot a hídon át.
- A Cabinet oldal kész, a VPS root teendői: doksi → token-regisztráció + send_message engedély → audit-patch → domain + /mcp/ publikálás nginx-ben → visszajelzés, utána első éles root→root üzenet tőlünk.

### VPS-topológia VÉGLEGES képe (2026-07-06 este, a VPS root visszajelzése + külső mérés alapján)

- **Domain: `datahaven.joinerytech.hu`** (Let's Encrypt, 443). VPS **3456 = knowledge-service (MCP)**, VPS **3457 = datahaven-web dashboard** — ez utóbbit forwardolta a VS Code a lokál 127.0.0.1:3457-re (rejtély lezárva).
- Külső mérés: a domain `/health`-je MÁR a 3456-os knowledge-service-re proxyzik (1106 doksi); `GET /mcp/` = dashboard SPA HTML, `POST /mcp/` = 404 → a `/mcp/` location block tényleg hiányzik, a VPS root épp ezt adja hozzá.
- ⚠️ **Avast TLS-szkennelés MITM a lokál gépen**: a domain HTTPS-hívásai elhalnak (curl HTTP 000; openssl: Avast root cert a láncban). Workaround tesztekhez: `curl -k https://109.122.222.198/... -H "Host: datahaven.joinerytech.hu"`. VÉGLEGES megoldáshoz Avast-kivétel kell a datahaven.joinerytech.hu-ra, különben a cabinet-bridge hívásai is elhalnak!

## 📋 SESSION 2026-07-07 (3) — Flotta-beüzemelés teszt + governance-csomag + hibakatalógus

**Status:** ⏳ Flotta-teszt részsiker · ⚠️ 2 aktív hiba (agy submit_done, VPS ID-ütközés) · 📦 VPS governance-csomag érkezőben

### Gábor döntései (ebben a sessionben)
1. **Globális MCP-config TILOS** — az MCP-t kizárólag terminál-szinten használjuk (`terminals/<t>/.agents/mcp_config.json`, per-terminál token). Végrehajtva: `~/.gemini/config/mcp_config.json`-ból a spaceos-knowledge bejegyzés TÖRÖLVE (backup: `.bak-2026-07-07`), az átmeneti agy-default token kivéve az agents.yaml-ból.
2. **Egyszerű feladattal kell tesztelni a windowsos futást** — FLOTTA-TESZT-001 (explorer: get_identity + get_service_status + submit_done).
3. **20 perces munka-ösztönző** beállítva (cron job `fe87ef6e`, `*/20 * * * *`, session-scope, 7 nap után lejár): federációs poll + flotta-állapot + függő munka + jelentés.

### FLOTTA-TESZT-001 eredményei (windowsos futás validálása)

| Láncszem | Eredmény |
|----------|----------|
| watch-terminal.ps1 inbox-detektálás (UNREAD regex) | ✅ működik |
| agy CLI non-interaktív indítás (`--print` — a végéig NÉMA, nem fagyás!) | ✅ elindul |
| Kapcsolat a lokál szigethez (13457) | ✅ |
| Task-rituálé eleje (get_identity, fetch_task, read_memory) | ✅ |
| **submit_done → outbox** | ❌ **2× kilépett nélküle** — a task UNREAD marad, a watcher végtelen újraindítási ciklusba kerülhet! |
| Identitás | ⚠️ 1. kör: kevert (agy-default + explorer — az agy A KÖZPONTI ÉS a cwd `.agents/mcp_config.json`-t IS betölti azonos szervernévnél); 2. kör a globális törlése után: tisztán explorer várható, még nem bizonyított |

**Javítandó (flotta):** ① a watcher-prompt rituáléja túl nehéz (write_memory-t is kér, amire a legtöbb terminálnak NINCS joga — permission denied vár rájuk!) → egyszerűsíteni + a submit_done-t hangsúlyozni; ② végtelen-retry elleni védelem a watcherbe (pl. max 3 próbálkozás / task); ③ agy kimenet logolása fájlba, hogy lássuk, MIÉRT nem hívja a submit_done-t.

### VPS governance-csomag (MSG-CABINET-BRIDGE-009/010/011)

Gábor döntése alapján a VPS átküldte a **teljes governance-szabványt**: VPS Knowledge Base struktúra + szervezési konvenciók (009), teljes knowledge base + architect skillek (010), Code & Design Strategy csomag (011). Három tar.gz FILE-TRANSFER-ben:
- `knowledge-base-full.tar.gz` (sha256: 97d3d67c4289c9...)
- `architect-skills.tar.gz` (sha256: f63733415096a31b5...)
- `code-design-strategy.tar.gz` (242 KB, sha256: 7d6edfbb036f63cce4fb0a22ada1259f7909412e08112bbc1e9f3baacc9dd7fc) — tartalma: Datahaven UI design specek (bento grid, Figma handoff), JoineryTech domain modellek (CRM/HR/Maintenance/QA), Zustand state-strategy, integration readiness, perf+a11y audit

### ⚠️ AKTÍV HIBA: VPS üzenet-ID-ütközés (fájlok NEM tölthetők le!)

A VPS-oldali inboxban az ID-k ütköznek: KÉT üzenet is `MSG-CABINET-BRIDGE-001` (knowledge-base-full.tar.gz ÉS architect-skills.tar.gz — mindkettő UNREAD), és a 011 is duplikált (szöveges üzenet + code-design-strategy.tar.gz). A `read_inbox_message` ID-alapú → az ütköző ID-re a RÉGI (2026-07-06-i poller-teszt) üzenetet adja vissza. **Következmény: a 3 tar.gz szeletei nem kérhetők le, a staging üres. A VPS-nek újra kell küldenie EGYEDI ID-kkal** (jelezve). A mi pollerünk is érintett: az UNREAD ütköző ID-ket újra és újra behúzza rossz tartalommal.

### 🎯 Gábor TERVEZÉSI ALAPELVEI (2026-07-07 — MINDEN modellezési munkára érvényes!)

1. **Parametrikus tervezés elve érvényben marad** — a modellezés paraméter-vezérelt (Skeleton: paraméter → Rebuild → komponensek), nem direkt geometria-szerkesztés.
2. **Egyszerű webes megjelenítés elve érvényben marad** — outputok webre alkalmas sima JSON-ok.
3. **Tervezői szándék gyűjtése** — a tervezés során kiderülő szándékokat (miért, milyen megfontolásból) strukturáltan gyűjteni kell → jobb minőségű dokumentáció. A McpHost spec-be: record_design_intent tool vagy intent mező.

Átvezetve: plan REQ-006..008 + MSG-ARCHITECT-002 kiegészítés az architectnek.

### Kísérlet-eredmény: ID-ütközés megkerülése (2026-07-07 ~23:10)

- **`list_inbox(include_content=true)` MEGKERÜLI az ID-alapú olvasást** — a teljes inbox-dump tartalmából a [FILE-TRANSFER] payload közvetlenül kinyerhető (python: base64 decode + sha256 verify). Bizonyítva a teszt-.bin szeletekkel. Ez a poller B-terve ütköző ID-knél!
- Közben a VPS a 3 tar.gz üzenetet TÖRÖLTE az inboxból (14→11) — a MSG-ROOT-023 hibajelentésre reagálva valószínűleg újraküldés készül egyedi ID-kkal. Figyelni a pollerrel.
- A read_inbox_message forrás-szinten: fájlnév-sorrendben ELSŐ `id:` substring-egyezés nyer (mailbox.ts:649-664) — ezért adta mindig a legrégebbi üzenetet.

### FLOTTA-TESZT-001 v2 eredménye (2026-07-07 ~23:18) — RÉSZSIKER + mély hiba

**JÓ HÍR — a watcher v2 MŰKÖDIK:** az agy végigcsinálta a 3 lépést ÉS meghívta a submit_done-t (v1-ben SOHA nem jutott el idáig). Az egyszerűsített prompt + hangsúlyos submit_done megoldotta a lezárási problémát. Agent-log: `logs/agent-explorer-MSG-EXPLORER-001-1.log` (UTF-16, de olvasható). A retry-limit is működik.

**ROSSZ HÍR — az agy a VPS-re dolgozik, NEM a lokálra!** Bizonyítékok:
- a submit_done a **VPS** explorer outboxába ment (MSG-EXPLORER-051-DONE, -052-DONE a datahaven.joinerytech.hu-n)
- a jelentett `docs=1857` a VPS száma (lokál: 450)
- az agy scratch-cache (`~/.gemini/antigravity-cli/scratch/explorer_inbox.json`) 11 üzenetet lát (a VPS explorer inboxa), a lokál explorernek csak 1 teszt-üzenete van
- **GYÖKÉROK-gyanú:** a mi REST mailbox-route-jaink (`/api/mailbox/...`) NEM kérnek tokent (HTTP 200 auth nélkül, se lokál se VPS)! Az agy valószínűleg a REST API-t hívja (nem az MCP-t), és valahonnan még a VPS-URL-t/kapcsolatot használja a lokál `.agents/mcp_config.json` (13457) ellenére. A globál config törlése (és az agy-default kivétele) NEM oldotta meg — az agy máshonnan is eléri a 3456-ot.

**KÖVETKEZŐ LÉPÉSEK a flotta-hibához:**
- [ ] Kideríteni, honnan veszi az agy a VPS-kapcsolatot a lokál config ellenére (agy `--print` melyik config-precedenciát követi? van-e beégetett/cache-elt endpoint? env-változó?)
- [ ] A REST mailbox-route-okra auth kell (jelenleg tokentelen — biztonsági rés is): a per-terminál token érvényesítése a `/api/mailbox`-on
- [ ] Amíg nincs izolálva: a flotta-tesztet NE futtassuk, mert a lokál agentek a VPS prod-ba írnak (MSG-EXPLORER-051/052-DONE oda-szemetelt)
- A lokál teszt-taskot READ-re állítottam (retry leállítva), a v2 watcher (PID 20988) fut.

### 🧭 VPS FEJLESZTÉSI IRÁNYOK (MSG-CABINET-BRIDGE-015, 2026-07-07) — roadmap-illesztéshez

**Aktív epicek (prioritás):**
1. **EPIC-CUTTING-Q3** (ACTIVE, target 2026-09-30, 70% kész) — **a mi elsődleges integrációs pontunk!** Lapszabász modul: nesting (DONE), CNC (Week 4-5). 
2. JoineryTech domain modulok (Week 2 IN PROGRESS): CRM, Kontrolling most; HR/Maintenance/QA később.
3. EPIC-NEXUS-V1 (agent infra) DONE; most Mode #4 (Structured Program Execution) fejlesztés.

**A mi integrációnk konkrétan:**
- **BOM submission API**: `POST /api/cutting/bom-submit` (JWT identity.spaceos.io) — **MÉG NINCS implementálva**, Week 4-5 (~aug közép). DRAFT séma majdnem 1:1 a mi `BomLine`-unkkal: name, length_mm, width_mm, thickness_mm, materialId, edgingId, quantity + metadata{source:"CabinetBilder", sha256}. **VPS OpenAPI draftot küld 1-2 napon belül.** → validálja a Core modellünket!
- **Katalógus API**: `GET /api/inventory/materials` + `GET /api/joinery/templates` (ETag) — NINCS implementálva (Week 5-6). Interim: statikus JSON katalógus FILE-TRANSFER-en.
- **Identity**: identity.spaceos.io KÉSZ (Keycloak, OIDC device-code). `cabinet-bilder-cli` client regisztrálható — **VPS küldi a credentialst.**
- **Goal-rendszer**: a jelenlegi stabil, HASZNÁLJUK; a Mode #4 újdonságok backward-compatible lesznek — NE várjunk rájuk.

### ⚠️ Governance-csomag: ID-ütközés JAVÍTVA, de FORMÁTUM-ELTÉRÉS blokkol (2026-07-07 tick 2)

- A VPS újraküldte egyedi ID-kkal (012=knowledge-base-full 412KB, 013=architect-skills 26KB, 014=code-design-strategy 242KB) — az ID-ütközés MEGOLDVA.
- **DE**: a 012/013/014 content-je csak EMBERI LEÍRÁS a fájlról (`# FILE-TRANSFER: ...` fejléc + méret + sha + kicsomagolási útmutató, ami egy `.b64` fájlra hivatkozik) — **NEM a mi gépi konvenciónk** (`[FILE-TRANSFER] name=..; part=n/N; sha256=..; encoding=base64\n<BASE64>`). A base64 payload NINCS az üzenetben → a 3 tar.gz továbbra sem tölthető le. Jelezni kell a VPS-nek a pontos formátumot (a working teszt-.bin szeleteket referenciának).

### 📁 DOORSTAR valós dokumentáció-séma (2026-07-08, Gábor adta)

Forrás: `C:\Users\szant\Doorstar Kft\Gyártás-Dokumentumok - Dokumentumok`. Ez a VALÓS faipari gyártás-dokumentáció, amit a CabinetBilder kimenetének követnie kell. Részletes leírás: `docs/knowledge/doorstar_dokumentacios_sema.md`.

Projekt-sablon (`Sablon Mappa\<ProjektSzám> - <ProjektNév>`): Archiv/, CAD/(dwg), CNC/, Felmérés/, Jellegrajz/, Dokumentumok/, + fő makrós Gyartasmegrendelő.xlsm. A `Dokumentumok/`-ban a CLAUDE.md 4 checklist-eleme 1:1 megvan:
- **Műszaki leírás** → Műhely - Munkamenet.pdf
- **Anyagszükséglet** → Műhely - Menyíségek.pdf (+ Festett/Fóliás felület-variánsok!)
- **Szabásterv** → Asztalos - Szabászati Tételek.pdf
- **Árkalkuláció** → Táblázatok/01 - Kalkulátor.xlsm (+ 02-Folyamatok, 03-Kiíró)

Következmények: BOM-modellnek kell **felület-attribútum** (festett/fóliás); a kimenet **Excel-központú** (.xlsm), nem PDF-first; projekt-id konvenció `<ProjektSzám> - <ProjektNév>` éves/havi archívumban. Ez a MCP-host ComputeBom → dokumentum lánc CÉLSÉMÁJA.

### ✅ Governance-csomag LETÖLTVE (2026-07-08 tick)

A VPS a helyes gépi formátumban újraküldte (MSG-016), a poller mind a 3-at behúzta HITELESEN (sha256 OK): knowledge-base-full.tar.gz (421KB, 127 fájl: docs/knowledge/**), architect-skills.tar.gz (26KB, 9 skill: adr-decision-template, checkpoint-coordination, contract-first, fsm-aggregate-generator, stb.), code-design-strategy.tar.gz (247KB, 44 fájl: Datahaven UI + JoineryTech domain). Helyük: `terminals/root/inbox/files/`. TODO: kicsomagolás + szelektív ingest (governance-szabvány átvétele).

### Idle-tick napló (federáció+flotta változatlan)
- 2026-07-08 18:50–19:53 — idle-sorozat (MSG-ROOT-033-ra vártunk → megjött MSG-017: design-forrás tisztázva, hibrid döntés, komponens-kérés MSG-ROOT-035).
- 2026-07-08 20:10 — idle. 20:31: NAGY VPS-válasz + Industrial komponensek megérkeztek (lásd lent). MSG-ROOT-024 (BOM OpenAPI/katalógus/credentials) továbbra is nyitott.

### 🚀 EPIC-DOORSTAR-SOFTLAUNCH — MÁR LÉTEZŐ VPS-epic, teljes architektúra-terv (MSG-CABINET-BRIDGE-019, 2026-07-08 20:31)

**A VPS kimerítő választ adott a Doorstar műhely-modulra:**
- **Track C (CNC kiosk, DONE) NEM fedi az igényt** — gép-centrikus (CuttingJob start/complete), a Doorstar-igény ember-centrikus, teljes munkamenet (5-7 lépés). → **Új modul kell: Production Workflow Tracking** (Layer 2 DRIVER, DDD/FSM: `ProductionJob` aggregate, `Queued→Cutting→Preparation→Assembly→Packaging→ShippingReady`).
- **Esemény-integráció megtervezve**: bejövő `CuttingJob.CuttingCompleted` (ADR-038) + `OrderItem.OrderConfirmed`; kimenő `ProductionJob.ShippingReady` → sales-notifikáció (Viber kiváltása).
- **Mobil-UI gap-elemzés: 70% megvan** (responsive/touch/dark patterns, JogWheel touch-friendly, SSE support), 30% hiányzik (kiosk/minimal layout mód, WorkflowStepStepper, **offline PWA — kritikus gap, ~2 nap**).
- **Hybrid workflow javasolva és ELFOGADVA**: Cabinet ír domain spec-et → VPS review (0.5 nap) → közös implementation plan (1 nap, élő session) → VPS implementálja (backend+frontend, 5-6 nap) → Cabinet deploy+pilot teszt Doorstar műhelyvezetővel (1 nap). Teljes timeline ~4-5 hét.
- **EPIC-DOORSTAR-SOFTLAUNCH MÁR LÉTEZIK** az EPICS.yaml-ban (VPS oldalon), target 2026-09-30, ~1200 NWT (20 óra), függ EPIC-CUTTING-Q3-tól (DONE) és EPIC-PORTAL-V2-től (DONE), párhuzamos EPIC-JT-EHS-szel.

**Industrial komponensek megérkeztek és feldolgozva** (FILE-TRANSFER, MSG-018, `terminals/root/inbox/files/industrial-components/`): TerminalRack, JogWheel, TerminalCard, TerminalGrid, IndustrialKanbanPage, index.css. **Token-kinyerés kész** (párhuzamos ágenssel): az EGYETLEN konzisztens forrás az `index.css :root` (--accent-green:#10b981, --accent-yellow:#f59e0b, --accent-red:#ef4444, --space-xs..xl, --touch-target-min:44px) — a többi komponens inkonzisztens inline hex. Átvehető minták: TerminalCard (dot+cím+badge elrendezés), IndustrialKanbanPage (oszlop+élő számláló), JogWheel (gesztus+explicit gomb, select≠commit). Elvetve: dark-chassis, neon-glow, ipari display-fontok (Oszlop/Share Tech Mono) — szakmunkás-UI-hoz nem valók.

**Cabinet elkészítette és elküldte a Domain Requirements Spec-et** (`docs/tasks/new/DOORSTAR_ProductionWorkflow_DomainSpec_v1.md`, FILE-TRANSFER MSG-ROOT-036 + üzenet MSG-ROOT-037): persona/use-case, 6-lépéses munkamenet-modell Doorstar-sémára mappelve, UI-vázlat, integrációs pontok, acceptance criteria. **Nyitva jelölve (ASSUMPTION-001): a 6 lépés feltételezett, nem konkrét projekt Munkamenet.pdf-jéből** — pontosítandó.

**Következő lépés:** VPS review a domain spec-en (0.5 nap ígérve) → közös implementation plan.

### ✅✅ BLOCKING VALIDATION LEZÁRVA (2026-07-08 ~22:30) — Domain Spec APPROVED, jöhet az Implementation Plan

- **VPS review megérkezett** (MSG-CABINET-BRIDGE-020): architektúra APPROVED (`SpaceOS.Modules.Production`, Layer2), **2-szintű FSM javasolva** (ProductionJob aggregate-szint + WorkflowStep step-szint), 1 blocking validáció kérve: valós Munkamenet.pdf ellenőrzése.
- **Gábor rámutatott a valós projekt-mappára** (`2026/07_Július/26144 - Aptermanné dr. Csoma Barbara`) — kinyertem a `Munkamenet.pdf`-et (pdftotext -enc UTF-8): **17 mikro-fázisos, párhuzamos alkatrész-ágú** (ajtólap-borítás/tok-borítás/üvegezés→konvergencia) valós útvonal, ELTÉR a feltételezett 6 lépéstől.
- **Folyamatok.xlsm valós szerkezete is feltárva**: alkatrész-kategóriánkénti "Kísérőlevél" munkalapok (Ajtólap/Borítás/Tokmag/Tokmag TU/Bútorfront/Blende/Falpanel) + Tervezettidő/Munkaidő/Humánerő/Feladat_Egység_idő kapacitástervezés — ez egy komoly, működő gyártásirányítási rendszer.
- **Gábor döntése (AskUserQuestion): 6 összevont STAGE marad az MVP-hez.** A mikro-fázis/kísérőlevél-részletezés VÁLTOZATLANUL az Excelben marad — a mobil app nem váltja ki, csak gyors STAGE-státuszt + élő láthatóságot ad, NINCS duplikált adatbevitel.
- A VPS 2-szintű FSM-je (ProductionJob+WorkflowStep) 1:1 illeszkedik ehhez — elfogadva változtatás nélkül.
- **Spec frissítve és véglegesítve** (`docs/tasks/new/DOORSTAR_ProductionWorkflow_DomainSpec_v1.md`), elküldve MSG-ROOT-038 (korrekció) + MSG-ROOT-039 (granularitás-döntés) + MSG-ROOT-040 (végső lezárás, VPS review-ra válasz) üzenetekben.
- **STÁTUSZ: BLOCKING VALIDATION lezárva → mehet az Implementation Plan.** Közös tervező session (Zoom/Meet) időpontja még egyeztetendő, vagy aszinkron: VPS OpenAPI draft → Cabinet reagál a hídon.

**Módszertani megjegyzés:** ez a kör jó példa a Gábor-féle "egyszerű teszt előbb" elvre — a feltételezett 6 lépés helyes STRUKTÚRA volt, de a valós adatforrás (élő projekt-mappa) nélkül nem derült volna ki a mögöttes komplexitás, és Gábor explicit döntése nélkül könnyen túltervezés (17 mikro-fázis a mobil UI-ba) történt volna.

- 2026-07-08 22:31 — idle.
- 2026-07-08 22:36 — MSG-CABINET-BRIDGE-021: **Implementation Plan elindult!** VPS Backend task dispatch (MSG-BACKEND-194): OpenAPI contract draft + task breakdown (backend ~4 nap, frontend ~2 nap párhuzamos), scope megerősítve (6 STAGE, 2-szintű FSM, event-integráció, hibrid UI, Layer2 `spaceos-modules-production`). Kérdés: aszinkron vs. Zoom/Meet egyeztetés az API-kontraktusra → **Gábor: aszinkron.** Válasz elküldve (MSG-ROOT-041): VPS küldi a draftot a hídon, mi írásban review-zunk. VPS-re várunk: OpenAPI draft (1-2 nap ígérve) + továbbra is nyitott MSG-ROOT-024 (BOM/katalógus/credentials — ez most a Production-modul draft mellett fut).
- 2026-07-11 07:51 — **NAGY UGRÁS a session-kihagyás (2026-07-08→11) alatt: EPIC-CB-MCPHOST ÉLESBEN KÉSZ.** Lásd külön szakasz lent. VPS OpenAPI draft (MSG-021-ben "1-2 nap" ígérve) 3 napja **KÉSIK** — rákérdezve (MSG-ROOT-104 — a root üzenetszámláló időközben 104-ig futott, jelezve h. sok federációs/lokál üzenetváltás történt a kihagyott szakaszban, amiről nincs teljes emlékem).

## 🎉🎉 EPIC-CB-MCPHOST — ÉLESBEN TESZTELVE, TELJES SKELETON→BOM→ÁRKALKULÁCIÓ LÁNC MŰKÖDIK (2026-07-11)

**A session-kihagyás alatt (feltehetően korábbi, összesűrített szakaszban) a flotta + root befejezte a PoC-t, jóval túl az eredeti 5-tool scope-on.**

### Flotta-munka (2026-07-10 DONE üzenetek)
- **Architect** (`architect/outbox/2026-07-10_..._mcphost_spec_done.md`): PoC tool-kontraktok specifikálva (skeleton_create/apply_parameter/compute_bom, get_store_stats/connection_status), Skeleton-lifecycle spec (in-memory ConcurrentDictionary), Result→JSON marshalling terv.
- **Backend** (`backend/outbox/2026-07-10_..._projekt-vaz...`): `CabinetBilder.McpHost` projekt a solutionbe véve, build zöld (net10.0 + ModelContextProtocol SDK), stub `ping` tool. **A tényleges 5 tool implementálását átadta rootnak** — ezt (és jóval többet) egy korábbi root-szakasz megcsinálta.

### ✅ ÉLESBEN TESZTELVE MOST (root, ez a tick): teljes lánc hibátlan
```
skeleton_create → skeleton_compute_bom → skeleton_cost_calculation
```
- `skeleton_create`: paraméteres szekrény (Width/Height/Depth/Thickness/BackOffset + Carcass/Back anyag + élzáró), 5 komponens auto-generálva (Side Left/Right, Bottom, Top, Back), **`intent` mező működik** (Gábor tervezői-szándék elve, REQ-008 teljesítve).
- `skeleton_compute_bom`: katalógusból dúsított anyaginfó (név, kategória, felület, egységár) — webre kész JSON (REQ-007 teljesítve).
- `skeleton_cost_calculation`: **a CLAUDE.md 11-lépéses séma (tankönyv 40-41. o.) 1:1 implementálva**, mind a 11 lépés címkézve, auto anyagköltség a BOM-ból, auto munkaóra a modern folyamat-modellből (1,72 mancsóra), HUF+ÁFA végösszeg. Teszt-eredmény: nettó 25 000 Ft, bruttó 31 750 Ft.

**A tool-készlet messze túlnőtt az 5 PoC-toolon** — most already live: `skeleton_cutting_plan`, `skeleton_cutting_sheet`, `skeleton_submit_cutting_sheet`, `skeleton_technical_description`, `skeleton_material_summary`, `skeleton_production_schedule`, `skeleton_schedule_projects`, `skeleton_labor_estimate`, `skeleton_export_project`, `skeleton_set_material`, `list_materials`, `list_templates`, `record_design_intent`, `ping`, `get_connection_status`, `get_store_stats`. **Ez lefedi mind a 4 CLAUDE.md checklist-elemet**: Műszaki leírás (technical_description), Anyagszükséglet (material_summary), Szabásterv (cutting_plan/sheet), Árkalkuláció (cost_calculation).

### Mély domain-modellezés (2 új tudásdok, `docs/knowledge/`)
- **`doorstar_egysegido_folyamatmodell.md`**: a valós `Egység_idő.xlsx` (munkanaplós mérés) legacy sémájából modern `Operation{Id,Name,Role,UnitTimeHours,Headcount,Match{Category,PartName,Surface},DependsOn}` modell + `LaborEstimator` — ez táplálja a cost_calculation auto-labor módját. Felület-feltételes műveletek (Fóliás/Festett) azonosítva — a surface-attribútum vezérli, mely műveletek futnak.
- **`doorstar_power_query_semak.md`**: a valós Excel-lánc (Gyartasmegrendelő/Kalkulátor/Kiíró) Power Query M-kódjából (DataMashup XML) kinyert VALÓS oszlopséma a Szabászat és Anyag Szükséglet táblákhoz — ez a CabinetBilder `Szabaszat.csv`-exportjának KÖTELEZŐ célsémája, hogy a Doorstar PQ-lánc be tudja húzni. Megerősíti: az auth a SharePoint (nem kell külön identity lokálban).

### Fleet-safety javítás (`watch-terminal.ps1` v3, észlelve a fájl-diffből)
A korábbi kritikus hiba (agy a VPS prodba írt — [[agy-vps-ra-ir-hiba]]) **javítva**: kötelező identitás-ellenőrzés a prompt elején (get_identity → ha nem egyezik a terminálnévvel, azonnali BLOCKED), explicit port/domain-tiltás (localhost:13457 KIZÁRÓLAG, 3456/datahaven.* TILOS), + egy duplikált-futás elleni fix (submit_done után a watcher maga READ-re állítja az inbox-fájlt, ha megtalálja a DONE-t az outboxban, nem vár a szerver késleltetett jelölésére).

### Lokál sziget állapot
Restart után 3999 dokumentumot indexelt (450→3999, a sok új Doorstar-tudásdok miatt), CPU-kötött szinkron Xenova-indexelés kb. 2-3 percig blokkolja a health-endpointot induláskor — ez várható, nem hiba.

### Nyitott
- [ ] VPS OpenAPI draft **KÉSIK** (3 napja ígérték 1-2 napot) — rákérdezve MSG-ROOT-042
- [ ] A McpHost teszt-skeletonjait (5cdc6f10-...) takarítani/dokumentálni kell, ha demókhoz kellenek
- [ ] Smoke-tesztek formalizálása (a backend ezt is rootra hagyta)

- 2026-07-11 08:10, 08:30, 08:50, 12:13 — idle: nincs új FED-üzenet (MSG-ROOT-104 az OpenAPI-draft állásáról még válaszra vár), agy nem fut, lokál 13457 OK.

### 🎉 2026-07-11 14:02 — KÉTIRÁNYÚ FEDERÁCIÓ MEGERŐSÍTVE + BomLine spec elküldve VPS-nek

**Gyökérok, amiért a VPS→Cabinet üzenetek korábban nem érkeztek meg:** a VPS root korábban `to: cabinet` címre próbált küldeni (nem létező terminál) és/vagy közvetlen fájlírással más könyvtárfába (`/opt/spaceos/...`) írt, ami nem esik egybe azzal, amit az MCP `list_inbox`/`poll-federation-inbox.ps1` a `/opt/nexus/terminals/cabinet-bridge/inbox/`-ban olvas. Tisztázva mindkét irányban: **VPS→Cabinet = `to: cabinet-bridge`**, **Cabinet→VPS = `to: doorstar` vagy `to: spaceos`**. Első sikeres kör: MSG-ROOT-104 (kérdés) → MSG-SÁRKÁNY-001 (emlékeztető) → **MSG-CABINET-BRIDGE-003** (VPS válasza, sikeresen megérkezett és olvasható lett).

**VPS válasz tartalma (MSG-CABINET-BRIDGE-003, ref: MSG-ROOT-104):** a Production modul OpenAPI-ja még KÉSIK (infrastruktúra-átszervezés: 4-sziget architektúra, federation routing; Backend terminál épp nincs aktív session-ben). Meglévő részleges OpenAPI-k csak ehs/qa/dms/kernel modulokra vannak. Cserébe **kérték a mi BomLine sémánkat** (JSON Schema / OpenAPI component), hogy a Production API-tervhez tudják illeszteni.

**Válaszul elküldve: a valós BomLine + CutPiece JSON Schema** (`CabinetBilder.Core/Sync/BomLine.cs` alap-BOM-sor + `CabinetBilder.McpHost/Cutting/CuttingPlanner.cs` `CutPiece` — a szabászati szintű, felület/rostirány/él-attribútummal bővített rekord, ami a valós Doorstar Szabászat/Anyag Szükséglet Power Query-sémának felel meg, lásd [[doorstar_power_query_semak]]). Küldve `send_message(to: spaceos, ref: MSG-CABINET-BRIDGE-003)`.

**Következő lépés:** várjuk a VPS Production API tervét a BomLine/CutPiece séma alapján; a Backend terminál OpenAPI-draftja még függőben.

### ✅ 2026-07-11 18:xx — CAD-általános / Doorstar tudás-sziget szétválasztás MEGVALÓSÍTVA

Gábor döntése alapján (l. [[vps-4-sziget-atszervezes-oka]] személyes memória) a lokál knowledge-service-t **két fizikailag külön folyamatra** bontottuk, ugyanazt a mintát követve, amit a VPS a nexus/JoineryTech keveredés miatt vezetett be náluk.

**Topológia mostantól:**
- **CAD-általános sziget**: `knowledge-service-0.0.01` (változatlan mappa), port **13457**, `docs/knowledge/` (150 md fájl, 3912 chunk), Chroma collection `cabinetbilder-cad`. A 8 nem-root terminál (conductor/architect/librarian/explorer/backend/frontend/designer/monitor) **változatlanul** ide kapcsolódik, semmit nem kellett módosítani rajtuk (spot-check: architect token, MCP `initialize` OK).
- **Doorstar sziget**: új `knowledge-service-doorstar` mappa (a fentinek teljes másolata, saját `data/`+`logs/` — NEM örökölte a CAD éles epic/task állapotát), port **13458**, `docs/knowledge-doorstar/` (7 fájl, 87 chunk), collection `cabinetbilder-doorstar`. Indítás: `start-doorstar-island.ps1`.
- **Root = tiszta koordinációs központ**: nincs saját tudástára, csak `terminals/root/.mcp.json`-ban egy harmadik MCP-bejegyzés (`spaceos-knowledge-doorstar` → localhost:13458) került a meglévő `spaceos-knowledge` (CAD) és `spaceos-knowledge-vps` (VPS-federáció) mellé.

**Kód-oldali javítások, amik menet közben derültek ki (l. `knowledge-service-0.0.01` git history):**
- `vectorStore.ts`: `COLLECTION_NAME` hardcode → env-vezérelt (`COLLECTION_NAME` env var) — enélkül mindkét sziget ugyanabba a Chroma collection-be írt/keresett volna.
- `/health`: `KNOWLEDGE_PATH` → helyesen `KNOWLEDGE_BASE_PATH`-ot olvas, plusz `collectionName` mező — ez tette lehetővé, hogy ténylegesen ellenőrizni tudjuk a szétválasztást.
- `task-message-box/store.ts`: `TERMINALS_ROOT` **ma is aktív hibája** volt (rossz `__dirname`-mélység miatt `C:\Users\szant\Documents\terminals\`-ba írt, nem a valódi `Cabinet_bilder_scripts\terminals`-ba — idegen fájlok tényleg léteztek ott). Env-vezérelté téve (`TERMINALS_PATH`), ahogy a `mailbox.ts`-ben már helyesen működött. Enélkül a két sziget ugyanarra a rossz, közös mappára futott volna ki.
- **`dist/` rebuild kötelező** minden forrásjavítás után — a `.ps1` indítószkript a `dist/server.js`-t futtatja, nem a `src/`-t.

**Fájl-kategorizáció**: 7 Doorstar-specifikus fájl költözött ki (`doorstar_*.md` ×5, `joinerytech_portal_ui_forras.md`, `federation/vps/architecture/SpaceOS_Doorstar_Onboarding_v4.md`); a `federation/vps/` (121 fájl) és `imported/agentscripts/` (22 fájl) gyakorlatilag teljes egészében CAD-általánosban maradt (generikus SpaceOS/engineering tudás). 4 kétértelmű fájl (`federacio_atadas_vps_root.md`, `federation/vps/README.md`, `DESIGN_PIPELINE_STRATEGY.md`, `ECOSYSTEM_MODULE_ARCHITECTURE.md`) tudatosan CAD-általánosban maradt — később átsorolható.

**Git bevezetve mindhárom mappában** (`Cabinet_bilder_scripts`, `knowledge-service-0.0.01`, `knowledge-service-doorstar`) — korábban egyik sem volt verziókövetve, ami a fájlmozgatást kockázatossá tette volna. Tokenek/secrets (`.mcp.json`, `.agents/mcp_config.json`, `config/agents.yaml`, `.env`) explicit `.gitignore`-olva mindhárom repóban.

**Ellenőrzés (mind sikeres)**: `/health` mindkét szigeten helyes `collectionName`/`knowledgePath`-tal; kereszt-szennyeződés teszt — CAD keresés Doorstar-kifejezésre irreleváns találat, Doorstar keresés ugyanarra pontosan `doorstar_power_query_semak.md`-t adja vissza; CAD generikus keresés (`woodwork domain`) sértetlen; architect terminál változatlan token/port MCP `initialize` OK.

**VPS tájékoztatva** (MSG-SPACEOS-002, elküldve a döntés pillanatában, a megvalósítás előtt).

**Nyitott**: a 8 nem-root terminál egyike sem kapott Doorstar-hozzáférést — ha valamelyiknek Doorstar-specifikus munkája lesz, egy második MCP-bejegyzést kell kapnia (kis, célzott follow-up).

### ⚠️ 2026-07-11 — BIZTONSÁGI INCIDENS + HELYREÁLLÍTÁS: GitHub repók + token-szivárgás

Gábor kérésére a 3 lokál mappa (Cabinet_bilder_scripts → **spaceos-cad-core**, knowledge-service-0.0.01 → **spaceos-cad-nexus**, knowledge-service-doorstar → **spaceos-cad-doorstar**) publikus GitHub repót kapott a Szantoi fiók alatt, a VPS 4-repós mintáját követve (l. [[vps-github-repok]] személyes memória). A "helyi fejlesztés a VPS-fejlesztés hardver-kötött része" elv alapján (l. [[lokal-fejlesztes-hardver-kotott-resze]]).

**Incidens:** az első push után kiderült, hogy a `spaceos-cad-core` publikus commit-jaiban **valódi, élő bearer tokenek** voltak (`docs/knowledge/federacio_atadas_vps_root.md`-ben és a backup-másolatában: a **cabinet-bridge** és **vps-bridge** federációs tokenek; `.claude/settings.local.json`-ban és `scratch/`-ban régebbi, már nem élő claude-main/conductor tokenek). A `spaceos-cad-nexus`/`spaceos-cad-doorstar` repókban egy előre létező, hardcode-olt fallback token is volt a `bin/stdio-bridge.js`-ben (nem élő, de rossz gyakorlat).

**Azonnali intézkedés:** mindhárom repó privátra zárva a felfedezés pillanatában.

**Teljes helyreállítás (ugyanaznap):**
1. VPS értesítve (MSG-SPACEOS-001, kritikus prioritás) a cabinet-bridge token kompromittálódásáról, kérve az ő oldali rotációjukat — **✅ MEGTÖRTÉNT**: a VPS rotálta, az új tokent Gábor relézte, `.mcp.json` (root + gyökér) frissítve, `poll-federation-inbox.ps1` hibamentesen fut vele.
2. `vps-bridge` token (ezt MI validáljuk) azonnal rotálva lokálban, az új érték elküldve a VPS-nek (MSG-SPACEOS-002).
3. `claude-main` és `conductor` tokenek rotálva (elővigyázatosságból, bár a git historyban talált értékek már eleve nem voltak élők) — `agents.yaml` (mindkét knowledge-service példány), `.mcp.json` (root + gyökér), `terminals/conductor/.agents/mcp_config.json` frissítve, hot-reload-dal ellenőrizve (régi token elutasítva, új elfogadva).
4. A leaked tartalom redaktálva minden trackelt fájlból (MEMORY.md, outbox, federacio_atadas_vps_root.md).
5. `.gitignore` szigorítva mindhárom repóban: `.claude/settings.local.json`, `scratch/`, `docs/knowledge.pre-split-backup-*/` kizárva.
6. `bin/stdio-bridge.js` hardcode-olt fallback tokenje eltávolítva (fail-closed, kötelező env var), teszt-fixture placeholderre cserélve.
7. Mindhárom repó git történelme **egyetlen tiszta commit-ra nullázva** (orphan branch + force-push) — a szennyezett history-t senki sem tudja többé elérni a GitHubon. (Nem repó-törlés: az ahhoz szükséges `delete_repo` GitHub-scope tartós jogosultság-bővítés lett volna, ezt Gábor elutasította, helyette a force-push megoldást választotta.)
8. Mindhárom repó visszaállítva publikusra (Gábor eredeti kérése).

**Tanulság:** mielőtt bármilyen mappát publikus repóba pusholunk, **előbb** teljes `git grep`-es token-seprést kell futtatni (base64-minta, `[A-Za-z0-9+/]{40,44}=`, kizárva `package-lock.json`/`node_modules` a zaj miatt), NEM utólag. A `.claude/settings.local.json` és bármilyen `scratch/`-jellegű mappa alapból gitignore-listás legyen minden új repónál a kezdetektől.

### Nyitott szálak
- [ ] VPS: tar.gz-k újraküldése egyedi ID-kkal → utána: kicsomagolás + ingest + governance-szabvány átvétele
- [ ] Flotta: watcher-prompt egyszerűsítés + retry-limit + agy-log; FLOTTA-TESZT-001 újrafuttatás tiszta configgal → identitás-bizonyítás
- [ ] EPIC-CB-MCPHOST: conductor (MSG-CONDUCTOR-002) és architect (MSG-ARCHITECT-001) taskok feldolgozásra várnak — a flotta-teszt sikere az előfeltétel
- [ ] VPS-válasz a MSG-ROOT-021 integrációs kérdésekre (Cutting API, katalógus, identity) — még nem jött
- [ ] Avast visszakapcsolása kivétellel; Chroma-perzisztencia lokálban

## 📋 SESSION 2026-07-07 (2) — Fejlesztés-indítás: EPIC-CB-MCPHOST + VPS-módszertan átvétele

**Status:** ✅ Terv + csapat-dispatch kész · ⏳ VPS-válasz az integrációs kérdésekre (MSG-ROOT-021), architect spec folyamatban

### Gábor direktívái
Fejlesztések indítása; a VPS-nek bemutatni, mit építünk lokálisan és hogyan épül be az ő fejlesztéseikbe; helyi csapat felépítése; goal-definiálási módszerek + skillek lekérése a VPS-ről (a tervezés kiemelten fontos).

### VPS-módszertan átvéve
- **Goal-séma** (list_goals éles példákból): completion_criteria (done_outbox+pattern), on_complete láncolás, expires_at, epic_id — dokumentálva: `docs/knowledge/federation/GOAL_DEFINING_METHOD_VPS.md` (+ Goal Drift 5 hibamódja)
- **Skillek lementve** `.claude/skills/`-be: `project-setup` (epic/task struktúra, dispatch-sorrend: explorer→architect→designer→backend→frontend), `create-implementation-plan` (determinisztikus tervsablon). A VPS-en 36 skill van — továbbiak igény szerint (spaceos-conductor, spaceos-terminal, cqrs-handler-generator, ddd-arch-planner a legrelevánsabbak).

### CabinetBilder felmérés (Explore-riport lényege — RÉSZLETEK A RIPORTBAN!)
- .NET **10.0.102**, minden projekt net10.0; solution: `.slnx` (Cli NINCS benne!); MediatR 12.4.1; **KÉT Result-típus**: Ardalis.Result (handlerek) + Core.Common.Result (Skeleton)
- **AutoCAD-csapda**: az Adapter csak AutoCAD 2027-tel buildel (ValidateAutoCadPath gate) + a sync-handlerek Postgres RemoteDbContextet várnak → **a McpHost SOHA ne referáljon az Adapterre**
- AutoCAD-mentesen elérhető MA: Skeleton domain (ComputeBom!), SyncManager, ILocalStore (SQLite client.db), ISpaceOsClient (HTTP), DeviceCodeAuthenticator
- **Integrációs arany**: a HttpSpaceOsClient már definiálja a VPS-irányú műveleteket — PullMaterials/PullTemplates (katalógus) + SubmitCuttingSheet/BOM (az ő Cutting moduljuk bemenete!)

### Elindított munka
- **Terv**: `plan/feature-mcphost-cabinetbilder-1.md` (VPS-sablon szerint; 3 fázis, TASK-001..010, 2 Result-típus CON-ként, OutboxWorker RISK-ként)
- **VPS-nek bemutatkozó + 4 integrációs kérdés** (MSG-ROOT-021): Cutting API kontrakt? katalógus source of truth? identity.spaceos.io él? goal-rendszer újabb iterációi?
- **Lokál dispatch**: conductor (MSG-CONDUCTOR-002: jóváhagyás + goal-létrehozás VPS-sémával + fázis-dispatch) · architect (MSG-ARCHITECT-001: Phase 1 spec, output: docs/specs/mcphost-tool-contracts-v1.md, DONE-pattern: *mcphost*spec*done*)

## 📋 SESSION 2026-07-07 — Federációs embedding-szabvány: Xenova mindkét szigeten

**Status:** ✅✅ TELJES — cross-island szemantikus keresés MINDKÉT oldalon üzemel

### 🎉 Lezárás (2026-07-07 ~22:20)

- A VPS javította a search_knowledge MCP-hibát (gyökérok: a fix csak a dist/-be került, src/ nélkül → a következő build felülírta; + a vectorStore nem használta a XenovaEmbeddingFunction-t). Restart után (PID 81764, 1857 dok) ✅
- Cabinet-újrateszt a hídon: referencia-query PASS (0.5535, egyező eredmény), MAGYAR query is releváns találatokat ad → cross-lingual semantic search a hídon át él ✅ (MSG-ROOT-020-ban visszaigazolva)
- **A federációs tudásmegosztási alap-infra ezzel KÉSZ**: root↔root üzenetek + fájlátvitel (sha256) + kétirányú szemantikus keresés azonos embedding-térben.
- Következő javasolt lépés (VPS-nek jelezve): az ERP dokumentumkezelési/tervezési sémák kijelölése és átvétele (FILE-TRANSFER vagy híd-keresés).

### A VPS "minőségi döntése" (MSG-CABINET-BRIDGE-007, VPS Root "Sárkány")

- A mi C opciónk (szerver-oldali chroma embedding) **náluk nem működött**: a chromadb npm kliens VAGY embeddingFunction-t, VAGY explicit vektorokat vár — "server-side only" mód NINCS. (Nálunk csak azért "működött", mert Chroma nem is fut → in-memory + kulcsszó-fallback!)
- **Federációs szabvány: `@xenova/transformers`** — kliens-oldali ONNX, `Xenova/all-MiniLM-L6-v2` (384 dim, mean pooling + L2 normalize). Se Sharp, se Python, se külső API. VPS: production, 1857 dok.
- Gábor követelménye: "A lokális is legyen minőségi szinten megoldva. Kell a szemantikus keresés."

### Cabinet-implementáció (KÉSZ)

- `@xenova/transformers ^2.17.2` telepítve; `embeddings.ts` átírva: default = Xenova (valódi vektorok), Voyage env-kulccsal felülbírálható. `vectorStore.ts` változatlan (vektorokkal automatikusan cosineSim ág). ESM-import CJS-ből: `eval('import(...)')` trükk.
- `/health`: `"embeddingBackend": "xenova-local (Xenova/all-MiniLM-L6-v2)"`, 450 dok. Első betöltéskor ~30 MB modell-letöltés (transformers cache).
- **Szemantikus teszt PASS**: angol query magyar multi-agent doksit talált (cross-lingual match — kulcsszóval 0 találat lett volna).
- VPS-nek visszaigazolva (MSG-ROOT-019) + jelezve: a search_knowledge MCP-tool a hídon NÁLUK még mindig a régi embedding-hibát adja, pedig a REST /api/knowledge/search már állítólag jó — ellenőrzést kértünk.
- Emlékeztető: a lokál szerver kézi indítású — session-váltásnál újra kell indítani (`PORT=13457 node dist/server.js` vagy start-local-island.ps1).

### ✅ FÁJLÁTVITEL A HÍDON — élesben tesztelve (2026-07-06 ~22:10)

- **Konvenció**: `send_message` content = `[FILE-TRANSFER] name=..; part=n/N; sha256=..; encoding=base64` fejléc + base64 szelet. Szeletelés a TELJES fájl base64-éből (~87k karakter/szelet ≈ 64 KB nyers) — bájt-szeletelés TILOS (közbenső padding!). Ez volt az első teszt hibája is (FromBase64String error), javítva.
- **Szkriptek**: `send-federation-file.ps1` (küldés, darabolás, hash) · `poll-federation-inbox.ps1` (fogadás: staging `scratch/fed-files/`, összerakás sorrendhelyesen, sha256-ellenőrzés → `terminals/root/inbox/files/`) · `ingest-federation-knowledge.ps1` (jóváhagyás után → `Development/docs/knowledge/federation/` + reindex POST /api/knowledge/index).
- **Teszt PASS**: 100 KB, 2 szelet, fordított érkezési sorrend kezelve, sha256 OK. Encoding-fix: minden bridge-hívás explicit UTF-8 (`charset=utf-8` + UTF8.GetBytes) — a VPS � karakter-panasza így megoldva.
- **Értesítés-mechanizmus**: session alatt háttér-watcher (bash loop, exit új üzenetnél → harness felébreszt); sessionök között poller loop/Task Scheduler + inbox-check session-startkor; később SSE `/api/mailbox/:terminal/subscribe`.
- ⚠️ PS 5.1 lesson: a .ps1 fájlokba CSAK ASCII szöveg menjen (ékezet/em-dash BOM nélküli UTF-8-ban parser-hibát okoz)!
- A VPS `search_knowledge` a restartjuk után is hibás (No embedding function) — továbbra is az ő javításukra vár.

### RAG-megosztás felmérés (2026-07-06 ~22:15)

- **Federált keresés elvben MÁR MŰKÖDIK**: a `search_knowledge` "all" engedélyű → a cabinet-bridge átkérdezheti a VPS RAG-ját a hídon (és a hívás a federation-logba kerül).
- ⚠️ **ÉLES TESZTEN A VPS RAG HIBÁS**: `search_knowledge` a hídon át → "No embedding function found for collection 'spaceos-knowledge'" — VPS-oldali chroma embedding-function gond, a VPS rootnak jelezve. A lokál RAG (13457) keresése működik.
- Javaslat (Gáborral egyeztetendő): hibajavítás után **hibrid modell** — ad-hoc kérdésekre federált keresés a hídon; a kötelezően követendő ERP-sémákra szelektív DOKUMENTUM-szinkron (nem vektor-szinkron!) + lokál újraindexelés. Embedding-modell azonos (all-MiniLM-L6-v2), de vektor-DB megosztás fölösleges csatolás. Lokál előfeltétel: in-memory → perzisztens chroma backend váltás.

### ✅✅ ROUND-TRIP KÉSZ (2026-07-06 ~22:00) — kétirányú federáció ÜZEMEL

- **VPS root visszaigazolta** (MSG-CABINET-BRIDGE-002): MSG-ROOT-010 megérkezett, token+engedély validálva, InboxWatcher detektálta, MCP-log rögzítette, governance elfogadva. Round-trip: **PASS**.
- **Poller elkészült**: `Cabinet_bilder_scripts/poll-federation-inbox.ps1` — a VPS-beli cabinet-bridge inboxot pollozza (UNREAD), behúzza a lokál `terminals/root/inbox`-ba `FED_` prefixszel, logol a `logs/federation-poll.log`-ba. Token/URL forrása: a root `.mcp.json` spaceos-knowledge-vps bejegyzése. Használat: `-Once` vagy loop (`-IntervalSeconds 120`). FONTOS: a list_inbox üzeneteknél az ID a `frontmatter.id`-ban van!
- Hátralévő apróságok: VPS-oldali audit-patch (folyamatban ott), Avast visszakapcsolása kivétellel, poller ütemezése (Task Scheduler vagy loop indítása).

### 🎉 CSATORNA ÉLES (2026-07-06 késő este) — első sziget-közi üzenet átment!

- **Lokál→VPS ✅**: `send_message(to="root")` a cabinet-bridge tokennel HTTPS-en → **MSG-ROOT-010** landolt a VPS `/opt/spaceos/terminals/root/inbox/`-ában.
- **Visszairány ✅ validálva**: `list_inbox(terminal="cabinet-bridge")` működik a hídon át (üres, valid inbox) — a VPS root `send_message(to="cabinet-bridge")`-dzsel válaszol, mi ezt pollozzuk.
- VPS oldal kész: token regisztrálva, send_message engedély, auth-teszt PASS (82 tool). Hátra: VPS-oldali audit-patch (a kompakt telepítő-blokkot megkapták) + átadási doksi referenciának.
- Hívás-recept (lokál root): `curl -4 -X POST https://datahaven.joinerytech.hu/mcp/ -H "Authorization: Bearer <cabinet-bridge-token>" ...` — a token a `.mcp.json` spaceos-knowledge-vps bejegyzésében.

### Csatorna-státusz (2026-07-06 késő este)

- ✅ VPS `/mcp/` publikálva: **https://datahaven.joinerytech.hu/mcp/** (nginx: `/etc/nginx/sites-enabled/joinerytech` ~352. sor, proxy_read_timeout 120s, backup készült). GET → info JSON, kívülről ellenőrizve.
- ✅ Avast leállítva a lokál gépen → domain elérhető (Let's Encrypt cert). TODO: Avast vissza + kivétel a domainre.
- ✅ Lokál `.mcp.json`-ok: `spaceos-knowledge-vps` → a domainre + cabinet-bridge tokenre átállítva (a claude-main-es VPS-bejegyzés megszűnt, claude-main identitás nem kell).
- ⏳ UTOLSÓ LÉPÉS: cabinet-bridge token regisztrációja a VPS agents.yaml-ba + send_message engedély + audit-patch (a VPS root az átadási doksira vár). Utána: első éles [CABINET→VPS] üzenet tőlünk.
- Mellékinfó: a VPS-oldali dokumentáció-domain: https://nexus.joinerytech.hu

### VPS-oldali teendők (a VPS root végzi az átadási doksi alapján)

1. `cabinet-bridge` token bejegyzése a VPS agents.yaml-ba + jogosultságok (send_message, create_task) az ottani tool-permissions.yaml-ban
2. Federációs log-patch átadása a VPS-csapatnak (ugyanaz az mcp.ts minta)
3. Domain kiderítése (nginx `server_name`) → a cabinet-bridge HTTPS-en, a domainen át hívja a VPS-t (dedikált, tokenes, loggolt csatorna); addig fallback: a 3456-os SSH-forward
4. Irány VPS→lokál: a lokál gép nem publikus → a VPS a "közös postahivatal", a cabinet-bridge POLLOZZA a saját VPS-oldali inboxát (később: watcher script)

---

## 📋 SESSION 2026-07-06 (2) — Lokális sziget: új tokenkészlet + cabinet-bilder csoport + 3457-es port

**Status:** ✅ KÉSZ — a lokális knowledge-service saját tokenekkel fut a 3457-en, tesztelve

### Ami történt

1. **Új tokenkészlet a LOKÁLIS szerverhez** (11 db, openssl rand -base64 32): master + 9 terminál (root, conductor, architect, librarian, explorer, backend, frontend, designer, monitor) + claude-main. A tokenek a `knowledge-service-0.0.01/config/agents.yaml`-ban és a kliens-configokban vannak — ide szándékosan nem másoljuk be őket.
2. **Csoportazonosító**: `cabinet-bilder` group az agents.yaml `groups:` szekciójában — a teljes lokális flotta + claude-main. A marketing példa-tokenek törölve.
3. **Port-szeparáció**: lokális szerver a **3457**-en fut (`PORT=3457`), indító: `knowledge-service-0.0.01/start-local-3457.ps1`. A 3456 marad a VPS-forward.
4. **Kliens-configok frissítve** (mind → localhost:3457 + saját új token):
   - 9× `Cabinet_bilder_scripts/terminals/<t>/.agents/mcp_config.json` (Antigravity formátum)
   - `terminals/root/.mcp.json` és `Cabinet_bilder_scripts/.mcp.json`: `spaceos-knowledge` → lokál 3457 (új claude-main token), ÚJ `spaceos-knowledge-vps` bejegyzés → 3456 (régi claude-main token, VPS-regisztráció még PENDING)
5. **Teszt (3457)**: `/health` OK (450 doksi, in-memory vector backend, `port:3457` a válaszban) · új claude-main token → HTTP 200 · új conductor token → 200 · régi (VPS-sel közös) token → 403 ✅ a két példány már megkülönböztethető

### Fontos következmények

- A régi master token (`IoUpLUgr...`) már CSAK a VPS-en érvényes; lokálisan az `~/.gemini/config/mcp_config.json` (Antigravity) még ezt használja a 3456 felé — a master→per-terminál csere továbbra is TODO.
- Az MCP-kapcsolat (spaceos-knowledge, 3457) a Claude Code session ÚJRAINDÍTÁSA után töltődik be.
- Lokális vector backend: in-memory (a VPS-é chroma) — indexelés/perzisztencia később ellenőrizendő.

---

## 📋 SESSION 2026-07-06 — Claude Code MCP integráció + VPS/lokál topológia felderítés

**Status:** ✅ Elemzés és lokál setup KÉSZ, VPS-oldali token-regisztráció PENDING (user-döntés)

**Résztvevő:** Claude Code (Fable 5) — új, saját identitással: `claude-main`

### Kritikus felfedezés: a 3456-os port VPS-forward

| Tény | Bizonyíték |
|------|-----------|
| A `localhost:3456` NEM a lokális knowledge-service | Lokális `agents.yaml` szerkesztés hatástalan volt (hot-reload ellenére 403) |
| VS Code Remote-SSH forwardolja a `spaceos-gabor` VPS 3456-os portját | Listener = Code.exe NodeService utility process; 2 aktív ssh.exe kapcsolat a `spaceos-gabor` hostra |
| A futó szerver a VPS-build | Terminál-listájában `chat-root` és `reviewer` szerepel, ami a lokális 0.0.01 forrásban nincs |
| A lokális knowledge-service-0.0.01 jelenleg NEM fut | Nincs saját node listener a 3456-on |

**Lesson:** Két példány (lokál dev + VPS prod) azonos tokenekkel és azonos porton megkülönböztethetetlen. Diagnózis előtt mindig: `Get-Process ssh` + a `/health` válasz önmagában NEM elég (nincs benne instance-azonosító).

### claude-main identitás (új agent a flottában)

| Elem | Állapot |
|------|---------|
| Token generálva (32 byte, base64) | `<régi, már rotált token — érték eltávolítva>` |
| Regisztrálva LOKÁL `knowledge-service-0.0.01/config/agents.yaml` | ✅ (`"<token>": "claude-main"`) |
| Regisztrálva VPS agents.yaml | ⏳ PENDING — SSH-t a permission-rendszer tiltotta, user futtatja vagy engedélyezi |
| Klienskonfig: `Cabinet_bilder_scripts/.mcp.json` → `spaceos-knowledge` | ✅ natív HTTP transport, Bearer token |
| Jogosultsági szint | Effektíve read+report (create_task/send_message root/conductor-only marad) |

**VPS-regisztráció egy lépésben** (user futtatja):
```bash
ssh spaceos-gabor
# agents.yaml megkeresése (várhatóan /opt/spaceos/spaceos-nexus/knowledge-service/config/agents.yaml)
# az agents: szekcióba beszúrni:
#   "<régi, már rotált token — érték eltávolítva>": "claude-main"
# 30 mp-en belül hot-reload, restart nem kell
```

### Antigravity CLI integráció — állapot

- Antigravity 2.0 óta a CLI (`agy`) és az IDE közös configot olvas: `~/.gemini/config/mcp_config.json` (`serverUrl` kulcs, standard MCP séma).
- A spaceos-knowledge **már be van kötve** oda — de a **MASTER (root) tokennel**, `mcp-remote` wrapperen át. ⚠️ Minden agy-session root-ként fut → per-terminál tokenre cserélendő, wrapper helyett natív `serverUrl` + `headers`.
- A per-terminál `terminals/*/.agents/mcp_config.json` fájlok formátuma már Antigravity-kompatibilis.

### Stratégiai döntések / irányok (Gáborral egyeztetve)

1. **CabinetBilder CLI → MCP host**: nem a CLI-t alakítjuk át (az ma vékony: diagnose+login+sync-stub), hanem a `CabinetBilder.Core` fölé épül MCP host a hivatalos C# SDK-val (`ModelContextProtocol` NuGet). A CQRS requestek (GetSmartObjectMetadataQuery, PushMetadataCommand, …) ~1:1 tool-definíciók. Cél: humán (AutoCAD UI) és agent (MCP tool) ugyanazokat a use-case handlereket hívja.
2. **Sziget-architektúra**: minden projekt/munkaegység saját knowledge-service példányt ("szigetet") kap; minden szereplő (Claude, agy, CabinetBilder MCP-host, másik sziget) ugyanazon a token-alapú MCP HTTP interfészen csatlakozik. Sziget-közi kommunikáció = MCP HTTP + federation token, SSH-tunnel/Tailscale fölött.
   - **Élesítve 2026-07-11** (VPS-tanulság — l. [[vps-4-sziget-atszervezes-oka]] személyes memóriában): a VPS-en egy szerver/egy közös RAG szolgálta ki egyszerre a nexus- és a JoineryTech-fejlesztést, ezért egy lekérdezésnél a két domain adatai összekeveredtek. Gábor döntése: **nálunk is projektenként külön tudástár és agent-management kell**, a sziget-közi kommunikáció (federáció) áthidalja, DE elválasztva tartja a tudást. TODO: a jelenlegi lokál knowledge-service (13457) egy közös indexbe tölti a saját CabinetBilder-anyagot és a `docs/knowledge/federation/`-be beolvasztott VPS-tudást — ezt izolálni kell (külön collection/namespace projektenként), mielőtt több projekt kerül a rendszerbe.
3. **ERP-séma követés**: a VPS-en futó csapat a faipari ERP-t fejleszti — a lokális (CabinetBilder) csapatnak az ottani dokumentumkezelési és tervezési sémákat KELL követnie, hogy a tudásmegosztás működjön.
4. **Datahaven-first elv**: minden projektnél először a tudásközpont épül ki; a keletkezett tudás fájlokban + adatbázisokban + RAG-ban tárolódik → "az ősök vállán állva" nőnek a terminálok.

### TODO / Pending

- [ ] **claude-main token regisztráció a VPS-en** (user: ssh spaceos-gabor, agents.yaml)
- [ ] `/health` + `get_service_status` kapjon `instance`/`project`/`environment` mezőt (sziget-előfeltétel)
- [ ] Antigravity központi config: master token → per-terminál token, mcp-remote → natív serverUrl
- [ ] Explicit `LocalForward 3456 localhost:3456` a `~/.ssh/config` spaceos-gabor bejegyzésébe (VS Code-független tunnel)
- [x] Lokális dev knowledge-service külön porton (3457) — ✅ 2026-07-06 (2) session, saját tokenkészlettel
- [ ] CabinetBilder MCP-host PoC: 4-5 olvasó tool (list_smart_objects, get_smart_object_metadata, get_skeleton, get_sync_status, generate_bom)
- [ ] Titkok kivezetése a configokból env-be (MCP_AUTH_TOKEN, MCP_TOKEN_<NAME> már támogatott)

**Kapcsolódó tudásdokumentum:** `docs/knowledge/multi_agent_mcp_integracio_rag.md`

### 2026-07-11 (késő este) — ÚJ, MÁSODIK federációs csatorna: PostgreSQL Messaging REST API

A VPS a `federation.messages` központi PostgreSQL táblára (l. [[vps-szeparalt-kozpontositas-nexus]] személyes memória) épített egy sima REST endpointot, ami **kiváltja/kiegészíti** az eddigi MCP `send_message`/`list_inbox` hívásokat:

- **Endpoint**: `POST https://datahaven.joinerytech.hu/api/messages/send`
- **Auth**: ugyanaz a `cabinet-bridge` bearer token, mint az MCP-nél (`terminals/root/.mcp.json` → `spaceos-knowledge-vps` → `Authorization`).
- **Body**: `{from_island, from_terminal, to_island, to_terminal, msg_type, priority, subject, body}` — itt `from_island: "cabinet"`, `to_island: "nexus"` (nem "spaceos"/"cabinet-bridge" mint az MCP-nél — MÁS címzési séma!).
- **Élesben tesztelve** (2026-07-11 21:42): `HTTP 201`, valós UUID-vel visszaigazolva.

**Előny a jelenlegi Claude Code session MCP-kapcsolatához képest**: ez egy sima `curl`/`Invoke-RestMethod` hívás, NEM szenved a "session-szintű MCP-kapcsolat a régi, gyorsítótárazott tokent használja" problémától (l. a cabinet-bridge token-csere incidens tanulsága fentebb) — minden hívás friss processz, friss `.mcp.json`-beli tokent olvas.

**Nyitott**: nincs még bejövő (VPS→Cabinet) megfelelője letesztelve ezen az API-n (a `poll-federation-inbox.ps1` továbbra is a régi MCP `list_inbox`-ot használja) — érdemes lenne rákérdezni, van-e `GET /api/messages/inbox`-szerű végpont is, hogy a pollert is erre lehessen átállítani.

**Talált API-bug (2026-07-11 21:50) — ✅ JAVÍTVA, deploy-olva (21:57):** a VPS dokumentációja top-level `"body"` (string) mezőt ír elő, de eredetileg ez nem került mentésre — a ténylegesen eltárolt tartalom a `"payload"` objektumban volt. Jelezve a VPS-nek (üzenet ID: dc1225c6-df17-4322-a5f4-30f569788138), ők javították: mostantól **mindkét formátum működik** — top-level `body: "..."` automatikusan `payload: {body: "..."}`-ba kerül, VAGY közvetlenül `payload: {body: "..."}` is küldhető. Visszaigazolva egy teszt-üzenettel (id: 6f284ec8-1b46-4f29-a725-6c58d0665eec).
