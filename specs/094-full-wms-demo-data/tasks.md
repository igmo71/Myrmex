# Tasks: Full WMS Demo Data Seeding

**Input**: Design documents from `specs/094-full-wms-demo-data/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/demo-data-admin.openapi.yaml`, `quickstart.md`

**Tests**: Automated tests are required for the identified transaction, idempotency, foreign-key deletion, route-registration, binding, and result-mapping risks. Write each test before the implementation it protects and use the existing SQL Server and Minimal API test infrastructure.

**Organization**: Tasks are grouped by user story. Every user-story phase remains independently reviewable and has an explicit behavior-level completion check.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it changes different files and has no dependency on another incomplete task in the same phase.
- **[Story]**: Maps the task to the corresponding specification user story.
- Every task includes an exact repository-relative file path.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish safe default host configuration without enabling destructive behavior.

- [ ] T001 Add default-disabled `Myrmex:Wms:DemoData` configuration keys without a confirmation secret in `Myrmex.ApiService/appsettings.json`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add transport types, configuration, stable definitions, common errors, and test support shared by every user story.

**Critical**: Complete this phase before beginning a user-story phase.

- [ ] T002 [P] Add `ClearDemoDataRequest`, `DemoDataAreaSummary`, and `DemoDataOperationResponse` transport records in `Myrmex.Shared/Wms/DemoData/ClearDemoDataRequest.cs`, `Myrmex.Shared/Wms/DemoData/DemoDataAreaSummary.cs`, and `Myrmex.Shared/Wms/DemoData/DemoDataOperationResponse.cs`
- [ ] T003 [P] Implement safe-default option binding fields and the `Myrmex:Wms:DemoData` section name in `Myrmex.Modules.Wms/DemoData/Configuration/WmsDemoDataOptions.cs`
- [ ] T004 [P] Define stable validation, forbidden, conflict, readiness, and execution `ServiceError` values in `Myrmex.Modules.Wms/DemoData/Features/WmsDemoDataErrors.cs`
- [ ] T005 [P] Implement one non-waiting process-local lease shared by seed and clear in `Myrmex.Modules.Wms/DemoData/Features/WmsDemoDataOperationGate.cs`
- [ ] T006 Encode the exact UoM, SKU, warehouse, zone, location, opening-stock, transfer, and count definitions from `data-model.md` in `Myrmex.Modules.Wms/DemoData/Features/DemoDataDefinitions.cs`
- [ ] T007 Add fixed time, authenticated actor, configurable environment/options, stub command results, and in-process HTTP helpers in `Myrmex.Tests/Wms/DemoData/Testing/DemoDataTestHost.cs`
- [ ] T008 Bind `WmsDemoDataOptions`, register `TimeProvider`, and register the singleton operation gate without mapping routes in `Myrmex.Modules.Wms/WmsModule.cs`

**Checkpoint**: Shared contracts and primitives compile conceptually without changing the database model or enabling any endpoint.

---

## Phase 3: User Story 1 - Seed a Complete Demo Warehouse (Priority: P1) — MVP

**Goal**: Seed an empty schema-ready non-production database with the bounded Russian WMS catalog, topology, balances, ledger history, transfers, and counts, returning a concise committed summary.

**Independent Test**: Enable demo support against an empty migrated test database, call seed once, and verify the 200 summary plus the exact bounded/coherent dataset through WMS queries and existing WebApp views.

### Tests for User Story 1

- [ ] T009 [P] [US1] Add SQL Server service tests protecting exact catalog/topology volume, Russian text, supported reference reuse, absent barcodes, and success-area counts in `Myrmex.Tests/Wms/DemoData/Features/WmsDemoDataSeederTests.cs`
- [ ] T010 [P] [US1] Add SQL Server service tests protecting opening balance/ledger consistency, direct/cart transfer states, cart stock, count variance states, and whole-request rollback after an injected mid-stage failure in `Myrmex.Tests/Wms/DemoData/Features/WmsDemoDataOperationalStateTests.cs`
- [ ] T011 [P] [US1] Add focused Minimal API tests protecting seed route binding, authenticated-actor dispatch, cancellation propagation, 200 serialization, and standard failure ProblemDetails in `Myrmex.Tests/Wms/DemoData/Endpoints/DemoDataSeedEndpointTests.cs`

