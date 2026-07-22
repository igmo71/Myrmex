# Phase 0 Research: Local Receiving Order MVP

**Date**: 2026-07-22

**Status**: Complete; all planning decisions resolved

## Decision: Add a Receiving-Owned Vertical Slice Inside the WMS Module

**Decision**: Add `Myrmex.Modules.Wms/Receiving` with Domain, Features, and Endpoints subtrees. Receiving owns `ReceivingOrder`, `ReceivingOrderLine`, lifecycle rules, orchestration, and its public routes. Continue using the existing WMS DbContext/schema, command/query dispatchers, Shared contracts, authorization policy, and WebApp project.

**Rationale**: Receiving is a distinct warehouse capability and document owner, while Topology already owns warehouses/locations and Inventory already owns balances/history. A new project or module boundary would add deployment and dependency overhead without changing the current local outcome.

**Alternatives considered**:

- Put Receiving under Inventory: rejected because Receiving owns a warehouse document and process, not authoritative inventory state.
- Add a separate assembly/database: rejected as unnecessary for the MVP.
- Add a generic document/workflow layer: rejected because only one three-state workflow is required.

## Decision: Model One Aggregate With Separately Persisted Lines

**Decision**: Implement `ReceivingOrder : AggregateRoot` with a private line collection and `ReceivingOrderLine : EntityBase`. Normalize Number with `DomainText.NormalizeCode`, limit it with `DomainTextLengths.Code`, use base-unit `decimal(18,4)` quantities, and express all state changes through aggregate behavior. Persist status as a string, consistent with existing WMS documents.

**Rationale**: `InventoryTransfer` and `InventoryCount` establish aggregate/entity, normalization, timestamp, status, and private collection patterns. One aggregate is the natural consistency boundary for plan uniqueness, lifecycle, full receipt, and idempotency.

**Alternatives considered**:

- Make lines aggregate roots: rejected because the plan, status, and concurrency boundary belong to the order.
- Store line status: rejected because it is derivable from planned and received quantities.
- Use a generalized state machine: rejected because three explicit transitions are sufficient.

## Decision: Preserve Retained Draft Line IDs During Complete-Plan Reconciliation

**Decision**: An update materializes and validates the full proposed Draft before mutation. Every non-null LineId must be unique and belong to the loaded aggregate. Retained lines update in place, omitted lines are removed, and null-ID lines are created. The final proposed set must have at least one line, unique SKUs, and positive planned quantities. The aggregate returns removed lines so the handler can mark them for explicit deletion.

**Rationale**: This implements the clarification exactly, preserves stable identities, and prevents partial in-memory mutation when a later submitted line is invalid.

**Alternatives considered**:

- Delete and recreate all lines: rejected because retained IDs must remain stable.
- Reconcile by SKU: rejected because LineId is the explicit identity and a retained line may change SKU while Draft.
- Persist each cell independently: rejected because the contract is one complete plan.

## Decision: Use Aggregate-Level Rowversion Only

**Decision**: Add SQL Server RowVersion only to `ReceivingOrder`. Encode it as Base64 `OrderVersion` in read models and parse exactly eight bytes in a Receiving-specific version helper. Draft update, Draft delete, Start, Receive, and Complete require the expected order version for an actual mutation. Every child mutation touches the order so the parent row changes; EF rowversion catches races between validation and save.

Idempotent reads of current state are ordered before version rejection: Start on an already InProgress order returns current details; Complete on a valid already Completed order returns current details. Lines have no independent version.

**Rationale**: The clarified aggregate boundary means any line change makes the complete order representation stale. This is simpler and safer than combining parent and line tokens.

**Alternatives considered**:

- Add line rowversions: rejected by the clarification and because it permits a stale aggregate view.
- Rely only on client-side checks: rejected because concurrent server mutations must not overwrite one another.
- Auto-merge stale requests: rejected because the business intent is unknown.

## Decision: Add One Seeded `RECEIVING` StorageLocationType

**Decision**: Add one active system `StorageLocationType` with technical code `RECEIVING`, a stable `WmsSeedIds` identifier, and the established `HasData`/migration pattern. Do not reuse `DOCK`: its current description covers both receiving and shipping and the specification excludes dock behavior. Do not add a capability flag, separate ReceivingLocation entity, or multiple Receiving categories.

Add at least one new local demo location using `RECEIVING` so the workflow is demonstrable. Keep existing DOCK demo identities unchanged rather than silently reclassifying them.

**Rationale**: Existing special location semantics are represented with seeded system type rows and exact code comparisons. One new type makes Receiving unambiguous while preserving the existing topology model.

**Alternatives considered**:

- Reuse `DOCK`: rejected because it is ambiguous and would model an explicitly excluded concept.
- Reuse `STAGING`: rejected because staging is out of scope and not equivalent to receipt.
- Add `CanReceive` or a generalized capability collection: rejected as unnecessary infrastructure.
- Reclassify existing DOCK demo rows: rejected because demo seeding treats existing type mismatches as identity conflicts.

