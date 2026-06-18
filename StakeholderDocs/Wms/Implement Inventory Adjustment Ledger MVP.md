# Stakeholder Document: Implement Inventory Adjustment Ledger MVP

## Summary

Introduce the first immutable Inventory Ledger capability for Myrmex.

The initial vertical slice supports one business operation only:

```text
Inventory Adjustment
```

An Inventory Adjustment records a physically counted quantity for one SKU in one storage location.

The system must calculate the resulting quantity delta, record an immutable ledger transaction and entry, and update the current `InventoryBalance` snapshot atomically.

This feature establishes the foundation for future inventory movements, receipts, picks, shipments, transfers, and other stock operations without implementing those workflows now.

## Stakeholder Goal

A warehouse operator or administrator must be able to correct an inventory balance while preserving a durable, auditable record of:

* what quantity existed before the adjustment;
* what quantity was physically counted;
* how much the balance changed;
* why the adjustment was made;
* when the adjustment occurred.

The current balance and the ledger record must never diverge because of a partial save.

## Current Problem

The current Inventory Balance workflow directly replaces the stored quantity.

This does not preserve:

* the previous quantity;
* the adjustment delta;
* the reason for the change;
* a durable history of inventory changes;
* protection against concurrent edits based on stale data.

`InventoryBalance` currently represents only the latest materialized quantity. It must remain a current-state snapshot, while the new ledger becomes the durable history of inventory changes.

## Scope

The MVP includes:

* Inventory Adjustment as the only ledger-producing operation;
* immutable `InventoryTransaction`;
* immutable `InventoryLedgerEntry`;
* atomic ledger and balance persistence;
* optimistic concurrency using SQL Server `rowversion`;
* adjustment of an existing balance;
* creation of a missing balance from an expected zero state;
* successful no-op handling;
* required adjustment reason;
* API contract and endpoint;
* replacement of the current direct quantity-update semantics;
* focused automated tests for critical risks;
* EF Core mappings and database migration.

The MVP does not include a ledger-history UI.

## Domain Concepts

### InventoryBalance

`InventoryBalance` remains the current materialized quantity of one SKU in one storage location.

For this MVP:

```text
StockKeepingUnitId + StorageLocationId
```

continues to uniquely identify the logical balance.

`InventoryBalance` is not the source of historical truth.

### InventoryTransaction

`InventoryTransaction` is the aggregate root for one completed inventory operation.

For the MVP, the only supported transaction type is:

```text
Adjustment
```

An adjustment transaction contains exactly one ledger entry.

Future transaction types may contain multiple entries, but they are outside this feature.

### InventoryLedgerEntry

`InventoryLedgerEntry` is an immutable quantity change affecting one SKU in one storage location.

The MVP entry stores:

```text
StockKeepingUnitId
StorageLocationId
QuantityDelta
BalanceBefore
BalanceAfter
```

The following invariant must always hold:

```text
BalanceAfter = BalanceBefore + QuantityDelta
```

Ledger entries must not be edited or deleted after persistence.

An incorrect adjustment must be corrected through a new adjustment transaction.

## Adjustment Semantics

The user submits the absolute physically counted quantity:

```text
CountedQuantity
```

The user does not submit a signed delta.

The handler calculates:

```text
QuantityDelta = CountedQuantity - CurrentQuantity
```

Example:

```text
CurrentQuantity = 10
CountedQuantity = 7
QuantityDelta = -3
```

The ledger entry stores all three values:

```text
BalanceBefore = 10
QuantityDelta = -3
BalanceAfter = 7
```

## Balance Addressing

An adjustment is addressed by:

```text
StockKeepingUnitId + StorageLocationId
```

It must not require `InventoryBalanceId`.

This allows an adjustment to create a balance when the pair currently has no persisted balance row.

The existing unique database index on:

```text
StockKeepingUnitId + StorageLocationId
```

must remain the final protection against duplicate balance rows.

## Adjustment Request

The public request should carry:

```csharp
public sealed record AdjustInventoryBalanceRequest(
    Guid StockKeepingUnitId,
    Guid StorageLocationId,
    decimal CountedQuantity,
    string Reason,
    string? ExpectedBalanceVersion);
```

The exact record/property style may follow current `Myrmex.Shared` conventions.

### CountedQuantity

`CountedQuantity` is the absolute physical quantity observed by the user.

It must not be negative.

### Reason

`Reason` is required.

It must be trimmed and validated using current Myrmex validation conventions.

The MVP does not introduce a reason-code catalog. A textual reason is sufficient.

### ExpectedBalanceVersion

`ExpectedBalanceVersion` expresses the exact balance state expected by the client.

It is transported as a Base64-encoded SQL Server `rowversion`.

Strict semantics:

```text
ExpectedBalanceVersion is not null
→ the client expects an existing balance with exactly that version

ExpectedBalanceVersion is null
→ the client expects that no balance row currently exists
```

`null` must not mean “skip concurrency validation.”

An invalid Base64 value must produce a validation error, not an unhandled server error.

## Concurrency Rules