### Implementation for User Story 1

- [ ] T012 [US1] Implement schema-connectivity, pending-migration, required-system-reference checks plus the outer seed transaction, rollback, tracking cleanup, timing, and area accounting in `Myrmex.Modules.Wms/DemoData/Features/WmsDemoDataSeeder.cs`
- [ ] T013 [US1] Implement UoM, SKU, warehouse, zone, and storage-location creation through current domain factories in `Myrmex.Modules.Wms/DemoData/Features/WmsDemoDataSeeder.cs`
- [ ] T014 [US1] Implement opening balances and immutable adjustment/ledger history through the current inventory adjustment use case in `Myrmex.Modules.Wms/DemoData/Features/WmsDemoDataSeeder.cs`
- [ ] T015 [US1] Implement stable-code direct, completed-cart, in-progress-cart, and created transfer scenarios using current transfer movement use cases in `Myrmex.Modules.Wms/DemoData/Features/WmsDemoDataSeeder.cs`
- [ ] T016 [US1] Implement InProgress variance and Completed zero-variance inventory-count scenarios using current count use cases in `Myrmex.Modules.Wms/DemoData/Features/WmsDemoDataSeeder.cs`
- [ ] T017 [US1] Implement `SeedWmsDemoData.Command` and handler with actor propagation and service-result mapping in `Myrmex.Modules.Wms/DemoData/Features/SeedWmsDemoData.cs`
- [ ] T018 [US1] Map the authenticated `POST /api/admin/demo-data/seed` action and shared success/ProblemDetails result in `Myrmex.Modules.Wms/DemoData/Endpoints/DemoDataAdminEndpoints.cs`
- [ ] T019 [US1] Register the scoped seeder/seed handler dependencies and map the demo endpoint group when `Enabled=true` in `Myrmex.Modules.Wms/WmsModule.cs`

**Checkpoint**: User Story 1 can seed and demonstrate the complete dataset from an empty migrated database without any clear operation.

---

## Phase 4: User Story 2 - Rerun Seeding Safely (Priority: P2)

**Goal**: Repeated seed calls reuse compatible business identities and resume compatible missing stages without duplicating inventory effects; incompatible identities abort the entire request.

**Independent Test**: Seed twice and compare identities, balances, transactions, ledger entries, transfers, counts, and summaries; then repeat from compatible partial data and from an incompatible stable-code collision.

### Tests for User Story 2

- [ ] T020 [P] [US2] Add SQL Server tests protecting second-run zero-duplicate identities/effects and accurate reused/skipped summaries in `Myrmex.Tests/Wms/DemoData/Features/WmsDemoDataIdempotencyTests.cs`
- [ ] T021 [P] [US2] Add SQL Server tests protecting compatible partial-stage resume for references, topology, opening history, transfers, and counts in `Myrmex.Tests/Wms/DemoData/Features/WmsDemoDataResumeTests.cs`
- [ ] T022 [P] [US2] Add SQL Server tests protecting incompatible or ambiguous identity detection, 409 errors, no overwrite, and complete rollback in `Myrmex.Tests/Wms/DemoData/Features/WmsDemoDataConflictTests.cs`

### Implementation for User Story 2

- [ ] T023 [US2] Add normalized identity lookup and compatibility validation for UoMs, SKUs, warehouse, zones, and locations in `Myrmex.Modules.Wms/DemoData/Features/WmsDemoDataSeeder.cs`
- [ ] T024 [US2] Add stable `DEMO-OPEN-*`, `DEMO-TRF-*`, and `DEMO-CNT-*` reconciliation with compatible operational-stage resume in `Myrmex.Modules.Wms/DemoData/Features/WmsDemoDataSeeder.cs`
- [ ] T025 [US2] Return precise created/reused/skipped area counts and map incompatible or ambiguous identities to `DemoData.IdentityConflict` without exposing data or committing changes in `Myrmex.Modules.Wms/DemoData/Features/WmsDemoDataSeeder.cs`

