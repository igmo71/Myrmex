# Contract: Receiving Orders WebApp

## Navigation and Routes

Add one localized Receiving navigation entry under WMS and these full-page routes:

| Route | Purpose |
|---|---|
| `/wms/receiving-orders` | Server-driven list and filters. |
| `/wms/receiving-orders/new` | Create a complete Draft. |
| `/wms/receiving-orders/{id}/edit` | Replace a complete existing Draft. |
| `/wms/receiving-orders/{id}` | View and execute Start, Receive, Complete. |

Creation and complete-document editing never use a modal dialog.

## List Page

Follow the existing WMS MudDataGrid server-data pattern:

- search by Receiving Order Number;
- Warehouse autocomplete filter;
- Status filter with Draft/InProgress/Completed;
- server sorting and paging;
- Create action;
- Open details action for every order;
- Edit and Delete actions only for Draft;
- on successful Draft delete, reload the grid;
- on delete conflict, reload and show existing conflict guidance.

The grid displays Number, Warehouse, ReceivingLocation, Status, line count, planned/received/remaining totals, and relevant timestamps without rendering all lines.

## Full-Page Draft Editor

Use one reusable page component for create and edit modes.

### Header State

- Number text input.
- Warehouse lookup using the existing selectable Warehouse endpoint.
- ReceivingLocation lookup disabled until Warehouse is selected.
- ReceivingLocation lookup always sends `SelectableOnly=true` and `StorageLocationTypeCode=StorageLocationTypeCodes.Receiving`; UI code imports the shared Topology-owned constant and does not repeat the raw persisted code.
- Changing Warehouse clears the selected ReceivingLocation and any cached location results.
- Edit mode loads and preserves `OrderVersion`; create mode has none.

The lookup offers only locations that satisfy the authoritative rule: active Warehouse, active location in that Warehouse, active Receiving type, and current status/other inventory-selectability eligibility. The backend revalidates the same rule on Create, Update Draft, and Start.

### Line State

Keep the complete plan in one backing collection for the page lifetime:

| Field | Create/new row | Loaded retained row |
|---|---|---|
| `LineId` | null | Existing ID |
| `Sku` | Selected summary | Loaded summary, replaceable while Draft |
| `PlannedQuantity` | User-entered positive value | Editable positive value |

- Add/remove actions mutate the backing collection only until Save.
- Locally prevent duplicate SKU selection, but retain server validation.
- Local line search is case-insensitive over SKU code/name and filters only rendered rows.
- Save serializes the entire backing collection, never only the current filtered view.
- Omitted persisted LineIds are intentional removals.
- A retained row keeps its LineId even when its SKU or quantity changes.

### Focused SKU Selection

Do not render one autocomplete control for every line. Select/Change opens one small focused search dialog that:

- calls the existing active SKU lookup with the current search text and standard capped result count;
- shows code, name, and base UOM;
- excludes or disables SKUs already present in another current line;
- returns one selected SKU summary to the row;
- adds no bulk paste, import, or spreadsheet behavior.

### Save and Conflict Behavior

- Create sends `CreateReceivingOrderRequest` and navigates to details on success.
- Edit sends the complete `UpdateReceivingOrderDraftRequest` with current OrderVersion.
- On edit success, replace local state with returned details or navigate to details.
- On HTTP 409, do not auto-retry and do not silently discard the unsaved complete plan. Show explicit current-data conflict guidance, disable repeated Save, and provide a confirmed Reload latest/discard-local-changes action. Keep the local plan visible so the user may resolve it manually, but add no automatic or three-way merge, automatic replay, or generalized conflict-resolution workflow.

## Details and Execution Page

Display:

- Number, Warehouse, ReceivingLocation, status, Created/Updated/Started/Completed timestamps;
- total and per-line planned, received, and remaining quantities with base UOM;
- InventoryTransaction reference/link after completion;
- current actions determined by status and quantities.

Action availability:

| State | Actions |
|---|---|
| Draft | Edit, Delete, Start |
| InProgress with remaining quantities | Receive per line |
| InProgress fully received | Complete; Receive controls show no remaining amount |
| Completed | Read-only; open InventoryTransaction |

### Receive Quantity Dialog

One small dialog accepts a strictly positive increment for one line, shows planned/received/remaining context, and sends the current aggregate OrderVersion. It blocks a locally obvious over-receipt but relies on server validation for correctness.

### Mutation Refresh and Conflict UX

- Replace page details with every successful Start/Receive/Complete response.
- A concurrently resolved Complete returned as success is shown as ordinary Completed state.
- On true HTTP 409, execution actions may reload current details, show clear conflict/refresh guidance, and require a deliberate retry because they have no comparable large unsaved page plan.
- Never automatically retry inventory posting or another mutation.
- Use the existing Inventory Transaction details dialog/client when opening the completion reference.

## Representative 300-Line Functional Acceptance Dataset

The page must support a representative 300-line functional acceptance dataset without treating it as a maximum or performance target. The deterministic procedure uses exactly 300 distinct active SKU lines:

1. Create a 300-line Draft through the WebApp.
2. Reopen it and verify all 300 identities and quantities.
3. Revise retained lines, remove at least one, add replacement lines, and save the complete plan.
4. Reopen and locally search/filter the current line set; confirm filtering did not alter the backing plan.
5. Start, receive every line, and complete.
6. Confirm no line loss, no order splitting, correct totals, and one InventoryTransaction reference.

There is no response-time, throughput, memory, browser-load, or user-study assertion. Preparing 300 active SKUs is an acceptance-environment prerequisite and does not add Receiving import behavior.

## Localization and Accessibility

- Add keys to invariant, English, and Russian `SharedResource` files.
- Use existing MudBlazor labels, validation messages, buttons, alerts, confirmation dialogs, focus behavior, and disabled-state conventions.
- Give icon-only line actions accessible labels/tooltips.
- Preserve keyboard-accessible dialog and form behavior provided by current components.

## Scope Guardrails

The WebApp adds none of the following:

- complete-document modal;
- automatic numbering;
- supplier, purchase order, ASN, dock, door, or staging behavior;
- bulk paste, file import, spreadsheet framework, or server paging of an unsaved plan;
- scanner/mobile flow;
- correction, reversal, damage, discrepancy, shortage, excess, partial completion, or putaway workflow;
- automatic retry after conflict.
