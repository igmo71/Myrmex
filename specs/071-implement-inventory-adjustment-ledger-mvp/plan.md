# Implementation Plan: Inventory Adjustment Ledger MVP

**Branch**: `071-implement-inventory-adjustment-ledger-mvp` | **Date**: 2026-06-18 | **Spec**: `specs/071-implement-inventory-adjustment-ledger-mvp/spec.md`

**Input**: Feature specification from `specs/071-implement-inventory-adjustment-ledger-mvp/spec.md`, stakeholder document `StakeholderDocs/Wms/Implement Inventory Adjustment Ledger MVP.md`, Myrmex Constitution, durable architecture/testing guidance, server-driven Inventory Balance list guidance, and current Inventory Balance backend/client/UI/test implementation.

## Summary

Introduce the first immutable Inventory Adjustment ledger slice for Myrmex. The feature replaces direct current-balance stock mutation paths with one business command and one public endpoint, `POST /api/wms/inventory/adjustments`, used for both existing-balance adjustments and missing-balance initialization from expected zero. The command calculates delta from an absolute counted quantity, writes immutable ledger history for material quantity changes, updates or creates the `InventoryBalance` snapshot, and uses strict Base64 rowversion concurrency semantics.

The design keeps `InventoryBalance` as the current-state snapshot and adds `InventoryTransaction` as the ledger aggregate root with immutable `InventoryLedgerEntry` children. The persistence plan uses one EF Core `SaveChangesAsync` as the database atomicity boundary for balance and ledger changes, adds SQL Server rowversion to `InventoryBalance`, and maps stale-state conflicts to `409 InventoryBalance.ConcurrencyConflict`.

## Technical Context

**Language/Version**: C# on the existing Myrmex .NET 10 solution.

**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core SQL Server provider, Blazor WebApp, MudBlazor, existing Myrmex command/query dispatchers, `ServiceResult`/ProblemDetails helpers, `WmsInventoryApiClient`, xUnit test project.

**Storage**: Existing SQL Server-backed WMS schema through `WmsDbContext`. Add `InventoryBalance.RowVersion`, `InventoryTransaction`, and `InventoryLedgerEntry` mappings plus a future EF Core migration during implementation. Do not generate the migration during planning.

**Testing**: Existing xUnit tests. Follow risk-based minimal testing: domain tests for invariants and immutability, handler/persistence tests for adjustment behavior and atomicity, provider-sensitive persistence tests for rowversion and unique-index translation where practical, focused endpoint/API-client tests for route/body/Base64/problem-details boundaries, and manual UI smoke validation for Blazor flows.

**Target Platform**: Existing Myrmex modular-monolith API service and Blazor WebApp.

**Project Type**: Brownfield WMS vertical slice spanning shared contracts, WMS backend domain/application/persistence/endpoints, WebApp API client and UI dialogs.

**Performance Goals**: Preserve current Inventory Balance list performance. A user starting from a loaded balance row can complete an existing-balance adjustment in under 1 minute. Adjustment command performs bounded reads and a single save for one SKU/location pair.

**Constraints**: Use one adjustment command and endpoint for existing and missing balances. Remove obsolete direct create/update mutation paths. Use SQL Server rowversion as `byte[]` in persistence and Base64 in public contracts. Use explicit expected-version validation plus EF concurrency protection. Do not automatically retry absolute adjustments. Do not introduce `InventoryAccount`, Transfer, LPN, history UI, zero-row deletion, event sourcing, generic repository, mediator, transaction abstraction, or speculative framework.

