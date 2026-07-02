# Tasks: Phase 2 Deterministic Legacy List Ordering

**Input**: Design documents from `/specs/090-normalize-wms-list-contracts/`

**Prerequisites**: `plan.md`, `spec.md`, completed `research.md`, `data-model.md`, and `quickstart.md`

**Tests**: Focused handler/persistence tests are required because deterministic EF ordering across page boundaries is the changed regression risk. No endpoint, API-client, WebApp, contract, resource, schema, migration, or import tests are included because those boundaries do not change.

**Organization**: Tasks are grouped by the two current Phase 2 user stories. Completed Phase 1 audit outcomes in `spec.md` do not generate implementation tasks.

## Phase 1: Setup

**Purpose**: Confirm that existing infrastructure is sufficient.

No setup tasks are required. The WMS module, SQL Server-backed `TestWmsDbContext`, domain seed patterns, and xUnit project already exist. Do not add dependencies or test infrastructure.

---

## Phase 2: Foundational

**Purpose**: Confirm that no shared prerequisite is needed before slice work.

No foundational tasks are required. Each list owns its explicit sorting expression and can be changed independently. Do not introduce shared sorting helpers, contracts, or abstractions.

---

## Phase 3: User Story 1 - Stable Legacy List Paging (Priority: P1) 🎯 MVP

**Goal**: Give Zones, Storage Locations, SKUs, and UoM a total, repeatable order by appending database-ascending entity ID to every existing supported and fallback primary order.

**Independent Test**: In each slice, seed three records with the same Name, request two adjacent pages for ascending and descending Name sorting, and verify the concatenated IDs equal SQL Server `uniqueidentifier` order with no duplicate or omitted record.

### Tests for User Story 1

> Write these focused handler/persistence tests before their corresponding implementation task. The expected pre-implementation failure is an ID sequence that is not guaranteed to match the explicit SQL Server order.

- [ ] T001 [P] [US1] Add a two-direction `HandleAsync_WhenNameValuesMatch_OrdersByIdAcrossPages` theory using one valid Warehouse, three same-name Zones, adjacent pages, and `SqlGuid` expected ordering in `Myrmex.Tests/Wms/Topology/Features/Zones/ListZonesHandlerTests.cs`
- [ ] T002 [P] [US1] Add a two-direction `HandleAsync_WhenNameValuesMatch_OrdersByIdAcrossPages` theory using valid Warehouse/Zone/type/status relationships, three same-name Storage Locations, adjacent pages, and `SqlGuid` expected ordering in `Myrmex.Tests/Wms/Topology/Features/StorageLocations/ListStorageLocationsHandlerTests.cs`
- [ ] T003 [P] [US1] Extend the existing SKU handler suite with a two-direction `HandleAsync_WhenNameValuesMatch_OrdersByIdAcrossPages` theory, returning seeded entities from helpers as needed and comparing adjacent-page IDs with `SqlGuid` order in `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/ListStockKeepingUnitsHandlerTests.cs`
- [ ] T004 [P] [US1] Extend the existing UoM handler suite with a two-direction `HandleAsync_WhenNameValuesMatch_OrdersByIdAcrossPages` theory, returning seeded entities from helpers as needed and comparing adjacent-page IDs with `SqlGuid` order in `Myrmex.Tests/Wms/Catalog/Features/UnitsOfMeasure/ListUnitsOfMeasureHandlerTests.cs`

### Implementation for User Story 1

- [ ] T005 [P] [US1] After T001, append ascending `ThenBy(x => x.Id)` to the Code, Name, CreatedAtUtc, UpdatedAtUtc, IsActive, and fallback branches without changing primary ordering in `Myrmex.Modules.Wms/Topology/Features/Zones/ListZones.cs`
- [ ] T006 [P] [US1] After T002, append ascending `ThenBy(x => x.Id)` to the Code, Name, IsPickable, CreatedAtUtc, UpdatedAtUtc, IsActive, and fallback branches without changing primary ordering in `Myrmex.Modules.Wms/Topology/Features/StorageLocations/ListStorageLocations.cs`
- [ ] T007 [P] [US1] After T003, append ascending `ThenBy(x => x.Id)` to the Code, Name, IsActive, and fallback branches without changing primary ordering in `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/ListStockKeepingUnits.cs`
- [ ] T008 [P] [US1] After T004, append ascending `ThenBy(x => x.Id)` to the Code, Name, IsActive, and fallback branches without changing primary ordering in `Myrmex.Modules.Wms/Catalog/Features/UnitsOfMeasure/ListUnitsOfMeasure.cs`

**Checkpoint**: All four handlers retain their existing primary sort behavior and produce complete, stable adjacent pages for duplicate Name values in both primary directions.

---

## Phase 4: User Story 2 - Protect Changed Ordering Behavior (Priority: P2)

