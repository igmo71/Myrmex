# UI Contract: Internal Inventory Transfer MVP

## Navigation

Add an Inventory Transfers page under the WMS Inventory area, beside Inventory Balances and Inventory Ledger.

## Transfer List Page

### Columns

- Transfer code
- Warehouse
- Status
- Created date
- Transit location, when present
- Total requested quantity
- Total picked quantity
- Total placed quantity
- Total in-transit quantity
- Details action

### Filters

- Warehouse
- Status
- Created date range
- Transfer code
- Source location
- Destination location
- SKU
- Has transit location

Filter changes reset the grid to the first page. Refresh reloads the current grid state. Sorting and paging are server-driven.

## Create Transfer Dialog

### Header Inputs

- Source warehouse
- Destination warehouse, constrained to the same warehouse for MVP
- Optional transit storage location

### Line Inputs

- SKU
- Source storage location
- Destination storage location
- Requested quantity

The dialog supports multiple lines. Source and destination locations are required at creation. Automatic source selection and destination suggestion are not included.

## Transfer Details Dialog or Page

### Header

- Transfer code
- Source warehouse
- Destination warehouse
- Transit location, when present
- Status
- Created and updated times

### Line Table

- SKU code/name
- Source location
- Destination location
- Requested quantity
- Picked quantity
- Placed quantity
- In-transit quantity
- Remaining to pick
- Remaining to place
- Available action

### Actions

- Direct transfer line: `Move`
- Transit transfer line: `Pick`, `Place`
- Completed transfer: no movement actions

Actions are hidden or disabled when no longer valid, but backend validation is final.

## Movement Dialogs

### Move Dialog

Shown for direct transfers. Displays SKU, source location, destination location, requested/progress quantities, and accepts quantity.

### Pick Dialog

Shown for transit transfers. Displays SKU, source location, transit location, requested/progress quantities, and accepts quantity.

### Place Dialog

Shown for transit transfers. Displays SKU, transit location, destination location, in-transit quantity, and accepts quantity.

### Success Behavior

- Close the movement dialog.
- Show success notification using existing UI convention.
- Reload transfer details.
- Reload list grid when returning to list context.

### Error Behavior

Show existing ProblemDetails/API-result messages for wrong movement pattern, completed transfer, non-positive quantity, insufficient balance, over-move, over-pick, over-place, missing or invalid references, and stale state.

## Movement History

Movement history is read-only.

### Columns

- Occurred at
- SKU
- From location
- To location
- Quantity
- Derived movement meaning
- Inventory transaction reference

No edit, delete, cancel, correction, scanner audit, or package-level controls are included.

## Out of Scope UI

- Mobile scanner UI
- Barcode scanner device integration
- Scan sessions
- Fixed scan order workflow
- Package barcode resolution
- LPN, batch, serial, expiry, reservation, discrepancy, cancellation, correction, receiving, putaway, approval, route optimization, and external transfer workflows

## Manual Smoke Scope

Quickstart validation should manually verify direct transfer, transit transfer, movement actions, completed read-only state, list filters, details progress, read-only movement history, and absence of scanner workflow.
