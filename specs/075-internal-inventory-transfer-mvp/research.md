# Research: Internal Inventory Transfer MVP

## Decision: Derive execution pattern from nullable transit location

Use `InventoryTransfer.TransitStorageLocationId == null` for direct storage-to-storage transfer and `InventoryTransfer.TransitStorageLocationId != null` for internal-transit transfer.

**Rationale**: The stakeholder requirements and user constraints forbid persisted `TransferExecutionMode`. A nullable transit location distinguishes the two MVP execution patterns and prevents duplicate state.

**Alternatives considered**:

- Persist `TransferExecutionMode`: rejected by constraint and unnecessary duplication.
- Infer pattern per movement only: rejected because transfer-level validation must prevent mixing direct and transit movements.

## Decision: Do not persist movement type

Store `InventoryTransferMovement` as a physical fact: transfer id, line id, from location, to location, quantity, inventory transaction id, and occurrence time. Derive meaning from the from/to location categories.

**Rationale**: Persisted `MovementType` is explicitly out of bounds. Derivation avoids drift and supports future scanner workflows where scan order is an input concern, not movement identity.

**Alternatives considered**:

- Persist `Direct`, `Pick`, and `Place`: rejected by constraint and drift risk.
- Store scanner step state on movement: rejected because scanner workflow is out of scope and should be modeled separately later.

## Decision: Reuse InventoryTransaction and InventoryLedgerEntry

Extend `InventoryTransactionType` with `Transfer = 2`. Each transfer movement creates one transaction and two ledger entries: negative at the from location and positive at the to location.

**Rationale**: Inventory Adjustment Ledger already established the immutable transaction/entry model, and Inventory Ledger history reads it. Reuse keeps transfer visible through the same inventory history mechanism.

**Alternatives considered**:

- Add transfer-specific ledger tables: rejected as duplicate ledger infrastructure.
- Add source-reference fields to `InventoryTransaction`: rejected by user constraint; `InventoryTransferMovement.InventoryTransactionId` provides the transfer-to-ledger link.

## Decision: Keep transfer linkage on InventoryTransferMovement

`InventoryTransferMovement` stores the created `InventoryTransactionId`. `InventoryTransaction` remains generic and source-agnostic.

**Rationale**: This satisfies transfer-to-ledger traceability without coupling the general ledger transaction model to one document type.

**Alternatives considered**:

- Add nullable transfer fields to `InventoryTransaction`: rejected by explicit constraint and future source-document coupling.
- Add a generic source-document table now: rejected as speculative abstraction.

## Decision: Use one WmsDbContext save as default atomicity boundary

Movement commands create or update all required transfer, transaction, ledger-entry, and balance rows in one `WmsDbContext` unit of work and call `SaveChangesAsync` once.

**Rationale**: Existing adjustment behavior uses one context for balance and ledger effects. A single save keeps the implementation simple while meeting the observable atomicity requirement.

**Alternatives considered**:

- Add explicit transaction abstraction: rejected unless implementation discovers multiple saves or cross-context persistence.
- Use event sourcing for movements: rejected as out of scope.

## Decision: Extend storage-location type reference data

Add `InternalTransit` and `ExternalTransit` storage-location type values. Implement only `InternalTransit` behavior for this MVP.

**Rationale**: Internal transit is required for trolley-style movement. External transit is future-compatible reference data requested by stakeholders, while external transfer behavior is explicitly out of scope.

**Alternatives considered**:

- Model transit outside storage locations: rejected because balances and ledger entries already operate by storage location.
- Implement external transit now: rejected by scope.

## Decision: Use existing WebApp grid/dialog patterns

Add an Inventory Transfers page with server-driven list, create dialog, details dialog, and movement dialogs.

**Rationale**: Inventory Balance and Inventory Ledger pages already establish the Myrmex WebApp pattern for operational WMS pages. Reusing it limits UI risk and keeps supervisor workflows consistent.

**Alternatives considered**:

- Scanner/mobile UI: rejected by MVP scope.
- Generic document framework: rejected as broad abstraction not needed for the current WMS problem.

## Decision: Use risk-based tests at owning layers

Plan domain tests for transfer invariants and progress, handler/persistence tests for command effects, focused endpoint/API-client tests for contracts, and manual UI smoke validation.

**Rationale**: This follows Myrmex testing guidance and existing Inventory Balance/Ledger plans. The highest risks are domain state transitions and atomic balance/ledger effects.

**Alternatives considered**:

- Full duplicated matrix at every layer: rejected as costly and less useful.
- UI component automation now: rejected because no component-test infrastructure exists and quickstart smoke checks cover repeated UI patterns.
