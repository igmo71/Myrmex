# Data Model: WMS Catalog/UoM MVP Vertical Slice

## UnitOfMeasure

**Purpose**: Represents a WMS catalog reference record for expressing quantities in future fulfillment workflows.

**User-facing name**: UoM.

**Domain base pattern**: Uses the existing `AggregateRoot`/`EntityBase` pattern for identity, timestamps, active state, and domain events. The MVP must not introduce a new domain base type.

**Fields**:

- `Id`: Stable system identity.
- `Code`: Required UoM business code. Normalized for casing and surrounding whitespace, stored directly as the duplicate-protected value, and globally unique within the WMS catalog.
- `Name`: Required display name.
- `Symbol`: Optional short display label. It has no conversion meaning in this MVP.
- `IsActive`: Lifecycle flag. New UoMs start active.
- `CreatedAtUtc`: Creation timestamp.
- `UpdatedAtUtc`: Null on create; set only when details or lifecycle change.
- `DomainEvents`: In-memory domain event collection ignored by persistence.

**Validation Rules**:

- `Code` is required.
- `Code` must not exceed the shared WMS code length.
- `Name` is required.
- `Name` must not exceed the shared WMS name length.
- `Symbol`, when present, must not exceed the shared WMS code length. This keeps symbol bounded without introducing a new text-length convention for the repeated slice.
- `Code` cannot be changed through the MVP detail update flow.
- Duplicate codes are rejected after normalization.
- A separate `NormalizedCode` field is not part of the MVP.

**State Transitions**:

```text
Create valid UoM -> Active
Active -> Deactivate -> Inactive
Inactive -> Reactivate -> Active
Active -> Reactivate -> Active (idempotent, no lifecycle event)
Inactive -> Deactivate -> Inactive (idempotent, no lifecycle event)
Active or Inactive -> Update Details -> same lifecycle state
```

**Domain Events**:

- `UnitOfMeasureCreatedDomainEvent`
- `UnitOfMeasureDetailsUpdatedDomainEvent`
- `UnitOfMeasureDeactivatedDomainEvent`
- `UnitOfMeasureReactivatedDomainEvent`

Events are emitted only when the matching state change occurs. No lifecycle event is emitted for an idempotent no-op deactivate or reactivate call.

**Relationships**:

- No SKU relationship in this MVP.
- No base or alternative UoM relationship in this MVP.
- No conversion relationship in this MVP.
- No packaging, barcode, inventory, receiving, LPN, picking, shipping, or integration relationship in this MVP.

## UnitOfMeasureDetails

**Purpose**: Read model returned by handlers, API endpoints, API client, and UI.

**Fields**:

- `Id`
- `Code`
- `Name`
- `Symbol`
- `IsActive`
- `CreatedAtUtc`
- `UpdatedAtUtc`

**Projection Rules**:

- Must be constructible from a `UnitOfMeasure` aggregate.
- Must support query projection for list and get operations.

## CreateUnitOfMeasure Command

**Purpose**: Creates one UoM reference record.

**Inputs**:

- `Code`
- `Name`
- `Symbol`

**Result**:

- Success returns `UnitOfMeasureDetails`.
- Invalid input returns field-specific validation errors.
- Duplicate code returns a conflict error on `code`.
- Unexpected persistence failure returns a failure error through existing Myrmex conventions.

## UpdateUnitOfMeasureDetails Command

**Purpose**: Changes UoM descriptive details without changing the UoM code.

**Inputs**:

- `UnitOfMeasureId`
- `Name`
- `Symbol`

**Result**:

- Success returns updated `UnitOfMeasureDetails`.
- Missing UoM returns not found.
- Invalid details return field-specific validation errors.

## DeactivateUnitOfMeasure Command

**Purpose**: Marks an existing UoM inactive.

**Inputs**:

- `UnitOfMeasureId`

**Result**:

- Success returns current `UnitOfMeasureDetails`.
- Missing UoM returns not found.
- Repeating the command for an inactive UoM succeeds without a new lifecycle change.

## ReactivateUnitOfMeasure Command

**Purpose**: Marks an existing UoM active.

**Inputs**:

- `UnitOfMeasureId`

**Result**:

- Success returns current `UnitOfMeasureDetails`.
- Missing UoM returns not found.
- Repeating the command for an active UoM succeeds without a new lifecycle change.

## GetUnitOfMeasureById Query

**Purpose**: Retrieves one UoM by system identity.

**Inputs**:

- `UnitOfMeasureId`

**Result**:

- Existing active or inactive UoM returns `UnitOfMeasureDetails`.
- Missing UoM returns not found.

## ListUnitsOfMeasure Query

**Purpose**: Lists UoM reference data for catalog review and selection.

**Inputs**:

- `Skip`
- `Take`
- `SearchText`
- `SortBy`
- `SortDescending`
- `IncludeInactive`

**Result**:

- Returns bounded items plus total count, skip, and take.
- Default behavior excludes inactive UoMs.
- Search matches code, name, and symbol.
- Supported sorting includes `code`, `name`, and `isActive`.
- Unknown or unsupported sort fields fall back to code ordering.
- Created and updated timestamp sorting are not part of the MVP.
- Sorting must remain provider-safe and must not use provider-specific branching or in-memory `AsEnumerable()` ordering workarounds.

## Persistence Shape

**Table**: `wms.units_of_measure`

**Columns**:

- `Id`
- `Code`
- `Name`
- `Symbol`
- `CreatedAtUtc`
- `UpdatedAtUtc`
- `IsActive`

**Indexes and Constraints**:

- Primary key on `Id`.
- Unique index on `Code`.
- Required columns for `Code`, `Name`, `CreatedAtUtc`, and `IsActive`.
- Optional columns for `Symbol` and `UpdatedAtUtc`.
- Length constraints aligned with the domain model.
- No `NormalizedCode` column.

## Out of Scope Data

The MVP must not add data model fields, tables, relationships, or reference records for:

- Conversion rules or factors.
- Base or alternative UoM relationships.
- SKU-to-UoM binding.
- Packaging hierarchy.
- Barcode support.
- Inventory quantities or availability.
- Receiving records.
- LPN behavior.
- Picking or shipping work.
- External integration messages.
