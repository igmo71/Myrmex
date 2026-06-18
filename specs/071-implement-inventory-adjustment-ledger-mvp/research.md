# Research: Inventory Adjustment Ledger MVP

## Decision: Use One Adjustment Command and Endpoint

Use one internal command, `AdjustInventoryBalance`, and one public endpoint, `POST /api/wms/inventory/adjustments`, for both existing-balance adjustments and missing-balance initialization.

**Rationale**: The approved specification requires one stock-mutation path. One command centralizes strict `ExpectedBalanceVersion` semantics, no-op behavior, missing-zero initialization, ledger creation, and concurrency mapping.

**Alternatives considered**:

- Keep direct create/update endpoints as compatibility paths: rejected because the specification forbids parallel stock-mutation mechanisms.
- Separate existing-adjust and initial-count endpoints: rejected because it duplicates expected-version and atomicity behavior.

## Decision: Keep InventoryTransaction as Aggregate Root

Model `InventoryTransaction` as the aggregate root for ledger history and `InventoryLedgerEntry` as immutable child entity. Keep `InventoryBalance` as a separate current-state snapshot aggregate.

**Rationale**: This matches stakeholder language, keeps the ledger as durable history, and avoids turning `InventoryBalance` into historical truth. The command coordinates both aggregate boundaries in one WMS application slice.

**Alternatives considered**:

- Make ledger entries independent aggregate roots: rejected because adjustment transaction consistency is the business boundary.
- Make entries children of `InventoryBalance`: rejected because future operations may affect multiple entries and balances.

## Decision: Use One EF Core Save as Atomicity Boundary

Persist balance and ledger changes through one `WmsDbContext.SaveChangesAsync` call when all affected entities are tracked together.

**Rationale**: EF Core wraps a single save in a database transaction for relational providers. The feature only needs one command-side persistence operation, so adding an explicit transaction abstraction would add complexity without a current need.

**Alternatives considered**:

- Explicit transaction for every adjustment: rejected unless implementation discovers multiple saves or another concrete repository constraint.
- Generic unit-of-work or transaction abstraction: rejected by project simplicity constraints.

## Decision: SQL Server Rowversion with Base64 Transport

Add `InventoryBalance.RowVersion` as `byte[]` and map it as SQL Server rowversion. Expose and accept rowversion as Base64 in public contracts.

**Rationale**: Stakeholder concurrency semantics depend on SQL Server rowversion. Base64 keeps the transport contract stable and JSON-friendly while preserving exact bytes.

**Alternatives considered**:

- String version stored in domain: rejected because persistence owns rowversion bytes.
- Timestamp-based versioning: rejected because it is weaker than SQL Server rowversion and conflicts with the stakeholder decision.

## Decision: Combine Explicit Version Check with EF Concurrency

For existing balances, decode and compare expected rowversion before mutation, then rely on EF Core concurrency during save.

**Rationale**: The explicit check gives clear early business feedback. EF concurrency protects the race between load/compare and save.

**Alternatives considered**:

- EF concurrency only: rejected because expected absence/existence mismatches and invalid Base64 need explicit handling.
- Explicit check only: rejected because it misses save-time races.

## Decision: Capability-Specific Concurrency Error

Return `409 Conflict` with public code `InventoryBalance.ConcurrencyConflict` for explicit version mismatch, expected existence mismatch, EF concurrency exception, and adjustment duplicate insert.

**Rationale**: The API and UI need one business error concept for stale absolute adjustments. The current generic `ServiceError.Conflict<TEntity>` code is not specific enough for this capability.

**Alternatives considered**:

- Reuse generic entity conflict code: rejected by stakeholder decision.
- Add many separate conflict codes: rejected because users need the same refresh-and-review action for all stale-state cases.

## Decision: Adjustment-Specific Duplicate Insert Translation

Translate SQL Server error numbers 2601 and 2627 for `UX_wms_inventory_balances_stock_keeping_unit_id_storage_location_id` to `InventoryBalance.ConcurrencyConflict` in the adjustment path.

**Rationale**: In the adjustment command, this duplicate insert means a concurrent request created the balance after the client expected absence. Other create flows should not be globally reclassified unless they share the adjustment semantics.

**Alternatives considered**:

- Globally change the unique-index mapper to `InventoryBalance.ConcurrencyConflict`: rejected because non-adjustment duplicate conflicts may have different semantics.
- Ignore duplicate insert and let a generic 500 surface: rejected because it violates the conflict contract.

## Decision: Missing vs Existing Eligibility Rules

Apply full current create eligibility rules only when no balance exists. Existing-balance adjustment only requires referenced records to still exist; inactive related records do not block correction.

**Rationale**: Missing-balance initialization creates new stock state and should honor current create eligibility. Existing referenced stock must remain correctable even if reference records are later inactive.

**Alternatives considered**:

- Apply full active eligibility to existing balances: rejected because it could trap stock in inactive references.
- Skip eligibility for missing balances: rejected because it would allow new stock state against ineligible references.

## Decision: Non-Ledger Success Cases

Existing-balance no-op returns unchanged details without balance update, timestamp change, rowversion change, or ledger. Missing-balance counted quantity zero creates the persisted zero balance and returns success without ledger.

**Rationale**: The ledger records material quantity changes. A missing zero initialization is current-state initialization, not a historical quantity change.

**Alternatives considered**:

- Create zero-delta ledger entries: rejected because it pollutes the adjustment ledger with non-changes.
- Skip missing-zero balance creation: rejected because the confirmed zero-row policy persists zero balances.

## Decision: Manual UI Smoke Validation

Use manual UI smoke validation for Blazor dialog behavior and focus automated tests on domain, handler/persistence, endpoint, and API-client boundaries.

**Rationale**: Existing guidance allows manual UI checks for simple repeated patterns when no component-test infrastructure exists. The meaningful business risks are protected below the UI.

**Alternatives considered**:

- Add component-test infrastructure: rejected as disproportionate for this MVP and a cross-cutting decision.
- No UI validation: rejected because the feature changes user-facing stock mutation flows.
