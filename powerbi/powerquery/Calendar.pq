// Power Query (M) for table "Calendar"  <-  data/dim_date.csv
// Paste into: Home > Transform data > New Source > Blank Query > Advanced Editor, then rename the query to "Calendar".
// Requires the text parameter "DataFolder" (Manage Parameters), e.g. C:\Repos\TalentPulse\data\  (with trailing backslash).
let
    Source = Csv.Document(
        File.Contents(DataFolder & "dim_date.csv"),
        [Delimiter = ",", Encoding = 65001, QuoteStyle = QuoteStyle.Csv]
    ),
    PromotedHeaders = Table.PromoteHeaders(Source, [PromoteAllScalars = true]),
    // Culture "en-US": the files use ISO dates and "." as decimal separator, independent of the PC's locale (e.g. hu-HU).
    Typed = Table.TransformColumnTypes(
        PromotedHeaders,
        {
        {"Date", type date},
        {"Year", Int64.Type},
        {"Quarter", type text},
        {"MonthNumber", Int64.Type},
        {"MonthName", type text},
        {"YearMonth", type text},
        {"MonthStart", type date},
        {"IsMonthEnd", Int64.Type}
        },
        "en-US"
    )
in
    Typed
