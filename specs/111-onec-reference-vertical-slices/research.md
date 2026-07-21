# Research: 1C Reference Vertical Slices

## Decision 1: Organize by reference-owned integration slices

**Decision**: Create explicit `Warehouses`, `UnitsOfMeasure`, and `StockKeepingUnits` areas under `Myrmex.Integrations/OneC`. Each owns its source record and OData source, manual import operation, synchronize-one operation, durable synchronization handler, reference mapping, outcome interpretation, and logs.

**Rationale**: The current composite services require developers to traverse technical folders, a central type switch, and delegate-driven runners. Reference ownership makes all seven flows locally understandable and isolates reference-specific change.

**Alternatives considered**:

- Keep the current technical folders and only rename classes: rejected because execution paths would remain distributed.
- Introduce one new generic reference engine configured per type: rejected because it recreates the abstraction problem this feature removes.
- Move files without changing composite service boundaries: rejected because ownership and call chains would remain implicit.

## Decision 2: Keep only uniform mechanisms in Common

**Decision**: Common code is limited to the singleton reference gate, import-response construction/error cap, synchronization result/outcome/reason primitives, pure durable-result mapping, configuration, authenticated OData execution, JSON envelope handling, and transport error taxonomy.

**Rationale**: These mechanisms are uniform and do not select or execute a Warehouse/UoM/SKU business flow. Keeping them shared avoids duplicating security, timeout, envelope, coordination, and contract-shaping behavior while preserving local orchestration.

**Alternatives considered**:

- Duplicate all mechanics in every slice: rejected because authentication, HTTP/error handling, response shape, and lease implementation are technical invariants.
- Retain `RunImportAsync` and `RunAsync`: rejected because callback-driven runners hide acquisition, error, and workflow order.
- Retain shared batch classification for all synchronize-one flows: rejected for application-result interpretation; each slice must show how its WMS result becomes its internal outcome.

## Decision 3: Split typed source reads from generic OData execution

**Decision**: Replace the all-reference typed `IOneCODataClient` surface with a generic authenticated OData transport plus three narrow slice sources. Each source explicitly owns entity-set selection, `$select`, ordering, filtering, key lookup, current-object validation, paging, and its source record type. Preserve current integration-wide configuration validation, including the requirement that all three entity-set settings, base URL, credentials, batch size, and timeout are valid before a manual import starts.

**Rationale**: Entity sets, source fields, folder semantics, and SKU paging are reference knowledge. Authentication, timeout, status handling, query encoding, envelope deserialization, and validation of the already-established integration-wide configuration are technical transport mechanisms. Retaining all-settings validation avoids a subtle behavioral change in which one reference import could start under configuration that previously failed before all source access.

**Alternatives considered**:

- Keep `OneCODataClient` unchanged and call it from slices: rejected because a developer must still leave the slice to discover projections, filters, and paging.
- Give each slice a separate `HttpClient`: rejected because it duplicates authentication, timeout, status, and deserialization mechanics.
- Add metadata-driven OData mapping: rejected as out of scope and less explicit.

## Decision 4: Use narrow manual-import contracts

**Decision**: Define one internal import contract and implementation per slice, each exposing `ImportAsync(CancellationToken)`. `OneCEndpoints` injects the matching contract for each existing route. A common response factory may construct responses and cap errors but accepts no workflow delegate. Preserve the current start boundary: invalid/disabled integration configuration produces pre-start `400 OneC.ConfigurationInvalid`, same-reference lease contention produces pre-start `409 OneCImport.AlreadyInProgress`, and platform authentication/authorization remains `401/403`. Once configuration succeeds and source processing starts, authentication rejection, entity-set unavailability, malformed/source-unavailable/timeout responses, and unexpected application/batch failures remain incomplete `200 OK OneCImportResponse` results with safe `OperationError`. Transport Problem Details remain applicable to the connection-test endpoint, not active manual imports.

**Rationale**: The endpoint-to-operation dependency becomes explicit while the precise pre-start versus active-import error boundary remains stable. The full import sequence, lease scope, reference mapping, cancellation, result conversion, and logging remain visible in the owning slice.

