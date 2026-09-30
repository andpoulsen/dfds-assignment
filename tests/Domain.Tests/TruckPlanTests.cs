namespace Dfds.TruckPlans.Domain.Tests;

public class TruckPlanTests
{
    private static readonly Driver Driver = new(DriverId.New(), "Jane Doe", new DateOnly(1974, 2, 15));
    private static readonly Truck Truck = new(TruckId.New(), "AB 12 345");
    private static readonly Truck OtherTruck = new(TruckId.New(), "CD 67 890");
    private static readonly Coordinate Hamburg = new(53.5511, 9.9937);

    private static DateTimeOffset At(int hour, int minute = 0) =>
        new(2024, 2, 12, hour, minute, 0, TimeSpan.Zero);

    private static TruckPlan NewPlan() => new(TruckPlanId.New(), Driver, Truck);

    private static PositionReading Reading(Truck truck, DateTimeOffset timestamp) =>
        new(truck.Id, timestamp, Hamburg);

    [Fact]
    public void Constructor_rejects_null_driver()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new TruckPlan(TruckPlanId.New(), null!, Truck));
        Assert.Equal("driver", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_truck()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new TruckPlan(TruckPlanId.New(), Driver, null!));
        Assert.Equal("truck", ex.ParamName);
    }

    [Fact]
    public void New_plan_has_no_readings()
    {
        Assert.Empty(NewPlan().Readings);
    }

    [Fact]
    public void AddReading_adds_reading_from_own_truck()
    {
        var plan = NewPlan();
        var reading = Reading(Truck, At(10));

        plan.AddReading(reading);

        Assert.Equal([reading], plan.Readings);
    }

    [Fact]
    public void AddReading_keeps_readings_in_chronological_order_when_they_arrive_out_of_order()
    {
        var plan = NewPlan();

        plan.AddReading(Reading(Truck, At(10, 5)));
        plan.AddReading(Reading(Truck, At(8)));
        plan.AddReading(Reading(Truck, At(9, 30)));
        plan.AddReading(Reading(Truck, At(11)));

        Assert.Equal([At(8), At(9, 30), At(10, 5), At(11)], plan.Readings.Select(r => r.Timestamp));
    }

    [Fact]
    public void AddReading_keeps_arrival_order_for_readings_with_the_same_timestamp()
    {
        var plan = NewPlan();
        var first = new PositionReading(Truck.Id, At(10), new Coordinate(53.5511, 9.9937));
        var second = new PositionReading(Truck.Id, At(10), new Coordinate(53.5600, 10.0000));

        plan.AddReading(Reading(Truck, At(11)));
        plan.AddReading(first);
        plan.AddReading(second);

        Assert.Equal([first, second], plan.Readings.Take(2));
    }

    [Fact]
    public void Readings_cannot_be_modified_directly()
    {
        var plan = NewPlan();
        var readings = (ICollection<PositionReading>)plan.Readings;

        Assert.Throws<NotSupportedException>(() => readings.Add(Reading(Truck, At(10))));
        Assert.Empty(plan.Readings);
    }

    [Fact]
    public void AddReading_rejects_reading_from_another_truck()
    {
        var plan = NewPlan();

        var ex = Assert.Throws<ArgumentException>(() => plan.AddReading(Reading(OtherTruck, At(10))));
        Assert.Equal("reading", ex.ParamName);
        Assert.Empty(plan.Readings);
    }

    [Fact]
    public void AddReading_rejects_null()
    {
        Assert.Throws<ArgumentNullException>(() => NewPlan().AddReading(null!));
    }

    [Fact]
    public void DistanceDrivenKm_is_zero_without_readings()
    {
        Assert.Equal(0, NewPlan().DistanceDrivenKm());
    }

    [Fact]
    public void DistanceDrivenKm_is_zero_with_a_single_reading()
    {
        var plan = NewPlan();
        plan.AddReading(Reading(Truck, At(8)));

        Assert.Equal(0, plan.DistanceDrivenKm());
    }

    [Fact]
    public void DistanceDrivenKm_sums_the_distances_between_consecutive_readings()
    {
        var plan = NewPlan();
        plan.AddReading(new PositionReading(Truck.Id, At(8), new Coordinate(0, 0)));
        plan.AddReading(new PositionReading(Truck.Id, At(8, 5), new Coordinate(1, 0)));
        plan.AddReading(new PositionReading(Truck.Id, At(8, 10), new Coordinate(2, 0)));

        Assert.Equal(2 * 111.1949, plan.DistanceDrivenKm(), precision: 3);
    }

    [Fact]
    public void DistanceDrivenKm_follows_time_order_not_arrival_order()
    {
        var plan = NewPlan();
        plan.AddReading(new PositionReading(Truck.Id, At(8), new Coordinate(0, 0)));
        plan.AddReading(new PositionReading(Truck.Id, At(8, 10), new Coordinate(2, 0)));
        plan.AddReading(new PositionReading(Truck.Id, At(8, 5), new Coordinate(1, 0))); // arrives late

        // In arrival order the route would be 0 → 2 → 1 (3 degrees); in time order it is 0 → 1 → 2 (2 degrees).
        Assert.Equal(2 * 111.1949, plan.DistanceDrivenKm(), precision: 3);
    }

    [Fact]
    public void DistanceDrivenKm_adds_nothing_for_repeated_readings_at_the_same_position()
    {
        var plan = NewPlan();
        plan.AddReading(new PositionReading(Truck.Id, At(8), new Coordinate(0, 0)));
        plan.AddReading(new PositionReading(Truck.Id, At(8, 5), new Coordinate(0, 0)));  // parked
        plan.AddReading(new PositionReading(Truck.Id, At(8, 10), new Coordinate(1, 0)));

        Assert.Equal(111.1949, plan.DistanceDrivenKm(), precision: 3);
    }
}
