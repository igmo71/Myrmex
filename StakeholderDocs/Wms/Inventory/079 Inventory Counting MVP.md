# 079 Inventory Counting MVP

## Context

Myrmex now has a solid Inventory foundation:

* inventory balances by SKU and storage location;
* initial count / adjustment operations;
* internal inventory transfer;
* manual inventory move within one warehouse;
* inventory transaction and ledger history;
* exact SKU/location inventory balance lookup.

The next step is to introduce a controlled inventory counting process.

Inventory counting is not the same as a direct adjustment. An adjustment changes inventory immediately. A count records a physical verification process: what the system believed, what the operator physically counted, what variance was detected, and what inventory correction was applied.

This feature introduces an Inventory Counting MVP for warehouse operators to create count sessions, count SKU/location lines, calculate variance, and apply counted variance through the existing inventory transaction and ledger mechanism.

## Goal

Implement a minimal but auditable Inventory Counting process.

The system must allow an operator to:

1. Create an inventory count session for one warehouse.
2. Add SKU/location count lines.
3. Capture the system quantity snapshot for each line.
4. Enter the physically counted quantity.
5. Calculate the variance.
6. Apply the variance to inventory through an adjustment transaction.
7. Complete or cancel the count session.
8. View count sessions and count details for audit purposes.

## Business Value

Inventory Counting enables warehouse operators to periodically verify physical stock against system stock.

The feature helps detect and correct discrepancies caused by operational mistakes, damaged goods, misplaced stock, missed moves, receiving/shipping errors, or manual corrections.

This MVP establishes the process foundation for future cycle counting, scanner-assisted counting, blind counting, approvals, recounts, and mobile counting.

## Scope

### In Scope

The MVP includes:

* Inventory Count document/session.
* Inventory Count lines.
* Count session creation for a single warehouse.
* Manual addition of SKU/location lines.
* Capturing current system quantity and balance version when a count line is added.
* Entering counted quantity for a line.
* Calculating variance as:

```text
VarianceQuantity = CountedQuantity - SystemQuantity
```

* Applying variance through the existing inventory adjustment/ledger mechanism.
* Handling zero variance without creating an unnecessary inventory transaction.
* Viewing list of inventory count sessions.
* Viewing inventory count details with lines.
* Basic UI in the web application.
* Server-side validation and concurrency protection.
* Audit through InventoryTransaction and InventoryLedgerEntry.
* Cancellation of a count session that has not been completed.
* Completion of a count session after all lines have been resolved.

### Out of Scope

The MVP explicitly excludes:

* Mobile application flow.
* Scanner-specific workflow.
* Blind count.
* Recount workflow.
* Approval workflow.
* Inventory freeze.
* Reservation or availability changes.
* Batch, serial, expiry, or LPN counting.
* Count waves.
* ABC/XYZ cycle-count scheduling.
* Automatic generation of count tasks.
* Multi-operator assignment.
* Inter-warehouse transfer.
* Receiving.
* Shipping.
* Integration with external systems.
* Reporting dashboards.
* Cost/accounting valuation.
* Printing count sheets.
* Import/export of count lines.

## Core Concepts

### Inventory Count

An Inventory Count is a warehouse-level document/session used to track a physical inventory counting process.

It belongs to exactly one warehouse.

It has a status and one or more count lines.

### Inventory Count Line

An Inventory Count Line represents one SKU at one storage location within the count warehouse.

A line stores:

* SKU;
* storage location;
* system quantity captured at line creation;
* expected balance version captured at line creation;
* counted quantity entered by the operator;
* calculated variance;
* status;
* optional comment;
* applied inventory transaction reference, when variance was applied.

### System Quantity

System Quantity is the quantity recorded by Myrmex at the moment the line is added to the count.

If the SKU/location balance exists, the system quantity is the current `InventoryBalance.Quantity`.

If the SKU/location balance does not exist, the system quantity is `0`.

### Counted Quantity

Counted Quantity is the physical quantity entered by the operator.

It must be greater than or equal to zero.

### Variance

