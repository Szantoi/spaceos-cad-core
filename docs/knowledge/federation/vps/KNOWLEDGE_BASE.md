# SpaceOS Knowledge Base Architecture — ADR-040

> **Decision Record:** PostgreSQL tsvector FTS + MCP Tool Interface (Phase 1)
>
> **Document Version:** v3 (2026-06-17, Architect review complete)
>
> **Status:** DESIGN ✅ — Ready for INFRA/ORCH implementation

---

## Executive Summary

SpaceOS knowledge base (103 fájl, ~556 KB) uses **PostgreSQL full-text search (tsvector)** instead of vector embeddings.

**Why?** Walking Skeleton principle: FTS works at corpus <1000 files with minimal overhead. Upgrade path to pgvector + Voyage-3 exists if corpus grows.

**Architecture:**
```
docs/knowledge/ + terminál memóriák
    ↓
[Node.js ingestion script — parameterized pg queries, SHA-256 hash tracking]
    ↓
PostgreSQL spaceos_knowledge DB, knowledge.documents table (TSVECTOR index)
    ↓
[MCP Server: knowledge_search() + knowledge_read() tools]
    ↓
Claude Code sessions, planning scanner, Haiku tool use
```

---

## Data Model

### Database Schema

```sql
-- Database: spaceos_knowledge (port 5433, isolated from Kernel DB per ADR-039)
CREATE SCHEMA knowledge;

CREATE TABLE knowledge.documents (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    file_path       TEXT NOT NULL UNIQUE,          -- 'knowledge/security/SECURITY_PATTERNS.md'
    source_type     TEXT NOT NULL CHECK (source_type IN ('knowledge', 'memory')),
    category        TEXT,                          -- 'security', 'architecture', 'deployment', 'patterns', 'context'
    terminal        TEXT,                          -- 'kernel', 'orch', 'joinery', etc (from memory/)
    title           TEXT NOT NULL,
    content         TEXT NOT NULL,
    content_tsvector TSVECTOR GENERATED ALWAYS AS (
        to_tsvector('simple', coalesce(title, '') || ' ' || content)
    ) STORED,
    content_hash    TEXT NOT NULL,                 -- SHA-256 for incremental ingestion
    word_count      INT NOT NULL DEFAULT 0,
    indexed_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Row-level security (SpaceOS pattern enforcement)
ALTER TABLE knowledge.documents ENABLE ROW LEVEL SECURITY;
CREATE POLICY admin_full_access ON knowledge.documents FOR ALL USING (true);

-- Indexes
CREATE INDEX idx_documents_tsvector ON knowledge.documents USING GIN (content_tsvector);
CREATE INDEX idx_documents_source ON knowledge.documents (source_type);
CREATE INDEX idx_documents_category ON knowledge.documents (category);
CREATE INDEX idx_documents_terminal ON knowledge.documents (terminal);
```

### Chunking Strategy

**Phase 1:** No chunking. Current files <30 KB; entire document indexed.

**Phase 2 (if files >50 KB):**
```sql
CREATE TABLE knowledge.chunks (
    id          UUID PRIMARY KEY,
    document_id UUID REFERENCES knowledge.documents(id) ON DELETE CASCADE,
    chunk_index INT NOT NULL,
    content     TEXT NOT NULL,
    content_tsvector TSVECTOR GENERATED ALWAYS AS (to_tsvector('simple', content)) STORED
);
```

---

## Ingestion Pipeline

### Node.js Ingestion Script (`rag-ingest.js`)

**Security:** Parameterized pg queries (SEC-P1 compliance). No bash psql injection risk.

**Algorithm:**
1. Scan `/opt/spaceos/docs/knowledge/**/*.md` + `/home/gabor/.claude/projects/*/memory/*.md`
2. For each file:
   - Calculate SHA-256 hash
   - Check DB: if hash changed → UPDATE, if new → INSERT, if deleted → DELETE
3. Metadata extraction:
   - `source_type`: "knowledge" or "memory" (from path)
   - `category`: subdirectory name (security, architecture, deployment, patterns, context)
   - `terminal`: project dir name for memory files (e.g., `-opt-spaceos-SpaceOS-Kernel` → "kernel")
   - `title`: first `# ` heading or filename

**Cron Trigger:**
```bash
0 */5 * * * /opt/spaceos/spaceos-nexus/knowledge-service/src/rag-ingest.js
# Runs after Librarian (every 5 hours)
```

**Termination logic:**
```bash
# Mark deleted files
DELETE FROM knowledge.documents
WHERE file_path NOT IN (
    -- current files in docs/knowledge/ and memory/
);
```

---

## Query Interface: MCP Tools

### Tool 1: `knowledge_search`

```typescript
{
  name: "knowledge_search",
  description: "Search SpaceOS knowledge base (docs/knowledge + terminal memories). Keyword-based full-text search.",
  inputSchema: {
    query: string,                              // e.g., "kernel deployment issues"
    source_type?: "knowledge" | "memory" | "all", // default: "all"
    category?: string,                          // e.g., "deployment"
    terminal?: string,                          // e.g., "kernel"
    limit?: number                              // default: 5, max: 20
  },
  output: {
    results: [{
      file_path: string,
      title: string,
      source_type: string,
      category: string,
      relevance_rank: number,
      snippet: string,                          // ~200 char context around match
      word_count: number
    }],
    total_matches: number
  }
}
```

