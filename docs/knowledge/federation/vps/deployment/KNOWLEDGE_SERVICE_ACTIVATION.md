# Knowledge Service Activation Runbook

**Status:** VPS activation pending (2026-06-18)
**Reference:** MSG-ROOT-011, MSG-INFRA-055, MSG-LIBRARIAN-002

---

## Quick Start (VPS gabor@109.122.222.198)

```bash
# 1. Navigate to knowledge service
cd /opt/spaceos/spaceos-nexus

# 2. Setup environment
export VOYAGE_API_KEY="sk_..." # Get from https://dash.voyageai.com/api-keys
echo "VOYAGE_API_KEY=$VOYAGE_API_KEY" > knowledge-service/.env

# 3. Start ChromaDB (background)
docker compose up -d

# 4. Start knowledge service (foreground terminal)
cd knowledge-service
npm install   # if needed
npm run dev

# Expected output: "Server running on http://localhost:3456"

# 5. Validate (separate terminal)
./scripts/test-rag.sh

# Expected: ✅ 5/5 tests pass
```

---

## Detailed Steps

### Step 1: VOYAGE_API_KEY Setup

**Why Voyage AI?**
- Free tier: 50k embedding requests/month
- Model: `voyage-3-lite` (512 dimensions)
- Latency: <100ms per request
- Already integrated (see `spaceos-nexus/knowledge-service/src/embeddings.ts`)

```bash
# A. Get API key
# Go to https://dash.voyageai.com/api-keys
# Create new API key or copy existing
# Format: sk_...

# B. Setup .env file
cd /opt/spaceos/spaceos-nexus/knowledge-service
cp .env.example .env   # if needed
nano .env

# C. Add your key
# VOYAGE_API_KEY=sk_...
# (single line, no quotes)
```

### Step 2: ChromaDB Docker Service

**Configuration:** `/opt/spaceos/spaceos-nexus/docker-compose.yml`

```bash
# Start in background
cd /opt/spaceos/spaceos-nexus
docker compose up -d

# Verify
docker ps | grep chromadb
# Expected: spaceos_chromadb_<id> ... spaceos-chroma-service:latest

# Check health
curl http://localhost:8001/api/v1/heartbeat
# Expected: {"nanosecond timestamp":"..."}
```

**Persistent storage:** `spaceos_chromadb_data` volume

### Step 3: Knowledge Service Startup

```bash
# Terminal 1: Knowledge service
cd /opt/spaceos/spaceos-nexus/knowledge-service
npm install    # First time or if deps changed
npm run dev    # Watch mode

# Wait for:
# Server running on http://localhost:3456
# Loading embedded files...
# Documents indexed: 440+
# ✓ Server ready
```

**Ports:**
- `3456` — knowledge-service API
- `8001` — ChromaDB internal

### Step 4: Validation

```bash
# Terminal 2: Run test suite
cd /opt/spaceos/spaceos-nexus
./scripts/test-rag.sh

# Expected output:
# ✅ /health endpoint
# ✅ POST /api/knowledge/search (with results)
# ✅ GET /api/knowledge/search (with results)
# ✅ POST /api/knowledge/index (reindex)
# ✅ Semantic ranking (top result relevance > 0.5)
#
# Summary: 5/5 PASS ✅
```

### Step 5: Manual Search Test

```bash
# Query examples
curl "http://localhost:3456/api/knowledge/search?q=kernel&topK=5"
curl "http://localhost:3456/api/knowledge/search?q=RLS%20security&topK=3"
curl "http://localhost:3456/api/knowledge/search?q=VOYAGE%20API&topK=2"

# Expected: JSON array with [{ document, similarity_score, ... }, ...]
```

---

## Production Deployment (Later)

**Fázis 2:** Systemd service + auto-restart

```bash
# Copy systemd unit (already exists)
sudo systemctl enable spaceos-knowledge.service
sudo systemctl start spaceos-knowledge.service

# Status
sudo systemctl status spaceos-knowledge.service

# Logs
sudo journalctl -u spaceos-knowledge.service -f
```

---

## Troubleshooting

### ChromaDB won't start
```bash
docker compose down
docker compose up -d --force-recreate
```

### VOYAGE_API_KEY error
```bash
# Check .env exists
cat /opt/spaceos/spaceos-nexus/knowledge-service/.env | grep VOYAGE

# Verify format: sk_...
# Re-export: export VOYAGE_API_KEY="sk_..."
```

### Knowledge service won't bind port 3456
```bash
# Check existing process
lsof -i :3456

# Kill if needed
kill -9 <PID>

# Restart npm
cd /opt/spaceos/spaceos-nexus/knowledge-service
npm run dev
```

### No documents indexed
```bash
# Check docs/knowledge/ exists
ls /opt/spaceos/docs/knowledge/*.md | head -5

# Force reindex
curl -X POST http://localhost:3456/api/knowledge/index

# Verify count
curl http://localhost:3456/api/knowledge/search?q=.*&topK=1 | jq '.length'
```

---

## Integration Checklist

- [ ] VPS SSH access: `ssh gabor@109.122.222.198`
- [ ] Docker available: `docker --version`
- [ ] Node.js 22+: `node --version`
- [ ] VOYAGE_API_KEY from https://dash.voyageai.com
- [ ] `.env` file created with key
- [ ] `docker compose up -d` successful
- [ ] `npm run dev` listening on 3456
- [ ] `test-rag.sh` passes 5/5
- [ ] Semantic search responds <500ms
- [ ] Ready for Librarian indexing cron

---

## Next Steps (Librarian)

After validation:
1. Confirm in MSG-LIBRARIAN-002-DONE
2. Enable Librarian cron: `pipeline-knowledge-index.sh`
3. Schedule: 5-hourly reindex (0 */5 * * *)
4. Monitor: `curl http://localhost:3456/health`

---

**Reference:** ROADMAP.md / Fázis 1, Nexus Knowledge Service implementation
