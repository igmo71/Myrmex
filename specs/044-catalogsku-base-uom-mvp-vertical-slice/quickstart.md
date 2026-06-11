# Quickstart: Validate Catalog/SKU Base UoM MVP Plan and Implementation

Use this guide to validate that issue #44 remains a small required SKU Base UoM binding increment and proves the expected behavior after implementation.

## Prerequisites

- Run commands from the repository root.
- Stay on branch `044-catalogsku-base-uom-mvp-vertical-slice`.
- Read `specs/044-catalogsku-base-uom-mvp-vertical-slice/spec.md` and `specs/044-catalogsku-base-uom-mvp-vertical-slice/plan.md` before implementation.
- Build, test, application startup, database update, EF migration generation, and EF migration application are developer-controlled. Do not run those commands automatically from the planning workflow.

## 1. Confirm Planning Artifacts Exist

```powershell
Test-Path specs\044-catalogsku-base-uom-mvp-vertical-slice\plan.md
Test-Path specs\044-catalogsku-base-uom-mvp-vertical-slice\research.md
Test-Path specs\044-catalogsku-base-uom-mvp-vertical-slice\data-model.md
Test-Path specs\044-catalogsku-base-uom-mvp-vertical-slice\contracts\catalog-sku-base-uom-api-contract.md
Test-Path specs\044-catalogsku-base-uom-mvp-vertical-slice\quickstart.md
```

Expected outcome: every command returns `True`.

## 2. Confirm No Clarification Markers Remain

```powershell
$marker = "NEEDS " + "CLARIFICATION"
rg -n $marker specs\044-catalogsku-base-uom-mvp-vertical-slice
```

Expected outcome: no matches.

## 3. Confirm Scope Boundaries

```powershell
rg -n "Alternative UoM|conversion|Packaging|Inventory|Receiving|LPN|Picking|Shipping|seed|demo|Integration|new UI|navigation|dialogs|grids" specs\044-catalogsku-base-uom-mvp-vertical-slice
```

Expected outcome: matches appear only as explicit exclusions, rejected alternatives, or future-consideration guardrails.

## 4. Recommended Build Command

Developer-controlled command:

```powershell
dotnet build Myrmex.slnx -nologo -v:minimal
```

Expected outcome: build succeeds without new warnings caused by SKU Base UoM changes.

## 5. Recommended Focused SKU Base UoM Tests

Developer-controlled command:

```powershell
dotnet test Myrmex.Tests\Myrmex.Tests.csproj --filter "FullyQualifiedName~StockKeepingUnit|FullyQualifiedName~StockKeepingUnits|FullyQualifiedName~WmsCatalogApiClient" -nologo -v:minimal
```

Expected outcome:

- SKU domain tests cover required base UoM identity.
- SKU create/update handler tests cover missing, nonexistent, inactive, and valid base UoM assignments.
- SKU get/list handler tests cover `BaseUnitOfMeasureId` projection.
- SKU persistence tests cover the required UoM relationship and index.
- Catalog API client tests cover updated SKU request and response contracts.

## 6. Recommended Full Regression Tests

Developer-controlled command:

```powershell
dotnet test Myrmex.Tests\Myrmex.Tests.csproj -nologo -v:minimal
```

Expected outcome:

- Existing WMS Topology tests still pass.
- Existing Catalog/SKU tests still pass after expected request/response updates.
- Existing Catalog/UoM tests still pass.
- Existing Catalog/SKU Barcode tests still pass.
- New focused Catalog/SKU Base UoM tests pass.

## 7. Developer-Controlled Migration Generation

Migration generation is expected after implementation because the feature adds required `BaseUnitOfMeasureId` to `wms.stock_keeping_units`.

Developer-controlled command:

```powershell
dotnet ef migrations add AddStockKeepingUnitBaseUnitOfMeasure --project Myrmex.Modules.Wms\Myrmex.Modules.Wms.csproj --startup-project Myrmex.ApiService\Myrmex.ApiService.csproj --context WmsDbContext --output-dir Infrastructure\Persistence\Migrations
```

Expected generated artifacts:

- `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/*_AddStockKeepingUnitBaseUnitOfMeasure.cs`
- `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/*_AddStockKeepingUnitBaseUnitOfMeasure.Designer.cs`
- Updated `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/WmsDbContextModelSnapshot.cs`

## 8. Developer-Controlled Database Update

Developer-controlled command:

```powershell
dotnet ef database update --project Myrmex.Modules.Wms\Myrmex.Modules.Wms.csproj --startup-project Myrmex.ApiService\Myrmex.ApiService.csproj --context WmsDbContext
```

Expected outcome: database schema includes required SKU Base UoM relationship after the developer applies the migration.

## 9. Verify Persistence Migration

Review the generated migration and model snapshot.

Expected outcome:

- Adds required `BaseUnitOfMeasureId` to `wms.stock_keeping_units`.
- Adds a foreign key from `BaseUnitOfMeasureId` to `wms.units_of_measure.Id`.
- Adds an index on `BaseUnitOfMeasureId`.
- Preserves the existing SKU `Code` unique index.
- Does not add nullable transition scaffolding for production data preservation.
- Does not add conversion, alternative UoM, packaging, inventory, receiving, LPN, picking, shipping, seed/demo, or integration tables.

## 10. Manual API Behavior Check

After the developer starts the application in the normal local development flow, verify:

1. Create an active UoM such as `EA`.
2. Create SKU `ITEM-001` with `baseUnitOfMeasureId` set to the active UoM id.
3. Confirm the create response includes that `baseUnitOfMeasureId`.
4. Create SKU without `baseUnitOfMeasureId` and confirm field-specific validation feedback.
5. Create SKU with a nonexistent UoM id and confirm missing-UoM feedback.
6. Deactivate a second UoM, try to create a SKU with that inactive UoM id, and confirm inactive-UoM feedback.
7. Get SKU `ITEM-001` by id and confirm `baseUnitOfMeasureId` is returned.
8. List SKUs and confirm every returned SKU includes `baseUnitOfMeasureId`.
9. Update SKU `ITEM-001` to another active UoM id and confirm get/list return the new id.
10. Confirm existing SKU duplicate-code, deactivate, reactivate, include-inactive, and not-found behavior still matches the existing Catalog/SKU contract.
11. Confirm existing UoM and SKU Barcode behavior still matches their contracts.

Expected outcome: all behaviors match `contracts/catalog-sku-base-uom-api-contract.md`.

## 11. Verify No UI Phase Was Added

Review the final diff.

Expected outcome:

- No new Blazor pages under `Myrmex.WebApp/Components/Pages/Wms/Catalog`.
- No Catalog navigation changes for SKU Base UoM.
- No new grids, dialogs, filters, forms, or UI component tests for SKU Base UoM.
- Existing SKU UI is touched only if required for compile-time request/response compatibility.

## 12. Verify No Conversion or Operational Scope Was Added

Review the final diff.

Expected outcome:

- No alternative UoM model.
- No conversion-factor model.
- No packaging model.
- No inventory quantity behavior.
- No receiving, LPN, picking, or shipping behavior.
- No seed or demo data changes.
- No external integration behavior.
- Existing Catalog/SKU, Catalog/UoM, and Catalog/SKU Barcode behavior was not reworked except where shared SKU contracts must compile.
