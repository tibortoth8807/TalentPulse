using System.Globalization;
using System.Text;
using TalentPulse.Generator.Domain;
using TalentPulse.Generator.Simulation;

namespace TalentPulse.Generator.Export;

/// <summary>
/// Writes the star schema as CSV files (UTF-8, comma separated, ISO dates, invariant decimals).
/// Power BI reads them with the Power Query scripts in /powerbi; the same files can be bulk-loaded into SQL.
/// </summary>
public static class CsvExporter
{
    public static IReadOnlyList<(string File, int Rows)> Export(SimulationResult result, string folder)
    {
        Directory.CreateDirectory(folder);
        var written = new List<(string, int)>();

        void Write<T>(string file, string[] header, IEnumerable<T> rows, Func<T, object?[]> map)
        {
            var count = WriteCsv(Path.Combine(folder, file), header, rows, map);
            written.Add((file, count));
        }

        var requisitionsByKey = result.Requisitions.ToDictionary(r => r.Key);
        var asOf = result.Options.AsOf;

        // ---- dimensions
        var firstDate = new DateOnly(result.Employees.Min(e => e.HireDate).Year, 1, 1);
        var lastDate = new DateOnly(asOf.Year, 12, 31);
        Write(
            "dim_date.csv",
            ["Date", "Year", "Quarter", "MonthNumber", "MonthName", "YearMonth", "MonthStart", "IsMonthEnd"],
            Enumerable.Range(0, lastDate.DayNumber - firstDate.DayNumber + 1).Select(firstDate.AddDays),
            d => [d, d.Year, $"Q{((d.Month - 1) / 3) + 1}", d.Month, CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(d.Month),
                  $"{d.Year}-{d.Month:D2}", new DateOnly(d.Year, d.Month, 1), d.AddDays(1).Day == 1]);

        Write(
            "dim_location.csv",
            ["LocationKey", "City", "Country", "Region"],
            ReferenceData.Locations,
            l => [l.Key, l.City, l.Country, l.Region]);

        Write(
            "dim_org_unit.csv",
            ["OrgUnitKey", "Function", "Department"],
            ReferenceData.Departments,
            d => [d.Key, d.Function, d.Name]);

        Write(
            "dim_job.csv",
            ["JobKey", "JobTitle", "JobFamily", "Level", "LevelOrder", "Grade", "OrgUnitKey"],
            result.Jobs,
            j => [j.Key, j.Title, j.JobFamily, j.Level.ToString(), (int)j.Level, j.Grade, j.DepartmentKey]);

        Write(
            "dim_source.csv",
            ["SourceKey", "Source", "Channel", "CostPerHireEur"],
            ReferenceData.Sources,
            s => [s.Key, s.Name, s.Channel, s.CostPerHireEur]);

        Write(
            "dim_employee.csv",
            ["EmployeeKey", "EmployeeCode", "Gender", "BirthYear", "HireDate", "TerminationDate", "TerminationType", "TerminationReason",
             "HireSourceKey", "WorkModel", "EmploymentType", "FTE", "OrgUnitKey", "LocationKey", "CurrentJobKey", "IsActive"],
            result.Employees,
            e => [e.Key, e.Code, e.Gender, e.BirthYear, e.HireDate, e.TerminationDate, e.TerminationType, e.TerminationReason,
                  e.HireSourceKey, e.WorkModel, e.EmploymentType, e.Fte, e.DepartmentKey, e.LocationKey,
                  result.Jobs.First(j => j.DepartmentKey == e.DepartmentKey && j.Level == e.Level).Key, e.IsActiveOn(asOf)]);

        // Demo users for dynamic row-level security (fictional e-mail addresses on a reserved .example domain).
        (string Email, string Country)[] security =
        [
            ("hr.budapest@northwind.example", "Hungary"),
            ("hr.dach@northwind.example", "Germany"),
            ("hr.emea@northwind.example", "Hungary"), ("hr.emea@northwind.example", "Germany"),
            ("hr.emea@northwind.example", "Poland"), ("hr.emea@northwind.example", "Portugal"),
            .. ReferenceData.Locations.Select(l => ("hr.global@northwind.example", l.Country)),
        ];
        Write("security_user_country.csv", ["UserEmail", "Country"], security, s => [s.Email, s.Country]);

        // ---- facts
        Write(
            "fact_headcount_monthly.csv",
            ["MonthEnd", "EmployeeKey", "JobKey", "OrgUnitKey", "LocationKey", "FTE", "TenureMonths", "Age"],
            result.Snapshots,
            s => [s.MonthEnd, s.EmployeeKey, s.JobKey, s.DepartmentKey, s.LocationKey, s.Fte, s.TenureMonths, s.Age]);

        Write(
            "fact_employee_event.csv",
            ["Date", "EmployeeKey", "EventType", "JobKey", "OrgUnitKey", "LocationKey", "FromJobKey", "ToJobKey", "Detail"],
            result.Events.OrderBy(e => e.Date).ThenBy(e => e.EmployeeKey),
            e => [e.Date, e.EmployeeKey, e.EventType, e.JobKey, e.DepartmentKey, e.LocationKey, e.FromJobKey, e.ToJobKey, e.Detail]);

        Write(
            "fact_requisition.csv",
            ["RequisitionKey", "RequisitionCode", "JobKey", "OrgUnitKey", "LocationKey", "Reason", "Status", "OpenedDate", "FilledDate",
             "StartDate", "CancelledDate", "HireSourceKey", "HireCostEur", "HiredEmployeeKey", "HiredApplicationKey", "DaysToFill", "DaysOpen"],
            result.Requisitions,
            r => [r.Key, r.Code, r.JobKey, r.DepartmentKey, r.LocationKey, r.Reason.ToString(), r.Status.ToString(), r.OpenedDate, r.FilledDate,
                  r.StartDate, r.CancelledDate, r.HireSourceKey, r.HireCostEur, r.HiredEmployeeKey, r.HiredApplicationKey,
                  r.FilledDate is { } filled ? filled.DayNumber - r.OpenedDate.DayNumber : null,
                  r.Status == RequisitionStatus.Open ? asOf.DayNumber - r.OpenedDate.DayNumber : null]);

        Write(
            "fact_application.csv",
            ["ApplicationKey", "RequisitionKey", "JobKey", "OrgUnitKey", "LocationKey", "CandidateCode", "SourceKey", "AppliedDate", "ScreenDate", "InterviewDate", "OfferDate",
             "OfferAcceptedDate", "StartDate", "Outcome", "FurthestStage", "FurthestStageOrder",
             "ReachedScreen", "ReachedInterview", "ReachedOffer", "IsHire", "DaysToHire"],
            result.Applications,
            a => [a.Key, a.RequisitionKey, requisitionsByKey[a.RequisitionKey].JobKey, requisitionsByKey[a.RequisitionKey].DepartmentKey,
                  requisitionsByKey[a.RequisitionKey].LocationKey, a.CandidateCode, a.SourceKey, a.AppliedDate, a.ScreenDate, a.InterviewDate, a.OfferDate,
                  a.OfferAcceptedDate, a.StartDate, a.Outcome, a.FurthestStage.ToString(), (int)a.FurthestStage,
                  a.FurthestStage >= Stage.Screened, a.FurthestStage >= Stage.Interviewed, a.FurthestStage >= Stage.Offered,
                  a.OfferAcceptedDate is not null,
                  a.OfferAcceptedDate is { } acc ? acc.DayNumber - a.AppliedDate.DayNumber : null]);

        return written;
    }

    private static int WriteCsv<T>(string path, string[] header, IEnumerable<T> rows, Func<T, object?[]> map)
    {
        using var writer = new StreamWriter(path, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)) { NewLine = "\n" };
        writer.WriteLine(string.Join(',', header));

        var count = 0;
        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',', map(row).Select(Format)));
            count++;
        }

        return count;
    }

    private static string Format(object? value) => value switch
    {
        null => string.Empty,
        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        bool b => b ? "1" : "0",
        decimal m => m.ToString(CultureInfo.InvariantCulture),
        double x => x.ToString(CultureInfo.InvariantCulture),
        int i => i.ToString(CultureInfo.InvariantCulture),
        string s => Escape(s),
        _ => Escape(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty),
    };

    private static string Escape(string value) =>
        value.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : value;
}
