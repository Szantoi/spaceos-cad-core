# Datahaven Dashboard — UI/UX Design Brief

> Comprehensive design specification for SpaceOS agent infrastructure visualization dashboard

**Created:** 2026-06-20
**Version:** 1.0
**Target:** Grafikai tervező agent / UI designer
**Project:** SpaceOS Datahaven Dashboard (React 19)

---

## 📋 Executive Summary

**Mi ez?**
Real-time monitoring és management dashboard a SpaceOS agent infrastruktúrához. 17 autonomous AI agent működését vizualizálja, track-eli a task flow-t, és megjeleníti a planning pipeline-t.

**Miért kell?**
- 17 AI agent párhuzamosan dolgozik különböző projekteken
- Nehéz átlátni ki min dolgozik, mi van queue-ban, mi blokkolva
- Kell egy központi control center ahol mindent látunk

**Ki használja?**
- SpaceOS project lead (root terminal)
- DevOps engineer
- Planning/prioritization review
- Internal admin tool (nem customer-facing)

---

## 🎯 Fő Célok

### 1. **Transparency (Átláthatóság)**
Egy pillantással látni kell:
- Hány agent dolgozik most
- Mennyi work in progress van
- Hol vannak bottleneck-ek
- Mi van queue-ban

### 2. **Monitoring (Megfigyelés)**
Real-time insight kell:
- Terminal status (WORKING/IDLE)
- UNREAD inbox count
- Planning pipeline state
- Project timeline

### 3. **Decision Support (Döntéstámogatás)**
Segíteni kell:
- Priority döntéseket
- Resource allocation-t
- Blocker feloldását
- Timeline planning-et

---

## 📱 Oldalak Specifikációja

### **1. Dashboard Page (Főoldal)**

**URL:** `/`
**Cél:** Global overview minden agent-ről és metrikákról

#### Funkciók:
- **Global metrics cards:**
  - Total inbox messages
  - Total outbox messages
  - Total unread messages
  - Active sessions count
  - Total terminals count

- **Terminal grid (17 terminal card):**
  - Minden terminal egy card
  - Card információk:
    - Terminal név (kernel, orch, fe, joinery, etc.)
    - Inbox count
    - Outbox count
    - Unread inbox count
    - Unread outbox count
    - Status badge (WORKING/IDLE)
    - Last activity timestamp
  - Click → navigate to Kanban filtered by terminal

- **Service health indicator:**
  - Backend connection status
  - Last update timestamp

#### UI Elemek:
- **Metrics Cards:** 5 darab metric card a tetején
- **Terminal Grid:** 17 terminal card responsive grid-ben (3-4 oszlop desktop, 1-2 oszlop mobile)
- **Status Badges:** Color-coded (green = WORKING, gray = IDLE)
- **Refresh Button:** Manual refresh trigger
- **Auto-refresh:** Minden 60 másodpercben

#### Design Célok:
- Gyors scan-elhető
- Metrics at-a-glance
- Problem terminals azonnal látszanak (sok UNREAD = probléma)
- Clean, dense layout (sok info, kevés scroll)

---

### **2. Kanban Page (Workflow Board)**

**URL:** `/kanban`
**Cél:** Dual-track workflow visualization (Discovery + Delivery)

#### Funkciók:

**Discovery Track (Planning Pipeline):**
- **5 oszlop:** Ideas → Selected → Debate → Consensus → Queue
- Card-ok: Planning item-ek
- Card info: title, priority, confidence score
- WIP count minden oszlopban
- Click on card → modal with details

**Delivery Track (Execution Pipeline):**
- **Swimlane per terminal** (17 swimlane)
- **4 oszlop per swimlane:** Inbox → Working → Review → Done
- Card-ok: Mailbox messages
- Card info: message ID, priority, type
- WIP count per terminal
- Click on card → modal with message details

**Metrics Bar:**
- Discovery WIP total
- Delivery WIP total
- Active sessions count
- Throughput (later)
- Cycle time (later)

