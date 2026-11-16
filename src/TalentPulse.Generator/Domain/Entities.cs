namespace TalentPulse.Generator.Domain;

public sealed class Employee
{
    public int Key { get; init; }

    public required string Code { get; init; }

    public required string Gender { get; init; }

    public int BirthYear { get; init; }

    public DateOnly HireDate { get; init; }

    public DateOnly? TerminationDate { get; set; }

    public string? TerminationType { get; set; }

    public string? TerminationReason { get; set; }

    public int HireSourceKey { get; init; }

    public required string WorkModel { get; init; }

    public required string EmploymentType { get; init; }

    public decimal Fte { get; init; }

    public int DepartmentKey { get; init; }

    public int LocationKey { get; init; }

    public Level Level { get; set; }

    public DateOnly LastPromotionOrHire { get; set; }

    /// <summary>Employed on that day (hired on or before it, not yet left).</summary>
    public bool IsActiveOn(DateOnly day) => HireDate <= day && (TerminationDate is null || TerminationDate > day);
}

public enum RequisitionReason
{
    Backfill,
    Growth,
}

public enum RequisitionStatus
{
    Open,

    /// <summary>Offer accepted, start date is after the as-of date.</summary>
    PendingStart,
    Filled,
    Cancelled,
}

public sealed class Requisition
{
    public int Key { get; init; }

    public required string Code { get; init; }

    public int JobKey { get; init; }

    public int DepartmentKey { get; init; }

    public int LocationKey { get; init; }

    public Level Level { get; init; }

    public RequisitionReason Reason { get; init; }

    public DateOnly OpenedDate { get; init; }

    /// <summary>Planned start date of the hired candidate; null when the requisition will be cancelled.</summary>
    public DateOnly? PlannedStartDate { get; set; }

    /// <summary>Planned offer-accepted date of the hired candidate (time-to-fill end).</summary>
    public DateOnly? PlannedOfferAcceptedDate { get; set; }

    /// <summary>Source and cost of the planned hire, applied when the requisition is filled.</summary>
    public int? PlannedHireSourceKey { get; set; }

    /// <summary>Planned cancellation date; null when the requisition will be filled.</summary>
    public DateOnly? PlannedCancelDate { get; set; }

    public RequisitionStatus Status { get; set; } = RequisitionStatus.Open;

    /// <summary>Offer accepted (time-to-fill end).</summary>
    public DateOnly? FilledDate { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? CancelledDate { get; set; }

    public int? HiredEmployeeKey { get; set; }

    public int? HiredApplicationKey { get; set; }

    public int? HireSourceKey { get; set; }

    public decimal? HireCostEur { get; set; }

    public bool IsResolved => Status is RequisitionStatus.Filled or RequisitionStatus.Cancelled;
}

public enum Stage
{
    Applied = 1,
    Screened = 2,
    Interviewed = 3,
    Offered = 4,
    Hired = 5,
}

public sealed class Application
{
    public int Key { get; init; }

    public int RequisitionKey { get; init; }

    public required string CandidateCode { get; init; }

    public int SourceKey { get; init; }

    public DateOnly AppliedDate { get; set; }

    public DateOnly? ScreenDate { get; set; }

    public DateOnly? InterviewDate { get; set; }

    public DateOnly? OfferDate { get; set; }

    public DateOnly? OfferAcceptedDate { get; set; }

    public DateOnly? StartDate { get; set; }

    /// <summary>Hired, OfferAccepted (start pending), Rejected, Withdrawn, OfferDeclined, InProgress.</summary>
    public required string Outcome { get; set; }

    public Stage FurthestStage { get; set; }
}

/// <summary>Hire / termination / promotion, with the org keys at the time of the event (so leavers can be sliced by department).</summary>
public sealed record EmployeeEvent(
    DateOnly Date, int EmployeeKey, string EventType, int JobKey, int DepartmentKey, int LocationKey, int? FromJobKey, int? ToJobKey, string? Detail);

public sealed record HeadcountSnapshot(DateOnly MonthEnd, int EmployeeKey, int JobKey, int DepartmentKey, int LocationKey, decimal Fte, int TenureMonths, int Age);
