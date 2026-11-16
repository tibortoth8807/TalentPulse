using TalentPulse.Generator.Domain;

namespace TalentPulse.Generator.Simulation;

public sealed record SimulationOptions(int Seed, DateOnly Start, DateOnly AsOf, int InitialHeadcount, double AnnualGrowth)
{
    public static SimulationOptions Default { get; } = new(42, new DateOnly(2023, 1, 1), new DateOnly(2026, 10, 31), 1800, 0.06);
}

public sealed record SimulationResult(
    SimulationOptions Options,
    IReadOnlyList<Job> Jobs,
    IReadOnlyList<Employee> Employees,
    IReadOnlyList<Requisition> Requisitions,
    IReadOnlyList<Application> Applications,
    IReadOnlyList<EmployeeEvent> Events,
    IReadOnlyList<HeadcountSnapshot> Snapshots);

/// <summary>
/// Month-by-month simulation of a fictional company:
/// attrition (driven by department, location, tenure, level and season), backfill and growth requisitions,
/// a full recruiting funnel per requisition, hires, and yearly promotions. Deterministic for a given seed.
/// </summary>
public sealed class CompanySimulator
{
    private const double BaseMonthlyAttrition = 0.0095;
    private const double BackfillRate = 0.85;
    private const double CancelRate = 0.05;
    private const double VoluntaryShare = 0.80;

    private static readonly decimal[] PartTimeFte = [0.5m, 0.6m, 0.8m];
    private static readonly int[] PartTimeFteWeights = [30, 30, 40];

    private readonly SimulationOptions _options;
    private readonly Random _rng;
    private readonly IReadOnlyList<Job> _jobs;
    private readonly Dictionary<(int DepartmentKey, Level Level), Job> _jobLookup;
    private readonly IReadOnlyList<Level> _levels = ReferenceData.LevelMix.Select(x => x.Level).ToList();
    private readonly IReadOnlyList<int> _levelWeights = ReferenceData.LevelMix.Select(x => x.Weight).ToList();

    private readonly List<Employee> _employees = [];
    private readonly List<Requisition> _requisitions = [];
    private readonly List<Application> _applications = [];
    private readonly List<EmployeeEvent> _events = [];
    private readonly List<HeadcountSnapshot> _snapshots = [];
    private int _candidateSequence;

    public CompanySimulator(SimulationOptions options)
    {
        if (options.AsOf <= options.Start)
        {
            throw new ArgumentException("AsOf must be after Start.", nameof(options));
        }

        _options = options;
        _rng = new Random(options.Seed);
        _jobs = ReferenceData.BuildJobs();
        _jobLookup = _jobs.ToDictionary(j => (j.DepartmentKey, j.Level));
    }

    public SimulationResult Run()
    {
        CreateInitialWorkforce();

        for (var month = new DateOnly(_options.Start.Year, _options.Start.Month, 1); month <= _options.AsOf; month = month.AddMonths(1))
        {
            var monthEnd = Min(month.AddMonths(1).AddDays(-1), _options.AsOf);

            ProcessTerminations(month, monthEnd);
            OpenGrowthRequisitions(month, monthEnd);
            if (month.Month == 3)
            {
                ProcessPromotions(month);
            }

            ResolveRequisitions(monthEnd);
            TakeSnapshot(monthEnd);
        }

        TrimToAsOf();

        return new SimulationResult(_options, _jobs, _employees, _requisitions, _applications, _events, _snapshots);
    }

    // ---------------------------------------------------------------- workforce

    private void CreateInitialWorkforce()
    {
        for (var i = 0; i < _options.InitialHeadcount; i++)
        {
            var department = _rng.PickWeighted(ReferenceData.Departments, d => d.HeadcountWeight);
            var location = _rng.PickWeighted(ReferenceData.Locations, l => l.HeadcountWeight);
            var level = _rng.PickWeighted(_levels, _levelWeights);
            var source = _rng.PickWeighted(ReferenceData.Sources, s => s.ApplicantWeight);

            // Tenure: exponential with a 40-month mean, capped at 15 years.
            var tenureDays = (int)Math.Min(15 * 365, _rng.Exponential(40) * 30.4) + 1;
            var hireDate = _options.Start.AddDays(-tenureDays);

            var employee = NewEmployee(department, location, level, hireDate, source.Key);
            employee.LastPromotionOrHire = hireDate.AddDays(_rng.Next(tenureDays));
        }
    }

