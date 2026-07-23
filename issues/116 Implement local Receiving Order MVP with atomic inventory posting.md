# Implement local Receiving Order MVP with atomic inventory posting

## Context

Myrmex already has working WMS capabilities for:

* warehouses, zones, and storage locations;
* stock keeping units and base units of measure;
* inventory balances;
* inventory adjustments and initial counts;
* inventory ledger and transaction history;
* internal inventory transfers;
* inventory counting;
* server-driven list, search, sorting, and paging conventions;
* WMS WebApp pages and execution workflows.

The next warehouse capability is the first internal **Receiving** process.

Receiving must initially be implemented as a standalone local WMS workflow, independent of 1C and other external systems.

Existing integration issues:

* #105 Synchronize receiving orders from external systems;
* #107 Synchronize receiving status and actual quantities back to 1C.

must not be implemented as part of this issue. They will use the internal Receiving model introduced here.

The implementation must follow:

* `.specify/memory/constitution.md`;
* existing WMS vertical-slice conventions;
* existing inventory balance, transaction, ledger, transfer, and counting patterns;
* the simplest implementation that delivers a complete local Receiving outcome.

## Goal

Implement the first end-to-end local Receiving Order MVP:

```text
Create Receiving Order
→ define planned SKU lines
→ start receiving
→ record received quantities
→ complete receiving
→ atomically increase inventory at the receiving location
```

The result must be a usable local workflow through domain, persistence, application, API, shared contracts, and WebApp.

## Warehouse behavior

### Quantity rules

* All quantities are expressed in the SKU base unit of measure.
* No packaging or unit conversion is performed.
* Planned quantity must be greater than zero.
* Received quantity cannot be negative.
* A receive operation quantity must be greater than zero.
* Received quantity cannot exceed planned quantity.
* Inventory quantities must remain non-negative.

### Receiving Order lifecycle

The MVP supports exactly these statuses:

```text
Draft
→ InProgress
→ Completed
```

`Cancelled` is out of scope.

### Draft

A Receiving Order is created in `Draft`.

While in `Draft`:

* header fields may be changed;
* the complete planned line set may be replaced;
* lines may be added or removed through the draft editing workflow;
* planned quantities may be changed;
* no inventory effect exists.

A valid Draft must contain:

* a unique non-empty `Number`;
* an existing active Warehouse;
* an existing active receiving StorageLocation belonging to that Warehouse;
* at least one valid planned line;
* an existing active SKU for every line;
* a positive planned quantity for every line;
* no duplicate SKU lines.

The MVP may temporarily require the user to enter `Number`.

Do not introduce an automatic document-number generator, sequence table, numbering service, or other numbering infrastructure in this issue.

### Start Receiving

Starting a Receiving Order changes:

```text
Draft → InProgress
```

Starting is allowed only when the current header and complete planned line set remain valid.

On start:

* `StartedAtUtc` is set;
* the header becomes immutable;
* the planned line set becomes immutable;
* planned quantities become immutable.

A repeated start request for an already `InProgress` order may return the current order as a successful idempotent outcome.

Starting a `Completed` order must be rejected.

### Record received quantity

Received quantity may be recorded only while the order is `InProgress`.

The operation:

* identifies the line by `LineId`;
* accepts a positive quantity;
* increments `ReceivedQuantity`;
* rejects a resulting quantity greater than `PlannedQuantity`;
* updates the aggregate modification timestamp;
* does not change inventory.

Negative corrections, quantity reversal, replacement of the accumulated quantity, damaged quantities, excess receipt, and discrepancy workflows are out of scope.

### Complete Receiving

Completion changes:

```text
InProgress → Completed
```

Completion is allowed only when every line is fully received:

```text
ReceivedQuantity == PlannedQuantity
```

Completion must:

* post all received lines to inventory;
* increase inventory at the Receiving Order receiving location;
* create one Receiving inventory transaction containing one positive ledger entry per order line;
* save the Receiving Order, Inventory Balances, Inventory Transaction, and ledger entries atomically;
* set `InventoryTransactionId`;
* set `CompletedAtUtc`;
* make the order immutable.

No inventory effect may exist before completion.

Completion must be idempotent:

* an already completed order with its `InventoryTransactionId` must not post inventory again;
* a repeated completion request must return the current completed result;
* concurrent completion attempts must not duplicate inventory.

## Domain model

### ReceivingOrder

Add a `ReceivingOrder` aggregate root owned by the Receiving capability.

Expected fields:

```csharp
Id
Number
WarehouseId
ReceivingLocationId
Status
StartedAtUtc?
CompletedAtUtc?
InventoryTransactionId?
CreatedAtUtc
UpdatedAtUtc?
RowVersion
Lines
```

Requirements:

* `Number` is required and globally unique for the current MVP.
* Use the existing domain text normalization conventions.
* `ReceivingLocationId` references an existing `StorageLocation`.
* Do not create a separate staging-location entity or aggregate.
* `InventoryTransactionId` references the inventory transaction created during completion.
* Add optimistic concurrency through SQL Server `RowVersion`.
* State transitions and aggregate invariants must be expressed through aggregate behavior rather than public property mutation.

### ReceivingOrderLine

Add `ReceivingOrderLine` as a domain entity inside the Receiving Order aggregate and as a separately persisted EF entity.

Expected fields:

```csharp
Id
ReceivingOrderId
StockKeepingUnitId
PlannedQuantity
ReceivedQuantity
```

Requirements:

* a line is not an aggregate root;
* lines are owned and changed through `ReceivingOrder`;
* `ReceivedQuantity` starts at zero;
* duplicate SKU lines are prohibited;
* enforce uniqueness with a database constraint on:

```text
ReceivingOrderId + StockKeepingUnitId
```

Do not add a persisted line status. The current line state is derived from planned and received quantities.

### ReceivingOrderStatus

Add:

```csharp
internal enum ReceivingOrderStatus
{
    Draft = 1,
    InProgress = 2,
    Completed = 3
}
```

Persist using the existing WMS status conventions.

## Inventory posting

Receiving must not directly assign or independently maintain inventory quantities outside the existing Inventory model.

Reuse:

* `InventoryBalance`;
* Inventory Balance create/update validation;
* SKU/location balance uniqueness;
* `InventoryTransaction`;
* `InventoryLedgerEntry`;
* existing transaction boundaries;
* existing persistence exception mapping;
* optimistic concurrency handling.

### Inventory transaction type

Extend:

```csharp
InventoryTransactionType
```

with:

```csharp
Receiving
```

Do not represent Receiving as `Adjustment` or `Transfer`.

Receiving is a distinct warehouse operation:

```text
external physical source
→ receiving StorageLocation
→ positive SKU inventory
```

No fictitious source StorageLocation must be introduced.

### Receiving transaction factory

Add the minimal Inventory domain behavior required to create one Receiving transaction with multiple positive ledger entries.

Conceptually:

```csharp
InventoryTransaction.CreateReceiving(
    changes,
    reason,
    occurredAtUtc,
    out transaction);
```

Each change must contain the values required to produce a ledger entry:

```text
StockKeepingUnitId
StorageLocationId
QuantityDelta
BalanceBefore
BalanceAfter
```

Requirements:

* every quantity delta must be positive;
* every balance transition must be valid;
* one completed Receiving Order creates one Inventory Transaction;
* each Receiving Order line creates one ledger entry;
* do not introduce a generalized inventory engine, universal posting abstraction, polymorphic source-document model, or workflow framework.

### Balance updates

For each Receiving Order line during completion:

* find the Inventory Balance for the line SKU and receiving location;
* create a missing balance when allowed by the existing inventory eligibility rules;
* otherwise increase the existing balance by the received quantity;
* use domain behavior rather than direct property assignment;
* preserve SQL Server RowVersion concurrency behavior;
* handle a concurrent missing-balance creation through the existing unique SKU/location constraint pattern.

### Atomicity

The following changes must be included in one EF Core `SaveChangesAsync` transaction boundary:

* Receiving Order status and timestamps;
* Receiving Order `InventoryTransactionId`;
* Inventory Balance creation or quantity changes;
* Inventory Transaction;
* Inventory Ledger Entries.

Do not perform inventory posting from a domain-event handler.

A completion domain event is optional and must not be required to create the inventory effect.

### Completion idempotency

Use the Receiving Order itself as the idempotency boundary.

The completed-state invariant is:

```text
Status == Completed
InventoryTransactionId != null
CompletedAtUtc != null
```

Use:

* aggregate status;
* `InventoryTransactionId`;
* `ReceivingOrder.RowVersion`;
* one atomic save;
* appropriate database constraints.

Do not introduce generic idempotency-key infrastructure or external source identity in this issue.

## Persistence

Add EF Core configurations and migration for:

* Receiving Orders;
* Receiving Order Lines;
* relationships to Warehouse;
* relationship to receiving StorageLocation;
* relationships from lines to SKU;
* optional relationship to Inventory Transaction;
* status conversion;
* quantities and precision;
* `CreatedAtUtc`;
* `UpdatedAtUtc`;
* `StartedAtUtc`;
* `CompletedAtUtc`;
* SQL Server `RowVersion`.

Add constraints and indexes for at least:

* unique `ReceivingOrder.Number`;
* unique `(ReceivingOrderId, StockKeepingUnitId)`;
* Warehouse;
* ReceivingLocation;
* Status;
* InventoryTransactionId.

Use restrictive delete behavior consistent with existing WMS documents and inventory records.

Do not add cascade deletion of completed warehouse documents or inventory history.

## Application features

Implement vertical slices for:

### Create Receiving Order

Create a valid Draft with header and complete planned line set in one operation.

Do not require a sequence of separate add-line commands to construct the initial aggregate.

### Update Receiving Order Draft

Replace the editable Draft header and complete planned line set in one operation.

The operation is allowed only for `Draft`.

### Start Receiving Order

Validate and move a Draft to `InProgress`.

### Receive Receiving Order Line

Increment one line received quantity while `InProgress`.

### Complete Receiving Order

Validate full receipt and atomically post the result to Inventory.

### Get Receiving Order by ID

Return complete header, lines, quantities, status, concurrency version, timestamps, and inventory transaction reference.

### List Receiving Orders

Follow existing server-driven list conventions:

* search;
* filtering;
* sorting;
* paging.

At minimum support filtering by:

* Warehouse;
* Status.

Search should cover the Receiving Order number.

## Validation responsibilities

Keep validation at the narrowest appropriate layer.

### Domain validation

Include:

* required identifiers;
* normalized required Number;
* quantity invariants;
* duplicate line detection;
* state transitions;
* plan immutability after start;
* full receipt requirement for completion;
* immutable completed state.

### Application validation

Include:

* Warehouse existence and active state;
* StorageLocation existence and active state;
* receiving location ownership by the selected Warehouse;
* SKU existence and active state;
* inventory balance creation eligibility;
* concurrency version checks.

Do not copy external-system rules into the internal domain.

## Shared contracts

Add shared request and response contracts consistent with existing WMS conventions.

Expected contracts include:

```text
ReceivingOrderListRequest
ReceivingOrderListItem
ReceivingOrderDetails
ReceivingOrderLineDetails
ReceivingOrderStatusDetails
ReceivingOrderSortBy

CreateReceivingOrderRequest
UpdateReceivingOrderRequest
ReceiveReceivingOrderLineRequest
ReceivingOrderActionRequest
```

Requests that mutate an existing order must carry the expected aggregate version where required.

