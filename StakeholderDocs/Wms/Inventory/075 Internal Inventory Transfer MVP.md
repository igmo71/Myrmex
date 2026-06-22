# Internal Inventory Transfer MVP

## Context

Myrmex WMS already supports Inventory Balance, Inventory Ledger, Inventory Adjustment Ledger, and server-driven Inventory Ledger history.

The next domain capability is Inventory Transfer.

Inventory Transfer is required to represent controlled movement of inventory between storage locations inside a warehouse. The first MVP is limited to internal transfer within one warehouse, but it must support two everyday warehouse execution patterns:

```text
Direct internal movement:
  Storage → Storage

Internal movement through transit:
  Storage → InternalTransit → Storage
```

The implementation must remain compatible with future barcode scanner execution, but scanner workflow itself is out of scope for this MVP.

This issue intentionally does not introduce external transfer, system warehouse `TRANSIT`, LPN, package-level tracking, batches, serials, expiry dates, reservations, discrepancies, movement cancellation, or scanner sessions.

---

## Problem

Current Inventory Balance shows the current quantity of SKU inventory by storage location. Inventory Ledger records historical inventory transactions.

However, there is no domain document that represents an intention and execution flow for moving inventory from one storage location to another.

A warehouse needs both:

1. A simple direct relocation scenario:

```text
A-01-01 → A-01-02
```

Example:

```text
The operator takes two packages from one storage location
and places them into a nearby storage location where three packages already exist.
```

2. A movement-through-transit scenario:

```text
A-01-01 → TROLLEY-001 → B-02-03
```

Example:

```text
The operator picks inventory from a source location into a trolley,
then later places it from the trolley into a destination location.
```

Both scenarios are inventory transfers. They should be represented through the same document and ledger mechanism.

The system must be able to answer:

* what was requested to move;
* what has already been picked from the source location;
* what has already been placed into the destination location;
* what quantity is currently in transit;
* whether the transfer is completed;
* which ledger transactions were created for each movement.

---

## Goal

Implement the first MVP for internal inventory transfer inside one warehouse.

The MVP must support:

* internal transfer document;
* multiple transfer lines;
* one source warehouse and one destination warehouse, both the same warehouse;
* optional transit storage location;
* direct movement from source storage location to destination storage location;
* movement from source storage location to internal transit location;
* movement from internal transit location to destination storage location;
* one Inventory Ledger transaction per committed movement;
* computed requested, picked, placed, and in-transit quantities;
* automatic completion after full placement;
* read-only movement history.

---

## Stakeholders

## Warehouse operator

The warehouse operator physically executes inventory movements.

The operator needs to:

* know from which source location inventory must be picked;
* know where inventory must be placed;
* optionally use a trolley/internal transit location;
* confirm direct movement from source to destination;
* confirm pick movement from source to transit;
* confirm place movement from transit to destination.

Future scanner-driven execution may support multiple physical workflows. The scan order must not be assumed by the domain model.

Examples:

```text
Nearby direct movement:
  scan source location
  scan destination location
  scan goods / enter quantity
  commit movement
```

```text
Distant direct movement:
  scan source location
  scan goods / enter quantity
  walk to destination
  scan destination location
  commit movement
```

```text
Movement through trolley:
  scan source location
  scan trolley
  scan goods / enter quantity
  commit pick movement

  later:
  scan trolley
  scan destination location
  scan goods / enter quantity
  commit place movement
```

For this MVP, scanner execution is not implemented.

---

## Warehouse supervisor / inventory manager

The supervisor needs to:

* create internal transfer documents;
* monitor transfer progress;
* see which quantities were requested;
* see which quantities were already picked from source locations;
* see which quantities are currently in transit;
* see which quantities were already placed into destination locations;
* review movement history;
* verify that ledger transactions were created.

---

## System / API consumers

The backend must provide a stable command-oriented API for:

* creating internal transfer documents;
* executing direct movement;
* executing pick movement to internal transit;
* executing place movement from internal transit;
* reading transfer details;
* listing transfers;
* displaying read-only movement history.

The API must not be designed as generic CRUD-only updates. Direct move, pick, and place are explicit domain operations.

---

## Development team

The implementation must provide a small but extendable vertical slice.

The design must support future features without implementing them now:

* barcode scanner execution;
* flexible scanner step order;
* scanner sessions;
* package-level scanning;
* LPN;
* external transfer;
* transfer through system warehouse `TRANSIT`;
* transfer discrepancies;
* movement correction flows.

---

## Scope

## Included in this MVP

This MVP includes only internal transfer inside one warehouse.

Rule:

```text
SourceWarehouseId == DestinationWarehouseId
```

Supported internal movement patterns:

```text
Storage → Storage
Storage → InternalTransit
InternalTransit → Storage
```

Included capabilities:

* create internal transfer document;
* create multiple transfer lines;
* optionally assign transit storage location to transfer header;
* execute direct movement from line source location to line destination location;
* execute pick movement from line source location to transfer transit location;
* execute place movement from transfer transit location to line destination location;
* create Inventory Transfer Movement for each committed movement;
* create Inventory Transaction for each committed movement;
* create two Inventory Ledger Entries for each committed movement;
* update Inventory Balance through the existing ledger/balance mechanism;
* calculate transfer line progress;
* complete transfer after full placement;
* display read-only movement history.

---

## Out of scope

This MVP does not include:

* external transfer;
* warehouse `TRANSIT`;
* `ExternalTransit` behavior;
* LPN;
* packages as tracked inventory objects;
* batch tracking;
* serial tracking;
* expiry date tracking;
* reservation;
* automatic source location selection;
* automatic destination location suggestion;
* discrepancies;
* over-pick / under-pick discrepancy flow;
* movement cancellation;
* movement correction;
* receiving integration;
* putaway integration;
* mobile scanner UI;
* barcode scanner device integration;
* scan sessions;
* package-level barcode scanning;
* automatic quantity calculation from scanned package barcodes;
* persisted scanner audit;
* route optimization;
* approval workflow.

---

## Design decision: optional transit location

`TransitStorageLocationId` is optional.

Suggested model:

```csharp
public Guid? TransitStorageLocationId { get; private set; }
```

The presence or absence of `TransitStorageLocationId` determines available movement patterns.

If `TransitStorageLocationId` is null:

```text
Direct internal transfer pattern is used.

Allowed movement:
  Storage → Storage
```

If `TransitStorageLocationId` is not null:

```text
Transit storage location must have type InternalTransit.

Allowed movements:
  Storage → InternalTransit
  InternalTransit → Storage
```

For this MVP, do not allow mixing direct and transit movements inside the same transfer document.

Rules:

```text
Transfer without TransitStorageLocationId:
  only Storage → Storage movements are allowed

Transfer with TransitStorageLocationId:
  only Storage → InternalTransit and InternalTransit → Storage movements are allowed
```

No persisted `TransferExecutionMode` is introduced.

Execution pattern is derived from data:

```text
TransitStorageLocationId == null:
  direct internal transfer

TransitStorageLocationId != null:
  internal transfer through transit
```

---

## Design decision: no persisted transfer type

`InventoryTransferType` is not persisted in this MVP.

Transfer scope is computed:

```text
SourceWarehouseId == DestinationWarehouseId → Internal transfer
SourceWarehouseId != DestinationWarehouseId → External transfer
```

This MVP only allows:

```text
SourceWarehouseId == DestinationWarehouseId
```

Any attempt to create external transfer must be rejected.

---

## Design decision: no persisted movement type

`MovementType` is not persisted.

Movement stores only the physical fact:

```text
FromStorageLocationId
ToStorageLocationId
Quantity
InventoryTransactionId
OccurredAtUtc
```

The meaning of movement is derived from source and destination storage location types.

Agreed movement meanings:

```text
Storage → Storage
  direct internal movement

Storage → InternalTransit
  pick to trolley / internal transit

InternalTransit → Storage
  place from trolley / internal transit

Storage → ExternalTransit
  dispatch to inter-warehouse transit

ExternalTransit → Receiving/Staging
  receive at destination warehouse
```

This MVP implements only:

```text
Storage → Storage
Storage → InternalTransit
InternalTransit → Storage
```

---

## Design decision: scanner-friendly execution

Inventory Transfer MVP must be designed to support future scanner-driven execution, but scanner workflow implementation is out of scope for this issue.

Scanner flow is an input mechanism. It is not the core domain model.

The domain model must not assume a fixed scan order.

Different physical workflows may scan source location, destination location, transit location, and goods in different sequences depending on warehouse layout and operating practice.

The domain fact remains:

```text
InventoryTransferMovement
InventoryTransaction
InventoryBalance update
```