**Goal**: Ensure the focused handler suites protect the changed ordering risk without duplicating coverage at unrelated boundaries.

**Independent Test**: Review the four focused suites and confirm each would fail if its handler's ID tie-breaker were removed, exercises both Name directions and a page boundary, and uses SQL Server-compatible ID expectations.

- [ ] T009 [US2] Review and tighten the four focused ordering theories so each isolates duplicate-primary-value paging risk, covers both Name directions, asserts `SqlGuid` ID order across adjacent pages, and adds no endpoint/client/UI coverage in `Myrmex.Tests/Wms/Topology/Features/Zones/ListZonesHandlerTests.cs`, `Myrmex.Tests/Wms/Topology/Features/StorageLocations/ListStorageLocationsHandlerTests.cs`, `Myrmex.Tests/Wms/Catalog/Features/StockKeepingUnits/ListStockKeepingUnitsHandlerTests.cs`, and `Myrmex.Tests/Wms/Catalog/Features/UnitsOfMeasure/ListUnitsOfMeasureHandlerTests.cs`

**Checkpoint**: The smallest behavior-owning test set protects all four changed list slices.

---

## Phase 5: Polish & Cross-Cutting Validation

**Purpose**: Perform static scope and consistency checks without expanding Phase 2.

- [ ] T010 Verify every supported and fallback switch branch has ascending ID tie resolution, primary expressions/directions and `Skip`/`Take` placement are unchanged, and no out-of-scope files changed in `Myrmex.Modules.Wms/Topology/Features/Zones/ListZones.cs`, `Myrmex.Modules.Wms/Topology/Features/StorageLocations/ListStorageLocations.cs`, `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/ListStockKeepingUnits.cs`, and `Myrmex.Modules.Wms/Catalog/Features/UnitsOfMeasure/ListUnitsOfMeasure.cs`

### Developer-Controlled Validation

Do not run these automatically. The developer may execute the focused validation from `specs/090-normalize-wms-list-contracts/quickstart.md`:

```powershell
dotnet test Myrmex.Tests/Myrmex.Tests.csproj --filter "FullyQualifiedName~ListZonesHandlerTests|FullyQualifiedName~ListStorageLocationsHandlerTests|FullyQualifiedName~ListStockKeepingUnitsHandlerTests|FullyQualifiedName~ListUnitsOfMeasureHandlerTests"
```

Optional broader compilation check:

```powershell
dotnet build Myrmex.slnx --no-restore
```

Do not start WebApp/AppHost, run Docker or infrastructure, generate/apply migrations, or update a database.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup and Foundational**: No work is required; Phase 3 can begin immediately.
- **User Story 1**: Test tasks T001–T004 precede their corresponding implementation tasks T005–T008.
- **User Story 2**: T009 depends on completion of T001–T008 so it can review the final focused protection.
- **Polish**: T010 depends on T005–T009.

### Slice Dependencies

- T001 → T005 (Zones)
- T002 → T006 (Storage Locations)
- T003 → T007 (SKUs)
- T004 → T008 (UoM)
- T001–T008 → T009 → T010

### Parallel Opportunities

- T001, T002, T003, and T004 modify different test files and can run in parallel.
- After its test task is complete, each implementation pair can proceed independently: T005, T006, T007, and T008 can run in parallel.
- T009 and T010 are cross-slice checkpoints and run sequentially after slice work.

---

## Parallel Example: User Story 1

```text
Parallel test wave:
T001 Zones tests
T002 Storage Location tests
T003 SKU tests
T004 UoM tests

Parallel implementation wave after matching test tasks:
T005 Zones ordering (after T001)
T006 Storage Location ordering (after T002)
T007 SKU ordering (after T003)
T008 UoM ordering (after T004)
```

---

## Implementation Strategy

### MVP First

1. Complete T001–T004 to define the focused regression expectations.
2. Complete T005–T008 independently by slice.
3. Stop and request the developer-controlled focused test run.
4. User Story 1 is the MVP: all four legacy lists have deterministic page ordering with focused protection.

### Incremental Delivery

1. Implement and review one test/handler pair at a time if parallel work is unavailable.
2. Keep each pair independently reviewable and avoid touching shared contracts or UI code.
3. Complete T009 to verify minimal, failure-sensitive coverage across all slices.
4. Complete T010 as the final static scope gate.

## Notes

- `[P]` tasks change separate files and may run in parallel subject to their stated pair dependency.
- Tests intentionally use duplicate Name values because Code is identity-like/unique.
- Secondary ID ordering remains ascending even when the primary order is descending.
- `System.Data.SqlTypes.SqlGuid` is required for SQL Server-compatible expected ordering.
- Existing Phase 1 audit findings outside this narrow scope remain deferred.
