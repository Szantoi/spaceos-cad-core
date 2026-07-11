# Projekt Setup Workflow — Lessons Learned

> **Frissítve:** 2026-07-01
> **Projekt:** joinerytech-prod setup tapasztalatok

## Fennakadások és Megoldások

### 1. MCP create_project felülírja a manuális fájlokat

**Probléma:** Az mcp create_project tool felülírja a TASKS.yaml-t üres milestone-okkal, ha már létezik a mappa.

**Megoldás:** 
- VAGY először MCP-vel hozd létre a projektet, MAJD szerkeszd a TASKS.yaml-t
- VAGY mentsd el a részletes TASKS.yaml-t máshova, és az MCP hívás után írd vissza

### 2. Knowledge Service RAG nem működik (ChromaDB embedding)

**Probléma:** search_knowledge hibát dob - No embedding function found.

**Workaround:** Dokumentumok közvetlen olvasása a Read tool-lal.

### 3. Project registry vs filesystem mismatch

**Probléma:** Az MCP get_project_status "Project not found" hibát ad, ha a projektet manuálisan hoztad létre.

**Megoldás:** Mindig az MCP create_project tool-lal hozd létre a projektet.

### 4. Checkpoint subscription-ök nem jönnek létre automatikusan

**Probléma:** A refresh_checkpoint_subscriptions 0 subscription-t hozott létre targetId nélkül.

## Workflow Értékelés

| Lépés | MCP Tool | Működik? |
|-------|----------|----------|
| Projekt létrehozás | create_project | OK |
| Projekt státusz | get_project_status | OK (csak MCP projektekre) |
| Checkpoint lista | get_checkpoint_status | OK |
| RAG keresés | search_knowledge | HIBA |
| Task dispatch | dispatch_next | Nem tesztelve |

## Konklúzió

A workflow összességében működik, de van friction az EPICS.yaml és TASKS.yaml kézi szerkesztésénél.
