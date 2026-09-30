using System.Text.Json;
using Dfds.TruckPlans.Domain;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.IO.Converters;
using Coordinate = Dfds.TruckPlans.Domain.Coordinate;
using NtsCoordinate = NetTopologySuite.Geometries.Coordinate;

namespace Dfds.TruckPlans.Infrastructure;

/// <summary>
/// Resolves countries offline by testing which Natural Earth country boundary contains the point.
/// Loading the boundaries is relatively expensive, so create one instance and reuse it.
/// </summary>
public sealed class NaturalEarthCountryResolver : ICountryResolver
{
    private const string ResourceName = "ne_50m_admin_0_countries.geojson";

    // Natural Earth's ISO_A2 is "-99" for some countries (e.g. France, Norway); the _EH ("eh") variant fills those in.
    private const string CountryCodeAttribute = "ISO_A2_EH";
    private const string UnknownCountryCode = "-99";

    private readonly GeometryFactory _geometryFactory = new();
    private readonly STRtree<Country> _index = new();

    public NaturalEarthCountryResolver()
    {
        foreach (var feature in LoadFeatures())
        {
            var code = feature.Attributes[CountryCodeAttribute]?.ToString();
            if (string.IsNullOrEmpty(code) || code == UnknownCountryCode)
                continue;

            var country = new Country(code, PreparedGeometryFactory.Prepare(feature.Geometry));
            _index.Insert(feature.Geometry.EnvelopeInternal, country);
        }

        _index.Build();
    }

    public string? GetCountryCode(Coordinate coordinate)
    {
        // GeoJSON and NetTopologySuite use (x, y) = (longitude, latitude).
        var point = _geometryFactory.CreatePoint(new NtsCoordinate(coordinate.Longitude, coordinate.Latitude));

        return _index.Query(point.EnvelopeInternal)
            .FirstOrDefault(country => country.Boundary.Covers(point))
            ?.Code;
    }

    private static FeatureCollection LoadFeatures()
    {
        using var stream = typeof(NaturalEarthCountryResolver).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' not found.");

        var options = new JsonSerializerOptions { Converters = { new GeoJsonConverterFactory() } };
        return JsonSerializer.Deserialize<FeatureCollection>(stream, options)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' contains no features.");
    }

    private sealed record Country(string Code, IPreparedGeometry Boundary);
}
