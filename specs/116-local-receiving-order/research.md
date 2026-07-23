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

**Decision**: An update materializes and validates the full proposed Draft before mutation. Every non-null LineId must be unique and belong to the loaded aggregate. Retained lines update in place, omitted lines are removed, and null-ID lines are created. A retained Draft line may change SKU while preserving its LineId; LineId, not SKU, is the reconciliation identity. The final proposed set must have at least one line, unique SKUs, and positive planned quantities. The aggregate returns removed lines so the handler can mark them for explicit deletion.

**Rationale**: This implements the clarification exactly, preserves stable identities, and prevents partial in-memory mutation when a later submitted line is invalid.

**Alternatives considered**:

- Delete and recreate all lines: rejected because retained IDs must remain stable.
- Reconcile by SKU: rejected because LineId is the explicit identity and a retained line may change SKU while Draft.
- Persist each cell independently: rejected because the contract is one complete plan.

## Decision: Use Aggregate-Level Rowversion Only

**Decision**: Add SQL Server RowVersion only to `ReceivingOrder`. Encode it as Base64 `OrderVersion` in read models and parse exactly eight bytes in a small static `ReceivingOrderVersion` helper analogous to the existing `InventoryCountVersion`; do not add a Receiving-specific value object. Draft update, Draft delete, Start, Receive, and Complete require the expected order version for an actual mutation. Every child mutation touches the order so the parent row changes; EF rowversion catches races between validation and save.

Idempotent reads of current state are ordered before version rejection: Start on an already InProgress order returns current details; Complete on a valid already Completed order returns current details. Lines have no independent version.

**Rationale**: The clarified aggregate boundary means any line change makes the complete order representation stale. This is simpler and safer than combining parent and line tokens.

**Alternatives considered**:

- Add line rowversions: rejected by the clarification and because it permits a stale aggregate view.
- Rely only on client-side checks: rejected because concurrent server mutations must not overwrite one another.
- Auto-merge stale requests: rejected because the business intent is unknown.

## Decision: Add One Seeded `RECEIVING` StorageLocationType

**Decision**: Add one active system `StorageLocationType` with technical code `RECEIVING`, a stable `WmsSeedIds` identifier, and the established `HasData`/migration pattern. Add one Topology-owned public constant, `StorageLocationTypeCodes.Receiving`, in `Myrmex.Shared.Wms.Topology`; seeding, validation, lookup requests, demo definitions, and WebApp code reference that constant instead of scattering raw code comparisons. Do not reuse `DOCK`: its current description covers both receiving and shipping and the specification excludes dock behavior. Do not add a capability flag, separate ReceivingLocation entity, or multiple Receiving categories.

Add at least one new local demo location using `StorageLocationTypeCodes.Receiving` so the workflow is demonstrable. Keep existing DOCK demo identities unchanged rather than silently reclassifying them.

**Rationale**: Existing special location semantics are represented with seeded system type rows and exact code comparisons. One new type makes Receiving unambiguous while preserving the existing topology model.

**Alternatives considered**:

- Reuse `DOCK`: rejected because it is ambiguous and would model an explicitly excluded concept.
- Reuse `STAGING`: rejected because staging is out of scope and not equivalent to receipt.
- Add `CanReceive` or a generalized capability collection: rejected as unnecessary infrastructure.
- Reclassify existing DOCK demo rows: rejected because demo seeding treats existing type mismatches as identity conflicts.

## Decision: Reuse Existing Lookup and Validate Receiving Eligibility Server-Side

**Decision**: Define one authoritative rule: the Warehouse is active; the StorageLocation is active and belongs to that Warehouse; its active StorageLocationType has `StorageLocationTypeCodes.Receiving`; and its current StorageLocationStatus plus all other conditions pass the existing authoritative inventory/selectability eligibility rules. Create, Update Draft, and Start call the same narrow Receiving eligibility orchestration. The orchestration checks Receiving-specific warehouse ownership and type semantics while delegating location/type/status selectability to one reused Topology-owned eligibility predicate/result; `InventoryBalanceCreateEligibility` also reuses that predicate rather than independently repeating the same status checks.

The WebApp calls the existing warehouse-scoped StorageLocation lookup with `SelectableOnly=true` and `StorageLocationTypeCode=StorageLocationTypeCodes.Receiving`. The lookup uses the same selectability predicate and type constant, so it offers only eligible Receiving locations, but backend validation remains authoritative because topology may change or a client may bypass the UI. Missing-balance creation additionally reuses the existing SKU/base-UOM eligibility. Use set-based loading for a representative 300-line functional acceptance dataset and validate in request order without per-line database queries.

**Rationale**: Lookup filtering improves UX but is not a security or correctness boundary. One shared location eligibility source prevents lookup, Receiving, and balance creation from drifting or duplicating active-status checks while server validation prevents crafted requests and catches topology changes between editing and Start.

