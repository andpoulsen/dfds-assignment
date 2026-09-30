using Dfds.TruckPlans.Domain;

namespace Dfds.TruckPlans.Infrastructure.Tests;

public class NaturalEarthCountryResolverTests
{
    // Loading the boundary data takes a moment, so all tests share one instance.
    private static readonly NaturalEarthCountryResolver Resolver = new();

    [Theory]
    [InlineData(53.5511, 9.9937, "DE")]  // Hamburg
    [InlineData(48.1351, 11.5820, "DE")] // Munich
    [InlineData(52.5200, 13.4050, "DE")] // Berlin
    [InlineData(55.6786, 12.5317, "DK")] // Copenhagen (Frederiksberg)
    [InlineData(59.9139, 10.7522, "NO")] // Oslo
    [InlineData(48.8566, 2.3522, "FR")]  // Paris
    [InlineData(51.9244, 4.4777, "NL")]  // Rotterdam
    [InlineData(51.5072, -0.1276, "GB")] // London
    public void Resolves_the_country_of_a_city(double latitude, double longitude, string expected)
    {
        Assert.Equal(expected, Resolver.GetCountryCode(new Coordinate(latitude, longitude)));
    }

    [Theory]
    [InlineData(54.7833, 9.4333, "DE")] // Flensburg, a few km south of the Danish border
    [InlineData(54.8261, 9.3634, "DK")] // Padborg, just north of the German border
    public void Resolves_towns_on_either_side_of_a_border(double latitude, double longitude, string expected)
    {
        Assert.Equal(expected, Resolver.GetCountryCode(new Coordinate(latitude, longitude)));
    }

    [Theory]
    [InlineData(56.0, 3.0)]  // North Sea
    [InlineData(58.0, 9.0)]  // Skagerrak, on the Copenhagen–Oslo ferry route
    public void Returns_null_at_sea(double latitude, double longitude)
    {
        Assert.Null(Resolver.GetCountryCode(new Coordinate(latitude, longitude)));
    }

    [Fact]
    public void Known_limitation_simplified_coastlines_can_put_a_coastal_city_centre_at_sea()
    {
        // Copenhagen City Hall Square. Natural Earth's simplified coastline (both 1:50m and 1:10m)
        // leaves it outside Denmark, so it resolves to no country. Relevant for ports and harbours.
        var copenhagenCityHallSquare = new Coordinate(55.6761, 12.5683);

        Assert.Null(Resolver.GetCountryCode(copenhagenCityHallSquare));
    }
}
