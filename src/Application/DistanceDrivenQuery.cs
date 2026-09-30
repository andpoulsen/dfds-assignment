using Dfds.TruckPlans.Domain;

namespace Dfds.TruckPlans.Application;

/// <summary>
/// Answers questions like "How many kilometres did drivers over the age of 50 drive in Germany in February 2024?".
/// </summary>
public sealed class DistanceDrivenQuery
{
    private readonly ICountryResolver _countryResolver;

    public DistanceDrivenQuery(ICountryResolver countryResolver)
    {
        ArgumentNullException.ThrowIfNull(countryResolver);
        _countryResolver = countryResolver;
    }

    /// <summary>
    /// Kilometres driven in the given country during [periodStart, periodEnd) by the drivers that
    /// <paramref name="includeDriver"/> accepts. The filter is given the driver and the date (UTC) of the
    /// plan's first reading, so age-based filters (see <see cref="DriverFilters"/>) use a well-defined date.
    /// Each stretch between two readings is split at its midpoint: each half counts only if the reading
    /// at that end is in the country and inside the period. See docs/ASSUMPTIONS.md.
    /// </summary>
    public double KilometresDriven(
        IEnumerable<TruckPlan> plans,
        string countryCode,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        Func<Driver, DateOnly, bool> includeDriver)
    {
        ArgumentNullException.ThrowIfNull(plans);
        ArgumentException.ThrowIfNullOrWhiteSpace(countryCode);
        ArgumentNullException.ThrowIfNull(includeDriver);
        if (periodEnd <= periodStart)
            throw new ArgumentException("Period end must be after period start.", nameof(periodEnd));

        var total = 0.0;
        foreach (var plan in plans)
        {
            var readings = plan.Readings;
            if (readings.Count < 2)
                continue;

            var firstReadingDate = DateOnly.FromDateTime(readings[0].Timestamp.UtcDateTime);
            if (!includeDriver(plan.Driver, firstReadingDate))
                continue;

            // A reading with no country (at sea, on a ferry, on a simplified coastline) never counts.
            var countries = readings.Select(r => _countryResolver.GetCountryCode(r.Position)).ToArray();
            bool Counts(int i) =>
                string.Equals(countries[i], countryCode, StringComparison.OrdinalIgnoreCase)
                && readings[i].Timestamp >= periodStart
                && readings[i].Timestamp < periodEnd;

            for (var i = 1; i < readings.Count; i++)
            {
                var halfStretchKm = readings[i - 1].Position.DistanceToKm(readings[i].Position) / 2;
                if (Counts(i - 1)) total += halfStretchKm;
                if (Counts(i)) total += halfStretchKm;
            }
        }

        return total;
    }
}
