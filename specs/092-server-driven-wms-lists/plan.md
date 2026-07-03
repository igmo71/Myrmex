# Implementation Plan: Server-Driven WMS Catalog and Topology Lists

**Branch**: `092-server-driven-wms-lists` | **Date**: 2026-07-03 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/092-server-driven-wms-lists/spec.md`

## Summary

Convert the SKU, Unit of Measure, Zone, and Storage Location pages to the established server-driven `MudDataGrid` flow, complete the already server-driven Warehouse page's public contract normalization, and replace the fixed Warehouse selectors on the Zone and Storage Location pages with a bounded server-search lookup. Move affected public contracts into `Myrmex.Shared`; retain separate internal queries and backend-owned projections; bind feature-specific requests at existing routes; add missing audit-field sorts and Storage Location type/status filters; preserve deterministic ID tie-breakers delivered by feature 090; and protect changed handler, endpoint-binding, and API-client boundaries with focused tests.

## Technical Context

**Language/Version**: C# / .NET 10

**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core 10 with SQL Server, Blazor Web App, MudBlazor 9, existing Myrmex command/query dispatchers, `Myrmex.Shared` transport contracts

**Storage**: Existing WMS SQL Server schema through `WmsDbContext`; no model, schema, index, seed, or migration changes

**Testing**: xUnit v3, SQL Server-backed `TestWmsDbContext` handler/persistence tests, focused in-process Minimal API endpoint tests, existing fake-HTTP API-client tests, and manual WebApp smoke validation

**Target Platform**: Existing Myrmex modular-monolith API and WebApp

**Project Type**: Multi-project web application with one WMS module, shared transport assembly, WebApp client/UI, and consolidated test project

**Performance Goals**: At least 95% of page, sort, and filter interactions display within 2 seconds on the agreed representative environment with datasets up to 50,000 records; SKU search remains complete with at least 35,000 SKUs

**Constraints**: Preserve routes, search meaning, authorization, domain rules, mutations, localized text, ProblemDetails conventions, imports, and public details shapes; no generic list/lookup framework; no unrestricted Storage Location load without a selected Warehouse; validation commands remain developer-controlled

**Scale/Scope**: Five list slices, two API clients, five WebApp list pages, one Warehouse lookup used by two pages, shared DTO migration for affected Catalog/Topology contracts, focused tests at three owning boundaries

## Constitution Check

*GATE: Passed before Phase 0 research and re-checked after Phase 1 design.*

- **Domain Model First**: PASS. The plan uses SKU, Unit of Measure, Warehouse, Zone, and Storage Location concepts and preserves all entity invariants and lifecycle behavior.
- **Modular Monolith Boundaries**: PASS. Catalog and Topology keep their current ownership. Only plain transport types move to `Myrmex.Shared`; internal queries, handlers, projections, and persistence remain in `Myrmex.Modules.Wms`.
- **Vertical Slice Delivery**: PASS. Each list retains explicit endpoint-to-query mapping and backend projection. The Warehouse lookup is a separate explicit Topology slice rather than a generic framework.
- **Testing Discipline**: PASS. Handler tests own query semantics, endpoint tests own binding, API-client tests own URLs/cancellation, and repeated grid interactions use manual smoke validation because no component-test pattern exists.
- **Simplicity and Observability**: PASS. The design reuses Inventory Balance and Warehouse grid patterns, `ListResult<T>`, existing read/load exception flow, write `ApiResult<T>` flow, and current UI error presentation.
- **Post-design re-check**: PASS. Research, data model, and contracts preserve module, domain, persistence, route, and error boundaries. No constitution exception remains.

## Project Structure

### Documentation (this feature)

```text
specs/092-server-driven-wms-lists/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- catalog-contracts.md
|   `-- topology-contracts.md
`-- checklists/
    `-- requirements.md
```

`tasks.md` is generated later by `/speckit-tasks`.

### Source Code (repository root)
```text
Myrmex.Shared/
|-- Common/ListResult.cs
`-- Wms/
    |-- Catalog/                 # list/details/create/update/sort contracts
    `-- Topology/                # list/lookup/details/create/update/sort contracts

Myrmex.Modules.Wms/
|-- Catalog/{Endpoints,Features/{StockKeepingUnits,UnitsOfMeasure}}/
`-- Topology/{Endpoints,Features/{Warehouses,Zones,StorageLocations}}/

Myrmex.WebApp/
|-- Wms/{Catalog,Topology}/
`-- Components/Pages/Wms/
    |-- Catalog/{SkuPages,UomPages}/
    `-- Topology/{WarehousePages,ZonePages,StorageLocationPages}/

Myrmex.Tests/Wms/
|-- Catalog/{Client,Endpoints,Features}/
`-- Topology/{Client,Endpoints,Features}/
```

**Structure Decision**: Keep current projects and feature directories. Add plain shared contract types; keep domain-aware mapping and EF projection expressions in internal per-entity helpers. Add page-local grid-request records for the four legacy pages and retain `WarehouseGridRequest`.

## Architectural Design Notes

