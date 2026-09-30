using Dfds.TruckPlans.Domain;

namespace Dfds.TruckPlans.Application;

/// <summary>Determines which country a GPS coordinate is in.</summary>
public interface ICountryResolver
{
    /// <summary>
    /// The ISO 3166-1 alpha-2 code (e.g. "DE") of the country containing the coordinate,
    /// or null if it isn't in any country, such as at sea.
    /// </summary>
    string? GetCountryCode(Coordinate coordinate);
}
