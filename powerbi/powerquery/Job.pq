// Power Query (M) for table "Job"  <-  data/dim_job.csv
// Paste into: Home > Transform data > New Source > Blank Query > Advanced Editor, then rename the query to "Job".
// Requires the text parameter "DataFolder" (Manage Parameters), e.g. C:\Repos\TalentPulse\data\  (with trailing backslash).
let
    Source = Csv.Document(
        File.Contents(DataFolder & "dim_job.csv"),
        [Delimiter = ",", Encoding = 65001, QuoteStyle = QuoteStyle.Csv]
    ),
    PromotedHeaders = Table.PromoteHeaders(Source, [PromoteAllScalars = true]),
    // Culture "en-US": the files use ISO dates and "." as decimal separator, independent of the PC's locale (e.g. hu-HU).
    Typed = Table.TransformColumnTypes(
        PromotedHeaders,
        {
        {"JobKey", Int64.Type},
        {"JobTitle", type text},
        {"JobFamily", type text},
        {"Level", type text},
        {"LevelOrder", Int64.Type},
        {"Grade", Int64.Type},
        {"OrgUnitKey", Int64.Type}
        },
        "en-US"
    )
in
    Typed
