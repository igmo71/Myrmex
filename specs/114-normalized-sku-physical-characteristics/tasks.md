# Tasks: Normalized SKU Physical Characteristics

**Input**: Design documents from `specs/114-normalized-sku-physical-characteristics/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: No automated test tasks are included. The repository has no tracked test project, and this feature must not add testing infrastructure. Focused manual verification begins only after the user confirms that they have built the solution, applied the user-generated migration, and started the application.

**Organization**: Tasks are grouped by user story so normalization, display, and refresh behavior can be implemented in priority order, followed by one combined focused verification pass.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel after its stated dependencies because it changes different files.
- **[Story]**: Maps a task to User Story 1, 2, or 3.
- Every task names the exact implementation file or documentation path it affects.

## Phase 1: Setup — Source-Contract Verification

**Purpose**: Confirm the provisional 1C interpretation before formula-dependent implementation begins.

- [X] T001 Validate `source numerator / source denominator × unit numerator / unit denominator`, exact `ТипИзмеряемойВеличины` wire values, ordinary nullable numeric DTO compatibility, and planned `decimal(28,12)` persistence against additional representative linked 1C records; record the conclusion in `specs/114-normalized-sku-physical-characteristics/research.md` and update `specs/114-normalized-sku-physical-characteristics/plan.md` plus affected contracts before continuing if evidence contradicts the design

**Checkpoint**: The working source contract is confirmed or the design artifacts are corrected; formula-dependent implementation may begin.

---

## Phase 2: Foundational — Shared Source, Domain, and Read Contracts

**Purpose**: Establish the source fields and WMS-owned canonical state required by every user story.

**⚠️ CRITICAL**: Complete this phase before user-story implementation.

- [X] T002 [P] Extend the existing SKU OData projection and source record with the 16 physical-characteristic fields using ordinary nullable numeric properties and existing deserialization-failure behavior in `Myrmex.Integrations/OneC/StockKeepingUnits/StockKeepingUnitOneCSource.cs` and `Myrmex.Integrations/OneC/StockKeepingUnits/StockKeepingUnitSourceRecord.cs`
- [X] T003 [P] Extend the existing unit projection and source record with `ТипИзмеряемойВеличины`, `Числитель`, and `Знаменатель` for operation-scoped resolution in `Myrmex.Integrations/OneC/UnitsOfMeasure/UnitOfMeasureOneCSource.cs` and `Myrmex.Integrations/OneC/UnitsOfMeasure/UnitOfMeasureSourceRecord.cs`
- [X] T004 [P] Add the four nullable canonical properties and import-only mutation semantics, including null-versus-zero handling and change detection, in `Myrmex.Modules.Wms/Catalog/Domain/StockKeepingUnits/StockKeepingUnit.cs` and configure the verified ordinary nullable decimal mapping in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/StockKeepingUnitConfiguration.cs`
- [X] T005 Extend `ImportStockKeepingUnits.Item` and its handler to carry and apply only the four normalized nullable decimals without 1C conversion metadata in `Myrmex.Modules.Wms/Catalog/Features/Imports/ImportStockKeepingUnits.cs`
- [X] T006 Extend the existing SKU read response and projections with the four nullable canonical values while leaving create/update and lookup requests unchanged in `Myrmex.Shared/Wms/Catalog/StockKeepingUnitDetails.cs` and `Myrmex.Modules.Wms/Catalog/Features/StockKeepingUnits/StockKeepingUnitDetails.cs`

**Checkpoint**: Existing SKU flows can accept, persist, and expose canonical values without any source-unit details entering WMS.

---

## Phase 3: User Story 1 — Synchronize Physical Characteristics (Priority: P1) 🎯 MVP

**Goal**: Normalize valid 1C weight, length, area, and volume values independently and synchronize them through the existing full and reactive SKU paths.

**Independent Test**: After the user performs the build, migration application, and startup, synchronize representative SKUs and verify through the existing SKU GET response that valid values are stored in kg, m, m², and m³; source-numerator zero remains numeric zero; unit-numerator zero and zero denominators make only their characteristic absent; volume works without linear dimensions; and each normalization issue is logged once by the caller.

### Implementation for User Story 1

