# Quickstart: Validate Catalog/SKU Barcode MVP Plan and Implementation

Use this guide to validate that issue #42 remains a small Catalog/SKU Barcode master-data vertical slice and proves the expected behavior after implementation.

## Prerequisites

- Run commands from the repository root.
- Stay on branch `042-catalogsku-barcode-mvp-vertical-slice`.
- Read `specs/042-catalogsku-barcode-mvp-vertical-slice/spec.md` and `specs/042-catalogsku-barcode-mvp-vertical-slice/plan.md` before implementation.
- Build, test, application startup, database update, EF migration generation, and EF migration application are developer-controlled. Do not run those commands automatically from the planning workflow.

## 1. Confirm Planning Artifacts Exist

```powershell
Test-Path specs\042-catalogsku-barcode-mvp-vertical-slice\plan.md
Test-Path specs\042-catalogsku-barcode-mvp-vertical-slice\research.md
Test-Path specs\042-catalogsku-barcode-mvp-vertical-slice\data-model.md
Test-Path specs\042-catalogsku-barcode-mvp-vertical-slice\contracts\catalog-sku-barcode-api-contract.md
Test-Path specs\042-catalogsku-barcode-mvp-vertical-slice\quickstart.md
```

Expected outcome: every command returns `True`.

## 2. Confirm No Clarification Markers Remain

```powershell
$marker = "NEEDS " + "CLARIFICATION"
rg -n $marker specs\042-catalogsku-barcode-mvp-vertical-slice
```

Expected outcome: no matches.

## 3. Confirm Scope Boundaries

```powershell
rg -n "BarcodeType|generic Barcode|OwnerType|OwnerId|IHasBarcodes|Barcode module|scanning|printing|labels|GS1|check digit|Packaging|SKU/UoM|Inventory|Receiving|LPN|Picking|Shipping|Integration|UI pages|navigation|dialogs|grids" specs\042-catalogsku-barcode-mvp-vertical-slice
```

Expected outcome: matches appear only as explicit exclusions, rejected alternatives, or future-consideration guardrails.

## 4. Recommended Build Command

Developer-controlled command:

```powershell
dotnet build Myrmex.slnx -nologo -v:minimal
```

Expected outcome: build succeeds without new warnings caused by Catalog/SKU Barcode changes.

## 5. Recommended Focused SKU Barcode Tests

Developer-controlled command:

```powershell
dotnet test Myrmex.Tests\Myrmex.Tests.csproj --filter "FullyQualifiedName~SkuBarcode|FullyQualifiedName~SkuBarcodes" -nologo -v:minimal
```

Expected outcome:

- SKU barcode domain validation and lifecycle tests pass.
- Focused SKU barcode handler tests pass.
- SKU barcode persistence tests pass.
- SKU barcode Catalog API/client route, DTO, and result wiring tests pass if client support is included.

## 6. Recommended Full Regression Tests

Developer-controlled command:

```powershell
dotnet test Myrmex.Tests\Myrmex.Tests.csproj -nologo -v:minimal
```

Expected outcome:

- Existing WMS Topology tests still pass.
- Existing Catalog/SKU tests still pass.
- Existing Catalog/UoM tests still pass.
- New focused Catalog/SKU Barcode tests pass.

## 7. Developer-Controlled Migration Generation

Migration generation is expected after implementation because the feature adds `wms.sku_barcodes`.

Developer-controlled command:

```powershell
dotnet ef migrations add AddSkuBarcodes --project Myrmex.Modules.Wms\Myrmex.Modules.Wms.csproj --startup-project Myrmex.ApiService\Myrmex.ApiService.csproj --context WmsDbContext --output-dir Infrastructure\Persistence\Migrations
```

Expected generated artifacts:

- `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/*_AddSkuBarcodes.cs`
- `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/*_AddSkuBarcodes.Designer.cs`
- Updated `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/WmsDbContextModelSnapshot.cs`

## 8. Developer-Controlled Database Update

Developer-controlled command:

```powershell
dotnet ef database update --project Myrmex.Modules.Wms\Myrmex.Modules.Wms.csproj --startup-project Myrmex.ApiService\Myrmex.ApiService.csproj --context WmsDbContext
```

Expected outcome: database schema includes the SKU barcode table after the developer applies the migration.

## 9. Verify Persistence Migration

Review the generated migration and model snapshot.

Expected outcome:

- Adds `wms.sku_barcodes`.
- Adds required `StockKeepingUnitId`, `Value`, `Symbology`, `IsPrimary`, `IsActive`, and `CreatedAtUtc` columns.
- Leaves `UpdatedAtUtc` nullable.
- Adds a foreign key from `StockKeepingUnitId` to `wms.stock_keeping_units`.
- Adds a unique case-sensitive constraint or index for `Value`.
- Adds an index on `StockKeepingUnitId`.
- Stores `Symbology` as a string.
- Does not add `NormalizedValue`.
- Does not add BarcodeType, generic Barcode, OwnerType, OwnerId, scanning, printing, labels, packaging, SKU/UoM conversion, inventory, receiving, LPN, picking, shipping, or integration tables.

## 10. Verify Practical Persistence Tests

Review or run the Catalog/SKU Barcode persistence tests.

Expected outcome:

- Tests use the existing SQLite/EnsureCreated WMS test infrastructure.
- Tests verify the `SkuBarcode` mapping/table can be created.
- Tests verify the required SKU relationship.
- Tests verify duplicate trimmed `Value` entries are protected.
- Tests verify case-sensitive uniqueness allows `abc` and `ABC`.
- Tests verify `Symbology` is stored as a string.
- Tests do not require SQL Server-specific migration execution.

## 11. Manual API Behavior Check

After the developer starts the application in the normal local development flow, verify:

1. Create SKU barcode `  AbC-123  ` for an existing SKU with symbology `Code128` succeeds and stores value `AbC-123`.
2. The created barcode returns `updatedAtUtc` as empty/null.
3. Creating the same value with surrounding whitespace fails with duplicate-value feedback.
4. Creating `ABC-123` succeeds when `AbC-123` already exists because uniqueness is case-sensitive.
5. Creating a barcode for a missing SKU returns missing-SKU feedback.
6. Listing SKU barcodes shows active barcodes by default.
7. Filtering by `stockKeepingUnitId` returns only that SKU's barcodes.
8. Including inactive records shows deactivated barcodes.
9. Getting an inactive barcode by id still returns the barcode.
10. Updating value or symbology succeeds and sets `updatedAtUtc`.
11. Updating an active barcode with `isPrimary=true` clears primary status from other active barcodes for the same SKU.
12. Deactivating a primary barcode sets `isActive=false`, clears its `isPrimary`, does not promote another barcode, and may leave zero active primary barcodes.
13. Reactivating a barcode sets `isActive=true` and leaves `isPrimary=false`.
14. Explicitly updating the reactivated barcode with `isPrimary=true` makes it primary and clears other active primary flags.
15. Updating an inactive barcode with `isPrimary=true` fails; reactivation must happen first.

Expected outcome: all behaviors match `contracts/catalog-sku-barcode-api-contract.md`.

## 12. Verify No UI Phase Was Added

Review the final diff.

Expected outcome:

- No new Blazor pages under `Myrmex.WebApp/Components/Pages/Wms/Catalog`.
- No Catalog navigation changes for SKU barcodes.
- No grids, dialogs, filters, forms, or UI component tests for SKU barcodes.
- No UI behavior appears in the API contract.

## 13. Verify No Generic Barcode Abstraction Was Added

Review the final diff.

Expected outcome:

- No generic Barcode table.
- No Barcode module.
- No OwnerType or OwnerId.
- No IHasBarcodes.
- No BarcodeType reference data or CRUD.
- No generic barcode ownership model.
- Existing Catalog/SKU and Catalog/UoM behavior was not reworked except where shared Catalog registration must compile.
