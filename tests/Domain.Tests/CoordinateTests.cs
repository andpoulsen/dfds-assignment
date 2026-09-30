namespace Dfds.TruckPlans.Domain.Tests;

public class CoordinateTests
{
    [Theory]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    [InlineData(0, 0)]
    [InlineData(55.6761, 12.5683)] // Copenhagen
    public void Constructor_accepts_valid_coordinates(double latitude, double longitude)
    {
        var coordinate = new Coordinate(latitude, longitude);

        Assert.Equal(latitude, coordinate.Latitude);
        Assert.Equal(longitude, coordinate.Longitude);
    }

    [Theory]
    [InlineData(-90.0001)]
    [InlineData(90.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Constructor_rejects_invalid_latitude(double latitude)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new Coordinate(latitude, 0));
        Assert.Equal("latitude", ex.ParamName);
    }

    [Theory]
    [InlineData(-180.0001)]
    [InlineData(180.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void Constructor_rejects_invalid_longitude(double longitude)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new Coordinate(0, longitude));
        Assert.Equal("longitude", ex.ParamName);
    }
}
