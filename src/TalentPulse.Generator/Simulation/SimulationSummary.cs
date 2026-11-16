using TalentPulse.Generator.Domain;

namespace TalentPulse.Generator.Simulation;

/// <summary>Headline KPIs, computed the same way as the DAX measures - handy to cross-check the Power BI report.</summary>
public sealed record SimulationSummary(
    int HeadcountAtStart, int HeadcountAsOf, double Attrition12Months, double AverageDaysToFill, double OfferAcceptanceRate)
{
    public static SimulationSummary From(SimulationResult result)
    {
        var asOf = result.Options.AsOf;
        var yearAgo = asOf.AddMonths(-12);

        var firstMonthEnd = result.Snapshots.Min(s => s.MonthEnd);
        var headcountAtStart = result.Snapshots.Count(s => s.MonthEnd == firstMonthEnd);
        var headcountAsOf = result.Employees.Count(e => e.IsActiveOn(asOf));

        // Attrition = leavers in the last 12 months / average month-end headcount over the same months.
        var monthEnds = result.Snapshots.Select(s => s.MonthEnd).Distinct().Where(d => d > yearAgo).ToList();
        var averageHeadcount = monthEnds.Count == 0
            ? 0
            : result.Snapshots.Count(s => s.MonthEnd > yearAgo) / (double)monthEnds.Count;
        var leavers = result.Employees.Count(e => e.TerminationDate is { } t && t > yearAgo && t <= asOf);

        var filled = result.Requisitions.Where(r => r.FilledDate is not null).ToList();
        var averageDaysToFill = filled.Count == 0
            ? 0
            : filled.Average(r => r.FilledDate!.Value.DayNumber - r.OpenedDate.DayNumber);

        var offers = result.Applications.Count(a => a.OfferDate is not null && a.Outcome is "Hired" or "OfferAccepted" or "OfferDeclined");
        var accepted = result.Applications.Count(a => a.OfferAcceptedDate is not null);

        return new SimulationSummary(
            headcountAtStart,
            headcountAsOf,
            averageHeadcount == 0 ? 0 : leavers / averageHeadcount,
            averageDaysToFill,
            offers == 0 ? 0 : accepted / (double)offers);
    }
}
