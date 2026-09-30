namespace Dfds.TruckPlans.Domain;

/// <summary>A person who drives trucks.</summary>
public sealed class Driver
{
    public Driver(DriverId id, string name, DateOnly dateOfBirth)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Driver name is required.", nameof(name));

        Id = id;
        Name = name.Trim();
        DateOfBirth = dateOfBirth;
    }

    public DriverId Id { get; }
    public string Name { get; }
    public DateOnly DateOfBirth { get; }

    /// <summary>Age in completed years on the given date.</summary>
    public int AgeOn(DateOnly date)
    {
        if (date < DateOfBirth)
            throw new ArgumentOutOfRangeException(nameof(date), "Date is before the driver's date of birth.");

        var age = date.Year - DateOfBirth.Year;
        if (date < DateOfBirth.AddYears(age))
            age--;
        return age;
    }
}
