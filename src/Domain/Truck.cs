namespace Dfds.TruckPlans.Domain;

/// <summary>A truck with an installed GPS device that reports its position.</summary>
public sealed class Truck
{
    public Truck(TruckId id, string registrationNumber)
    {
        if (string.IsNullOrWhiteSpace(registrationNumber))
            throw new ArgumentException("Registration number is required.", nameof(registrationNumber));

        Id = id;
        RegistrationNumber = registrationNumber.Trim();
    }

    public TruckId Id { get; }
    public string RegistrationNumber { get; }
}