**Alternatives considered**:

- Add a Receiving-specific location endpoint: rejected because the existing lookup already has the required filters.
- Trust the WebApp selection: rejected because clients can bypass it and eligibility may change.
- Add generalized location capabilities: rejected as out of scope.

## Decision: Persist Restrictive Relationships and Guard Physical Draft Deletion

**Decision**: Add restrictive foreign keys from orders to Warehouse, ReceivingLocation, and optional InventoryTransaction; from lines to order and SKU; and keep parent-to-lines delete behavior restrictive. Draft deletion is not an aggregate lifecycle transition and adds no `Deleted` status or aggregate delete-validation operation. The application handler loads the stored aggregate and lines, validates the expected order version, and permits physical removal only when the stored status is Draft with no inventory transaction or inventory effect. It explicitly removes lines then the order and calls SaveChanges once, releasing the unique Number.

Valid Draft invariants already require zero received quantity and null start/completion/transaction fields. Encountering a stored Draft with any received quantity is an invalid persisted-state failure and blocks deletion; zero received quantity is a defensive consistency check, not an additional legal Draft state. Add `CanBeDeleted` or `EnsureCanDelete` only if implementation proves a domain member materially clarifies the handler; no aggregate delete method is required by the plan.

**Rationale**: Existing WMS documents use restrictive relationships. Explicit dependent removal supports the clarified Draft-only delete without enabling cascade removal of completed documents or inventory history.

**Alternatives considered**:

- Cascade all order-line deletes: rejected because direct deletion could bypass the Draft-only invariant.
- Soft delete/archive: rejected by the clarification.
- Cancellation: rejected because the lifecycle supports exactly three statuses.

## Decision: Keep Integrity Indexes and Derive List Indexes From Query Shape

**Decision**: Add the required unique indexes for normalized Number and `(ReceivingOrderId, StockKeepingUnitId)`, plus a unique filtered index for non-null InventoryTransactionId; retain the existing unique balance `(StockKeepingUnitId, StorageLocationId)` index. Map duplicate Number and duplicate order/SKU violations through `WmsPersistenceExceptionMapper`. Do not prescribe a separate single-column index for every filter or sort field. During implementation, inspect the generated Receiving list SQL and existing WMS configurations, then add only justified composite/non-unique indexes, with `(WarehouseId, Status, CreatedAtUtc)` as a candidate rather than a requirement. The user-generated migration contains the Receiving tables, justified indexes, constraints, relationships, rowversion, and Receiving type seed.

**Rationale**: Named database constraints close concurrency and integrity gaps and fit existing SQL Server exception mapping. Query-shape review avoids speculative index proliferation while allowing a composite index that matches the actual common filter/order path.

**Alternatives considered**:

- Application-only uniqueness: rejected because concurrent saves can pass both checks.
- Add a numbering table/service: rejected because Number remains user-entered.
- Add a generic source-document reference: rejected because the order directly references its one transaction.

## Decision: Create One Narrow Multi-Entry Receiving Transaction Factory and Direct Trace Link

**Decision**: Extend `InventoryTransactionType` with `Receiving` and add `InventoryTransaction.CreateReceiving(receivingLocationId, changes, reason, occurredAtUtc, out transaction)`. Pass the single order `ReceivingLocationId` once. Each feature-specific change contains only SKU, positive quantity delta, balance before, and balance after. Require a non-empty set and positive deltas, and delegate entry consistency to existing `InventoryLedgerEntry.Create`.

`ReceivingOrder.InventoryTransactionId` remains the authoritative direct order-to-transaction link. Use one stable, invariant, non-localized reason convention containing both identifiers: `ReceivingOrder {ReceivingOrderId:D} Number {NormalizedNumber}`. Do not add a generic source-document abstraction or an Inventory-owned Receiving reference. Reverse navigation may later be composed in a query/read model by joining the direct link, without changing aggregate ownership.

**Rationale**: Inventory already owns transaction and entry validation. A narrow factory creates exactly one transaction with one entry per order line, avoids repeating invariant location data, and leaves document ownership with Receiving while providing stable human and diagnostic traceability.

**Alternatives considered**:

- One transaction per line: rejected because one completed order must produce one transaction.
- A fictitious source location: rejected because the physical source is external.
- A universal posting engine: rejected because no second use case requires it.

## Decision: Complete Through One Save and No Posting Event

**Decision**: The completion handler bulk-loads existing balances for all line SKU/location pairs, creates eligible missing balances, increases existing balances through domain behavior, constructs the one Receiving transaction, completes the order with the transaction ID and common UTC timestamp, adds all new entities, and invokes exactly one `SaveChangesAsync`. Before that call, validate every planned quantity, receipt increment, accumulated received quantity, balance before/after, transaction delta, and calculated balance-after value against SQL Server `decimal(18,4)` scale and range. Rely on EF Core's transaction for a multi-command save. Mirror the existing `MoveInventoryBalance` pattern by capturing tracked aggregate domain events before save and dispatching/clearing them only after a successful save; no event creates the Receiving inventory effect. Do not split validation/posting into separate saves.