**Checkpoint**: User Stories 1 and 2 support empty, repeated, and compatible-partial seeding while rejecting incompatible data atomically.

---

## Phase 5: User Story 3 - Reset the Demo Database (Priority: P3)

**Goal**: Clear all mutable WMS/application records, preserve system references/schema/migration history, and reseed the known demonstration state.

**Independent Test**: Add user-created WMS records, clear with valid controls, verify every mutable table is empty and preserved structures remain, then seed and compare stable identities/scenarios with the original dataset.

### Tests for User Story 3

- [ ] T026 [P] [US3] Add SQL Server tests protecting foreign-key-safe deletion counts, user-created record removal, system type/status preservation, schema/history preservation, and rollback after an injected clear-stage failure in `Myrmex.Tests/Wms/DemoData/Features/WmsDemoDataClearServiceTests.cs`
- [ ] T027 [P] [US3] Add SQL Server reset-roundtrip tests protecting stable identities and equivalent demo scenarios after seed → mutation → clear → reseed in `Myrmex.Tests/Wms/DemoData/Features/WmsDemoDataResetTests.cs`
- [ ] T028 [P] [US3] Add focused Minimal API tests protecting clear JSON binding, authenticated-actor dispatch, cancellation, 200 deletion-summary serialization, and standard failure ProblemDetails in `Myrmex.Tests/Wms/DemoData/Endpoints/DemoDataClearEndpointTests.cs`

### Implementation for User Story 3

- [ ] T029 [US3] Implement the explicit clear transaction and ordered `ExecuteDeleteAsync` stages with per-area deletion counts, rollback, tracking cleanup, and preservation rules in `Myrmex.Modules.Wms/DemoData/Features/WmsDemoDataClearService.cs`
- [ ] T030 [US3] Implement `ClearWmsDemoData.Command` and handler with actor/confirmation propagation and service-result mapping in `Myrmex.Modules.Wms/DemoData/Features/ClearWmsDemoData.cs`
- [ ] T031 [US3] Add the authenticated JSON-body `POST /api/admin/demo-data/clear` action and shared success/ProblemDetails result in `Myrmex.Modules.Wms/DemoData/Endpoints/DemoDataAdminEndpoints.cs`
- [ ] T032 [US3] Register the scoped clear service and clear command dependencies in `Myrmex.Modules.Wms/WmsModule.cs`

**Checkpoint**: User Story 3 can independently clear arbitrary mutable WMS data and, with User Story 1, restore the known demo state.

---

## Phase 6: User Story 4 - Prevent Unsafe Demo Operations (Priority: P4)

**Goal**: Fail closed by omitting routes when disabled/Production, requiring existing actor identity, applying stronger clear controls, rejecting overlap, and emitting secret-safe diagnostics.

**Independent Test**: Exercise disabled, Production, unauthenticated, clear-disabled, blank/wrong confirmation, concurrent, failed, cancelled, and successful cases and verify routes/statuses, zero rejected mutations, and sanitized logs.

### Tests for User Story 4

- [ ] T033 [P] [US4] Add unit tests protecting the shared zero-wait lease, seed/clear mutual exclusion, and release-after-success/failure behavior in `Myrmex.Tests/Wms/DemoData/Features/WmsDemoDataOperationGateTests.cs`
- [ ] T034 [P] [US4] Add focused host tests protecting disabled and Production 404 route absence plus enabled non-production registration in `Myrmex.Tests/Wms/DemoData/Endpoints/DemoDataRouteRegistrationTests.cs`
- [ ] T035 [P] [US4] Add focused endpoint tests protecting 401 actor checks, 403 clear-disabled/wrong-confirmation behavior, 400 missing confirmation, 409 overlap, and zero service calls for rejected requests in `Myrmex.Tests/Wms/DemoData/Endpoints/DemoDataSafetyEndpointTests.cs`
- [ ] T036 [P] [US4] Add logging tests protecting attempted/completed/rejected/failed/cancelled diagnostics and ensuring confirmation values never appear in messages or structured properties in `Myrmex.Tests/Wms/DemoData/Features/WmsDemoDataDiagnosticsTests.cs`