Variance is calculated as:

```text
CountedQuantity - SystemQuantity
```

Examples:

```text
System: 10
Counted: 12
Variance: +2

System: 10
Counted: 7
Variance: -3

System: 10
Counted: 10
Variance: 0
```

### Apply

Applying a counted line means resolving the variance.

If variance is zero, the line is marked as applied without creating an inventory transaction.

If variance is non-zero, the system updates the inventory balance through the existing adjustment transaction mechanism and creates ledger history.

## Proposed Statuses

### Inventory Count Status

```text
Draft
InProgress
Completed
Cancelled
```

#### Draft

The count session was created but no line has been counted or applied yet.

#### InProgress

The count has at least one active line or counted line.

#### Completed

All lines are resolved and the count is closed.

A completed count cannot be modified.

#### Cancelled

The count was cancelled.

A cancelled count cannot be modified or applied further.

### Inventory Count Line Status

```text
Pending
Counted
Applied
Conflict
```

#### Pending

The line was added, system quantity was captured, but counted quantity has not been entered yet.

#### Counted

The counted quantity was entered and variance was calculated, but the variance has not been applied yet.

#### Applied

The line variance was resolved.

For non-zero variance, an inventory adjustment transaction was created.

For zero variance, no transaction was created.

#### Conflict

The line could not be applied because the underlying inventory balance changed after the line was captured.

The operator must review the current balance and recount or recreate the line.

## User Stories

### US1 — Create Inventory Count Session

As a warehouse operator, I want to create an inventory count session for a warehouse so that physical counting can be tracked as a separate inventory process.

#### Acceptance Criteria

* The operator can create a new inventory count for an active warehouse.
* The count is created with status `Draft`.
* The count records created timestamp.
* The count can include an optional reason or description.
* A count cannot be created for a missing or inactive warehouse.
* The operator can view the created count details.

### US2 — Add SKU/Location Count Line

As a warehouse operator, I want to add a SKU/location pair to a count session so that the system captures what it currently believes exists at that location.

#### Acceptance Criteria

* The operator can add a line to a Draft or InProgress count.
* The SKU must exist and be active.
* The storage location must exist, be active, and belong to the count warehouse.
* The storage location type and status must be active.
* Transit storage locations are not eligible for MVP counting.
* The same SKU/location pair cannot be added twice to the same count.
* When the line is added, the system captures:

  * current system quantity;
  * current inventory balance version, when a balance exists;
  * `null` expected version when the balance does not yet exist.
* If no inventory balance exists for the SKU/location pair, the system quantity is captured as `0`.
* The line is created with status `Pending`.
* Existing count details must remain viewable even if SKU/location references later become inactive.

### US3 — Enter Counted Quantity

As a warehouse operator, I want to enter the physically counted quantity for a count line so that the system can calculate the variance.

#### Acceptance Criteria

* The operator can enter counted quantity for a Pending or Counted line.
* Counted quantity must be greater than or equal to zero.
* The system calculates variance as counted quantity minus system quantity.
* The line status becomes `Counted`.
* The operator can update counted quantity until the line is applied.
* Applied lines cannot be changed.
* Cancelled or completed counts cannot be changed.

### US4 — Apply Counted Variance

As a warehouse operator, I want to apply a counted line so that inventory balances and audit history reflect the physical count.

#### Acceptance Criteria

* The operator can apply a Counted line.
* If variance is zero:

  * no inventory transaction is created;
  * no inventory ledger entry is created;
  * the line is marked `Applied`.
* If variance is non-zero:

  * the system creates an inventory adjustment transaction;
  * the system creates an inventory ledger entry;
  * the inventory balance is updated to the counted quantity;
  * the line stores the created transaction reference.
* Applying a line must be atomic.
* If the balance existed when the line was created, the current balance version must still match the captured expected balance version.
* If the balance did not exist when the line was created, the balance must still be missing when applying a positive counted quantity.
* If the balance changed after the line was created, applying the line fails with conflict and the line becomes `Conflict`.
* A conflict must not partially update inventory balance, transaction, ledger, or line state.
* Applying a line with negative resulting quantity is not allowed.
* Applying a non-zero variance must use the existing inventory adjustment mechanism; direct balance changes without ledger history are not allowed.