Repository research found consistent `HasPrecision(18, 4)` mappings but no shared range validator. Add one minimal static WMS-domain helper, `WmsQuantityPersistence`, for the `decimal(18,4)` boundary (maximum absolute value `99,999,999,999,999.9999` and no more than four fractional digits) and reuse it across the Receiving aggregate and Inventory posting inputs/calculations instead of adding Receiving-only magic limits. This feature adds no weight fields, normalization, or calculations.

**Rationale**: Existing transfer flows assemble balance, transaction, and document changes before saving. One save is the smallest boundary that guarantees all-or-nothing persistence.

**Alternatives considered**:

- Explicit multi-save transaction: rejected because the requirement is one SaveChanges boundary and EF already wraps it.
- Post inventory from a domain event: rejected because dispatch occurs after save and cannot guarantee the same atomic outcome.
- Directly assign balance quantities: rejected because existing domain validation must remain authoritative.

## Decision: Resolve Concurrent Completion by Observation, Never Retry

**Decision**: If the initially loaded order satisfies the complete persisted Completed invariant, return it immediately. For an InProgress mutation, verify its version, assemble posting, and save once. On order/balance rowversion failure or the existing missing-balance unique race, clear the failed tracked graph and reload the order with all lines no-tracking. Return current details only when the reload has `Status == Completed`, non-null `StartedAtUtc`, non-null `CompletedAtUtc`, non-null `InventoryTransactionId`, and every line fully received. If it is not Completed, return the appropriate 409 posting/concurrency conflict. If it claims Completed but any invariant member is missing, return a stable invalid-persisted-state failure. Never rerun posting inside the handler.

**Rationale**: This satisfies both idempotent repeated completion and the no-duplicate/no-auto-retry decisions. Reload is observation, not execution.

**Alternatives considered**:

- Always return conflict to the losing completion: rejected by the clarification.
- Automatically repeat the business operation: rejected because balance/order state may have changed and the specification forbids it.
- Add an idempotency-key store: rejected because the order itself is the boundary.

## Decision: Expose the Established Minimal API and Shared Contract Surface

**Decision**: Add an authorized `/api/wms/receiving-orders` group with list, details, create, Draft update, Draft delete, start, line receive, and complete routes. Use separate create/update line request shapes so create never accepts IDs while `UpdateReceivingOrderDraftRequest` accepts nullable IDs for retained/new semantics. Return `ReceivingOrderDetails` with `200 OK` from successful create and other payload mutations, 204 from deletion, `ListResult<T>` from list, and existing Problem Details mappings. The repository's shared generic `ServiceResult<T>.ToHttpResult()` maps all successful payloads—including current WMS creates—to `200 OK`; introducing a Receiving-only `201 Created` path would break that deliberate endpoint convention, so the plan retains 200 and documents the reason. Add one narrow non-generic WebApp HTTP helper overload for successful no-content DELETE responses; retain the existing generic readers for all payload responses.

**Rationale**: This follows current dispatch, naming, and HTTP conventions and keeps internal entities private. Existing public WMS status containers are named `InventoryCountStatusDetails` and `InventoryTransferStatusDetails`, so retain `ReceivingOrderStatusDetails` rather than introducing the inconsistent `ReceivingOrderStatuses` name.

**Alternatives considered**:

- Nest routes under Inventory: rejected because Receiving is a separate capability and the issue fixes the route shape.
- Add a request per edited line: rejected because Draft updates replace the complete plan.
- Add a second error envelope: rejected because existing Problem Details is sufficient.

## Decision: Use Full Pages and One Focused SKU Search Dialog

**Decision**: Add list, Draft editor, and execution pages at the specified Receiving routes. The Draft editor holds the complete unpaged plan in page state, uses one dense table, opens one focused server-backed SKU search dialog per Select/Change action, rejects duplicates locally, and filters displayed rows locally without filtering the submitted backing collection. Changing Warehouse clears ReceivingLocation; location lookup always filters with `StorageLocationTypeCodes.Receiving`.

On a Draft save conflict, preserve the unsaved complete plan, show current-data resolution choices, and disable repeated Save until the user explicitly reloads/discards or resolves the plan against current details. The execution page mirrors the current Inventory Count details pattern: header/status, planned/received/remaining columns, state-gated actions, one small receive-quantity dialog, transaction link, refresh after mutation, and explicit conflict guidance. Because execution actions have no comparable large unsaved page state, a true execution conflict may reload current details. Neither flow automatically retries.

