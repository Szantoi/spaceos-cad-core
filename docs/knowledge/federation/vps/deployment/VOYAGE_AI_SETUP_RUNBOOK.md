# Voyage AI Setup Runbook for Knowledge Service

**Status:** Manual operator task (requires external Voyage AI registration)
**Timeline:** 20 minutes (5 min registration + 5 min key gen + 10 min VPS)
**Priority:** CRITICAL — Nexus Phase 1 blocker

---

## Prerequisites

- VPS SSH access: `gabor@109.122.222.198`
- Internet access to https://dash.voyageai.com/
- Voyage AI free tier eligibility (no payment method required)

---

## Step 1: Voyage AI Registration (5 minutes)

**Manual: Browser-based**

1. Visit https://dash.voyageai.com/
2. Click "Sign Up"
3. Enter email + password
4. Verify email (check inbox)
5. Accept free tier terms
6. Dashboard loads

**Free Tier Details:**
- **Tokens/month:** 25M
- **Cost:** $0
- **Model:** voyage-3-lite (512 dimensions)
- **Sufficient for:** docs/knowledge (~500K tokens) + margin

---

## Step 2: Generate API Key (5 minutes)

**Manual: Dashboard**

1. Login to https://dash.voyageai.com/ (if not already logged in)
2. Navigate to: **API Keys** section (usually left sidebar)
3. Click **"Create new API key"** or **"Generate key"**
4. **Copy the key immediately** (format: `pa-XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX`)
5. Save it temporarily (notepad/password manager)

**Important:** The key is only displayed once. If you lose it, you'll need to delete and regenerate.

---

## Step 3: VPS Configuration (10 minutes)

**Manual: SSH to VPS**

```bash
# 1. SSH to VPS
ssh gabor@109.122.222.198

# 2. Navigate to knowledge service directory
cd /opt/spaceos/spaceos-nexus/knowledge-service

# 3. Verify current .env (should have VOYAGE_API_KEY commented or empty)
cat .env | grep VOYAGE_API_KEY

# 4. Add the API key to .env
# Option A: Append to existing .env
echo "VOYAGE_API_KEY=pa-<PASTE-YOUR-KEY-HERE>" >> .env

# Option B: Or edit .env directly
nano .env
# Find the line: VOYAGE_API_KEY=...
# Uncomment and replace with: VOYAGE_API_KEY=pa-YOUR-KEY-HERE
# Save (Ctrl+X, Y, Enter)

# 5. Verify the key was added correctly
grep "^VOYAGE_API_KEY" .env
# Should output: VOYAGE_API_KEY=pa-XXXXXXXXX...

# 6. Check .env is readable by Node process
ls -la .env
```

---

## Step 4: Notify Nexus Terminal (Immediate)

**Automated: ROOT inbox message**

Once VPS .env is configured, send continuation message to Nexus:

```markdown
Voyage AI key successfully configured on VPS.
Ready for Nexus Phase 1 indexing.

Nexus: Pull MSG-NEXUS-003 and execute:
  cd /opt/spaceos/spaceos-nexus/knowledge-service
  npm run index
  npm run dev
  ./scripts/test-rag.sh
```

---

## Troubleshooting

### Issue: "VOYAGE_API_KEY=your_voyage_key_here" is still in .env

**Fix:** The .env file has a commented placeholder. Replace it:

```bash
# Remove the placeholder line
grep -v "VOYAGE_API_KEY=your_voyage_key_here" .env > .env.tmp && mv .env.tmp .env

# Add the real key
echo "VOYAGE_API_KEY=pa-YOUR-KEY-HERE" >> .env
```

### Issue: "No matching key found in Voyage dashboard"

**Check:**
1. Are you logged into the correct Voyage AI account?
2. Is the key in the correct format? (starts with `pa-`)
3. Did you copy the entire key (including the `pa-` prefix)?

### Issue: Node process can't read VOYAGE_API_KEY

**Check permissions:**
```bash
# Make sure .env is readable
chmod 644 .env

# Check if Node can see it
npm run dev
# Should see: "✓ Voyage AI initialized" in logs
```

### Issue: Nexus indexing fails with "401 Unauthorized"

**Possibilities:**
1. Key is invalid or expired
2. Key was not pasted correctly (missing characters)
3. Voyage AI API is down (rare)

**Fix:**
1. Regenerate key from https://dash.voyageai.com/
2. Update .env with new key
3. Restart: `npm run dev`

---

## Expected Output

**When configured correctly, Nexus logs should show:**

```
[knowledge-service] Starting at 3456...
✓ Voyage AI initialized (voyage-3-lite, 512 dims)
✓ ChromaDB connected (http://localhost:8001)
✓ Knowledge base loaded: 21 documents, ~200 chunks
[GET /health] → 200 OK
[POST /api/knowledge/index] → Starting indexing...
  Embedding 200 chunks via Voyage AI...
  Storing in ChromaDB...
[POST /api/knowledge/search?q=RLS] → 5 results, 0.82s
✓ All tests passing (5/5)
```

---

## Rollback (if needed)

If you need to revert to a different embedding backend:

```bash
# Restore .env backup (if exists)
cp .env.bak .env

# Or edit .env and comment out VOYAGE_API_KEY
nano .env
# Comment: # VOYAGE_API_KEY=pa-...
# Save

# Restart Nexus
npm run dev
```

---

## Timeline Summary

| Step | Time | Who | Status |
|---|---|---|---|
| Voyage AI registration | 5 min | **Manual (Operator)** | ⏳ PENDING |
| API key generation | 5 min | **Manual (Operator)** | ⏳ PENDING |
| VPS .env configuration | 10 min | **Manual (VPS Operator)** | ⏳ PENDING |
| Nexus indexing | 5-10 min | **Automated (Nexus terminal)** | ⏳ AWAITING KEY |
| **Total** | **20-25 min** | — | — |

---

**Created:** 2026-06-17
**Updated:** 2026-06-17 06:45 UTC
**Owner:** ROOT (operational coordination)
**Status:** READY FOR EXECUTION