### US5 — Complete or Cancel Count

As a warehouse operator, I want to complete or cancel a count session so that the counting process has a clear final state.

#### Acceptance Criteria

* A count can be completed only when all lines are `Applied`.
* Completed counts cannot be modified.
* A count can be cancelled if it is not completed.
* Cancelling a count does not reverse already applied inventory adjustments.
* Cancelled counts cannot be modified.
* Count details remain viewable after completion or cancellation.

### US6 — View Inventory Count History

As a warehouse operator, I want to view inventory count sessions and their details so that I can audit counting activity.

#### Acceptance Criteria

* The operator can list inventory count sessions.
* The list shows at least:

  * count id or number;
  * warehouse;
  * status;
  * created timestamp;
  * completed timestamp, when completed;
  * cancelled timestamp, when cancelled;
  * line count;
  * applied line count;
  * unresolved/conflict line count.
* The operator can open count details.
* Count details show all lines with:

  * SKU;
  * storage location;
  * system quantity;
  * counted quantity;
  * variance;
  * status;
  * applied transaction reference, when available.

## Functional Requirements

### Count Session

* The system must support creating an inventory count for one warehouse.
* The count must have a stable identity.
* The count must have a status.
* The count must record created timestamp.
* The count should record updated timestamp when changed.
* The count should optionally record completed timestamp.
* The count should optionally record cancelled timestamp.
* The count should support an optional reason or description.

### Count Line

* The system must support adding SKU/location lines to a count.
* The system must prevent duplicate SKU/location lines within the same count.
* The system must capture system quantity when the line is added.
* The system must capture expected balance version when the balance exists.
* The system must allow missing balance as system quantity `0`.
* The system must support counted quantity entry.
* The system must calculate variance.
* The system must support applying counted variance.
* The system must store the adjustment transaction id when a non-zero variance is applied.

### Inventory Effects

* Applying non-zero variance must update inventory through the existing adjustment transaction and ledger path.
* The adjustment must set the final balance quantity to counted quantity.
* The ledger entry must show before/after quantity and variance delta.
* The count process must not create manual move, transfer, receiving, shipping, or inter-warehouse artifacts.

### Concurrency

* A count line captures the inventory balance version when the line is added.
* Applying a line must compare the current inventory state with the captured state.
* If the current state changed, the apply operation must fail with conflict.
* Conflict must be safe and atomic.
* The operator must refresh/recount after conflict.

### Visibility

* Count details must remain visible even when related SKU or storage location references become inactive later.
* New count lines can only be added for active eligible references.

## UI Requirements

### Navigation

Add a new Inventory area page:

```text
Inventory -> Counts
```

### Count List Page

The list page should show count sessions with status and basic progress.

Minimum columns:

```text
Status
Warehouse
Created At
Completed At
Cancelled At
Lines
Applied Lines
Conflict Lines
Actions
```

Actions:

```text
Open
Cancel
```

### Count Details Page

The details page should show:

* warehouse;
* status;
* reason/description;
* created/completed/cancelled timestamps;
* lines grid;
* actions based on status.

Line grid columns:

```text
SKU
Storage Location
System Quantity
Counted Quantity
Variance
Status
Applied Transaction
Actions
```

Line actions:

```text
Enter Count
Apply
```

Count-level actions:

```text
Add Line
Complete Count
Cancel Count
```

### Add Line Dialog

The dialog should allow selecting:

* SKU;
* storage location.

The storage location lookup must be scoped to the count warehouse.

Transit locations must be excluded.

### Enter Count Dialog

The dialog should show:

* SKU;
* storage location;
* system quantity;
* base UoM;
* counted quantity input;
* calculated variance;
* optional comment.

### Apply Result

After applying a line, the UI should show whether:

* no variance existed and no transaction was created;
* variance was applied and an adjustment transaction was created;
* conflict occurred and recount is required.

