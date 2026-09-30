using Dfds.TruckPlans.Domain;

namespace Dfds.TruckPlans.Application.Tests;

public class DistanceDrivenQueryTests
{
    // Points one degree of longitude apart along the equator: each stretch is 111.1949 km.
    private static readonly Coordinate P0 = new(0, 0);
    private static readonly Coordinate P1 = new(0, 1);
    private static readonly Coordinate P2 = new(0, 2);
    private const double StretchKm = 111.1949;

    private static readonly DateTimeOffset FebruaryStart = new(2024, 2, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FebruaryEnd = new(2024, 3, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly Truck Truck = new(TruckId.New(), "AB 12 345");

    private static DateTimeOffset Feb(int day, int hour, int minute = 0) =>
        new(2024, 2, day, hour, minute, 0, TimeSpan.Zero);

    private static Driver DriverBornOn(int year, int month, int day) =>
        new(DriverId.New(), "Jane Doe", new DateOnly(year, month, day));

    private static readonly Driver DriverAged60 = DriverBornOn(1964, 1, 1);

    private static bool AllDrivers(Driver driver, DateOnly date) => true;

    private static TruckPlan Plan(Driver driver, params (DateTimeOffset Timestamp, Coordinate Position)[] readings)
    {
        var plan = new TruckPlan(TruckPlanId.New(), driver, Truck);
        foreach (var (timestamp, position) in readings)
            plan.AddReading(new PositionReading(Truck.Id, timestamp, position));
        return plan;
    }

    private static double KilometresInGermanyInFebruary(ICountryResolver resolver, params TruckPlan[] plans) =>
        new DistanceDrivenQuery(resolver).KilometresDriven(plans, "DE", FebruaryStart, FebruaryEnd, DriverFilters.OlderThan(50));

    private static FakeCountryResolver AllIn(string? country) =>
        new(new() { [P0] = country, [P1] = country, [P2] = country });

    [Fact]
    public void Counts_every_stretch_driven_in_the_country_during_the_period()
    {
        var plan = Plan(DriverAged60, (Feb(12, 8), P0), (Feb(12, 8, 5), P1), (Feb(12, 8, 10), P2));

        Assert.Equal(2 * StretchKm, KilometresInGermanyInFebruary(AllIn("DE"), plan), precision: 3);
    }

    [Fact]
    public void Sums_over_several_plans()
    {
        var first = Plan(DriverAged60, (Feb(12, 8), P0), (Feb(12, 8, 5), P1));
        var second = Plan(DriverAged60, (Feb(13, 8), P1), (Feb(13, 8, 5), P2));

        Assert.Equal(2 * StretchKm, KilometresInGermanyInFebruary(AllIn("DE"), first, second), precision: 3);
    }

    [Fact]
    public void Ignores_driving_in_other_countries()
    {
        var plan = Plan(DriverAged60, (Feb(12, 8), P0), (Feb(12, 8, 5), P1));

        Assert.Equal(0, KilometresInGermanyInFebruary(AllIn("DK"), plan));
    }

    [Fact]
    public void Matches_country_codes_case_insensitively()
    {
        var plan = Plan(DriverAged60, (Feb(12, 8), P0), (Feb(12, 8, 5), P1));

        var km = new DistanceDrivenQuery(AllIn("DE")).KilometresDriven([plan], "de", FebruaryStart, FebruaryEnd, DriverFilters.OlderThan(50));

        Assert.Equal(StretchKm, km, precision: 3);
    }

    // Age

    [Theory]
    [InlineData(1973, 2, 12, true)]  // turns 51 on the day of the first reading
    [InlineData(1973, 2, 13, false)] // exactly 50 on the day of the first reading
    [InlineData(1974, 2, 12, false)] // turns 50 on the day of the first reading
    [InlineData(1950, 6, 1, true)]
    public void Counts_only_drivers_older_than_the_age_limit_on_the_date_of_the_first_reading(
        int birthYear, int birthMonth, int birthDay, bool counted)
    {
        var plan = Plan(DriverBornOn(birthYear, birthMonth, birthDay), (Feb(12, 8), P0), (Feb(12, 8, 5), P1));

        Assert.Equal(counted ? StretchKm : 0, KilometresInGermanyInFebruary(AllIn("DE"), plan), precision: 3);
    }

    [Fact]
    public void Uses_the_age_at_the_first_reading_for_the_whole_plan()
    {
        // Turns 51 on 13 February, while the plan is under way: the plan starts on 12 February, when the driver is 50.
        var plan = Plan(DriverBornOn(1973, 2, 13), (Feb(12, 23, 55), P0), (Feb(13, 0, 0), P1), (Feb(13, 0, 5), P2));

        Assert.Equal(0, KilometresInGermanyInFebruary(AllIn("DE"), plan));
    }

    [Fact]
    public void Passes_the_driver_and_the_date_of_the_first_reading_to_the_filter()
    {
        var plan = Plan(DriverAged60, (Feb(12, 23, 55), P0), (Feb(13, 0, 5), P1));
        var calls = new List<(Driver Driver, DateOnly Date)>();

        new DistanceDrivenQuery(AllIn("DE")).KilometresDriven([plan], "DE", FebruaryStart, FebruaryEnd,
            (driver, date) => { calls.Add((driver, date)); return true; });

        Assert.Equal([(DriverAged60, new DateOnly(2024, 2, 12))], calls);
    }

    [Fact]
    public void Supports_filters_other_than_age()
    {
        var jane = new Driver(DriverId.New(), "Jane Doe", new DateOnly(1990, 1, 1));
        var john = new Driver(DriverId.New(), "John Doe", new DateOnly(1990, 1, 1));
        var janesPlan = Plan(jane, (Feb(12, 8), P0), (Feb(12, 8, 5), P1));
        var johnsPlan = Plan(john, (Feb(12, 8), P1), (Feb(12, 8, 5), P2));

        var km = new DistanceDrivenQuery(AllIn("DE")).KilometresDriven([janesPlan, johnsPlan], "DE", FebruaryStart, FebruaryEnd,
            (driver, _) => driver.Id == jane.Id);

        Assert.Equal(StretchKm, km, precision: 3);
    }

    [Fact]
    public void Rejects_null_driver_filter()
    {
        var query = new DistanceDrivenQuery(AllIn("DE"));

        Assert.Throws<ArgumentNullException>(() => query.KilometresDriven([], "DE", FebruaryStart, FebruaryEnd, null!));
    }

    // Stretches split at the midpoint

    [Fact]
    public void Counts_half_a_stretch_that_crosses_the_border()
    {
        var resolver = new FakeCountryResolver(new() { [P0] = "DE", [P1] = "DK" });
        var plan = Plan(DriverAged60, (Feb(12, 8), P0), (Feb(12, 8, 5), P1));

        Assert.Equal(StretchKm / 2, KilometresInGermanyInFebruary(resolver, plan), precision: 3);
    }

    [Fact]
    public void Counts_half_a_stretch_that_starts_before_the_period()
    {
        var plan = Plan(DriverAged60,
            (new DateTimeOffset(2024, 1, 31, 23, 58, 0, TimeSpan.Zero), P0),
            (new DateTimeOffset(2024, 2, 1, 0, 3, 0, TimeSpan.Zero), P1));

        Assert.Equal(StretchKm / 2, KilometresInGermanyInFebruary(AllIn("DE"), plan), precision: 3);
    }

    [Fact]
    public void Counts_half_a_stretch_that_ends_after_the_period()
    {
        var plan = Plan(DriverAged60,
            (new DateTimeOffset(2024, 2, 29, 23, 58, 0, TimeSpan.Zero), P0),
            (new DateTimeOffset(2024, 3, 1, 0, 3, 0, TimeSpan.Zero), P1));

        Assert.Equal(StretchKm / 2, KilometresInGermanyInFebruary(AllIn("DE"), plan), precision: 3);
    }

    [Fact]
    public void Treats_the_period_start_as_inclusive_and_the_end_as_exclusive()
    {
        var plan = Plan(DriverAged60, (FebruaryStart, P0), (FebruaryEnd, P1));

        Assert.Equal(StretchKm / 2, KilometresInGermanyInFebruary(AllIn("DE"), plan), precision: 3);
    }

    [Fact]
    public void Interprets_the_period_in_UTC()
    {
        // 00:30 on 1 March in German local time (UTC+1) is 23:30 on 29 February in UTC, so it is inside February.
        var plan = Plan(DriverAged60,
            (new DateTimeOffset(2024, 3, 1, 0, 30, 0, TimeSpan.FromHours(1)), P0),
            (new DateTimeOffset(2024, 3, 1, 0, 35, 0, TimeSpan.FromHours(1)), P1));

        Assert.Equal(StretchKm, KilometresInGermanyInFebruary(AllIn("DE"), plan), precision: 3);
    }

    // Readings with no country

    [Fact]
    public void A_reading_with_no_country_does_not_count()
    {
        // Germany, then a reading at sea or on a simplified coastline, then Germany again:
        // each stretch counts only its German half.
        var resolver = new FakeCountryResolver(new() { [P0] = "DE", [P1] = null, [P2] = "DE" });
        var plan = Plan(DriverAged60, (Feb(12, 8), P0), (Feb(12, 8, 5), P1), (Feb(12, 8, 10), P2));

        Assert.Equal(StretchKm, KilometresInGermanyInFebruary(resolver, plan), precision: 3);
    }

    [Fact]
    public void A_ferry_crossing_does_not_count()
    {
        // Leaves Germany by ferry: only the half of the first stretch at the German end counts.
        var resolver = new FakeCountryResolver(new() { [P0] = "DE", [P1] = null, [P2] = null });
        var plan = Plan(DriverAged60, (Feb(12, 8), P0), (Feb(12, 9), P1), (Feb(12, 10), P2));

        Assert.Equal(StretchKm / 2, KilometresInGermanyInFebruary(resolver, plan), precision: 3);
    }

    // Edge cases and argument checks

    [Fact]
    public void Plans_with_fewer_than_two_readings_contribute_nothing()
    {
        var empty = Plan(DriverAged60);
        var single = Plan(DriverAged60, (Feb(12, 8), P0));

        Assert.Equal(0, KilometresInGermanyInFebruary(AllIn("DE"), empty, single));
    }

    [Fact]
    public void Returns_zero_without_plans()
    {
        Assert.Equal(0, KilometresInGermanyInFebruary(AllIn("DE")));
    }

    [Fact]
    public void Constructor_rejects_null_resolver()
    {
        Assert.Throws<ArgumentNullException>(() => new DistanceDrivenQuery(null!));
    }

    [Fact]
    public void Rejects_period_that_does_not_end_after_it_starts()
    {
        var query = new DistanceDrivenQuery(AllIn("DE"));

        Assert.Throws<ArgumentException>(() => query.KilometresDriven([], "DE", FebruaryEnd, FebruaryStart, AllDrivers));
        Assert.Throws<ArgumentException>(() => query.KilometresDriven([], "DE", FebruaryStart, FebruaryStart, AllDrivers));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Rejects_missing_country_code(string? countryCode)
    {
        var query = new DistanceDrivenQuery(AllIn("DE"));

        Assert.ThrowsAny<ArgumentException>(() => query.KilometresDriven([], countryCode!, FebruaryStart, FebruaryEnd, AllDrivers));
    }

    [Fact]
    public void Rejects_null_plans()
    {
        var query = new DistanceDrivenQuery(AllIn("DE"));

        Assert.Throws<ArgumentNullException>(() => query.KilometresDriven(null!, "DE", FebruaryStart, FebruaryEnd, AllDrivers));
    }
}
