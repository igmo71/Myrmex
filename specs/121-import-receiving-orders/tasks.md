# Tasks: Import External Receiving Orders

**Input**: [spec.md](spec.md), [plan.md](plan.md), [data-model.md](data-model.md),
[receiving-order-import.md](contracts/receiving-order-import.md), and
[quickstart.md](quickstart.md)

**Policy**: Feature-specific implementation tasks only. Automated tests and
developer-controlled operations are not tasks.

## Format: `[ID] [P?] [Story?] Description with exact repository path`

- **[P]**: Can run in parallel after its listed dependencies because it affects different
  files.
- **[Story]**: Maps a task to the corresponding user story in the specification.

## Phase 1: Foundational Prerequisites

**Purpose**: Add the WMS-owned configuration and durable import identity required before
external documents can create or refresh receiving plans.

- [ ] T001 Extend Warehouse default receiving-location behavior and update semantics in `Myrmex.Modules.Wms/Topology/Domain/Warehouses/Warehouse.cs`.
- [ ] T002 Configure only the Warehouse default receiving-location FK and restrictive persistence mapping in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/WarehouseConfiguration.cs`.
- [ ] T003 [P] Extend Warehouse details and update request contracts with the default receiving-location value in `Myrmex.Shared/Wms/Topology/WarehouseDetails.cs` and `Myrmex.Shared/Wms/Topology/UpdateWarehouseDetailsRequest.cs`.
- [ ] T004 Validate the selected Warehouse-scoped selectable Receiving location and apply the default-location update through the existing application/PUT flow and details projection in `Myrmex.Modules.Wms/Topology/Features/Warehouses/UpdateWarehouseDetails.cs`, `Myrmex.Modules.Wms/Topology/Endpoints/WarehouseEndpoints.cs`, and `Myrmex.Modules.Wms/Topology/Features/Warehouses/WarehouseDetails.cs`.
- [ ] T005 Update the topology API client and existing Warehouse edit dialog to select, clear, display, and save a warehouse-scoped selectable Receiving location in `Myrmex.WebApp/Wms/Topology/WmsTopologyApiClient.cs` and `Myrmex.WebApp/Components/Pages/Wms/Topology/WarehousePages/WarehouseEditDialog.razor`.
- [ ] T006 Add only ReceivingOrder-owned validated external import-state initialization/access behavior needed by persistence and matching in `Myrmex.Modules.Wms/Receiving/Domain/ReceivingOrders/ReceivingOrder.cs`.
- [ ] T007 Map ReceivingOrder external import state, its filtered unique external-key index, and associated persistence error identity in `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/ReceivingOrderConfiguration.cs`, `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDatabaseNames.cs`, and `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsPersistenceExceptionMapper.cs`.

**Checkpoint**: A WmsOperator can configure a valid local Receiving location for an
existing Warehouse; WMS has an enforceable durable external key ready for imported orders.

---

## Phase 2: User Story 1 - Import Receiving Plans for a Period (Priority: P1)

**Goal**: An authorized user imports eligible 1C receiving documents for a period and
gets Created/Updated/Skipped/Failed document results.

**Independent Verification**: With valid imported dependencies and an eligible 1C
document, a developer starts an import from the WebApp and observes one new Draft plan
and a document-level Created result.

- [ ] T008 [P] [US1] Add date-range request, receiving-import response, outcome, operation-error, and document-result DTOs in `Myrmex.Shared/Integrations/OneC/ReceivingOrderImportRequest.cs`, `Myrmex.Shared/Integrations/OneC/ReceivingOrderImportResponse.cs`, and `Myrmex.Shared/Integrations/OneC/ReceivingOrderImportDocumentResult.cs`.
- [ ] T009 [P] [US1] Add the configured receiving-document entity set and its startup validation to `Myrmex.Integrations/OneC/Configuration/OneCOptions.cs`.
- [ ] T010 [US1] Implement typed `Document_ПриходныйОрдерНаТовары` and `Товары` DTOs plus the raw OData period query with deterministic Date/Ref_Key ordering and posted/deletion/status filters in `Myrmex.Integrations/OneC/ReceivingOrders/ReceivingOrderSourceRecord.cs`, `Myrmex.Integrations/OneC/ReceivingOrders/ReceivingOrderSourceLineRecord.cs`, and `Myrmex.Integrations/OneC/ReceivingOrders/ReceivingOrderOneCSource.cs`.
- [ ] T011 [US1] Map eligible raw source records to source-neutral import values; validate document/line identities, map `Количество` to planned quantity, retain `КоличествоУпаковок` only for diagnostics, and return `UnsupportedPackage` for a non-empty `Упаковка_Key` in `Myrmex.Integrations/OneC/ReceivingOrders/ReceivingOrderOneCMapper.cs`.
- [ ] T012 [US1] Extract the existing Draft-line delete/reassign transaction behavior into a reusable Receiving application service used by the existing edit path in `Myrmex.Modules.Wms/Receiving/Features/ReceivingOrders/ReceivingOrderDraftReconciler.cs` and `Myrmex.Modules.Wms/Receiving/Features/ReceivingOrders/UpdateReceivingOrderDraft.cs`.
- [ ] T013 [US1] Implement the happy-path public source-neutral single-document import command to resolve imported Warehouse/SKU dependencies, derive and validate the Warehouse default Receiving location, aggregate lines by SKU, and create a Draft order in `Myrmex.Modules.Wms/Receiving/Features/ReceivingOrders/ImportExternalReceivingOrder.cs`.
- [ ] T014 [US1] Wrap each single-document import in its WMS transaction; roll back on failures, map persistence/domain/dependency errors to stable outcomes, and emit structured Created/Skipped/Failed diagnostics in `Myrmex.Modules.Wms/Receiving/Features/ReceivingOrders/ImportExternalReceivingOrder.cs`.
- [ ] T015 [US1] Validate that the selected start/end date range is complete and ordered before contacting 1C, then read the selected period, map every document, dispatch one WMS import command per document, and accumulate operator results in `Myrmex.Integrations/OneC/ReceivingOrders/ReceivingOrderOneCImport.cs`.
- [ ] T016 [US1] Register the receiving-order source/import operation and expose the WmsOperator-authorized manual endpoint in `Myrmex.Integrations/OneC/OneCIntegrationModule.cs` and `Myrmex.Integrations/OneC/Endpoints/OneCEndpoints.cs`.
- [ ] T017 [US1] Add the receiving-order import client method and date-range/result state to `Myrmex.WebApp/Integrations/OneC/OneCIntegrationApiClient.cs` and `Myrmex.WebApp/Components/Pages/Integrations/OneC/Index.razor`.
- [ ] T018 [US1] Render the period selector, import action, Created/Updated/Skipped/Failed counters, request-wide error, and per-document identity/reason results in `Myrmex.WebApp/Components/Pages/Integrations/OneC/Index.razor`, `Myrmex.WebApp/Resources/Localization/SharedResource.resx`, `Myrmex.WebApp/Resources/Localization/SharedResource.en-US.resx`, and `Myrmex.WebApp/Resources/Localization/SharedResource.ru-RU.resx`.

**Checkpoint**: An authorized user selects a valid period and imports one eligible
document into a new local Draft order with resolvable dependencies and a clear result.

---

## Phase 3: User Story 2 - Refresh an Imported Draft Plan (Priority: P2)

**Goal**: A re-import reconciles the matching Draft receiving order without duplicates
and never changes an order that has left Draft.

**Independent Verification**: After a successful import, a developer changes a mapped
external header or planned quantity, re-imports, and observes the same local Draft order
updated with no duplicate lines.

- [ ] T019 [US2] Extend the source-neutral import command to locate ReceivingOrder by durable external key, compare mapped header/aggregated plan values, reconcile matching Draft orders through `ReceivingOrderDraftReconciler`, and preserve retained line identities in `Myrmex.Modules.Wms/Receiving/Features/ReceivingOrders/ImportExternalReceivingOrder.cs`.
- [ ] T020 [US2] Record the latest opaque data version/observation time after successful creation, update, or mapped-plan-equal Skip; return Skip for non-Draft matches without mutation in `Myrmex.Modules.Wms/Receiving/Domain/ReceivingOrders/ReceivingOrder.cs` and `Myrmex.Modules.Wms/Receiving/Features/ReceivingOrders/ImportExternalReceivingOrder.cs`.
- [ ] T021 [US2] Map Updated and non-Draft/unchanged Skipped outcomes into the manual import response and structured logs in `Myrmex.Integrations/OneC/ReceivingOrders/ReceivingOrderOneCImport.cs`.
- [ ] T022 [US2] Present Updated and Skipped document reasons without treating a changed opaque version alone as a plan change in `Myrmex.WebApp/Components/Pages/Integrations/OneC/Index.razor`.

**Checkpoint**: A matching Draft order is reconciled in place; unchanged mapped data and
non-Draft matches are visibly Skipped, and no duplicate order or plan line is created.

---

## Phase 4: User Story 3 - Continue After Individual Document Problems (Priority: P3)

**Goal**: One bad external document is clearly reported without concealing results for
other documents in the selected period.

**Independent Verification**: A period containing one valid document and one document
with an unresolved dependency produces an outcome for each; the valid document remains
imported and the failure identifies the affected document/reason.

- [ ] T023 [US3] Isolate malformed source, unsupported-package, dependency, and WMS command failures per document while continuing the selected-period loop in `Myrmex.Integrations/OneC/ReceivingOrders/ReceivingOrderOneCImport.cs`.
- [ ] T024 [US3] Add stable document-level failure reason mapping and structured completion diagnostics with processed/created/updated/skipped/failed counts in `Myrmex.Integrations/OneC/ReceivingOrders/ReceivingOrderOneCImport.cs` and `Myrmex.Integrations/OneC/ReceivingOrders/ReceivingOrderImportReasons.cs`.
- [ ] T025 [US3] Display document identity, immediate failure/skip reason, and request-wide transport/configuration errors distinctly; add any new result/failure localization keys in `Myrmex.WebApp/Components/Pages/Integrations/OneC/Index.razor`, `Myrmex.WebApp/Resources/Localization/SharedResource.resx`, `Myrmex.WebApp/Resources/Localization/SharedResource.en-US.resx`, and `Myrmex.WebApp/Resources/Localization/SharedResource.ru-RU.resx`.

**Checkpoint**: A document-level failure never hides the outcomes of other documents, and
the operator can identify the document and immediate reason.

---

## Dependencies & Execution Order

- Phase 1 blocks all user stories: the Warehouse setting is the only supported local
  receiving-location configuration path, and ReceivingOrder external state is the durable
  document match key.
- US1 depends on T001–T007; within US1, T008/T009 may run in parallel, T010 precedes
  T011/T015, T012 precedes T013, T013/T014 precede T015, T015/T016 precede T017/T018.
- US2 depends on the completed US1 import command and result contract (T013–T018).
- US3 depends on the US1 import loop/response (T015–T018) and can be completed before or
  alongside US2 once those prerequisites are present.

### Parallel Opportunities

- T003 and T006 can run in parallel after T001; T007 follows T006.
- T008 and T009 can run in parallel with each other and with the completed foundational
  persistence work.
- After T015, T019–T022 (US2) and T023–T025 (US3) can proceed in parallel on their
  respective command/import/UI files with ordinary integration coordination.

## Developer Actions

- Generate, review, and apply the WMS EF Core migration for Warehouse default-location and
  ReceivingOrder external-import-state schema changes.
- Configure the 1C receiving-order entity-set and secure source connection settings.
- Build the affected projects or solution after implementation.
- Perform the developer manual-acceptance scenarios in [quickstart.md](quickstart.md),
  including Warehouse default-location configuration, create, Draft refresh, unchanged
  re-import, non-Draft skip, and per-document failure isolation.
- Create the Git commit and pull request after implementation review.

## Notes

- All numbered tasks use the existing WMS/Integration boundary and exclude
  `SynchronizationRequest`, workers, background processing, reference repair, source
  location mapping, package conversion, conflict workflows, sagas, receipts, locks, and
  distributed transactions.
- Automated tests and test infrastructure are intentionally excluded by repository
  governance.
