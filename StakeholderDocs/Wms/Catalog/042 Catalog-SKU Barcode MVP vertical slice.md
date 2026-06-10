# Catalog/SKU Barcode MVP vertical slice

## Context

Myrmex WMS already has Catalog MVP vertical slices for:

* `StockKeepingUnit`
* `UnitOfMeasure`

Both slices follow the current Catalog CRUD-style reference-data pattern.

This issue adds the next small Catalog master-data increment: SKU barcodes.

## Goal

Add support for assigning barcode values to existing SKUs.

A `StockKeepingUnit` may have multiple barcodes. Each barcode belongs to exactly one SKU.

This issue is limited to barcode master data under the Catalog capability. It must not introduce scanning, printing, labels, packaging, inventory, receiving, LPN, picking, or shipping behavior.

## Scope

Add a new `SkuBarcode` entity/model with the following suggested shape:

* `Id`
* `StockKeepingUnitId`
* `Value`
* `Type`
* `IsPrimary`
* `IsActive`
* `CreatedAtUtc`
* nullable `UpdatedAtUtc`

Implement:

* create SKU barcode
* list SKU barcodes
* get SKU barcode by id
* update SKU barcode
* deactivate SKU barcode
* reactivate SKU barcode

Listing should support filtering by `StockKeepingUnitId` where practical.

## Barcode type

For this MVP, barcode type is **not** a separate reference-data table.

Use a simple constrained value / enum on `SkuBarcode`, for example:

* `Unknown`
* `Ean13`
* `Ean8`
* `UpcA`
* `Code128`
* `QrCode`
* `Internal`

Suggested domain/API name:

* `SkuBarcodeType`

Suggested persistence:

* store `Type` as a string column, not as an integer
* max length may be small, for example 32

Do not implement CRUD for barcode types in this issue.

## Barcode value normalization

Barcode value must be normalized before persistence.

For this MVP, normalization means:

* trim leading whitespace
* trim trailing whitespace

Do not force uppercase/lowercase normalization because some barcode formats may be case-sensitive.

Store the normalized barcode directly in `Value`.

Do not add a separate `NormalizedValue` column/property.

## Suggested EF persistence

Suggested table:

* `wms.sku_barcodes`

Suggested indexes:

* unique index on `Value`
* index on `StockKeepingUnitId`

Suggested relationship:

* `SkuBarcode.StockKeepingUnitId` references `StockKeepingUnit.Id`

Prefer enforcing that one SKU has at most one active primary barcode, if this can be implemented cleanly within the existing EF/provider conventions.

Suggested rule:

* at most one active `IsPrimary = true` barcode per `StockKeepingUnit`

If this rule complicates the MVP too much, document the trade-off in the plan before implementation.

## Timestamp rules

Follow the existing Catalog timestamp behavior:

* `CreatedAtUtc` is set on creation
* `UpdatedAtUtc` is `null` on creation
* `UpdatedAtUtc` is set only on update/deactivate/reactivate

## Out of scope

Do not implement:

* barcode scanning
* barcode printing
* barcode labels
* GS1 parsing
* barcode check digit validation
* packaging levels
* SKU/UoM conversion
* inventory
* receiving
* LPN
* picking
* shipping
* separate `BarcodeType` reference data
* UI implementation in this phase unless explicitly requested separately

## Workflow constraints

* Follow the existing Catalog/SKU and Catalog/UoM MVP vertical slice patterns.
* Reuse existing WebApp WMS API primitives where relevant.
* Keep this as a small reviewable increment.
* Do not run build, tests, app startup, database update, EF migration generation, or EF migration application automatically.
* If migration work is needed, propose exact developer-controlled commands only.