**Scale/Scope**: One WMS Inventory command slice, one public adjustment request contract, one updated balance details contract, two new domain entities, three persistence mappings or mapping updates, one migration shape, WebApp adjustment and initial-count UI updates, focused tests, and validation guidance.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Domain Model First**: PASS. The plan names Inventory Adjustment, InventoryBalance, InventoryTransaction, InventoryLedgerEntry, counted quantity, quantity delta, balance before/after, reason, strict expected balance version, no-op, and concurrency invariants before implementation mechanics.
- **Modular Monolith Boundaries**: PASS. Runtime changes stay in `Myrmex.Modules.Wms`, `Myrmex.Shared`, and `Myrmex.WebApp`. Public transport contracts remain in `Myrmex.Shared`; internal commands, handlers, EF mappings, projections, and domain entities remain in the WMS module.
- **Vertical Slice Delivery**: PASS. The slice covers shared contracts, endpoint, internal command handler, domain logic, persistence mappings, API client, WebApp UI, and focused tests. Public transport contracts are separate from internal command types.
- **Testing Discipline**: PASS with documented UI automation exception below. The plan identifies concrete regression risks and the lowest owning layer for each; UI behavior uses manual smoke validation unless implementation discovers a distinct UI risk not protected elsewhere.
- **Simplicity and Observability**: PASS. The plan uses current EF Core, dispatcher, Minimal API, API client, ProblemDetails, and MudBlazor patterns. It avoids new abstractions and provides capability-specific concurrency errors plus diagnostics through existing conventions.

## Project Structure

### Documentation (this feature)

```text
specs/071-implement-inventory-adjustment-ledger-mvp/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── inventory-adjustment-api-contract.md
│   └── inventory-adjustment-ui-contract.md
├── checklists/
│   └── requirements.md
└── spec.md
```

### Source Code (repository root)

```text
Myrmex.Shared/
└── Wms/Inventory/
    ├── AdjustInventoryBalanceRequest.cs              # create
    ├── InventoryBalanceDetails.cs                    # modify: add BalanceVersion
    ├── CreateInventoryBalanceRequest.cs              # remove
    └── UpdateInventoryBalanceQuantityRequest.cs      # remove

Myrmex.Modules.Wms/
├── Inventory/
│   ├── Domain/
│   │   ├── InventoryBalances/
│   │   │   └── InventoryBalance.cs                   # modify: RowVersion and adjustment/no-op-safe behavior
│   │   └── InventoryTransactions/                    # create
│   │       ├── InventoryTransaction.cs
│   │       ├── InventoryTransactionType.cs
│   │       └── InventoryLedgerEntry.cs
│   ├── Features/
│   │   ├── InventoryAdjustments/
│   │   │   └── AdjustInventoryBalance.cs             # create
│   │   └── InventoryBalances/
│   │       ├── CreateInventoryBalance.cs             # remove
│   │       ├── UpdateInventoryBalanceQuantity.cs     # remove
│   │       └── InventoryBalanceQueryableExtensions.cs # modify: project BalanceVersion
│   └── Endpoints/
│       ├── InventoryAdjustmentEndpoints.cs           # create
│       ├── InventoryBalanceEndpoints.cs              # modify: remove create/update mutation routes
│       └── InventoryEndpoints.cs                     # modify: map adjustment endpoints
└── Infrastructure/Persistence/
    ├── WmsDbContext.cs                               # modify: add DbSet(s)
    ├── WmsDbContextSaveExtensions.cs                 # modify: map DbUpdateConcurrencyException
    ├── WmsDatabaseNames.cs                           # modify: add table/key/index names
    ├── WmsPersistenceExceptionMapper.cs              # modify: duplicate insert -> concurrency code for adjustment path
    ├── Configurations/
    │   ├── InventoryBalanceConfiguration.cs          # modify: rowversion
    │   ├── InventoryTransactionConfiguration.cs      # create
    │   └── InventoryLedgerEntryConfiguration.cs      # create
    └── Migrations/
        └── <timestamp>_AddInventoryAdjustmentLedger.cs # create during implementation only

Myrmex.WebApp/
├── Wms/Inventory/WmsInventoryApiClient.cs
└── Components/Pages/Wms/Inventory/InventoryBalancePages/
    ├── Index.razor.cs
    ├── InventoryBalanceGrid.razor
    ├── AdjustInventoryBalanceDialog.razor            # create
    ├── CreateInventoryBalanceDialog.razor            # replace or remove
    └── UpdateInventoryBalanceQuantityDialog.razor    # remove

Myrmex.Tests/
└── Wms/Inventory/
    ├── Client/WmsInventoryApiClientTests.cs
    ├── Domain/
    │   ├── InventoryBalanceTests.cs
    │   └── InventoryTransactionTests.cs              # create
    ├── Endpoints/InventoryBalanceEndpointTests.cs
    ├── Features/InventoryAdjustments/
    │   └── AdjustInventoryBalanceHandlerTests.cs     # create
    ├── Persistence/InventoryBalancePersistenceTests.cs
    └── Testing/InventoryBalanceTestData.cs
```

