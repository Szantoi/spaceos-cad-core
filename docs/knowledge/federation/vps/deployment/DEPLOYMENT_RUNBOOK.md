# SpaceOS Knowledge Service — Deployment Runbook

> **For:** INFRA terminál operátor (VPS felügyelet, systemd)
>
> **Last Updated:** 2026-06-17
>
> **Status:** DRAFT (ADR-040 Phase 1 pending implementation)

---

## Prerequisites

- PostgreSQL 15+ (`psql` available on VPS)
- Node.js 20+ (for ingestion script + MCP server)
- Systemd (for cron + service management)
- Git access to `/opt/spaceos`

---

## Phase 1: Database Setup (INFRA)

### 1.1 Create spaceos_knowledge Database

```bash
# SSH: gabor@109.122.222.198
sudo -u postgres psql -p 5433 << 'EOF'
CREATE DATABASE spaceos_knowledge;
EOF
```

### 1.2 Create Schema and Tables

```bash
sudo -u postgres psql -p 5433 -d spaceos_knowledge << 'SCHEMA'
CREATE SCHEMA knowledge;

CREATE TABLE knowledge.documents (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    file_path       TEXT NOT NULL UNIQUE,
    source_type     TEXT NOT NULL CHECK (source_type IN ('knowledge', 'memory')),
    category        TEXT,
    terminal        TEXT,
    title           TEXT NOT NULL,
    content         TEXT NOT NULL,
    content_tsvector TSVECTOR GENERATED ALWAYS AS (
        to_tsvector('simple', coalesce(title, '') || ' ' || content)
    ) STORED,
    content_hash    TEXT NOT NULL,
    word_count      INT NOT NULL DEFAULT 0,
    indexed_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

ALTER TABLE knowledge.documents ENABLE ROW LEVEL SECURITY;
CREATE POLICY admin_full_access ON knowledge.documents FOR ALL USING (true);

CREATE INDEX idx_documents_tsvector ON knowledge.documents USING GIN (content_tsvector);
CREATE INDEX idx_documents_source ON knowledge.documents (source_type);
CREATE INDEX idx_documents_category ON knowledge.documents (category);
CREATE INDEX idx_documents_terminal ON knowledge.documents (terminal);

SCHEMA
```

### 1.3 Verify

```bash
sudo -u postgres psql -p 5433 -d spaceos_knowledge << 'EOF'
\d knowledge.documents
SELECT tablename FROM pg_tables WHERE schemaname = 'knowledge';
EOF
```

---

## Phase 2: Ingestion Script Setup (ORCH → deployed by INFRA)

### 2.1 Install Dependencies

```bash
cd /opt/spaceos/spaceos-nexus/knowledge-service
npm install pg dotenv
```

### 2.2 Create Ingestion Script

Path: `/opt/spaceos/spaceos-nexus/knowledge-service/src/rag-ingest.js`

