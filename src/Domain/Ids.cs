namespace Dfds.TruckPlans.Domain;

public readonly record struct DriverId(Guid Value)
{
    public static DriverId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}

public readonly record struct TruckId(Guid Value)
{
    public static TruckId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}

public readonly record struct TruckPlanId(Guid Value)
{
    public static TruckPlanId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