- **Domain concepts first**: SKU and Unit of Measure remain Catalog reference data. Warehouse, Zone, Storage Location, Type, and Status remain Topology concepts. No identity, ownership, lifecycle, validation, or state transition changes.
- **Shared contract boundary**: Move five details DTOs, Storage Location type/status details, affected create/update requests, five feature-specific list requests, and five sort classes to `Myrmex.Shared`. Add `LookupWarehousesRequest` and `WarehouseLookupItem`. Do not move domain conversion or EF projection code.
- **Internal request boundary**: Retain the five internal list queries and map shared endpoint requests explicitly. Add internal `LookupWarehouses.Query` and handler under Topology.
- **Backend-owned projection**: Replace projections attached to module-local details records with internal helpers that construct shared details DTOs. List/get handlers project before materialization; commands map domain results without serializing entities.
- **Server-driven list behavior**: Normalize paging, filter, count, deterministically order with ascending ID ties, page, then project. Preserve feature 090 tie-breakers. Add CreatedAtUtc/UpdatedAtUtc sorts to SKU/UoM and Type/Status ID filters to Storage Locations. Unknown sorts retain deterministic fallbacks.
- **Storage Location binding**: Keep both nested routes. One `ListStorageLocationsRequest` exposes nullable WarehouseId and ZoneId so `[AsParameters]` binds matching route values plus type/status/list query values. Existing not-found and Warehouse/Zone mismatch behavior remains. Unknown type/status IDs produce zero matches.
- **Warehouse lookup**: Add `GET /api/wms/topology/warehouses/lookup`; search Code/Name/Description; default/max Take 20; selectable-only defaults active; order Name, Code, ID; return ID, Code, Name, IsActive.
- **Client/grid behavior**: Clients build feature-specific query strings and omit nulls. SKU, UoM, Zone, and Storage Location grids adopt `ServerData`, single sort, shared tags, default Code order, standard page sizes, filter reset, and current-page mutation refresh. Warehouse switches contract/fallback constants without redesign.
- **Storage Location gating**: Without a Warehouse, return empty `GridData` and make no list request. Warehouse changes clear Zone/results; all list filters reset page zero.
- **Deferred Zone selector**: Keep the current first-page Zone selector on Storage Locations. It does not block server filtering. Record dedicated Zone lookup/autocomplete as follow-up.
- **Ancillary UoM data**: Keep SKU base-UoM display/edit selection separate; do not add a UoM lookup slice in this issue.
- **Cancellation and errors**: Propagate grid/autocomplete tokens through client, endpoint, dispatcher, and EF. Suppress only expected token cancellation. Preserve `_errorMessage` for real reads and `ApiResult<T>`/ProblemDetails for writes.
- **Localization**: Reuse current text. Any required autocomplete key must exist in neutral, `ru-RU`, and `en-US`; sort keys and business data remain unlocalized.
- **Risk-based testing**: Handler tests cover SKU/UoM audit sorts, Storage Location filters/totals, shared projection shape, and Warehouse lookup. Endpoint tests cover request/route binding. Client tests cover distinct URLs and representative cancellation. UI interactions use manual smoke checks.
- **Existing pattern precedence**: Follow Inventory Balance and Warehouse server-grid code plus existing SKU/Storage Location bounded lookup slices.

## Implementation Sequence

1. Add shared Catalog/Topology contracts and internal projection helpers; update backend, WebApp, Inventory consumers, and tests to shared namespaces without changing JSON.
2. Bind five feature-specific list requests and add the Warehouse lookup endpoint/slice while preserving route order and paths.
3. Add SKU/UoM audit sorts, PascalCase constants with case-insensitive compatibility, and Storage Location type/status filters before count.
4. Update both API clients and add bounded Warehouse lookup URL construction.
5. Migrate SKU/UoM, then Zone/Storage Location grids; normalize Warehouse request/constants; replace two Warehouse fixed selects with autocomplete.
6. Add focused handler, endpoint, and client tests, then perform static scope/contract review.
7. Hand off developer-controlled automated, manual, and performance validation in [quickstart.md](./quickstart.md).

## Validation Plan

- Confirm shared types have no domain/EF/UI dependencies, duplicate public DTOs are removed, JSON shapes and routes remain stable.
- Verify changed sort/filter/lookup behavior with SQL Server-backed handler tests.
- Verify route/query binding into shared requests and internal queries with real Minimal API endpoint tests.
- Verify client encoding, omitted values, nested routes, deserialization, and cancellation.
- Manually smoke page/page-size/sort/search/filter reloads, totals, reset, mutation refresh, Warehouse autocomplete, no-Warehouse gating, cancellation, and genuine errors.
- Record representative 35,000-SKU and up-to-50,000-record interaction latency; at least 95% must display within 2 seconds.
- Confirm no migration, schema, domain, import, route, generic framework, redesign, or unrelated Inventory behavior change.

## Deferred Follow-Up

- Replace the Storage Location page's Zone `Take = 100` selector with a dedicated Topology-owned bounded Zone lookup/autocomplete in a separate approved feature.

## Complexity Tracking

No constitution violations or unjustified complexity are introduced.

### Principle IV Endpoint/UI Test Exceptions

| Deferred automated test | Why deferred | Lower-level automated coverage | Manual validation required | Follow-up issue needed? |
|-------------------------|--------------|--------------------------------|----------------------------|-------------------------|
| Repeated MudDataGrid paging, reset, reload, and autocomplete interactions | No established component-test pattern; adding one is disproportionate for accepted Inventory/Warehouse UI behavior | Handler tests protect data; endpoint tests protect binding; client tests protect URLs/responses/cancellation | All five pages and both Warehouse selectors per `quickstart.md` | No; add automation only when an established pattern or distinct regression justifies it |
