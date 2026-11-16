# Building the Power BI report

Power BI Desktop (free, Windows) – about 1–2 hours the first time. The result is saved as a **Power BI Project (.pbip)**,
so the report and the semantic model are plain text files that git can diff.

## 0. One-time setup

1. Install Power BI Desktop (Microsoft Store).
2. File → Options and settings → Options → **Preview features** → enable *Power BI Project (.pbip) save option* and
   *Store semantic model using TMDL format* (if your version still lists them as preview).
3. Options → **Current file → Data load** → untick *Autodetect new relationships after data is loaded*.
4. Generate the data (repo root): `dotnet run --project src/TalentPulse.Generator -- --out data`

## 1. Load the data (Power Query)

1. Home → Transform data → **Manage Parameters → New**: `DataFolder`, type *Text*, value = full path of the repo's `data\` folder **with trailing backslash**.
2. For each file in [`powerquery/`](powerquery): New Source → **Blank Query** → Advanced Editor → paste → rename the query to the file name
   (`Calendar`, `Location`, `OrgUnit`, `Job`, `Source`, `Employee`, `SecurityUserCountry`, `HeadcountMonthly`, `EmployeeEvent`, `Requisition`, `Application`).
3. Close & Apply.

> The scripts convert types with culture `en-US`, so ISO dates and `.` decimals load correctly on a Hungarian (or any) Windows locale.

## 2. Model

Follow [`model.md`](model.md):

1. Mark `Calendar` as date table.
2. Create the relationships (Model view) – check the inactive `Requisition[FilledDate]` one.
3. New table `FunnelStage`, calculated columns, sort-by columns, hide key columns.
4. **DAX query view** → paste [`measures.dax`](measures.dax) → Run → *Update model with changes*. The `EVALUATE` at the end prints the headline KPIs –
   they should match the numbers the generator printed in the console.
5. Set measure formats.
6. Security: create the **Country HR** role and test with *View as*.

## 3. Report

Build the pages from [`report-spec.md`](report-spec.md), apply the theme from [`theme/`](theme).

## 4. Save and commit

1. File → **Save as** → type *Power BI project files (*.pbip)* → save into `powerbi/TalentPulse.pbip`.
   This creates `TalentPulse.Report/` and `TalentPulse.SemanticModel/` folders.
2. Export screenshots of every page into `docs/screenshots/` (they are shown in the main README).
3. Optional: File → Export → PDF → `docs/TalentPulse.pdf` - lets anyone see the report without Power BI.
4. `git add powerbi docs` and commit. `.gitignore` already excludes the local cache files (`.pbi/localSettings.json`, `cache.abf`).

## Checklist before you call it done

- [ ] No "Sum of …" / "Count of …" default titles
- [ ] Every visual has a title that answers a question
- [ ] KPI cards match the generator's console summary
- [ ] RLS tested with two different users
- [ ] Screenshots in the README
