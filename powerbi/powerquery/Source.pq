// Power Query (M) for table "Source"  <-  data/dim_source.csv
// Paste into: Home > Transform data > New Source > Blank Query > Advanced Editor, then rename the query to "Source".
// Requires the text parameter "DataFolder" (Manage Parameters), e.g. C:\Repos\TalentPulse\data\  (with trailing backslash).
let
    Source = Csv.Document(
        File.Contents(DataFolder & "dim_source.csv"),
        [Delimiter = ",", Encoding = 65001, QuoteStyle = QuoteStyle.Csv]
    ),
    PromotedHeaders = Table.PromoteHeaders(Source, [PromoteAllScalars = true]),
    // Culture "en-US": the files use ISO dates and "." as decimal separator, independent of the PC's locale (e.g. hu-HU).
    Typed = Table.TransformColumnTypes(
        PromotedHeaders,
        {
        {"SourceKey", Int64.Type},
        {"Source", type text},
        {"Channel", type text},
        {"CostPerHireEur", Currency.Type}
        },
        "en-US"
    )
in
    Typed