### Implementation for User Story 4

- [ ] T037 [US4] Enforce default-disabled and Production route omission while preserving enabled non-production mapping in `Myrmex.Modules.Wms/WmsModule.cs`
- [ ] T038 [US4] Enforce actor presence, `AllowClear`, non-empty configured confirmation, required JSON confirmation, and ordinal exact-match guards before database dispatch in `Myrmex.Modules.Wms/DemoData/Endpoints/DemoDataAdminEndpoints.cs`
- [ ] T039 [US4] Integrate the shared operation gate and stable overlap/cancellation/error outcomes into seed orchestration in `Myrmex.Modules.Wms/DemoData/Features/SeedWmsDemoData.cs`
- [ ] T040 [US4] Integrate the shared operation gate and stable clear-disabled/confirmation/overlap/cancellation/error outcomes into clear orchestration in `Myrmex.Modules.Wms/DemoData/Features/ClearWmsDemoData.cs`
- [ ] T041 [US4] Add secret-safe structured attempted/completed/rejected/failed/cancelled logging with actor, environment, duration, category, and area counts in `Myrmex.Modules.Wms/DemoData/Features/WmsDemoDataSeeder.cs` and `Myrmex.Modules.Wms/DemoData/Features/WmsDemoDataClearService.cs`

**Checkpoint**: All unsafe and overlapping requests fail closed without data changes or confirmation disclosure.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Finalize operator guidance and verify the implementation remains within the approved boundaries.

- [ ] T042 [P] Write operator enable/seed/clear/reseed, response interpretation, troubleshooting, diagnostics, and safety guidance in `docs/demo-data.md`
- [ ] T043 [P] Link the demo-data operator guide from `./README.md`
- [ ] T044 [P] Add non-secret seed/clear request examples that match the implemented JSON contract in `Myrmex.ApiService/Myrmex.ApiService.http`
- [ ] T045 Reconcile implemented routes, schemas, status codes, area keys, and error codes with `specs/094-full-wms-demo-data/contracts/demo-data-admin.openapi.yaml`
- [ ] T046 Reconcile configuration, developer-controlled commands, negative scenarios, and the twelve-step WebApp walkthrough with `specs/094-full-wms-demo-data/quickstart.md`
- [ ] T047 Perform a static scope audit confirming no migration, `HasData` operational seed, 1C behavior change, SKU group/barcode, generic import framework, WebApp redesign, or deployment change and record any correction in `specs/094-full-wms-demo-data/plan.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 — Setup**: Starts immediately.
- **Phase 2 — Foundational**: Depends on T001 and blocks every user-story phase.
- **Phase 3 — US1**: Depends on Phase 2 and delivers the seed MVP.
- **Phase 4 — US2**: Depends on US1 because it extends the seed implementation with reconciliation.
- **Phase 5 — US3**: Clear-service work can begin after Phase 2, but the complete reset/reseed acceptance test depends on US1; execute after US2 in the default sequence.
- **Phase 6 — US4**: Depends on the seed and clear endpoints from US1/US3; it completes route, authorization, confirmation, overlap, and diagnostics hardening.
- **Phase 7 — Polish**: Depends on every user story selected for delivery.

### User Story Dependency Graph

```text
Setup -> Foundation -> US1 Seed MVP -> US2 Idempotency
                         |
                         +-----------> US3 Clear/Reset -> US4 Safety
US2 ----------------------------------------------^       |
                                                          v
                                                        Polish
