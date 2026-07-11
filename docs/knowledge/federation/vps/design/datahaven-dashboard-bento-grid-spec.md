# Datahaven Dashboard — Bento Grid Design Spec

> **Státusz:** APPROVED | **Verziószám:** 1.0 | **Frissítve:** 2026-06-30

---

## 1. Layout Grid (12 Column)

### Desktop (≥1200px)

```
┌───────────────────────────────────────────┐
│   KPI Strip (6 cards, sticky top)          │  ← 12/12 col, 80px height
├───────────────────┬───────────────────────┤
│   Terminal Grid   │   Activity Feed       │  ← 8/12 + 4/12 col
│   (8 terminál)    │   (Live events)       │
├───────────────────┼───────────────────────┤
│   Kanban Mini     │   Pipeline Status     │  ← 6/12 + 6/12 col
│   (Discovery)     │   (Queue/Health)      │
└───────────────────┴───────────────────────┘
```

### Tablet (768-1199px)

```
┌────────────────────────────┐
│   KPI Strip (3 visible)    │  ← Horizontal scroll
├────────────────────────────┤
│   Terminal Grid (4 col)    │  ← Full width
├────────────────────────────┤
│   Activity Feed            │  ← Full width
├────────────────────────────┤
│   Kanban Mini              │  ← Full width
└────────────────────────────┘
```

### Mobile (≤767px)

```
┌────────────────────┐
│ KPI Strip (scroll) │  ← Horizontal scroll
├────────────────────┤
│ Terminal Grid 2col │
├────────────────────┤
│ Activity Feed      │
├────────────────────┤
│ Kanban Mini        │
└────────────────────┘
```

---

## 2. Color System

### Dark Theme (Default)

```css
/* Backgrounds */
--bg-primary: #0f1419;      /* Page background */
--bg-card: #1a1d23;         /* Card background */
--bg-hover: #242931;        /* Hover state */
--bg-active: #2f3440;       /* Active/selected */

/* Text */
--text-primary: #e7e9ea;    /* Primary text */
--text-secondary: #8b949e;  /* Secondary text */
--text-muted: #6b7280;      /* Disabled/inactive */

/* Borders */
--border-default: #30363d;  /* Default border */
--border-hover: #484f58;    /* Hover border */

/* Status Colors */
--status-success: #00ba7c;  /* Green - healthy */
--status-warning: #ffd400;  /* Yellow - warning */
--status-error: #f4212e;    /* Red - critical */
--status-info: #1d9bf0;     /* Blue - info */

/* Accent */
--accent: #1d9bf0;          /* Primary accent */
--accent-hover: #1a8cd8;    /* Accent hover */
```

### Light Theme (Inverted)

```css
--bg-primary: #ffffff;
--bg-card: #f6f8fa;
--text-primary: #1f2328;
--text-secondary: #656d76;
--border-default: #d0d7de;
--status-success: #1a7f37;
--status-warning: #9a6700;
--status-error: #cf222e;
```

---

## 3. Component Specs

### Card Component

```css
.card {
  background: var(--bg-card);
  border: 1px solid var(--border-default);
  border-radius: 12px;
  padding: 16px;
  transition: all 0.2s ease;
}

.card:hover {
  background: var(--bg-hover);
  border-color: var(--border-hover);
  box-shadow: 0 4px 12px rgba(0,0,0,0.3);
}
```

### KPI Card

```css
.kpi-card {
  min-width: 140px;
  min-height: 80px;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  touch-action: manipulation; /* 44px touch target */
}

.kpi-card--healthy { border-left: 3px solid var(--status-success); }
.kpi-card--warning { border-left: 3px solid var(--status-warning); }
.kpi-card--critical { border-left: 3px solid var(--status-error); }
```

### Button

```css
.btn-primary {
  background: var(--accent);
  color: white;
  padding: 10px 20px;
  border-radius: 8px;
  min-height: 44px; /* Touch target */
  transition: background 0.2s;
}

.btn-primary:hover {
  background: var(--accent-hover);
}

.btn-primary:active {
  transform: scale(0.98);
}
```

---

## 4. Typography

```css
/* Font Stack */
--font-sans: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
--font-mono: "Fira Code", "Consolas", "Monaco", monospace;

/* Font Sizes */
--text-xs: 12px;
--text-sm: 14px;
--text-base: 16px;
--text-lg: 18px;
--text-xl: 20px;
--text-2xl: 24px;

/* Line Heights */
--leading-tight: 1.25;
--leading-normal: 1.5;
--leading-relaxed: 1.75;

/* Font Weights */
--font-normal: 400;
--font-medium: 500;
--font-semibold: 600;
--font-bold: 700;
```

---

## 5. Spacing System (8px Base)

```css
--space-1: 4px;   /* xs */
--space-2: 8px;   /* sm */
--space-3: 12px;  /* md-sm */
--space-4: 16px;  /* md */
--space-5: 20px;  /* md-lg */
--space-6: 24px;  /* lg */
--space-8: 32px;  /* xl */
--space-10: 40px; /* 2xl */
--space-12: 48px; /* 3xl */

/* Grid Gap */
--grid-gap: 16px;         /* Desktop */
--grid-gap-mobile: 12px;  /* Mobile */
```

---

## 6. Accessibility (WCAG AA)

| Elem | Minimális kontraszt | Aktuális |
|------|---------------------|----------|
| Text / bg-primary | 4.5:1 | ✅ 13.2:1 |
| Text-secondary / bg-primary | 4.5:1 | ✅ 5.4:1 |
| Status colors / bg-card | 3:1 (large text) | ✅ |

### Keyboard Navigation

- Tab order: KPI Strip → Terminal Grid → Activity → Kanban
- Focus visible: `outline: 2px solid var(--accent); outline-offset: 2px;`
- Skip link: "Skip to main content"

### Motion

```css
@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after {
    animation-duration: 0.01ms !important;
    transition-duration: 0.01ms !important;
  }
}
```

---

## 7. Responsive Breakpoints

```css
/* Mobile first approach */
/* Default: Mobile (≤767px) */

@media (min-width: 480px) {
  /* Small tablets, large phones */
}

@media (min-width: 768px) {
  /* Tablets */
  .grid-container {
    grid-template-columns: repeat(6, 1fr);
  }
}

@media (min-width: 1200px) {
  /* Desktop */
  .grid-container {
    grid-template-columns: repeat(12, 1fr);
  }
}

@media (min-width: 1600px) {
  /* Large desktop */
  .grid-container {
    max-width: 1536px;
    margin: 0 auto;
  }
}
```

---

## 8. Micro-interactions

### Hover Effects

```css
.interactive {
  transition: all 0.2s ease;
}

.interactive:hover {
  background: var(--bg-hover);
  transform: translateY(-1px);
}
```

### Loading State

```css
@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.6; }
}

.loading {
  animation: pulse 1.5s ease-in-out infinite;
}
```

### Click Feedback

```css
.clickable:active {
  transform: scale(0.98);
  transition: transform 0.1s;
}
```

---

## 9. Implementation Checklist

- [x] Layout grid definition (12 col, responsive)
- [x] Color system (dark + light theme)
- [x] Component specs (card, button, KPI)
- [x] Typography scale
- [x] Spacing system (8px base)
- [x] Accessibility guidelines (WCAG AA)
- [x] Micro-interaction specs

---

## References

- [UX Design Principles](../knowledge/patterns/UX_DESIGN_PRINCIPLES.md)
- [Apple HIG - Dark Mode](https://developer.apple.com/design/human-interface-guidelines/dark-mode)
- [Material Design 3 - Color](https://m3.material.io/styles/color/overview)