Do not expose internal domain entities or EF models.

## API

Add Receiving Order endpoints following the current WMS Minimal API conventions.

Expected surface:

```text
GET    /api/wms/receiving-orders
GET    /api/wms/receiving-orders/{id}

POST   /api/wms/receiving-orders
PUT    /api/wms/receiving-orders/{id}

POST   /api/wms/receiving-orders/{id}/start
POST   /api/wms/receiving-orders/{id}/lines/{lineId}/receive
POST   /api/wms/receiving-orders/{id}/complete
```

Requirements:

* use the existing command/query dispatcher;
* use current `ServiceResult` and Problem Details mappings;
* return not found, validation, state, uniqueness, and concurrency failures consistently with existing WMS endpoints;
* do not add a second endpoint convention.

## WebApp

Receiving documents may contain hundreds of lines. Creation, editing, and execution must therefore use full pages rather than a single document modal dialog.

### Receiving Order list page

Add a page such as:

```text
/wms/receiving-orders
```

Include:

* server-driven grid;
* search;
* Warehouse filter;
* Status filter;
* sorting;
* paging;
* create action;
* navigation to details and Draft editing.

Follow the existing WMS grid and request conventions.

### Receiving Order create/edit page

Add pages such as:

```text
/wms/receiving-orders/new
/wms/receiving-orders/{id}/edit
```

Include:

* document header;
* Warehouse selection;
* receiving StorageLocation selection;
* editable planned line table;
* SKU selection;
* planned quantity;
* add and remove line;
* duplicate SKU prevention;
* save the complete Draft.

The page must remain usable with hundreds of lines.

For the MVP:

* keeping Draft lines in page state is acceptable;
* local filtering of the current line set is acceptable;
* SKU selection may use a focused search dialog;
* the complete Draft may be saved in one request.

Do not implement:

* separate persistence request for every edited cell;
* generalized editable-document infrastructure;
* spreadsheet framework;
* bulk paste or file import;
* server-driven paging of an unsaved Draft line collection unless required by an existing proven project pattern.

### Receiving Order details/execution page

Add a page such as:

```text
/wms/receiving-orders/{id}
```

Include:

* document header;
* status;
* timestamps;
* planned, received, and remaining quantities;
* Start Receiving action;
* per-line Receive Quantity action;
* Complete Receiving action;
* inventory transaction reference after completion;
* refresh after successful mutations;
* clear concurrency-conflict handling.

A small modal dialog is acceptable for entering a received quantity for one line.

Do not use a modal dialog for creating or editing the complete Receiving Order.

## Concurrency and error handling

### Receiving Order concurrency

Use `ReceivingOrder.RowVersion` for:

* Draft update;
* Start;
* Receive Quantity;
* Complete.

Return a conflict result when the order has changed since it was loaded.

### Inventory concurrency

During completion, handle:

* Inventory Balance RowVersion conflict;
* concurrent creation of the same SKU/location balance;
* Receiving Order RowVersion conflict.

Do not silently overwrite another inventory operation.

Do not automatically retry the complete business operation inside the handler.

A failed atomic save must leave:

* the Receiving Order not completed;
* no partial balance changes;
* no partial ledger transaction.

## Logging

Add concise structured logs for important Receiving actions and outcomes:

* Create;
* Update Draft;
* Start;
* Receive Quantity;
* Complete;
* Conflict;
* Validation or state rejection where operationally useful.

Include relevant identifiers such as:

* ReceivingOrderId;
* line ID;
* WarehouseId;
* ReceivingLocationId;
* StockKeepingUnitId;
* quantity;
* InventoryTransactionId;
* outcome.

Do not add a new logging abstraction.

## Testing

Use the existing test infrastructure and conventions.

Do not create new testing infrastructure.

Cover at least:

### Domain behavior

