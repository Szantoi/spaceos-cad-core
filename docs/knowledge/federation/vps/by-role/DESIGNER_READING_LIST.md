# Designer (Vízió) Tudásbázis — Reading List

> **Gyakorlati design és UX források** a SpaceOS Designer terminál számára
>
> Fókusz: Dashboard design, industrial UI, accessibility, dark theme

**Last updated:** 2026-06-30
**Maintained by:** Librarian

---

## 📦 SPACEOS BELSŐ DOKUMENTÁCIÓ

### Design Briefs

| Fájl | Tartalom | Használat |
|------|----------|-----------|
| `docs/design/DATAHAVEN_UI_DESIGN_BRIEF.md` | Datahaven Dashboard design spec | Agent monitoring UI patterns |
| `docs/design/llm-koordin-ci-s-projekt-ui/` | LLM Coordination Panel UI handoff | Figma → React workflow példa |

**Összefoglaló:**
- Datahaven Dashboard: Real-time agent monitoring, 17 terminal vizualizáció
- Target: Internal admin tool (nem customer-facing)
- Goals: Transparency, Monitoring, Decision Support

---

### SpaceOS Screenshots

| Lokáció | Tartalom |
|---------|----------|
| `docs/joinerytech/screenshots/` | Design dashboard screenshots (dash2-5.png, catalog-ui.png) |
| Root projekt screenshots | `dashboard-final.png`, `kanban-final.png`, `projects-gantt-final.png`, `planning-final.png` |

**Használat:** Vizuális referencia meglévő UI mintákhoz

---

### Component Patterns

| Fájl | Tartalom |
|------|----------|
| `docs/joinerytech/ui.jsx` | UI kit komponensek (apakovasz projekt) |
| `docs/joinerytech/design-item-wizard.jsx` | Wizard pattern implementáció |

**Tanulság:** React komponens struktúra, wizard flow UX

---

## 🎨 DATAHAVEN SPECIFIKUS — CSS & DESIGN SYSTEM

### CSS Struktúra

```
datahaven-web/public/css/
├── styles.css          — Global design system (color palette, typography)
├── planning.css        — Planning pipeline UI
├── kanban.css          — Dual-track kanban board
└── projects.css        — Gantt timeline & project list
```

### Színpaletta

**Dark theme (alapértelmezett):**
```css
--bg-primary: #0f1419       /* Főháttér (sötét) */
--bg-secondary: #1a1f26     /* Másodlagos háttér */
--bg-card: #242b33          /* Kártya háttér */

--text-primary: #e7e9ea     /* Elsődleges szöveg (világos) */
--text-secondary: #8b98a5   /* Másodlagos szöveg (halványabb) */

--accent-blue: #1d9bf0      /* Info, links */
--accent-green: #00ba7c     /* Success, DONE */
--accent-yellow: #ffd400    /* Warning, PENDING */
--accent-red: #f4212e       /* Error, BLOCKED */
--accent-purple: #7856ff    /* Special, priority */

--border-color: #2f3336     /* Borders */
--shadow: 0 4px 12px rgba(0, 0, 0, 0.4)  /* Card shadow */
```

**Semantic color usage:**
- **Green:** DONE, SUCCESS, WORKING status
- **Yellow:** PENDING, IN_PROGRESS
- **Red:** BLOCKED, ERROR
- **Blue:** Info, navigation links
- **Purple:** Priority tasks, special highlights

### Tipográfia

```css
font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
line-height: 1.5;
```

**System font stack:** Native OS fonts (fastest, best rendering)

---

## 🌐 KÜLSŐ FORRÁSOK — READING LIST

### Design Systems & Component Libraries

#### Tailwind CSS
**URL:** https://tailwindcss.com/docs
**Miért hasznos:**
- Utility-first CSS framework (SpaceOS React komponensek használják)
- Dark mode support (class="dark:bg-gray-900")
- Responsive design utilities

**Kulcs koncepciók:**
- Utility classes (bg-blue-500, text-white, p-4)
- Dark mode toggle (dark: prefix)
- Flexbox/Grid utilities

---

#### Shadcn/ui
**URL:** https://ui.shadcn.com
**Miért hasznos:**
- Copy-paste React komponensek (nem NPM dependency)
- Tailwind + Radix UI alapokon
- Accessible out-of-the-box

**Hasznos komponensek SpaceOS-nek:**
- Dashboard layouts
- Card components
- Badge/Status indicators
- Tooltip/Popover (agent status details)

---

#### Radix UI
**URL:** https://www.radix-ui.com/primitives/docs/overview/introduction
**Miért hasznos:**
- Accessibility-first primitives
- Keyboard navigation
- ARIA attributes built-in

**Kulcs komponensek:**
- Dropdown Menu (terminal actions)
- Dialog/Modal (task details)
- Tabs (Dashboard/Kanban/Planning/Projects nézetek)

