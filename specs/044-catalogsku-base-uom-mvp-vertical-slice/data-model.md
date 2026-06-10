# Data Model: Catalog/SKU Base UoM MVP Vertical Slice

## StockKeepingUnit

**Purpose**: Existing WMS Catalog item reference. This feature adds the required base UoM identity that future quantity workflows will use to express the SKU's base quantity.

**User-facing name**: SKU.

**Domain base pattern**: Continue using the existing `AggregateRoot`/`EntityBase` pattern. Do not introduce a new domain base type or a separate Base UoM aggregate.

**Fields**:

- `Id`: Stable system identity.
- `Code`: Required normalized SKU business code, unchanged by this feature.
- `Name`: Required SKU name, unchanged by this feature.
- `Description`: Optional SKU description, unchanged by this feature.
- `BaseUnitOfMeasureId`: Required identity of the SKU's base Unit of Measure.
- `IsActive`: Existing lifecycle flag. New SKUs start active.
- `CreatedAtUtc`: Creation timestamp.
- `UpdatedAtUtc`: Null on create; set by existing SKU update/lifecycle behavior.
- `DomainEvents`: In-memory domain event collection ignored by persistence.

**Validation Rules**:

- `BaseUnitOfMeasureId` is required for SKU create.
- `BaseUnitOfMeasureId` is required for SKU detail update.
- `BaseUnitOfMeasureId` must not be an empty identity.
- The application handler must verify the referenced UoM exists before saving.
- The application handler must verify the referenced UoM is active at assignment time.
- Existing SKU code, name, description, duplicate-code, and lifecycle validation remains unchanged.

**State Transitions**:

```text
Create valid SKU with active base UoM -> Active SKU with required BaseUnitOfMeasureId
Active or inactive SKU -> Update details with active base UoM -> same lifecycle state with current BaseUnitOfMeasureId
Active SKU -> Deactivate -> Inactive SKU, BaseUnitOfMeasureId retained
Inactive SKU -> Reactivate -> Active SKU, BaseUnitOfMeasureId retained
```

**Domain Events**:

- Continue using existing `StockKeepingUnitCreatedDomainEvent`, `StockKeepingUnitDetailsUpdatedDomainEvent`, `StockKeepingUnitDeactivatedDomainEvent`, and `StockKeepingUnitReactivatedDomainEvent`.
- Creating a SKU with a base UoM still emits the existing created event.
- Updating the base UoM through the SKU detail update flow emits the existing details-updated event when the update succeeds.
- No separate Base UoM assigned domain event is introduced for this MVP.

**Relationships**:

- Required many-to-one relationship from `StockKeepingUnit.BaseUnitOfMeasureId` to `UnitOfMeasure.Id`.
- No relationship to alternative UoM, conversion factor, packaging, inventory, receiving, LPN, picking, shipping, or integration records in this MVP.

## UnitOfMeasure

**Purpose**: Existing Catalog reference data used to express quantities.

**Feature Role**: A UoM may be assigned as a SKU's base UoM when it exists and is active.

**Fields Used by This Feature**:

- `Id`: Stable system identity used as `BaseUnitOfMeasureId`.
- `IsActive`: Assignment eligibility flag at SKU create/update time.

**Behavior Unchanged by This Feature**:

- UoM create, update, list, get, deactivate, and reactivate behavior remains governed by the Catalog/UoM MVP.
- Deactivating a UoM that is already assigned to a SKU does not cascade changes to SKU records in this MVP.

## StockKeepingUnitDetails

**Purpose**: Read model returned by SKU handlers, API endpoints, WebApp API client, and validation scenarios.

**Fields**:

- `Id`
- `Code`
- `Name`
- `Description`
- `BaseUnitOfMeasureId`
- `IsActive`
- `CreatedAtUtc`
- `UpdatedAtUtc`

**Projection Rules**:

- Must be constructible from a `StockKeepingUnit` aggregate.
- Must support query projection for list and get operations.
- Must include `BaseUnitOfMeasureId` for create, update, get, and list results.
- Must not embed full UoM details in this MVP.

## CreateStockKeepingUnit Command

**Purpose**: Creates one SKU with required base UoM assignment.

**Inputs**:

- `Code`
- `Name`
- `Description`
- `BaseUnitOfMeasureId`

**Result**:

- Success returns `StockKeepingUnitDetails` including `BaseUnitOfMeasureId`.
- Missing or empty base UoM identity returns field-specific validation feedback.
- Nonexistent base UoM returns missing-UoM feedback.
- Inactive base UoM returns inactive-UoM feedback.
- Existing SKU validation and duplicate-code failures remain unchanged.
- Unexpected persistence failure returns a failure through existing Myrmex conventions.

## UpdateStockKeepingUnitDetails Command

**Purpose**: Changes SKU name, description, and required base UoM without changing SKU code or lifecycle state.

**Inputs**:

- `StockKeepingUnitId`
- `Name`
- `Description`
- `BaseUnitOfMeasureId`

**Result**:

- Success returns `StockKeepingUnitDetails` including the current `BaseUnitOfMeasureId`.
- Missing SKU returns not found.
- Missing or empty base UoM identity returns field-specific validation feedback.
- Nonexistent base UoM returns missing-UoM feedback.
- Inactive base UoM returns inactive-UoM feedback.
- Existing SKU detail validation failures remain unchanged.

## GetStockKeepingUnitById Query

**Purpose**: Retrieves one SKU by identity.

**Inputs**:

- `StockKeepingUnitId`

**Result**:

- Existing active or inactive SKU returns `StockKeepingUnitDetails` with `BaseUnitOfMeasureId`.
- Missing SKU returns not found.

## ListStockKeepingUnits Query

**Purpose**: Lists SKU reference data for catalog review.

**Inputs**:

- Existing SKU list inputs: `Skip`, `Take`, `SearchText`, `SortBy`, `SortDescending`, `IncludeInactive`.

**Result**:

- Returns bounded items plus total count, skip, and take.
- Each item includes `BaseUnitOfMeasureId`.
- Existing list filtering, search, and sorting behavior remains unchanged.

## Persistence Shape

**Table**: `wms.stock_keeping_units`

**New/Changed Columns**:

- `BaseUnitOfMeasureId`: Required UoM identity column.

**Existing Columns**:

- `Id`
- `Code`
- `Name`
- `Description`
- `IsActive`
- `CreatedAtUtc`
- `UpdatedAtUtc`

**Indexes and Constraints**:

- Existing primary key on `Id`.
- Existing unique index on `Code`.
- Required foreign key from `BaseUnitOfMeasureId` to `wms.units_of_measure.Id`.
- Index on `BaseUnitOfMeasureId`.
- No nullable production-compatibility transition is required for this development-only database.

## Out of Scope Data

The MVP must not add data model fields, tables, relationships, or reference records for:

- Alternative UoMs.
- UoM conversion factors.
- Packaging levels.
- Inventory quantities or availability.
- Receiving records.
- LPN behavior.
- Picking or shipping work.
- Seed or demo data.
- External integration messages.
- Embedded UoM display snapshots on SKU records.