A movement is created only when the system has enough validated information to commit an inventory fact:

```text
transfer line
from storage location
to storage location
quantity
occurred time
```

Future scanner layer may keep temporary workflow state such as:

```text
source scanned
goods scanned
destination pending
transit scanned
```

But this temporary state must not be persisted as `InventoryTransferMovement`.

Architecture rule:

```text
Scanner/UI flow
    ↓
Application command
    ↓
Domain validation
    ↓
InventoryTransferMovement
    ↓
InventoryTransaction
    ↓
InventoryBalance update
```

The MVP must not introduce scanner-specific domain state.

Do not add scanner-specific fields to `InventoryTransferMovement`, such as:

```text
ScannedSourceBarcode
ScannedDestinationBarcode
ScannedPackageBarcode
ScannerDeviceId
CurrentScanStep
```

If scanner audit is needed later, it should be modeled separately from the movement fact.

---

## Domain entities

## InventoryTransfer

Represents the transfer document.

Suggested properties:

```csharp
internal sealed class InventoryTransfer : AggregateRoot
{
    public Guid Id { get; private set; }

    public string Code { get; private set; }

    public Guid SourceWarehouseId { get; private set; }
    public Guid DestinationWarehouseId { get; private set; }

    public Guid? TransitStorageLocationId { get; private set; }

    public InventoryTransferStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    private readonly List<InventoryTransferLine> _lines = [];
    public IReadOnlyCollection<InventoryTransferLine> Lines => _lines;

    private readonly List<InventoryTransferMovement> _movements = [];
    public IReadOnlyCollection<InventoryTransferMovement> Movements => _movements;
}
```

MVP rules:

```text
SourceWarehouseId == DestinationWarehouseId

TransitStorageLocationId is optional

If TransitStorageLocationId is not null:
  transit location must belong to the same warehouse
  transit location must have type InternalTransit

If TransitStorageLocationId is null:
  only direct Storage → Storage movements are allowed
```

---

## InventoryTransferLine

Represents requested movement of one SKU from one source location to one destination location.

Suggested properties:

```csharp
internal sealed class InventoryTransferLine : EntityBase
{
    public Guid InventoryTransferId { get; private set; }

    public Guid StockKeepingUnitId { get; private set; }

    public Guid SourceStorageLocationId { get; private set; }
    public Guid DestinationStorageLocationId { get; private set; }

    public decimal RequestedQuantity { get; private set; }
}
```

Computed quantities:

```text
RequestedQuantity
PickedQuantity
PlacedQuantity
InTransitQuantity
RemainingToPickQuantity
RemainingToPlaceQuantity
```

Suggested formulas:

```text
PickedQuantity =
  sum movements for this line
  where FromStorageLocationId == SourceStorageLocationId

PlacedQuantity =
  sum movements for this line
  where ToStorageLocationId == DestinationStorageLocationId

InTransitQuantity =
  if TransitStorageLocationId is null:
    0
  else:
    sum movements where ToStorageLocationId == TransitStorageLocationId
    -
    sum movements where FromStorageLocationId == TransitStorageLocationId

RemainingToPickQuantity =
  RequestedQuantity - PickedQuantity

RemainingToPlaceQuantity =
  PickedQuantity - PlacedQuantity
```

For direct movement:

```text
Storage → Storage

PickedQuantity increases
PlacedQuantity increases
InTransitQuantity remains 0
RemainingToPlaceQuantity remains 0
```

For pick to transit:

```text
Storage → InternalTransit

PickedQuantity increases
PlacedQuantity unchanged
InTransitQuantity increases
RemainingToPlaceQuantity increases
```

For place from transit:

```text
InternalTransit → Storage

PickedQuantity unchanged
PlacedQuantity increases
InTransitQuantity decreases
RemainingToPlaceQuantity decreases
```

---

## InventoryTransferMovement

Represents an immutable committed physical movement fact.

Suggested properties:

```csharp
internal sealed class InventoryTransferMovement : EntityBase
{
    public Guid InventoryTransferId { get; private set; }
    public Guid InventoryTransferLineId { get; private set; }

    public Guid FromStorageLocationId { get; private set; }
    public Guid ToStorageLocationId { get; private set; }

    public decimal Quantity { get; private set; }

    public Guid InventoryTransactionId { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }
}
```

The movement must not store persisted `MovementType`.

