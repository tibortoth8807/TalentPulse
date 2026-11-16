namespace TalentPulse.Generator.Simulation;

internal static class RandomExtensions
{
    public static T PickWeighted<T>(this Random rng, IReadOnlyList<T> items, Func<T, double> weight)
    {
        var total = items.Sum(weight);
        var roll = rng.NextDouble() * total;
        foreach (var item in items)
        {
            roll -= weight(item);
            if (roll <= 0)
            {
                return item;
            }
        }

        return items[^1];
    }

    public static T PickWeighted<T>(this Random rng, IReadOnlyList<T> items, IReadOnlyList<int> weights)
    {
        var total = weights.Sum();
        var roll = rng.Next(total);
        for (var i = 0; i < items.Count; i++)
        {
            roll -= weights[i];
            if (roll < 0)
            {
                return items[i];
            }
        }

        return items[^1];
    }

    public static bool Chance(this Random rng, double probability) => rng.NextDouble() < probability;

    /// <summary>Inclusive range.</summary>
    public static int Between(this Random rng, int min, int max) => rng.Next(min, max + 1);

    /// <summary>Multiplicative noise, e.g. 0.6..1.6.</summary>
    public static double Noise(this Random rng, double min, double max) => min + (rng.NextDouble() * (max - min));

    /// <summary>Exponential distribution with the given mean.</summary>
    public static double Exponential(this Random rng, double mean) => -Math.Log(1 - rng.NextDouble()) * mean;

    public static DateOnly DayInMonth(this Random rng, DateOnly monthStart, DateOnly monthEnd) =>
        monthStart.AddDays(rng.Next(monthEnd.DayNumber - monthStart.DayNumber + 1));
}
