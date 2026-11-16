// Power Query (M) for table "Location"  <-  data/dim_location.csv
// Paste into: Home > Transform data > New Source > Blank Query > Advanced Editor, then rename the query to "Location".
// Requires the text parameter "DataFolder" (Manage Parameters), e.g. C:\Repos\TalentPulse\data\  (with trailing backslash).
let
    Source = Csv.Document(
        File.Contents(DataFolder & "dim_location.csv"),
        [Delimiter = ",", Encoding = 65001, QuoteStyle = QuoteStyle.Csv]
    ),
    PromotedHeaders = Table.PromoteHeaders(Source, [PromoteAllScalars = true]),
    // Culture "en-US": the files use ISO dates and "." as decimal separator, independent of the PC's locale (e.g. hu-HU).
    Typed = Table.TransformColumnTypes(
        PromotedHeaders,
        {
        {"LocationKey", Int64.Type},
        {"City", type text},
        {"Country", type text},
        {"Region", type text}
        },
        "en-US"
    )
in
    Typed