#### UI Elemek:
- **Dual-track layout:** 2 board egymás alatt vagy tab-okkal
- **Kanban columns:** Vertical columns per stage
- **Swimlanes:** Horizontal per terminal (delivery track)
- **Cards:** Draggable cards (later feature)
- **WIP Badges:** Count per column
- **Metrics Bar:** Top of page sticky
- **Filters:** Filter by terminal, priority, status
- **Modal:** Card detail view

#### Design Célok:
- Látni a complete flow-t (idea → done)
- Bottleneck-ek azonnal látszanak (sok card egy oszlopban)
- Terminal workload összehasonlítható (swimlane height)
- Real-time updates (SSE)

---

### **3. Planning Page (Planning Pipeline)**

**URL:** `/planning`
**Cél:** Planning pipeline 5-stage workflow részletes view

#### Funkciók:

**Pipeline Overview:**
- **5 stage metrics cards:**
  - Ideas count
  - Selected count
  - In Debate count
  - Consensus count
  - Queued count
- Last scan timestamp

**Planning Items List:**
- Filter by status (all / idea / selected / debate / consensus / queue)
- Filter by priority (all / critical / high / medium / low)
- Item cards showing:
  - Status badge
  - Priority badge
  - Title
  - Segment (if any)
  - Confidence score (if available)
  - Created date

#### UI Elemek:
- **Metrics Cards:** 5 stage cards (color-coded)
- **Filters:** 2 dropdown (status, priority)
- **Item List:** Scrollable list of planning items
- **Status Badges:** Color per stage (blue/purple/yellow/green/cyan)
- **Priority Badges:** Color per priority (red/orange/yellow/blue)
- **Confidence Score:** Percentage display
- **Empty State:** Message when no items

#### Design Célok:
- Pipeline flow látható (metrics cards)
- Filtering gyors és egyszerű
- Item details at-a-glance
- Planning process transparency

---

### **4. Projects Page (Gantt Timeline)**

**URL:** `/projects`
**Cél:** Project timeline visualization Gantt chart-tal

#### Funkciók:

**View Modes:**
- **List View:** Project cards vertical list
- **Gantt View:** Timeline visualization

**List View Elements:**
- Project card showing:
  - Project name
  - Status badge (planning/active/blocked/done)
  - Priority badge
  - Terminal assignment
  - Epic name (if any)
  - Progress percentage
  - Task completion (e.g., "5/10 tasks")
  - Progress bar
  - Date range (start → end)

**Gantt View Elements:**
- **Timeline:** 8-month window (-2 months to +6 months from today)
- **Month labels:** Header with month names
- **"Today" marker:** Vertical line at current date
- **Project bars:** Horizontal bars per project
  - Color by status (green=active, red=blocked, gray=done, blue=planning)
  - Width = duration
  - Position = start/end dates
  - Progress fill within bar
  - Hover tooltip with details
- **Milestones:** Diamond markers on timeline
- **Time scale selector:** Week / Month / Quarter

#### UI Elemek:
- **View Toggle:** Button group (List / Gantt)
- **Time Scale Dropdown:** Week/Month/Quarter selector (Gantt view)
- **Timeline Header:** Month labels with "Today" marker
- **Project Bars:** Interactive bars with progress fill
- **Milestone Markers:** Diamond shapes
- **Legend:** Status colors explained
- **Refresh Button:** Manual refresh

#### Design Célok:
- Timeline easy to scan
- Project dependencies látszanak (position/overlap)
- Progress at-a-glance
- Dual view flexibility (list for details, Gantt for timeline)

---

## 🎨 Design System Követelmények

### Színpaletta

**Background:**
```css
--bg-primary: #0a0e17      /* Main background */
--bg-secondary: #141821    /* Cards, panels */
--bg-tertiary: #1e2433     /* Input fields */
```

**Text:**
```css
--text-primary: #e2e8f0    /* Main text */
--text-secondary: #94a3b8  /* Secondary text, labels */
--text-muted: #64748b      /* Disabled, placeholder */
```