The movement must not store scanner workflow state.

---

## StorageLocationType extension

`StorageLocationType` must be extended with:

```csharp
InternalTransit
ExternalTransit
```

For this issue:

* `InternalTransit` is used by internal transfer through transit;
* `ExternalTransit` is added only as a future-compatible value;
* no external transfer behavior is implemented.

---

## InventoryTransactionType extension

Existing transaction type:

```csharp
internal enum InventoryTransactionType
{
    Adjustment = 1
}
```

Must be extended with:

```csharp
internal enum InventoryTransactionType
{
    Adjustment = 1,
    Transfer = 2
}
```

`InventoryTransactionType.Transfer` means:

```text
This inventory transaction was created by Inventory Transfer.
```

It does not distinguish direct movement, pick movement, or place movement.

The concrete movement meaning is derived from `InventoryTransferMovement.FromStorageLocationId`, `InventoryTransferMovement.ToStorageLocationId`, and storage location types.

---

## InventoryTransferStatus

Suggested minimal statuses:

```text
Created
InProgress
Completed
```

Rules:

```text
Created:
  transfer exists and has no movements

InProgress:
  at least one movement exists
  and transfer is not completed

Completed:
  every line has PlacedQuantity == RequestedQuantity
  and every line has InTransitQuantity == 0
```

Cancellation is out of scope.

---

## Invariants

## Transfer-level invariants

1. `SourceWarehouseId` is required.
2. `DestinationWarehouseId` is required.
3. For this MVP, `SourceWarehouseId` must equal `DestinationWarehouseId`.
4. `TransitStorageLocationId` is optional.
5. If `TransitStorageLocationId` is specified:

   * transit location must belong to the transfer warehouse;
   * transit location must have type `InternalTransit`.
6. Transfer must contain at least one line.
7. Completed transfer is read-only for new movements.
8. Transfer scope is computed and not persisted.
9. Transfer execution mode is not persisted.
10. Direct and transit movement patterns must not be mixed inside the same transfer document in this MVP.

---

## Line-level invariants

1. `StockKeepingUnitId` is required.
2. `SourceStorageLocationId` is required.
3. `DestinationStorageLocationId` is required.
4. `RequestedQuantity` must be greater than zero.
5. Source location must belong to the transfer warehouse.
6. Destination location must belong to the transfer warehouse.
7. Source and destination locations must be different.
8. Source location must be a normal storage location.
9. Destination location must be a normal storage location.
10. Source location must not be `InternalTransit`.
11. Destination location must not be `InternalTransit`.
12. Picked quantity must never exceed requested quantity.
13. Placed quantity must never exceed picked quantity.
14. Placed quantity must never exceed requested quantity.
15. In-transit quantity must never be negative.

---

## Movement-level invariants

1. Movement quantity must be greater than zero.
2. Movement must belong to an existing transfer.
3. Movement must belong to an existing transfer line.
4. Movement transfer id must match line transfer id.
5. Movement must create exactly one `InventoryTransaction`.
6. Movement must reference the created `InventoryTransactionId`.
7. Movement is immutable after creation.
8. `OccurredAtUtc` is required.
9. Movement cannot be added to completed transfer.
10. Movement type is not persisted.
11. Movement meaning is derived from storage location types.

For direct transfer without transit location:

```text
Allowed movement:
  Storage → Storage

FromStorageLocationId must be line.SourceStorageLocationId
ToStorageLocationId must be line.DestinationStorageLocationId
```

For transfer with transit location:

```text
Allowed movements:
  Storage → InternalTransit
  InternalTransit → Storage
```

Pick movement rules:

```text
FromStorageLocationId must be line.SourceStorageLocationId
ToStorageLocationId must be transfer.TransitStorageLocationId
```

Place movement rules:

```text
FromStorageLocationId must be transfer.TransitStorageLocationId
ToStorageLocationId must be line.DestinationStorageLocationId
```

Quantity rules:

```text
Direct movement quantity must not exceed remaining quantity to pick.

Pick movement quantity must not exceed remaining quantity to pick.

Place movement quantity must not exceed current in-transit quantity.
```

---

## Inventory balance invariant

Any movement that decreases a storage location balance must not create negative balance.

If source balance is insufficient, the movement operation must be rejected.

This MVP assumes that Inventory Balance is the current source of truth for available quantity by SKU and storage location.

---

