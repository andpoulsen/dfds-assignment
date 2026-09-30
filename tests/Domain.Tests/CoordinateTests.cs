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

    [Theory]
    [InlineData(0, 0, 1, 0, 111.1949)]         // 1 degree of latitude
    [InlineData(0, 0, 0, 1, 111.1949)]         // 1 degree of longitude at the equator
    [InlineData(60, 0, 60, 1, 55.5969)]        // 1 degree of longitude at 60°N is about half
    [InlineData(0, 179.5, 0, -179.5, 111.1949)] // across the 180° meridian, the short way round
    public void DistanceToKm_matches_known_great_circle_distances(
        double lat1, double lon1, double lat2, double lon2, double expectedKm)
    {
        var from = new Coordinate(lat1, lon1);
        var to = new Coordinate(lat2, lon2);

        Assert.Equal(expectedKm, from.DistanceToKm(to), precision: 4);
    }

    [Fact]
    public void DistanceToKm_between_Hamburg_and_Munich_is_about_612_km()
    {
        var hamburg = new Coordinate(53.5511, 9.9937);
        var munich = new Coordinate(48.1351, 11.5820);

        Assert.Equal(612.43, hamburg.DistanceToKm(munich), precision: 2);
    }

    [Fact]
    public void DistanceToKm_is_zero_for_the_same_point()
    {
        var hamburg = new Coordinate(53.5511, 9.9937);

        Assert.Equal(0, hamburg.DistanceToKm(hamburg));
    }

    [Fact]
    public void DistanceToKm_is_symmetric()
    {
        var hamburg = new Coordinate(53.5511, 9.9937);
        var munich = new Coordinate(48.1351, 11.5820);

        Assert.Equal(hamburg.DistanceToKm(munich), munich.DistanceToKm(hamburg), precision: 9);
    }
}