**SQL Implementation:**
```sql
SELECT
    file_path, title, source_type, category,
    ts_rank(content_tsvector, query) AS relevance_rank,
    ts_headline('simple', content, query, 'MaxWords=30, MinWords=15') AS snippet,
    word_count
FROM knowledge.documents,
     to_tsquery('simple', $1) query  -- parameterized: user query → tsquery
WHERE content_tsvector @@ query
  AND ($2 IS NULL OR source_type = $2)
  AND ($3 IS NULL OR category = $3)
  AND ($4 IS NULL OR terminal = $4)
ORDER BY relevance_rank DESC
LIMIT $5;
```

### Tool 2: `knowledge_read`

```typescript
{
  name: "knowledge_read",
  description: "Read full content of a knowledge base document.",
  inputSchema: {
    file_path: string                           // from knowledge_search result
  },
  output: {
    content: string,
    metadata: {
      source_type: string,
      category: string,
      terminal: string | null,
      indexed_at: string,
      word_count: number
    }
  }
}
```

### Query Conversion (Natural Language → tsquery)

Simple heuristic (no NLP required):
```
"Kernel deployment issues"
→ to_tsquery('simple', 'kernel & deployment & issue')

"EF migration suppressTransaction"
→ to_tsquery('simple', 'migration & suppressTransaction')

"blocked errors memory"
→ to_tsquery('simple', 'blocked & error & memory')
```

---

## Implementation Sequence

| # | Task | Terminal | Dependencies | Estimated Days |
|---|------|----------|---|---|
| 1 | DDL: `spaceos_knowledge` DB + schema | INFRA | None | 0.5 |
| 2 | Node.js ingest script + parameterized queries | ORCH | DB ready | 1.5 |
| 3 | MCP server (knowledge_search + knowledge_read) | ORCH | Script ready | 2 |
| 4 | MCP registration in Claude settings.json | INFRA | MCP ready | 0.5 |
| 5 | Scanner cron integration | INFRA | All above | 0.5 |
| **Total** | | | | **~5 days (sequential)** |

**Dependency chain:**
```
1 → 2 → 3 → 4 → 5
```

---

## Upgrade Path (Phase 2+)

### If Corpus Grows >500 Files

1. Install pgvector: `CREATE EXTENSION vector;`
2. Add embedding column:
   ```sql
   ALTER TABLE knowledge.documents
   ADD COLUMN embedding vector(1024);
   ```
3. Update ingestion to call Voyage-3-lite API per document
4. Implement hybrid ranking: `(ts_rank + vector_rank) / 2`

**Why Voyage-3-lite?**
- Vendor alignment with Anthropic
- 1024 dimensions (efficient)
- Cost-effective (<$0.001 per document for 103 docs)

**Alternatives rejected:**
- OpenAI/Gemini embedding: 2nd/3rd vendor dependency
- ChromaDB: New Python process (VPS 8 GB constraint)
- sqlite-vec: No SQL integration benefit

---

## Security Review (v3)

| Issue | Severity | Status | Mitigation |
|---|---|---|---|
| **SEC-P1:** SQL injection in ingestion | MEDIUM | ✅ RESOLVED | Node.js parameterized queries, no bash psql |
| **SEC-P2:** Path traversal | LOW | ✅ ACCEPTED | Hardcoded paths, no user input |
| **SEC-P3:** PII in terminal memories | LOW | ✅ ACCEPTED | Technical docs only, no customer data |
| **SEC-P4:** MCP auth | LOW | ✅ ACCEPTED | stdio transport, local process, no network |

---

## Risk Matrix

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| FTS recall low (mixed Magyar/English) | MEDIUM | MEDIUM | 'simple' dictionary + stopwords; escalate to vector if needed |
| Memory file deletion → stale DB refs | LOW | LOW | Ingestion DELETE WHERE file_path NOT IN (...) |
| `spaceos_knowledge` DB unavailable | LOW | MEDIUM | Fallback: scanner `grep` pattern (graceful degradation) |
| MCP server crash | LOW | LOW | Fallback: grep; session can continue without RAG |

---

## What's NOT Included

- **Real-time indexing** — 5-hourly cron sufficient
- **Semantic search** — FTS adequate for corpus size
- **Chunk queries** — no chunking at Phase 1
- **Orchestrator API endpoint** — MCP tool replaces HTTP (no extra port)
- **Multi-tenant isolation** — knowledge base is platform-level, not tenant-scoped

---

## Referenced Documents

- `RAG_Knowledge_Base_v1.md` — Full specification (Architect)
- `ADR_CATALOGUE.md` — ADR-040 (proposed), ADR-039 (cross-module isolation)
- `DEPLOYMENT_RUNBOOK.md` — Knowledge Service setup steps (to be added)
- `KNOWN_GOTCHAS.md` — Common indexing pitfalls (to be added)
- `INFRA_CONTEXT.md` — Knowledge Service operational context (to be added)

---

**Status:** Ready for INFRA/ORCH implementation sprint.
