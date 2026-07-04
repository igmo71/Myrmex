# Research: Full WMS Demo Data Seeding

## Decision: Keep demo administration inside the WMS module

Place configuration, endpoints, commands, orchestration, definitions, and persistence logic under `Myrmex.Modules.Wms/DemoData`; place only HTTP request/response records under `Myrmex.Shared/Wms/DemoData`.

**Rationale**: Every affected aggregate is owned by WMS, and the current module already exposes Minimal API endpoints and owns `WmsDbContext`. A separate project or generic administration module would add a boundary without solving a current problem.

**Alternatives considered**:

- Add a new demo-data project: rejected because it would require privileged internal WMS access or duplicated mutation rules.
- Put orchestration in `Myrmex.ApiService`: rejected because the host should compose modules, not own WMS domain/persistence behavior.
- Build a generic seeding/import framework: rejected by scope and the constitution's simplicity rule.

## Decision: Use `Myrmex:Wms:DemoData` options and conditional route mapping

Bind `Enabled`, `AllowClear`, and `ClearConfirmation` from `Myrmex:Wms:DemoData`. Register services regardless of the flag, but map `/api/admin/demo-data/seed` and `/api/admin/demo-data/clear` only when `Enabled=true` and the host environment is not Production.

**Rationale**: The configuration path is module-scoped, defaults are safely false/null, and conditional mapping directly implements the clarified 404 behavior. Mapping both routes when enabled allows `AllowClear=false` to return an explicit forbidden response while leaving seed usable.

**Alternatives considered**:

- Top-level `DemoData`: workable but less consistent with existing `Myrmex:*` configuration ownership.
- Always map and return 403/404: rejected by the clarification that disabled and Production routes are not registered.
- Enable automatically in Development: rejected because the feature must be explicitly enabled.

## Decision: Reuse the existing authenticated-actor boundary

Both endpoints require `HttpContext.GetActorId()` and return the existing unauthorized result when no actor is available. Do not add an authentication scheme, policy, role, or user-management behavior.

**Rationale**: Administrative mutation should be attributable, while identity-provider and authorization-policy selection remain host-wide concerns explicitly outside scope. This follows inventory-count and 1C administrative endpoint precedent.

**Alternatives considered**:

- Configuration gates only: rejected because a reachable destructive endpoint should not be anonymous when the application already has an actor convention.
- Add a new demo-administrator role/policy: rejected as an authorization change outside this feature.
- Accept actor identity in JSON: rejected because caller-supplied identity is forgeable.

## Decision: Use one shared non-waiting process-local operation gate

Register a singleton gate backed by one `SemaphoreSlim`. Seed and clear both acquire it with zero wait; failure returns `409 DemoData.OperationInProgress`; disposal releases it.

**Rationale**: Seed and clear conflict with each other, not just with the same operation. The dataset is for a single demo API process, and the existing OneC gate provides an accepted local pattern.

**Alternatives considered**:

- Separate seed and clear locks: rejected because seed and clear must never overlap.
- Queue requests: rejected because callers need an immediate, explicit conflict rather than an unbounded administrative request.
- Distributed lock: rejected because multi-instance demo administration is out of scope.

## Decision: Wrap every operation in one explicit database transaction

After safety and schema-readiness checks, begin one explicit `WmsDbContext` transaction. Commit only after every stage succeeds; on any failed service result, exception, or cancellation, roll back and clear the change tracker.