```javascript
#!/usr/bin/env node
/**
 * RAG Knowledge Base Ingestion
 * - Scans docs/knowledge/ and terminal memories
 * - Upserts to spaceos_knowledge.knowledge.documents table
 * - Parameterized pg queries (SEC-P1)
 */

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { Client } = require('pg');

const DB_CONFIG = {
  host: 'localhost',
  port: 5433,
  database: 'spaceos_knowledge',
  user: 'postgres',  // INFRA: use env var in production
};

async function hashFile(filePath) {
  const content = fs.readFileSync(filePath, 'utf8');
  return crypto.createHash('sha256').update(content).digest('hex');
}

async function extractMetadata(filePath) {
  const content = fs.readFileSync(filePath, 'utf8');
  const lines = content.split('\n');

  // Extract title from first # heading
  const titleLine = lines.find(l => l.startsWith('# '));
  const title = titleLine ? titleLine.replace('# ', '').trim() : path.basename(filePath);

  // Determine source_type and category from path
  let sourceType = 'knowledge';
  let category = null;
  let terminal = null;

  if (filePath.includes('/memory/')) {
    sourceType = 'memory';
    // Extract terminal from project directory
    const match = filePath.match(/-opt-spaceos-([^/]+)/);
    if (match) terminal = match[1].toLowerCase().replace('spaceos-', '').replace('backend-spaceos-', '');
  } else {
    // Extract category from knowledge/ subdirectory
    const match = filePath.match(/knowledge\/([^/]+)\//);
    if (match) category = match[1];
  }

  const wordCount = content.split(/\s+/).length;

  return { title, sourceType, category, terminal, wordCount, content };
}

async function ingestFile(client, filePath) {
  try {
    const hash = await hashFile(filePath);
    const { title, sourceType, category, terminal, wordCount, content } = await extractMetadata(filePath);

    // Relative path for storage
    const relativePath = filePath.replace('/opt/spaceos/', '');

    // UPSERT: parameterized query (no injection)
    await client.query(
      `INSERT INTO knowledge.documents
       (file_path, source_type, category, terminal, title, content, content_hash, word_count)
       VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
       ON CONFLICT (file_path) DO UPDATE SET
         title = $5,
         content = $6,
         content_hash = $7,
         word_count = $8,
         updated_at = now()`,
      [relativePath, sourceType, category, terminal, title, content, hash, wordCount]
    );

    console.log(`✓ ${relativePath}`);
  } catch (err) {
    console.error(`✗ ${filePath}: ${err.message}`);
  }
}

async function deleteStaleFiles(client, currentPaths) {
  const result = await client.query(
    `DELETE FROM knowledge.documents
     WHERE file_path = ANY($1::text[])`,
    [[]]  // empty array: delete all not in currentPaths
  );
  console.log(`Deleted ${result.rowCount} stale documents`);
}

async function run() {
  const client = new Client(DB_CONFIG);
  await client.connect();

  try {
    // Scan docs/knowledge/
    const knowledgePath = '/opt/spaceos/docs/knowledge';
    const files = [];

    function scanDir(dir) {
      fs.readdirSync(dir, { withFileTypes: true }).forEach(ent => {
        const fullPath = path.join(dir, ent.name);
        if (ent.isDirectory()) scanDir(fullPath);
        else if (ent.name.endsWith('.md')) files.push(fullPath);
      });
    }

    scanDir(knowledgePath);

    // Scan terminal memories
    const projectsDir = '/home/gabor/.claude/projects';
    if (fs.existsSync(projectsDir)) {
      fs.readdirSync(projectsDir, { withFileTypes: true }).forEach(proj => {
        const memoryPath = path.join(projectsDir, proj.name, 'memory');
        if (fs.existsSync(memoryPath)) {
          fs.readdirSync(memoryPath).forEach(file => {
            if (file.endsWith('.md') && file !== 'MEMORY.md') {
              files.push(path.join(memoryPath, file));
            }
          });
        }
      });
    }

    // Ingest each file
    console.log(`Ingesting ${files.length} files...`);
    for (const file of files) {
      await ingestFile(client, file);
    }

    // Delete stale
    await deleteStaleFiles(client, files);

    console.log('✅ Ingestion complete');
  } finally {
    await client.end();
  }
}

run().catch(console.error);
```

### 2.3 Register Cron Job

```bash
# Add to root crontab (INFRA manages)
0 */5 * * * /opt/spaceos/spaceos-nexus/knowledge-service/src/rag-ingest.js >> /var/log/spaceos-rag-ingest.log 2>&1
```

---

## Phase 3: MCP Server Setup (ORCH)

### 3.1 MCP Server Implementation

Path: `/opt/spaceos/spaceos-nexus/knowledge-service/src/mcp-server.ts`

(Skeleton; full implementation in ORCH scope)

