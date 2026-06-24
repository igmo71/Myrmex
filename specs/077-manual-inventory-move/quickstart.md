# Quickstart: Validate Manual Inventory Move

## Prerequisites

- Branch `077-implement-manual-inventory-move`.
- Existing Inventory Balance, Adjustment Ledger, and Inventory Transfer migrations applied.
- Active SKU, warehouse, two regular locations, source balance, and transit locations for rejection checks.
- `MYRMEX_WMS_TEST_CONNECTION` points to a dedicated SQL Server database ending in `_test` for integration tests.

Builds, tests, startup, database updates, migrations, Docker, and infrastructure commands are developer-controlled.

## Recommended commands

```powershell
dotnet build
dotnet test
```

No migration command is expected.

## Lookup

1. Call `GET /api/wms/inventory/balances/lookup?skuId={skuId}&storageLocationId={locationId}`.
2. Confirm quantity and non-empty balance version.
3. Confirm an existing balance still returns when related records are inactive.
4. Confirm a missing pair returns 404.

## Existing destination

1. Capture source quantity/version and destination quantity.
2. POST a positive quantity/reason to `/api/wms/inventory/balances/move`.
3. Confirm source decreases and destination increases by the moved quantity.
4. Confirm one Transfer transaction, exactly two balanced entries, and visible trimmed reason.
5. Confirm no Inventory Transfer or adjustment record is created.

## Missing destination

1. Move to an eligible location with no balance for the SKU.
2. Confirm destination-before is zero and one destination balance is created.
3. Confirm the same transaction/ledger invariants.

## Full source quantity

Move the entire source quantity and confirm the source row remains with zero quantity and a new version.

## Concurrency

1. Submit with a stale source version; confirm 409 and no persisted effects.
2. Submit more than available; confirm 409 and no effects.
3. Remove the source balance while retaining the SKU and source location references; confirm 409 and no persisted effects.
4. Run two moves to the same existing destination; confirm one succeeds and one returns 409.
5. Repeat with a missing destination; confirm one row is created and the competing request returns 409 without partial effects.

## Missing references

Confirm `404 Not Found` for each independently missing reference:

- SKU;
- source storage location;
- destination storage location.

Confirm that a missing destination balance is not an error: a valid move creates it with a prior quantity of zero.

## Eligibility

Confirm clear rejection with no changes for same location, cross-warehouse destination, inactive SKU/location/type/status, transit source/destination, invalid quantity/reason, and malformed/missing version.

## UI

1. Choose Move on an Inventory Balances row.
2. Confirm read-only SKU, warehouse, source, quantity, and Base UoM.
3. Confirm destination choices are active/selectable, same-warehouse, non-transit, and exclude source.
4. Complete a valid move and verify the result summary.
5. Choose Done and confirm the grid refreshes.
6. Verify a conflict requires close/refresh before retry.

## Completion evidence

- Automated owning-layer tests pass.
- Manual UI scenarios pass.
- No migration or excluded transfer/scanner behavior is introduced.