**Accent & Status:**
```css
--accent: #3b82f6          /* Primary blue */
--accent-hover: #2563eb    /* Hover state */
--accent-red: #ef4444      /* Error, critical */
--accent-green: #10b981    /* Success, active */
--accent-yellow: #f59e0b   /* Warning, medium priority */
--accent-purple: #8b5cf6   /* Info, selected state */
```

**Status Colors:**
```css
--status-planning: #3b82f6  /* Blue */
--status-active: #10b981    /* Green */
--status-blocked: #ef4444   /* Red */
--status-done: #6b7280      /* Gray */
--status-idle: #6b7280      /* Gray */
--status-working: #10b981   /* Green */
```

**Priority Colors:**
```css
--priority-critical: #ef4444  /* Red */
--priority-high: #f59e0b      /* Orange */
--priority-medium: #eab308    /* Yellow */
--priority-low: #3b82f6       /* Blue */
```

**Border:**
```css
--border: #1e293b          /* Card borders */
```

### Typography

**Font Family:**
```css
font-family: system-ui, -apple-system, 'Segoe UI', sans-serif;
```

**Font Sizes:**
- **Headings:**
  - H1: 2xl (1.5rem / 24px) — Page titles
  - H2: xl (1.25rem / 20px) — Section titles
  - H3: lg (1.125rem / 18px) — Card titles
- **Body:**
  - Base: 1rem (16px) — Normal text
  - Small: 0.875rem (14px) — Secondary info
  - XSmall: 0.75rem (12px) — Labels, badges

**Font Weights:**
- Regular: 400 — Body text
- Medium: 500 — Labels, emphasis
- Semibold: 600 — Card titles
- Bold: 700 — Page headings

### Spacing

**Consistent scale (Tailwind-based):**
```
1 = 0.25rem (4px)
2 = 0.5rem (8px)
3 = 0.75rem (12px)
4 = 1rem (16px)
6 = 1.5rem (24px)
8 = 2rem (32px)
```

**Common usages:**
- Card padding: 1rem (p-4)
- Section gap: 1.5rem (gap-6)
- Button padding: 0.5rem 1rem (py-2 px-4)

### Components

#### **Card**
```
Background: var(--bg-secondary)
Border: 1px solid var(--border)
Border-radius: 0.5rem (8px)
Padding: 1rem (16px)
Hover: border-color → var(--accent)
Transition: 150ms ease
```

#### **Button**
```
Primary:
  Background: var(--accent)
  Text: white
  Hover: var(--accent-hover)
  Padding: 0.5rem 1rem
  Border-radius: 0.5rem

Secondary:
  Background: transparent
  Border: 1px solid var(--border)
  Text: var(--text-secondary)
  Hover: text → var(--text-primary), border → var(--accent)
```

#### **Badge**
```
Padding: 0.25rem 0.5rem (py-1 px-2)
Border-radius: 0.25rem (4px)
Font-size: 0.75rem (12px)
Font-weight: 500
Border: 1px solid (matching background color)
Background: status-color/10 (10% opacity)
Text: status-color
```

#### **Progress Bar**
```
Height: 0.5rem (8px)
Background: var(--bg-primary)
Fill: var(--accent)
Border-radius: 9999px (fully rounded)
Transition: width 300ms ease
```

#### **Input**
```
Background: var(--bg-tertiary)
Border: 1px solid var(--border)
Text: var(--text-primary)
Placeholder: var(--text-muted)
Padding: 0.5rem 1rem
Border-radius: 0.5rem
Focus: border-color → var(--accent), outline: none
```

### Layout

**Header:**
- Height: auto (responsive padding)
- Background: var(--bg-secondary)
- Border-bottom: 1px solid var(--border)
- Sticky: top-0 (fixed while scrolling)

**Navigation:**
- Horizontal tabs/links
- Active state: underline + accent color
- Hover state: text color change

**Content Area:**
- Max-width: 100% (full width)
- Padding: 1.5rem (24px)
- Background: var(--bg-primary)

**Grid Layouts:**
- Dashboard: 3-4 columns (desktop), 1-2 columns (mobile)
- Kanban: Horizontal scroll if needed
- Responsive breakpoints: 640px (sm), 768px (md), 1024px (lg), 1280px (xl)

