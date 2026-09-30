namespace Dfds.TruckPlans.Domain;

/// <summary>A WGS84 position in decimal degrees.</summary>
public readonly record struct Coordinate
{
    /// <summary>Mean Earth radius, the conventional value for the haversine formula.</summary>
    private const double EarthRadiusKm = 6371.0;

    public Coordinate(double latitude, double longitude)
    {
        if (double.IsNaN(latitude) || latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must be between -90 and 90.");
        if (double.IsNaN(longitude) || longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude must be between -180 and 180.");

        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }
    public double Longitude { get; }

    /// <summary>
    /// Great-circle distance in kilometres, using the haversine formula on a spherical Earth.
    /// </summary>
    public double DistanceToKm(Coordinate other)
    {
        var lat1 = double.DegreesToRadians(Latitude);
        var lat2 = double.DegreesToRadians(other.Latitude);
        var deltaLat = double.DegreesToRadians(other.Latitude - Latitude);
        var deltaLon = double.DegreesToRadians(other.Longitude - Longitude);

        var a = Math.Pow(Math.Sin(deltaLat / 2), 2)
              + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(deltaLon / 2), 2);

        // atan2 rather than asin(√a): stays defined if rounding pushes a slightly above 1.
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }
}