**Structure Decision**: Add the adjustment slice beside existing inventory-balance features instead of creating a new module. Remove direct create/update mutation files because the approved specification forbids parallel stock-mutation mechanisms. Keep read/list/get balance behavior and server-driven list structure intact.

## Architectural Design Notes

- **Domain concepts first**: `InventoryBalance` remains the current SKU/location snapshot and gains rowversion. `InventoryTransaction` is the aggregate root for immutable inventory operations. `InventoryLedgerEntry` is an immutable child entity that stores SKU, storage location, quantity delta, balance before, and balance after. For MVP, every material transaction is type `Adjustment` and has exactly one entry.
- **Aggregate boundaries**: `InventoryTransaction` owns its `InventoryLedgerEntry` collection and enforces ledger-entry invariants. `InventoryBalance` remains a separate current-state aggregate/snapshot. `AdjustInventoryBalance` orchestrates both aggregates in one command and one save.
- **Shared contract boundary**: Add `AdjustInventoryBalanceRequest` in `Myrmex.Shared.Wms.Inventory`. Update `InventoryBalanceDetails` to include `BalanceVersion` as a Base64 string. Remove `CreateInventoryBalanceRequest` and `UpdateInventoryBalanceQuantityRequest` from public stock-mutation usage.
- **Internal request boundary**: Add internal `AdjustInventoryBalance.Command` and handler under `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments`. The command accepts nullable IDs, counted quantity, reason, and expected version from the public request, but remains internal to the WMS module.
- **Backend-owned projection**: Update `InventoryBalanceQueryableExtensions.ProjectDetails()` to convert `InventoryBalance.RowVersion` to Base64 in the backend projection. Shared DTOs do not contain EF expressions or domain types.
- **Server-driven list behavior**: Keep existing Inventory Balance list filters, count-before-paging, deterministic sorting, `Skip`/`Take`, and `ListResult<T>` behavior. The only list contract change is adding `BalanceVersion` to each item.
- **Client/grid behavior**: Keep the existing server-driven MudDataGrid flow. Existing row action becomes "Adjust" and opens an adjustment dialog seeded with the loaded row and `BalanceVersion`. Initial-count workflow uses the same adjustment API with `ExpectedBalanceVersion = null`.
- **Cancellation and errors**: Preserve cancellation propagation through WebApp client, endpoint, dispatcher, and EF Core. Write/action API client methods continue to return `ApiResult<T>`. `InventoryBalance.ConcurrencyConflict` maps to HTTP 409 ProblemDetails and is shown as a refresh-and-review message.
- **Risk-based testing**: Domain tests cover ledger invariants and immutable lifecycle. Handler/persistence tests cover adjustment matrices and no partial persistence. Persistence tests cover rowversion mapping and indexes. Endpoint/client tests cover route, JSON, Base64 transport, and representative ProblemDetails mapping. UI uses manual smoke validation.
- **Existing pattern precedence**: Follow current `InventoryBalanceEndpoints`, `WmsInventoryApiClient`, server-driven list, `ServiceResult`, EF configuration, and WebApp dialog patterns. Do not introduce generic repository, mediator, transaction abstraction, or framework changes.

## Required Design Details

### Domain Model and Aggregate Boundaries

- `InventoryTransaction` is an aggregate root with:
  - `TransactionType = Adjustment` for MVP.
  - `Reason` trimmed, required, max length 500.
  - `OccurredAtUtc` and existing creation timestamp semantics.
  - Read-only entries collection.
  - Factory for material adjustments that creates exactly one `InventoryLedgerEntry`.
