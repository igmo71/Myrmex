# Quickstart: Validate WMS Catalog/SKU MVP Plan and Implementation

Use this guide to validate that issue #32 remains a small Catalog/SKU vertical slice and proves the expected behavior after implementation.

## Prerequisites

- Run commands from the repository root.
- Stay on branch `032-implement-wms-catalog-sku-mvp-vertical-slice`.
- Read `specs/032-implement-wms-catalog-sku-mvp-vertical-slice/spec.md` and `specs/032-implement-wms-catalog-sku-mvp-vertical-slice/plan.md` before implementation.

## 1. Confirm Planning Artifacts Exist

```powershell
Test-Path specs\032-implement-wms-catalog-sku-mvp-vertical-slice\plan.md
Test-Path specs\032-implement-wms-catalog-sku-mvp-vertical-slice\research.md
Test-Path specs\032-implement-wms-catalog-sku-mvp-vertical-slice\data-model.md
Test-Path specs\032-implement-wms-catalog-sku-mvp-vertical-slice\contracts\catalog-sku-api-and-ui-contract.md
Test-Path specs\032-implement-wms-catalog-sku-mvp-vertical-slice\quickstart.md
```

Expected outcome: every command returns `True`.

## 2. Confirm No Clarification Markers Remain

```powershell
$marker = "NEEDS " + "CLARIFICATION"
rg -n $marker specs\032-implement-wms-catalog-sku-mvp-vertical-slice
```

Expected outcome: no matches.

## 3. Confirm Scope Boundaries

```powershell
rg -n "Inventory|Barcode|UoM|Packaging|Receiving|LPN|Picking|Shipping|Integration|MediatR|new frameworks|broad refactoring" specs\032-implement-wms-catalog-sku-mvp-vertical-slice
```

Expected outcome: matches appear only as explicit exclusions or rejected alternatives.

## 4. Build the Solution

```powershell
dotnet build Myrmex.slnx -nologo -v:minimal
```

Expected outcome: build succeeds without new warnings caused by Catalog/SKU changes.

## 5. Run Regression Tests

```powershell
dotnet test Myrmex.Tests\Myrmex.Tests.csproj -nologo -v:minimal
```

Expected outcome:

- Existing WMS Topology tests still pass.
- New Catalog/SKU domain, handler, persistence, and API client tests pass.

## 6. Verify Persistence Migration

Review the generated migration and model snapshot.

Expected outcome:

- Adds `wms.stock_keeping_units`.
- Adds required columns for code, name, created timestamp, and active state.
- Adds optional columns for description and updated timestamp.
- Adds a unique index for SKU code.
- Does not add inventory, barcode, UoM, packaging, receiving, LPN, picking, shipping, or integration tables.

## 7. Manual API Behavior Check

After starting the application in the normal local development flow, verify:

1. Create SKU `ITEM-001` with a valid name succeeds.
2. Creating ` item-001 ` again fails with duplicate-code feedback.
3. Listing SKUs shows `ITEM-001` when active.
4. Searching by `ITEM` or the SKU name returns the SKU.
5. Updating name or description succeeds and preserves code.
6. Deactivating hides the SKU from the default list.
7. Including inactive records shows the deactivated SKU.
8. Reactivating makes it appear in the default list again.

Expected outcome: all behaviors match `contracts/catalog-sku-api-and-ui-contract.md`.

## 8. Manual UI Behavior Check

Open `/wms/catalog/skus` in the web app and verify:

- The page loads without affecting WMS Topology pages.
- Create, edit, deactivate, reactivate, refresh, search, and include-inactive controls work.
- The edit dialog does not allow changing SKU code.
- Validation and duplicate-code errors are visible to the user.
- The UI does not show inventory, barcode, UoM, packaging, receiving, LPN, picking, shipping, or integration controls.

Expected outcome: the Catalog/SKU page demonstrates the MVP without scope drift.
