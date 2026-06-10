# Research: Catalog/SKU Barcode MVP Vertical Slice

## Decision: Implement `SkuBarcode` as a concrete Catalog aggregate

**Rationale**: Issue #42 is limited to assigning barcode values to existing SKUs. A concrete `SkuBarcode` aggregate with a required `StockKeepingUnitId` relationship keeps the domain language specific and follows the existing Catalog/SKU and Catalog/UoM vertical-slice style.

**Alternatives considered**:

- Generic `Barcode` table with OwnerType/OwnerId: rejected because the issue explicitly forbids a generic barcode ownership model.
- Barcode module: rejected because SKU barcode master data belongs in the existing WMS Catalog capability for this MVP.
- `IHasBarcodes` abstraction: rejected because future barcode-bearing entities such as StorageLocation or LPN do not justify a generic abstraction now.
- Child collection only on `StockKeepingUnit`: rejected because SKU barcodes have their own identity, lifecycle, list/get/update operations, and persistence mapping.

## Decision: Use `BarcodeSymbology` and `Symbology`

**Rationale**: Symbology is the accurate barcode-domain term for the barcode format. The constrained value set is carried on `SkuBarcode` and covers Unknown, Ean13, Ean8, UpcA, Code128, QrCode, and Internal.

**Alternatives considered**:

- `Type`: rejected as ambiguous after stakeholder terminology refinement.
- BarcodeType reference-data table: rejected because symbology values are constrained values in this MVP, not user-managed reference data.
- Free-form symbology strings: rejected because invalid values should be caught through validation and tests.

## Decision: Keep SKU barcode fields to SKU relationship, value, symbology, primary state, active state, and timestamps

**Rationale**: The spec requires master data only. The fields support assignment, lookup, primary selection, and lifecycle while avoiding operational barcode behavior.

**Alternatives considered**:

- Add scan/print/label fields: rejected by explicit non-goals.
- Add packaging, UoM conversion, inventory, receiving, LPN, picking, or shipping relationships: rejected because those workflows are outside the MVP.
- Add shared barcode primitive tables now: rejected because this issue must avoid blocking future reuse without creating a premature generic model.

## Decision: Store trimmed barcode value directly in `Value`

**Rationale**: The spec defines normalization as leading/trailing whitespace trimming only. Storing the result directly in `Value` matches the explicit rule and avoids a redundant `NormalizedValue` field.

**Alternatives considered**:

- Add `NormalizedValue`: rejected by the spec.
- Uppercase or lowercase values: rejected because some barcode formats may be case-sensitive.
- Preserve raw entered value separately: rejected because the MVP does not require raw input audit or display.

## Decision: Enforce case-sensitive barcode value uniqueness after trimming

**Rationale**: The clarification states that `abc` and `ABC` may coexist. Duplicate checks must compare the stored trimmed `Value` case-sensitively. Persistence must protect the same invariant with a unique index and case-sensitive value comparison/collation appropriate for the configured provider.

**Alternatives considered**:

- Case-insensitive uniqueness: rejected by clarification and case-sensitive barcode format concerns.
- Symbology-dependent uniqueness: rejected as more complex than the MVP needs.
- Handler-only uniqueness: rejected because persistence should protect uniqueness under concurrent writes.

## Decision: Explicit primary selection clears other active primary barcodes

**Rationale**: When a create or update explicitly sets `IsPrimary = true`, the requested active barcode becomes the SKU default and other active barcodes for the same SKU must have `IsPrimary` cleared. This gives users one direct operation to choose a default while preserving the invariant.

**Alternatives considered**:

- Reject if another active primary exists: rejected by clarification.
- Allow multiple active primaries and resolve at read time: rejected because the spec requires at most one active primary in validated results.
- Add a provider-specific filtered unique primary index as the main mechanism: rejected for this MVP because handler-driven reassignment is clearer and avoids provider-specific branching. A non-unique supporting index may still be useful.

## Decision: Lifecycle operations never choose a new default barcode

**Rationale**: The final lifecycle clarification requires deactivate/reactivate to remain lifecycle operations, not default-selection operations. Deactivating a primary barcode clears its own `IsPrimary`, does not promote another barcode, and may leave the SKU with zero active primaries. Reactivation sets `IsActive = true` and leaves the barcode non-primary until explicitly updated.

**Alternatives considered**:

- Automatically promote another active barcode on deactivation: rejected by clarification.
- Restore primary status on reactivation: rejected by clarification.
- Make deactivate fail when the barcode is primary: rejected because lifecycle operations should still be simple and explicit.