    private Employee NewEmployee(Department department, Location location, Level level, DateOnly hireDate, int sourceKey)
    {
        var (minAge, maxAge) = ReferenceData.AgeAtHire(level);
        var partTime = _rng.Chance(0.08);
        var key = _employees.Count + 1;

        var employee = new Employee
        {
            Key = key,
            Code = $"E{key:D5}",
            Gender = _rng.Chance(0.02) ? "Undisclosed" : _rng.Chance(department.FemaleShare) ? "Female" : "Male",
            BirthYear = hireDate.Year - _rng.Between(minAge, maxAge),
            HireDate = hireDate,
            HireSourceKey = sourceKey,
            WorkModel = _rng.PickWeighted(department.WorkModels, department.WorkModelWeights),
            EmploymentType = partTime ? "Part-time" : "Full-time",
            Fte = partTime ? _rng.PickWeighted(PartTimeFte, PartTimeFteWeights) : 1.0m,
            DepartmentKey = department.Key,
            LocationKey = location.Key,
            Level = level,
            LastPromotionOrHire = hireDate,
        };

        _employees.Add(employee);
        return employee;
    }

    private void ProcessTerminations(DateOnly month, DateOnly monthEnd)
    {
        var seasonal = month.Month switch
        {
            1 or 2 or 3 => 1.3, // after bonus payouts
            7 or 8 => 1.1,
            12 => 0.6,
            _ => 1.0,
        };

        var candidates = _employees.Where(e => e.TerminationDate is null && e.HireDate < month).ToList();
        foreach (var employee in candidates)
        {
            var department = DepartmentOf(employee.DepartmentKey);
            var location = LocationOf(employee.LocationKey);
            var tenureMonths = MonthsBetween(employee.HireDate, month);

            var tenureFactor = tenureMonths < 12 ? 1.6 : tenureMonths < 36 ? 1.1 : 0.8;
            var levelFactor = employee.Level switch
            {
                Level.Junior => 1.2,
                Level.Lead => 0.8,
                Level.Manager => 0.6,
                _ => 1.0,
            };

            var probability = BaseMonthlyAttrition * department.AttritionFactor * location.AttritionFactor * tenureFactor * levelFactor * seasonal;
            if (!_rng.Chance(probability))
            {
                continue;
            }

            var date = _rng.DayInMonth(month, monthEnd);
            var voluntary = _rng.Chance(VoluntaryShare);
            employee.TerminationDate = date;
            employee.TerminationType = voluntary ? "Voluntary" : "Involuntary";
            employee.TerminationReason = voluntary
                ? _rng.PickWeighted(ReferenceData.VoluntaryReasons, ReferenceData.VoluntaryReasonWeights)
                : _rng.PickWeighted(ReferenceData.InvoluntaryReasons, ReferenceData.InvoluntaryReasonWeights);

            _events.Add(new EmployeeEvent(
                date, employee.Key, "Termination", JobOf(employee).Key, employee.DepartmentKey, employee.LocationKey,
                JobOf(employee).Key, null, $"{employee.TerminationType}: {employee.TerminationReason}"));

            if (_rng.Chance(BackfillRate))
            {
                OpenRequisition(department, location, employee.Level, RequisitionReason.Backfill, date);
            }
        }
    }

