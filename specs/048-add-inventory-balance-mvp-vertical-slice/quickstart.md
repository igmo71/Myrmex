# Quickstart: Validate Inventory Balance MVP Plan and Implementation

Use this guide to validate that issue #48 remains a small current-stock Inventory Balance vertical slice and proves the expected behavior after implementation.

## Prerequisites

- Run commands from the repository root.
- Stay on branch `048-add-inventory-balance-mvp-vertical-slice`.
- Read `specs/048-add-inventory-balance-mvp-vertical-slice/spec.md` and `specs/048-add-inventory-balance-mvp-vertical-slice/plan.md` before implementation.
- Build, test, application startup, database update, EF migration generation, and EF migration application are developer-controlled. Do not run those commands automatically from the planning workflow.

## 1. Confirm Planning Artifacts Exist

```powershell
Test-Path specs\048-add-inventory-balance-mvp-vertical-slice\plan.md
Test-Path specs\048-add-inventory-balance-mvp-vertical-slice\research.md
Test-Path specs\048-add-inventory-balance-mvp-vertical-slice\data-model.md
Test-Path specs\048-add-inventory-balance-mvp-vertical-slice\contracts\inventory-balance-api-contract.md
Test-Path specs\048-add-inventory-balance-mvp-vertical-slice\quickstart.md
```

Expected outcome: every command returns `True`.

## 2. Confirm No Clarification Markers Remain

```powershell
$marker = "NEEDS " + "CLARIFICATION"
rg -n $marker specs\048-add-inventory-balance-mvp-vertical-slice
```

Expected outcome: no matches.

## 3. Confirm Scope Boundaries

```powershell
rg -n "receiving|putaway|picking|shipping|LPN|reservation|transaction|movement|adjustment|conversion|alternative UoM|packaging|cycle counting|seed|demo|integration|WebApp UI|delete|deactivate|reactivate" specs\048-add-inventory-balance-mvp-vertical-slice
```

Expected outcome: matches appear only as explicit exclusions, rejected alternatives, or future-consideration guardrails.

## 4. Recommended Build Command

Developer-controlled command:

```powershell
dotnet build Myrmex.slnx -nologo -v:minimal
```

Expected outcome: build succeeds without new warnings caused by Inventory Balance changes.

## 5. Recommended Focused Inventory Balance Tests

Developer-controlled command:

```powershell
dotnet test Myrmex.Tests\Myrmex.Tests.csproj --filter "FullyQualifiedName~InventoryBalance|FullyQualifiedName~InventoryBalances|FullyQualifiedName~WmsInventoryApiClient" -nologo -v:minimal
```

Expected outcome:

- Inventory Balance domain tests cover required identities, non-negative quantity, zero quantity, and quantity update.
- Create handler tests cover missing SKU, inactive SKU, SKU without base UoM, missing storage location, inactive storage location, inactive storage location type/status, negative quantity, zero quantity, duplicate SKU/location pair, and valid create.
- Get handler tests cover found and not-found behavior with display context.
- List handler tests cover no filters, SKU filter, storage location filter, warehouse filter, and SKU-within-warehouse filter, including zero balances by default.
- Update handler tests cover quantity update, zero quantity update, missing balance, negative quantity rejection, and no SKU/location mutation path.
- Persistence tests cover required FKs, restrict delete behavior where practical, unique SKU/location index, quantity mapping, and timestamps.
- API client tests cover request/response/result contracts.

## 6. Recommended Full Regression Tests

Developer-controlled command:

```powershell
dotnet test Myrmex.Tests\Myrmex.Tests.csproj -nologo -v:minimal
```

Expected outcome:

- Existing WMS Topology tests still pass.
- Existing Catalog/SKU, Catalog/UoM, and Catalog/SKU Barcode tests still pass.
- New focused Inventory Balance tests pass.

## 7. Developer-Controlled Migration Generation

Migration generation is expected after implementation because the feature adds `wms.inventory_balances`.

Developer-controlled command:

```powershell
dotnet ef migrations add AddInventoryBalance --project Myrmex.Modules.Wms\Myrmex.Modules.Wms.csproj --startup-project Myrmex.ApiService\Myrmex.ApiService.csproj --context WmsDbContext --output-dir Infrastructure\Persistence\Migrations
```

Expected generated artifacts:

- `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/*_AddInventoryBalance.cs`
- `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/*_AddInventoryBalance.Designer.cs`
- Updated `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/WmsDbContextModelSnapshot.cs`

## 8. Developer-Controlled Database Update

Developer-controlled command:

```powershell
dotnet ef database update --project Myrmex.Modules.Wms\Myrmex.Modules.Wms.csproj --startup-project Myrmex.ApiService\Myrmex.ApiService.csproj --context WmsDbContext
```

Expected outcome: database schema includes `wms.inventory_balances` after the developer applies the migration.

## 9. Verify Persistence Migration

Review the generated migration and model snapshot.

Expected outcome:

- Adds `wms.inventory_balances`.
- Adds required `StockKeepingUnitId` and `StorageLocationId`.
- Adds decimal `Quantity`.
- Adds `CreatedAtUtc` and nullable `UpdatedAtUtc`.
- Adds foreign keys to `wms.stock_keeping_units.Id` and `wms.storage_locations.Id`.
- Adds a unique index on `(StockKeepingUnitId, StorageLocationId)`.
- Adds an index on `StorageLocationId`.
- Uses delete behavior that prevents accidental deletion of referenced SKU or storage location while balances exist.
- Does not add warehouse or UoM columns to `inventory_balances`.
- Does not add movement, transaction, reservation, adjustment, conversion, seed/demo, integration, delete, or lifecycle tables.

## 10. Manual API Behavior Check

After the developer starts the application in the normal local development flow, verify:

1. Create or locate an active UoM, active SKU with base UoM, active warehouse, active zone, active storage location type/status, and active storage location.
2. Create an inventory balance with quantity `10`.
3. Confirm the create response includes SKU, storage location, warehouse, base UoM, quantity, created timestamp, and null updated timestamp.
4. Try to create a second balance for the same SKU/location pair and confirm duplicate feedback with the existing balance unchanged.
5. Try to create with a negative quantity and confirm field-specific validation feedback.
6. Create or update a balance with quantity `0` and confirm zero is accepted and visible in list results.
7. Try to create with a missing, inactive, or base-UoM-less SKU and confirm clear failure feedback.
8. Try to create with a missing storage location, inactive storage location, inactive storage location type, or inactive storage location status and confirm clear failure feedback.
9. Confirm `IsPickable = false` does not prevent balance creation when all eligibility rules pass.
10. Get the balance by id and confirm full display context.
11. Get a nonexistent balance id and confirm not-found feedback.
12. List without filters and confirm balances are returned with display context.
13. List by SKU, storage location, warehouse, and SKU within warehouse and confirm each result set only contains matching balances.
14. Update quantity to `5` and confirm SKU/location are unchanged and `updatedAtUtc` changes.
15. Confirm update payload accepts only `quantity`; no SKU or storage location fields are part of the contract.

Expected outcome: all behaviors match `contracts/inventory-balance-api-contract.md`.

## 11. Verify No WebApp UI Phase Was Added

Review the final diff.

Expected outcome:

- No new Blazor pages under `Myrmex.WebApp/Components/Pages/Wms/Inventory`.
- No WMS navigation changes for Inventory Balance.
- No new grids, dialogs, filters, forms, or UI component tests for Inventory Balance.
- WebApp changes, if any, are limited to typed API client contracts.

## 12. Verify No Ledger, Conversion, or Operational Scope Was Added

Review the final diff.

Expected outcome:

- No inventory transaction ledger.
- No movement history.
- No receiving, putaway, picking, shipping, LPN, reservations, or adjustments.
- No batch/lot, expiry, serial number, packaging, or cycle counting model.
- No UoM conversion or alternative UoM model.
- No delete, deactivate, reactivate, or zero-balance cleanup behavior.
- No seed or demo data changes.
- No external integration behavior.
- Existing Catalog and Topology behavior was not reworked except where Inventory Balance must reference current records.