## Decision: Use existing `AggregateRoot`/`EntityBase` patterns

**Rationale**: Existing Catalog aggregates use current WMS domain base patterns for identity, timestamps, active state, and domain events. SKU barcode does not justify a new base class.

**Alternatives considered**:

- Create a Catalog reference-data base class: rejected because this slice does not need a new abstraction.
- Create a barcode-specific base class: rejected as premature generalization.
- Introduce a new domain base entity: rejected as broad architecture work outside issue #42.

## Decision: Emit SKU barcode domain events only for real changes

**Rationale**: SKU and UoM emit create, details-updated, deactivated, and reactivated events only when state changes occur. SKU barcode should match that behavior. Primary reassignment caused by explicit create/update is part of the same real change.

**Alternatives considered**:

- Omit events entirely: rejected because comparable Catalog aggregates use events.
- Emit events for idempotent lifecycle no-ops: rejected because no state transition occurred.
- Add separate promoted/demoted events for every primary reassignment: rejected because no current handler or operational requirement needs that detail.

## Decision: Implement the Catalog SKU barcode command/query set

**Rationale**: Create, list, get by id, update details, deactivate, and reactivate match the user stories and the existing reference-data slice shape.

**Alternatives considered**:

- Create/list only: rejected because maintenance and lifecycle stories are in scope.
- Add GetByValue: rejected because list/search and get-by-id satisfy the MVP.
- Add bulk import/export: rejected as non-MVP.

## Decision: Add EF Core mapping with `sku_barcodes`

**Rationale**: SKU barcode records need durable Catalog master data, a required relationship to `StockKeepingUnit`, a unique case-sensitive `Value`, a `StockKeepingUnitId` index, and string persisted symbology. The table belongs under the existing `wms` schema and `WmsDbContext`.

**Alternatives considered**:

- Store barcode values in `stock_keeping_units`: rejected because a SKU may have multiple barcodes.
- Add a separate BarcodeType table: rejected by scope.
- Rely only on application checks for value uniqueness: rejected because persistence should protect the invariant.

## Decision: Expose endpoints under `/api/wms/catalog/sku-barcodes`

**Rationale**: A SKU-specific route avoids generic barcode ownership while supporting list, get, create, update, deactivate, and reactivate operations. `stockKeepingUnitId` is a create payload field and list filter, not a generic owner field.

**Alternatives considered**:

- `/api/wms/catalog/barcodes`: rejected because it sounds generic and may imply non-SKU ownership.
- Nested-only `/api/wms/catalog/skus/{id}/barcodes`: rejected because get/update/deactivate/reactivate operate on barcode identity and list supports optional SKU filtering.
- `/api/wms/barcodes`: rejected because Catalog owns this master-data slice.

## Decision: Extend Catalog API client support without UI screens

**Rationale**: Existing Catalog work has a typed client and shared WMS API primitives. If client support is included, it should add SKU barcode DTOs and methods to `WmsCatalogApiClient` using existing read/load and write/action behavior. This is not a UI implementation and must not add Blazor pages or navigation.

**Alternatives considered**:

- Build UI pages now: rejected by the user request and spec.
- Create a separate barcode API client: rejected because the existing Catalog client is the local pattern.
- Move API helpers to a new shared abstraction: rejected as broad refactoring.

## Decision: Apply focused barcode-specific tests

**Rationale**: This is a repeated Catalog slice but introduces new behavior: SKU relationship validation, trimming-only case-sensitive value uniqueness, constrained symbology, primary reassignment, and lifecycle primary clearing/non-restoration. Tests should target those behaviors without duplicating every SKU/UoM test.

**Alternatives considered**:

- Duplicate the full SKU matrix: rejected because testing guidance prefers focused repeated-slice coverage.
- Manual testing only: rejected because new domain, handler, persistence, and API/client behavior needs automated coverage.
- Add endpoint/UI automation infrastructure: rejected as disproportionate to this slice.

## Decision: Keep diagnostics inside existing error/result conventions

**Rationale**: SKU barcode failures should be distinguishable through current validation errors, conflict/not-found errors, ProblemDetails mapping, service results, `ApiResult<T>`, and API exceptions. No new logging, telemetry, or diagnostics infrastructure is needed.

**Alternatives considered**:

- Add new observability infrastructure: rejected because issue #42 only needs a small master-data slice.
- Create new error/result shapes: rejected because Myrmex already has accepted conventions.

## Clarification Status

All planning unknowns are resolved. No unresolved clarification markers remain.
