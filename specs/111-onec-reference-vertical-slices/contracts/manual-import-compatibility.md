# Contract: 1C Manual Import Compatibility

## Purpose

This contract freezes the existing operator-facing behavior while endpoint dependencies move from one all-reference service to three explicit import operations.

## Routes and Authorization

All routes remain under `/api/integrations/1c`, require the existing WMS operator policy, and retain their endpoint names and summaries.

| Reference | Method and route | Internal target after refactor |
|-----------|------------------|--------------------------------|
| Warehouse | `POST /warehouses/import` | `IWarehouseOneCImport.ImportAsync` |
| Unit of Measure | `POST /uoms/import` | `IUnitOfMeasureOneCImport.ImportAsync` |
| Stock Keeping Unit | `POST /skus/import` | `IStockKeepingUnitOneCImport.ImportAsync` |

The route table, authorization behavior, and WebApp client URLs do not change.

## Success and Incomplete Response

An import that starts returns `200 OK` with the existing `OneCImportResponse`, whether the operation is complete or incomplete. The response retains:

- `ReferenceType`
- `IsComplete`
- `Processed`
- `Created`
- `Updated`
- `Unchanged`
- `Skipped`
- `Failed`
- `StartedAtUtc`
- `CompletedAtUtc`
- `OperationError`
- `Errors`

No field is renamed, removed, added, or reinterpreted.

## Pre-Start Error Contract

Errors raised before the operation produces an import response retain existing Problem Details behavior:

Before any manual import source read, configuration validation continues to require the integration to be enabled and the existing base URL, credentials, all three reference entity-set settings, batch size, and timeout to be valid. Splitting source readers does not weaken this integration-wide validation.

| Condition | Status | Code |
|-----------|--------|------|
| Integration disabled or configuration invalid | 400 | `OneC.ConfigurationInvalid` |
| Same reference import already in progress | 409 | `OneCImport.AlreadyInProgress` |
| 1C authentication rejected | 502 | `OneC.AuthenticationFailed` |
| Entity set unavailable | 502 | `OneC.EntitySetUnavailable` |
| Malformed source response | 502 | `OneC.MalformedResponse` |
| Source unavailable | 502 | `OneC.SourceUnavailable` |
| Source timeout | 504 | `OneC.Timeout` |

Authentication/authorization failures retain existing platform behavior.

## Accounting Invariants

- Counts remain mutually consistent with existing WMS batch results and folder skips.
- At most 50 record errors are returned; total failed/skipped counts are not reduced by the cap.
- Warehouse and SKU folders remain skipped with the existing reason and safe message.
- UoM has no folder semantics.
- Warehouse and UoM full imports apply one complete collection command when eligible records exist.
- SKU retains configured stable paging and one WMS command per returned page.
- SKU results include only committed pages and committed folder counts; later failure/cancellation preserves those committed results.
- A page containing only SKU folders is counted without dispatching an empty command.
- Same-version records remain `Unchanged` and do not become updated, skipped, or failed.

## Cancellation and Coordination

- Caller cancellation after import start returns the existing incomplete response with reason `Cancelled`.
- SKU cancellation retains prior committed counts and errors.
- Manual acquisition is non-waiting; same-type overlap returns the existing 409 contract.
- Different reference types remain independently executable.
- Each manual lease covers configuration validation, all source reads, all command dispatches, response construction, and operation logging.

## Internal Endpoint Composition

The endpoint may retain a small uniform adapter for `OneCImportResponse` and Problem Details. That adapter must not select a reference type or accept a callback that owns source read, mapping, dispatch, or classification. Each endpoint invokes its matching explicit import contract.

## Unchanged Clients

`Myrmex.WebApp/Integrations/OneC/OneCIntegrationApiClient.cs`, the operator page, localization resources, and shared public response types remain unchanged.
