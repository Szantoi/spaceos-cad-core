---
name: color-theory
description: 'Professional color palette design using 60-30-10 Rule and color theory. Use when designing color schemes, ensuring visual consistency and accessibility compliance.'
domain: design
last_updated: 2026-02-24
---

# Skill: Color Theory & Visual Design

## ?? Purpose

Professzionális, accessible color palette-ek tervezése a **60-30-10 Rule** és color theory alapján. Biztosítja a vizuális konzisztenciát és accessibility-t.

---

## ?? Teoretikus Háttér

### Color Psychology & Usage

**Hot Colors** (Warm Colors):

- **Red** ?? - Urgency, danger, error, passion
- **Orange** ?? - Energy, enthusiasm, warning
- **Yellow** ?? - Attention, caution, optimism (ROSSZ text color!)

**Cool Colors** (Cool Colors):

- **Blue** ?? - Trust, calm, professional, primary brand color
- **Green** ?? - Success, growth, positive feedback
- **Purple** ?? - Creativity, luxury (USE SPARINGLY)

**Neutral Colors**:

- **Gray** ?? - Professional, modern, text, backgrounds
- **Black** ? - Text, high contrast
- **White** ? - Backgrounds, clean, minimalist

---

## ?? 60-30-10 Rule (Golden Ratio)

**Rule:** Oszd el a színeket 60% - 30% - 10% arányban.

### 60%: Primary (Dominant) Color

