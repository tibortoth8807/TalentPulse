# Report pages

Canvas 16:9 (1280 × 720). Theme: `theme/talentpulse.json` (View → Themes → Browse for themes).
Every page has the same slicer header: **Year** (`Calendar[Year]`), **Function** (`OrgUnit[Function]`), **Region / Country** (`Location`).
Sync the slicers across pages (View → Sync slicers).

Visual names below are the Power BI built-in visuals - no custom visuals needed.

---

## 1. Executive Overview – "How is our workforce doing?"

| Area | Visual | Fields |
|---|---|---|
| Top row | 6 × **Card (new)** | `Headcount` (+ `Headcount YoY %` as reference label), `Attrition Rate R12M`, `Voluntary Attrition R12M`, `Open Requisitions`, `Avg Time to Fill (days)`, `Offer Acceptance Rate` |
| Left, big | **Line chart** | X: `Calendar[YearMonth]` · Y: `Headcount` · secondary Y: `Attrition Rate R12M` |
| Right | **Clustered column** | X: `Calendar[YearMonth]` · Y: `Hires`, `Leavers` (green / red) |
| Bottom left | **Bar chart** | Y: `Location[Country]` · X: `Headcount` |
| Bottom right | **Donut** | Legend: `OrgUnit[Function]` · Values: `Headcount` |
| Footer | **Text box** | "Data as of" + card with `As Of Date` |

Story: headcount grows ~6% a year while attrition stays ~14%; hires keep up with leavers.

## 2. Recruiting Funnel – "Where do we lose candidates, and which source is worth the money?"

| Area | Visual | Fields |
|---|---|---|
| Left | **Funnel** | Category: `FunnelStage[Stage]` · Values: `Funnel Count` · tooltip: `Funnel Conversion %` |
| Top right | 4 × **Card** | `Applications`, `Accepted Offers`, `Applications per Hire`, `Avg Time to Hire (days)` |
| Middle right | **Table** "Source efficiency" | `Source[Source]`, `Applications`, `Screen Pass Rate`, `Accepted Offers`, `Source Hire Rate`, `Cost per Hire (EUR)` – conditional formatting (data bars on hire rate, color scale on cost) |
| Bottom left | **Clustered column** | X: `Job[Level]` · Y: `Avg Time to Fill (days)`, `Median Time to Fill (days)` |
| Bottom right | **Scatter** | Details: `Source[Source]` · X: `Cost per Hire (EUR)` · Y: `Source Hire Rate` · Size: `Accepted Offers` |

Story: referrals convert best at moderate cost, agencies convert well but are by far the most expensive, job boards bring volume but low quality.

## 3. Attrition & Retention – "Who leaves, when and why?"

| Area | Visual | Fields |
|---|---|---|
| Top | 3 × **Card** | `Leavers`, `Attrition Rate R12M`, `Early Leavers %` |
| Left | **Line chart** | X: `Calendar[YearMonth]` · Y: `Attrition Rate R12M` · Legend: `OrgUnit[Function]` |
| Middle | **Matrix heatmap** | Rows: `OrgUnit[Department]` · Columns: `Location[Country]` · Values: `Attrition Rate R12M` · background color scale (white → red) |
| Right | **Bar chart** | Y: `Employee[TerminationReason]` · X: `Leavers` · Legend: `Employee[TerminationType]` |
| Bottom | **Column chart** | X: `EmployeeEvent[Tenure at Event (months)]` binned (New group → bin size 6) · Y: `Leavers` |

Story: Customer Support and Inside Sales in Bengaluru and Austin are the hotspots; the first year is the riskiest.

## 4. Workforce Structure – "What does our organization look like?"

| Area | Visual | Fields |
|---|---|---|
| Left | **Stacked bar** | Y: `OrgUnit[Function]` · X: `Headcount` · Legend: `Job[Level]` |
| Middle | **100% stacked column** | X: `OrgUnit[Function]` · Y: `Headcount` · Legend: `Employee[Gender]` |
| Right top | **Donut** | Legend: `Employee[WorkModel]` · Values: `Headcount` |
| Right bottom | **Column** | X: `HeadcountMonthly[Age Band]` · Y: `Headcount` |
| Bottom | **Table** | `OrgUnit[Department]`, `Headcount`, `FTE`, `Average Tenure (years)`, `Average Age`, `Female %`, `Promotion Rate R12M` |

## 5. Drill-through: Department detail

Drill-through field: `OrgUnit[Department]`. Cards (Headcount, Attrition R12M, Open Requisitions, Avg Time to Fill),
headcount trend line, open requisitions table (`Requisition[RequisitionCode]`, `Job[JobTitle]`, `Location[City]`, `Requisition[OpenedDate]`, `Requisition[DaysOpen]`).

## Finishing touches (they show seniority)

- **Tooltips page** (Page info → Allow use as tooltip) with a mini trend for the hovered department.
- **Field parameter** to switch the main KPI on page 1 (Modeling → New parameter → Fields: Headcount, Hires, Leavers, Open Requisitions).
- **Bookmarks + buttons** for "Absolute / Rate" view on the attrition page.
- **Alt text** on every visual (accessibility).
- Consistent number formats from `model.md`, no default "Sum of …" titles.