## Ledger semantics

Inventory Transfer does not directly mutate balances outside the existing inventory ledger/balance mechanism.

Each committed physical movement produces:

```text
1 InventoryTransferMovement
1 InventoryTransaction
2 InventoryLedgerEntry records
InventoryBalance updates through ledger/balance mechanism
```

`InventoryTransferMovement` is the transfer-specific execution fact.

`InventoryTransaction` is the inventory accounting fact.

The movement stores the reference to the corresponding ledger transaction:

```text
InventoryTransferMovement.InventoryTransactionId
```

No source-reference fields are required in `InventoryTransaction` for this MVP.

The source document can be resolved through:

```text
InventoryTransaction
  ← InventoryTransferMovement by InventoryTransactionId
  ← InventoryTransferLine
  ← InventoryTransfer
```

---

## Direct movement ledger semantics

Direct movement:

```text
FromStorageLocationId = TransferLine.SourceStorageLocationId
ToStorageLocationId   = TransferLine.DestinationStorageLocationId
Quantity              = moved quantity
```

Derived meaning:

```text
Storage → Storage
```

Inventory effect:

```text
source storage location quantity decreases
destination storage location quantity increases
```

Transfer progress effect:

```text
PickedQuantity increases
PlacedQuantity increases
InTransitQuantity remains 0
```

Ledger transaction:

```text
InventoryTransactionType.Transfer
```

Ledger entries:

```text
source location:
  QuantityDelta = -quantity

destination location:
  QuantityDelta = +quantity
```

---

## Pick movement ledger semantics

Pick movement:

```text
FromStorageLocationId = TransferLine.SourceStorageLocationId
ToStorageLocationId   = InventoryTransfer.TransitStorageLocationId
Quantity              = picked quantity
```

Derived meaning:

```text
Storage → InternalTransit
```

Inventory effect:

```text
source storage location quantity decreases
internal transit location quantity increases
```

Transfer progress effect:

```text
PickedQuantity increases
InTransitQuantity increases
PlacedQuantity unchanged
```

Ledger transaction:

```text
InventoryTransactionType.Transfer
```

Ledger entries:

```text
source location:
  QuantityDelta = -quantity

transit location:
  QuantityDelta = +quantity
```

---

## Place movement ledger semantics

Place movement:

```text
FromStorageLocationId = InventoryTransfer.TransitStorageLocationId
ToStorageLocationId   = TransferLine.DestinationStorageLocationId
Quantity              = placed quantity
```

Derived meaning:

```text
InternalTransit → Storage
```

Inventory effect:

```text
internal transit location quantity decreases
destination storage location quantity increases
```

Transfer progress effect:

```text
PlacedQuantity increases
InTransitQuantity decreases
```

Ledger transaction:

```text
InventoryTransactionType.Transfer
```

Ledger entries:

```text
transit location:
  QuantityDelta = -quantity

destination location:
  QuantityDelta = +quantity
```

---

## Atomicity rule

Each movement operation must be atomic.

Direct move, pick, and place commands must perform the following in one application operation and one database transaction:

```text
1. validate transfer
2. validate line
3. validate locations
4. validate allowed movement pattern
5. validate available balance
6. create InventoryTransaction with type Transfer
7. create two InventoryLedgerEntry records
8. update InventoryBalance records
9. create InventoryTransferMovement with InventoryTransactionId
10. update InventoryTransfer status if needed
11. save changes
```

The system must not allow:

```text
InventoryTransaction exists without InventoryTransferMovement
```

or:

```text
InventoryTransferMovement exists without InventoryTransaction
```

---

## API expectations

The API should be command-oriented.

## Create transfer

Conceptual endpoint:

```http
POST /inventory-transfers
```

Payload shape:

```json
{
  "sourceWarehouseId": "...",
  "destinationWarehouseId": "...",
  "transitStorageLocationId": null,
  "lines": [
    {
      "stockKeepingUnitId": "...",
      "sourceStorageLocationId": "...",
      "destinationStorageLocationId": "...",
      "requestedQuantity": 10
    }
  ]
}
```

For transfer through transit:

```json
{
  "sourceWarehouseId": "...",
  "destinationWarehouseId": "...",
  "transitStorageLocationId": "...",
  "lines": [
    {
      "stockKeepingUnitId": "...",
      "sourceStorageLocationId": "...",
      "destinationStorageLocationId": "...",
      "requestedQuantity": 10
    }
  ]
}
```

