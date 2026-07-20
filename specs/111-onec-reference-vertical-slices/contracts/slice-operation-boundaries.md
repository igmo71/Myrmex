# Contract: Explicit 1C Reference Slice Boundaries

## Purpose

Define the internal ownership and dependency direction used during implementation. Names are planning decisions for internal types; no type in this document is a public transport contract.

## Warehouse Slice

```text
IWarehouseOneCSource
  ReadAllAsync(CancellationToken)
  ReadCurrentAsync(Guid, CancellationToken)
  ProbeAsync(CancellationToken)

IWarehouseOneCImport
  ImportAsync(CancellationToken) -> OneCImportResponse

IWarehouseOneCSynchronizer
  SynchronizeAsync(Guid, CancellationToken) -> ReferenceSynchronizationResult
```

`WarehouseOneCImport` and `WarehouseOneCSynchronizer` both depend on `IWarehouseOneCSource`, the singleton gate, time/logging, and the command dispatcher. Only the slice chooses `ImportWarehouses.Command` and Warehouse mapping/folder rules.

`WarehouseReferenceSynchronizationHandler` depends only on `IWarehouseOneCSynchronizer` plus common result mapping.

## Unit of Measure Slice

```text
IUnitOfMeasureOneCSource
  ReadAllAsync(CancellationToken)
  ReadCurrentAsync(Guid, CancellationToken)
  ProbeAsync(CancellationToken)

IUnitOfMeasureOneCImport
  ImportAsync(CancellationToken) -> OneCImportResponse

IUnitOfMeasureOneCSynchronizer
  SynchronizeAsync(Guid, CancellationToken) -> ReferenceSynchronizationResult
```

Only the slice chooses `ImportUnitsOfMeasure.Command`, full-name/symbol fallbacks, and the explicit absence of folder semantics.

`IUnitOfMeasureOneCSynchronizer` is the sole cross-slice operation contract and may be consumed by the SKU synchronizer. It is not a generic reference dispatcher.

## Stock Keeping Unit Slice

```text
IStockKeepingUnitOneCSource
  ReadPagesAsync(CancellationToken)
  ReadCurrentAsync(Guid, CancellationToken)
  ProbeAsync(CancellationToken)

IStockKeepingUnitOneCImport
  ImportAsync(CancellationToken) -> OneCImportResponse

IStockKeepingUnitOneCSynchronizer
  SynchronizeAsync(Guid, CancellationToken) -> ReferenceSynchronizationResult
```

Only the slice chooses `ImportStockKeepingUnits.Command`, paging/batching, partial-result accounting, folder rules, base-UoM mapping, repair eligibility, and retry limit.

`StockKeepingUnitOneCSynchronizer` depends directly on `IUnitOfMeasureOneCSynchronizer` and may invoke it once per SKU synchronization attempt.

## Common Dependencies

### OData Transport

`IOneCODataTransport` provides the existing integration-wide configuration validation, authenticated collection execution, and uniform transport failures. It may accept an entity-set name and explicit query parameters from a slice and deserialize the requested internal source-record type. Configuration validation retains the current requirement for enabled integration, valid base URL/credentials, all three configured entity sets, valid batch size, and valid timeout. The transport must not contain reference-type selection, entity-set lookup by enum, reference projections, folder rules, paging rules, mapping, or WMS command knowledge.

### Import Gate

One singleton `OneCImportGate` retains the Warehouse/UoM/SKU lease identities and existing `Acquire`/`TryAcquire` semantics. A slice must not register or instantiate a private gate.

### Import Response Factory

`OneCImportResponseFactory` constructs complete/incomplete response objects, maps the existing transport error reason/message, converts existing WMS batch errors, and enforces the 50-error return cap. It accepts counts/results only and does not invoke source reads or application operations.

### Synchronization Result and Mapper

`ReferenceSynchronizationResult`, outcome/reason values, and safe diagnostic rendering remain common. `ReferenceSynchronizationHandlerResultMapper.Map(result)` is pure translation from an already-produced internal result to the existing Feature #104 handler result. It does not parse a type, choose a slice, or invoke a callback.

## Composition Root

`OneCIntegrationModule` registers:

- one common typed OData transport;
- one singleton import gate;
- three source contracts;
- three manual-import contracts;
- three synchronize-one contracts;
- the same three `ISynchronizationHandler` implementations;
- existing Feature #104 resolver/processor/worker and intake dependencies;
- the integration-wide connection test.

It removes registrations for `IOneCImportService`, `IOneCReferenceSynchronizationService`, and the all-reference typed OData client.

## Endpoint Dependencies

`OneCEndpoints` binds each existing import route directly to its matching import contract. The existing connection route binds to the integration-wide connection test. Notification endpoints remain unchanged because they own durable intake, not reference application.

## Forbidden Dependencies

- No slice depends on another slice except SKU's direct dependency on the UoM synchronize-one contract.
- Common code does not depend on a concrete reference slice.
- Source code does not depend on WMS domain/persistence types.
- Integration code does not query WMS persistence directly.
- No enum/string switch chooses a reference workflow.
- No generic helper receives the source-read, mapping, WMS-dispatch, and result-classification callbacks for a reference flow.
- No compatibility facade preserves the removed composite services.