```

- **US1 (P1)**: First deployable behavior; no other story dependency after Foundation.
- **US2 (P2)**: Extends US1 seeding and cannot complete independently of its service.
- **US3 (P3)**: Clear is independently implementable after Foundation; full reset/reseed validation uses US1.
- **US4 (P4)**: Hardens the routes and orchestration introduced by US1 and US3.

### Within Each User Story

- Write the named automated tests first and verify their intended failure through developer-controlled execution.
- Implement definitions/contracts before services, services before command handlers, and handlers before endpoints/registration.
- Keep shared HTTP contracts separate from internal commands and persistence types.
- Use one explicit transaction per operation and propagate cancellation through every async call.
- Complete the story checkpoint before advancing to the next priority.

### Parallel Opportunities

- T002–T005 can run in parallel; T006–T008 then integrate those primitives.
- T009–T011 can run in parallel before US1 implementation.
- T020–T022 can run in parallel before US2 reconciliation implementation.
- T026–T028 can run in parallel before US3 implementation.
- T033–T036 can run in parallel before US4 hardening.
- T042–T044 can run in parallel after the implemented contract stabilizes.
- US3 clear-service implementation can proceed alongside US2 reconciliation after US1, but T027 and final integration wait for both.

---

## Parallel Execution Examples

### User Story 1

```text
T009: SQL Server catalog/topology/summary tests in WmsDemoDataSeederTests.cs
T010: SQL Server operational-state/rollback tests in WmsDemoDataOperationalStateTests.cs
T011: Seed HTTP binding/serialization tests in DemoDataSeedEndpointTests.cs
```

### User Story 2

```text
T020: Repeat-seed idempotency tests in WmsDemoDataIdempotencyTests.cs
T021: Compatible partial-resume tests in WmsDemoDataResumeTests.cs
T022: Identity-conflict rollback tests in WmsDemoDataConflictTests.cs
```

### User Story 3

```text
T026: Clear ordering/preservation/rollback tests in WmsDemoDataClearServiceTests.cs
T027: Reset-roundtrip tests in WmsDemoDataResetTests.cs
T028: Clear HTTP contract tests in DemoDataClearEndpointTests.cs
```

### User Story 4

```text
T033: Operation-gate unit tests in WmsDemoDataOperationGateTests.cs
T034: Conditional route-registration tests in DemoDataRouteRegistrationTests.cs
T035: Actor/confirmation/overlap endpoint tests in DemoDataSafetyEndpointTests.cs
T036: Secret-safe diagnostics tests in WmsDemoDataDiagnosticsTests.cs
```

---

## Implementation Strategy

### MVP First — User Story 1

1. Complete Setup and Foundation.
2. Write T009–T011 before implementing the seed slice.
3. Complete T012–T019.
4. Stop and have a developer run the focused tests and empty-database seed walkthrough.
5. Proceed only after the full bounded dataset and summary are coherent.

### Incremental Delivery

1. **US1**: Seed a complete empty demo database.
2. **US2**: Make seed repeatable, resumable for compatible partial state, and conflict-safe.
3. **US3**: Add atomic clear and known-state reseed.
4. **US4**: Complete fail-closed registration, confirmation, overlap, and diagnostic protections.
5. **Polish**: Finalize documentation and contract/scope consistency.

### Review Boundaries

- Review each task or tightly related test/implementation pair separately.
- Do not combine schema, UI, 1C, deployment, or generic framework work with this feature.
- Stop at any checkpoint if an existing domain use case cannot produce the planned state; update the plan before introducing a demo-only persistence shortcut.

---

## Developer-Controlled Validation Commands

These commands are recommendations only and are not executed automatically by task implementation:

```powershell
dotnet build Myrmex.slnx -nologo -v:minimal
dotnet test Myrmex.Tests/Myrmex.Tests.csproj --settings Myrmex.Tests/local.runsettings --filter "FullyQualifiedName~Myrmex.Tests.Wms.DemoData"
dotnet test Myrmex.Tests/Myrmex.Tests.csproj --settings Myrmex.Tests/local.runsettings
dotnet run --project Myrmex.AppHost/Myrmex.AppHost.csproj
```

Database prerequisites and manual API/WebApp validation are defined in `specs/094-full-wms-demo-data/quickstart.md`. No migration generation or application is expected for this feature.

## Notes

- `[P]` means different files with no incomplete same-phase dependency.
- User-story labels provide specification traceability.
- Tests protect behavior at the lowest owning layer; do not duplicate service matrices through HTTP.
- No task authorizes builds, tests, startup, database changes, migrations, Docker, or infrastructure operations without developer action.
