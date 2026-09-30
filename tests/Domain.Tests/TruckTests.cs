namespace Dfds.TruckPlans.Domain.Tests;

public class TruckTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_rejects_missing_registration_number(string? registrationNumber)
    {
        Assert.Throws<ArgumentException>(() => new Truck(TruckId.New(), registrationNumber!));
    }

    [Fact]
    public void Constructor_trims_registration_number()
    {
        var truck = new Truck(TruckId.New(), " AB 12 345 ");

        Assert.Equal("AB 12 345", truck.RegistrationNumber);
    }
}