Add SQL Server `rowversion` concurrency protection to `InventoryBalance`.

Domain/persistence representation:

```csharp
byte[] RowVersion
```

EF Core configuration must use the provider-supported rowversion mapping.

Transport representation:

```text
Base64 string
```

The current rowversion must be included in `InventoryBalanceDetails` so the UI can submit it with an adjustment.

### Existing balance

When a balance exists and `ExpectedBalanceVersion` is provided:

* decode the expected version;
* compare it with the currently loaded version;
* reject an explicit mismatch;
* retain EF Core concurrency protection through `SaveChangesAsync`;
* translate `DbUpdateConcurrencyException` into the defined concurrency conflict.

The explicit comparison provides an early meaningful result.

The EF concurrency token remains the final protection against a race between loading and saving.

### Missing balance

When no balance exists and `ExpectedBalanceVersion` is null:

* treat current quantity as zero;
* calculate the delta from zero;
* create the balance;
* create the transaction and ledger entry;
* save everything atomically.

If another concurrent request creates the same SKU/location balance first, the unique-index violation must be translated into the same concurrency conflict semantics.

### State mismatch matrix

| Current database state             | ExpectedBalanceVersion | Result               |
| ---------------------------------- | ---------------------- | -------------------- |
| Balance exists and version matches | non-null               | Process adjustment   |
| Balance exists and version differs | non-null               | Concurrency conflict |
| Balance exists                     | null                   | Concurrency conflict |
| Balance does not exist             | null                   | Create from zero     |
| Balance does not exist             | non-null               | Concurrency conflict |

### Conflict result

All stale-state conflicts should use one public error concept:

```text
HTTP 409 Conflict
InventoryBalance.ConcurrencyConflict
```

This includes:

* explicit version mismatch;
* expected absence but balance exists;
* expected balance but it was removed;
* EF `DbUpdateConcurrencyException`;
* concurrent duplicate insertion of the SKU/location pair.

The UI should instruct the user to refresh and review the counted quantity.

The system must not automatically retry an absolute adjustment.

## No-Op Adjustment

When:

```text
CurrentQuantity == CountedQuantity
```

and the expected state/version is valid:

* return success;
* do not create `InventoryTransaction`;
* do not create `InventoryLedgerEntry`;
* do not update `InventoryBalance`;
* preserve the current rowversion.

A zero-delta ledger entry must not be created.

Recording that a physical count occurred without changing quantity belongs to a future Inventory Count or Cycle Count capability, not this MVP.

## Zero-Balance Policy

For this MVP, zero-quantity `InventoryBalance` rows remain persisted.

Therefore:

```text
CountedQuantity = 0
```

updates or creates a zero balance according to current Inventory Balance semantics.

The introduction of the ledger must not silently change the current zero-balance behavior.

Removing zero-balance snapshot rows is a separate future decision and feature.

## Validation and Eligibility

The adjustment must validate the current production rules for:

* SKU existence;
* SKU activity or eligibility;
* base unit-of-measure eligibility where currently required;
* storage-location existence;
* storage-location activity or eligibility;
* warehouse/topology relationships where currently required;
* non-negative counted quantity;
* required reason.

The feature must not invent conflicting eligibility rules.

The specification and implementation plan must inspect the current create/update handlers and document which rules are reused.

## Atomicity

The following changes must be committed atomically:

* creation of `InventoryTransaction`;
* creation of `InventoryLedgerEntry`;
* creation or update of `InventoryBalance`.

The database must never contain:

* a ledger entry without the corresponding balance change;
* a balance change without the corresponding ledger entry.

A single EF Core `SaveChangesAsync` may provide the transaction boundary when all changes are saved together and current repository behavior guarantees atomicity.

The plan must document whether an explicit database transaction is necessary. Do not introduce one automatically without a concrete need.

## Immutability

After persistence:

* `InventoryTransaction` cannot be edited or deleted;
* `InventoryLedgerEntry` cannot be edited or deleted;
* entry quantity, SKU, location, before value, and after value cannot change;
* corrections are represented by a new adjustment.

No update or delete endpoints for ledger records are included.

## Time Semantics

All timestamps use UTC according to current Myrmex conventions.

The transaction should distinguish, where useful:

```text
OccurredAtUtc
CreatedAtUtc
```

For the MVP, both may be set to the current UTC time when the adjustment is recorded.

Do not introduce backdated adjustments unless explicitly required by the specification.

## Account Model Direction

The first implementation supports storage-location inventory only.

Therefore, an MVP ledger entry may contain:

```text
StorageLocationId
```

directly.

However, future ledger design must remain extensible toward a general inventory-account concept supporting:

* storage locations;
* transfer/transit inventory;
* handling units or LPNs;
* other stock-holding accounts.

Do not implement `InventoryAccount` in this feature.

Do not introduce nullable foreign keys for future account types in the MVP schema.

## API Behavior

Introduce an explicit adjustment endpoint following current Minimal API and result conventions.

The exact route should be selected during specification/plan work, with a preferred direction such as:

```text
POST /api/wms/inventory/adjustments
```

or:

