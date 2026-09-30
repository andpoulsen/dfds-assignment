namespace Dfds.TruckPlans.Domain;

/// <summary>A single driver driving a single truck for a continuous period.</summary>
public sealed class TruckPlan
{
    public TruckPlan(TruckPlanId id, Driver driver, Truck truck, TimeWindow period)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(truck);

        Id = id;
        Driver = driver;
        Truck = truck;
        Period = period;
    }

    public TruckPlanId Id { get; }
    public Driver Driver { get; }
    public Truck Truck { get; }
    public TimeWindow Period { get; }

    /// <summary>Whether the reading was produced by this plan's truck during the plan's period.</summary>
    public bool Covers(PositionReading reading) =>
        reading.TruckId == Truck.Id && Period.Contains(reading.Timestamp);

    /// <summary>The readings that belong to this plan, in chronological order.</summary>
    public IReadOnlyList<PositionReading> SelectReadings(IEnumerable<PositionReading> readings) =>
        readings.Where(Covers).OrderBy(r => r.Timestamp).ToList();
}
