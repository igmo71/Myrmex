# Quickstart: Validate WebApp Inventory Balance Management UI

Use this guide to validate that issue #52 remains a WebApp Inventory Balance management UI over the existing Inventory Balance backend.

## Prerequisites

- Run commands from the repository root.
- Stay on branch `52-add-webapp-inventory-balance-management-ui`.
- Read `specs/052-add-webapp-inventory-balance-management-ui/spec.md` and `specs/052-add-webapp-inventory-balance-management-ui/plan.md` before implementation.
- Build, test, application startup, database update, EF migration generation, EF migration application, and infrastructure commands are developer-controlled. Do not run them automatically from the planning workflow.
- The backend Inventory Balance slice must be available, including the existing API client under `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs`.

## 1. Confirm Planning Artifacts Exist

```powershell
Test-Path specs\052-add-webapp-inventory-balance-management-ui\plan.md
Test-Path specs\052-add-webapp-inventory-balance-management-ui\research.md
Test-Path specs\052-add-webapp-inventory-balance-management-ui\data-model.md
Test-Path specs\052-add-webapp-inventory-balance-management-ui\contracts\inventory-balance-webapp-ui-contract.md
Test-Path specs\052-add-webapp-inventory-balance-management-ui\quickstart.md
```

Expected outcome: every command returns `True`.

## 2. Confirm No Clarification Markers Remain

```powershell
$marker = "NEEDS " + "CLARIFICATION"
rg -n $marker specs\052-add-webapp-inventory-balance-management-ui
```

Expected outcome: no matches.

## 3. Confirm Scope Boundaries

```powershell
rg -n "receiving|putaway|picking|shipping|LPN|batch|lot|expiry|serial|reservation|transaction|movement|adjustment|conversion|packaging|cycle counting|delete|deactivate|reactivate|bulk|import|export|seed|demo|integration|domain redesign|persistence redesign" specs\052-add-webapp-inventory-balance-management-ui
```

Expected outcome: matches appear only as explicit exclusions, rejected alternatives, or validation guardrails.

## 4. Recommended Build Command

Developer-controlled command:

```powershell
dotnet build Myrmex.slnx -nologo -v:minimal
```

Expected outcome: build succeeds without new warnings caused by the WebApp Inventory Balance UI.

## 5. Recommended Focused Tests

Developer-controlled command:

```powershell
dotnet test Myrmex.Tests\Myrmex.Tests.csproj --filter "FullyQualifiedName~WmsInventoryApiClient|FullyQualifiedName~InventoryBalance" -nologo -v:minimal
```

Expected outcome:

- Existing Inventory Balance domain, handler, persistence, and API client tests still pass.
- New or updated WebApp client/registration tests pass if implementation changes Inventory client wiring.
- No new UI test framework is required by this feature.

## 6. Recommended Full Regression Tests

Developer-controlled command:

```powershell
dotnet test Myrmex.Tests\Myrmex.Tests.csproj -nologo -v:minimal
```

Expected outcome:

- Existing WMS Inventory Balance tests still pass.
- Existing WMS Catalog and Topology tests still pass.
- Existing WebApp API client tests still pass.

## 7. Recommended App Startup

Developer-controlled command:

```powershell
dotnet run --project Myrmex.AppHost\Myrmex.AppHost.csproj
```

Expected outcome: the application starts through the normal local development flow, and the WebApp can reach the API service.

## 8. Manual UI Smoke Validation

After the developer starts the application:

1. Open the WebApp.
2. Confirm WMS navigation includes Inventory and an Inventory Balances child page.
3. Open Inventory Balances in no more than 3 navigation interactions.
4. Confirm the page loads without a fatal error.
5. Confirm the grid shows SKU, warehouse, storage location, quantity, and base UoM for existing balances.
6. Confirm zero quantity balances remain visible.
7. Confirm a successful empty result shows an empty state, not an error.
8. Confirm a list/load failure produces a page-level error message when simulated or naturally encountered.

Expected outcome: page visibility behavior matches `contracts/inventory-balance-webapp-ui-contract.md`.

## 9. Manual Filter Validation

1. Confirm the storage location filter is disabled before warehouse selection.
2. Select a warehouse.
3. Confirm storage location choices are scoped to that warehouse.
4. Select a storage location and confirm the list reloads to matching balances.
5. Change warehouse to one that does not contain the selected location.
6. Confirm the storage location selection is cleared.
7. Select a SKU and confirm the list shows where that SKU is stored and in what quantity.
8. Combine SKU and warehouse filters and confirm only matching balances remain.

Expected outcome: filter behavior follows the clarified warehouse-first storage-location rule.

## 10. Manual Create Dialog Validation

1. Open the create dialog.
2. Confirm SKU, warehouse, storage location, quantity, and read-only base UoM context are present.
3. Confirm storage location selection is disabled until warehouse is selected.
4. Select an active SKU and confirm base UoM context appears.
5. Select a warehouse and storage location from that warehouse.
6. Enter quantity `0` and confirm the UI accepts it.
7. Enter a negative quantity and confirm validation rejects it.
8. Create a valid balance and confirm the dialog closes, success feedback appears, and the refreshed list includes the new balance when it matches active filters.
9. Attempt to create a duplicate SKU/location balance and confirm conflict feedback appears.

Expected outcome: create behavior matches the spec and contract without introducing transactions or adjustments.

## 11. Manual Update Quantity Validation

1. Open the update action from a balance row.
2. Confirm SKU, warehouse, storage location, and base UoM are read-only.
3. Confirm quantity is the only editable business field.
4. Enter quantity `0` and confirm the UI accepts it.
5. Enter a negative quantity and confirm validation rejects it.
6. Submit a valid quantity update and confirm the dialog closes, success feedback appears, and the refreshed list shows the new quantity.
7. If practical, simulate or encounter a missing balance and confirm not-found feedback is shown and the page can refresh.

Expected outcome: update behavior remains quantity-only.

## 12. Verify No Persistence or Migration Work Was Added

Review the final diff.

Expected outcome:

- No EF migrations are generated.
- No database update commands are required.
- No backend domain, handler, endpoint, or persistence redesign is included.
- WebApp changes are limited to page/navigation/dialog/grid/filter/API-client wiring needed for the UI.

## 13. Verify No Out-of-Scope Operational Workflow Was Added

Review the final diff.

Expected outcome:

- No receiving, putaway, picking, shipping, LPN, reservations, transactions, movements, adjustments, batch/lot, expiry, serial numbers, UoM conversion, packaging, or cycle counting behavior.
- No Inventory Balance delete, deactivate, reactivate, bulk editing, import, or export.
- No seed/demo data.
- No external integrations.