**Rationale**: Existing modal create patterns are unsuitable for a representative 300-line functional acceptance dataset, and hundreds of live autocomplete controls would be unnecessarily heavy. Current full-page inventory execution already provides the closest UX and conflict pattern.

**Alternatives considered**:

- Complete-document modal: rejected by the specification.
- One autocomplete per line: rejected for a representative 300-line functional acceptance dataset.
- Server paging of unsaved lines, spreadsheet controls, or bulk import: rejected as unnecessary scope.

## Decision: Validate Multiple References in Stable Fail-Fast Order

**Decision**: Follow the established WMS fail-fast style but make ordering explicit for set-based plans. Validate request/version shape first; for existing-order commands, load the target and validate its current lifecycle/version; validate submitted LineId/plan structure in request order; then validate the Warehouse; then the ReceivingLocation with the authoritative rule; then SKUs/base UOMs in the order of each SKU's first appearance; then remaining aggregate and persistence constraints. Create omits the target-order step. Set-based database reads populate keyed results, but the handler walks the original request order to choose the first failure and never relies on database result order.

**Rationale**: Current WMS handlers return the first encountered validation failure. Reapplying that convention after bulk loading gives deterministic behavior for multiple invalid references without creating a multi-error framework.

**Alternatives considered**:

- Return whichever invalid row the database yields first: rejected because SQL row order is undefined.
- Accumulate every reference error: rejected because it changes the established fail-fast contract and is unnecessary for the MVP.

## Decision: Keep Aggregate-Total Sort Because the Existing WMS List Convention Supports It

**Decision**: Retain `TotalPlannedQuantity` in the initial sort surface. Repository research confirms that the comparable Inventory Transfer list deliberately exposes and translates aggregate-total sorts including `TotalRequestedQuantity`, `TotalPickedQuantity`, `TotalPlacedQuantity`, and `TotalInTransitQuantity`. Receiving implements only the operationally useful planned total alongside Number, Status, WarehouseCode, and lifecycle timestamps; every sort retains an ID tie-breaker.

**Rationale**: The architectural-review condition for retaining the aggregate sort is met by an existing WMS list with an equivalent summed planned/requested quantity. This is a query contract choice, not a requirement for a dedicated single-column index.

**Alternatives considered**:

- Remove all aggregate-total sorts: rejected because it would be less consistent with the comparable operational WMS list.

### Implemented list-index review

The implemented query independently combines normalized Number search, optional Warehouse and Status filters, seven supported sorts, paging, and an ID tie-breaker. The existing unique Number index remains required for integrity, while the existing Warehouse index supports the only high-selectivity foreign-key filter. Leading-wildcard Number search and aggregate `TotalPlannedQuantity` sorting cannot benefit from a conventional additional composite index, and no single composite ordering matches the independently selectable filter/sort combinations. Consistent with the local MVP and existing WMS convention, no additional non-unique Receiving Order list index is justified without observed production query evidence.
- Add received and remaining total sorts as well: rejected because the MVP needs only the narrow, useful initial surface.

## Decision: Reuse Existing Structured Logging

**Decision**: Log concise Create, UpdateDraft, DeleteDraft, Start, Receive, Complete, validation/state rejection, and conflict outcomes through each handler's existing `ILogger`. Include action, outcome, actor when available, order/line/warehouse/location/SKU/quantity/transaction identifiers as applicable. Add no logging abstraction or persistent audit workflow.

**Rationale**: Existing WMS handlers already use structured logs with action/outcome and identifiers.

**Alternatives considered**:

- Add an audit table or event stream: rejected as outside scope.
- Log the same outcome in domain and handler: rejected because it duplicates signals.

## Decision: Use Deterministic Acceptance Because No Tracked Test Infrastructure Exists

**Decision**: Do not restore or create `Myrmex.Tests`, xUnit, browser/component tests, benchmarks, or load infrastructure. The tracked solution currently has only production projects; `Myrmex.Tests` contains ignored build remnants but no tracked project or sources. User-owned validation uses the existing build/application paths and the deterministic scenarios in [quickstart.md](quickstart.md), including domain rules through public behavior, persistence/atomicity, API error mapping, WebApp workflow, and a representative 300-line functional acceptance dataset without timing assertions. The deterministic large-plan procedure uses exactly 300 distinct lines.

If project policy later authorizes restoring a test project, use the former repository conventions rather than inventing another framework, but that is not part of this plan.

**Rationale**: The issue forbids new testing infrastructure. Manual deterministic acceptance is the only current in-tree option and is proportional to the repository's present state.

**Alternatives considered**:

- Restore the removed test project inside this feature: rejected because it is a material scope expansion requiring separate authorization.
- Add Playwright or bUnit: rejected because no such WebApp framework exists and the issue forbids new infrastructure.
- Convert 300 lines into a benchmark: rejected because it is explicitly a functional dataset.