- [X] T007 [US1] Implement the SKU-specific normalizer with the provisional verified formula, measurement-type matching, independent characteristic outcomes, source-zero/unit-zero rules, overflow handling, and structured issue return without logging in `Myrmex.Integrations/OneC/StockKeepingUnits/StockKeepingUnitPhysicalCharacteristicsNormalizer.cs`
- [X] T008 [US1] Extend the existing full SKU import to load unit definitions once per operation, normalize each materialized SKU, log each returned issue once in the caller, and dispatch the existing WMS batch command in `Myrmex.Integrations/OneC/StockKeepingUnits/StockKeepingUnitOneCImport.cs`
- [X] T009 [P] [US1] Extend reactive SKU synchronization to resolve distinct referenced units, normalize the current SKU, log each returned issue once in the caller, preserve existing base-UoM repair, and dispatch the existing one-item command in `Myrmex.Integrations/OneC/StockKeepingUnits/StockKeepingUnitOneCSynchronizer.cs`
- [X] T010 [P] [US1] Complete the entity and EF configuration required for four ordinary nullable decimal columns in `Myrmex.Modules.Wms/Catalog/Domain/StockKeepingUnits/StockKeepingUnit.cs` and `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/StockKeepingUnitConfiguration.cs`, and document the exact user-run command `dotnet ef migrations add AddSkuPhysicalCharacteristics --project Myrmex.Modules.Wms --startup-project Myrmex.ApiService --context WmsDbContext --output-dir Infrastructure/Persistence/Migrations` in `specs/114-normalized-sku-physical-characteristics/quickstart.md`; do not manually create or edit migration files or `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/WmsDbContextModelSnapshot.cs`, and leave migration generation and application to the user so the generated migration and snapshot can be reviewed later before application
- [X] T011 [US1] Replace the data-version-only early exit with an unchanged check that also compares all four incoming canonical nullable values so existing SKUs can receive initial physical-characteristic values after migration and factor-driven changes can refresh them, while preserving existing import accounting in `Myrmex.Modules.Wms/Catalog/Features/Imports/ImportStockKeepingUnits.cs`

**Checkpoint**: User Story 1 implementation supports initial population and synchronization through existing paths and is ready for the final combined verification. This is the suggested MVP scope.

---

## Phase 4: User Story 2 — View Available Characteristics (Priority: P2)

**Goal**: Show available canonical values read-only in the existing SKU edit view without changing grids, lookups, requests, navigation, or adding a screen.

**Independent Test**: After the user makes the updated application available, open existing SKU edit dialogs for all/some/no-value cases and verify canonical labels, meaningful precision, numeric zero versus “Not available,” and the absence of edit controls or new grid/lookup presentation.

### Implementation for User Story 2

- [X] T012 [P] [US2] Add localized physical-characteristic section and field labels to `Myrmex.WebApp/Resources/Localization/SharedResource.resx`, `Myrmex.WebApp/Resources/Localization/SharedResource.en-US.resx`, and `Myrmex.WebApp/Resources/Localization/SharedResource.ru-RU.resx`
- [X] T013 [US2] Render weight, length, area, and volume as culture-aware read-only text with kg, m, m², and m³ labels only in edit mode, preserving numeric zero and using `Common.NotAvailable` for null, in `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/SkuEditDialog.razor`

**Checkpoint**: User Story 2 implementation is ready for the final combined verification in the existing SKU edit experience with no new UI surface.

---

## Phase 5: User Story 3 — Refresh Changed or Removed Values (Priority: P3)

**Goal**: Replace changed normalized values and clear disabled or individually unresolvable values during repeated synchronization.

**Independent Test**: After the user makes the updated application available, synchronize an SKU, change one valid source value, disable another, invalidate a third, and change a referenced unit factor without changing the SKU data version; verify update, clearing, preservation of unaffected values, and unchanged behavior on an identical repeat.

### Implementation Coverage for User Story 3

User Story 3 requires no additional implementation task: T004, T005, T007, T008, T009, and the US1 correction in T011 collectively implement changed-value replacement, disabled/unresolvable clearing, unaffected-value preservation, and same-SKU-version unit-factor refresh.

**Checkpoint**: All three user stories are implemented and ready for one final focused verification through existing synchronization and UI paths.

---

## Phase 6: Polish, Scope Review, and Combined Verification

**Purpose**: Confirm constitutional scope containment, then perform one focused verification pass after the user completes all manual environment actions once.

