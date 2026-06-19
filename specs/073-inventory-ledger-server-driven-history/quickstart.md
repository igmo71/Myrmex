# Quickstart: Validate Inventory Ledger Server-Driven History

This guide describes developer-controlled validation for the Inventory Ledger read-side plan. Codex must not run these commands automatically.

## Prerequisites

- The feature is implemented from `specs/073-inventory-ledger-server-driven-history/plan.md`.
- The database contains Inventory Adjustment Ledger history created by the existing adjustment workflow.
- Migrations, database updates, application startup, builds, and tests are run only by the developer.

## Recommended Validation Commands

Run only after implementation and only when the developer chooses.

```powershell
dotnet build Myrmex.slnx -nologo -v:minimal
```

```powershell
dotnet test Myrmex.Tests\Myrmex.Tests.csproj -nologo -v:minimal
```

Targeted test selection may be useful, but use the repository's current Microsoft.Testing.Platform-compatible syntax rather than assuming VSTest `--filter` behavior.

No EF migration command is expected for this feature because no schema changes are planned.

## Backend Behavior Checks

### 1. Ledger List Default Load

Expected:

- `GET /api/wms/inventory/ledger` returns success.
- Result uses normalized default paging.
- Rows are ordered newest-first by occurrence time with deterministic transaction and entry tie-breakers.
- `TotalCount` is calculated before paging.

### 2. Filters

Verify list filtering by:

- SKU.
- Warehouse.
- Storage location.
- Transaction type `Adjustment`.
- Occurrence-from UTC inclusive boundary.
- Occurrence-to UTC exclusive boundary.

Expected:

- Filters are applied server-side before count and paging.
- Invalid occurrence range returns validation ProblemDetails.
- Equal occurrence boundaries return a valid empty interval.
- Unsupported transaction type returns validation ProblemDetails.

### 3. Sorting and Paging

Verify supported sort keys:

- Occurred UTC.
- Transaction type.
- SKU code and name.
- Warehouse code and name.
- Storage-location code.
- Balance before.
- Quantity delta.
- Balance after.
- Reason.

Expected:

- Every requested sort is deterministic with stable tie-breakers.
- Requested sorts use the requested primary direction, then transaction ID ascending, then entry ID ascending.
- Default sort uses occurred UTC descending, then transaction ID descending, then entry ID descending.
- Repeated requests return the same row order when data has not changed.
- Paging is applied after sorting.

### 4. Projection and Historical References

Expected:

- List rows include transaction type, reason, occurrence UTC, SKU, base UoM, warehouse, storage location, before, delta, and after values.
- The query does not depend on current `InventoryBalance` rows.
- Inactive SKU, UoM, warehouse, and storage-location references remain visible when history exists.
- Missing referenced records fail visibly rather than fabricating partial history.

### 5. Transaction Details

Expected:

- `GET /api/wms/inventory/transactions/{transactionId}` returns transaction header and all entries.
- Details support more than one entry.
- Entry ordering is deterministic.
- Missing transaction returns NotFound ProblemDetails.

## API Client Checks

Expected:

- Empty ledger list request produces `/api/wms/inventory/ledger` without trailing `?`.
- Explicit request values produce query parameters for paging, sort, SKU, warehouse, storage location, transaction type, and occurrence range.
- Details request uses `/api/wms/inventory/transactions/{transactionId}`.
- Cancellation propagates through client calls.
- Nested list and details DTOs deserialize correctly.

## Manual UI Smoke Checks

1. Open WMS Inventory navigation and choose Inventory Ledger.
2. Confirm the initial page loads unfiltered, newest-first, and paged.
3. Apply SKU filter using autocomplete; confirm inactive historical SKUs can be found when present.
4. Apply warehouse filter; confirm inactive historical warehouses can be selected when present.
5. Apply storage-location filter after selecting a warehouse; confirm lookup is warehouse-scoped and inactive-inclusive.
6. Change warehouse after selecting a storage location; confirm incompatible storage location is cleared.
7. Apply transaction-type filter `Adjustment`.
8. Apply occurrence-from and occurrence-to UTC filters; confirm boundary behavior.
9. Sort each supported visible grid column; confirm no paging instability.
10. Open transaction details; confirm header, reason, timestamps, and all entries display.
11. Confirm full reason is visible in details when grid text is shortened.
12. From Inventory Balances, use a row history action.
13. Confirm Ledger opens with that row's SKU, warehouse, and storage-location filters active.
14. Copy the resulting Ledger URL.
15. Reload the page or open the copied URL in a new browser tab.
16. Confirm SKU, warehouse, and storage-location filters are visibly restored.
17. Confirm the same filtered history request is issued.
18. Confirm inactive referenced SKU, warehouse, or storage-location records can still hydrate and display when present.
19. Clear or change filters and continue normal Ledger browsing.
20. Confirm rapid filter/search changes do not show cancellation as an error.
21. Confirm empty results show a clear empty state, not an error.
22. Confirm no ledger edit, delete, correction, reversal, transfer, export, analytics, or rebuild controls are present.

## Scope Review

Confirm implementation did not add:

- Ledger mutation endpoints or UI.
- Inventory Transfer.
- InventoryAccount or transit inventory.
- LPN, handling units, lot, batch, serial, or expiry history.
- Export, dashboards, or analytics.
- Historical snapshots of reference-data names.
- Generic grid, lookup, report, or observability frameworks.
- New indexes, migrations, or `WmsDbContextModelSnapshot` changes.
