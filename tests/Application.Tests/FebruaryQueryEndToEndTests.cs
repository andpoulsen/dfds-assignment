using Dfds.TruckPlans.Domain;
using Dfds.TruckPlans.Infrastructure;

namespace Dfds.TruckPlans.Application.Tests;

/// <summary>
/// The question from the brief, answered with the real country boundaries:
/// "How many kilometres did drivers over the age of 50 drive in Germany in February 2024?"
/// </summary>
public class FebruaryQueryEndToEndTests
{
    private static readonly Coordinate Hamburg = new(53.5511, 9.9937);
    private static readonly Coordinate Neumuenster = new(54.0717, 9.9900);
    private static readonly Coordinate Flensburg = new(54.7833, 9.4333);
    private static readonly Coordinate Padborg = new(54.8261, 9.3634);

    [Fact]
    public void Kilometres_driven_in_Germany_in_February_2024_by_drivers_over_50()
    {
        var truck = new Truck(TruckId.New(), "AB 12 345");
        var driverOver50 = new Driver(DriverId.New(), "Jane Doe", new DateOnly(1965, 5, 1));
        var driverUnder50 = new Driver(DriverId.New(), "John Doe", new DateOnly(1990, 5, 1));

        // Hamburg → Neumünster → Flensburg → across the border to Padborg (Denmark).
        var plan = new TruckPlan(TruckPlanId.New(), driverOver50, truck);
        plan.AddReading(new PositionReading(truck.Id, new DateTimeOffset(2024, 2, 12, 8, 0, 0, TimeSpan.Zero), Hamburg));
        plan.AddReading(new PositionReading(truck.Id, new DateTimeOffset(2024, 2, 12, 9, 0, 0, TimeSpan.Zero), Neumuenster));
        plan.AddReading(new PositionReading(truck.Id, new DateTimeOffset(2024, 2, 12, 10, 0, 0, TimeSpan.Zero), Flensburg));
        plan.AddReading(new PositionReading(truck.Id, new DateTimeOffset(2024, 2, 12, 10, 10, 0, TimeSpan.Zero), Padborg));

        // Same route, younger driver: not counted.
        var otherPlan = new TruckPlan(TruckPlanId.New(), driverUnder50, truck);
        otherPlan.AddReading(new PositionReading(truck.Id, new DateTimeOffset(2024, 2, 13, 8, 0, 0, TimeSpan.Zero), Hamburg));
        otherPlan.AddReading(new PositionReading(truck.Id, new DateTimeOffset(2024, 2, 13, 9, 0, 0, TimeSpan.Zero), Neumuenster));

        var query = new DistanceDrivenQuery(new NaturalEarthCountryResolver());

        var km = query.KilometresDriven(
            [plan, otherPlan],
            countryCode: "DE",
            periodStart: new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero),
            periodEnd: new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero),
            includeDriver: DriverFilters.OlderThan(50));

        // Both German stretches in full, plus half of the stretch that crosses into Denmark.
        var expected = Hamburg.DistanceToKm(Neumuenster)
                     + Neumuenster.DistanceToKm(Flensburg)
                     + Flensburg.DistanceToKm(Padborg) / 2;
        Assert.Equal(expected, km, precision: 6);
    }
}
