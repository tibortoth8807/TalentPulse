# Workforce Planning – Report Page Specification

## Page: "Workforce Planning (12M)"

This page answers: **"Given my growth and attrition assumptions, how many people do I need to hire in the next 12 months — and how many recruiters do I need?"**

---

## Layout overview

```
┌─────────────────────────────────────────────────────────────────┐
│  SLICERS (top bar)                                               │
│  [Growth Rate %]  [Attrition Rate %]  [Hires / Recruiter / Year]│
├───────────┬───────────┬───────────┬───────────┬─────────────────┤
│  CARD     │  CARD     │  CARD     │  CARD     │  CARD           │
│  Base HC  │ Proj. HC  │ Req Hires │ Req Recs  │ Recruiter Gap   │
│  (actual) │ (12M)     │ (12M)     │ (12M FTE) │ (+/- vs today)  │
├───────────┴───────────┴─────────────────────────────────────────┤
│  LINE CHART: HC Projection                                       │
│  X: PlanningMonth[Month Label]                                   │
│  Y: [Plan: HC at Month]  (one line per selected growth scenario) │
│  Reference line at Base HC                                       │
├─────────────────────────────────────────────────────────────────┤
│  CLUSTERED BAR: Monthly Hires Required                           │
│  X: PlanningMonth[Month Label]                                   │
│  Y: [Plan: Hires at Month]                                       │
├───────────────────────┬─────────────────────────────────────────┤
│  TABLE: Scenario      │  TABLE: Recruiter Capacity               │
│  Summary              │  Breakdown                               │
│  Row | Value          │  Metric               | Value            │
│  ---                  │  ---                                     │
│  Base HC              │  Current Recruiters                      │
│  Projected HC 12M     │  Required Recruiters                     │
│  Growth Delta         │  Recruiter Gap                           │
│  Replacement Hires    │  Avg Time to Fill                        │
│  Required Hires 12M   │  Open Pipeline Size                      │
│  Actual Attrition R12M│  Monthly Hires Target                    │
│  Scenario Attrition   │                                          │
│  Attrition Delta      │                                          │
└───────────────────────┴─────────────────────────────────────────┘
```

---

## Slicers

| Slicer | Table | Column | Type | Default |
|---|---|---|---|---|
| Annual Growth Rate | `WhatIf_GrowthRate` | `Growth Rate` | Between (slider) | 5% |
| Annual Attrition Rate | `WhatIf_AttritionRate` | `Attrition Rate` | Between (slider) | 13% |
| Hires / Recruiter / Year | `WhatIf_RecruiterCapacity` | `Hires per Recruiter per Year` | Between (slider) | 30 |

> [!IMPORTANT]
> The slicers for the what-if tables must **NOT** be connected to the existing OrgUnit/Calendar slicers via Edit Interactions. The planning measures use `REMOVEFILTERS()` internally, so they always show company-wide totals regardless of other page filters. If you want department-level planning, duplicate the page and remove `REMOVEFILTERS()` from the base measures.

---

## KPI Cards

| Card | Measure | Format | Conditional formatting |
|---|---|---|---|
| Base Headcount | `[Plan: Base Headcount]` | `#,0` | None |
| Projected HC (12M) | `[Plan: Projected Headcount 12M]` | `#,0` | Green if > Base, red if < Base |
| Required Hires (12M) | `[Plan: Required Hires 12M]` | `#,0` | Gradient: low=green, high=red |
| Required Recruiters | `[Plan: Required Recruiters]` | `#,0.0` | None |
| Recruiter Gap | `[Plan: Recruiter Gap]` | `+#,0.0;-#,0.0;0` | Positive=red, negative=green |

---

## Line chart: HC Projection

- **X-axis**: `PlanningMonth[Month Label]` (sort by `PlanningMonth[Month Offset]`)
- **Y-axis**: `[Plan: HC at Month]`
- **Reference line**: constant at `[Plan: Base Headcount]` value (add via Analytics pane)
- **Tooltip**: Month Offset, Projected HC, Monthly Hires at Month

> [!TIP]
> To show multiple growth scenarios simultaneously: duplicate the visual, pin both to the same page, and use a bookmark navigator to switch between "Optimistic / Base / Conservative" scenarios. Alternatively, use a small multiples layout with 3 fixed slicer values.

---

## Bar chart: Monthly Hires Required

- **X-axis**: `PlanningMonth[Month Label]`
- **Y-axis**: `[Plan: Hires at Month]`
- **Data label**: on, showing the value
- Note: hires are spread evenly; in reality front-load hiring if you have a long time-to-fill

---

## Scenario summary table (left)

Build with a **New table** visual (not a matrix):

| Metric | Value |
|---|---|
| Base Headcount | `[Plan: Base Headcount]` |
| Projected HC (12M) | `[Plan: Projected Headcount 12M]` |
| Growth Delta | `[Plan: Headcount Growth Delta]` |
| Replacement Hires | `[Plan: Replacement Hires 12M]` |
| **Required Hires (12M)** | **`[Plan: Required Hires 12M]`** |
| Actual Attrition (R12M) | `[Plan: Actual Attrition R12M]` |
| Scenario Attrition | `[Attrition Rate Value]` |
| Scenario vs Actual | `[Plan: Attrition Scenario vs Actual]` |

---

## Recruiter capacity table (right)

| Metric | Value |
|---|---|
| Current Recruiters | `[Plan: Current Recruiters]` |
| Required Recruiters | `[Plan: Required Recruiters]` |
| **Recruiter Gap** | **`[Plan: Recruiter Gap]`** |
| Hires / Recruiter (scenario) | `[Recruiter Capacity Value]` |
| Avg Time to Fill | `[Plan: Avg Time to Fill]` |
| Open Pipeline Size | `[Plan: Open Pipeline at Any Time]` |
| Monthly Hire Target | `[Plan: Hires at Month]` |

---

## Interactivity notes

- **No cross-filtering** from the HC projection chart to the cards (disable via Edit Interactions) — the cards should always show the full-company scenario total
- The `PlanningMonth` table has **no relationship** to Calendar — it's an independent axis. Keep it that way.
- The existing `Calendar` date slicer from other pages should be **hidden** on this page (page-level filter: Calendar[Date] is not on this page)
- Add a **Reset to defaults** bookmark that clears all three slicers back to 5% growth / 13% attrition / 30 hires per recruiter

---

## Assumptions & caveats (add as a text box on the page)

> **Modell feltételezések:**
> - Növekedés: éves arány, havonként egyenletesen elosztva (tőkésített kamat-módszer)
> - Fluktuáció: lineáris közelítés, az átlagos létszámon alkalmazva (trapéz-módszer)
> - Recruiter kapacitás: bruttó hire/év, nem veszi figyelembe a toborzási tölcsér hatékonyságát
> - Az „Open Pipeline" becslésnél a jelenlegi time-to-fill átlag az alap
> - Élesben: a tervszámokat üzleti területenként külön kell kalkulálni (a szűrők eltávolítása nélkül)