    private void ProcessPromotions(DateOnly month)
    {
        var eligible = _employees
            .Where(e => e.IsActiveOn(month) && e.Level != Level.Manager && MonthsBetween(e.LastPromotionOrHire, month) >= 18)
            .ToList();

        foreach (var employee in eligible)
        {
            var probability = employee.Level switch
            {
                Level.Junior => 0.25,
                Level.Medior => 0.15,
                _ => 0.08,
            };

            if (!_rng.Chance(probability))
            {
                continue;
            }

            var from = JobOf(employee);
            employee.Level = employee.Level + 1;
            employee.LastPromotionOrHire = month;
            _events.Add(new EmployeeEvent(
                month, employee.Key, "Promotion", JobOf(employee).Key, employee.DepartmentKey, employee.LocationKey,
                from.Key, JobOf(employee).Key, $"{from.Level} -> {employee.Level}"));
        }
    }

    private void TakeSnapshot(DateOnly monthEnd)
    {
        foreach (var employee in _employees.Where(e => e.IsActiveOn(monthEnd)))
        {
            _snapshots.Add(new HeadcountSnapshot(
                monthEnd,
                employee.Key,
                JobOf(employee).Key,
                employee.DepartmentKey,
                employee.LocationKey,
                employee.Fte,
                MonthsBetween(employee.HireDate, monthEnd),
                monthEnd.Year - employee.BirthYear));
        }
    }

    // ---------------------------------------------------------------- recruiting

    private void OpenGrowthRequisitions(DateOnly month, DateOnly monthEnd)
    {
        // Keep "active + open requisitions" on track with the growth plan, 3 months ahead.
        var yearsAhead = (monthEnd.AddMonths(3).DayNumber - _options.Start.DayNumber) / 365.25;
        var target = _options.InitialHeadcount * (1 + (_options.AnnualGrowth * yearsAhead));
        var active = _employees.Count(e => e.IsActiveOn(monthEnd));
        var pending = _requisitions.Count(r => !r.IsResolved);
        var toOpen = (int)Math.Round(target - active - pending);

        for (var i = 0; i < toOpen; i++)
        {
            var department = _rng.PickWeighted(ReferenceData.Departments, d => d.HeadcountWeight);
            var location = _rng.PickWeighted(ReferenceData.Locations, l => l.HeadcountWeight);
            var level = _rng.PickWeighted(_levels, _levelWeights);
            OpenRequisition(department, location, level, RequisitionReason.Growth, _rng.DayInMonth(month, monthEnd));
        }
    }

    private void OpenRequisition(Department department, Location location, Level level, RequisitionReason reason, DateOnly opened)
    {
        var key = _requisitions.Count + 1;
        var requisition = new Requisition
        {
            Key = key,
            Code = $"REQ-{key:D5}",
            JobKey = _jobLookup[(department.Key, level)].Key,
            DepartmentKey = department.Key,
            LocationKey = location.Key,
            Level = level,
            Reason = reason,
            OpenedDate = opened,
        };
        _requisitions.Add(requisition);

        if (_rng.Chance(CancelRate))
        {
            requisition.PlannedCancelDate = opened.AddDays(_rng.Between(20, 75));
            GenerateApplications(requisition, location, filled: false);
        }
        else
        {
            GenerateApplications(requisition, location, filled: true);
        }
    }

