---
name: user-journey-mapping
description: 'User journey mapping methodology for detailed process flow analysis. Use when designing UI flows, mapping user touchpoints, or planning Figma prototypes.'
domain: design
last_updated: 2026-02-24
---

# Skill: User Journey Mapping

## ?? Purpose

Részletes térképet készíteni arról, hogy a felhasználó **hogyan halad végig a folyamaton**, mit gondol, érez és csinál minden lépésben. Ez inform-álja a UI flow design-t Figma-ban.

---

## ?? Teoretikus Háttér

**User Journey Map:**
> Vizuális reprezentáció a felhasználó útjáról egy célhoz.

**Dokumentálja minden stage-re:**

1. **What user is doing** - Akció (mit csinál)
2. **What user is thinking** - Gondolat (mit gondol)
3. **What user is feeling** - Érzelem (hogyan érzi magát: ????????)
4. **Pain points** - Problémák (hol akad el)
5. **Opportunity** - Design lehetõség (mit javíthatunk)

---

## ?? Journey Map Template

```markdown
# User Journey: [Feature Név]

Created: [dátum]
Based on JTBD: `docs/ux/{feature}-jtbd.md`

---

## User Persona

- **Who**: [konkrét szerepkör - pl. "Frontend Developer joining new team"]
- **Goal**: [mit akar elérni - pl. "Get access to all critical tools"]
- **Context**: [mikor/hol - pl. "First day onboarding, remote work"]
- **Success Metric**: [hogyan tudja, hogy sikeres volt - pl. "All tools accessible within 1 hour"]

---

## Journey Stages

### Stage 1: Awareness (Tudatosság)

**What user is doing**:
- [konkrét akció - pl. "Receiving onboarding email with login info"]

**What user is thinking**:
- [gondolat - pl. "Where do I start? Is there a checklist?"]

**What user is feeling**:
- ?? [érzelem - pl. "Overwhelmed, uncertain"]

**Pain points**:
- [probléma 1 - pl. "No clear starting point"]
- [probléma 2 - pl. "Too many tools listed at once"]
- [probléma 3 - pl. "Unsure which tools are critical vs optional"]

**Opportunity**:
- [design lehetõség - pl. "Single landing page with progressive disclosure"]
- [konkrét javaslat - pl. "Show critical tools first, reveal optional later"]

---

### Stage 2: Exploration (Felfedezés)

**What user is doing**:
- [akció - pl. "Clicking through different tools, reading descriptions"]

**What user is thinking**:
- [gondolat - pl. "Do I need access to all of these? Which are critical?"]

**What user is feeling**:
- ?? [érzelem - pl. "Confused about priorities"]

**Pain points**:
- [probléma 1 - pl. "No indication of which tools are essential vs optional"]
- [probléma 2 - pl. "Can't find help when stuck"]
- [probléma 3 - pl. "Unfamiliar tools with no context"]

**Opportunity**:
- [design lehetõség - pl. "Categorize tools by urgency (Critical / Recommended / Optional)"]
- [konkrét javaslat - pl. "Inline help tooltips: 'Why do I need this?'"]

---

### Stage 3: Action (Cselekvés)

**What user is doing**:
- [akció - pl. "Setting up accounts, configuring tool access"]

**What user is thinking**:
- [gondolat - pl. "Am I doing this right? Did I miss anything?"]

**What user is feeling**:
- ?? [érzelem - pl. "Progress, but checking frequently for confirmation"]

**Pain points**:
- [probléma 1 - pl. "No confirmation of completion after each tool"]
- [probléma 2 - pl. "Unclear if setup is correct"]
- [probléma 3 - pl. "Can't see overall progress"]

**Opportunity**:
- [design lehetõség - pl. "Progress tracker showing completed vs remaining tools"]
- [konkrét javaslat - pl. "Validation checkmarks + 'Verify Access' button per tool"]

---

### Stage 4: Outcome (Eredmény)

**What user is doing**:
- [akció - pl. "Working in tools, referring back to checklist"]

**What user is thinking**:
- [gondolat - pl. "I think I'm all set, but I'll check the list again"]

**What user is feeling**:
- ?? [érzelem - pl. "Confident, productive"]

**Success metrics**:
- [mérõszám 1 - pl. "All critical tools accessed within 24 hours"]
- [mérõszám 2 - pl. "No blocked work due to missing access"]
- [mérõszám 3 - pl. "No repeat 'I don't have access' questions"]

**Pain points** (even at success):
- [probléma ha van - pl. "Still unsure if optional tools would be helpful later"]

**Opportunity**:
- [design lehetõség - pl. "Post-onboarding email: 'Explore optional tools when ready'"]

---

## Key Insights

**Emotional Journey:**
- Start: ?? Overwhelmed
- Middle: ?? Confused › ?? Making progress
- End: ?? Confident

**Critical Pain Points (prioritás szerint):**
1. [#1 pain point - pl. "No clear starting point - unclear which tools are critical"]
2. [#2 pain point - pl. "No progress tracker - user checks repeatedly if complete"]
3. [#3 pain point - pl. "No confirmation per tool - unsure if setup correct"]

**Design Implications:**
1. [implication 1 - pl. "Progressive disclosure: Critical › Recommended › Optional"]
2. [implication 2 - pl. "Visual progress tracker with checkmarks"]
3. [implication 3 - pl. "Validation per tool: 'Verify Access' button"]

---

## Next Steps

- [ ] Create Design Flow Specification based on this journey
- [ ] Identify accessibility requirements for each stage
- [ ] Define design principles (progressive disclosure, clear progress, contextual help)
- [ ] Handoff to Frontend Developer with flow spec

**Flow Spec Output:** `docs/ux/{feature}-flow.md`

```

