# Implementation Plan: Import External Receiving Orders

**Branch**: `121-import-external-receiving-orders-manually-as-local-draft-plans` | **Date**: 2026-07-24 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/121-import-receiving-orders/spec.md`

## Summary

Add a WMS-operator-initiated 1C import for a selected inclusive date range. The 1C
adapter reads eligible `Document_ПриходныйОрдерНаТовары` documents and their `Товары`
lines, maps them to source-neutral import data, resolves only already imported local
references, and dispatches one WMS command per document. The WMS command owns durable
external identity, Draft creation/reconciliation, and its own transaction. A unique
external document key plus the stored data version makes a repeated successful import a
no-op; per-document commands isolate failures and return Created, Updated, Skipped, or
Failed outcomes to the existing 1C integration page.

## Technical Context

**Language/Version**: C# on .NET 10

**Primary Dependencies**: Existing ASP.NET Core Minimal APIs, Blazor/MudBlazor, EF Core,
SQL Server, in-process command dispatcher, and the existing 1C OData transport

**Storage**: WMS SQL Server schema changes: external import state on `ReceivingOrder` and
an optional default receiving location on `Warehouse`; migration generation, review, and
application are developer-controlled.

**Verification**: Developer-performed manual acceptance using a configured 1C instance;
see [quickstart.md](quickstart.md). Automated tests are excluded by the constitution.

**Target Platform**: Existing Myrmex API service and WebApp, hosted through the existing
application composition.

**Project Type**: Modular-monolith vertical slices in `Myrmex.Integrations`,
`Myrmex.Modules.Wms`, `Myrmex.Shared`, and `Myrmex.WebApp`.

**Performance/Scale Requirements**: N/A; no targets were supplied.

**Constraints**: WmsOperator authorization; no external side effects; no
`SynchronizationRequest`, worker, scheduled processing, generalized synchronization
framework, receipt/saga, lock, distributed transaction, post-Draft behavior, or
automatic reference repair. Each document is independently applied through WMS domain
and application rules.

## Repository Findings and Selected Patterns

| Concern | Repository finding | Selected use for #121 |
|---|---|---|
| Draft receiving behavior | `ReceivingOrder.Create` and `ReplaceDraft` validate non-empty plans, unique SKU lines, and Draft state. `UpdateReceivingOrderDraft` owns the delete/reassign savepoint path. | Extract or introduce a narrow internal Draft reconciliation service in the Receiving slice and use it from both the existing edit command and the new import command. Do not duplicate aggregate mutation logic. |
| 1C source contract | Research material records the verified source as `Document_ПриходныйОрдерНаТовары`, with `Товары` lines. The current OData transport reads typed collection DTOs with `$select`, `$filter`, `$orderby`, and `$expand` parameters. | Add typed adapter DTOs under `Myrmex.Integrations/OneC/ReceivingOrders`; raw Cyrillic properties remain there. Add a receiving-orders entity-set option and validate it with existing 1C settings. |
| Eligibility and period | Source header provides `Ref_Key`, `DataVersion`, `DeletionMark`, `Number`, `Date`, `Posted`, `Склад_Key`, and `Статус`. The planned source states are `КПоступлению`, `ВРаботе`, and `ТребуетсяОбработка`; `Принят` is excluded. | Query a half-open source-date range `[start, day after end)` and process only posted, non-deleted documents in the three plan states. Reject invalid ranges before contacting 1C. |
| Line mapping | A source line has parent `Ref_Key`, positive `LineNumber`, `Номенклатура_Key`, optional package/other context, and `Количество`. Its stable identity is `<document Ref_Key>:<LineNumber>`. | Use `Количество` as planned quantity. Reject unsupported non-empty `Упаковка_Key`; retain source line identity only in the transient diagnostic/result mapping. Group valid lines by resolved SKU because local ReceivingOrder permits one line per SKU. |
| External identity | Warehouses, SKUs, and UoMs use an owned `ExternalImportState` with unique `ExternalRefKey`, opaque `DataVersion`, and imported timestamp. ReceivingOrder has no equivalent state today. | Reuse that value-object pattern on ReceivingOrder with a unique filtered external-key index. For this 1C-only feature, document `Ref_Key` is the durable local match key; no Integration-owned link is needed. |
| Dependencies | Existing reference imports locate Warehouse and SKU by `ImportState.RefKey`; `ReceivingOrderEligibility` verifies active Warehouse, receiving StorageLocation, active SKU, and active base UoM. StorageLocation has no 1C import pattern. | Resolve Warehouse and SKU only from existing imported records. Resolve UoM transitively through each SKU's active base UoM. Resolve the receiving location from an explicitly configured local `Warehouse.DefaultReceivingLocationId`; no arbitrary location or source premise/zone mapping. |
| UI and responses | The 1C page already starts manual imports, disables the initiating action, shows counters, operation errors, and per-record reasons. | Add a date-range import action and a receiving-order result model that uses Created/Updated/Skipped/Failed (rather than the reference import's Unchanged naming), then render it beside the existing import results. |
| API and authorization | `/api/integrations/1c` is restricted to `WmsOperator`; endpoints are thin and delegate to scoped import operations. | Add `POST /api/integrations/1c/receiving-orders/import` with a date-range request and the existing authorization convention. |
| Transactions and failure | Existing reference imports and Draft updates use owned transactions or savepoints; `UpdateReceivingOrderDraft` maps persistence conflicts. | The integration operation reads documents then dispatches one WMS command per document. Each command begins/uses only its WMS transaction and rolls back its own document on validation or persistence failure. Continue the loop after a document-level failure. No cross-context transaction is introduced. |

## Constitution Check

Governance is taken from the active repository constitution, v2.1.0. The requested v2.0.0
is superseded in the repository; v2.1.0 retains and strengthens the applicable
developer-controlled-operation rules.

- [x] Domain invariants and state transitions remain in domain/application code; state
      mutations are validated and atomic.
- [x] The design stays within the owning module and vertical slice; Integration calls an
      explicit public WMS import command and does not access WMS persistence directly.
- [x] Commands, DTOs, endpoints, and UI responsibilities are explicit and thin.
- [x] Acceptance scenarios remain independently verifiable without automated tests.
- [x] The design is the smallest adequate solution; it reuses the existing OData client,
      import response UI, import-state value object, and Draft reconciliation path.
- [x] Security, configuration, persistence impact, health checks, and diagnostics are
      addressed where applicable. Existing 1C health/configuration and structured logging
      are extended; no new health-check boundary is required.
- [x] Build, migration, commit, and pull request operations appear only as
      developer-controlled handoff notes.

## Design

### Import flow

1. The WebApp sends an inclusive start/end date to the authorized integration endpoint.
2. The endpoint calls a scoped `IReceivingOrderOneCImport` operation. It validates the
   dates and uses `IReceivingOrderOneCSource` to request the configured source entity
   with a half-open `Date` filter, deterministic order by `Date,Ref_Key`, and the
   required `Товары` projection.
3. The adapter maps each valid raw 1C document to a source-neutral import item. It keeps
   `Ref_Key` and `DataVersion` opaque, normalizes `Number`, validates source line numbers
   and quantities, and makes the document identity available for results.
4. For every document, the import operation dispatches a public
   `ImportExternalReceivingOrder.Command` in WMS. Transport-wide errors produce the
   operation error; document failures produce a Failed result and processing continues.
5. The WMS command loads imported Warehouse and SKU records by external keys, obtains the
   Warehouse default receiving location, and runs existing receiving eligibility checks.
   Missing, inactive, or invalid dependencies return a document-level failure; this
   feature never synchronizes or repairs them.
6. The command finds a ReceivingOrder by its unique external `Ref_Key`. A matching
   `DataVersion` returns Skipped. A missing match creates a Draft order; a matching Draft
   reconciles it using the shared Draft reconciliation service; a matching non-Draft
   returns Skipped. The command records the external data version only after the aggregate
   change commits.
7. The import operation converts each command result to the operator response and logs
   aggregate counts and document-level failure context.

### Reconciliation and idempotency

- An external document maps to exactly one `ReceivingOrder.ImportState.RefKey`; the
  unique filtered index is the durable duplicate guard.
- Imported document number is the initial local number. If it conflicts with an unrelated
  local order, the document fails with a precise reason; no generated naming scheme is
  introduced.
- The local plan is one line per resolved SKU. The command aggregates the source lines'
  `Количество` values with checked decimal arithmetic before aggregate validation.
- On a Draft match, retained SKU lines retain their current local line IDs; missing SKUs
  are removed and new SKUs receive new local IDs through the existing replacement rules.
- A same-version document is Skipped without mutating header, lines, timestamps, or
  import state. A newer version that maps to the same aggregate values may be recorded as
  Updated because it advances the imported version; no duplicate lines are created.
- WMS owns the entire document write in one transaction. A failed mutation clears/rolls
  back its tracked state before the next document command is dispatched.

### Affected projects and slices

- `Myrmex.Integrations/OneC/ReceivingOrders/`: source interface, typed document/line DTOs,
  period query, mapping, scoped manual import operation, and DI registration.
- `Myrmex.Integrations/OneC/Configuration/OneCOptions.cs`: configured receiving document
  entity set and validation.
- `Myrmex.Integrations/OneC/Endpoints/OneCEndpoints.cs`: authorized manual import endpoint.
- `Myrmex.Modules.Wms/Receiving/Domain/ReceivingOrders/`: controlled external-import
  state methods on the aggregate.
- `Myrmex.Modules.Wms/Receiving/Features/ReceivingOrders/`: public inbound import command,
  dependency lookup, outcome mapping, and shared Draft reconciliation service.
- `Myrmex.Modules.Wms/Topology/Domain/Warehouses/` and warehouse feature contracts:
  default receiving-location configuration and validation.
- `Myrmex.Modules.Wms/Infrastructure/Persistence/Configurations/`: owned import-state and
  default-location mapping/indexes; database-name constants and persistence error mapping.
- `Myrmex.Shared/Integrations/OneC/`: request and response DTOs for receiving-order import.
- `Myrmex.WebApp/Integrations/OneC/` and `Components/Pages/Integrations/OneC/`: API client,
  date range form, busy state, counters, and per-document results.

## Supporting Artifacts

- [data-model.md](data-model.md): created because `ReceivingOrder` and `Warehouse` gain
  material persistent state.
- [contracts/receiving-order-import.md](contracts/receiving-order-import.md): created
  because the feature adds an HTTP request/response and a public Integration-to-WMS command
  boundary.
- [quickstart.md](quickstart.md): created as a reusable concise developer manual
  verification guide.
- `research.md`: intentionally omitted. Current code plus the allowed archived issue
  research resolved the material choices; repository findings are recorded above.

## Project Structure

### Documentation (this feature)

```text
specs/121-import-receiving-orders/
├── spec.md
├── plan.md
├── data-model.md
├── contracts/
│   └── receiving-order-import.md
└── quickstart.md
```

### Source Code (repository root)

```text
Myrmex.Integrations/OneC/ReceivingOrders/
Myrmex.Integrations/OneC/Configuration/OneCOptions.cs
Myrmex.Integrations/OneC/Endpoints/OneCEndpoints.cs
Myrmex.Modules.Wms/Receiving/
Myrmex.Modules.Wms/Topology/
Myrmex.Modules.Wms/Infrastructure/Persistence/
Myrmex.Shared/Integrations/OneC/
Myrmex.WebApp/Integrations/OneC/
Myrmex.WebApp/Components/Pages/Integrations/OneC/
```

**Structure Decision**: Retain the existing ownership boundary: 1C-specific transport and
mapping stay in Integrations; receiving rules and WMS persistence stay in WMS; only
serialization-safe requests/results are added to Shared; WebApp remains a thin client.

## Persistence & Migration Handoff

The existing ReceivingOrder table needs an owned external import state containing nullable
`ExternalRefKey`, `ExternalDataVersion`, and `ImportedAtUtc`, with a filtered unique index
on the key. This matches the existing imported reference model and is sufficient for
durable 1C document matching and same-version idempotency.

Warehouse needs an optional `DefaultReceivingLocationId` foreign key. Its value must point
to a local selectable Receiving storage location belonging to that Warehouse; validation
occurs when configuring it and again during import. Neither `Помещение_Key` nor
`ЗонаПриемки_Key` is persisted or mapped to a local location in this feature.

The developer must generate, review, and apply the resulting EF Core migration. The plan
does not generate or modify migration files.

## Developer Actions

- Build the affected projects or solution after implementation.
- Generate, review, and apply the EF Core migration for the WMS schema changes.
- Configure the receiving-order 1C entity set and a valid source connection using secure
  existing configuration practices; do not commit credentials.
- Ensure each Warehouse used for import has an active, selectable local Receiving location
  configured as its default receiving location.
- Perform the manual acceptance scenarios in [quickstart.md](quickstart.md).
- Create the Git commit and pull request when the implementation and developer review are
  complete.

## Complexity Tracking

No Constitution Check exceptions require justification.
