# Research: WMS Catalog/UoM MVP Vertical Slice

## Decision: Implement UoM as a repeated Catalog reference-data slice

**Rationale**: Catalog/SKU is the representative reference-data slice. UoM has the same CRUD-style reference-data shape and should extend the existing Catalog capability rather than introducing a new module or generic framework.

**Alternatives considered**:

- Create a separate UoM module: rejected because the existing WMS Catalog capability already owns reference data.
- Build a generic reference-data engine: rejected because issue #36 needs one concrete repeated slice and Myrmex favors explicit local patterns.
- Add UoM under Topology: rejected because UoM describes quantities, not warehouse locations.

## Decision: Use `UnitOfMeasure` as the domain aggregate and UoM as user-facing wording

**Rationale**: `UnitOfMeasure` is explicit domain language and keeps code readable while still presenting "UoM" in labels, route summaries, and validation messages.

**Alternatives considered**:

- Name the aggregate `Uom`: rejected because the codebase uses descriptive domain names such as `StockKeepingUnit`.
- Model base and alternate units now: rejected because conversions and base-unit relationships are explicit non-goals.

## Decision: Keep UoM fields to code, name, optional symbol, active state, and timestamps

**Rationale**: Issue #36 asks for a narrow reference-data entity. `Symbol` is useful as a short display label and fits the current reference-data shape cleanly without introducing conversion meaning. Use the shared WMS code length for symbol validation to avoid adding a new text-length convention.

**Alternatives considered**:

- Add description: rejected because the issue proposes symbol rather than description and UoM needs a compact display label.
- Add conversion factor, precision, dimension, or base unit: rejected as conversion/base-unit scope.
- Add SKU binding or packaging relationship: rejected by explicit non-goals.

## Decision: Store normalized UoM code directly in `Code`

**Rationale**: Existing Topology and Catalog/SKU code normalizes business codes before storing them in `Code`, then protects uniqueness on that stored value. UoM should match the pattern.

**Alternatives considered**:

- Add `NormalizedCode`: rejected because issue #36 explicitly forbids a separate normalized field.
- Preserve user-entered code casing separately: rejected because the MVP does not require display-preserved codes.

## Decision: Leave `UpdatedAtUtc` null on create

**Rationale**: Existing domain base behavior and Catalog/SKU leave `UpdatedAtUtc` null until details or lifecycle changes occur.

**Alternatives considered**:

- Set `UpdatedAtUtc` equal to `CreatedAtUtc`: rejected because it diverges from existing entity lifecycle semantics.
- Hide update timestamp from UoM details: rejected because existing reference-data details expose it consistently.

## Decision: Use existing `AggregateRoot`/`EntityBase` patterns

**Rationale**: The current WMS domain uses `AggregateRoot` for domain events and `EntityBase` for identity, timestamps, and active state. UoM does not justify a new base type.

**Alternatives considered**:

- Create a Catalog reference-data base class: rejected because this repeated slice does not need a new abstraction.
- Introduce a new domain base entity: rejected as broad architecture work outside issue #36.

## Decision: Emit UoM domain events only for real changes

**Rationale**: SKU and Topology aggregates emit create, details-updated, deactivated, and reactivated events only when state actually changes. Idempotent no-op lifecycle calls should not produce lifecycle events.

**Alternatives considered**:

- Omit UoM domain events: rejected because the representative slices use comparable events.
- Emit events for idempotent no-ops: rejected because it would misrepresent a transition.

## Decision: Implement the same command/query set as SKU

**Rationale**: Create, list, get by id, update details, deactivate, and reactivate match all user stories and the established reference-data vertical-slice pattern.

**Alternatives considered**:

- Create/list only: rejected because maintenance and lifecycle user stories are in scope.
- Add GetByCode: rejected because list/search and get-by-id satisfy the MVP.
- Add import/export: rejected as non-MVP.

## Decision: Add EF Core mapping with `units_of_measure` and a unique code index

**Rationale**: UoM needs durable reference data under the existing `wms` schema, with uniqueness protected by both handler checks and persistence.

**Alternatives considered**:

- Store UoMs in the SKU table: rejected because UoM is separate reference data.
- Rely only on handler duplicate checks: rejected because persistence must protect uniqueness under concurrent writes.

## Decision: Expose UoM endpoints under `/api/wms/catalog/uoms`

**Rationale**: This keeps UoM with the existing Catalog route group and mirrors the SKU endpoint style.

**Alternatives considered**:

- Use `/api/wms/uoms`: rejected because Catalog is the established WMS reference-data capability.
- Nest UoMs under SKUs: rejected because SKU-to-UoM binding is out of scope.

## Decision: Extend the existing `WmsCatalogApiClient`

**Rationale**: SKU already established a Catalog client with read/load exception behavior and write/action `ApiResult<T>` behavior. UoM should add methods to that client and reuse the same local support types.

**Alternatives considered**:

- Create a separate UoM API client: rejected because it would fragment one Catalog client without adding value.
- Move API result/exception support to shared infrastructure: rejected as a broader refactor.
- Rewrite Topology API client support types: rejected because issue #36 must not affect Topology behavior.

## Decision: Build a minimal UoM page that mirrors SKU UI composition

**Rationale**: SKU already provides the page, filters, grid, edit dialog, snackbar, reload, and lifecycle-action pattern. UoM needs the same shape with code, name, and symbol.

**Alternatives considered**:

- Build a richer unit management UI: rejected as non-MVP.
- Add conversion or SKU-binding controls: rejected by explicit non-goals.

## Decision: Limit UoM list sorting to `code`, `name`, and `isActive`

**Rationale**: Current Catalog/SKU implementation supports `code`, `name`, and `isActive`; issue #36 specifically requires provider-safe sorting and forbids created/updated timestamp sorting, provider-specific branching, and `AsEnumerable()` sorting workarounds.

**Alternatives considered**:

- Add `createdAtUtc` and `updatedAtUtc`: rejected because issue #36 explicitly says not to add date sorting.
- Branch by database provider: rejected because provider-specific branching is out of scope.
- Sort in memory after `AsEnumerable()`: rejected because issue #36 explicitly forbids that workaround.

## Decision: Apply focused repeated-reference-data tests

**Rationale**: Issue #34 says repeated reference-data slices should add targeted tests for genuinely new behavior and avoid copying the full representative SKU matrix. UoM introduces a new entity and persistence mapping, but not a new client/error/UI pattern.

**Alternatives considered**:

- Duplicate every SKU test: rejected because UoM follows the representative pattern.
- Use manual testing only: rejected because domain invariants, handlers, persistence mappings, and API clients changed.
- Add endpoint/UI automation infrastructure: rejected as disproportionate to this repeated slice.

## Decision: Keep diagnostics inside existing error/result conventions

**Rationale**: UoM failures should be distinguishable through current validation errors, conflict/not-found errors, ProblemDetails mapping, service results, `ApiResult<T>`, and API exceptions. No new logging, telemetry, observability, or diagnostics infrastructure is needed.

**Alternatives considered**:

- Add new logging or telemetry for UoM: rejected because issue #36 only needs a repeated reference-data slice.
- Create new error/result shapes: rejected because Myrmex already has accepted conventions.

## Clarification Status

All planning unknowns are resolved. No unresolved clarification markers remain.
