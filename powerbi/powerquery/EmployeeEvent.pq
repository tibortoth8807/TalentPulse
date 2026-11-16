// Power Query (M) for table "EmployeeEvent"  <-  data/fact_employee_event.csv
// Paste into: Home > Transform data > New Source > Blank Query > Advanced Editor, then rename the query to "EmployeeEvent".
// Requires the text parameter "DataFolder" (Manage Parameters), e.g. C:\Repos\TalentPulse\data\  (with trailing backslash).
let
    Source = Csv.Document(
        File.Contents(DataFolder & "fact_employee_event.csv"),
        [Delimiter = ",", Encoding = 65001, QuoteStyle = QuoteStyle.Csv]
    ),
    PromotedHeaders = Table.PromoteHeaders(Source, [PromoteAllScalars = true]),
    // Culture "en-US": the files use ISO dates and "." as decimal separator, independent of the PC's locale (e.g. hu-HU).
    Typed = Table.TransformColumnTypes(
        PromotedHeaders,
        {
        {"Date", type date},
        {"EmployeeKey", Int64.Type},
        {"EventType", type text},
        {"JobKey", Int64.Type},
        {"OrgUnitKey", Int64.Type},
        {"LocationKey", Int64.Type},
        {"FromJobKey", Int64.Type},
        {"ToJobKey", Int64.Type},
        {"Detail", type text}
        },
        "en-US"
    )
in
    Typed