## Decision: Reuse Existing Lookup and Validate Receiving Eligibility Server-Side

**Decision**: The WebApp calls the existing warehouse-scoped StorageLocation lookup with `SelectableOnly=true` and `StorageLocationTypeCode=RECEIVING`. This already restricts results to the selected Warehouse and active location/type/status. Create, Update Draft, and Start independently load and validate the active Warehouse, active location, warehouse ownership, active status, active exact Receiving type, and line SKU eligibility.

For missing balance creation during completion, reuse the existing Inventory Balance eligibility rules for active SKU/base UOM and active location/type/status. Use set-based loading for 300 distinct lines and apply the same narrow rules without per-line database queries.

**Rationale**: Lookup filtering improves UX but is not a security or correctness boundary. Server validation prevents crafted requests and catches topology changes between editing and Start.

**Alternatives considered**:

- Add a Receiving-specific location endpoint: rejected because the existing lookup already has the required filters.
- Trust the WebApp selection: rejected because clients can bypass it and eligibility may change.
- Add generalized location capabilities: rejected as out of scope.

## Decision: Persist Restrictive Relationships and Explicit Draft Deletion

**Decision**: Add restrictive foreign keys from orders to Warehouse, ReceivingLocation, and optional InventoryTransaction; from lines to order and SKU; and keep parent-to-lines delete behavior restrictive. Draft deletion loads order and lines, validates the expected order version, Draft state, null transaction/completion fields, and zero inventory effect, explicitly removes lines then the order, and calls SaveChanges once. The physical delete releases the unique Number.

**Rationale**: Existing WMS documents use restrictive relationships. Explicit dependent removal supports the clarified Draft-only delete without enabling cascade removal of completed documents or inventory history.

**Alternatives considered**:

- Cascade all order-line deletes: rejected because direct deletion could bypass the Draft-only invariant.
- Soft delete/archive: rejected by the clarification.
- Cancellation: rejected because the lifecycle supports exactly three statuses.

## Decision: Add Named Constraints and One Migration

**Decision**: Add unique indexes for normalized Number and `(ReceivingOrderId, StockKeepingUnitId)`, a unique filtered index for non-null InventoryTransactionId, and indexes for WarehouseId, ReceivingLocationId, Status, and CreatedAtUtc. Map duplicate Number and duplicate order/SKU violations through `WmsPersistenceExceptionMapper`; retain the existing balance-pair race detector. Generate one migration for the Receiving tables, constraints, relationships, rowversion, and `RECEIVING` seed.

**Rationale**: Domain checks provide immediate feedback; named database constraints close concurrency and integrity gaps and fit existing SQL Server exception mapping.

**Alternatives considered**:

- Application-only uniqueness: rejected because concurrent saves can pass both checks.
- Add a numbering table/service: rejected because Number remains user-entered.
- Add a generic source-document reference: rejected because the order directly references its one transaction.

## Decision: Create One Narrow Multi-Entry Receiving Transaction Factory

**Decision**: Extend `InventoryTransactionType` with `Receiving` and add `InventoryTransaction.CreateReceiving(changes, reason, occurredAtUtc, out transaction)`. Each feature-specific change contains SKU, receiving location, positive delta, balance before, and balance after. Require a non-empty set and positive deltas, and delegate entry consistency to existing `InventoryLedgerEntry.Create`.

**Rationale**: Inventory already owns transaction and entry validation. A narrow factory creates exactly one transaction with one entry per order line without pretending Receiving is an Adjustment or Transfer.

**Alternatives considered**:

- One transaction per line: rejected because one completed order must produce one transaction.
- A fictitious source location: rejected because the physical source is external.
- A universal posting engine: rejected because no second use case requires it.

## Decision: Complete Through One Save and No Posting Event

**Decision**: The completion handler bulk-loads existing balances for all line SKU/location pairs, creates eligible missing balances, increases existing balances through domain behavior, constructs the one Receiving transaction, completes the order with the transaction ID and common UTC timestamp, adds all new entities, and invokes exactly one `SaveChangesAsync`. Rely on EF Core's transaction for a multi-command save. Mirror the existing `MoveInventoryBalance` pattern by capturing tracked aggregate domain events before save and dispatching/clearing them only after a successful save; no event creates the Receiving inventory effect. Do not split validation/posting into separate saves.

**Rationale**: Existing transfer flows assemble balance, transaction, and document changes before saving. One save is the smallest boundary that guarantees all-or-nothing persistence.

**Alternatives considered**:

