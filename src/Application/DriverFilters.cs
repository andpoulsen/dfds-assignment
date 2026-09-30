using Dfds.TruckPlans.Domain;

namespace Dfds.TruckPlans.Application;

/// <summary>Ready-made driver filters for <see cref="DistanceDrivenQuery"/>.</summary>
public static class DriverFilters
{
    /// <summary>Drivers strictly older than <paramref name="age"/> on the given date.</summary>
    public static Func<Driver, DateOnly, bool> OlderThan(int age) =>
        (driver, date) => driver.AgeOn(date) > age;
}