- **Usage**: Backgrounds, large surfaces
- **Típus**: Cool vagy light color
- **Példák**: White (#FFFFFF), Light Gray (#f8f9fa), Light Blue (#e3f2fd)
- **TILOS**: Hot colors (red, orange, yellow, pink, purple, magenta)

**Why?**

- Legnagyobb visual impact › nyugtató, nem zavaró kell legyen
- Hot color 60%-ban = szem fáradás, aggresszív

### 30%: Secondary (Supporting) Color

- **Usage**: Components, sections, supporting elements
- **Típus**: Cool vagy light color (más, mint primary)
- **Példák**: Light Gray (#e9ecef), Light Blue (#cce5ff), Soft Green (#d4edda)
- **TILOS**: Hot colors, high saturation

**Why?**

- Visual hierarchy › different from primary, de nem túl kontrasztos
- Támogatja a primary-t, nem versenyez vele

### 10%: Accent Color

- **Usage**: CTAs, highlights, attention-grabbing elements
- **Típus**: Complementary hot color VAGY high contrast cool color
- **Példák**: Vibrant Blue (#007bff), Warning Orange (#FFC107), Success Green (#28a745)
- **Mikor hot color OK**: CTA buttons, alerts, warnings, errors

**Why?**

- Kis mennyiség › felhívja figyelmet, de nem overwhelming
- Hot color 10%-ban = effective attention-grabbing

---

## ?? Color Usage Rules

### SOHA ne használd háttérnek

- ? Purple vagy magenta
- ? Red, orange, yellow
- ? Pink
- ? Bármilyen hot color

**Recommended backgrounds:**

- ? White (#FFFFFF)
- ? Off-white (#f8f9fa, #fafbfc)
- ? Light cool colors (light blue #e3f2fd, light green #e8f5e9)
- ? Neutral tones (light gray #f1f3f5)
- ? Subtle gradients (minimal color shift)

---

### SOHA ne használd text color-nak

- ? Yellow (poor contrast, unreadable)
- ? Pink (unprofessional, low contrast)
- ? Pure white on light background (invisible)
- ? Pure black on dark background (invisible)
- ? Light text on light background (<4.5:1 contrast)
- ? Dark text on dark background (<4.5:1 contrast)

**Recommended text colors:**

- ? Dark neutral (#1f2328, #24292f, #333333) **on light background**
- ? Near-black (#000000-#333333) **on white/light background**
- ? Dark gray (#4d4d4d, #6c757d) **on light background**
- ? Near-white (#ffffff, #f0f2f3) **on dark background**
- ? **ALWAYS check 4.5:1 contrast ratio (WCAG AA)**

**Text Contrast Checker:**

```markdown
| Text Color | Background | Contrast | WCAG AA |
| ---------- | ---------- | -------- | ------- |
| #1f2328 | #FFFFFF | 15.48:1 | ? Pass |
| #6c757d | #FFFFFF | 4.68:1 | ? Pass |
| #FFC107 | #FFFFFF | 1.83:1 | ? Fail (Yellow!) |
| #FFFFFF | #007bff | 4.54:1 | ? Pass |
| #000000 | #FFFFFF | 21:1 | ? Pass |
```

---

### Hot colors használata (ritkán!)

- ?? **Only for**: Critical alerts, warnings, errors, CTAs
- ?? **Small accent areas** › ne large surfaces
- ?? **Use icons + color** › ne csak color jelezzen

**Examples:**

- ? Error message: Red icon ? + red border + "Error: Invalid email" text
- ? Warning: Orange icon ?? + orange background + "Warning: Action cannot be undone"
- ? CTA Button: Blue background #007bff + white text "Get Started"
- ? Entire page red background (overwhelming, aggressive)

---

## ?? Color Palette Creation Template

```markdown
# Color Palette: [Project/Feature Név]

Created: [dátum]
Based on: 60-30-10 Rule
WCAG Level: AA (4.5:1 minimum contrast)

---

## 60%: Primary (Dominant)

**Color**: White (#FFFFFF)
**Usage**: Main background, page background, card backgrounds
**Percentage**: 60% of visual space
**Type**: Neutral light
**Accessibility**: High contrast base for text

**Alternative Primary Options:**
- Light Gray: #f8f9fa (softer than pure white)
- Light Blue: #e3f2fd (cool, calming)
- Off-white: #fafbfc (warmer than pure white)

---

## 30%: Secondary (Supporting)

**Color**: Light Gray (#e9ecef)
**Usage**: Sections, panels, cards, borders, dividers
**Percentage**: 30% of visual space
**Type**: Neutral cool
**Accessibility**: Contrast with white (#FFFFFF)

**Contrast Check:**
- Text (#1f2328) on Secondary (#e9ecef): 12.63:1 ? Pass

**Alternative Secondary Options:**
- Lighter Gray: #f1f3f5 (more subtle)
- Light Blue Gray: #e7f3ff (cool tone)

---

## 10%: Accent (Highlight)

**Color**: Primary Blue (#007bff)
**Usage**: CTA buttons, links, highlights, focus states, active states
**Percentage**: 10% of visual space
**Type**: Cool vibrant (complementary)
**Accessibility**: 4.54:1 contrast on white ?

**Contrast Check:**
- White text (#FFFFFF) on Accent (#007bff): 4.54:1 ? Pass
- Accent (#007bff) on White (#FFFFFF): 4.54:1 ? Pass

**Alternative Accent Options:**
- Vibrant Green: #28a745 (success-oriented brand)
- Vibrant Purple: #6f42c1 (creative brand)
- Orange: #FFC107 (energetic brand - USE CAREFULLY)

---

## Semantic Colors (Additional)

### Success
- **Color**: Green (#28a745)
- **Usage**: Success messages, positive feedback, confirmation
- **Contrast on White**: 4.54:1 ?

### Error
- **Color**: Red (#dc3545)
- **Usage**: Error messages, destructive actions, alerts
- **Contrast on White**: 5.48:1 ?
- **IMPORTANT**: Always use icon ? + text, not just color!

### Warning
- **Color**: Orange (#FFC107)
- **Usage**: Warning messages, caution states
- **Contrast on White**: 1.83:1 ? (Use with care, prefer text on white background)
- **IMPORTANT**: Dark text (#1f2328) on Warning background (#FFC107): 8.45:1 ?

### Info
- **Color**: Cyan (#17a2b8)
- **Usage**: Informational messages, help text
- **Contrast on White**: 4.54:1 ?

---

## Text Colors

### Primary Text
- **Color**: Near-black (#1f2328)
- **Usage**: Body text, headings, main content
- **Contrast on White**: 15.48:1 ? Excellent

### Secondary Text
- **Color**: Dark Gray (#6c757d)
- **Usage**: Supporting text, captions, metadata
- **Contrast on White**: 4.68:1 ? Pass

### Disabled Text
- **Color**: Light Gray (#adb5bd)
- **Usage**: Disabled form inputs, inactive states
- **Contrast on White**: 2.93:1 ?? (Intentionally lower for disabled state)

---

## Border & Divider Colors

- **Color**: Light Gray (#dee2e6)
- **Usage**: Borders, dividers, separators
- **Contrast on White**: Subtle, not meant for text

---

## Gradients (Optional)

### Primary Gradient
- **Colors**: White (#FFFFFF) › Light Blue (#e3f2fd)
- **Usage**: Hero sections, headers
- **Direction**: Top to bottom or diagonal
- **Rule**: Minimal color shift (stay within cool/light)

### Accent Gradient
- **Colors**: Primary Blue (#007bff) › Darker Blue (#0056b3)
- **Usage**: CTA buttons hover states
- **Direction**: Top to bottom

---

## Usage Examples

### Landing Page Layout
```

-¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¬
-  Header (60% Primary: White)        -
+¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦+
-  Hero Section (30% Secondary: Gray) -
-  -¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¬  -
-  - CTA Button (10% Accent: Blue) -  -
-  L¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦-  -
+¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦+
-  Content Section (60% Primary)      -
-  -¦¦¦¦¦¦¦¦¦¦¦¦¦¬  -¦¦¦¦¦¦¦¦¦¦¦¦¦¬  -
-  - Card (30%)  -  - Card (30%)  -  -
-  - -¦¦¦¦¦¦¦¦¦¬ -  - -¦¦¦¦¦¦¦¦¦¬ -  -
-  - -Link(10%)- -  - -Link(10%)- -  -
-  - L¦¦¦¦¦¦¦¦¦- -  - L¦¦¦¦¦¦¦¦¦- -  -
-  L¦¦¦¦¦¦¦¦¦¦¦¦¦-  L¦¦¦¦¦¦¦¦¦¦¦¦¦-  -
+¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦+
-  Footer (30% Secondary)             -
L¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦¦-

```

**Breakdown:**
- 60%: White backgrounds (header, content sections)
- 30%: Light gray (hero section, cards, footer)
- 10%: Blue accents (CTA buttons, links, highlights)

---

## Accessibility Validation

**WCAG AA Requirements:**
- [ ] All text colors pass 4.5:1 contrast (large text 3:1)
- [ ] Interactive elements pass 3:1 contrast to adjacent colors
- [ ] Focus states visible (2px outline, 3:1 contrast)
- [ ] Don't rely on color alone (use icons + text)

**Contrast Checker Tool:**
[WebAIM Contrast Checker](https://webaim.org/resources/contrastchecker/)

**Test Cases:**
| Test | Color Combo | Contrast | Pass? |
| ---- | ----------- | -------- | ----- |
| Body text | #1f2328 on #FFFFFF | 15.48:1 | ? |
| Secondary text | #6c757d on #FFFFFF | 4.68:1 | ? |
| Accent button | #FFFFFF on #007bff | 4.54:1 | ? |
| Error text | #dc3545 on #FFFFFF | 5.48:1 | ? |
| Warning (FAIL) | #FFC107 on #FFFFFF | 1.83:1 | ? |

**Warning color fix:**
- ? Don't use: Yellow text (#FFC107) on white
- ? Do use: Dark text (#1f2328) on yellow background (#FFC107) › 8.45:1 ?

---

## Anti-Patterns (KERÜLD!)

1. ? **Purple/Magenta background**
   - Overwhelming, unprofessional
   - Use: Accent only (10%), not background (60%)

2. ? **Yellow text**
   - Low contrast, unreadable
   - Use: Yellow background with dark text instead

3. ? **Red/Orange large surface**
   - Aggressive, eye strain
   - Use: Small accent (error messages, alerts)

4. ? **Pure white text on light background**
   - Invisible, no contrast
   - Use: Dark text on light background

5. ? **Color-only signals**
   - Color blind users miss information
   - Use: Color + icon + text

```

---

## ?? Execution Steps

### Step 1: Identify Brand/Feature

**Kérdések:**

1. Mi a brand personality? (professional, playful, energetic, calm)
2. Van meglévõ brand color? (logo color?)
3. Mi a target audience mood? (trust, urgency, creativity)

### Step 2: Choose Primary (60%)

**Általában**: White (#FFFFFF) vagy Light Gray (#f8f9fa)

**Decision tree:**

- Clean, minimal › White
- Softer, less stark › Off-white or Light Gray
- Brand-specific › Light brand color (ha cool color)

**NEVER:** Hot colors (red, orange, yellow, purple, pink)

### Step 3: Choose Secondary (30%)

**Általában**: Light Gray (#e9ecef) vagy Light Blue Gray

**Requirements:**

- Distinguishable from Primary (de nem nagy kontraszt)
- Cool vagy neutral tone
- Text contrast 4.5:1+ on this color

### Step 4: Choose Accent (10%)

**Általában**: Vibrant Blue (#007bff)

**Decision tree:**

- Professional, trust › Blue
- Success-oriented › Green
- Creative, luxury › Purple (USE CAREFULLY)
- Energetic, warm › Orange (USE SPARINGLY)

**Requirements:**

- High contrast on Primary (white text on accent › 4.5:1+)
- Visible but not overwhelming

### Step 5: Define Semantic Colors

**Always include:**

- Success (green)
- Error (red)
- Warning (orange - dark text on light orange background!)
- Info (cyan or blue)

**Accessibility:**

- NEVER rely on color alone › icon + text
- Error: ? icon + red border + "Error: ..." text

### Step 6: Validate Accessibility

**Tool**: [WebAIM Contrast Checker](https://webaim.org/resources/contrastchecker/)

**Check every color combination:**

- [ ] Body text on primary background: 4.5:1+
- [ ] Secondary text on primary background: 4.5:1+
- [ ] Accent button text on accent background: 4.5:1+
- [ ] Error/warning/success text: 4.5:1+

### Step 7: Document Color Palette

**Fájl neve:**

- `docs/design-system/color-palette.md`

**Tartalom:** Használd a fenti template-et

### Step 8: Create Design Tokens (Optional)

**CSS Variables:**

```css
:root {
  /* Primary (60%) */
  --color-background: #ffffff;

  /* Secondary (30%) */
  --color-surface: #e9ecef;

  /* Accent (10%) */
  --color-primary: #007bff;

  /* Semantic */
  --color-success: #28a745;
  --color-error: #dc3545;
  --color-warning: #FFC107;

  /* Text */
  --color-text-primary: #1f2328;
  --color-text-secondary: #6c757d;
}
```

---

## ?? Common Mistakes

1. ? **Túl sok color:**
   - Rossz: 10 különbözõ szín mindenhez
   - Jó: 3 fõ szín (60-30-10) + 4 semantic (success/error/warning/info)

2. ? **Hot color background:**
   - Rossz: Piros háttér az egész oldalon
   - Jó: Fehér háttér, piros accent error message-ben

3. ? **Yellow text:**
   - Rossz: Yellow (#FFC107) text on white › unreadable
   - Jó: Dark text (#1f2328) on yellow background (#FFC107)

4. ? **Contrast ellenõrzés nélkül:**
   - Rossz: "Ez szerintem elég kontrasztos"
   - Jó: WebAIM Contrast Checker › 4.5:1+ validated

5. ? **Csak color jelzés:**
   - Rossz: Piros text = error (color blind users?)
   - Jó: ? icon + piros border + "Error: ..." text

---

## ?? Related Skills

- **Used by**: `design_system_maintenance.knowledge.md` (design tokens)
- **Informs**: `accessibility_audit.knowledge.md` (contrast validation)
- **Output to**: Frontend Developer (CSS color variables)

---

## ?? Resources

**Source:**

- Based on [`html-css-style-color-guide.instructions.md`](../instructions/html-css-style-color-guide.instructions.md) (60-30-10 Rule, Color Usage Guidelines)

**Tools:**

- [WebAIM Contrast Checker](https://webaim.org/resources/contrastchecker/)
- [Coolors](https://coolors.co/) (palette generator)
- [Adobe Color](https://color.adobe.com/) (color wheel)
- [Material Design Color Tool](https://material.io/resources/color/)

**Further Reading:**

- "The Principles of Beautiful Web Design" by Jason Beaird
- "Refactoring UI" by Adam Wathan & Steve Schoger
- Nielsen Norman Group: Color in UI Design

---

**Output Example Path:**
`docs/design-system/color-palette.md`