**Alternatives considered**:

- Keep `IOneCImportService` as a facade: rejected because it preserves an all-reference compatibility path.
- Put all orchestration directly in endpoint methods: rejected because transport/application behavior belongs in testable integration operations, not HTTP composition.
- Dispatch a new generic integration command: rejected because it adds an unnecessary framework and selector.

## Decision 5: Use narrow synchronize-one contracts behind existing durable handlers

**Decision**: Define one internal synchronize-one contract and implementation per slice, each exposing `SynchronizeAsync(Guid, CancellationToken)`. Each concrete `ISynchronizationHandler` moves into its slice and depends on its matching synchronizer, the pure common durable-result mapper, and its typed logger. Its visible flow is `parse and validate ExternalId -> call matching synchronizer -> write structured correlation log -> map completed result`. The log records `SynchronizationRequestId`, `EntityType`, `ExternalId`, Base64-rendered `NotifiedDataVersion`, `CurrentOutcome`, `CurrentReason`, and `RetrySuitable`; invalid `ExternalId` logs the equivalent permanent invalid-request result. Credentials, secrets, and source payloads are excluded. The common mapper neither parses requests, selects a slice, invokes callbacks, nor logs.

**Rationale**: Feature #104 remains the durable lifecycle owner, while each reference handler has a direct dependency on exactly one business flow and is the only concrete boundary that sees both durable request correlation data and the current internal outcome. Keeping mapping pure prevents correlation concerns from becoming another shared orchestration layer.

**Alternatives considered**:

- Keep `IOneCReferenceSynchronizationService`: rejected because its type switch and all-reference surface are the central coupling to remove.
- Make the handler contain the entire source/application flow: rejected because internal callers also need synchronize-one and SKU needs a direct UoM capability.
- Add public synchronize-one endpoints: rejected as explicitly out of scope.

## Decision 6: Model SKU repair as one direct UoM dependency

**Decision**: `StockKeepingUnitOneCSynchronizer` depends directly on the Unit-of-Measure synchronize-one contract. It attempts that dependency once only for the existing missing/inactive base-UoM error reasons and retries the same SKU application once.

**Rationale**: This exactly represents the current bounded rule, makes ownership visible, and prevents recursive or generic dependency handling.

**Alternatives considered**:

- Call a generic reference synchronizer by enum: rejected because it restores the central switch.
- Introduce a dependency resolver/graph: rejected because there is one known dependency and recursive resolution is prohibited.
- Move repair into the WMS import handler: rejected because source retrieval and retry orchestration belong to the 1C adapter, while WMS validation remains authoritative.

## Decision 7: Preserve the singleton gate and exact lease scopes

**Decision**: Retain one singleton gate with independent Warehouse, UoM, and SKU semaphores. Manual imports use fail-fast acquisition and hold the lease over configuration validation and the full operation; synchronize-one uses non-throwing acquisition and holds the lease over source read through final classification. SKU holds its lease while invoking the independently gated UoM synchronizer.

**Rationale**: Feature #109 established these observable coordination semantics. Per-slice gate instances would break same-type coordination across manual, reactive, and internal entry points.

**Alternatives considered**:

- One gate instance per slice: rejected because registrations could accidentally allow manual and reactive work to overlap.
- Waiting acquisition: rejected because current public 409 and internal `Busy` behavior is non-waiting.
- Distributed locking: rejected as out of scope.

## Decision 8: Preserve every public, durable, domain, and persistence contract

**Decision**: Keep public routes, endpoint names, authorization, request/response shapes, WebApp client/localization, stable entity-type strings, synchronization statuses, source-version rules, WMS commands, domain behavior, schema, EF mappings, and migrations unchanged. Preserve existing Problem Details only at their current boundaries: manual-import pre-start configuration/lease failures and connection-test transport failures. Active manual-import transport/application failures remain incomplete `200 OK OneCImportResponse` results.

**Rationale**: Issue #111 is a behavior-preserving organization change. Any change in these areas would broaden scope and weaken the compatibility baseline.

