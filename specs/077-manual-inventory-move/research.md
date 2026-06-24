# Research: Manual Inventory Move

## Decision: Model the move as an operation, not a document

Implement `MoveInventoryBalance` as an explicit command coordinating existing balances and ledger records. Add no Manual Move entity and do not use `InventoryTransfer`.

**Rationale**: This is an ad-hoc completed relocation, not requested work with lifecycle or progress. Existing immutable ledger history is sufficient audit evidence.

**Alternatives considered**:

- Reuse `InventoryTransfer`: rejected because document creation and progress semantics are explicitly excluded.
- Add a manual-move table: rejected because it duplicates transaction history without a distinct lifecycle.

## Decision: Reuse InventoryTransaction.CreateTransfer

Create one existing `Transfer` transaction with exactly two existing ledger-entry children.

**Rationale**: The factory already validates negative source delta, positive destination delta, balanced total, reason length, and before/after arithmetic.

**Alternatives considered**:

- Add a manual-move transaction type: rejected because the requirement specifies `Transfer`.
- Construct entries directly in the handler: rejected because that bypasses established domain invariants.

## Decision: Retain zero source balances

Update source quantity and leave the balance persisted when a full move produces zero.

**Rationale**: This is the clarified business rule and preserves balance identity/version continuity.

**Alternatives considered**:

- Delete zero rows: rejected by clarification.
- Derive balances from ledger history: rejected as event-sourcing scope expansion.

## Decision: Use explicit source version and EF concurrency on both balances

Require/compare the source Base64 rowversion before mutation. Retain EF rowversion tracking for source and existing destination rows during save.

**Rationale**: Explicit validation protects the operator's observed source state. EF concurrency closes save-time races and implements conflict-and-retry for destination changes.

**Alternatives considered**:

- Source check only: rejected because destination updates could be overwritten.
- Add destination version to the request: rejected because the caller did not observe a destination balance row.
- Automatic retry: rejected because clarified behavior requires user retry against current state.

## Decision: Use the existing unique index for concurrent destination creation

Create an absent destination normally. Let the unique SKU/location index reject a competing insert.

**Rationale**: The database invariant is the authoritative race protection and naturally produces the required conflict.

**Alternatives considered**:

- Serializable locking: rejected as unnecessary complexity.
- Upsert-and-sum: rejected because both requests must not silently succeed.

## Decision: Use one EF Core save as atomicity boundary

Track source, destination, transaction, and entries in one `WmsDbContext` and save once.

**Rationale**: The relational save transaction provides the required all-or-nothing persisted outcome without a new abstraction.

**Alternatives considered**:

- Multiple saves and compensation: rejected because it creates partial-state risk.
- New unit-of-work abstraction: rejected because `WmsDbContext` already owns the unit of work.

## Decision: Validate eligibility in the move command

Require active SKU; active source/destination locations, types, and statuses; same warehouse; and non-transit location types. Do not require active Base UoM.

**Rationale**: The command must be authoritative. The approved rules explicitly name SKU and location eligibility; adding Base UoM activity would broaden scope.

**Alternatives considered**:

- Trust UI filters: rejected because API callers and stale UI state remain possible.
- Reuse adjustment create eligibility unchanged: rejected because it adds Base UoM activity and does not validate two regular locations.

## Decision: Keep lookup observational

Return an existing balance regardless of active SKU/location/type/status state.

**Rationale**: The read reports actual inventory state. Eligibility belongs to the write.

**Alternatives considered**:

- Hide inactive balances as not found: rejected because the balance exists.
- Return validation failure: rejected because operational eligibility is not a read concern.

## Decision: Reuse details projection and topology lookup

Use `ProjectDetailsData()` for lookup/result mapping. Search destinations through existing warehouse-scoped topology lookup with `SelectableOnly = true` and `ExcludeTransitTypes = true`, then remove the source location.

**Rationale**: These existing boundaries already own balance transport mapping and location search.

**Alternatives considered**:

- Add duplicate DTO/projection: rejected as unnecessary.
- Add inventory-specific destination search: rejected as duplicated topology behavior.

## Decision: Return a dedicated move result

Return both updated balance details, moved quantity, all before/after quantities, and occurrence time.

**Rationale**: The UI can display the authoritative result without reconstructing values or issuing extra reads.

**Alternatives considered**:

- Return balances only: rejected because explicit before/after values are required.
- Return transaction details: rejected because ledger investigation remains a separate workflow.

## Decision: Focus automation on owning layers

Automate SQL Server handler behavior, endpoint binding, and client transport. Use manual smoke validation for the repeated MudBlazor dialog/grid pattern.

**Rationale**: Concurrency, atomicity, and eligibility are the primary risks; current project guidance permits manual UI checks without component-test infrastructure.

**Alternatives considered**:

- Duplicate every business case through HTTP/client tests: rejected as low-value duplication.
- Introduce component-test infrastructure: rejected as disproportionate cross-cutting scope.

