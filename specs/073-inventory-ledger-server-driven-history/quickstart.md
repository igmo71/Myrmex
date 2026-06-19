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
- Occurrence From date mapped to the inclusive UTC start of that date.
- Occurrence To date mapped from the inclusive UI date to the exclusive UTC start of the following day.

Expected:

- Request validation occurs before constructing the filtered EF query.
- Filters are applied server-side before count and paging.
- Invalid occurrence range returns validation ProblemDetails.
- Equal occurrence boundaries return a valid empty interval.
- Unsupported transaction type returns validation ProblemDetails.
- Unsupported transaction type values and invalid occurrence ranges do not participate in SQL query construction.

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

- Public sort values follow the existing PascalCase `InventoryBalanceSortBy` convention, such as `OccurredAtUtc`, `TransactionType`, and `SkuCode`.
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
8. Confirm occurrence filters are clearable date pickers and do not accept free-text ISO timestamp input.
9. Confirm picker display follows the current regional culture, for example `ru-RU` as `19.06.2026`, `en-GB` as `19/06/2026`, and `en-US` as `6/19/2026`.
10. Select only a From date; confirm the Ledger request sends `OccurredFromUtc` as `00:00:00Z` at the start of that selected date and sends no To boundary.
11. Select only a To date; confirm the Ledger request sends `OccurredToUtc` as `00:00:00Z` at the start of the following UTC day and sends no From boundary.
12. Select the same From and To date; confirm the request covers the complete selected UTC calendar day.
13. Select a From date later than the To date; confirm page-level validation appears and no Ledger request is sent.
14. Clear both dates; confirm occurrence filters are removed from the next Ledger request.
15. Sort each supported visible grid column; confirm no paging instability.
16. Open transaction details; confirm header, reason, timestamps, and all entries display.
17. Confirm full reason is visible in details when grid text is shortened.
18. From Inventory Balances, use a row history action.
19. Confirm Ledger opens with that row's SKU, warehouse, and storage-location filters active.
20. Open a routed Ledger URL with only `stockKeepingUnitId`; confirm the SKU filter hydrates visibly and only the SKU-filtered first page is requested.
21. Open a routed Ledger URL with only `warehouseId`; confirm the warehouse filter hydrates visibly and only the warehouse-filtered first page is requested.
22. Open a routed Ledger URL with only `storageLocationId`; confirm the storage location hydrates, its warehouse is derived and displayed, and the first request includes both warehouse and storage-location filters.
23. Open a routed Ledger URL with matching `warehouseId` and `storageLocationId`; confirm both filters hydrate visibly and the first request includes both filters.
24. Open a routed Ledger URL with mismatched `warehouseId` and `storageLocationId`; confirm clear page-level feedback appears and no contradictory filtered ledger request is sent.
25. While that mismatch feedback is active, change only SKU; confirm the Ledger list remains blocked until warehouse/location is corrected or filters are fully cleared.
26. For every routed URL above, confirm the page does not make an initial unfiltered ledger request before hydration completes.
27. Copy a valid routed Ledger URL.
28. Reload the page or open the copied URL in a new browser tab.
29. Confirm SKU, warehouse, and storage-location filters that were present in the link are visibly restored.
30. Confirm the same filtered history request is issued after copied-link hydration.
31. Confirm inactive referenced SKU, warehouse, or storage-location records can still hydrate and display when present.
32. Clear or change filters and continue normal Ledger browsing.
33. Confirm rapid filter/search or routed-hydration cancellation does not show cancellation as an error.
34. Confirm empty results show a clear empty state, not an error.
35. Confirm no ledger edit, delete, correction, reversal, transfer, export, analytics, or rebuild controls are present.

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