* valid Draft creation;
* missing lines rejected;
* non-positive planned quantity rejected;
* duplicate SKU rejected;
* Draft update allowed only in Draft;
* Start transition;
* plan immutable after Start;
* receive quantity allowed only in progress;
* zero and negative receive quantity rejected;
* over-receipt rejected;
* incomplete completion rejected;
* completed order immutable.

### Application and persistence behavior

* Warehouse must exist and be active;
* receiving location must exist, be active, and belong to the Warehouse;
* SKU must exist and be active;
* list/search/filter/sort/paging behavior;
* missing Inventory Balance is created on completion;
* existing Inventory Balance is increased on completion;
* one Receiving transaction is created per completed order;
* one ledger entry is created per order line;
* no inventory effect exists before completion;
* transaction type is `Receiving`;
* repeated completion does not duplicate inventory;
* concurrent completion produces one posting;
* Inventory Balance concurrency conflict does not partially complete the order;
* duplicate order Number is mapped to the expected conflict;
* duplicate SKU database constraint is mapped consistently.

### API behavior

* successful create/update/start/receive/complete;
* invalid state responses;
* not found responses;
* validation responses;
* concurrency conflict responses.

### WebApp behavior

Use only verification consistent with the current WebApp testing approach. Do not introduce a new UI test framework.

## Out of scope

Do not implement:

* 1C integration;
* #105;
* #107;
* external document identity;
* external snapshots;
* import or synchronization;
* automatic document numbering;
* yearly numbering rules;
* suppliers or partners;
* purchase orders;
* ASN;
* dock or door scheduling;
* packaging;
* packaging levels;
* quantity conversion;
* LPN or handling units;
* lot or batch;
* expiry dates;
* serial numbers;
* quality states;
* quarantine;
* damaged goods;
* substitutions;
* shortage workflow;
* excess approval;
* partial completion;
* discrepancy approval;
* multiple receiving sessions;
* correction or reversal of received quantities;
* inventory posting before completion;
* putaway;
* automatic storage-location selection;
* capacity or physical-dimension validation;
* scanner or mobile workflow;
* printing;
* notifications;
* generalized workflow or state-machine framework;
* generalized inventory posting engine;
* generic source-document references;
* generic idempotency infrastructure;
* new testing infrastructure;
* performance benchmarks.

## Compatibility with future 1C synchronization

The internal Receiving domain must remain independent from 1C statuses and transport types.

Future synchronization must use stable external identity such as the 1C document `Ref_Key`, not the document number alone.

The fact that 1C document numbers may only be unique within a year must not be modeled in this issue.

For the current MVP:

* `ReceivingOrder.Number` remains globally unique;
* user entry is a temporary local creation mechanism;
* automatic numbering is deferred;
* future integration may retain the original external number and date separately from integration identity;
* #105 may define a deterministic internal number representation for imported orders without changing Receiving execution behavior.

## Acceptance criteria

* A user can create a local Receiving Order with a unique number, Warehouse, receiving StorageLocation, and planned SKU lines.
* A user can edit the header and complete planned line set while the order is Draft.
* A user can start a valid Receiving Order.
* The plan cannot be changed after receiving starts.
* A user can increment received quantities for individual lines.
* Over-receipt is rejected.
* The order cannot be completed until every line is fully received.
* No Inventory Balance changes occur before completion.
* Completion creates or updates all required Inventory Balances at the receiving location.
* Completion creates exactly one `Receiving` Inventory Transaction.
* The transaction contains one positive ledger entry for every Receiving Order line.
* The order, balances, transaction, and ledger entries are persisted atomically.
* Repeating completion does not duplicate inventory.
* Concurrent mutations return a consistent conflict instead of silently overwriting data.
* The Receiving Order list follows existing server-driven list conventions.
* Creation and Draft editing use a full page suitable for hundreds of lines.
* Receiving execution uses a full details page.
* The complete local workflow can be demonstrated without 1C or another external system.
* No functionality listed as out of scope is introduced.
