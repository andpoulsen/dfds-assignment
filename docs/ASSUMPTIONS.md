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

### Open questions

To be decided when the February 2024 query is built:

- Does "over the age of 50" mean strictly above 50?
- On which date is age measured: the day of driving, the start of the plan, or the query date?
- Is February read in UTC or in German local time?

## Out of scope

- **O1** Storage: no persistence is implemented, but the model is shaped for a relational database (see A12).
- **O2** Receiving GPS data: device protocols, messaging, and routing readings to the active plan.
- **O3** API or user interface.
- **O4** Consistency rules such as a driver or truck being in two plans at the same time.
- **O5** GPS data quality: jitter, outliers, gaps in the data, removing duplicates.
- **O6** Device management: replacing devices or moving them between trucks.
- **O7** Adding readings to one plan from several threads at once.