    /// <summary>Creates the whole candidate funnel up front; <see cref="TrimToAsOf"/> later cuts off the future part.</summary>
    private void GenerateApplications(Requisition requisition, Location location, bool filled)
    {
        var level = requisition.Level;
        DateOnly cutoff;

        if (filled)
        {
            // The hire: quality-weighted source (referrals and agencies convert better).
            var source = _rng.PickWeighted(ReferenceData.Sources, s => SourceWeight(s, level) * s.ScreenPassRate * s.InterviewPassRate);
            var screenGap = _rng.Between(2, 7);
            var interviewGap = _rng.Between(5, 14);
            var offerGap = _rng.Between(3, 10);
            var acceptGap = _rng.Between(1, 5);

            var targetDays = ReferenceData.BaseDaysToFill(level) * location.TimeToFillFactor * _rng.Noise(0.6, 1.6);
            var appliedOffset = Math.Max(1, (int)targetDays - (screenGap + interviewGap + offerGap + acceptGap));

            var applied = requisition.OpenedDate.AddDays(appliedOffset);
            var screen = applied.AddDays(screenGap);
            var interview = screen.AddDays(interviewGap);
            var offer = interview.AddDays(offerGap);
            var accepted = offer.AddDays(acceptGap);
            var notice = level >= Level.Senior ? _rng.Between(30, 60) : _rng.Between(14, 35);
            var start = accepted.AddDays(notice);

            _applications.Add(new Application
            {
                Key = _applications.Count + 1,
                RequisitionKey = requisition.Key,
                CandidateCode = NextCandidateCode(),
                SourceKey = source.Key,
                AppliedDate = applied,
                ScreenDate = screen,
                InterviewDate = interview,
                OfferDate = offer,
                OfferAcceptedDate = accepted,
                StartDate = start,
                Outcome = "Hired",
                FurthestStage = Stage.Hired,
            });

            requisition.PlannedOfferAcceptedDate = accepted;
            requisition.PlannedStartDate = start;
            requisition.PlannedHireSourceKey = source.Key;
            requisition.HiredApplicationKey = _applications.Count;
            cutoff = accepted;
        }
        else
        {
            cutoff = requisition.PlannedCancelDate!.Value;
        }

        var applicants = (int)(ReferenceData.BaseApplicants(level) * _rng.Noise(0.5, 1.5));
        var window = Math.Max(2, cutoff.DayNumber - requisition.OpenedDate.DayNumber);

        for (var i = 0; i < applicants; i++)
        {
            var source = _rng.PickWeighted(ReferenceData.Sources, s => SourceWeight(s, level));
            var application = new Application
            {
                Key = _applications.Count + 1,
                RequisitionKey = requisition.Key,
                CandidateCode = NextCandidateCode(),
                SourceKey = source.Key,
                AppliedDate = requisition.OpenedDate.AddDays(_rng.Next(window)),
                Outcome = "Rejected",
                FurthestStage = Stage.Applied,
            };
            _applications.Add(application);
            RunFunnel(application, source, cutoff, offersPossible: filled);
        }
    }

    /// <summary>Moves a non-hired candidate through the stages until rejection, withdrawal or the requisition closes.</summary>
    private void RunFunnel(Application application, Source source, DateOnly cutoff, bool offersPossible)
    {
        if (!_rng.Chance(source.ScreenPassRate))
        {
            return; // rejected on CV
        }

        var screen = application.AppliedDate.AddDays(_rng.Between(2, 8));
        if (screen >= cutoff)
        {
            return; // position closed before screening
        }

        application.ScreenDate = screen;
        application.FurthestStage = Stage.Screened;

        if (_rng.Chance(0.06))
        {
            application.Outcome = "Withdrawn";
            return;
        }

        if (!_rng.Chance(source.InterviewPassRate))
        {
            return;
        }

        var interview = screen.AddDays(_rng.Between(4, 14));
        if (interview >= cutoff)
        {
            return;
        }

        application.InterviewDate = interview;
        application.FurthestStage = Stage.Interviewed;

        if (_rng.Chance(0.08))
        {
            application.Outcome = "Withdrawn";
            return;
        }

        // A finalist sometimes gets an offer first - and declines it.
        if (offersPossible && _rng.Chance(0.12))
        {
            var offer = interview.AddDays(_rng.Between(3, 10));
            if (offer < cutoff)
            {
                application.OfferDate = offer;
                application.FurthestStage = Stage.Offered;
                application.Outcome = "OfferDeclined";
            }
        }
    }