## API Shape

Exact endpoint names may follow existing project conventions, but the feature should expose these operations:

```text
GET    /api/wms/inventory/counts
POST   /api/wms/inventory/counts
GET    /api/wms/inventory/counts/{inventoryCountId}
POST   /api/wms/inventory/counts/{inventoryCountId}/lines
POST   /api/wms/inventory/counts/{inventoryCountId}/lines/{lineId}/count
POST   /api/wms/inventory/counts/{inventoryCountId}/lines/{lineId}/apply
POST   /api/wms/inventory/counts/{inventoryCountId}/complete
POST   /api/wms/inventory/counts/{inventoryCountId}/cancel
```

## Error Semantics

Use existing project error conventions.

Expected semantics:

* Missing count: `404 Not Found`.
* Missing warehouse: `404 Not Found`.
* Missing SKU: `404 Not Found`.
* Missing storage location: `404 Not Found`.
* Inactive or ineligible warehouse/SKU/location: validation error.
* Duplicate line within the same count: conflict or validation error according to existing project conventions.
* Count already completed/cancelled: conflict.
* Line already applied: conflict.
* Apply with stale inventory balance version: conflict.
* Apply when expected missing balance now exists: conflict.
* Apply when existing balance disappeared: conflict.
* Apply with invalid counted quantity: validation error.

## Audit Requirements

The count document must provide process audit.

InventoryTransaction and InventoryLedgerEntry must provide inventory movement audit.

For non-zero variance:

* exactly one adjustment transaction should be created;
* exactly one ledger entry should be created;
* the ledger entry should show quantity delta equal to variance;
* the transaction should be linked back to the inventory count line if the data model supports it in MVP.

For zero variance:

* no inventory transaction should be created;
* no ledger entry should be created;
* the line should still be marked as applied.

## Data Model Expectations

The implementation is expected to introduce new persistent entities for the count process.

Minimum conceptual model:

```text
InventoryCount
  Id
  WarehouseId
  Status
  Reason
  CreatedAtUtc
  UpdatedAtUtc
  CompletedAtUtc
  CancelledAtUtc

InventoryCountLine
  Id
  InventoryCountId
  StockKeepingUnitId
  StorageLocationId
  SystemQuantity
  ExpectedBalanceVersion
  CountedQuantity
  VarianceQuantity
  Status
  Comment
  CountedAtUtc
  AppliedAtUtc
  AppliedInventoryTransactionId
```

The exact names may follow existing Myrmex conventions.

## Non-Functional Requirements

* Use UTC timestamps.
* Preserve existing Inventory module patterns.
* Keep the MVP small and vertical-slice oriented.
* Avoid introducing new abstractions unless necessary.
* Avoid changing existing adjustment/manual-move/transfer behavior unless required by integration.
* Avoid direct inventory balance mutation without transaction/ledger history.
* Avoid migrations beyond the count entities required by this feature.
* Keep tests SQL Server-backed where concurrency, rowversion, or relational behavior matters.
* Keep UI consistent with existing MudBlazor inventory pages.

## MVP Definition of Done

The MVP is complete when:

* count session can be created;
* line can be added;
* counted quantity can be entered;
* variance is calculated;
* zero variance can be applied without inventory transaction;
* non-zero variance can be applied through adjustment transaction and ledger;
* stale inventory state causes conflict;
* count can be completed when all lines are applied;
* count can be cancelled before completion;
* count list and details are available in the web UI;
* handler, endpoint, API client, and UI integration are covered by appropriate tests;
* excluded scope remains excluded.

## Spec Kit / Repository Notes

* GitHub issue: `#79`.
* Branch: `079-inventory-counting-mvp`.
* Stakeholder document: `StakeholderDocs\Wms\Inventory\079 Inventory Counting MVP.md`.
* Spec Kit feature directory should be `specs/079-inventory-counting-mvp`.
* For Myrmex, the numeric prefix of the Spec Kit directory must match the GitHub issue and branch prefix.