**Alternatives considered**:

- Improve public names or result shapes during the refactor: rejected because unrelated contract changes obscure behavioral verification.
- Redesign external-link or synchronization persistence: rejected as explicitly out of scope.

## Decision 9: Retarget existing tests and close only the SKU repair outcome gap

**Decision**: Preserve existing #104/#109 test scenarios and minimally update constructors, fakes, calls, and namespaces. Remove only the test whose purpose is to verify the now-prohibited central type switch. Continue using `StockKeepingUnitReferenceRepairTests`: make the successful repair test parameterized for UoM `Applied` and `Unchanged`; rename the current second test to state that UoM synchronization succeeds but the single SKU retry still reports missing/inactive UoM and stops permanently; add one compact parameterized test for UoM `Busy`, `TransientFailure`, `NotFound`, `ControlledSkip`, and `PermanentFailure`. The theory expects the first two to produce transient SKU repair failure and the last three permanent SKU repair failure. Every failed-UoM row asserts one UoM call, one SKU dispatch, no SKU retry, and no recursion/additional dependency call. Do not add a new test class, DI composition test, per-reference matrix, Feature #104 suite, or logging suite.

**Rationale**: Existing coverage already protects source projections, mapping, accounting, paging, partial commits, cancellation, gate semantics, general outcome mapping, endpoint/auth contracts, and success/retry-still-fails repair limits. It does not directly prove all failed UoM outcomes or the no-retry boundary, so one compact theory is the minimum material addition. Correlation logging remains code-review/quickstart acceptance unless an existing assertion can be trivially extended.

**Alternatives considered**:

- Create one full test class per new production class: rejected because class splitting is not a new behavioral risk.
- Repeat the synchronize-one outcome matrix for all three references: rejected as duplicate coverage.
- Add a new DI test solely for split registrations: rejected because the structural split alone does not justify a new test under FR-025.
- Add a dedicated logging test suite: rejected because the structured fields can be reviewed directly and do not justify a new matrix.
- Add no repair test: rejected because existing cases do not cover failed UoM outcome mapping and the no-retry boundary.

## Decision 10: Preserve inconsistent one-item accounting as permanent application failure

**Decision**: Preserve the current classification exactly: `Processed != 1` or otherwise inconsistent one-item counts produce `PermanentFailure`, reason `ApplicationFailure`, and `retrySuitable = false`.

**Rationale**: Issue #111 is behavior-preserving. Reclassifying this invariant as an exception or transient processor failure would change retry and durable lifecycle behavior.

**Alternatives considered**:

- Throw and let the processor decide retry behavior: deferred to a separate issue because it changes the current contract.
- Map inconsistent accounting to transient failure: deferred to a separate issue because it can add retries and change terminal status.

## Decision 11: Migrate one reference at a time without compatibility wrappers

**Decision**: Extract common prerequisites, then move Warehouse, UoM, and SKU in order. For each reference, rewire its endpoint/handler in the same change that removes its old composite method. After SKU moves, delete the composite services, typed client, old source DTO locations, obsolete registrations, delegate helpers, central switch, and placeholder options file.

**Rationale**: This sequence keeps changes reviewable and ensures there is never more than one production path for a migrated reference.

**Alternatives considered**:

- Keep facades until a later cleanup: rejected because parallel paths and compatibility wrappers are prohibited.
- Move every file first and rewire later: rejected because it creates a large ambiguous intermediate state.

## Decision 12: No data-model or schema change

**Decision**: Treat source records, manual results, internal outcomes, durable requests, WMS references, and their state transitions as existing models whose ownership is clarified but whose shape and persistence remain unchanged.

**Rationale**: The issue changes code organization only. Existing external state, source version, request lifecycle, and WMS persistence already satisfy the required behavior.

**Alternatives considered**:

- Introduce a new reference-link or slice-state entity: rejected because it duplicates existing external import state and broadens persistence scope.
- Add new synchronization statuses for the slices: rejected because Feature #104 remains the lifecycle owner.
