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

Before any manual import source processing begins, configuration validation continues to require the integration to be enabled and the existing base URL, credentials, all three reference entity-set settings, batch size, and timeout to be valid. Splitting source readers does not weaken this integration-wide validation.

| Condition | Status | Code |
|-----------|--------|------|
| Integration disabled or configuration invalid | 400 | `OneC.ConfigurationInvalid` |
| Same reference import already in progress | 409 | `OneCImport.AlreadyInProgress` |
| Platform authentication required | 401 | Existing platform authentication response |
| Authenticated principal is not authorized | 403 | Existing platform authorization response |

The `400` and `409` cases retain their existing safe Problem Details. Platform `401/403` behavior remains unchanged.

## Active Import Failure Contract

After configuration succeeds and source processing begins, the following failures remain incomplete `200 OK OneCImportResponse` values with the existing safe `OperationError`. They do not become import-route `502/504` Problem Details.

| Failure during active import | `OperationError.Reason` | Result |
|------------------------------|-------------------------|--------|
| 1C authentication rejection | `AuthenticationFailed` | Incomplete `200 OK OneCImportResponse` |
| Entity set unavailable | `EntitySetUnavailable` | Incomplete `200 OK OneCImportResponse` |
| Malformed source response | `MalformedResponse` | Incomplete `200 OK OneCImportResponse` |
| Source unavailable | `SourceUnavailable` | Incomplete `200 OK OneCImportResponse` |
| Source timeout | `Timeout` | Incomplete `200 OK OneCImportResponse` |
| Unexpected application or batch failure | `BatchCommitFailed` | Incomplete `200 OK OneCImportResponse` |

The response uses the existing safe operation messages, exposes no credentials or source payload, and preserves committed SKU counts/errors when applicable.

## Manual Import Transition

```text
Requested
  -> Gate unavailable -> 409 Problem Details
  -> Configuration invalid -> 400 Problem Details
  -> Import started
       -> Complete 200 OneCImportResponse
       -> Incomplete 200 OneCImportResponse
       -> Incomplete 200 Cancelled response
       -> Lease released
```

Platform authentication/authorization (`401/403`) occurs before invoking the operation and does not start an import.

## Connection-Test Distinction

The existing connection-test endpoint remains separate from manual imports. It continues to return Problem Details for transport failures, including its existing `502` authentication/entity-set/malformed/source-unavailable responses and `504` timeout response. This behavior must not be applied to an import that has already started.

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

The endpoint may retain a small uniform adapter for `OneCImportResponse` and pre-start Problem Details. That adapter must not select a reference type or accept a callback that owns source read, mapping, dispatch, or classification. Each endpoint invokes its matching explicit import contract and returns active-import failures through the response produced by that contract.

## Unchanged Clients

`Myrmex.WebApp/Integrations/OneC/OneCIntegrationApiClient.cs`, the operator page, localization resources, and shared public response types remain unchanged.
