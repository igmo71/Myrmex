# Implementation Plan: Phase 2 Deterministic Legacy List Ordering

**Branch**: `090-normalize-wms-list-contracts` | **Date**: 2026-07-02 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/090-normalize-wms-list-contracts/spec.md`

## Summary

Make paging order deterministic in the four legacy backend-owned list slices identified by the completed audit: Zones, Storage Locations, SKUs, and UoM. Preserve every existing primary sort key, direction, fallback, filter, count, paging, projection, result, and public boundary. Add ascending record-ID tie-breakers to every ordering branch and focused handler/persistence tests using duplicate non-unique primary values. No grid or contract migration is included.

## Technical Context

**Language/Version**: C# / .NET 10

**Primary Dependencies**: EF Core query ordering, existing internal query handlers, xUnit, existing `TestWmsDbContext`; no dependency changes

**Storage**: Existing WMS EF Core model; no model, schema, index, or migration changes

**Testing**: Focused handler/persistence tests in `Myrmex.Tests`; validation commands are developer-run

**Target Platform**: Existing Myrmex modular-monolith backend

**Project Type**: Backend vertical-slice normalization within the WMS module

**Performance Goals**: Preserve current query shape and page bounds while making all returned pages deterministically ordered

**Constraints**: Four list handlers and focused tests only; preserve primary sorts, public contracts, routes, WebApp behavior, resources, imports, and database behavior

**Scale/Scope**: Four `ApplySorting` implementations and four handler test suites

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Domain Model First**: PASS. Zone, Storage Location, SKU, and UoM identity and business behavior are unchanged; only query ordering among equal values changes.
- **Modular Monolith Boundaries**: PASS. Ordering stays in each owning WMS handler. No shared, endpoint, client, or UI dependency changes.
- **Vertical Slices with Explicit Requests**: PASS. Existing explicit internal queries and handlers remain intact; public transport behavior is preserved.
- **Tests Protect Domain and Integration Behavior**: PASS. Handler/persistence tests protect the EF ordering and page-boundary risk at its owning layer.
- **Pragmatic Simplicity and Observability**: PASS. Reuse the established Warehouse/inventory `ThenBy(Id)` pattern; add no sorting abstraction or diagnostics.
- **Post-design re-check**: PASS. The design artifacts introduce no new boundary, schema, interface, or constitution exception.

## Project Structure

### Documentation (this feature)

```text
specs/090-normalize-wms-list-contracts/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
`-- checklists/
    `-- requirements.md
