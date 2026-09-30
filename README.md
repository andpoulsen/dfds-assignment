# DFDS Truck Plans

A solution to the DFDS Truck Plan exercise: modelling drivers, trucks and GPS readings, and answering

> "How many kilometres did drivers over the age of 50 drive in Germany in February 2024?"

The exercise is deliberately open-ended. Every interpretation of the brief, and everything deliberately left out, is recorded in [docs/ASSUMPTIONS.md](docs/ASSUMPTIONS.md). That document is as much part of the solution as the code.

## Getting started

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```sh
dotnet test
```

This builds the solution and runs all tests. No database, network access or configuration is needed: the country boundary data is embedded in the Infrastructure assembly.

## The four parts of the exercise

| Part | Where | In short |
|---|---|---|
| 1. Domain model | [src/Domain](src/Domain) | `Driver`, `Truck`, `TruckPlan` and `PositionReading`, with the value object `Coordinate` and typed IDs. A `TruckPlan` holds its driver and truck, and owns its GPS readings, which are added as they arrive and kept in time order. |
| 2. Distance for a Truck Plan | [`TruckPlan.DistanceDrivenKm`](src/Domain/TruckPlan.cs), [`Coordinate.DistanceToKm`](src/Domain/Coordinate.cs) | The sum of straight-line distances between consecutive readings, using the haversine formula. |
| 3. Country for a GPS coordinate | [`ICountryResolver`](src/Application/ICountryResolver.cs), [`NaturalEarthCountryResolver`](src/Infrastructure/NaturalEarthCountryResolver.cs) | Offline point-in-polygon lookup against Natural Earth country boundaries, using NetTopologySuite. Returns an ISO country code, or none at sea. |
| 4. The February 2024 question | [`DistanceDrivenQuery`](src/Application/DistanceDrivenQuery.cs), [`DriverFilters`](src/Application/DriverFilters.cs) | Kilometres driven in a country during a period, by the drivers a filter selects. |

## Using the code

**Part 2: distance for a Truck Plan** ([TruckPlanTests](tests/Domain.Tests/TruckPlanTests.cs))

```csharp
var plan = new TruckPlan(TruckPlanId.New(), driver, truck);
plan.AddReading(new PositionReading(truck.Id, new DateTimeOffset(2024, 2, 12, 8, 0, 0, TimeSpan.Zero), new Coordinate(53.5511, 9.9937)));  // Hamburg
plan.AddReading(new PositionReading(truck.Id, new DateTimeOffset(2024, 2, 12, 9, 0, 0, TimeSpan.Zero), new Coordinate(54.0717, 9.9900)));  // Neumünster

double km = plan.DistanceDrivenKm();
```

**Part 3: country for a GPS coordinate** ([NaturalEarthCountryResolverTests](tests/Infrastructure.Tests/NaturalEarthCountryResolverTests.cs))

```csharp
var resolver = new NaturalEarthCountryResolver();

resolver.GetCountryCode(new Coordinate(53.5511, 9.9937));  // "DE" (Hamburg)
resolver.GetCountryCode(new Coordinate(56.0, 3.0));        // null (North Sea)
```

**Part 4: the February 2024 question** ([FebruaryQueryEndToEndTests](tests/Application.Tests/FebruaryQueryEndToEndTests.cs))

```csharp
var query = new DistanceDrivenQuery(new NaturalEarthCountryResolver());

double km = query.KilometresDriven(
    plans,
    countryCode: "DE",
    periodStart: new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero),
    periodEnd: new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero),
    includeDriver: DriverFilters.OlderThan(50));
```

No real data came with the exercise, so there is no single number to report. The end-to-end test answers the question for a sample drive from Hamburg across the Danish border, using the real country boundaries.

## Project structure

```
src/
  Domain/            Entities and value objects. No external dependencies.
  Application/       The February query and the ICountryResolver interface it needs.
  Infrastructure/    NetTopologySuite-based country lookup and the embedded boundary data.
tests/
  Domain.Tests/
  Application.Tests/
  Infrastructure.Tests/
docs/
  ASSUMPTIONS.md     Assumptions (A1–A31) and out-of-scope items (O1–O14).
```

Dependencies point inwards: Infrastructure → Application → Domain. The query can be tested with a simple fake country resolver, and the country lookup could be replaced (for example by an external geocoding service) without changing the domain or the query.

## Key decisions

The full reasoning is in [docs/ASSUMPTIONS.md](docs/ASSUMPTIONS.md). The most important decisions are:

- **The model is shaped for a single relational database** accessed through an ORM such as EF Core, which is why a plan holds its driver and truck as objects (A12). Storage itself is out of scope (O1).
- **Distance is approximate.** Straight lines between readings taken about every five minutes slightly under-estimate the road distance (A13–A15). The haversine formula is our own code rather than a NuGet package (A16), and has not been independently verified by the author (O10).
- **Countries are resolved offline** rather than through an external service. It's fast, free, testable, and keeps truck positions in-house (A17). Boundaries are simplified, so points near borders and coastlines can resolve wrongly or to no country (A21).
- **"Over 50" means 51 or older**, with age measured on the date of the plan's first reading (A25, A26).
- **February is read in UTC** (A27).
- **A stretch between two readings is split at its midpoint**, so a stretch crossing a border or the start or end of the month counts half (A28).
- **Readings with no country never count**, so ferry crossings are never counted as driving, at the cost of a small under-count near coasts and ports (A29).
- **The query currently takes all plans in memory.** With storage, it would only load the relevant plans and readings (A30, O14).

## How this was built

The code was written by Claude Opus 5.5, in a conversation where I explained how I wanted the assignment solved as if I was pair/mob programming with another person at the keyboard. I spoke rather than typed, using the dictation tool Wispr Flow.

I started by modelling the domain, together with its unit tests. I moved on to the next parts of the assignment once the domain was modelled the way I saw fit. Along the way I made the design decisions. For example, I replaced the Truck Plan's time window with a list of GPS readings, and decided that readings with no country should never count, so that ferry crossings aren't counted as driving. Assumptions and out-of-scope items were recorded (also by Claude) as each decision was made, and each part of the assignment was committed separately, so the git history shows how the solution evolved.

The haversine distance formula is one part I have not verified myself (see O10).

### Why unit tests rather than a REST API

When I started, I assumed I would end up building a REST API to demonstrate the code. As I worked through the assignment, I realised the domain itself raised so many questions: how to model plans and readings, how to calculate distances, how to measure a driver's age, what counts as driving in Germany. Those decisions are what's interesting to discuss, rather than the plumbing around them.

Using an API to demonstrate would also need a lot of setup before it showed anything interesting: posting drivers, trucks, plans and lists of GPS readings before finally calling the query. Unit tests show the same behaviour directly, one decision per test, and can be read as examples. So I chose to demonstrate the domain code through its tests (see O3). 
