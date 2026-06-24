# UI Contract: Manual Inventory Move

## Inventory Balances grid

Each row exposes History, Adjust, and Move. Move passes the selected `InventoryBalanceDetails`, including `BalanceVersion`, to the dialog.

## Dialog context

Read-only:

- SKU code/name
- Source warehouse
- Source location
- Current quantity
- Base UoM

Editable:

- Destination location
- Positive quantity
- Required reason

## Destination search

Use existing topology lookup with:

- source warehouse ID;
- `SelectableOnly = true`;
- `ExcludeTransitTypes = true`;
- source location removed from results;
- existing bounded autocomplete and cancellation behavior.

Server validation remains authoritative.

## Submission

Submit selected SKU/source, destination, quantity, reason, and selected row `BalanceVersion`. Disable repeated submission while saving.

## Success

Show a read-only result state with:

- moved quantity;
- source before/after;
- destination before/after;
- source/destination labels.

Done closes the dialog. The parent reloads the grid and may show a concise success snackbar.

## Failure

- Validation/not-found: show returned message and allow correction.
- Conflict: explain inventory changed, disable stale resubmission, and require close/refresh/retry.
- Unexpected failure: use existing client error display.
- Canceled lookup: do not replace newer search results.

All fields/actions have visible text labels; results are not color-only.