Expected behavior:

* creates transfer;
* creates lines;
* validates internal transfer scope;
* validates optional transit location;
* validates source/destination locations;
* validates positive requested quantities;
* initial status is `Created`.

---

## Get transfer details

Conceptual endpoint:

```http
GET /inventory-transfers/{transferId}
```

Response should include:

```text
Transfer header
Transfer status
Computed transfer scope
Transit location, if specified
Lines
  SKU
  Source location
  Destination location
  RequestedQuantity
  PickedQuantity
  PlacedQuantity
  InTransitQuantity
  RemainingToPickQuantity
  RemainingToPlaceQuantity
Movements
  OccurredAtUtc
  From location
  To location
  Quantity
  Derived movement meaning
  InventoryTransactionId
```

---

## List transfers

Conceptual endpoint:

```http
GET /inventory-transfers
```

Expected capabilities:

* server-driven paging;
* deterministic sorting;
* filters.

Suggested filters:

```text
warehouse
status
created date range
transfer code
source location
destination location
sku
has transit location
```

---

## Direct move transfer line

Conceptual endpoint:

```http
POST /inventory-transfers/{transferId}/lines/{lineId}/move
```

MVP payload:

```json
{
  "quantity": 5
}
```

The system derives locations:

```text
from = line.SourceStorageLocationId
to   = line.DestinationStorageLocationId
```

Allowed only when:

```text
transfer.TransitStorageLocationId is null
```

The command must be compatible with future scanner flow, where quantity may be resolved from scanned SKU/package barcodes.

---

## Pick transfer line to transit

Conceptual endpoint:

```http
POST /inventory-transfers/{transferId}/lines/{lineId}/pick
```

MVP payload:

```json
{
  "quantity": 5
}
```

The system derives locations:

```text
from = line.SourceStorageLocationId
to   = transfer.TransitStorageLocationId
```

Allowed only when:

```text
transfer.TransitStorageLocationId is not null
```

---

## Place transfer line from transit

Conceptual endpoint:

```http
POST /inventory-transfers/{transferId}/lines/{lineId}/place
```

MVP payload:

```json
{
  "quantity": 5
}
```

The system derives locations:

```text
from = transfer.TransitStorageLocationId
to   = line.DestinationStorageLocationId
```

Allowed only when:

```text
transfer.TransitStorageLocationId is not null
```

---

## UI expectations

The first UI does not need to implement mobile scanner flow.

## Transfer list page

Should show:

```text
Transfer code
Warehouse
Status
Created date
Transit location, if specified
Total requested quantity
Total picked quantity
Total placed quantity
Total in-transit quantity
```

## Transfer details page

Should show:

```text
Transfer header
Source warehouse
Destination warehouse
Transit location, if specified
Status
Lines
Movement history
```

Line table should show:

```text
SKU code/name
Source location
Destination location
Requested quantity
Picked quantity
Placed quantity
In-transit quantity
Remaining to pick
Remaining to place
```

Available actions:

For transfer without transit location:

```text
Move
```

For transfer with transit location:

```text
Pick
Place
```

Actions should not be available for completed transfers.

The UI may prevent obvious invalid operations, but backend validation remains authoritative.

---

## Movement history UI

Movement history is read-only.

Movement row should show:

```text
Occurred at
SKU
From location
To location
Quantity
Derived movement meaning
Inventory transaction reference
```

No edit or delete action is included.

---

## User stories

## User Story 1: Create internal inventory transfer without transit

As a warehouse supervisor,
I want to create an internal transfer without transit location,
so that warehouse operators can directly move inventory from one storage location to another.

Acceptance:

```text
Given valid internal transfer data without transit location
When the transfer is created
Then InventoryTransfer is created
And transfer lines are created
And transfer status is Created
And only direct Storage → Storage movements are allowed
```

---

## User Story 2: Create internal inventory transfer with transit

As a warehouse supervisor,
I want to create an internal transfer with transit location,
so that warehouse operators can move inventory through trolley/internal transit.

Acceptance:

```text
Given valid internal transfer data with InternalTransit location
When the transfer is created
Then InventoryTransfer is created
And transfer lines are created
And transfer status is Created
And only Storage → InternalTransit and InternalTransit → Storage movements are allowed
```

---

## User Story 3: Execute direct movement

