# Quickstart: Internal Inventory Transfer MVP Validation

This guide describes validation scenarios for implementation. Per repository workflow guidance, planning does not run builds, tests, migrations, app startup, or infrastructure commands automatically.

## Prerequisites

- Feature implementation completed for `specs/075-internal-inventory-transfer-mvp`.
- Local database updated with the feature migration by the developer.
- Seed/reference data includes regular storage-location types plus `InternalTransit` and `ExternalTransit`.
- Test data includes one active warehouse, active SKU with active base UoM, two active regular storage locations, one active internal transit location, and source inventory balance.

## Recommended Validation Commands

Run these only when the developer is ready:

```powershell
dotnet build
dotnet test
```

If implementation creates an EF migration, validate migration shape and apply it only through the developer-controlled workflow.

## Scenario 1: Create Direct Transfer

1. Open Inventory Transfers.
2. Create a transfer in one warehouse with no transit location.
3. Add one line with SKU, source regular storage location, destination regular storage location, and requested quantity 10.
4. Save.

Expected:

- Transfer status is `Created`.
- The line shows requested 10, picked 0, placed 0, in-transit 0.
- Details expose `Move` action only.
- `Pick` and `Place` are not available.

## Scenario 2: Execute Direct Move

1. Use the direct transfer from Scenario 1.
2. Move quantity 4.

Expected:

- One movement is shown in read-only movement history.
- One inventory transaction exists with transaction type `Transfer`.
- Two inventory ledger entries exist: source negative 4, destination positive 4.
- Source balance decreases by 4.
- Destination balance increases by 4.
- Line shows requested 10, picked 4, placed 4, in-transit 0.
- Transfer status is `InProgress`.

## Scenario 3: Complete Direct Transfer

1. Move the remaining direct quantity 6.

Expected:

- Line shows requested 10, picked 10, placed 10, in-transit 0.
- Transfer status becomes `Completed`.
- No movement action is available.
- Attempting another move is rejected and changes nothing.

## Scenario 4: Create Transit Transfer

1. Create a transfer in one warehouse with the internal transit location selected.
2. Add one line with SKU, source regular storage location, destination regular storage location, and requested quantity 10.
3. Save.

Expected:

- Transfer status is `Created`.
- The line shows requested 10, picked 0, placed 0, in-transit 0.
- Details expose `Pick` and `Place` actions.
- `Move` is not available.

## Scenario 5: Pick To Internal Transit

1. Use the transit transfer from Scenario 4.
2. Pick quantity 4.

Expected:

- One movement is shown in read-only movement history.
- One inventory transaction exists with transaction type `Transfer`.
- Two inventory ledger entries exist: source negative 4, transit positive 4.
- Source balance decreases by 4.
- Transit balance increases by 4.
- Line shows requested 10, picked 4, placed 0, in-transit 4.
- Transfer status is `InProgress`.

## Scenario 6: Place From Internal Transit

1. Use the transit transfer after Scenario 5.
2. Place quantity 2.

Expected:

- A second movement is shown in history.
- One additional transfer transaction exists.
- Two additional ledger entries exist: transit negative 2, destination positive 2.
- Transit balance decreases by 2.
- Destination balance increases by 2.
- Line shows requested 10, picked 4, placed 2, in-transit 2.

## Scenario 7: Complete Transit Transfer

1. Pick the remaining requested quantity.
2. Place all remaining in-transit quantity.

Expected:

- Line shows requested 10, picked 10, placed 10, in-transit 0.
- Transfer status becomes `Completed`.
- No movement action is available.
- Attempting another pick or place is rejected and changes nothing.

## Scenario 8: Invalid Operations

Validate that each operation is rejected without changing balances, movement history, or progress:

- Create transfer with different source and destination warehouses.
- Create transfer with inactive SKU, inactive location, location from another warehouse, identical source/destination locations, or non-positive requested quantity.
- Create transit transfer with a non-internal-transit transit location.
- Direct move more than remaining requested quantity.
- Direct move or pick more than available source balance.
- Pick more than remaining requested quantity.
- Place more than current in-transit quantity.
- Pick/place on direct transfer.
- Move on transit transfer.

## Scenario 9: Transfer List and Details

1. Open Inventory Transfers list.
2. Filter by warehouse, status, created date range, transfer code, source location, destination location, SKU, and transit presence.
3. Open details for direct, transit, in-progress, and completed transfers.

Expected:

- List paging and sorting are deterministic.
- Totals match transfer details.
- Details show current progress and read-only movement history.
- Movement history includes inventory transaction references.

## Scenario 10: Inventory Ledger Visibility

1. Open Inventory Ledger.
2. Filter for transaction type `Transfer` and the SKU/location used in transfer scenarios.
3. Open transaction details.

Expected:

- Transfer movement ledger entries appear as normal inventory ledger history.
- Each transfer transaction has exactly two entries.
- Transaction details do not require transfer-specific source fields on `InventoryTransaction`.

## Scenario 11: MVP Scope Check

Verify the delivered UI and API do not expose persisted transfer execution mode, persisted movement type, scanner sessions, fixed scan order, package barcode, LPN, batch, serial, expiry, reservation, discrepancy, cancellation, correction, receiving, putaway, approval, route optimization, or external transfer workflow.
