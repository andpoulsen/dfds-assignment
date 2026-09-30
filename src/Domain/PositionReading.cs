namespace Dfds.TruckPlans.Domain;

/// <summary>
/// A single GPS report from a truck's device (roughly every five minutes).
/// Readings belong to the truck, not to a plan; a plan selects readings by truck and time window.
/// </summary>
public sealed record PositionReading(TruckId TruckId, DateTimeOffset Timestamp, Coordinate Position);