As a warehouse operator,
I want to confirm that inventory was directly moved from source location to destination location,
so that the system reflects the completed physical movement.

Acceptance:

```text
Given a transfer line without transit location
And the line has remaining quantity to move
When the operator moves quantity
Then InventoryTransferMovement is created
And InventoryTransaction is created
And source balance decreases
And destination balance increases
And PickedQuantity increases
And PlacedQuantity increases
And InTransitQuantity remains 0
```

---

## User Story 4: Pick inventory from source location to internal transit

As a warehouse operator,
I want to confirm that inventory was picked from source location to internal transit location,
so that the system reflects that inventory is no longer in the source location and is now in transit.

Acceptance:

```text
Given a transfer line with transit location
And the line has remaining quantity to pick
When the operator picks quantity
Then InventoryTransferMovement is created
And InventoryTransaction is created
And source balance decreases
And internal transit balance increases
And PickedQuantity increases
And InTransitQuantity increases
```

---

## User Story 5: Place inventory from internal transit to destination location

As a warehouse operator,
I want to confirm that inventory was placed from internal transit location to destination location,
so that the system reflects that inventory has reached its destination.

Acceptance:

```text
Given a transfer line with positive in-transit quantity
When the operator places quantity
Then InventoryTransferMovement is created
And InventoryTransaction is created
And internal transit balance decreases
And destination balance increases
And PlacedQuantity increases
And InTransitQuantity decreases
```

---

## User Story 6: View transfer progress

As a warehouse supervisor,
I want to view requested, picked, placed, and in-transit quantities,
so that I can understand the current execution state of the transfer.

Acceptance:

```text
Given transfer movements exist
When transfer details are opened
Then calculated quantities are shown for every line
```

---

## User Story 7: Complete transfer automatically

As a warehouse supervisor,
I want transfer to become completed after all requested quantities are placed,
so that document status reflects physical completion.

Acceptance:

```text
Given all transfer lines have PlacedQuantity == RequestedQuantity
And all transfer lines have InTransitQuantity == 0
When the final movement is committed
Then transfer status becomes Completed
```

---

## User Story 8: Preserve scanner-friendly execution boundary

As a development team,
we want movement operations to be modeled as explicit application commands,
so that future scanner workflows can resolve scanned barcodes into the same movement model.

Acceptance:

```text
Given this MVP does not implement scanner flow
When move/pick/place commands are implemented
Then they must not be generic CRUD updates
And they must create the same movement and ledger facts that scanner flow will create later
And they must not assume fixed scan order
```

---

## Acceptance criteria

## Create valid direct transfer

Given valid internal transfer data without transit location,
when the user creates a transfer,
then the system creates `InventoryTransfer` with lines and status `Created`.

---

## Create valid transit transfer

Given valid internal transfer data with transit location of type `InternalTransit`,
when the user creates a transfer,
then the system creates `InventoryTransfer` with lines and status `Created`.

---

## Reject external transfer

Given `SourceWarehouseId != DestinationWarehouseId`,
when the user tries to create transfer in this MVP,
then the system rejects the request.

---

## Reject invalid transit location

Given a transit location that is not type `InternalTransit`,
when the user creates a transfer with transit location,
then the system rejects the request.

---

## Reject line from another warehouse

Given a source or destination location from another warehouse,
when the user creates a transfer,
then the system rejects the request.

---

## Direct move quantity

Given a direct transfer line with requested quantity 10,
when the user moves 4,
then the system creates one movement and one inventory transaction,
and calculated quantities become:

```text
Requested = 10
Picked = 4
Placed = 4
InTransit = 0
```

---

## Prevent direct over-move

Given a direct transfer line with requested quantity 10 and already moved quantity 8,
when the user tries to move 3 more,
then the system rejects the operation.

---

## Prevent direct move with insufficient balance

Given source location has insufficient SKU balance,
when the user tries to move quantity greater than available balance,
then the system rejects the operation.

---

## Pick quantity

Given a transit transfer line with requested quantity 10,
when the user picks 4,
then the system creates one movement and one inventory transaction,
and calculated quantities become:

```text
Requested = 10
Picked = 4
Placed = 0
InTransit = 4
```

---

## Prevent over-pick

Given a transfer line with requested quantity 10 and already picked quantity 8,
when the user tries to pick 3 more,
then the system rejects the operation.

---

## Prevent pick with insufficient balance