- `InventoryLedgerEntry` is immutable after creation:
  - Stores `StockKeepingUnitId`, `StorageLocationId`, `QuantityDelta`, `BalanceBefore`, `BalanceAfter`.
  - Enforces `BalanceAfter = BalanceBefore + QuantityDelta`.
  - Has no public mutation methods.
- `InventoryBalance` remains the current snapshot:
  - Identity is still `StockKeepingUnitId + StorageLocationId`.
  - Add `byte[] RowVersion`.
  - Existing material adjustment changes quantity and touches timestamp.
  - Existing no-op returns success without changing quantity, timestamp, rowversion, or ledger.
  - Missing-zero initialization creates the row with quantity `0` but creates no ledger transaction or entry.

### EF Mappings, Relationships, Indexes, and Migration Shape

- `InventoryBalanceConfiguration`:
  - Add required rowversion property mapped with provider-supported SQL Server rowversion configuration.
  - Keep unique index on `(StockKeepingUnitId, StorageLocationId)`.
  - Keep quantity precision `decimal(18,4)`.
- `InventoryTransactionConfiguration`:
  - Table: `inventory_transactions`.
  - Primary key name added to `WmsDatabaseNames`.
  - `TransactionType` persisted as bounded string, max length 32, required.
  - `Reason` max length 500, required.
  - `OccurredAtUtc` required.
  - `CreatedAtUtc` required; `UpdatedAtUtc` nullable if inherited.
  - Index on `OccurredAtUtc` for future occurrence-time queries.
- `InventoryLedgerEntryConfiguration`:
  - Table: `inventory_ledger_entries`.
  - Primary key name added to `WmsDatabaseNames`.
  - Required `InventoryTransactionId`, `StockKeepingUnitId`, `StorageLocationId`.
  - Decimal precision `18,4` for `QuantityDelta`, `BalanceBefore`, `BalanceAfter`.
  - Relationship `InventoryTransaction` 1-to-many `InventoryLedgerEntry`; child rows required.
  - Restrict delete behavior to SKU and storage-location references.
  - Indexes on `InventoryTransactionId`, `StockKeepingUnitId`, `StorageLocationId`.
- Migration shape:
  - Add `row_version` rowversion column to `inventory_balances`.
  - Create `inventory_transactions` table.
  - Create `inventory_ledger_entries` table.
  - Add foreign keys and indexes listed above.
  - Update `WmsDbContextModelSnapshot`.
  - Migration is created during implementation only, not during planning.

### Concurrency Flow

Existing balance:
1. Load balance by `StockKeepingUnitId + StorageLocationId` with rowversion.
2. Require non-null `ExpectedBalanceVersion`.
3. Decode Base64; invalid value returns validation failure.
4. Explicitly compare expected bytes with current rowversion.
5. If mismatch, return `409 InventoryBalance.ConcurrencyConflict`.
6. If counted quantity equals current quantity, return current details without changing balance or ledger.
7. If material change, create transaction/entry and update balance.
8. Save once. Translate `DbUpdateConcurrencyException` to `409 InventoryBalance.ConcurrencyConflict`.

Missing balance:
1. No balance exists for `StockKeepingUnitId + StorageLocationId`.
2. Require `ExpectedBalanceVersion = null`; non-null returns `409 InventoryBalance.ConcurrencyConflict`.
3. Apply full current create eligibility rules from `CreateInventoryBalance`: active SKU, active base UoM, active storage location, active type, active status, plus existence checks.
4. If counted quantity is `0`, create zero `InventoryBalance`, create no transaction or entry, save once, return details.
5. If counted quantity is positive, create `InventoryBalance` from zero, create transaction/entry with before `0`, delta equal to counted quantity, save once, return details.
6. Translate concurrent duplicate insert on the SKU/location unique index to `409 InventoryBalance.ConcurrencyConflict`.

### Duplicate-Insert Exception Translation Strategy for SQL Server

