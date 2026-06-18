# Contract: Inventory Adjustment UI

## Existing Balance Adjustment

Entry point: Inventory Balances grid row action.

Action label: `Adjust`.

Dialog context:

- SKU code and name: read-only.
- Warehouse: read-only.
- Storage location: read-only.
- Base UoM: read-only.
- Current quantity: read-only.

Editable fields:

- Counted quantity: required, decimal, greater than or equal to zero.
- Reason: required after trimming, maximum 500 characters.

Submission:

- Uses `POST /api/wms/inventory/adjustments`.
- Sends row `StockKeepingUnitId`.
- Sends row `StorageLocationId`.
- Sends entered `CountedQuantity`.
- Sends trimmed `Reason`.
- Sends row `BalanceVersion` as `ExpectedBalanceVersion`.

Success:

- Dialog closes.
- Success feedback is shown.
- Inventory Balance grid reloads current server data.

Concurrency conflict:

- Show a refresh-and-review message.
- Do not retry automatically.
- Keep the entered counted quantity and reason where practical.
- Disable resubmission from the stale dialog.
- Require the user to close or cancel the stale dialog.
- Reload current grid data after the stale dialog closes.
- The user reopens adjustment from the refreshed row.

## Missing Balance Initial Count

Entry point: Inventory Balances page create/initial-count action.

Dialog context:

- SKU selector using existing SKU lookup behavior.
- Warehouse selector.
- Storage location selector constrained by selected warehouse.
- Base UoM display for selected SKU.

Editable fields:

- Counted quantity: required, decimal, greater than or equal to zero.
- Reason: required after trimming, maximum 500 characters.

Submission:

- Uses `POST /api/wms/inventory/adjustments`.
- Sends selected `StockKeepingUnitId`.
- Sends selected `StorageLocationId`.
- Sends entered `CountedQuantity`.
- Sends trimmed `Reason`.
- Sends `ExpectedBalanceVersion = null`.

Success:

- Counted quantity greater than zero creates balance and ledger.
- Counted quantity zero creates a persisted zero balance without ledger.
- Dialog closes.
- Success feedback is shown.
- Inventory Balance grid reloads current server data.
- If active filters hide the new balance, success feedback must not imply the row is visible.

## Removed UI Behavior

- The row action must no longer say or behave as `Update quantity`.
- The UI must not call direct quantity-update API methods.
- The create flow must not call direct balance-create API methods.
- No ledger-history page is added.
- No Transfer, InventoryAccount, LPN, zero-row deletion, or event-sourcing UI is added.

## Error Display

- Validation errors remain in the dialog and preserve entered values where practical.
- Not-found responses use the existing Myrmex message style for missing SKU, storage location, or required related records.
- Missing-balance eligibility failures use the current create-handler validation/conflict convention.
- `InventoryBalance.ConcurrencyConflict` tells the user to refresh and review counted quantity.
- For initial count, a concurrent duplicate insert shows the same stale-state concept and reloads the grid after the stale dialog is closed.
- Unexpected request failures use existing WebApp alert/dialog error patterns.
- Expected cancellation is not shown as a user-facing error.
