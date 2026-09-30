namespace Dfds.TruckPlans.Domain.Tests;

public class DriverTests
{
    private static Driver BornOn(int year, int month, int day) =>
        new(DriverId.New(), "Jane Doe", new DateOnly(year, month, day));

    [Theory]
    [InlineData(2024, 2, 14, 49)] // day before 50th birthday
    [InlineData(2024, 2, 15, 50)] // on 50th birthday
    [InlineData(2024, 2, 16, 50)] // day after 50th birthday
    [InlineData(2024, 12, 31, 50)]
    [InlineData(1974, 2, 15, 0)]  // day of birth
    public void AgeOn_counts_completed_years(int year, int month, int day, int expectedAge)
    {
        var driver = BornOn(1974, 2, 15);

        Assert.Equal(expectedAge, driver.AgeOn(new DateOnly(year, month, day)));
    }

    [Theory]
    [InlineData(2023, 2, 27, 22)]
    [InlineData(2023, 2, 28, 23)] // non-leap year: birthday counted on 28 Feb
    [InlineData(2024, 2, 28, 23)]
    [InlineData(2024, 2, 29, 24)]
    public void AgeOn_handles_leap_day_birthdays(int year, int month, int day, int expectedAge)
    {
        var driver = BornOn(2000, 2, 29);

        Assert.Equal(expectedAge, driver.AgeOn(new DateOnly(year, month, day)));
    }

    [Fact]
    public void AgeOn_throws_for_date_before_birth()
    {
        var driver = BornOn(1974, 2, 15);

        Assert.Throws<ArgumentOutOfRangeException>(() => driver.AgeOn(new DateOnly(1974, 2, 14)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_rejects_missing_name(string? name)
    {
        Assert.Throws<ArgumentException>(() => new Driver(DriverId.New(), name!, new DateOnly(1974, 2, 15)));
    }

    [Fact]
    public void Constructor_trims_name()
    {
        var driver = new Driver(DriverId.New(), "  Jane Doe  ", new DateOnly(1974, 2, 15));

        Assert.Equal("Jane Doe", driver.Name);
    }
}