- Explicit multi-save transaction: rejected because the requirement is one SaveChanges boundary and EF already wraps it.
- Post inventory from a domain event: rejected because dispatch occurs after save and cannot guarantee the same atomic outcome.
- Directly assign balance quantities: rejected because existing domain validation must remain authoritative.

## Decision: Resolve Concurrent Completion by Observation, Never Retry

**Decision**: If the initially loaded order is validly Completed, return it immediately. For an InProgress mutation, verify its version, assemble posting, and save once. On order/balance rowversion failure or the existing missing-balance unique race, clear the failed tracked graph and reload the order no-tracking. Return the current Completed details if another request established the completed invariant; otherwise return a 409 posting/concurrency conflict. Never rerun posting inside the handler.

**Rationale**: This satisfies both idempotent repeated completion and the no-duplicate/no-auto-retry decisions. Reload is observation, not execution.

**Alternatives considered**:

- Always return conflict to the losing completion: rejected by the clarification.
- Automatically repeat the business operation: rejected because balance/order state may have changed and the specification forbids it.
- Add an idempotency-key store: rejected because the order itself is the boundary.

## Decision: Expose the Established Minimal API and Shared Contract Surface

**Decision**: Add an authorized `/api/wms/receiving-orders` group with list, details, create, update, Draft delete, start, line receive, and complete routes. Use separate create/update line request shapes so create never accepts IDs while update accepts nullable IDs for retained/new semantics. Return `ReceivingOrderDetails` from successful mutations, 204 from deletion, `ListResult<T>` from list, and existing Problem Details mappings for 400/404/409 outcomes. Add one narrow non-generic WebApp HTTP helper overload for successful no-content DELETE responses; retain the existing generic readers for all payload responses.

**Rationale**: This follows current dispatch and HTTP conventions and keeps internal entities private.

**Alternatives considered**:

- Nest routes under Inventory: rejected because Receiving is a separate capability and the issue fixes the route shape.
- Add a request per edited line: rejected because Draft updates replace the complete plan.
- Add a second error envelope: rejected because existing Problem Details is sufficient.

## Decision: Use Full Pages and One Focused SKU Search Dialog

**Decision**: Add list, Draft editor, and execution pages at the specified Receiving routes. The Draft editor holds the complete unpaged plan in page state, uses one dense table, opens one focused server-backed SKU search dialog per Select/Change action, rejects duplicates locally, and filters displayed rows locally without filtering the submitted backing collection. Changing Warehouse clears ReceivingLocation; location lookup always filters by `RECEIVING`.

The execution page mirrors the current Inventory Count details pattern: header/status, planned/received/remaining columns, state-gated actions, one small receive-quantity dialog, transaction link, refresh after mutation, and explicit reload guidance on conflict. It never automatically retries.

**Rationale**: Existing modal create patterns are unsuitable for 300 lines, and hundreds of live autocomplete controls would be unnecessarily heavy. Current full-page inventory execution already provides the closest UX and conflict pattern.

**Alternatives considered**:

- Complete-document modal: rejected by the specification.
- One autocomplete per line: rejected for the 300-line functional case.
- Server paging of unsaved lines, spreadsheet controls, or bulk import: rejected as unnecessary scope.

## Decision: Reuse Existing Structured Logging

**Decision**: Log concise Create, UpdateDraft, DeleteDraft, Start, Receive, Complete, validation/state rejection, and conflict outcomes through each handler's existing `ILogger`. Include action, outcome, actor when available, order/line/warehouse/location/SKU/quantity/transaction identifiers as applicable. Add no logging abstraction or persistent audit workflow.

**Rationale**: Existing WMS handlers already use structured logs with action/outcome and identifiers.

**Alternatives considered**:

- Add an audit table or event stream: rejected as outside scope.
- Log the same outcome in domain and handler: rejected because it duplicates signals.

## Decision: Use Deterministic Acceptance Because No Tracked Test Infrastructure Exists

**Decision**: Do not restore or create `Myrmex.Tests`, xUnit, browser/component tests, benchmarks, or load infrastructure. The tracked solution currently has only production projects; `Myrmex.Tests` contains ignored build remnants but no tracked project or sources. Validate through the existing build/application paths and the deterministic scenarios in [quickstart.md](quickstart.md), including domain rules through public behavior, persistence/atomicity, API error mapping, WebApp workflow, and exactly 300 distinct lines without timing assertions.

If project policy later authorizes restoring a test project, use the former repository conventions rather than inventing another framework, but that is not part of this plan.

**Rationale**: The issue forbids new testing infrastructure. Manual deterministic acceptance is the only current in-tree option and is proportional to the repository's present state.

**Alternatives considered**:

- Restore the removed test project inside this feature: rejected because it is a material scope expansion requiring separate authorization.
- Add Playwright or bUnit: rejected because no such WebApp framework exists and the issue forbids new infrastructure.
- Convert 300 lines into a benchmark: rejected because it is explicitly a functional dataset.
