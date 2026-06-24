# Research: Inventory Counting MVP

## Decision: Model counting as a persisted aggregate

Create `InventoryCount` as the aggregate root with owned `InventoryCountLine` entities.

**Rationale**: Counting has a durable lifecycle, immutable physical evidence, conflict recovery, actor audit, and final states that cannot be represented by an immediate adjustment operation alone.

**Alternatives considered**:

- Extend direct adjustment only: rejected because it loses count-session lifecycle and snapshot evidence.
- Store independent lines without a count aggregate: rejected because warehouse ownership, completion, cancellation, and cross-line completion invariants require a document boundary.

## Decision: Reuse adjustment domain primitives, not the adjustment handler

Apply variance through `InventoryBalance.ApplyCountedQuantityAdjustment` or `InventoryBalance.Create`, then `InventoryTransaction.CreateAdjustment`, inside the count apply handler.

**Rationale**: This preserves existing balance and ledger invariants while allowing count-line state, balance, transaction, and ledger entry to commit atomically in one context/save.

**Alternatives considered**:

- Dispatch `AdjustInventoryBalance.Command` from the count handler: rejected because it would save independently and break atomicity with count-line state.
- Mutate balance without an adjustment transaction: rejected by the specification and constitution.
- Add a generic inventory mutation service: rejected as unnecessary abstraction for the current feature.

## Decision: Use explicit rowversions for count, line, and balance concurrency

Persist rowversions on counts and lines and expose them as Base64 strings. Continue using the captured balance rowversion/absence as the inventory snapshot.

**Rationale**: Count/version checks protect lifecycle and line edits from stale UI actions; the balance snapshot prevents applying a physical count over inventory changed by another operation.

**Alternatives considered**:

- Balance version only: rejected because count completion/cancellation and line edits can race independently.
- Last-write-wins: rejected because it can erase audit evidence or apply invalid state transitions.
- Automatic retry: rejected because operators must review changed physical/system state.

## Decision: Persist Conflict before returning 409

When apply detects a changed balance snapshot, mark the line Conflict and save that state without any inventory/ledger mutation, then return a conflict result.

**Rationale**: Conflict is required audit state and drives the supersede workflow. Persisting it separately is not a partial inventory apply; it is the complete outcome of a failed apply attempt.

**Alternatives considered**:

- Return 409 while leaving Counted: rejected because the audit and UI cannot distinguish a known stale line.
- Mark Conflict after attempting mutation: rejected because validation must occur before any balance mutation.

## Decision: Use Superseded plus a filtered current-line uniqueness index

Persist `IsCurrent` and enforce a unique filtered index for current `(Count, SKU, Location)` lines. A replacement line references the prior Conflict line; the prior line becomes Superseded and non-current.

**Rationale**: The database protects the one-current-line invariant while retaining immutable history and allowing a fresh snapshot in the same count.

**Alternatives considered**:

- Reuse/reset the Conflict row: rejected because it destroys the original snapshot and conflict evidence.
- Allow duplicate active lines: rejected because completion and apply ownership become ambiguous.
- Cancel the whole count: rejected by clarification.

## Decision: Delete only Pending lines

Physically remove a line only while Pending. Counted, Applied, Conflict, and Superseded lines are permanent.

**Rationale**: Pending is preparation data; count entry creates operational evidence that must remain auditable.

**Alternatives considered**:

- Soft-delete Pending: rejected because no audit requirement exists before counting and it adds unnecessary lifecycle state.
- Void Counted lines: rejected because a broader void/recount workflow is outside MVP.

## Decision: Resolve actor identity from authenticated server claims

Extract actor ID from `sub`, then `ClaimTypes.NameIdentifier`, then authenticated `Identity.Name`. Require it for every write and store it as a bounded string.

**Rationale**: Actor identity must be trustworthy, provider-neutral, and compatible with future identity providers. Client-supplied actor IDs can be forged.

**Alternatives considered**:

- Actor ID in request JSON: rejected as insecure and redundant.
- Nullable/anonymous audit: rejected because the clarified requirement says acting identity must be retained.
- Introduce a count-specific permission/identity system: rejected by clarification and scope.

## Decision: Keep authentication provider configuration outside this feature

Add reusable claims extraction and 401 behavior, but do not choose or configure an identity provider.

**Rationale**: Identity-provider selection is a host-wide security decision. Inventory Counting should consume the authenticated principal without creating a feature-specific authentication scheme.

**Alternatives considered**:

- Development fallback actor: rejected because fabricated identity undermines audit.
- New count-only authentication: rejected as a module-boundary and scope violation.

## Decision: Use current lines for list progress and all lines for details

List counts report totals for current lines only; details include Superseded history and replacement links.

**Rationale**: Progress must represent work required for completion, while details must preserve audit history.

**Alternatives considered**:

- Include Superseded lines in progress totals: rejected because completed work would appear unresolved or inflate totals.
- Hide Superseded lines everywhere: rejected because it removes required audit visibility.

## Decision: Keep zero variance free of inventory records

Mark the line Applied with actor/time but create no balance, transaction, or ledger entry when variance is zero.

**Rationale**: A physical confirmation with no discrepancy is count evidence, not inventory movement. This also leaves an expected-missing/zero pair absent.

**Alternatives considered**:

- Create zero-delta transaction: rejected because ledger entries prohibit zero delta and it adds noise.
- Create a missing zero balance: rejected because no inventory state changed.

## Decision: Generate the adjustment reason from count context

Compose a non-empty bounded reason using count identity plus optional count reason and line comment.

**Rationale**: Existing adjustment transactions require a reason, while the count apply action should not ask the operator to enter a second reason.

**Alternatives considered**:

- Require a reason at apply time: rejected as duplicate UX and not in the specification.
- Make transaction reason optional: rejected because it changes established ledger behavior.

## Decision: Follow existing list and WebApp patterns

Use shared list contracts, internal EF projections, `ListResult<T>`, deterministic paging, `WmsInventoryApiClient`, MudDataGrid ServerData, and lifecycle dialogs/details.

**Rationale**: Inventory Transfer and Inventory Balance already provide accepted local patterns.

**Alternatives considered**:

- Client-side load-all list: rejected for paging/performance consistency.
- New state-management framework: rejected as unnecessary.

## Decision: Focus automated tests at owning layers

Use domain tests for lifecycle rules, SQL Server tests for persistence/concurrency/atomicity, focused endpoint/client tests for transport, and manual UI smoke validation.

**Rationale**: Count concurrency and audit persistence are the highest risks. Existing guidance discourages duplicating the same business matrix at every layer.

**Alternatives considered**:

- Full HTTP duplication of every handler scenario: rejected as low-value repetition.
- Introduce a Blazor component-test framework: rejected as disproportionate to this feature.
