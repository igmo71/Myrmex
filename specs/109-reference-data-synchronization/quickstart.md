# Quickstart: Validate Feature 109

This guide records acceptance expectations after implementation. The agent may modify domain/application source, test source, and EF mappings only. The developer generates, reviews, and applies migrations. The agent must not generate, create, or edit migration `.cs`, `.Designer.cs`, or `WmsDbContextModelSnapshot` files, and it must not run build, test, migration-generation, database-update, AppHost, Docker, application-startup, or other environment-changing commands unless explicitly requested.

## Prerequisites

- Branch `109-add-reactive-and-on-demand-reference-data-synchronization`.
- Configured `ConnectionStrings:MyrmexDatabase` with the existing WMS and `integration` schemas.
- Valid existing 1C OData settings and machine API key/source identity settings.
- A controllable 1C dataset containing Warehouse, UoM, and SKU objects with observable `DataVersion` values; Warehouse/SKU folder and deletion-mark samples; and an SKU with a base-UoM reference.
- Existing Feature 104 synchronization worker configuration and migrations already applied.

## 1. Verify the Developer-Generated Additive Migration Shape

After the EF mappings are complete, the developer generates, reviews, and applies the migration. The expected shape is limited to:

- three nullable `ExternalDataVersion varbinary(128)` columns, one on each of `wms.warehouses`, `wms.units_of_measure`, and `wms.stock_keeping_units`;
- no changes to existing `ExternalRefKey` or `LastImportedAtUtc` columns or their filtered unique indexes;
- existing linked-row values remaining unchanged, with `ExternalDataVersion = NULL` representing the legacy unknown-version state until version-aware synchronization succeeds.

## 2. Test-Source and Validation Expectations

Prepare focused test source in the existing OneC transport/import/endpoint/synchronization and WMS import-handler/persistence suites. Expected coverage is the minimal matrix in [plan.md](./plan.md#risk-based-test-plan). Do not add a second Feature 104 processor/retry/recovery suite or identical per-reference-type matrices. Test execution is not part of this plan and remains developer-controlled.

## 3. Verify Reference Notification Intake

For each reference route, send only the required machine payload:

```json
{
  "Ref_Key": "<non-empty-source-guid>",
  "DataVersion": "<non-empty-base64-version>"
}
```

Routes:

- `POST /api/integrations/1c/warehouses/changed`
- `POST /api/integrations/1c/uoms/changed`
- `POST /api/integrations/1c/skus/changed`

Expected:

- valid machine authentication returns an empty `202 Accepted` only after durable insert/duplicate resolution;
- the stored stable entity type is `Warehouse`, `UnitOfMeasure`, or `StockKeepingUnit` respectively;
- replaying the same identity/version resolves the existing request;
- Receiving and Shipping notification routes behave unchanged.

The intake contract is detailed in [reference-change-notifications.md](./contracts/reference-change-notifications.md).

## 4. Verify Version-Aware Application

Use one linked record and current source object for the broader representative versioning cases, and include the compact same-version smoke case for all three explicit import handlers:

1. Start with a legacy linked row whose `ExternalDataVersion` is null. Synchronize it and verify current values, non-empty version, and import time are stored.
2. For `ImportWarehouses`, `ImportUnitsOfMeasure`, and `ImportStockKeepingUnits`, prove only `same current DataVersion -> Unchanged -> no timestamp mutation -> no aggregate mutation or domain event`, using one parameterized theory or one compact existing-handler test per type.
3. Change only the source version. Synchronize again. Verify the new version/import time are stored and the record counts as applied/updated, without a business-detail or activation event.
4. Change a source-owned business value and version. Verify the value and version are applied and only the corresponding business event is emitted.
5. Verify changing a buffer originally passed into or returned from external import state cannot mutate the stored version.

## 5. Verify Lifecycle and Folder Rules

- Linked active + deletion mark: deactivates, stores current version/import time, and emits one deactivation event.
- Linked already inactive + newer deletion mark: stores metadata without another deactivation event.
- Unlinked deletion mark: controlled skip; no creation or detail/dependency validation.
- Active source + linked inactive: reactivates and applies current values.
- Warehouse/SKU folder: controlled skip.
- UoM: no folder property or folder outcome is introduced.

Applied, unchanged, and controlled skip are operation outcomes. The reactive request reaches existing durable `Completed`; no new durable outcome statuses appear.

## 6. Verify On-Demand and Bounded SKU Repair

Exercise the internal synchronize-one service directly through its application test seam; no public route or WebApp action should exist.

- Existing current object returns Applied or Unchanged.
- Missing current object returns NotFound without local create/deactivate.
- Same-type busy gate returns Busy without reading the source.
- Caller cancellation throws cancellation to the caller.
- SKU with a missing/inactive linked base UoM synchronizes exactly that UoM once and applies the SKU at most one additional time.
- Failed UoM repair does not recurse, does not synchronize another dependency, and does not exceed two SKU applications.

See [reference-synchronize-one.md](./contracts/reference-synchronize-one.md).

## 7. Verify Coordination

Hold a manual SKU import after its first source read and before completion:

- another SKU manual import retains the existing fail-fast `409` behavior;
- reactive/on-demand SKU returns the appropriate retryable/Busy result;
- the first lease remains held across every SKU page and batch;
- a Warehouse or UoM synchronization can proceed independently;
- cancellation/failure releases the lease;
- no SQL/distributed/cross-process locking artifact is present.

## 8. Verify Shutdown Cancellation and Recovery

For the shutdown-cancellation acceptance case, cancel the processor/application stopping token after a reference request becomes `Processing`:

- `OperationCanceledException` propagates and is rethrown as shutdown cancellation only because the processor/application stopping token is cancelled;
- the request remains `Processing` and is not immediately classified Completed, transient, permanent, or a new cancelled status;
- source timeout and non-shutdown failures continue through their normal transient/permanent classification;
- on later startup/polling, the existing Feature 104 abandoned-processing recovery applies its existing retry/exhaustion decision;
- retry after an already committed WMS change observes the stored current version, returns Unchanged, and emits no duplicate mutation/event.

## 9. Verify Source-Owned Local Editing

- Linked Warehouse: actual Name change fails; unchanged Name plus Description change succeeds.
- Linked UoM: actual Name or Symbol change fails; identical resubmission succeeds without mutation/event.
- Linked SKU: actual Name or base-UoM change fails; unchanged source values plus Description change succeeds.
- Linked lifecycle: an actual local deactivate/reactivate transition fails; a redundant request that changes no state remains a no-op.
- Unlinked records preserve existing edit/lifecycle behavior.
- No normal local request/response exposes external identity, version, or import time for mutation.

## 10. Verify Manual Import and WebApp Compatibility

For any later explicitly authorized developer-controlled runtime validation, exercise all three existing manual import actions twice with unchanged source versions.

Expected:

- routes, WmsOperator authorization, paging, structured errors, 50-error cap, and existing fields are unchanged;
- first import reports Created/Updated as appropriate;
- second import reports current records in `Unchanged`;
- `Processed = Created + Updated + Unchanged + Skipped + Failed`;
- manual cancellation preserves its current incomplete `Cancelled` response and committed SKU-page counts;
- `/integrations/1c` shows the Unchanged count beside existing totals with the same three buttons and no synchronize-one action;
- neutral/en-US renders `Unchanged`, and ru-RU renders the approved Russian label;
- existing record and operation errors still render unchanged.

The additive response contract is detailed in [manual-import-compatibility.md](./contracts/manual-import-compatibility.md).