---

## ?? Execution Steps

### Step 1: Load JTBD Analysis

**Elõfeltétel:** JTBD analysis már létezik (`docs/ux/{feature}-jtbd.md`)

**Betöltendõ információk:**

- User Persona (Who)
- Goal (What they want to achieve)
- Context (When/Where)
- Pain Points (Current frustrations)

### Step 2: Identify Journey Stages

**Tipikus stage-ek (customize based on feature):**

1. **Awareness** - Felhasználó elõször találkozik a feature-rel
2. **Exploration** - Felhasználó felfedezi az opciókat
3. **Action** - Felhasználó cselekvések végrehajt
4. **Outcome** - Felhasználó eléri a célt (vagy nem)

**További stage-ek (opcionális):**

- **Consideration** - Döntés before action
- **Retention** - Visszatérõ használat
- **Advocacy** - Másoknak ajánlás

### Step 3: Document Each Stage

**Minden stage-re töltsd ki:**

1. **What user is doing** (Akció):
   - Konkrét, megfigyelhetõ akció
   - Pl. "Clicking 'Start Setup' button"

2. **What user is thinking** (Gondolat):
   - Belsõ monológ, kérdések
   - Idézõjelben, elsõ személyben
   - Pl. "Do I need all of these tools?"

3. **What user is feeling** (Érzelem):
   - Emoji + szöveges leírás
   - ?? Overwhelmed, ?? Confused, ?? Progress, ?? Confident
   - Pl. "?? Confused about priorities"

4. **Pain points** (Problémák):
   - Konkrét akadályok, frustrációk
   - Mit nem tud eldönteni/megtalálni?
   - Pl. "No indication of which tools are critical"

5. **Opportunity** (Design lehetõség):
   - Mit javíthatunk? Hogyan?
   - Konkrét design javaslat
   - Pl. "Categorize tools: Critical / Recommended / Optional"

### Step 4: Analyze Emotional Arc

**Rajzolj fel egy érzelmi ívet:**

```
?? Confident
?? Progress
?? Neutral
?? Confused
?? Overwhelmed

Stage:  [1----2----3----4]
         ??   ??   ??   ??
```

**Keress patterns-et:**

- Hol van a legnagyobb emotional dip? (legrosszabb point)
- Hol van turning point? (mikor javul)
- Mi okozza az emotional change-et?

### Step 5: Prioritize Pain Points

**Rank pain points hatás szerint:**

1. **High Impact** - Blocking, causes abandonment
2. **Medium Impact** - Frustrating, slows down
3. **Low Impact** - Minor inconvenience

**Design elõször a High Impact pain points-okat oldja meg!**

### Step 6: Save Output

**Fájl neve:**

- `docs/ux/{feature-name}-journey.md`

**Tartalom:** Használd a fenti template-et

---

## ?? Design Implications

**User Journey › Design Decisions:**

| Journey Insight | Design Implication |
| --------------- | ------------------ |
| ?? Overwhelmed at start | › Progressive disclosure (ne show mindent egyszerre) |
| ?? Confused priorities | › Clear categorization (Critical / Optional) |
| ?? Wants progress confirmation | › Progress tracker with checkmarks |
| ?? Confident at end | › Success screen with next steps |
| Pain: "No help when stuck" | › Inline contextual help (tooltips) |
| Pain: "Unclear if correct" | › Validation per step ('Verify Access') |

---

## ?? Common Mistakes

1. ? **Túl high-level:**
   - Rossz: "User használja a rendszert"
   - Jó: "User clicks 'Start Setup', sees critical tools list, selects first tool"

2. ? **Feltételezett érzelmek:**
   - Rossz: "User happy" (miért happy?)
   - Jó: "?? Confident (because all tools configured, progress tracker shows 5/5 done)"

3. ? **Solution-focus vs problem-focus:**
   - Rossz: "User navigates the dashboard" (ez már solution)
   - Jó: "User tries to find which tools are critical" (ez problem)

4. ? **Hiányzó pain points:**
   - Minden stage-nek van pain point (még a successful stage-eknek is)
   - Ha nem találsz, kérdezz mélyebbre!

5. ? **Generic opportunities:**
   - Rossz: "Make it better"
   - Jó: "Add 'Critical' label + sort critical tools to top"

---

## ?? Related Skills

- **Previous Skill**: `jtbd_analysis.knowledge.md` (JTBD elõfeltétel)
- **Next Skill**: Design Flow Specification (journey inform-álja flow design)
- **Complementary**: `accessibility_audit.knowledge.md` (journey stages highlight a11y needs)

---

## ?? Resources

**Source:**

- Based on [`se-ux-ui-designer.agent.md`](../agents/se-ux-ui-designer.agent.md) (Step 3: User Journey Mapping)

**Further Reading:**

- "Mapping Experiences" by James Kalbach
- "The Customer Journey" by Nienke Bloem
- Nielsen Norman Group: User Journey Mapping

---

**Output Example Path:**
`docs/ux/team-onboarding-journey.md`