Given source location has insufficient SKU balance,
when the user tries to pick quantity greater than available balance,
then the system rejects the operation.

---

## Place quantity

Given a transfer line with picked quantity 4 and placed quantity 0,
when the user places 2,
then the system creates one movement and one inventory transaction,
and calculated quantities become:

```text
Requested = 10
Picked = 4
Placed = 2
InTransit = 2
```

---

## Prevent over-place

Given a transfer line with in-transit quantity 2,
when the user tries to place 3,
then the system rejects the operation.

---

## Prevent wrong movement pattern

Given a transfer without transit location,
when the user tries to execute pick or place operation,
then the system rejects the operation.

Given a transfer with transit location,
when the user tries to execute direct move operation,
then the system rejects the operation.

---

## Complete direct transfer

Given all direct transfer lines have placed quantity equal to requested quantity,
when the final direct movement is created,
then transfer status becomes `Completed`.

---

## Complete transit transfer

Given all transit transfer lines have placed quantity equal to requested quantity,
and all lines have in-transit quantity equal to zero,
when the final place movement is created,
then transfer status becomes `Completed`.

---

## Completed transfer is read-only

Given a completed transfer,
when the user tries to add another movement,
then the system rejects the operation.

---

## Movement history is immutable

Given movements exist,
when transfer details are opened,
then movement history is shown as read-only.

---

## Scanner workflow is not implemented

Given this MVP,
when transfer execution is implemented,
then it must not introduce scan sessions, scanner device integration, package-level scanning, or fixed scanner step order.

---

## Open questions

1. Should `TransitStorageLocationId` remain optional?

   MVP recommendation: yes. This allows both direct and transit internal transfer without future schema rework.

2. Should transfer line source and destination locations be mandatory at creation time?

   MVP recommendation: yes. Automatic location suggestion is out of scope.

3. Should direct and transit movements be allowed in the same transfer document?

   MVP recommendation: no for this MVP. Do not mix execution patterns inside one transfer.

4. Should pick/direct move operation check current available balance?

   MVP recommendation: yes. Source balance must not become negative.

5. Should transfer status be persisted or fully computed?

   MVP recommendation: persist status for efficient list filtering and workflow visibility. Quantities should be computed from movements.

6. Should `InventoryTransferMovement` store `StockKeepingUnitId` directly?

   MVP recommendation: no. Derive SKU from transfer line. Add denormalized SKU only later if query performance requires it.

7. Should movement meaning be shown in UI?

   MVP recommendation: yes, but computed from storage location types. Do not persist `MovementType`.

8. Should partial direct move, pick, and place be allowed?

   MVP recommendation: yes. Partial execution is required for realistic warehouse operations.

9. Should multiple lines with the same SKU/source/destination be allowed?

   MVP recommendation: yes for now. Consolidation can be considered later.

10. Should one transfer line support multiple destination locations?

MVP recommendation: no. Use multiple lines.

11. Should completed transfers be immutable?

MVP recommendation: yes for this MVP.

12. Should `ExternalTransit` be added now?

MVP recommendation: yes as storage location type value only. No external transfer behavior is included.

13. Should transfer movements appear in the existing Inventory Ledger UI?

MVP recommendation: yes. They should appear as normal inventory transactions with transaction details identifying transfer movement as the source.

14. Should scanner audit be stored later?

MVP recommendation: possibly, but not in `InventoryTransferMovement`. Scanner audit should be modeled separately if needed.

15. Should scanner flow define a fixed scan order?

MVP recommendation: no. Scan order is a future application/mobile workflow concern and may vary by warehouse layout and physical process.

---

## Implementation boundary

This issue is complete when the system can:

1. create internal inventory transfer document without transit location;
2. create internal inventory transfer document with transit location;
3. create multiple transfer lines;
4. validate same-warehouse internal transfer;
5. validate optional internal transit location;
6. execute direct movement from source storage location to destination storage location;
7. execute pick movement from source storage location to internal transit location;
8. execute place movement from internal transit location to destination storage location;
9. create one inventory transaction per committed movement;
10. create two ledger entries per committed movement;
11. update inventory balances through ledger/balance mechanism;
12. compute requested, picked, placed, and in-transit quantities;
13. complete transfer after full placement;
14. display transfer details;
15. display read-only movement history;
16. keep implementation compatible with future scanner-driven execution without implementing scanner workflow.
