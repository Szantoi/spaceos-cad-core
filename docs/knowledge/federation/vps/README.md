# VPS governance-csomag (federációs eredetű tudás)

**Forrás:** spaceos-gabor VPS (datahaven.joinerytech.hu), root→cabinet-bridge FILE-TRANSFER
**Érkezett:** 2026-07-08 (MSG-CABINET-BRIDGE-016 újraküldés, sha256-ellenőrzéssel hitelesítve)
**Ingest:** 2026-07-10, Claude Code (root terminál), Gábor jóváhagyásával

| Archívum | sha256 | Tartalom |
|----------|--------|----------|
| knowledge-base-full.tar.gz | `97d3d67c4289c91c...` | VPS docs/knowledge/** (api, architecture, by-role, context, datahaven, debugging, deployment, engineering, graph, market, patterns, security, snippets + INDEX/KNOWLEDGE_BASE) |
| architect-skills.tar.gz | `f63733415096a31b...` | 9 architect skill → átmásolva: `.claude/skills/` |
| code-design-strategy.tar.gz | `7d6edfbb036f63cc...` | design/ (Datahaven UI brief, bento-grid spec) + joinerytech/ (CRM/HR/Maintenance/QA/DMS domain modellek + domain kód) |

**Fontos:** ez a VPS-csapat tudása és szabványa — a lokális Cabinet-csapat KÖVETI ezeket a
sémákat (Gábor governance-döntése). A fájlokban szereplő útvonalak (/opt/spaceos/...) a VPS
környezetére vonatkoznak. Kihagyva az ingestből: reading-list/ és synthesis/ (dátumhoz kötött
pillanatképek), test-reindex.md, design HTML/zip assetek (nem-md; a staging/inbox őrzi).

A faipari ERP dokumentumkezelési sémája: `joinerytech/domain/DMS_DOMAIN_MODEL.md` —
ez a CabinetBilder dokumentum-kimenetek elsődleges referencia-sémája.
