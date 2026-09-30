namespace Dfds.TruckPlans.Domain;

/// <summary>A WGS84 position in decimal degrees.</summary>
public readonly record struct Coordinate
{
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
}
