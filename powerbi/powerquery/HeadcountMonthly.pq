// Power Query (M) for table "HeadcountMonthly"  <-  data/fact_headcount_monthly.csv
// Paste into: Home > Transform data > New Source > Blank Query > Advanced Editor, then rename the query to "HeadcountMonthly".
// Requires the text parameter "DataFolder" (Manage Parameters), e.g. C:\Repos\TalentPulse\data\  (with trailing backslash).
let
    Source = Csv.Document(
        File.Contents(DataFolder & "fact_headcount_monthly.csv"),
        [Delimiter = ",", Encoding = 65001, QuoteStyle = QuoteStyle.Csv]
    ),
    PromotedHeaders = Table.PromoteHeaders(Source, [PromoteAllScalars = true]),
    // Culture "en-US": the files use ISO dates and "." as decimal separator, independent of the PC's locale (e.g. hu-HU).
    Typed = Table.TransformColumnTypes(
        PromotedHeaders,
        {
        {"MonthEnd", type date},
        {"EmployeeKey", Int64.Type},
        {"JobKey", Int64.Type},
        {"OrgUnitKey", Int64.Type},
        {"LocationKey", Int64.Type},
        {"FTE", Currency.Type},
        {"TenureMonths", Int64.Type},
        {"Age", Int64.Type}
        },
        "en-US"
    )
in
    Typed
