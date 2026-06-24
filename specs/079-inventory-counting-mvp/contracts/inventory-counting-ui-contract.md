# UI Contract: Inventory Counting MVP

## Navigation

Add `Inventory Counts` under the existing WMS Inventory navigation group, linking to:

```text
/wms/inventory/counts
```

## Count list page

Use MudDataGrid server data.

Columns:

- status;
- warehouse;
- created time and creator;
- completed/cancelled time when present;
- current lines;
- Applied;
- unresolved;
- Conflict;
- Open action;
- Cancel action when allowed.

Filters:

- warehouse from existing active warehouse lookup/visibility;
- exact status;
- created date range.

The list resets to page one when filters change. Empty, loading, and error states use existing inventory-page patterns.

## Create dialog

Fields:

- active visible warehouse;
- optional reason.

On success, navigate to or open the created details page. Actor identity is not displayed as an editable field and is not submitted.

## Count details page

Header:

- count ID;
- warehouse;
- status;
- reason;
- rowversion-backed action state;
- creator/completer/canceller actor IDs and lifecycle timestamps.

Actions:

- Add line for Draft/InProgress;
- Complete when all current lines are Applied;
- Cancel for Draft/InProgress;
- no mutating actions for Completed/Cancelled.

Line grid:

- SKU and base UoM;
- storage location;
- system quantity;
- counted quantity;
- variance;
- status/current indicator;
- comment;
- counter/time;
- applier/time;
- adjustment transaction link;
- replacement/superseded relationship;
- state-appropriate actions.

Superseded lines remain visible, visually identified as historical, and do not count toward progress.

## Add-line dialog

Select:

- active SKU using existing catalog lookup;
- active/selectable storage location scoped to the count warehouse;
- exclude internal/external transit types.

Server validation remains authoritative. Adding a line refreshes details and count version without moving Draft to InProgress.

## Pending-line actions

- Enter Count;
- Remove.

Remove requires confirmation that the line was added by mistake. Once a counted quantity is entered, Remove is no longer available.

## Count-entry dialog

Read-only:

- SKU;
- location;
- system quantity;
- base UoM.

Editable:

- non-negative counted quantity;
- optional comment.

Show calculated variance before submission. A successful first entry changes the count to InProgress and refreshes details.

## Counted-line actions

- Edit Count;
- Apply.

Disable repeated submission while saving. Apply results:

- zero variance: explain that the line was confirmed without an inventory transaction;
- non-zero variance: show the adjustment transaction link and resulting quantity;
- conflict: explain that inventory changed and offer Supersede.

## Conflict and Supersede

Conflict lines are read-only. Supersede requires confirmation, then:

- keeps the original line visible as Superseded;
- adds a fresh Pending replacement;
- displays its new system snapshot;
- refreshes count details/progress.

## Final states

Completed and Cancelled details are fully readable and expose no mutating actions. Cancelling explicitly warns that already Applied adjustments remain effective.

## Error and accessibility behavior

- Validation and ProblemDetails messages appear near the relevant action/form.
- Stale-version conflicts force details reload before another attempt.
- Actor, status, variance, and result meaning use visible text, not color alone.
- Buttons and icon actions have accessible labels/tooltips.
- Lookup cancellation must not replace newer search results.