    private void ResolveRequisitions(DateOnly monthEnd)
    {
        foreach (var requisition in _requisitions.Where(r => !r.IsResolved).ToList())
        {
            if (requisition.PlannedCancelDate is { } cancelDate && cancelDate <= monthEnd)
            {
                requisition.Status = RequisitionStatus.Cancelled;
                requisition.CancelledDate = cancelDate;
            }
            else if (requisition.PlannedStartDate is { } startDate && startDate <= monthEnd)
            {
                var sourceKey = requisition.PlannedHireSourceKey!.Value;
                var employee = NewEmployee(
                    DepartmentOf(requisition.DepartmentKey), LocationOf(requisition.LocationKey), requisition.Level, startDate, sourceKey);

                requisition.Status = RequisitionStatus.Filled;
                requisition.FilledDate = requisition.PlannedOfferAcceptedDate;
                requisition.StartDate = startDate;
                requisition.HiredEmployeeKey = employee.Key;
                requisition.HireSourceKey = sourceKey;
                requisition.HireCostEur = SourceOf(sourceKey).CostPerHireEur;

                _events.Add(new EmployeeEvent(
                    startDate, employee.Key, "Hire", requisition.JobKey, requisition.DepartmentKey, requisition.LocationKey,
                    null, requisition.JobKey, requisition.Reason.ToString()));
            }
        }
    }

    /// <summary>Removes everything that would happen after the as-of date, so the data looks like a real extract.</summary>
    private void TrimToAsOf()
    {
        var asOf = _options.AsOf;

        _applications.RemoveAll(a => a.AppliedDate > asOf);

        foreach (var application in _applications)
        {
            var wasHire = application.Outcome == "Hired";
            var hadFuture = false;

            if (application.ScreenDate > asOf) { application.ScreenDate = null; hadFuture = true; }
            if (application.InterviewDate > asOf) { application.InterviewDate = null; hadFuture = true; }
            if (application.OfferDate > asOf) { application.OfferDate = null; hadFuture = true; }
            if (application.OfferAcceptedDate > asOf) { application.OfferAcceptedDate = null; hadFuture = true; }
            if (application.StartDate > asOf) { application.StartDate = null; hadFuture = true; }

            if (!hadFuture)
            {
                continue;
            }

            application.Outcome = wasHire && application.OfferAcceptedDate is not null ? "OfferAccepted" : "InProgress";
            application.FurthestStage =
                application.OfferAcceptedDate is not null || application.OfferDate is not null ? Stage.Offered
                : application.InterviewDate is not null ? Stage.Interviewed
                : application.ScreenDate is not null ? Stage.Screened
                : Stage.Applied;
        }

        foreach (var requisition in _requisitions.Where(r => r.Status == RequisitionStatus.Open))
        {
            requisition.HiredApplicationKey = null; // not known yet as of the extract date

            if (requisition.PlannedOfferAcceptedDate is { } accepted && accepted <= asOf)
            {
                requisition.Status = RequisitionStatus.PendingStart;
                requisition.FilledDate = accepted;
                requisition.HiredApplicationKey = _applications.FirstOrDefault(a => a.RequisitionKey == requisition.Key && a.Outcome == "OfferAccepted")?.Key;
                requisition.HireSourceKey = requisition.PlannedHireSourceKey;
                requisition.HireCostEur = SourceOf(requisition.PlannedHireSourceKey!.Value).CostPerHireEur;
            }
        }
    }

    // ---------------------------------------------------------------- helpers

    private static double SourceWeight(Source source, Level level) =>
        source.Name == "University" && level != Level.Junior ? 0 : source.ApplicantWeight;

    private string NextCandidateCode() => $"C{++_candidateSequence:D6}";

    private Job JobOf(Employee employee) => _jobLookup[(employee.DepartmentKey, employee.Level)];

    private static Department DepartmentOf(int key) => ReferenceData.Departments[key - 1];

    private static Location LocationOf(int key) => ReferenceData.Locations[key - 1];

    private static Source SourceOf(int key) => ReferenceData.Sources[key - 1];

    private static DateOnly Min(DateOnly a, DateOnly b) => a < b ? a : b;

    internal static int MonthsBetween(DateOnly from, DateOnly to)
    {
        var months = ((to.Year - from.Year) * 12) + to.Month - from.Month - (to.Day < from.Day ? 1 : 0);
        return Math.Max(0, months);
    }
}
