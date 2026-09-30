# Assumptions and scope

The exercise is deliberately open-ended. This document records the assumptions made where the brief is ambiguous, and what has been deliberately left out of scope. It is updated as the solution evolves.

## Assumptions

### Domain

- **A1** A Truck Plan has exactly one driver and one truck, and neither changes during the plan.
- **A2** The GPS device is permanently installed in its truck, so readings are identified by truck. Mapping a device to a truck happens outside the domain.
- **A3** Whatever receives the GPS data knows which plan is active for a truck and adds each reading to that plan. The domain only checks that the reading is from the plan's truck.
- **A4** A plan has no explicit start and end. Its period is implied by its first and last reading.
- **A12** Data is expected to live in a single relational database, accessed through an ORM such as EF Core. `TruckPlan` therefore holds its `Driver` and `Truck` as object references (foreign keys in the database) and owns its GPS readings (a child table). Drivers and trucks remain separate entities with their own identity, and exist independently of any plan. If drivers, trucks, plans and readings lived in separate stores (e.g. document databases or event streams), the model would instead reference them by ID only.

### GPS readings

- **A5** Timestamps are exact moments in time (`DateTimeOffset`). Two timestamps with different UTC offsets that mean the same moment are treated as equal.
- **A6** Readings can arrive late or out of order, for example after a device was offline. The plan always keeps them in time order.
- **A7** Readings with the same timestamp are all kept, in arrival order. Duplicates aren't removed, and they add 0 km.
- **A8** Coordinates are standard GPS (WGS84) latitude and longitude in decimal degrees.

### Driver and truck

- **A9** Age is counted in completed calendar years. Someone born on 29 February has their birthday on 28 February in non-leap years.
- **A10** A driver's name and a truck's registration number are required, with surrounding spaces trimmed. Their format isn't validated.
- **A11** IDs are version 7 GUIDs created in the domain, not by the database.

### Distance

- **A13** The distance driven is the sum of straight-line (great-circle) distances between consecutive readings. Bends in the road between readings are lost, so it slightly under-estimates the road distance.
- **A14** The Earth is modelled as a sphere with a mean radius of 6,371 km, using the haversine formula. The error compared with the real Earth shape is well under 1%, which is acceptable for an approximate distance.
- **A15** Gaps between readings are bridged with a straight line, however long.
- **A16** The haversine formula is implemented in our own code rather than taken from a NuGet package. It is about ten lines and covered by tests against known distances, and avoiding a dependency avoids supply-chain and upgrade risk. The most trusted geo package, NetTopologySuite, calculates flat distances and doesn't solve this directly; the packages that do are maintained by single individuals. GeographicLib.NET would be the choice if higher accuracy were needed.

### Country lookup