- Keep the existing SQL Server detection pattern using `SqlException.Number is 2601 or 2627` and matching the named unique index.
- For the adjustment handler, translate `DbUpdateException` caused by `UX_wms_inventory_balances_stock_keeping_unit_id_storage_location_id` into `InventoryBalance.ConcurrencyConflict` because the duplicate insert means another request created the missing balance after expected-absence validation.
- Do not change generic duplicate handling for other create/reference-data flows. Existing direct duplicate create behavior is removed from stock mutation. If shared mapper changes are needed, add an adjustment-specific helper or overload so non-adjustment duplicate conflicts are not accidentally reclassified.

### Atomicity Strategy

- Use one EF Core `SaveChangesAsync` for balance and ledger persistence because all changes belong to the same `WmsDbContext`.
- No explicit database transaction is planned.
- Add an explicit transaction only if implementation discovers more than one database save is required or another concrete repository constraint requires it.
- Do not include domain-event dispatch as part of the database atomicity guarantee; the feature's atomicity requirement concerns persisted balance and ledger rows.

### Public Contracts and Endpoint

- Add `AdjustInventoryBalanceRequest(Guid StockKeepingUnitId, Guid StorageLocationId, decimal CountedQuantity, string Reason, string? ExpectedBalanceVersion)`.
- Update `InventoryBalanceDetails` to include `string BalanceVersion`.
- Endpoint: `POST /api/wms/inventory/adjustments`.
- Successful response: `InventoryBalanceDetails`.
- Validation failures: HTTP 400 ProblemDetails through existing conventions.
- Stale-state conflicts: HTTP 409 ProblemDetails with `code = InventoryBalance.ConcurrencyConflict`.
- No update/delete endpoints for transactions or ledger entries.

### Removal and Migration of Old API Client and UI Flows

- Remove direct backend mutation routes:
  - `POST /api/wms/inventory/balances`
  - `PUT /api/wms/inventory/balances/{inventoryBalanceId}/quantity`
- Remove corresponding shared request contracts:
  - `CreateInventoryBalanceRequest`
  - `UpdateInventoryBalanceQuantityRequest`
- Remove corresponding WebApp API-client methods:
  - `TryCreateInventoryBalanceAsync`
  - `TryUpdateInventoryBalanceQuantityAsync`
- Replace UI flows:
  - Existing row action opens adjustment dialog with `ExpectedBalanceVersion = InventoryBalanceDetails.BalanceVersion`.
  - Initial-count/create workflow uses the adjustment dialog or a dedicated initial-count dialog that submits `ExpectedBalanceVersion = null`.
- Remove or replace obsolete dialog/component files:
  - `UpdateInventoryBalanceQuantityDialog.razor` removed.
  - `CreateInventoryBalanceDialog.razor` removed or converted to initial-count adjustment, but no direct create request remains.

### UI Behavior

- Existing balance adjustment:
  - Open from grid row.
  - Show read-only SKU, warehouse, storage location, base UoM, and current quantity.
  - Editable fields: counted quantity and reason.
  - Submit row `BalanceVersion`.
  - On success: close dialog, show success, reload current grid state.
  - On concurrency conflict: show refresh-and-review message and keep user in recoverable state.
- Missing balance initialization:
  - Open from create/initial-count action.
  - Select SKU, warehouse, and storage location with existing lookup behavior.
  - Show base UoM context.
  - Editable fields: counted quantity and reason.
  - Submit `ExpectedBalanceVersion = null`.
  - Counted quantity `0` succeeds and creates a zero row with no ledger.
  - On success: close dialog, show success, reload current grid state.

### Error and Result Mapping

- Add a capability-specific concurrency error shape with code `InventoryBalance.ConcurrencyConflict`.
- Explicit mismatch and expected-existence mismatch return conflict before save.
- Invalid Base64 expected version returns validation error.
- `DbUpdateConcurrencyException` returns conflict.
- Adjustment-specific duplicate insertion returns conflict.
- Missing-balance eligibility errors reuse current create validation/not-found semantics.
- Existing-balance inactive references are allowed; missing references are rejected.