---

## 🔄 Interaktív Elemek

### **Auto-refresh**
- Minden oldal: 60 másodperc interval
- Visual indicator: "Last updated" timestamp
- Manual refresh button mindig elérhető

### **Hover States**
- Cards: border color change + subtle scale
- Buttons: background color change
- Project bars (Gantt): height increase + tooltip

### **Click Actions**
- Terminal card → Navigate to Kanban filtered by terminal
- Kanban card → Open modal with details
- Planning item → Expand/collapse details (later)
- Project bar → Show tooltip with project info

### **Loading States**
- Initial load: Spinner + "Loading..." text
- Refresh: Button disabled + "Refreshing..." text
- Skeleton screens for slow loads (later)

### **Error States**
- Network error: Red alert box with retry button
- 404: "Not found" message
- Auth error: Redirect to login overlay

### **Empty States**
- No projects: "No projects found" message
- No planning items: "Pipeline will generate ideas every 30 minutes"
- No data: Helpful message with action (e.g., "Refresh")

---

## 📊 Data Visualization Követelmények

### **Metrics Display**
- Large numbers (2xl font) for impact
- Small labels for context
- Color coding for status
- Icons optional but helpful

### **Gantt Chart**
- Clear timeline axis
- Month labels readable
- "Today" marker prominent
- Project bars distinct colors
- Progress visually clear (fill within bar)
- Milestones distinct from projects

### **Kanban Board**
- Columns equal width
- Cards fit comfortably (not cramped)
- WIP badges prominent
- Drag targets clear (later feature)
- Swimlane headers sticky

### **Status Indicators**
- Badges: rounded, color-coded, readable
- Icons: optional (dot, checkmark, warning)
- Tooltips: on hover for additional info

---

## 🎭 User Flow Példák

### **Flow 1: Check System Status**
1. Open Dashboard (`/`)
2. Scan global metrics cards → see 9 UNREAD messages
3. Scan terminal grid → find which terminals have UNREAD
4. Click terminal card → navigate to Kanban filtered by that terminal
5. See inbox messages in Kanban
6. Click card → see message details

### **Flow 2: Review Planning Pipeline**
1. Navigate to Planning (`/planning`)
2. See 5-stage metrics → 12 ideas, 5 in debate, 3 consensus
3. Filter by priority: "high"
4. Review high-priority items
5. Check confidence scores
6. Note which items ready for queue

### **Flow 3: Check Project Timeline**
1. Navigate to Projects (`/projects`)
2. Toggle to Gantt view
3. Scan timeline → see 3 active projects
4. Find "Today" marker
5. See which projects overdue (bar past today with <100% progress)
6. Hover project bar → see tooltip with details
7. Toggle to List view → see detailed task completion

### **Flow 4: Monitor Real-time Updates**
1. Open Dashboard
2. Leave tab open
3. Every 60 seconds: auto-refresh
4. New UNREAD inbox → metrics card updates
5. Terminal status changes IDLE → WORKING → status badge updates
6. Visual notification (optional): subtle flash on updated card

---

## 📐 Responsive Design Követelmények

### **Desktop (1280px+):**
- Dashboard: 4 columns terminal grid
- Kanban: Full dual-track visible
- Gantt: Full 8-month timeline visible
- Navigation: Horizontal tabs

### **Tablet (768px - 1279px):**
- Dashboard: 2-3 columns terminal grid
- Kanban: Horizontal scroll for columns
- Gantt: Horizontal scroll for timeline
- Navigation: Horizontal tabs (smaller)

### **Mobile (< 768px):**
- Dashboard: 1-2 columns terminal grid
- Kanban: Single column view (tabs per track)
- Gantt: Switch to List view recommended
- Navigation: Hamburger menu or bottom tabs

---

## 🚀 Performance Követelmények

### **Load Times:**
- Initial page load: < 2 seconds
- Page navigation: < 500ms
- Data refresh: < 1 second
- API response: < 500ms