```

No `contracts/` artifact is created because Phase 2 preserves every external interface.

### Source Code (repository root)
```text
Myrmex.Modules.Wms/
|-- Topology/Features/Zones/ListZones.cs
|-- Topology/Features/StorageLocations/ListStorageLocations.cs
|-- Catalog/Features/StockKeepingUnits/ListStockKeepingUnits.cs
`-- Catalog/Features/UnitsOfMeasure/ListUnitsOfMeasure.cs

Myrmex.Tests/Wms/
|-- Topology/Features/Zones/ListZonesHandlerTests.cs                    # new
|-- Topology/Features/StorageLocations/ListStorageLocationsHandlerTests.cs # new
|-- Catalog/Features/StockKeepingUnits/ListStockKeepingUnitsHandlerTests.cs
`-- Catalog/Features/UnitsOfMeasure/ListUnitsOfMeasureHandlerTests.cs
```

**Structure Decision**: Change only each slice's existing private `ApplySorting` method and its handler-level test suite. Keep sorting explicit per slice, matching the accepted Warehouse implementation, rather than introducing a common helper. `research.md` remains completed Phase 1 evidence.

## Architectural Design Notes

- **Ordering rule**: Append `.ThenBy(x => x.Id)` to ascending and descending primary sort branches and to the fallback/default order. The ID tie-breaker remains ascending in both directions, matching `ListWarehouses` and the durable convention.
- **Primary behavior**: Do not change supported raw sort keys, key casing, default Code sort, `SortDescending` handling for primary values, filtering, count timing, normalized `Skip`/`Take`, projection, or result metadata.
- **Zones**: Update all Code, Name, CreatedAtUtc, UpdatedAtUtc, IsActive, and fallback branches in `ListZones.cs`.
- **Storage Locations**: Update all Code, Name, IsPickable, CreatedAtUtc, UpdatedAtUtc, IsActive, and fallback branches in `ListStorageLocations.cs`.
- **SKUs**: Update Code, Name, IsActive, and fallback branches in `ListStockKeepingUnits.cs`.
- **UoM**: Update Code, Name, IsActive, and fallback branches in `ListUnitsOfMeasure.cs`.
- **Focused tests**: Use duplicate Name values because Code is identity-like/unique. Assert returned IDs follow SQL Server ascending `uniqueidentifier` order for both ascending and descending Name requests, and verify adjacent pages concatenate to that same order. In test expectations, follow `ListWarehousesHandlerTests` and compare with `System.Data.SqlTypes.SqlGuid` rather than .NET `Guid` ordering.
- **Topology test setup**: Add narrow list-handler suites using existing `TestWmsDbContext` and domain creation patterns from neighboring Zone/Storage Location tests.
- **Catalog test setup**: Extend the two existing list-handler suites and adapt their private seed helpers to return created entities where ID assertions require it.
- **Planned test scenario in each suite**: `HandleAsync_WhenNameValuesMatch_OrdersByIdAcrossPages`, implemented as a two-direction theory. Seed three same-name records, request `Take = 2` and then `Skip = 2`, concatenate both pages, and compare IDs with the SQL Server-ordered expected sequence. Zone queries use one seeded Warehouse; Storage Location queries use one valid Warehouse/Zone plus required type/status references.
- **No boundary changes**: Do not touch endpoints, API clients, DTOs, `Myrmex.Shared`, WebApp components, resources, persistence configuration, or migrations.

## Phase 0 Decision Resolution

- No unresolved technical unknowns remain. [research.md](./research.md) is the completed decision base and is not regenerated.
- **Decision**: use ascending ID as the secondary tie-breaker for both primary directions. **Rationale**: this matches Warehouse and durable list guidance and provides a total order without changing primary-direction semantics. **Alternative rejected**: direction-matched ID ties would diverge from the closest accepted legacy implementation without user value.
- **Decision**: keep explicit ordering in each handler. **Rationale**: only four small switch expressions change. **Alternative rejected**: a generalized sorting abstraction is expressly out of scope and would increase coupling.
- **Decision**: protect non-unique Name sorting and page boundaries at handler/persistence level using SQL Server's `uniqueidentifier` ordering semantics. **Rationale**: EF/database ordering owns the risk, and the existing Warehouse test establishes the correct expected-ID comparer. **Alternative rejected**: endpoint, client, and UI tests cannot add distinct protection because those boundaries do not change.

## Phase 2 Planning Artifacts

- [data-model.md](./data-model.md) records that entities and persistence models are unchanged and defines the ordering invariant.
- No `contracts/` artifact is created because public and internal request/response shapes are unchanged.
- [quickstart.md](./quickstart.md) provides developer-controlled focused validation commands and expected outcomes.
- The managed Spec Kit section in `AGENTS.md` already points to this plan and needs no content change.

## Implementation Sequence

1. Add `ListZonesHandlerTests.cs` and `ListStorageLocationsHandlerTests.cs` with the focused duplicate-Name, two-direction, adjacent-page scenario.
2. Extend `ListStockKeepingUnitsHandlerTests.cs` and `ListUnitsOfMeasureHandlerTests.cs` with the same focused scenario, reusing their existing seed helpers.
3. Update each of the four private `ApplySorting` switch expressions so every supported and fallback branch appends ascending ID ordering.
4. Statically compare primary expressions, directions, raw key values, fallback Code ordering, and query pipeline placement with the pre-change behavior.
5. Hand off the focused test and optional build commands in `quickstart.md` for developer execution.

## Implementation Risks

- **Provider ordering mismatch**: .NET `Guid` comparison does not model SQL Server `uniqueidentifier` ordering. Test expectations must use `SqlGuid`.
- **Incomplete switch update**: missing one supported or fallback branch leaves a nondeterministic path. Static branch-by-branch review is required in addition to focused runtime tests.
- **Direction regression**: secondary ID stays ascending even when the primary is descending; only the existing primary expression changes direction.
- **Over-expansion**: nearby contract, grid, casing, or default-sort findings remain deferred even if encountered during implementation.
- **Topology fixture complexity**: test data must satisfy existing Warehouse, Zone, type, and status relationships without changing domain or persistence setup.

## Validation Plan

- Static review: verify every supported and fallback branch in the four handlers includes ascending ID tie resolution after the unchanged primary order.
- Focused tests: verify duplicate Name values resolve by ascending ID for both primary directions and remain complete/stable across adjacent pages.
- Regression review: existing catalog sort/fallback tests remain green and topology setup validates the required warehouse/zone relationships.
- Scope review: changed production files are exactly the four handlers; changed tests are exactly the four handler suites.
- Developer-run commands are documented in `quickstart.md`; this planning step does not execute builds or tests.
- Do not start WebApp/AppHost, run Docker/infrastructure, generate/apply migrations, or update a database.

## Complexity Tracking

No constitution violations or test exceptions are introduced. Four explicit local changes are simpler and safer than a generalized sorting abstraction.