---

### UX Best Practices

#### Nielsen Norman Group
**URL:** https://www.nngroup.com
**Miért hasznos:**
- UX kutatás gold standard
- Dashboard design heuristics
- Data visualization best practices

**Hasznos cikkek:**
- "Dashboard Design Patterns" (executive dashboards)
- "Hick's Law" (decision time vs options count)
- "F-Pattern" (how users scan dashboards)

**SpaceOS alkalmazás:**
- Datahaven Dashboard: F-pattern layout (terminal list bal, metrics jobb)
- Kanban: Minimize cognitive load (dual-track = max 2 swimlane)

---

#### Laws of UX
**URL:** https://lawsofux.com
**Miért hasznos:**
- 20 UX törvény vizualizálva
- SpaceOS-re alkalmazható minták

**Releváns törvények:**
1. **Hick's Law** — Több opció = lassabb döntés
   - SpaceOS: Terminal actions max 3-4 gomb (wake/stop/logs/details)
2. **Fitts's Law** — Célpont méret vs távolság
   - SpaceOS: Priority buttons nagyobbak, közelebb
3. **Jakob's Law** — Users expect familiarity
   - SpaceOS: Kanban board = Trello-szerű (users know it)

---

#### Refactoring UI
**URL:** https://www.refactoringui.com
**Miért hasznos:**
- Gyakorlati UI design tips (nem elméleti)
- Visual hierarchy, spacing, typography

**Kulcs tanulságok:**
- **Visual hierarchy:** Size, color, weight (not just size!)
- **Whitespace:** Padding > Border (cleaner look)
- **Color palette:** 60-30-10 rule (primary 60%, secondary 30%, accent 10%)

**SpaceOS alkalmazás:**
- Datahaven: Dark background 60%, card backgrounds 30%, accent colors 10%

---

### Dashboard Design Specifikus

#### Dashboard Design Patterns (Smashing Magazine)
**URL:** https://www.smashingmagazine.com/2020/03/designing-better-dashboard/
**Miért hasznos:**
- Executive vs operational dashboards
- KPI visualization
- Real-time data display

**SpaceOS kontextus:**
- Datahaven = Operational dashboard (real-time agent status)
- KPIs: WORKING/IDLE ratio, UNREAD inbox count, queue depth

---

#### Data Visualization Best Practices
**Forrás:** Edward Tufte, "The Visual Display of Quantitative Information"
**Miért hasznos:**
- Minimize chart junk
- Data-ink ratio maximization
- Small multiples pattern

**SpaceOS alkalmazás:**
- Projects Gantt: Small multiples (8 terminals × timeline)
- Kanban: Card density vs readability balance

---

