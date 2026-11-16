// Power Query (M) for table "Application"  <-  data/fact_application.csv
// Paste into: Home > Transform data > New Source > Blank Query > Advanced Editor, then rename the query to "Application".
// Requires the text parameter "DataFolder" (Manage Parameters), e.g. C:\Repos\TalentPulse\data\  (with trailing backslash).
let
    Source = Csv.Document(
        File.Contents(DataFolder & "fact_application.csv"),
        [Delimiter = ",", Encoding = 65001, QuoteStyle = QuoteStyle.Csv]
    ),
    PromotedHeaders = Table.PromoteHeaders(Source, [PromoteAllScalars = true]),
    // Culture "en-US": the files use ISO dates and "." as decimal separator, independent of the PC's locale (e.g. hu-HU).
    Typed = Table.TransformColumnTypes(
        PromotedHeaders,
        {
        {"ApplicationKey", Int64.Type},
        {"RequisitionKey", Int64.Type},
        {"JobKey", Int64.Type},
        {"OrgUnitKey", Int64.Type},
        {"LocationKey", Int64.Type},
        {"CandidateCode", type text},
        {"SourceKey", Int64.Type},
        {"AppliedDate", type date},
        {"ScreenDate", type date},
        {"InterviewDate", type date},
        {"OfferDate", type date},
        {"OfferAcceptedDate", type date},
        {"StartDate", type date},
        {"Outcome", type text},
        {"FurthestStage", type text},
        {"FurthestStageOrder", Int64.Type},
        {"ReachedScreen", Int64.Type},
        {"ReachedInterview", Int64.Type},
        {"ReachedOffer", Int64.Type},
        {"IsHire", Int64.Type},
        {"DaysToHire", Int64.Type}
        },
        "en-US"
    )
in
    Typed