### Risk-Based Test Strategy

| Regression risk | Lowest owning layer | Planned coverage |
|-----------------|---------------------|------------------|
| Ledger entry stores invalid before/delta/after values | Domain | `InventoryTransaction`/`InventoryLedgerEntry` invariant tests |
| Ledger records become mutable | Domain | Constructor/factory and no public mutation behavior tests |
| Reason normalization/length regresses | Domain or handler | Required/trim/max-500 tests at the layer that owns validation |
| Existing material adjustment writes balance and ledger together | Handler/persistence | One scenario verifies balance, transaction, entry, before/delta/after |
| Existing no-op changes timestamp/version or creates ledger | Handler/persistence | No-op scenario verifies no changed state and no ledger |
| Missing positive adjustment fails to create from zero | Handler/persistence | Missing balance with null expected version creates balance and ledger |
| Missing zero initialization creates ledger or fails to persist row | Handler/persistence | Missing zero scenario creates row, no ledger |
| Existing inactive references block correction | Handler/persistence | Existing balance with inactive referenced records still adjusts |
| Missing inactive references are accepted incorrectly | Handler/persistence | Missing balance uses full current create eligibility rules |
| Explicit stale version is accepted | Handler/persistence | Version mismatch returns `InventoryBalance.ConcurrencyConflict` |
| Expected absence/existence mismatch is accepted | Handler/persistence | Matrix tests for null/non-null expected version |
| EF rowversion or unique index mapping regresses | Persistence | Mapping tests and provider-sensitive tests where infrastructure supports it |
| Duplicate missing-balance insert maps to generic conflict | Handler/persistence or mapper | Focused mapper/SQL Server exception test where practical |
| Route/body/Base64 contract changes | Endpoint/API client | Focused endpoint/client tests for `POST /api/wms/inventory/adjustments` |
| UI still calls obsolete mutation methods | API client/UI review/manual | Client tests plus quickstart scope checks |

Do not reproduce the full adjustment matrix at domain, endpoint, API-client, and UI layers. Protect each risk at the lowest layer that owns it.

## Project Artifact Plan

### Created Documentation

- `specs/071-implement-inventory-adjustment-ledger-mvp/research.md`
- `specs/071-implement-inventory-adjustment-ledger-mvp/data-model.md`
- `specs/071-implement-inventory-adjustment-ledger-mvp/contracts/inventory-adjustment-api-contract.md`
- `specs/071-implement-inventory-adjustment-ledger-mvp/contracts/inventory-adjustment-ui-contract.md`
- `specs/071-implement-inventory-adjustment-ledger-mvp/quickstart.md`

### Expected Production Files to Create During Implementation

- `Myrmex.Shared/Wms/Inventory/AdjustInventoryBalanceRequest.cs`
- `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransactions/InventoryTransaction.cs`
- `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransactions/InventoryTransactionType.cs`
- `Myrmex.Modules.Wms/Inventory/Domain/InventoryTransactions/InventoryLedgerEntry.cs`
- `Myrmex.Modules.Wms/Inventory/Features/InventoryAdjustments/AdjustInventoryBalance.cs`
- `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryAdjustmentEndpoints.cs`
- `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/InventoryTransactionConfiguration.cs`
- `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/InventoryLedgerEntryConfiguration.cs`
- `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/AdjustInventoryBalanceDialog.razor`
- `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/<timestamp>_AddInventoryAdjustmentLedger.cs`
- Focused test files under `Myrmex.Tests/Wms/Inventory/Domain` and `Myrmex.Tests/Wms/Inventory/Features/InventoryAdjustments`

### Expected Production Files to Modify During Implementation

