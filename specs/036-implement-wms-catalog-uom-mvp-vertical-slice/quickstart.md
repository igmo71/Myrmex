# Quickstart: Validate WMS Catalog/UoM MVP Plan and Implementation

Use this guide to validate that issue #36 remains a small repeated Catalog/UoM reference-data vertical slice and proves the expected behavior after implementation.

## Prerequisites

- Run commands from the repository root.
- Stay on branch `036-implement-wms-catalog-uom-mvp-vertical-slice`.
- Read `specs/036-implement-wms-catalog-uom-mvp-vertical-slice/spec.md` and `specs/036-implement-wms-catalog-uom-mvp-vertical-slice/plan.md` before implementation.

## 1. Confirm Planning Artifacts Exist

```powershell
Test-Path specs\036-implement-wms-catalog-uom-mvp-vertical-slice\plan.md
Test-Path specs\036-implement-wms-catalog-uom-mvp-vertical-slice\research.md
Test-Path specs\036-implement-wms-catalog-uom-mvp-vertical-slice\data-model.md
Test-Path specs\036-implement-wms-catalog-uom-mvp-vertical-slice\contracts\catalog-uom-api-and-ui-contract.md
Test-Path specs\036-implement-wms-catalog-uom-mvp-vertical-slice\quickstart.md
```

Expected outcome: every command returns `True`.

## 2. Confirm No Clarification Markers Remain

```powershell
$marker = "NEEDS " + "CLARIFICATION"
rg -n $marker specs\036-implement-wms-catalog-uom-mvp-vertical-slice
```

Expected outcome: no matches.

## 3. Confirm Scope Boundaries

```powershell
rg -n "conversion|base UoM|alternative UoM|SKU-to-UoM|Packaging|Barcode|Inventory|Receiving|LPN|Picking|Shipping|Integration|createdAtUtc.*sort|updatedAtUtc.*sort|AsEnumerable|provider-specific|new endpoint/UI test" specs\036-implement-wms-catalog-uom-mvp-vertical-slice
```

Expected outcome: matches appear only as explicit exclusions, rejected alternatives, or sorting guardrails.

## 4. Build the Solution

```powershell
dotnet build Myrmex.slnx -nologo -v:minimal
```

Expected outcome: build succeeds without new warnings caused by Catalog/UoM changes.

## 5. Run Focused UoM Tests

```powershell
dotnet test Myrmex.Tests\Myrmex.Tests.csproj --filter "FullyQualifiedName~UnitOfMeasure|FullyQualifiedName~UnitsOfMeasure" -nologo -v:minimal
```

Expected outcome:

- UoM domain validation and lifecycle tests pass.
- Focused UoM handler tests pass.
- UoM persistence tests pass.
- UoM-specific Catalog client route/DTO/result wiring tests pass if added.

## 6. Run Full Regression Tests

```powershell
dotnet test Myrmex.Tests\Myrmex.Tests.csproj -nologo -v:minimal
```

Expected outcome:

- Existing WMS Topology tests still pass.
- Existing Catalog/SKU tests still pass.
- New focused Catalog/UoM tests pass.

## 7. Verify Persistence Migration

Review the generated migration and model snapshot.

Expected outcome:

- Adds `wms.units_of_measure`.
- Adds required columns for code, name, created timestamp, and active state.
- Adds optional columns for symbol and updated timestamp.
- Adds a unique index for UoM code.
- Does not add a `NormalizedCode` column.
- Leaves `UpdatedAtUtc` nullable.
- Does not add conversion, base/alternative UoM, SKU binding, packaging, barcode, inventory, receiving, LPN, picking, shipping, or integration tables.

## 8. Verify Practical Persistence Tests

Review or run the Catalog/UoM persistence tests.

Expected outcome:

- Tests use the existing SQLite/EnsureCreated WMS test infrastructure.
- Tests verify the UnitOfMeasure mapping/table can be created.
- Tests verify required fields and duplicate normalized `Code` values are protected.
- Tests do not require SQL Server-specific migration execution.

## 9. Manual API Behavior Check

After starting the application in the normal local development flow, verify:

1. Create UoM `EA` with name `Each` and symbol `ea` succeeds.
2. The created UoM returns `updatedAtUtc` as empty/null.
3. Creating ` ea ` again fails with duplicate-code feedback.
4. Listing UoMs shows `EA` when active.
5. Searching by `EA`, `Each`, or `ea` returns the UoM.
6. Sorting by code, name, and active state works.
7. Sorting by created or updated timestamp is not exposed or falls back safely to code ordering.
8. Updating name or symbol succeeds, preserves code, and sets `updatedAtUtc`.
9. Deactivating hides the UoM from the default list and sets `updatedAtUtc`.
10. Including inactive records shows the deactivated UoM.
11. Reactivating makes it appear in the default list again and sets `updatedAtUtc`.

Expected outcome: all behaviors match `contracts/catalog-uom-api-and-ui-contract.md`.

## 10. Manual UI Smoke Validation

Open `/wms/catalog/uoms` in the web app and verify:

- The Catalog navigation includes UoMs.
- The page loads without affecting WMS Topology or Catalog/SKU pages.
- Create, edit, deactivate, reactivate, refresh, search, and include-inactive controls work.
- The edit dialog does not allow changing UoM code.
- Validation and duplicate-code errors are visible to the user.
- Snackbar and reload behavior matches the SKU page.
- The UI does not show conversion, SKU binding, packaging, barcode, inventory, receiving, LPN, picking, shipping, or integration controls.

Expected outcome: the Catalog/UoM page demonstrates the MVP without scope drift.

## 11. Verify Repeated-Slice Testing Scope

Review the final test list.

Expected outcome:

- UoM tests are focused and explicit.
- Tests cover UoM-specific validation, lifecycle idempotency, mapping, unique code, supported sorting, and route/DTO/result wiring where needed.
- Tests do not duplicate the full SKU-level matrix when the existing representative Catalog client and UI patterns already cover the behavior.
- No new endpoint or UI automation framework was introduced.

## 12. Verify No Broad Refactor

Review the final diff.

Expected outcome:

- Existing WMS Topology behavior and API client support types were not moved or rewritten.
- Existing Catalog/SKU behavior was not reworked except where shared Catalog registration must compile.
- No new logging, telemetry, observability, diagnostics, endpoint-test, UI-test, or architectural framework infrastructure was introduced.
