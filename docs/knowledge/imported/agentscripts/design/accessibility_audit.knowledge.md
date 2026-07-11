---
name: accessibility-audit
description: 'WCAG AA compliance audit reference for UI features and components. Use when reviewing accessibility, checking contrast ratios, ARIA patterns, or keyboard navigation.'
domain: design
last_updated: 2026-02-24
---

# Skill: Accessibility Audit (A11y)

## ?? Purpose

WCAG AA compliance minimum ellenõrzés minden feature-höz és komponenshez. **Accessibility nem opció, hanem alapkövetelmény.**

---

## ?? Teoretikus Háttér

**WCAG (Web Content Accessibility Guidelines):**

- **Level A**: Minimum (blocking issues)
- **Level AA**: Recommended (industry standard) ‹ **Ez a célunk**
- **Level AAA**: Enhanced (nice-to-have)

**4 Core Principles (POUR):**

1. **Perceivable** - Tartalom látható/hallható minden felhasználónak
2. **Operable** - Interfész használható keyboard, mouse, screen reader-rel
3. **Understandable** - Tartalom és mûködés érthetõ
4. **Robust** - Mûködik assistive technológiákkal

---

## ? Accessibility Checklist (WCAG AA)

### 1. Keyboard Navigation

**Requirements:**

- [ ] **Tab Navigation**: Minden interaktív elem (link, button, input) elérhetõ Tab-bal
- [ ] **Logical Tab Order**: Top-to-bottom, left-to-right (natural reading order)
- [ ] **Visual Focus Indicator**: Látható focus state (nem csak browser default)
  - Minimum 2px outline vagy background change
  - Színkontraszt minimum 3:1 a környezethez
- [ ] **Enter/Space Activation**: Enter vagy Space aktiválja a button-okat
- [ ] **Escape Closes Modals**: Escape key bezárja a modal/dialog-okat
- [ ] **Arrow Keys Navigation**: List-eknél, dropdown-oknál arrow key navigation
- [ ] **Skip Links**: "Skip to main content" link az oldal tetején (screen reader users-nek)

**Test:**

```markdown
Tesztelés keyboard-dal:
1. Tab végig az oldalon › minden interaktív elem elérhetõ?
2. Visual focus mindig látható?
3. Tab order logikus? (ne ugráljon random helyekre)
4. Enter/Space mûködik button-okon?
5. Escape bezárja a modal-t?
```

---

### 2. Screen Reader Support

**Requirements:**

- [ ] **Alt Text for Images**:
  - Content images: Describe content ("Team photo at conference")
  - Functional images: Describe function ("Search icon")
  - Decorative images: Alt="" (empty, screen reader skips)
- [ ] **Form Labels**: Minden input-nak van `<label>` (nem csak placeholder!)
  - Placeholder eltûnik typing közben › nem accessibility-friendly
  - Label mindig látható
- [ ] **Error Messages Announced**:
  - Error megjelenik › screen reader beolvassa
  - Használd `aria-live="assertive"` vagy `aria-invalid="true"`
- [ ] **Dynamic Content Changes Announced**:
  - Loading state › "Loading data..."
  - Success message › "Data saved successfully"
  - Használd `aria-live="polite"` vagy `aria-live="assertive"`
- [ ] **Heading Structure**: Logikus heading hierarchia (h1 › h2 › h3, ne h1 › h3)
  - h1: Page title (egy db)
  - h2: Major sections
  - h3: Subsections
- [ ] **Semantic HTML**: `<button>`, `<nav>`, `<main>`, `<article>` (ne `<div onclick>`!)
- [ ] **Link Text**: Descriptive link text (ne "click here", hanem "Download report")

**Test:**

```markdown
Tesztelés screen reader-rel (NVDA, JAWS, VoiceOver):
1. Tab végig › minden elem beolvasva?
2. Form submit error › error message beolvasva?
3. Loading indicator › "Loading" beolvasva?
4. Heading navigation (h key) › van logikus struktúra?
```

---

### 3. Visual Accessibility

**Requirements:**