- `Myrmex.Shared/Wms/Inventory/InventoryBalanceDetails.cs`
- `Myrmex.Modules.Wms/Inventory/Domain/InventoryBalances/InventoryBalance.cs`
- `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/InventoryBalanceQueryableExtensions.cs`
- `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryBalanceEndpoints.cs`
- `Myrmex.Modules.Wms/Inventory/Endpoints/InventoryEndpoints.cs`
- `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDbContext.cs`
- `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDbContextSaveExtensions.cs`
- `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDatabaseNames.cs`
- `Myrmex.Modules.Wms/Infrastructure/Persistence/WmsPersistenceExceptionMapper.cs`
- `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/InventoryBalanceConfiguration.cs`
- `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/WmsDbContextModelSnapshot.cs`
- `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs`
- `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/Index.razor.cs`
- `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/InventoryBalanceGrid.razor`
- `Myrmex.Tests/Wms/Inventory/Client/WmsInventoryApiClientTests.cs`
- `Myrmex.Tests/Wms/Inventory/Endpoints/InventoryBalanceEndpointTests.cs`
- `Myrmex.Tests/Wms/Inventory/Persistence/InventoryBalancePersistenceTests.cs`
- `Myrmex.Tests/Wms/Inventory/Testing/InventoryBalanceTestData.cs`

### Expected Production Files to Remove During Implementation

- `Myrmex.Shared/Wms/Inventory/CreateInventoryBalanceRequest.cs`
- `Myrmex.Shared/Wms/Inventory/UpdateInventoryBalanceQuantityRequest.cs`
- `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/CreateInventoryBalance.cs`
- `Myrmex.Modules.Wms/Inventory/Features/InventoryBalances/UpdateInventoryBalanceQuantity.cs`
- `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages/UpdateInventoryBalanceQuantityDialog.razor`
- `Myrmex.Tests/Wms/Inventory/Features/InventoryBalances/UpdateInventoryBalanceQuantityHandlerTests.cs`

`CreateInventoryBalanceDialog.razor` may be removed or converted into the initial-count adjustment dialog. It must not continue to submit direct create requests.

## Phase 0: Research Output

See `research.md`.

## Phase 1: Design Outputs

See `data-model.md`, `contracts/inventory-adjustment-api-contract.md`, `contracts/inventory-adjustment-ui-contract.md`, and `quickstart.md`.

## Post-Design Constitution Check

- **Domain Model First**: PASS. Design artifacts define InventoryBalance, InventoryTransaction, InventoryLedgerEntry, rowversion, expected version semantics, eligibility, no-op, missing-zero initialization, and concurrency outcomes.
- **Modular Monolith Boundaries**: PASS. Public contracts stay in `Myrmex.Shared`; domain/application/persistence/endpoints stay in `Myrmex.Modules.Wms`; UI/client work stays in `Myrmex.WebApp`.
- **Vertical Slice Delivery**: PASS. The design covers endpoint, contract, command handler, domain entities, persistence mappings, API client, UI flows, error mapping, and validation guide.
- **Testing Discipline**: PASS with UI automation exception below. Tests are risk-based and assigned to owning layers; duplicate endpoint/client/UI matrices are intentionally avoided.
- **Simplicity and Observability**: PASS. The design uses existing local patterns, one save, no new generic abstractions, capability-specific conflict code, and existing diagnostics conventions.

No architecture complexity exceptions are requested.

## Complexity Tracking

No architecture complexity exceptions are requested.

### Principle IV Endpoint/UI Test Exceptions

| Deferred automated test | Why deferred | Lower-level automated coverage | Manual validation required | Follow-up issue needed? |
|-------------------------|--------------|--------------------------------|----------------------------|-------------------------|
| Blazor component automation for adjustment dialogs | The current test project has no component-test infrastructure; adding one is disproportionate for this feature and would be a cross-cutting test decision. | Handler/persistence tests cover business outcomes; API-client tests cover request/body/error mapping. | Quickstart requires manual checks for existing adjustment, initial count, no-op, missing-zero, validation, and concurrency message. | No. Revisit only if the project adopts component automation broadly. |

## Unresolved Technical Decisions Before `/speckit-tasks`

- None. The plan selects one save as the default atomicity boundary, capability-specific concurrency errors, SQL Server duplicate-index translation by named unique index, and WebApp manual smoke validation.
