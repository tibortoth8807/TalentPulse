# Power BI – TalentPulse

## Project format

The model is stored as a **Power BI Project (PBIP)** with **TMDL** (Tabular Model Definition Language), which means every table, measure, and relationship lives in a plain-text file — fully diffable and reviewable in GitHub.

```
powerbi/
├── TalentPulse.pbip              # project pointer (open this in Power BI Desktop)
├── TalentPulse.Dataset/          # semantic model (TMDL)
│   ├── model.tmdl                # model-level settings + table/role references
│   ├── expressions.tmdl          # DataFolder parameter
│   ├── relationships.tmdl        # all relationships
│   ├── roles.tmdl                # Row-Level Security roles
│   └── tables/                   # one .tmdl file per table
│       ├── Calendar.tmdl
│       ├── Employee.tmdl
│       ├── OrgUnit.tmdl
│       ├── Location.tmdl
│       ├── Job.tmdl
│       ├── Source.tmdl
│       ├── SecurityUserCountry.tmdl
│       ├── FunnelStage.tmdl
│       ├── HeadcountMonthly.tmdl
│       ├── EmployeeEvent.tmdl
│       ├── Requisition.tmdl
│       ├── Application.tmdl
│       ├── WhatIf_GrowthRate.tmdl        # what-if: growth parameter (-20%..+30%)
│       ├── WhatIf_AttritionRate.tmdl     # what-if: attrition parameter (5%..40%)
│       ├── WhatIf_RecruiterCapacity.tmdl # what-if: recruiter capacity parameter (10..80 hires/yr)
│       ├── PlanningMonth.tmdl            # 12-month horizon projection axis
│       └── WorkforcePlan.tmdl            # 14 workforce planning & capacity measures
├── TalentPulse.Report/           # report stub
│   ├── definition.pbir
│   └── report.json
├── bpa-rules.json                # Tabular Editor 2 BPA rule set
├── bpa-run.sh                    # local BPA helper script
├── workforce-plan-spec.md        # layout and visual specs for workforce planning page
└── report-spec.md                # visual specification
```

## Opening the model

1. Install **Power BI Desktop** (June 2023 or later — TMDL support required).
2. Open `powerbi/TalentPulse.pbip`.
3. Update the **DataFolder** parameter (Home → Transform data → Parameters) to point to your local `data/` folder.

## Schema overview

### Star schema

```
                    ┌──────────┐
                    │ Calendar │
                    └────┬─────┘
                         │
      ┌──────────┐   ┌───┴──────────────┐   ┌─────────────┐
      │ Employee ├───┤ HeadcountMonthly ├───┤   OrgUnit   │
      └──────────┘   └──────────────────┘   └─────────────┘
                              │
                         ┌────┴────┐   ┌──────────┐
                         │Location │   │   Job    │
                         └─────────┘   └──────────┘

Fact tables: HeadcountMonthly, EmployeeEvent, Requisition, Application
Dim tables:  Calendar, Employee, OrgUnit, Location, Job, Source
Helper:      SecurityUserCountry (RLS), FunnelStage (calculated)
```

### Measures (40+)

| Domain | Key measures |
|--------|-------------|
| **Headcount** | Headcount, FTE, Average Headcount, Headcount PY, Headcount YoY % |
| **Workforce** | Average Tenure (years), Average Age, Female %, Remote / Hybrid % |
| **Attrition** | Attrition Rate, Attrition Rate R12M, Voluntary Attrition R12M, Early Leavers % |
| **Movement** | Hires, Leavers, Promotions, Net Change, Promotion Rate R12M |
| **Recruiting** | Requisitions Opened/Filled, Open Requisitions, Avg/Median Time to Fill, Cost per Hire |
| **Pipeline** | Applications, Screen/Interview/Offer rates, Offer Acceptance Rate, Funnel Count |
| **Workforce Planning (What-If)** | Base HC, Projected HC 12M, Required Hires 12M, Required Recruiters, Recruiter Gap, HC/Hires at Month |

### Row-Level Security

The `Country HR` role restricts rows in `Location`, `Employee`, and `SecurityUserCountry` to the signed-in user's assigned countries, using `USERPRINCIPALNAME()` to look up `SecurityUserCountry`.

## CI / Best Practice Analyzer

Every PR runs **Tabular Editor 2 BPA** against `bpa-rules.json` (10 rules across Model Design, DAX Best Practices, Documentation, Formatting, and Performance categories). Violations at severity ≥ 2 fail the build.

To run BPA locally:

```bash
# Linux / macOS (requires mono)
bash powerbi/bpa-run.sh

# Windows (TabularEditor.exe must be on PATH or in te2/)
TabularEditor.exe "powerbi/TalentPulse.Dataset" -A "powerbi/bpa-rules.json" -V 2
```

Results are saved to `bpa-results.trx` (TRX format, viewable in VS / VS Code Test Explorer).

## DataFolder parameter

The `DataFolder` M expression parameter points to the CSV data files. Update it to your local path in Power BI Desktop, or override it in a deployment pipeline parameter.

Default value: `C:\Repos\TalentPulse\data\`