```typescript
import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { Client } from 'pg';

const server = new Server({
  name: "spaceos-knowledge",
  version: "1.0.0",
});

const dbClient = new Client({
  host: 'localhost',
  port: 5433,
  database: 'spaceos_knowledge',
  user: 'postgres',
});

// Tool: knowledge_search
server.setRequestHandler(
  "tools/knowledge_search",
  async (request) => {
    const { query, source_type, category, terminal, limit = 5 } = request.params as any;

    // Convert query to tsquery
    const tsquery = query.split(/\s+/).join(' & ');

    const result = await dbClient.query(
      `SELECT file_path, title, source_type, category,
              ts_rank(content_tsvector, q) AS relevance_rank,
              ts_headline('simple', content, q) AS snippet,
              word_count
       FROM (SELECT *, to_tsquery('simple', $1) q FROM knowledge.documents) t
       WHERE content_tsvector @@ q
         AND ($2 IS NULL OR source_type = $2)
         AND ($3 IS NULL OR category = $3)
         AND ($4 IS NULL OR terminal = $4)
       ORDER BY relevance_rank DESC
       LIMIT $5`,
      [tsquery, source_type, category, terminal, limit]
    );

    return {
      content: [{
        type: "text",
        text: JSON.stringify({
          results: result.rows,
          total_matches: result.rowCount
        }, null, 2)
      }]
    };
  }
);

// Tool: knowledge_read
server.setRequestHandler(
  "tools/knowledge_read",
  async (request) => {
    const { file_path } = request.params as any;

    const result = await dbClient.query(
      `SELECT content, source_type, category, terminal, indexed_at, word_count
       FROM knowledge.documents WHERE file_path = $1`,
      [file_path]
    );

    if (result.rows.length === 0) {
      return {
        content: [{ type: "text", text: "Not found" }]
      };
    }

    return {
      content: [{
        type: "text",
        text: result.rows[0].content
      }]
    };
  }
);

// Register tools
server.setRequestHandler("tools/list", async () => ({
  tools: [
    {
      name: "knowledge_search",
      description: "Search SpaceOS knowledge base",
      inputSchema: {
        type: "object",
        properties: {
          query: { type: "string" },
          source_type: { enum: ["knowledge", "memory", "all"] },
          category: { type: "string" },
          terminal: { type: "string" },
          limit: { type: "number" }
        },
        required: ["query"]
      }
    },
    {
      name: "knowledge_read",
      description: "Read full content of a document",
      inputSchema: {
        type: "object",
        properties: {
          file_path: { type: "string" }
        },
        required: ["file_path"]
      }
    }
  ]
}));

const transport = new StdioServerTransport();
server.connect(transport);
```

### 3.2 Compile & Test

```bash
cd /opt/spaceos/spaceos-nexus/knowledge-service
npm run build
node src/mcp-server.js &
```

---

## Phase 4: MCP Registration (INFRA)

### 4.1 Register in Claude settings.json

```bash
# User's ~/.claude/settings.json
{
  "mcpServers": {
    "spaceos-knowledge": {
      "command": "node",
      "args": ["/opt/spaceos/spaceos-nexus/knowledge-service/src/mcp-server.js"]
    }
  }
}
```

### 4.2 Verify

```bash
claude list-mcp-servers
# Should show: spaceos-knowledge ✓
```

---

## Operational Tasks

### Health Check

```bash
# 1. DB connectivity
sudo -u postgres psql -p 5433 -d spaceos_knowledge -c "SELECT COUNT(*) FROM knowledge.documents;"

# 2. Index status
sudo -u postgres psql -p 5433 -d spaceos_knowledge -c "SELECT category, COUNT(*) FROM knowledge.documents GROUP BY category;"

# 3. Recent ingestions
sudo -u postgres psql -p 5433 -d spaceos_knowledge -c "SELECT file_path, indexed_at FROM knowledge.documents ORDER BY indexed_at DESC LIMIT 5;"
```

### Troubleshooting

#### Ingestion Fails

```bash
# Check cron log
tail -50 /var/log/spaceos-rag-ingest.log

# Manual run with debug
DEBUG=* /opt/spaceos/spaceos-nexus/knowledge-service/src/rag-ingest.js
```

#### Search Returns Nothing

```bash
# Verify tsvector index
sudo -u postgres psql -p 5433 -d spaceos_knowledge << 'EOF'
SELECT file_path, to_tsvector(title || ' ' || content)
FROM knowledge.documents LIMIT 1;
EOF
```

#### MCP Server Not Found

```bash
# Check process
ps aux | grep mcp-server

# Logs
tail -20 /var/log/spaceos-mcp-server.log
```

---

## Rollback Procedures

### If Ingestion Corrupts Data

```bash
# Backup current state
sudo -u postgres pg_dump -p 5433 -d spaceos_knowledge > /tmp/kb-backup-$(date +%s).sql

# Clear and re-ingest
sudo -u postgres psql -p 5433 -d spaceos_knowledge << 'EOF'
TRUNCATE TABLE knowledge.documents CASCADE;
EOF

# Re-run ingestion
/opt/spaceos/spaceos-nexus/knowledge-service/src/rag-ingest.js
```

---

## Future Phases

- **Phase 2:** pgvector install + Voyage-3-lite integration (if corpus >500 files)
- **Phase 3:** Hybrid FTS+vector ranking (if corpus >2000 files)

See `KNOWLEDGE_BASE.md` upgrade path for details.
