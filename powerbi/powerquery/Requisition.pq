// Power Query (M) for table "Requisition"  <-  data/fact_requisition.csv
// Paste into: Home > Transform data > New Source > Blank Query > Advanced Editor, then rename the query to "Requisition".
// Requires the text parameter "DataFolder" (Manage Parameters), e.g. C:\Repos\TalentPulse\data\  (with trailing backslash).
let
    Source = Csv.Document(
        File.Contents(DataFolder & "fact_requisition.csv"),
        [Delimiter = ",", Encoding = 65001, QuoteStyle = QuoteStyle.Csv]
    ),
    PromotedHeaders = Table.PromoteHeaders(Source, [PromoteAllScalars = true]),
    // Culture "en-US": the files use ISO dates and "." as decimal separator, independent of the PC's locale (e.g. hu-HU).
    Typed = Table.TransformColumnTypes(
        PromotedHeaders,
        {
        {"RequisitionKey", Int64.Type},
        {"RequisitionCode", type text},
        {"JobKey", Int64.Type},
        {"OrgUnitKey", Int64.Type},
        {"LocationKey", Int64.Type},
        {"Reason", type text},
        {"Status", type text},
        {"OpenedDate", type date},
        {"FilledDate", type date},
        {"StartDate", type date},
        {"CancelledDate", type date},
        {"HireSourceKey", Int64.Type},
        {"HireCostEur", Currency.Type},
        {"HiredEmployeeKey", Int64.Type},
        {"HiredApplicationKey", Int64.Type},
        {"DaysToFill", Int64.Type},
        {"DaysOpen", Int64.Type}
        },
        "en-US"
    )
in
    Typed
