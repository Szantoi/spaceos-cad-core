---
name: jtbd-analysis
description: 'Jobs-to-be-Done analysis framework for understanding user goals beyond feature requests. Use when conducting user research, defining product requirements, or mapping user needs to features.'
domain: design
last_updated: 2026-02-24
---

# Skill: Jobs-to-be-Done (JTBD) Analysis

## ?? Purpose

Megérteni, hogy a felhasználók milyen "munkát" (job) akarnak elvégezni amikor a terméket használják. **NEM a feature kérést**, hanem a **mögöttes célt** kell feltárni.

---

## ?? Teoretikus Háttér

**JTBD Framework:**

> "When [situation], I want to [motivation], so I can [outcome]."

**Kulcs gondolat:** A felhasználók nem terméket vesznek, hanem "munkavégzésre bérelnek" egy terméket.

**Példa:**

- ? Feature kérés: "Akarok egy gombot"
- ? JTBD: "Amikor új csapat tagot onboardolok, gyorsan akarom megosztani az eszköz hozzáféréseket, hogy elsõ naptól produktív legyen admin munka nélkül"

---

## ?? Discovery Questions (Mindig kérdezz!)

### 1. Ki a felhasználó? (Who)

**Kérdések:**

- Mi a szerepköre? (developer, manager, végfelhasználó)
- Mi a tapasztalati szintje hasonló eszközökkel? (kezdõ, haladó, expert)
- Milyen eszközön használja? (mobile, desktop, tablet)
- Van accessibility igénye? (screen reader, keyboard-only, motor limitations)
- Mennyire tech-savvy? (komplex interfészek vs egyszerûség)

**Template:**

```markdown
## User Persona
- **Role**: [konkrét szerepkör]
- **Skill Level**: [kezdõ/haladó/expert]
- **Primary Device**: [mobile/desktop/tablet]
- **Accessibility Needs**: [igen/nem - részletezd]
- **Tech Comfort**: [alacsony/közepes/magas]
```

---

### 2. Mi a kontextus? (Context)

**Kérdések:**

- Mikor/hol használja? (rohanós reggel, fókuszált munka, mobilon)
- Mit akar elérni? (valós cél, nem a feature kérés)
- Mi van, ha sikertelen? (kis kényelmetlenség vs komoly probléma/revenue loss)
- Milyen gyakran csinálja ezt? (napi, heti, ritkán)
- Milyen más eszközöket használ hasonlóra?

**Template:**

```markdown
## Context
- **When/Where**: [mikor és hol történik]
- **Goal**: [mit akar elérni - valós cél]
- **Consequence of Failure**: [mi van ha nem sikerül]
- **Frequency**: [milyen gyakran - napi/heti/havi/ritkán]
- **Other Tools**: [milyen más eszközöket használ]
```

---

### 3. Mi a pain point? (Pain Points)

**Kérdések:**

- Mi frusztráló a jelenlegi megoldással?
- Hol akadnak el vagy zavarodnak össze?
- Milyen workaround-okat találtak ki?
- Mit szeretnének egyszerûbbé tenni?
- Mi okozza, hogy feladják a task-ot?

**Template:**

```markdown
## Pain Points
- **Frustrations**: [mi a frusztráló]
- **Stuck Points**: [hol akadnak el]
- **Workarounds**: [milyen workaround-okat használnak]
- **Wishes**: [mit szeretnének egyszerûbbé tenni]
- **Abandon Triggers**: [mi okozza a feladást]
```

---

## ?? JTBD Statement Template

**Final output format:**

```markdown
# Jobs-to-be-Done Analysis: [Feature Név]

## Job Statement

When **[situation]**, I want to **[motivation]**, so I can **[outcome]**.

**Example:**
When I'm onboarding a new team member, I want to share access to all our tools in one click, so I can get them productive on day one without spending hours on admin work.

---

## User Persona

- **Role**: Frontend Developer joining new team
- **Skill Level**: Intermediate (3-5 years experience)
- **Primary Device**: Desktop (MacBook)
- **Accessibility Needs**: None
- **Tech Comfort**: High (comfortable with complex tools)

---

## Context

- **When/Where**: First day onboarding, remote work setup
- **Goal**: Get access to all critical tools (Slack, GitHub, Jira, Figma)
- **Consequence of Failure**: Blocked work, asking repeat questions, poor onboarding experience
- **Frequency**: Every new hire (monthly for growing teams)
- **Other Tools**: Manual email invites, spreadsheet checklists

---

## Current Solution & Pain Points

### Current Solution
- Manually adding to Slack, GitHub, Jira, Figma, AWS Console
- Following a Google Docs checklist
- Email invites one-by-one
- Takes 2-3 hours total

### Pain Points
- **Frustrations**: Repetitive, time-consuming, easy to forget a tool
- **Stuck Points**: Finding the right links, remembering passwords, different invite processes per tool
- **Workarounds**: Bookmarked invite links, copy-paste from previous hire
- **Wishes**: One-click invite, automated checklist, confirmation when all done
- **Abandon Triggers**: Too many tools, invite links expired, unclear which tools are critical

---

## Success Criteria

**How do we know this job is done well?**
- [ ] New hire has access to all critical tools within 1 hour (not 2-3 hours)
- [ ] No blocked work due to missing access
- [ ] No repeat "I don't have access to X" questions
- [ ] Onboarding admin uses clear checklist, not memory
- [ ] Confirmation when all tools are configured

---

## Opportunities (Design Implications)

1. **Progressive Disclosure**: Show critical tools first (Slack, GitHub), optional tools later
2. **One-Click Invites**: Batch invite to multiple tools
3. **Clear Progress**: Visual checklist with checkmarks
4. **Validation**: "Verify Access" button to test each tool
5. **Smart Defaults**: Pre-fill common team settings

```