- [X] T014 Review the delivered file set against the prohibited additions and acceptance contracts in `specs/114-normalized-sku-physical-characteristics/contracts/onec-normalization-contract.md`, `specs/114-normalized-sku-physical-characteristics/contracts/sku-details-ui-contract.md`, and `specs/114-normalized-sku-physical-characteristics/quickstart.md`; correct any scope drift without adding infrastructure, workflows, screens, grid/lookup presentation, performance work, or generalized conversion behavior
- [ ] T015 After all implementation tasks are complete and the user confirms they have built the solution, applied the migration, and started the application once, perform one focused pass covering normalization/canonical values, independent invalid-characteristic handling, null versus zero, volume-only behavior, single-owner logging, initial population, repeated refresh/clearing, same-SKU-version unit-factor changes, API response, read-only edit-dialog display, and absence of grid, lookup, workflow, or diagnostics scope drift using `specs/114-normalized-sku-physical-characteristics/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Starts immediately; T001 blocks formula-dependent implementation.
- **Foundational (Phase 2)**: Depends on T001; T002, T003, and T004 can proceed in parallel, then T005 depends on T004 and T006 depends on T004.
- **User Story 1 (Phase 3)**: Depends on Phase 2; T007 depends on T002 and T003, T008/T009 depend on T005 and T007, T010 depends on T004, and T011 depends on T005 and must complete before final verification.
- **User Story 2 (Phase 4)**: Implementation depends on T006; T013 also depends on T012.
- **User Story 3 (Phase 5)**: Depends on the shared and US1 import work, including T011; it adds no separate implementation task.
- **Polish and Combined Verification (Phase 6)**: T014 depends on all implementation work and may include review of the user-generated migration and snapshot before application; T015 depends on T014 and on the user's manual build, migration application, and application-start sequence.

### User Story Dependencies

- **User Story 1 (P1)**: Begins after the shared foundation and delivers initial population plus the MVP synchronization outcome.
- **User Story 2 (P2)**: UI implementation can begin after the read contract exists; final display verification uses values supplied by User Story 1.
- **User Story 3 (P3)**: Its refresh and clearing behavior is implemented by the shared/US1 import tasks, especially T011.

### Manual-Action Boundary

- Implementation tasks may complete the entity and EF configuration and document the migration-generation command, but MUST NOT execute builds, tests, EF commands, migration generation or application, database commands, or application startup.
- After implementation, the user may generate the migration with the documented command and pause before application so the generated migration and snapshot can be reviewed under T014.
- T015 begins only after all implementation work is complete and the user confirms that the required manual environment actions were completed once.
- No task creates or switches branches, commits, pushes, or creates another import/UI/diagnostics workflow.

### Parallel Opportunities

- After T001, T002, T003, and T004 affect separate files and can run in parallel.
- After the foundation, T007 and T010 can run in parallel.
- After T005 and T007, T008 and T009 can run in parallel; T011 can also proceed after T005 because it changes the WMS import handler rather than the integration callers.
- T012 can be prepared independently of backend synchronization once T006 fixes the response contract.
- Story implementation can be split across integration, WMS persistence/read-model, and WebApp files, while the combined verification retains all stated story dependencies.

---

## Parallel Example: User Story 1

```text
After T001 and Phase 2:
Task T007: Implement the SKU-specific normalizer.
Task T010: Complete the entity and EF configuration and document the exact user-run migration-generation command without creating migration or snapshot files.

After T005 and T007:
Task T008: Wire the full import caller.
Task T009: Wire the reactive synchronization caller.

After T005:
Task T011: Correct unchanged detection for initial population and factor-driven refresh.
```

## Parallel Example: User Story 2

```text
After T006:
Task T012: Add localization resources.
Then T013: Add the read-only edit-dialog section.
```

## Parallel Example: User Story 3

```text
No separate US3 implementation task is required.
T011 in User Story 1 supplies the unchanged-detection correction used by refresh and clearing.
Final behavior is checked once in T015 after all implementation and the user's manual environment actions.
```

---

## Implementation Strategy

### MVP First — User Story 1

1. Complete T001 before formula-dependent code.
2. Complete the shared foundation in Phase 2.
3. Complete User Story 1 integration, WMS, EF configuration and migration-command documentation, and read-contract work.
4. Continue through the desired UI and refresh scope before requesting the user's single manual environment sequence.
5. Perform the combined T015 verification only after all implementation and scope review are complete.

### Incremental Delivery

1. Setup + Foundational → verified contract and canonical data path ready.
2. User Story 1 → synchronization works through existing paths (MVP).
3. User Story 2 → values become visible read-only in the existing edit dialog.
4. User Story 3 → shared/US1 tasks already provide repeat synchronization refresh and clearing.
5. Polish → scope review, one user-run environment setup, and one combined focused verification pass.

## Notes

- `[P]` tasks change different files and have no dependency on another incomplete task at that point.
- No automated test project or testing infrastructure is introduced.
- Entity/EF configuration and migration-command documentation are allowed; migration generation, migration application, and every EF or database command remain user-owned manual actions, with generated files reviewable before application.
- Build, test, application-start, branch, commit, and push actions are absent from executable tasks.
- If T001 contradicts the provisional formula or wire contract, update planning artifacts before continuing rather than implementing heuristics or generalized conversion infrastructure.
