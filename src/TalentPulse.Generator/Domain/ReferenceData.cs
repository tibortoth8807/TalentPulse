namespace TalentPulse.Generator.Domain;

public enum Level
{
    Junior = 1,
    Medior = 2,
    Senior = 3,
    Lead = 4,
    Manager = 5,
}

public sealed record Location(int Key, string City, string Country, string Region, double HeadcountWeight, double AttritionFactor, double TimeToFillFactor);

/// <summary>Org unit = department inside a function, e.g. Engineering / Software Development.</summary>
public sealed record Department(
    int Key, string Function, string Name, string Role, double HeadcountWeight, double AttritionFactor, double FemaleShare, string[] WorkModels, int[] WorkModelWeights);

public sealed record Job(int Key, int DepartmentKey, Level Level, string Title, string JobFamily, int Grade);

/// <summary>Recruiting source with its own funnel quality and cost.</summary>
public sealed record Source(
    int Key, string Name, string Channel, double ApplicantWeight, double ScreenPassRate, double InterviewPassRate, decimal CostPerHireEur);

/// <summary>
/// Fictional company "Northwind Dynamics" (invented). All weights are tuned to give realistic-looking
/// but clearly synthetic HR data: ~13% yearly attrition, higher in Support/Sales and in the first year.
/// </summary>
public static class ReferenceData
{
    public static readonly IReadOnlyList<Location> Locations =
    [
        new(1, "Budapest", "Hungary", "EMEA", 0.30, 1.00, 1.00),
        new(2, "Berlin", "Germany", "EMEA", 0.18, 0.80, 1.30),
        new(3, "Kraków", "Poland", "EMEA", 0.15, 1.10, 1.00),
        new(4, "Lisbon", "Portugal", "EMEA", 0.10, 1.00, 1.10),
        new(5, "Austin", "United States", "Americas", 0.12, 1.20, 0.90),
        new(6, "Bengaluru", "India", "APAC", 0.15, 1.50, 0.80),
    ];

    private static readonly string[] TechWorkModels = ["Hybrid", "Remote", "Office"];
    private static readonly int[] TechWeights = [60, 30, 10];
    private static readonly string[] OfficeWorkModels = ["Hybrid", "Office", "Remote"];
    private static readonly int[] OfficeWeights = [60, 35, 5];

    public static readonly IReadOnlyList<Department> Departments =
    [
        new(1, "Engineering", "Software Development", "Software Engineer", 0.20, 1.00, 0.24, TechWorkModels, TechWeights),
        new(2, "Engineering", "QA & Test Automation", "QA Engineer", 0.07, 1.00, 0.35, TechWorkModels, TechWeights),
        new(3, "Engineering", "DevOps & Cloud", "DevOps Engineer", 0.05, 1.10, 0.18, TechWorkModels, TechWeights),
        new(4, "Product", "Product Management", "Product Manager", 0.04, 0.90, 0.45, TechWorkModels, TechWeights),
        new(5, "Product", "UX Design", "UX Designer", 0.03, 0.90, 0.58, TechWorkModels, TechWeights),
        new(6, "Sales", "Enterprise Sales", "Account Executive", 0.07, 1.20, 0.38, OfficeWorkModels, OfficeWeights),
        new(7, "Sales", "Inside Sales", "Sales Representative", 0.08, 1.40, 0.46, OfficeWorkModels, OfficeWeights),
        new(8, "Customer Success", "Customer Support", "Support Specialist", 0.15, 1.60, 0.55, OfficeWorkModels, OfficeWeights),
        new(9, "Customer Success", "Account Management", "Account Manager", 0.06, 1.10, 0.52, OfficeWorkModels, OfficeWeights),
        new(10, "Finance", "Accounting", "Accountant", 0.04, 0.70, 0.66, OfficeWorkModels, OfficeWeights),
        new(11, "Finance", "FP&A", "Financial Analyst", 0.03, 0.80, 0.48, OfficeWorkModels, OfficeWeights),
        new(12, "HR", "Talent Acquisition", "Recruiter", 0.03, 1.10, 0.72, OfficeWorkModels, OfficeWeights),
        new(13, "HR", "HR Business Partners", "HR Business Partner", 0.02, 0.80, 0.76, OfficeWorkModels, OfficeWeights),
        new(14, "Operations", "IT Operations", "IT Engineer", 0.04, 0.90, 0.20, OfficeWorkModels, OfficeWeights),
        new(15, "Operations", "Facilities", "Facility Coordinator", 0.01, 0.90, 0.40, ["Office"], [100]),
    ];

    public static readonly IReadOnlyList<Source> Sources =
    [
        new(1, "LinkedIn", "Paid", 35, 0.30, 0.32, 1_200m),
        new(2, "Job board", "Paid", 25, 0.18, 0.25, 600m),
        new(3, "Career site", "Organic", 20, 0.25, 0.30, 150m),
        new(4, "Employee referral", "Network", 8, 0.55, 0.45, 1_000m),
        new(5, "Agency", "Paid", 7, 0.45, 0.38, 6_000m),
        new(6, "University", "Organic", 5, 0.35, 0.28, 300m),
    ];

    /// <summary>Share of each level in the workforce and in new requisitions.</summary>
    public static readonly (Level Level, int Weight)[] LevelMix =
    [
        (Level.Junior, 25), (Level.Medior, 35), (Level.Senior, 25), (Level.Lead, 9), (Level.Manager, 6),
    ];

    public static readonly string[] VoluntaryReasons =
        ["Better compensation", "Career growth", "Work-life balance", "Relocation", "Manager relationship", "Personal reasons"];

    public static readonly int[] VoluntaryReasonWeights = [30, 25, 15, 10, 12, 8];

    public static readonly string[] InvoluntaryReasons = ["Performance", "Restructuring", "End of contract"];

    public static readonly int[] InvoluntaryReasonWeights = [50, 30, 20];

    public static IReadOnlyList<Job> BuildJobs()
    {
        var jobs = new List<Job>();
        var key = 0;
        foreach (var dept in Departments)
        {
            foreach (var level in Enum.GetValues<Level>())
            {
                var title = level switch
                {
                    Level.Junior => $"Junior {dept.Role}",
                    Level.Medior => dept.Role,
                    Level.Senior => $"Senior {dept.Role}",
                    Level.Lead => $"Lead {dept.Role}",
                    _ => $"{dept.Name} Manager",
                };
                jobs.Add(new Job(++key, dept.Key, level, title, dept.Role, (int)level));
            }
        }

        return jobs;
    }

    /// <summary>Typical age at hire per level (min, max).</summary>
    public static (int Min, int Max) AgeAtHire(Level level) => level switch
    {
        Level.Junior => (21, 28),
        Level.Medior => (25, 35),
        Level.Senior => (29, 45),
        Level.Lead => (32, 48),
        _ => (34, 55),
    };

    /// <summary>Base time-to-start in days per level, before location factor and noise.</summary>
    public static int BaseDaysToFill(Level level) => level switch
    {
        Level.Junior => 30,
        Level.Medior => 40,
        Level.Senior => 55,
        Level.Lead => 65,
        _ => 80,
    };

    /// <summary>Applicants per requisition before noise.</summary>
    public static int BaseApplicants(Level level) => level switch
    {
        Level.Junior => 55,
        Level.Medior => 38,
        Level.Senior => 28,
        Level.Lead => 18,
        _ => 22,
    };
}