```text
POST /api/wms/inventory/balances/adjust
```

The operation is a business command, not a generic entity update.

The current direct quantity-update endpoint must not remain the primary way to alter stock after this feature.

The plan must define a safe compatibility approach:

* replace the UI and internal client usage;
* remove the obsolete endpoint if no external compatibility requirement exists; or
* temporarily retain it only when an explicit migration reason is documented.

Do not preserve duplicate mutation paths indefinitely.

## Response

A successful adjustment should return the current `InventoryBalanceDetails`, including the current Base64 rowversion.

For a changed balance, the response contains the new rowversion.

For a no-op, the response contains the unchanged rowversion.

A separate wrapper such as `WasAdjusted` is not required unless the specification finds a concrete UI requirement.

## UI Behavior

The current quantity-update workflow should become an adjustment workflow.

The UI must:

* show SKU and storage location as read-only context for an existing balance;
* show current quantity;
* accept the physically counted quantity;
* require a reason;
* submit the balance rowversion loaded with the balance;
* show a clear concurrency-conflict message;
* refresh current server data after success;
* preserve existing cancellation and error behavior.

Adjustment from a missing balance may reuse the create workflow or be exposed through a dedicated adjustment command. The specification must select one coherent user workflow.

Do not add a ledger-history page in this feature.

## Persistence

Add EF Core mappings and migration for:

* `InventoryTransaction`;
* `InventoryLedgerEntry`;
* `InventoryBalance.RowVersion`;
* relationships and indexes required by the ledger model.

Recommended relationships:

```text
InventoryTransaction 1 → many InventoryLedgerEntry
InventoryLedgerEntry → StockKeepingUnit
InventoryLedgerEntry → StorageLocation
```

For the MVP, an adjustment transaction contains one entry, but the persistence model may support multiple entries for future transaction types.

The ledger should support efficient future queries by:

* transaction;
* SKU;
* storage location;
* occurrence time.

Do not add speculative indexes without a concrete planned query or integrity requirement.

## Testing Approach

Follow Myrmex risk-based minimal testing guidance.

Protect the following critical risks:

### Domain

* transaction and ledger-entry immutability;
* `BalanceAfter = BalanceBefore + QuantityDelta`;
* negative resulting/count quantity rejection as applicable;
* required reason normalization/validation.

### Handler and persistence

* existing balance with matching version updates balance and creates one transaction/entry;
* entry stores correct before, delta, and after values;
* missing balance with null expected version creates balance and ledger atomically;
* no-op succeeds without ledger records or balance update;
* stale version returns concurrency conflict with no partial persistence;
* expected absence when balance exists returns conflict;
* expected balance when balance is absent returns conflict;
* concurrent duplicate insertion is translated to concurrency conflict where practical to test;
* zero counted quantity preserves the current zero-row policy;
* EF mapping and rowversion behavior use the real provider where required.

### Endpoint/client boundary

Add only focused coverage for:

* request/response contract serialization;
* Base64 version transport;
* route/body construction;
* Problem Details mapping for the concurrency conflict when not already protected by shared behavior.

Do not reproduce the full adjustment matrix at every architectural layer.

### UI

Use manual smoke validation unless a distinct UI behavior cannot be protected elsewhere.

## Observability

Operationally important failures should expose meaningful diagnostics without logging sensitive or excessive payload data.

At minimum, concurrency conflicts and unexpected persistence failures should be distinguishable through existing logging and error conventions.

Do not add a new observability framework.

## Acceptance Criteria

* An existing Inventory Balance can be adjusted using an absolute counted quantity.
* A missing Inventory Balance can be created through an adjustment from expected zero.
* The ledger stores immutable before, delta, and after quantities.
* Adjustment reason is required and persisted.
* Ledger and balance changes are atomic.
* A valid no-op succeeds without creating ledger records.
* Zero balances remain persisted for this MVP.
* `InventoryBalance` has SQL Server rowversion concurrency protection.
* Rowversion is transported as Base64.
* Nullable expected version uses strict existence semantics.
* Stale state produces `409 InventoryBalance.ConcurrencyConflict`.
* No automatic retry is performed.
* The current direct quantity-update UI is replaced by adjustment semantics.
* The successful response includes current balance details and rowversion.
* No ledger-history UI is implemented.
* Database migration is included.
* Focused automated tests protect the critical risks.
* Build and full test suite pass.
* Manual smoke testing confirms success, no-op, validation, zero quantity, and concurrency-conflict behavior.

## Out of Scope

This feature must not implement:

* Inventory Ledger history page;
* InventoryTransfer;
* intra-warehouse or inter-warehouse movement;
* receipt, pick, shipment, return, or reservation operations;
* InventoryAccount;
* LPN or handling units;
* lot, batch, serial-number, or expiration tracking;
* cycle-count workflow;
* backdated adjustments;
* user/actor identity integration;
* event sourcing;
* ledger rebuilding;
* automatic concurrency retry;
* zero-balance row deletion;
* StorageLocation hierarchy;
* Catalog or Topology refactoring;
* external accounting-style double-entry inventory.
