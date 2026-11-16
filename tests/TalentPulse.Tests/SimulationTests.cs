using TalentPulse.Generator.Domain;
using TalentPulse.Generator.Export;
using TalentPulse.Generator.Simulation;

namespace TalentPulse.Tests;

/// <summary>
/// The generated data must be internally consistent - otherwise every KPI in the report is wrong.
/// These tests check the invariants a real HRIS extract would also satisfy.
/// </summary>
public sealed class SimulationTests
{
    private static readonly SimulationOptions SmallCompany =
        new(Seed: 7, Start: new DateOnly(2024, 1, 1), AsOf: new DateOnly(2025, 12, 31), InitialHeadcount: 400, AnnualGrowth: 0.06);

    private static readonly Lazy<SimulationResult> Result = new(() => new CompanySimulator(SmallCompany).Run());

    private static SimulationResult Data => Result.Value;

    [Fact]
    public void Same_seed_gives_identical_data()
    {
        var again = new CompanySimulator(SmallCompany).Run();

        Assert.Equal(Data.Employees.Count, again.Employees.Count);
        Assert.Equal(Data.Applications.Count, again.Applications.Count);
        Assert.Equal(Data.Employees[^1].HireDate, again.Employees[^1].HireDate);
        Assert.Equal(Data.Employees.Count(e => e.TerminationDate is not null), again.Employees.Count(e => e.TerminationDate is not null));
    }

    [Fact]
    public void Nothing_happens_after_the_as_of_date()
    {
        var asOf = SmallCompany.AsOf;

        Assert.All(Data.Employees, e => Assert.True(e.HireDate <= asOf && (e.TerminationDate is null || e.TerminationDate <= asOf)));
        Assert.All(Data.Events, e => Assert.True(e.Date <= asOf));
        Assert.All(Data.Applications, a =>
        {
            Assert.True(a.AppliedDate <= asOf);
            Assert.False(a.ScreenDate > asOf || a.InterviewDate > asOf || a.OfferDate > asOf || a.OfferAcceptedDate > asOf || a.StartDate > asOf);
        });
    }

    [Fact]
    public void Employees_leave_after_they_join()
    {
        Assert.All(Data.Employees.Where(e => e.TerminationDate is not null), e => Assert.True(e.TerminationDate > e.HireDate));
    }

    [Fact]
    public void Application_stages_are_in_chronological_order()
    {
        Assert.All(Data.Applications, a =>
        {
            DateOnly?[] stages = [a.AppliedDate, a.ScreenDate, a.InterviewDate, a.OfferDate, a.OfferAcceptedDate, a.StartDate];
            var present = stages.Where(d => d is not null).Select(d => d!.Value).ToList();
            Assert.Equal(present.Order(), present);
        });
    }

    [Fact]
    public void Every_filled_requisition_has_exactly_one_hire_who_started_on_the_start_date()
    {
        var filled = Data.Requisitions.Where(r => r.Status == RequisitionStatus.Filled).ToList();
        Assert.NotEmpty(filled);

        foreach (var requisition in filled)
        {
            var hires = Data.Applications.Where(a => a.RequisitionKey == requisition.Key && a.Outcome == "Hired").ToList();
            var hire = Assert.Single(hires);
            var employee = Data.Employees.Single(e => e.Key == requisition.HiredEmployeeKey);

            Assert.Equal(requisition.StartDate, employee.HireDate);
            Assert.Equal(hire.StartDate, employee.HireDate);
            Assert.Equal(requisition.HireSourceKey, employee.HireSourceKey);
        }
    }

    [Fact]
    public void Monthly_snapshot_contains_exactly_the_active_employees()
    {
        foreach (var monthEnd in Data.Snapshots.Select(s => s.MonthEnd).Distinct())
        {
            var expected = Data.Employees.Where(e => e.IsActiveOn(monthEnd)).Select(e => e.Key).Order().ToList();
            var actual = Data.Snapshots.Where(s => s.MonthEnd == monthEnd).Select(s => s.EmployeeKey).Order().ToList();
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void Kpis_are_in_a_realistic_range()
    {
        var summary = SimulationSummary.From(Data);

        Assert.InRange(summary.Attrition12Months, 0.06, 0.25);
        Assert.InRange(summary.AverageDaysToFill, 20, 120);
        Assert.InRange(summary.OfferAcceptanceRate, 0.6, 1.0);
        Assert.True(summary.HeadcountAsOf > summary.HeadcountAtStart * 0.95, "Company should roughly keep or grow its size.");
    }

    [Fact]
    public void Export_writes_all_star_schema_files()
    {
        var folder = Path.Combine(Path.GetTempPath(), "talentpulse-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var files = CsvExporter.Export(Data, folder);

            string[] expected =
            [
                "dim_date.csv", "dim_location.csv", "dim_org_unit.csv", "dim_job.csv", "dim_source.csv", "dim_employee.csv", "security_user_country.csv",
                "fact_headcount_monthly.csv", "fact_employee_event.csv", "fact_requisition.csv", "fact_application.csv",
            ];
            Assert.Equal(expected.Order(), files.Select(f => f.File).Order());
            Assert.All(files, f => Assert.True(f.Rows > 0, $"{f.File} is empty"));

            var header = File.ReadLines(Path.Combine(folder, "dim_employee.csv")).First().TrimStart('﻿');
            Assert.StartsWith("EmployeeKey,EmployeeCode,Gender", header);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Theory]
    [InlineData("2024-01-15", "2024-02-14", 0)]
    [InlineData("2024-01-15", "2024-02-15", 1)]
    [InlineData("2024-01-31", "2025-01-31", 12)]
    [InlineData("2025-01-01", "2024-01-01", 0)]
    public void Months_between_counts_full_months(string from, string to, int expected) =>
        Assert.Equal(expected, CompanySimulator.MonthsBetween(DateOnly.Parse(from), DateOnly.Parse(to)));
}