- **A17** Countries are resolved offline, by testing which country boundary contains the point (point-in-polygon), rather than by calling an external reverse-geocoding service. It is fast, free, deterministic and testable, and no truck positions (personal data, together with the driver) are sent to a third party. An external service could be added behind the same `ICountryResolver` interface without changing the domain.
- **A18** Country boundaries come from Natural Earth's 1:50m admin-0 countries dataset (public domain), embedded in the Infrastructure assembly. The file is taken unmodified from the `nvkelso/natural-earth-vector` repository (version 5.x, last changed in commit `9380cca`, May 2022).
- **A19** Point-in-polygon tests use NetTopologySuite, the most widely used .NET geometry library (EF Core's spatial support is built on it). Unlike the distance formula (A16), this is too large to write ourselves.
- **A20** Countries are identified by ISO 3166-1 alpha-2 codes (e.g. `DE`), taken from Natural Earth's `ISO_A2_EH` field. The plain `ISO_A2` field is `-99` for France and Norway, among others. Kosovo is returned as `XK`, a code in common use but not an official ISO code. Areas with no code in the data (Somaliland, Northern Cyprus, Siachen Glacier) resolve to no country.
- **A21** Boundaries are simplified, so points close to a border or coastline can be assigned to the wrong country or to none. At the 1:50m scale this can be several hundred metres or more; Copenhagen's city centre, for example, resolves to no country. The more detailed 1:10m dataset (about 13 MB) gives the same result there, so it wasn't worth the size. This matters for DFDS, since ports and harbours lie on the coast.
- **A22** Points at sea, including on ferries, resolve to no country. The domain returns null for these rather than guessing.
- **A23** A point exactly on a border between two countries is assigned to whichever country is found first.
- **A24** `ICountryResolver` is defined in the Application project, next to the February query that uses it, and implemented in Infrastructure. Dependencies point inwards (Infrastructure → Application → Domain, never the reverse), so the query doesn't depend on NetTopologySuite or the boundary data, and can be tested with a simple fake. It started out in the Domain project and moved to Application when the query was built, since country lookup is a need of a use case rather than a domain concept.

### The February 2024 query

- **A25** "Drivers over the age of 50" means strictly older than 50, i.e. 51 or older.
- **A26** Which drivers count is decided by a filter passed to the query, so the same query can answer other questions than "over 50". The filter is given the driver and the date (UTC) of the plan's first reading, so a driver's age is measured once per plan, on that date. If a birthday falls during a plan, the age at the start applies to the whole plan. `DriverFilters.OlderThan(50)` answers the brief's question.
- **A27** "February 2024" is read in UTC: from 1 February 00:00 UTC (inclusive) to 1 March 00:00 UTC (exclusive). German local time would be simple to support, but countries spanning several time zones would add complexity that was left out for simplicity.
- **A28** A stretch between two consecutive readings is split at its midpoint. Each reading owns half the stretch, and that half counts only if the reading is in the requested country and inside the period. A stretch crossing a border or the start or end of the month therefore counts half. Stretches are a few kilometres long, so the error per crossing is small.
- **A29** A reading that resolves to no country (at sea, on a ferry, or on a simplified coastline, see A21) counts as not being in the requested country. Ferry crossings are therefore never counted as driving. The cost is a small under-count near coasts and ports, such as Kiel or Rostock, where the simplified coastline misses land. Carrying the previous reading's country forward was considered, but rejected: it would count whole ferry crossings as driven in the country the ship left from.
- **A30** The query takes the Truck Plans as input, which in practice would mean loading all plans and all their readings. That is only acceptable because there is no storage (O1). With a persistence layer, the query would ask a repository for only the relevant data: plans with readings in the period, filtered in the database by timestamp and, roughly, by the drivers' date of birth. The exact age check and distance calculation would then run on that much smaller set.
- **A31** Country codes are compared case-insensitively (`de` matches `DE`).

## Out of scope

- **O1** Storage: no persistence is implemented, but the model is shaped for a relational database (see A12).
- **O2** Receiving GPS data: device protocols, messaging, and routing readings to the active plan.
- **O3** API or user interface.
- **O4** Consistency rules such as a driver or truck being in two plans at the same time.
- **O5** GPS data quality: jitter, outliers, gaps in the data, removing duplicates.
- **O6** Device management: replacing devices or moving them between trucks.
- **O7** Adding readings to one plan from several threads at once.
- **O8** Snapping readings to the road network (e.g. OSRM or Valhalla) for a more accurate distance.
- **O9** Calculating distances in the database. With spatial storage (e.g. PostGIS or SQL Server's `geography` type), distance calculations could move to the database layer. Storage is out of scope for now (O1).
- **O10** Independent verification of the haversine implementation. The author, Anders Poulsen, has not verified the formula or its implementation himself. The implementation was written by Claude (an AI assistant) and trusted, since learning the underlying mathematics well enough to check it was too much for the time available. The tests give partial assurance: 1° of latitude equals 111.195 km, which follows from basic geometry (2π × 6,371 km ÷ 360), and Hamburg to Munich comes out at about 612 km, which matches published straight-line distances. The other reference values in the tests were produced with the same formula, so they only confirm that the code matches the formula as written.
- **O11** External reverse-geocoding services (e.g. Nominatim, Azure Maps, Google). They are more accurate near borders and coastlines, but bring rate limits, cost, network dependency and privacy concerns. They could be added behind `ICountryResolver`.
- **O12** Keeping the boundary data up to date, and handling disputed borders beyond what Natural Earth provides.
- **O13** Assigning a country to coastal readings that resolve to none, for example using the nearest country, without mistaking ferry crossings for driving. This would remove the small under-count near coasts and ports described in A29.
- **O14** Scaling the query to real data volumes: a repository that filters in the database (A30), spatial filtering by country (O9), and precomputing kilometres per plan, country and day as readings arrive, so questions like the February one don't scan raw readings.
