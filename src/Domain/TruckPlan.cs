namespace Dfds.TruckPlans.Domain;

/// <summary>
/// A single driver driving a single truck for a continuous period.
/// Starts with no readings; GPS readings are added as they come in from the truck.
/// </summary>
public sealed class TruckPlan
{
    private readonly List<PositionReading> _readings = [];

    public TruckPlan(TruckPlanId id, Driver driver, Truck truck)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(truck);

        Id = id;
        Driver = driver;
        Truck = truck;
    }

    public TruckPlanId Id { get; }
    public Driver Driver { get; }
    public Truck Truck { get; }

    /// <summary>The plan's GPS readings in chronological order.</summary>
    public IReadOnlyList<PositionReading> Readings => _readings.AsReadOnly();

    /// <summary>
    /// Adds a reading from this plan's truck. Readings may arrive out of order;
    /// they are kept sorted by timestamp.
    /// </summary>
    public void AddReading(PositionReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        if (reading.TruckId != Truck.Id)
            throw new ArgumentException("Reading is from a different truck than the plan's.", nameof(reading));

        var index = _readings.FindLastIndex(r => r.Timestamp <= reading.Timestamp) + 1;
        _readings.Insert(index, reading);
    }

    /// <summary>
    /// Approximate distance driven in kilometres: the sum of straight-line distances
    /// between consecutive readings, in time order.
    /// </summary>
    public double DistanceDrivenKm()
    {
        var total = 0.0;
        for (var i = 1; i < _readings.Count; i++)
            total += _readings[i - 1].Position.DistanceToKm(_readings[i].Position);
        return total;
    }
}
