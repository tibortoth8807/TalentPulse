// Power Query (M) for table "Employee"  <-  data/dim_employee.csv
// Paste into: Home > Transform data > New Source > Blank Query > Advanced Editor, then rename the query to "Employee".
// Requires the text parameter "DataFolder" (Manage Parameters), e.g. C:\Repos\TalentPulse\data\  (with trailing backslash).
let
    Source = Csv.Document(
        File.Contents(DataFolder & "dim_employee.csv"),
        [Delimiter = ",", Encoding = 65001, QuoteStyle = QuoteStyle.Csv]
    ),
    PromotedHeaders = Table.PromoteHeaders(Source, [PromoteAllScalars = true]),
    // Culture "en-US": the files use ISO dates and "." as decimal separator, independent of the PC's locale (e.g. hu-HU).
    Typed = Table.TransformColumnTypes(
        PromotedHeaders,
        {
        {"EmployeeKey", Int64.Type},
        {"EmployeeCode", type text},
        {"Gender", type text},
        {"BirthYear", Int64.Type},
        {"HireDate", type date},
        {"TerminationDate", type date},
        {"TerminationType", type text},
        {"TerminationReason", type text},
        {"HireSourceKey", Int64.Type},
        {"WorkModel", type text},
        {"EmploymentType", type text},
        {"FTE", Currency.Type},
        {"OrgUnitKey", Int64.Type},
        {"LocationKey", Int64.Type},
        {"CurrentJobKey", Int64.Type},
        {"IsActive", Int64.Type}
        },
        "en-US"
    )
in
    Typed