---

## ?? Execution Steps

### Step 1: Load Context

**Betöltendõ fájlok:**

- Epic/Task dokumentáció
- Projekt goal (`docs/{project}/goal.md`)
- Meglévõ user research (ha van)

### Step 2: Ask Discovery Questions

**MINDIG kezdd kérdésekkel a felhasználótól:**

```markdown
Kezdjük a Jobs-to-be-Done analysis-szel. Kérlek válaszolj ezekre:

## JTBD Discovery Questions

### Ki a felhasználó?
1. Mi a szerepköre? (developer, manager, végfelhasználó)
2. Mi a tapasztalati szintje hasonló eszközökkel? (kezdõ, haladó, expert)
3. Milyen eszközön használja? (mobile, desktop, tablet)
4. Van accessibility igénye? (screen reader, keyboard-only, motor limitations)
5. Mennyire tech-savvy? (komplex interfészek vs egyszerûség)

### Mi a kontextus?
6. Mikor/hol használja ezt? (rohanós reggel, fókuszált munka, mobilon)
7. Mit akar elérni? (valós cél, nem a feature kérés)
8. Mi van, ha sikertelen? (kis kényelmetlenség vs komoly probléma)
9. Milyen gyakran csinálja ezt? (napi, heti, ritkán)
10. Milyen más eszközöket használ hasonlóra?

### Mi a pain point?
11. Mi frusztráló a jelenlegi megoldással?
12. Hol akadnak el vagy zavarodnak össze?
13. Milyen workaround-okat találtak ki?
14. Mit szeretnének egyszerûbbé tenni?
15. Mi okozza, hogy feladják a task-ot?
```

### Step 3: Create JTBD Statement

**Válaszok alapján hozd létre:**

1. **Job Statement**: When [situation], I want to [motivation], so I can [outcome]
2. **User Persona**: Role, skill level, device, accessibility, tech comfort
3. **Context**: When/where, goal, consequence, frequency, other tools
4. **Pain Points**: Frustrations, stuck points, workarounds, wishes, abandon triggers
5. **Success Criteria**: Mérõszámok hogy mikor "kész" a job

### Step 4: Save Output

**Fájl neve:**

- `docs/ux/{feature-name}-jtbd.md`

**Tartalom:** Használd a fenti template-et

### Step 5: Insights Summary

**Összegezd a key insights-okat:**

```markdown
## Key Insights from JTBD Analysis

1. **Core Job**: [mi a felhasználó valós célja egyetlen mondatban]
2. **Main Pain Point**: [mi a legnagyobb fájdalom pont]
3. **Success Metric**: [hogyan mérhetjük a sikert]
4. **Design Implication**: [milyen design döntést sugall ez]

**Next Steps:**
- Create User Journey Map › `docs/ux/{feature}-journey.md`
- Use insights to inform design flow specification
```

---

## ?? Common Mistakes (Kerüld ezeket!)

1. ? **Feature kérés mint job:**
   - Rossz: "A user gombot akar"
   - Jó: "A user gyorsan akar döntést hozni az opciók között"

2. ? **Túl általános:**
   - Rossz: "A user terméket akar használni"
   - Jó: "Amikor új projektet kezd, gyorsan fel akarja állítani a team eszközöket, hogy elsõ naptól dolgozhasson"

3. ? **Feltételezések user-rõl:**
   - Rossz: "A user tudja, hogy mit kell nyomni"
   - Jó: "Kérdezd meg: Milyen gyakran használják? Milyen képzettségûek?"

4. ? **Solution-focus vs problem-focus:**
   - Rossz: "A user dashboard-ot akar"
   - Jó: "A user gyorsan át akarja látni az analytics-et döntéshozatalhoz"

---

## ?? Related Skills

- **Next Skill**: `user_journey_mapping.knowledge.md` (JTBD után mindig user journey következik)
- **Complementary**: `accessibility_audit.knowledge.md` (user needs lehet accessibility requirements)
- **Design Output**: `design_flow_specification` (JTBD insights inform design döntések)

---

## ?? Resources

**Source:**

- Based on [`se-ux-ui-designer.agent.md`](../agents/se-ux-ui-designer.agent.md) (Step 1-2: Ask about users first, JTBD Analysis)

**Further Reading:**

- "Jobs to be Done" by Clayton Christensen
- "Competing Against Luck" (JTBD framework)
- "The Mom Test" by Rob Fitzpatrick (how to ask good questions)

---

**Output Example Path:**
`docs/ux/team-onboarding-jtbd.md`
