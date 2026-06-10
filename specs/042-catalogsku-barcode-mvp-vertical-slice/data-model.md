# Data Model: Catalog/SKU Barcode MVP Vertical Slice

## SkuBarcode

**Purpose**: Represents a WMS Catalog barcode value assigned to exactly one existing SKU.

**User-facing name**: SKU Barcode.

**Domain base pattern**: Uses the existing `AggregateRoot`/`EntityBase` pattern for identity, timestamps, active state, and domain events. The MVP must not introduce a barcode base class, generic ownership abstraction, or new domain base type.

**Fields**:

- `Id`: Stable system identity.
- `StockKeepingUnitId`: Required relationship to the owning SKU.
- `Value`: Required barcode value. Trimmed for leading/trailing whitespace before storage, stored directly as the duplicate-protected value, casing preserved, globally unique across SKU barcode assignments using case-sensitive comparison.
- `Symbology`: Required constrained `BarcodeSymbology` value representing the barcode format.
- `IsPrimary`: Indicates whether this active barcode is the SKU default barcode.
- `IsActive`: Lifecycle flag. New SKU barcodes start active.
- `CreatedAtUtc`: Creation timestamp.
- `UpdatedAtUtc`: Null on create; set only when details or lifecycle change.
- `DomainEvents`: In-memory domain event collection ignored by persistence.

**Validation Rules**:

- `StockKeepingUnitId` is required and must reference an existing SKU before a barcode can be created.
- `Value` is required after trimming.
- `Value` must not exceed the selected maximum barcode value length.
- Leading and trailing whitespace is trimmed before storing `Value`.
- Casing and internal whitespace in `Value` are preserved.
- Duplicate `Value` entries are rejected using case-sensitive comparison after trimming.
- A separate `NormalizedValue` field is not part of the MVP.
- `Symbology` must be one of the supported `BarcodeSymbology` values.
- `StockKeepingUnitId` cannot be changed through the MVP detail update flow.

**Primary Barcode Rules**:

- At most one active barcode for a SKU may have `IsPrimary = true`.
- Explicit create with `IsPrimary = true` clears `IsPrimary` from other active barcodes for the same SKU.
- Explicit update with `IsPrimary = true` clears `IsPrimary` from other active barcodes for the same SKU.
- Explicit update with `IsPrimary = false` clears only the updated barcode's primary flag.
- Updating an inactive barcode with `IsPrimary = true` is an unsupported primary change; the caller must reactivate the barcode first, then explicitly update it as primary.
- Deactivate/reactivate lifecycle operations do not choose a default barcode.
- Deactivating a primary barcode clears `IsPrimary` on the deactivated barcode and does not promote another barcode.
- Reactivating a barcode leaves it non-primary by default.
- To make a reactivated barcode primary, the user must explicitly update it with `IsPrimary = true`.

**State Transitions**:

```text
Create valid SKU barcode -> Active
Active non-primary -> Deactivate -> Inactive non-primary
Active primary -> Deactivate -> Inactive non-primary, no replacement primary
Inactive -> Reactivate -> Active non-primary
Active -> Reactivate -> Active (idempotent, no lifecycle event)
Inactive -> Deactivate -> Inactive (idempotent, no lifecycle event)
Active or Inactive -> Update Details -> same lifecycle state, with explicit primary-selection rules
```

**Domain Events**:

- `SkuBarcodeCreatedDomainEvent`
- `SkuBarcodeDetailsUpdatedDomainEvent`
- `SkuBarcodeDeactivatedDomainEvent`
- `SkuBarcodeReactivatedDomainEvent`

Events are emitted only when the matching state change occurs. No lifecycle event is emitted for an idempotent no-op deactivate or reactivate call.

**Relationships**:

- Required many-to-one relationship from `SkuBarcode.StockKeepingUnitId` to `StockKeepingUnit.Id`.
- No relationship to UnitOfMeasure in this MVP.
- No relationship to StorageLocation, LPN, inventory, receiving, picking, shipping, packaging, labels, or integration records in this MVP.

## BarcodeSymbology

**Purpose**: Constrained value representing barcode format/symbology on `SkuBarcode`.

**Values**:

- `Unknown`
- `Ean13`
- `Ean8`
- `UpcA`
- `Code128`
- `QrCode`
- `Internal`

**Persistence Rule**:

- Persist `Symbology` as a string value.
- Do not create a BarcodeType or BarcodeSymbology reference-data table.
- Do not expose CRUD behavior for symbology values.

## SkuBarcodeDetails

**Purpose**: Read model returned by handlers, API endpoints, API client, and validation scenarios.

**Fields**:

- `Id`
- `StockKeepingUnitId`
- `Value`
- `Symbology`
- `IsPrimary`
- `IsActive`
- `CreatedAtUtc`
- `UpdatedAtUtc`

**Projection Rules**:

- Must be constructible from a `SkuBarcode` aggregate.
- Must support query projection for list and get operations.
- Must not include a `NormalizedValue` field.
- Must not include generic owner fields such as OwnerType or OwnerId.

## CreateSkuBarcode Command

**Purpose**: Creates one SKU barcode assignment.

**Inputs**:

- `StockKeepingUnitId`
- `Value`
- `Symbology`
- `IsPrimary`

**Result**:

- Success returns `SkuBarcodeDetails`.
- Missing SKU returns not found or validation failure consistent with existing Myrmex conventions.
- Invalid value or symbology returns field-specific validation errors.
- Duplicate case-sensitive value returns a conflict error on `value`.
- When `IsPrimary = true`, other active primary barcodes for the same SKU are cleared.
- Unexpected persistence failure returns a failure error through existing Myrmex conventions.

## UpdateSkuBarcodeDetails Command

**Purpose**: Changes barcode value, symbology, and primary flag without changing the owning SKU.

**Inputs**:

- `SkuBarcodeId`
- `Value`
- `Symbology`
- `IsPrimary`

**Result**:

- Success returns updated `SkuBarcodeDetails`.
- Missing barcode returns not found.
- Invalid value or symbology returns field-specific validation errors.
- Duplicate case-sensitive value returns a conflict error on `value`.
- When `IsPrimary = true` for an active barcode, other active primary barcodes for the same SKU are cleared.
- When `IsPrimary = true` for an inactive barcode, the command returns an unsupported primary-change failure.

## DeactivateSkuBarcode Command

**Purpose**: Marks an existing SKU barcode inactive.

**Inputs**:

- `SkuBarcodeId`

**Result**:

- Success returns current `SkuBarcodeDetails`.
- Missing barcode returns not found.
- Repeating the command for an inactive barcode succeeds without a new lifecycle change.
- If the barcode was primary, the command clears its primary flag and does not promote another barcode.

## ReactivateSkuBarcode Command

**Purpose**: Marks an existing SKU barcode active.

**Inputs**:

- `SkuBarcodeId`

**Result**:

- Success returns current `SkuBarcodeDetails`.
- Missing barcode returns not found.
- Repeating the command for an active barcode succeeds without a new lifecycle change.
- Reactivated barcodes are non-primary by default.

## GetSkuBarcodeById Query

**Purpose**: Retrieves one SKU barcode by system identity.

**Inputs**:

- `SkuBarcodeId`

**Result**:

- Existing active or inactive barcode returns `SkuBarcodeDetails`.
- Missing barcode returns not found.

## ListSkuBarcodes Query

**Purpose**: Lists SKU barcode master data for catalog review and SKU filtering.

**Inputs**:

- `Skip`
- `Take`
- `SearchText`
- `SortBy`
- `SortDescending`
- `IncludeInactive`
- `StockKeepingUnitId`

**Result**:

- Returns bounded items plus total count, skip, and take.
- Default behavior excludes inactive barcodes.
- Optional `StockKeepingUnitId` filters to one SKU's barcode assignments.
- Search matches `Value`.
- Supported sorting includes `value`, `symbology`, and `isActive`.
- Unknown or unsupported sort fields fall back to value ordering.
- Sorting must remain provider-safe and must not use provider-specific branching or in-memory `AsEnumerable()` ordering workarounds.

## Persistence Shape

**Table**: `wms.sku_barcodes`

**Columns**:

- `Id`
- `StockKeepingUnitId`
- `Value`
- `Symbology`
- `IsPrimary`
- `IsActive`
- `CreatedAtUtc`
- `UpdatedAtUtc`

**Indexes and Constraints**:

- Primary key on `Id`.
- Required foreign key from `StockKeepingUnitId` to `wms.stock_keeping_units.Id`.
- Unique index on `Value` with case-sensitive comparison.
- Index on `StockKeepingUnitId`.
- Optional supporting index on `StockKeepingUnitId`, `IsActive`, and `IsPrimary` for primary-barcode operations; do not rely on a provider-specific filtered unique index as the only enforcement mechanism.
- Required columns for `StockKeepingUnitId`, `Value`, `Symbology`, `IsPrimary`, `IsActive`, and `CreatedAtUtc`.
- Optional column for `UpdatedAtUtc`.
- Length constraints aligned with the domain model.
- No `NormalizedValue` column.
- No BarcodeType table.
- No generic Barcode table.
- No OwnerType or OwnerId columns.

## Out of Scope Data

The MVP must not add data model fields, tables, relationships, or reference records for:

- BarcodeType reference data.
- Generic barcode ownership.
- Barcode scanning events.
- Barcode print jobs or labels.
- GS1 parsing output.
- Check digit validation state.
- Packaging hierarchy.
- SKU/UoM conversion.
- Inventory quantities or availability.
- Receiving records.
- LPN behavior.
- Picking or shipping work.
- External integration messages.