#### Dark Theme Design
**URL:** https://material.io/design/color/dark-theme.html (Material Design Guide)
**Miért hasznos:**
- Dark theme elevation (shadow vs overlay opacity)
- Color contrast ratios (WCAG AA compliance)
- Surface colors (pure black #000 vs dark gray #0f1419)

**SpaceOS decision:**
- ✅ Dark gray background (#0f1419) NOT pure black
- ✅ Elevated cards (+4dp shadow, lighter background #242b33)
- ✅ Text contrast: #e7e9ea on #0f1419 = 15.8:1 (WCAG AAA)

---

## ♿ ACCESSIBILITY (A11Y)

### WCAG 2.1 Guidelines
**URL:** https://www.w3.org/WAI/WCAG21/quickref/
**Miért hasznos:**
- AA compliance minimum (government contracts)
- Color contrast ratios
- Keyboard navigation

**SpaceOS compliance:**
- ✅ Color contrast: Text 15.8:1 (AAA), Links 4.5:1 (AA)
- ✅ Keyboard navigation: Tab order logical, Escape closes modals
- ⏳ TODO: Screen reader support (ARIA labels)

---

### WebAIM Contrast Checker
**URL:** https://webaim.org/resources/contrastchecker/
**Használat:**
- Ellenőrizd minden új color pair-t (background + text)
- Minimum: 4.5:1 (AA normal text), 3:1 (AA large text)

**SpaceOS példa:**
- #e7e9ea text on #0f1419 background = 15.8:1 ✅ (AAA)
- #1d9bf0 link on #0f1419 background = 8.2:1 ✅ (AAA)

---

## 🚀 INDUSTRIAL/B2B UI PATTERNS

### B2B Dashboard Examples

| Platform | URL | Tanulság |
|----------|-----|----------|
| **Linear** | linear.app | Clean, fast, keyboard-first |
| **Retool** | retool.com | Internal tool patterns |
| **Grafana** | grafana.com | Real-time monitoring, dark theme |

**SpaceOS inspiráció:**
- Linear: Keyboard shortcuts (G → D = Dashboard, G → K = Kanban)
- Retool: Component library approach (reusable widgets)
- Grafana: Dark theme + real-time SSE updates

---

### Manufacturing/Industrial UI

**Forrás:** SCADA UI patterns, industrial HMI design
**Miért hasznos:**
- SpaceOS customer = Joinery manufacturing (ajtó/szekrény gyártók)
- Industrial users expect: high information density, minimal decoration

**Pattern:**
- **Status indicators:** Traffic light colors (green/yellow/red)
- **Alarm panel:** Prioritized list (critical → warning → info)
- **Timeline view:** Gantt chart (Projects page pattern)

**SpaceOS alkalmazás:**
- Datahaven terminal status: Green dot (WORKING), Gray dot (IDLE)
- Kanban BLOCKED cards: Red badge (high visibility)

---

## 🔧 SPACEOS-SPECIFIKUS USE CASES

### Use Case 1: Agent Monitoring Dashboard

**Design challenge:** 17 terminals × real-time status
**Solution pattern:** Grid layout (3×6 cards) + SSE updates

**Referencia:**
- Grafana dashboard grid
- Linear project board compact view

**Kulcs döntés:**
- Compact cards (terminal name, status dot, inbox count)
- No unnecessary decoration (borderless cards, subtle shadow)

---

### Use Case 2: Dual-Track Kanban

**Design challenge:** Discovery vs Delivery track separation
**Solution pattern:** Horizontal swim lanes (2 track)

**Referencia:**
- Jira board swim lanes
- Linear roadmap view

**Kulcs döntés:**
- Discovery track = Planning pipeline (idea → consensus → queue)
- Delivery track = Terminal work (7 terminal swimlane)

---

### Use Case 3: Gantt Timeline (Projects)

**Design challenge:** 8 terminals × 8 month timeline
**Solution pattern:** Small multiples + horizontal scroll

**Referencia:**
- GitHub Projects timeline
- Notion timeline view

**Kulcs döntés:**
- Compact row height (24px/terminal)
- Color-coded epics (Kernel blue, Joinery green, Cutting yellow)
- Current date marker (vertical red line)

---

## 📚 SPACEOS DESIGN PRINCIPLES (extracted from DATAHAVEN_UI_DESIGN_BRIEF.md)

### 1. Transparency (Átláthatóság)
Egy pillantással látni kell:
- Hány agent dolgozik most
- Mennyi work in progress van
- Hol vannak bottleneck-ek

### 2. Monitoring (Megfigyelés)
Real-time insight:
- Terminal status (WORKING/IDLE)
- UNREAD inbox count
- Planning pipeline state

### 3. Decision Support (Döntéstámogatás)
Segíteni kell:
- Priority döntéseket
- Resource allocation-t
- Blocker feloldását

**Design consequence:**
- High information density (no wasted space)
- Real-time updates (SSE, not polling)
- Color-coded status (semantic colors: green/yellow/red)

---

## 🎯 KÖVETKEZŐ LÉPÉSEK (Designer terminál)

### Immediate Actions

1. **Read:** `docs/design/DATAHAVEN_UI_DESIGN_BRIEF.md` (teljes spec)
2. **Study:** Datahaven CSS (`public/css/styles.css`) — color palette, spacing
3. **Browse:** External reading list (Tailwind, Shadcn, Laws of UX)

### Skill Development

**Week 1-2:**
- Tailwind CSS fundamentals (utility classes, dark mode)
- Radix UI accessibility patterns (ARIA, keyboard nav)

**Week 3-4:**
- Dashboard design patterns (Nielsen Norman Group articles)
- Data visualization (Tufte principles, small multiples)

**Month 2:**
- B2B UI exploration (Linear, Retool, Grafana)
- Industrial UI patterns (SCADA HMI design)

### SpaceOS-Specific Practice

**Component design exercises:**
1. Terminal status card (compact, real-time, color-coded)
2. Kanban card (title, status, priority badge)
3. Gantt timeline row (epic bar, milestone markers)

---

## 🔗 QUICK LINKS

| Kategória | Link |
|-----------|------|
| **Tailwind CSS** | https://tailwindcss.com/docs |
| **Shadcn/ui** | https://ui.shadcn.com |
| **Radix UI** | https://www.radix-ui.com |
| **Nielsen Norman** | https://www.nngroup.com |
| **Laws of UX** | https://lawsofux.com |
| **Refactoring UI** | https://www.refactoringui.com |
| **WCAG 2.1** | https://www.w3.org/WAI/WCAG21/quickref/ |
| **WebAIM Contrast** | https://webaim.org/resources/contrastchecker/ |

---

**Last updated:** 2026-06-30
**Next review:** 2026-08-30 (bi-monthly)
**Maintained by:** Librarian (knowledge curator)
