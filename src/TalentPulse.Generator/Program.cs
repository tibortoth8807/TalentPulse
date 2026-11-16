using System.Diagnostics;
using System.Globalization;
using TalentPulse.Generator.Export;
using TalentPulse.Generator.Simulation;

// TalentPulse data generator
//   dotnet run --project src/TalentPulse.Generator -- --out data
//   options: --seed 42 --start 2023-01-01 --asof 2026-10-31 --headcount 1800 --growth 0.06

var defaults = SimulationOptions.Default;
var outFolder = "data";
var options = defaults;

for (var i = 0; i < args.Length - 1; i += 2)
{
    var value = args[i + 1];
    switch (args[i])
    {
        case "--out": outFolder = value; break;
        case "--seed": options = options with { Seed = int.Parse(value, CultureInfo.InvariantCulture) }; break;
        case "--start": options = options with { Start = DateOnly.Parse(value, CultureInfo.InvariantCulture) }; break;
        case "--asof": options = options with { AsOf = DateOnly.Parse(value, CultureInfo.InvariantCulture) }; break;
        case "--headcount": options = options with { InitialHeadcount = int.Parse(value, CultureInfo.InvariantCulture) }; break;
        case "--growth": options = options with { AnnualGrowth = double.Parse(value, CultureInfo.InvariantCulture) }; break;
        default:
            Console.Error.WriteLine($"Unknown option '{args[i]}'.");
            return 1;
    }
}

var stopwatch = Stopwatch.StartNew();
var result = new CompanySimulator(options).Run();
var files = CsvExporter.Export(result, outFolder);
stopwatch.Stop();

var summary = SimulationSummary.From(result);

Console.WriteLine($"TalentPulse - synthetic HR data (seed {options.Seed}, {options.Start:yyyy-MM-dd} .. {options.AsOf:yyyy-MM-dd})");
Console.WriteLine();
foreach (var (file, rows) in files)
{
    Console.WriteLine($"  {file,-28} {rows,8:N0} rows");
}

Console.WriteLine();
Console.WriteLine($"  Headcount at start / as-of:   {summary.HeadcountAtStart:N0} / {summary.HeadcountAsOf:N0}");
Console.WriteLine($"  Attrition, last 12 months:    {summary.Attrition12Months:P1}");
Console.WriteLine($"  Avg time to fill (days):      {summary.AverageDaysToFill:N1}");
Console.WriteLine($"  Offer acceptance rate:        {summary.OfferAcceptanceRate:P1}");
Console.WriteLine();
Console.WriteLine($"Written to '{Path.GetFullPath(outFolder)}' in {stopwatch.Elapsed.TotalSeconds:N1}s.");
return 0;