### **Optimization:**
- Lazy load components
- Virtualize long lists (100+ items)
- Debounce search/filter inputs
- Cache API responses (60s)
- Gzip compression
- Code splitting per route

---

## ♿ Accessibility Követelmények

### **WCAG 2.1 Level AA:**
- Color contrast: 4.5:1 minimum (text/background)
- Keyboard navigation: all interactive elements
- Focus indicators: visible outline on focus
- ARIA labels: for icons and interactive elements
- Alt text: for images (if any)
- Semantic HTML: proper heading hierarchy

### **Screen Reader Support:**
- Status badges: aria-label with full text
- Metrics: aria-label with context
- Buttons: descriptive text or aria-label
- Links: descriptive text (not "click here")

---

## 🧪 Testing Követelmények

### **Browser Support:**
- Chrome 100+ ✅
- Firefox 100+ ✅
- Safari 15+ ✅
- Edge 100+ ✅

### **Visual Testing:**
- Light mode (if implemented later)
- Dark mode (current)
- High contrast mode
- Zoom levels: 100%, 125%, 150%, 200%

### **Interaction Testing:**
- Click all buttons
- Navigate all links
- Test all filters
- Verify auto-refresh
- Test error states
- Test empty states

---

## 📦 Deliverables (Mit várunk a designertől)

### **1. High-Fidelity Mockups**
- 4 page design (Dashboard, Kanban, Planning, Projects)
- Desktop + Mobile views
- All states: default, hover, active, loading, error, empty

### **2. Component Library**
- Card variants (terminal card, project card, planning item card, kanban card)
- Button variants (primary, secondary, disabled)
- Badge variants (status, priority)
- Input fields
- Progress bars
- Modals/overlays

### **3. Interactive Prototype (Optional)**
- Figma/Adobe XD prototype
- Navigation flow
- Hover states
- Click interactions

### **4. Design Specs**
- Color codes
- Font sizes & weights
- Spacing values
- Border radius values
- Shadow values (if used)
- Animation timing

### **5. Asset Export**
- Icons (SVG format)
- Logos (if any)
- Illustrations (if any)
- Export ready for React development

---

## 🎯 Success Metrics

**Design sikeres ha:**
- ✅ Egy pillantással látom a rendszer állapotát
- ✅ Azonnal megtalálom a problémákat (bottleneck, blocker, UNREAD)
- ✅ Minden funkció max 2 kattintás távolságra
- ✅ Nem kell tutorial, intuitív
- ✅ Működik desktop és mobile-on is
- ✅ Nincs információ overload, de minden adat elérhető
- ✅ Real-time updates észrevehetők de nem zavaróak
- ✅ Gyors (< 2s load, < 1s refresh)

---

## 📞 Kapcsolat & Feedback

**Project Owner:** SpaceOS Root Terminal
**Tech Stack:** React 19, TypeScript 6, Tailwind CSS 4
**Backend:** Express.js, Port 3456
**Deployment:** https://datahaven.joinerytech.hu

**Feedback Channel:** docs/mailbox/root/inbox/
**Design Review:** Weekly sprint review

---

**Verzió:** 1.0
**Utolsó frissítés:** 2026-06-20
**Készítette:** Claude Sonnet 4.5 (SpaceOS Root Terminal)

---

## 🔗 Referenciák

**Meglévő implementáció:**
- Location: `/opt/spaceos/datahaven-web/client/src/`
- README: `/opt/spaceos/datahaven-web/client/README.md`
- Components: `src/components/`
- Pages: `src/pages/`

**Design inspirációk (opcionális):**
- Linear.app (clean, fast, minimal)
- Vercel Dashboard (dark theme, metrics)
- GitHub Projects (kanban, timeline)
- Jira (sprint board, Gantt)

**Technikai korlátok:**
- React functional components only
- Tailwind utility classes preferred
- No external UI libraries (custom components)
- SSE for real-time updates
- File system as data source (no DB)

---

**Kérdések?** → Indíts új thread `docs/mailbox/root/inbox/` alatt!