**Rationale**: Clarification requires all-or-nothing behavior. Seed invokes multiple saves through existing handlers, and clear executes multiple bulk deletes; neither sequence is atomic without an outer transaction. Current EF behavior supports `SaveChanges` inside an existing transaction and requires an explicit transaction around multiple `ExecuteDeleteAsync` calls. See [EF Core transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions) and [ExecuteUpdate/ExecuteDelete transaction behavior](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete#transactions).

**Alternatives considered**:

- One `SaveChanges` for all seed data: rejected because existing inventory use cases need generated rowversions and staged lifecycle transitions.
- Stage commits with resumability: rejected by clarification.
- `TransactionScope`: rejected because one context/database transaction is simpler and explicit.

## Decision: Verify database readiness without creating or migrating schema

Before mutation, require a successful connection, no pending WMS migrations, and presence of the required system location type/status codes. Return a safe failure when readiness checks fail.

**Rationale**: The feature assumes a schema-ready database and must not create or migrate it. Required system references are also a practical compatibility check for the current WMS model.

**Alternatives considered**:

- Call `EnsureCreated` or `Migrate`: rejected explicitly by the specification.
- Let the first query fail: rejected because it produces a less actionable error and may start work before incompatibility is known.
- Add a schema-version table: rejected because migration history already provides that signal.

## Decision: Reconcile stable identities before applying mutations

Use these logical identities:

- UoM, SKU, warehouse, and storage location: stable code (location scoped to warehouse where required by the model).
- Zone: `(WarehouseId, Code)`.
- Transfer: stable transfer code.
- Opening adjustment: exact `DEMO-OPEN-*` reason plus its SKU/location ledger entry.
- Inventory count: exact `DEMO-CNT-*` reason plus warehouse and expected line pairs.

Load all matching records inside the transaction. Compatible records count as reused; missing records/stages are created; multiple matches or incompatible immutable values return conflict and roll back.

**Rationale**: Existing tables provide no demo manifest and inventory counts have no code. These identities use current unique business keys where available and deterministic bounded markers elsewhere, avoiding schema changes and duplicate inventory effects.

**Alternatives considered**:

- Deterministic entity GUIDs only: rejected because current factories own IDs and business-code compatibility remains necessary.
- Add a demo manifest/history table: rejected because no schema change is needed for this bounded dataset.
- Treat any non-empty database as invalid: rejected because compatible partial data and repeat seeding must be supported.

## Decision: Reuse current domain factories and inventory use cases selectively

Create UoMs, SKUs, warehouse, zones, and locations with their domain factories. Use existing adjustment commands for opening balances/ledger, movement commands for transfer effects, and count commands for line snapshots, counts, applies, and completion. Construct a transfer aggregate directly only to supply its required deterministic code before using existing movement commands.

**Rationale**: This maximizes reuse of current validation, rowversion, balance, ledger, transfer, and count logic. Direct insertion is restricted to the one identity gap in the current create-transfer command and remains behind a demo-only service.

**Alternatives considered**:

- Insert all balance/ledger/transfer/count rows directly: rejected because it duplicates invariants and risks incoherent history.
- Dispatch every public create command unchanged: rejected because generated transfer codes prevent deterministic identity.
- Refactor production handlers into a generic inventory mutation framework: rejected as unrelated scope and risk.

## Decision: Use only current system reference vocabulary

Map demo purposes to existing types: receiving=`DOCK`, bulk=`PALLET_RACK`, picking=`SHELF`, packing/shipping=`STAGING`, quarantine=`FLOOR`, cart/transit=`INTERNAL_TRANSIT`. Reuse active statuses `AVAILABLE`, `BLOCKED`, and `INVENTORY_CHECK` where semantically appropriate.

**Rationale**: These rows already have fixed IDs, current domain validation recognizes internal transit, and the specification prohibits unsupported statuses/types. The feature must not rename schema-seeded English system references because that would change production reference behavior.

**Alternatives considered**:

- Create `CART`, `RECEIVING_DOCK`, `PICK_FACE`, `HOLD`, or `DAMAGED`: rejected because they are not current supported reference values.
- Modify system reference names to Russian: rejected because those rows are migration-owned and shared with normal application behavior.

## Decision: Seed four transfers and two counts to expose supported lifecycles

Create a completed direct transfer, completed cart transfer, in-progress cart transfer with stock remaining on `CART-01`, and created direct transfer. Create an InProgress picking count with zero/shortage/surplus lines and a Completed bulk count whose zero-variance lines are applied before completion.

**Rationale**: This stays within the requested volume while making Created, InProgress, Completed, direct, transit, variance, and historical states visible. It uses only current statuses and actions.

**Alternatives considered**:

- Seed only two completed transfers: rejected because no created/in-progress state or cart balance would be visible.
- Invent picked/placed transfer statuses: rejected because those are movements, not persisted transfer statuses.
- Seed a completed non-zero count: valid but adds adjustment noise unnecessary for demonstrating completed count history.

## Decision: Clear mutable WMS data but preserve system references

Within the clear transaction, execute bulk deletes in this order: count lines; transfer movements; transfer lines; counts; transfers; ledger entries; transactions; balances; SKU barcodes; storage locations; zones; SKUs; UoMs; warehouses. Preserve all storage-location type/status rows, schema objects, and migration history.

**Rationale**: The order follows current restrictive foreign keys. Type/status rows are migration-owned system reference data required for reseeding and are equivalent to schema baseline, not user/demo operational data.

**Alternatives considered**:

- Drop/recreate the database: rejected explicitly.
- Delete system reference rows and recreate them: rejected because migration history would claim data exists when it does not and seeding must reuse them.
- Load and remove tracked entities: rejected as unnecessary overhead and more error-prone than ordered set-based deletion.

## Decision: Return one bounded shared success response and standard ProblemDetails failures

Both actions return `DemoDataOperationResponse` with operation, UTC start/completion times, and ordered `DemoDataAreaSummary` records. Each area carries created/reused/skipped/deleted counts; fields not applicable to an operation are zero. Failures use `ServiceResult<T>` and existing ProblemDetails mapping with stable codes.

**Rationale**: One shared shape is concise, serializable, easy to document, and avoids a new result framework. No failed operation returns a success summary because all mutations roll back.

**Alternatives considered**:

- Separate large seed/clear response hierarchies: rejected because both need the same bounded area accounting.
- Return raw entity details: rejected because administrative results should not expose domain objects or produce large payloads.
- Persist execution history: rejected as out of scope; logs provide operational diagnostics.

## Decision: Use structured logs and `TimeProvider`

Log attempted, rejected, completed, failed, and cancelled outcomes with operation, actor, environment, duration, failure category, and summary counts. Never log the confirmation value. Use injected `TimeProvider` for timestamps and deterministic tests.

**Rationale**: Clear and seed are operationally important and destructive. Existing code already uses structured `ILogger` and `TimeProvider` patterns.

**Alternatives considered**:

- Console output: rejected because it is not structured or host-integrated.
- Database audit table: rejected because persistent history is not required and would force a migration.

## Decision: Focus tests at service/persistence and endpoint boundaries

Use SQL Server tests for seed reconciliation, lifecycle effects, data coherence, ordered clear, and rollback. Use focused Minimal API tests for conditional mapping, authentication, confirmation JSON, and HTTP mapping. Validate unchanged WebApp pages manually through the quickstart.

**Rationale**: Transaction, rowversion, filtered indexes, foreign keys, and `ExecuteDeleteAsync` are provider-sensitive risks. HTTP tests protect only routing/binding/result risks. Existing domain and WebApp tests already protect unchanged behavior.

**Alternatives considered**:

- Duplicate every seeded record through HTTP: rejected as slow repetition of service-owned assertions.
- Add WebApp component automation: rejected because no UI behavior changes.
- Use only mocked/in-memory persistence: rejected because it cannot prove SQL Server rollback and foreign-key behavior.
