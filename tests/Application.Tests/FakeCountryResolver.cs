using Dfds.TruckPlans.Domain;

namespace Dfds.TruckPlans.Application.Tests;

/// <summary>Resolves countries from a fixed lookup, so tests don't depend on real boundary data.</summary>
internal sealed class FakeCountryResolver(Dictionary<Coordinate, string?> countries) : ICountryResolver
{
    public string? GetCountryCode(Coordinate coordinate) => countries[coordinate];
}
