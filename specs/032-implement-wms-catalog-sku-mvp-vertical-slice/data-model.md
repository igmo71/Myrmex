# Data Model: WMS Catalog/SKU MVP Vertical Slice

## StockKeepingUnit

**Purpose**: Represents a WMS catalog item reference that future fulfillment workflows can identify by SKU code.

**User-facing name**: SKU.

**Domain base pattern**: Uses the existing `AggregateRoot`/`EntityBase` pattern for identity, timestamps, active state, and domain events. The MVP must not introduce a new domain base type or reference `Myrmex.Core\Domain\Entity.cs`.

**Fields**:

- `Id`: Stable system identity.
- `Code`: Required SKU business code. Normalized for casing and surrounding whitespace, stored directly as the duplicate-protected value, and globally unique within the WMS catalog.
- `Name`: Required display name.
- `Description`: Optional descriptive text.
- `IsActive`: Lifecycle flag. New SKUs start active.
- `CreatedAtUtc`: Creation timestamp.
- `UpdatedAtUtc`: Null on create; set only when details or lifecycle change.
- `DomainEvents`: In-memory domain event collection ignored by persistence.

**Validation Rules**:

- `Code` is required.
- `Code` must not exceed the shared WMS code length.
- `Name` is required.
- `Name` must not exceed the shared WMS name length.
- `Description`, when present, must not exceed the shared WMS description length.
- `Code` cannot be changed through the MVP detail update flow.
- Duplicate codes are rejected after normalization.
- A separate `NormalizedCode` field is not part of the MVP.

**State Transitions**:

```text
Create valid SKU -> Active
Active -> Deactivate -> Inactive
Inactive -> Reactivate -> Active
Active -> Reactivate -> Active (idempotent, no lifecycle event)
Inactive -> Deactivate -> Inactive (idempotent, no lifecycle event)
Active or Inactive -> Update Details -> same lifecycle state
```

**Domain Events**:

- `StockKeepingUnitCreatedDomainEvent`
- `StockKeepingUnitDetailsUpdatedDomainEvent`
- `StockKeepingUnitDeactivatedDomainEvent`
- `StockKeepingUnitReactivatedDomainEvent`

Events are emitted only when the matching state change occurs. No lifecycle event is emitted for an idempotent no-op deactivate or reactivate call.

**Relationships**:

- No warehouse relationship in this MVP.
- No inventory balance relationship in this MVP.
- No barcode, UoM, packaging, receiving, LPN, picking, shipping, or integration relationship in this MVP.

## StockKeepingUnitDetails

**Purpose**: Read model returned by handlers, API endpoints, API client, and UI.

**Fields**:

- `Id`
- `Code`
- `Name`
- `Description`
- `IsActive`
- `CreatedAtUtc`
- `UpdatedAtUtc`

**Projection Rules**:

- Must be constructible from a `StockKeepingUnit` aggregate.
- Must support query projection for list and get operations.

## CreateStockKeepingUnit Command

**Purpose**: Creates one SKU reference record.

**Inputs**:

- `Code`
- `Name`
- `Description`

**Result**:

- Success returns `StockKeepingUnitDetails`.
- Invalid input returns field-specific validation errors.
- Duplicate code returns a conflict error on `code`.
- Unexpected persistence failure returns a failure error.

## UpdateStockKeepingUnitDetails Command

**Purpose**: Changes SKU descriptive details without changing the SKU code.

**Inputs**:

- `StockKeepingUnitId`
- `Name`
- `Description`

**Result**:

- Success returns updated `StockKeepingUnitDetails`.
- Missing SKU returns not found.
- Invalid details return field-specific validation errors.

## DeactivateStockKeepingUnit Command

**Purpose**: Marks an existing SKU inactive.

**Inputs**:

- `StockKeepingUnitId`

**Result**:

- Success returns current `StockKeepingUnitDetails`.
- Missing SKU returns not found.
- Repeating the command for an inactive SKU succeeds without a new lifecycle change.

## ReactivateStockKeepingUnit Command

**Purpose**: Marks an existing SKU active.

**Inputs**:

- `StockKeepingUnitId`

**Result**:

- Success returns current `StockKeepingUnitDetails`.
- Missing SKU returns not found.
- Repeating the command for an active SKU succeeds without a new lifecycle change.

## GetStockKeepingUnitById Query

**Purpose**: Retrieves one SKU by system identity.

**Inputs**:

- `StockKeepingUnitId`

**Result**:

- Existing active or inactive SKU returns `StockKeepingUnitDetails`.
- Missing SKU returns not found.

## ListStockKeepingUnits Query

**Purpose**: Lists SKU reference data for catalog review and selection.

**Inputs**:

- `Skip`
- `Take`
- `SearchText`
- `SortBy`
- `SortDescending`
- `IncludeInactive`

**Result**:

- Returns bounded items plus total count, skip, and take.
- Default behavior excludes inactive SKUs.
- Search matches code, name, and description.
- Supported sorting includes code, name, created timestamp, updated timestamp, and active state, matching existing Warehouse/Zone list handler patterns.
- Unknown or unsupported sort fields fall back to code ordering.

## Persistence Shape

**Table**: `wms.stock_keeping_units`

**Columns**:

- `Id`
- `Code`
- `Name`
- `Description`
- `CreatedAtUtc`
- `UpdatedAtUtc`
- `IsActive`

**Indexes and Constraints**:

- Primary key on `Id`.
- Unique index on `Code`.
- Required columns for `Code`, `Name`, `CreatedAtUtc`, and `IsActive`.
- Length constraints aligned with the domain model.
- No `NormalizedCode` column.

## Out of Scope Data

The MVP must not add data model fields, tables, relationships, or reference records for:

- Inventory quantities or availability.
- Barcodes or alternate identifiers.
- Units of measure or conversions.
- Packaging hierarchy.
- Receiving records.
- LPN contents.
- Picking work.
- Shipping records.
- External integration messages.
