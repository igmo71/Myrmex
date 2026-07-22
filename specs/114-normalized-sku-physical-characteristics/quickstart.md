# Quickstart: Validate Normalized SKU Physical Characteristics

This guide is for implementation and acceptance after the planned changes exist. It uses the existing 1C synchronization, WMS API, database migration workflow, and SKU edit dialog. It adds no test infrastructure or performance work.

## 1. Complete the Early Formula Prerequisite

Before coding the normalizer:

1. Through approved read-only 1C access, select additional linked SKU and `Catalog_УпаковкиЕдиницыИзмерения` records. One or more representative non-unity factors are sufficient; all four measurement types are not required.
2. Record the source numerator/denominator, referenced unit numerator/denominator, unit measurement type, and independently understood canonical result.
3. Calculate `source numerator / source denominator × unit numerator / unit denominator`.
4. Confirm the result, exact numeric field types, compatibility with ordinary nullable numeric DTO properties in the existing OData transport, type discriminators, and whether planned `decimal(28,12)` persistence preserves the verified values.
5. If any result contradicts the planned rule, update `research.md`, `plan.md`, and the contracts before formula-dependent implementation continues.

Do not commit credentials or raw source dumps.

## 2. Implement Through Existing Paths

Use the boundaries defined in:

- [data-model.md](data-model.md)
- [1C normalization contract](contracts/onec-normalization-contract.md)
- [SKU details/UI contract](contracts/sku-details-ui-contract.md)

Generate one normal WMS migration for the four nullable SKU columns using the repository's existing migration command. Do not add integration persistence.

Use `decimal(28,12)` as the planned mapping unless the early representative-record verification demonstrates that different precision or scale is required. Any adjustment must remain four ordinary nullable decimal columns.

After the entity and EF configuration are complete, the user generates the migration from the repository root:

```powershell
dotnet ef migrations add AddSkuPhysicalCharacteristics --project Myrmex.Modules.Wms --startup-project Myrmex.ApiService --context WmsDbContext --output-dir Infrastructure/Persistence/Migrations
```

Migration generation and application are user-owned actions. After generation, pause before application so the generated migration and `WmsDbContextModelSnapshot.cs` can be reviewed.

## 3. Build and Start the Existing Application

Run these only after implementation:

```powershell
dotnet build Myrmex.slnx -nologo
dotnet ef database update --project Myrmex.Modules.Wms --startup-project Myrmex.ApiService --context WmsDbContext
dotnet run --project Myrmex.AppHost
```

No test command is listed because the repository has no tracked test project and this feature must not add testing infrastructure.

## 4. Trigger the Existing SKU Synchronization

Use either the existing 1C integration page or the authenticated existing endpoint:

```text
POST /api/integrations/1c/skus/import
```

Do not create another import route or workflow.

## 5. Verify Normalization and Display

For an imported SKU, read the existing endpoint:

```text
GET /api/wms/catalog/skus/{stockKeepingUnitId}
```

Then open the existing SKU Edit action in `/wms/catalog/skus`.

Verify:

- valid values match the expected kg, m, m², and m³ calculations;
- the API returns null for absent values and numeric zero for known zero;
- the edit dialog shows the same values with fixed unit labels;
- values are text-only and cannot be edited;
- the SKU grid and lookup surfaces have no new characteristic columns or controls.

## 6. Verify Independent Failure Handling

Use a record where one enabled characteristic is unresolvable and another is valid.

Expected outcomes:

- the SKU is still created or updated through the existing flow;
- the unresolvable characteristic is null;
- the valid characteristic is stored and displayed;
- the normalizer returns one structured issue for the unresolvable characteristic and the existing synchronization caller logs it exactly once;
- no new diagnostics screen, table, or workflow exists.

For a transport-level failure while reading SKU or unit data, verify that the existing operation reports failure/incompleteness and does not clear previously stored characteristics.

Also verify the separate zero rules:

- source numerator zero with a valid unit produces stored and displayed numeric zero;
- measurement-unit numerator zero makes only that characteristic unresolved and produces one caller-owned warning;
- source or unit denominator zero makes only that characteristic unresolved and produces one caller-owned warning.

## 7. Verify Refresh and Clearing

Synchronize an SKU, then change one valid source value, disable another characteristic, and make a third individually unresolvable. Synchronize again.

Expected outcomes:

- the changed value is replaced;
- the disabled and unresolvable values become null;
- remaining valid values stay available;
- the refresh occurs even when a changed unit factor leaves the SKU's own source data version unchanged;
- repeating unchanged data produces no visible value change.

## 8. Confirm Scope Containment

Confirm the implementation added none of the following:

- packaging levels or dimensions;
- a generalized measurement/unit-conversion framework;
- a custom resilient OData parser, generalized numeric-token wrapper, or shared parsing framework;
- a persistent integration cache;
- a diagnostics subsystem or new diagnostics workflow;
- a separate import path;
- editable physical-characteristic requests;
- a new SKU screen;
- SKU grid or lookup presentation;
- test infrastructure, performance benchmarks, load tests, or baseline work.