- [ ] **Text Contrast**: Minimum 4.5:1 contrast ratio (WCAG AA)
  - Large text (18pt+): Minimum 3:1
  - Tool: [WebAIM Contrast Checker](https://webaim.org/resources/contrastchecker/)
  - Példa: #1f2328 text on #ffffff background › 15.48:1 ?
- [ ] **Touch Target Size**: Minimum 44x44px (mobile), 24x24px (desktop)
  - Button, link, checkbox, radio minimum size
  - Padding is fontos! (ne csak a text area clickable)
- [ ] **Color + Icon**: Ne csak színre hagyatkozz
  - Rossz: ? Piros border = error
  - Jó: ? Piros border + ? ikon + "Error: Invalid email" text
- [ ] **Text Resizable**: Text 200%-ra zoom-olható layout törés nélkül
  - Browser zoom 200% › tartalom még olvasható?
  - Ne fixed px font sizes, használj rem/em
- [ ] **Focus State Visible**: Focus indicator mindig látható (min 2px, 3:1 contrast)
  - Ne `outline: none;` CSS-ben (accessibility nightmare!)
- [ ] **No Flashing Content**: Ne villogó tartalom (seizure trigger > 3 flash/sec)

**Color Contrast Examples:**

| Text Color | Background | Contrast | WCAG AA |
| ---------- | ---------- | -------- | ------- |
| #000000 | #FFFFFF | 21:1 | ? Pass |
| #1f2328 | #FFFFFF | 15.48:1 | ? Pass |
| #6c757d | #FFFFFF | 4.68:1 | ? Pass |
| #FFC107 | #FFFFFF | 1.83:1 | ? Fail |
| #FFFFFF | #007bff | 4.54:1 | ? Pass |

**Test:**

```markdown
Tesztelés visual accessibility:
1. Contrast checker tool › minden text 4.5:1 felett?
2. Touch targets › minden button min 44x44px?
3. Browser zoom 200% › layout nem tört el?
4. Csak grayscale › információ megmarad? (ne csak szín jelezzen)
```

---

### 4. Forms & Interactive Elements

**Requirements:**

- [ ] **Labels**: `<label for="email">` + `<input id="email">` (connected)
- [ ] **Required Fields**: `aria-required="true"` vagy `required` attribute
- [ ] **Error Indication**:
  - Visual: Red border + ? icon
  - Text: "Error: Email format invalid"
  - ARIA: `aria-invalid="true"` + `aria-describedby="email-error"`
- [ ] **Help Text**: `aria-describedby` connects help text to input
- [ ] **Autocomplete**: `autocomplete="email"` (helps autofill)
- [ ] **Disabled State**: Clearly visible + `aria-disabled="true"`

**Example Accessible Form:**

```html
<label for="email">Email Address *</label>
<input
  type="email"
  id="email"
  name="email"
  required
  aria-required="true"
  aria-invalid="false"
  aria-describedby="email-help email-error"
  autocomplete="email"
/>
<span id="email-help">We'll never share your email</span>
<span id="email-error" role="alert" aria-live="assertive" hidden>
  Error: Please enter a valid email address
</span>
```

---

### 5. Modals & Dialogs

**Requirements:**

- [ ] **Focus Trap**: Focus keyboard navigation-t modal-on belülre (ne escape modal-ból Tab-bal)
- [ ] **Focus Management**: Modal open › focus moves to modal, Modal close › focus visszakerül trigger element-re
- [ ] **Escape Closes**: Escape key bezárja a modal-t
- [ ] **ARIA Roles**: `role="dialog"` + `aria-labelledby` + `aria-modal="true"`
- [ ] **Background Inert**: Modal alatt background nem elérhetõ (keyboard vagy screen reader)

**Example Accessible Modal:**

```html
<div
  role="dialog"
  aria-modal="true"
  aria-labelledby="modal-title"
  aria-describedby="modal-description"
>
  <h2 id="modal-title">Confirm Delete</h2>
  <p id="modal-description">Are you sure you want to delete this item?</p>
  <button>Cancel</button>
  <button>Delete</button>
</div>
```

---

## ?? Accessibility Audit Report Template

```markdown
# Accessibility Audit Report: [Feature/Component Név]

Audit Date: [dátum]
Auditor: UI/UX Designer Agent
WCAG Level: AA

---

## Summary

**Overall Status**: [Pass / Partial Pass / Fail]

**Pass Rate**: X/Y checks passed (Z%)

**Critical Issues**: [szám] (blocking WCAG AA compliance)
**Warnings**: [szám] (best practice violations)

---

## 1. Keyboard Navigation ? / ?? / ?

| Requirement | Status | Notes |
| ----------- | ------ | ----- |
| Tab navigation | ? Pass | All interactive elements reachable |
| Logical tab order | ? Pass | Top-to-bottom, left-to-right |
| Visual focus indicator | ?? Warning | Focus indicator too subtle (1px vs 2px min) |
| Enter/Space activation | ? Pass | Buttons activate correctly |
| Escape closes modals | ? Fail | Escape key does not close modal |
| Arrow keys navigation | N/A | No dropdowns in this component |

**Recommendations:**
1. Increase focus indicator to 2px outline
2. Add Escape key handler to modal close

---

## 2. Screen Reader Support ? / ?? / ?

| Requirement | Status | Notes |
| ----------- | ------ | ----- |
| Alt text for images | ? Pass | All images have descriptive alt |
| Form labels | ? Fail | Email input missing `<label>` (only placeholder) |
| Error messages announced | ?? Warning | Error shows visually but no `aria-live` |
| Dynamic content announced | ? Pass | Loading state has `aria-live="polite"` |
| Heading structure | ? Pass | h1 › h2 › h3 correctly |
| Semantic HTML | ? Pass | Uses `<button>`, not `<div onclick>` |
| Link text | ? Pass | "Download report" (not "click here") |

**Recommendations:**
1. Add `<label for="email">Email Address</label>`
2. Add `aria-live="assertive"` to error message container

---

## 3. Visual Accessibility ? / ?? / ?

| Requirement | Status | Notes |
| ----------- | ------ | ----- |
| Text contrast 4.5:1 | ? Pass | All text passes (checked with WebAIM) |
| Touch target 44x44px | ?? Warning | Close button only 32x32px (mobile) |
| Color + Icon | ? Pass | Error uses red + ? icon + text |
| Text resizable 200% | ? Pass | Tested browser zoom, no breakage |
| Focus state visible | ?? Warning | Focus 1px instead of 2px minimum |
| No flashing content | ? Pass | No animations >3 flash/sec |

**Color Contrast Results:**
- Body text (#1f2328 on #FFFFFF): 15.48:1 ?
- Secondary text (#6c757d on #FFFFFF): 4.68:1 ?
- Button text (#FFFFFF on #007bff): 4.54:1 ?

**Recommendations:**
1. Increase close button size to 44x44px on mobile
2. Increase focus indicator to 2px

---

## 4. Forms & Interactive Elements ? / ?? / ?

[Repeat format...]

---

## 5. Modals & Dialogs ? / ?? / ?

[Repeat format...]

---

## Critical Issues (Must Fix)

1. **? Missing Form Label**
   - Issue: Email input has no `<label>`, only placeholder
   - Impact: Screen reader users don't know what field is
   - Fix: Add `<label for="email">Email Address</label>`
   - Priority: High

2. **? Escape Key Handler**
   - Issue: Escape key doesn't close modal
   - Impact: Keyboard users can't easily dismiss modal
   - Fix: Add `onKeyDown={(e) => e.key === 'Escape' && closeModal()}`
   - Priority: High

---

## Warnings (Should Fix)

1. **?? Focus Indicator Too Subtle**
   - Issue: 1px outline vs 2px recommended
   - Impact: Low vision users may miss focus state
   - Fix: Change CSS `outline: 2px solid blue`
   - Priority: Medium

2. **?? Touch Target Too Small**
   - Issue: Close button 32x32px on mobile (44px min recommended)
   - Impact: Mobile users may miss tap target
   - Fix: Increase button size or padding
   - Priority: Medium

---

## Next Steps

1. [ ] Fix critical issues (blocking WCAG AA)
2. [ ] Fix warnings (best practice)
3. [ ] Test with screen reader (NVDA, JAWS, VoiceOver)
4. [ ] Test with keyboard only (no mouse)
5. [ ] Re-audit after fixes

**Target Fix Date**: [dátum]
**Re-Audit Date**: [dátum]

---

## Resources

- [WCAG 2.1 Quick Reference](https://www.w3.org/WAI/WCAG21/quickref/)
- [WebAIM Contrast Checker](https://webaim.org/resources/contrastchecker/)
- [ARIA Authoring Practices](https://www.w3.org/WAI/ARIA/apg/)

```

---

## ?? Execution Steps

### Step 1: Identify Component/Feature to Audit

**Betöltendõ:**

- Component kód (React component file)
- Design spec (`docs/ux/{feature}-flow.md`)
- User journey (`docs/ux/{feature}-journey.md`)

### Step 2: Run Through Checklist

**Használd a fenti checklist-et:**

1. Keyboard Navigation (7 checks)
2. Screen Reader Support (7 checks)
3. Visual Accessibility (6 checks)
4. Forms & Interactive (6 checks)
5. Modals & Dialogs (5 checks ha van modal)

**Minden check:**

- ? Pass - Compliance
- ?? Warning - Best practice violation
- ? Fail - WCAG AA blocking issue
- N/A - Not applicable

### Step 3: Test with Tools

**Automated tools:**

- [axe DevTools](https://www.deque.com/axe/devtools/) (Chrome extension)
- [WAVE](https://wave.webaim.org/) (Browser extension)
- [Lighthouse](https://developers.google.com/web/tools/lighthouse) (Chrome DevTools)

**Manual testing:**

- Keyboard navigation (Tab, Enter, Space, Escape, Arrows)
- Screen reader (NVDA, JAWS, VoiceOver)
- Color contrast checker (WebAIM)
- Browser zoom 200%

### Step 4: Document Findings

**Kategorizáld issues-t:**

- **Critical** (?) - Blocking WCAG AA, must fix
- **Warning** (??) - Best practice, should fix
- **Pass** (?) - Compliant

**Minden issue-hoz adj:**

- Issue description (mi a probléma)
- Impact (ki érinti, mennyire súlyos)
- Fix recommendation (konkrét megoldás)
- Priority (High/Medium/Low)

### Step 5: Save Audit Report

**Fájl neve:**

- `docs/ux/{feature-name}-a11y-audit.md`

**Tartalom:** Használd a fenti report template-et

### Step 6: Handoff to Frontend Developer

**Mit add át:**

- Audit report (`docs/ux/{feature}-a11y-audit.md`)
- Priority fixes (Critical issues)
- Code examples (accessible component patterns)

---

## ?? Common Mistakes

1. ? **Csak automated tool-ra hagyatkozás:**
   - Automated tools csak ~30% a11y issues-t találnak
   - Manual testing (keyboard, screen reader) elengedhetetlen

2. ? **Placeholder mint label:**
   - Placeholder eltûnik typing közben
   - Screen reader nem olvassa be placeholder-t label-ként
   - Mindig használj `<label>` element-et!

3. ? **Csak szín jelzés:**
   - Color blind users nem látják a színkülönbséget
   - Mindig használj szín + ikon + text kombinációt

4. ? **outline: none CSS:**
   - Keyboard users nem látják, hol vannak
   - SOHA ne távolítsd el a focus indicator-t!

5. ? **<div onclick> button helyett:**
   - Screen reader nem tudja, hogy button
   - Keyboard (Enter/Space) nem aktiválja
   - Mindig használj semantic HTML (`<button>`)!

---

## ?? Related Skills

- **Input from**: `user_journey_mapping.knowledge.md` (journey pain points › a11y needs)
- **Output to**: Frontend Developer (a11y requirements implementation)
- **Complementary**: `design_system_maintenance.knowledge.md` (a11y patterns reusable)

---

## ?? Resources

**Source:**

- Based on [`se-ux-ui-designer.agent.md`](../agents/se-ux-ui-designer.agent.md) (Step 5: Accessibility Checklist)
- Additional guidance from [`a11y.instructions.md`](../instructions/a11y.instructions.md) and [`accessibility.agent.md`](../agents/accessibility.agent.md)

**Further Reading:**

- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [WebAIM Resources](https://webaim.org/resources/)
- [A11y Project Checklist](https://www.a11yproject.com/checklist/)
- [Inclusive Components](https://inclusive-components.design/)

---

**Output Example Path:**
`docs/ux/team-onboarding-a11y-audit.md`
