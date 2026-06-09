# Contract: WMS Catalog/UoM API and UI

This contract defines the expected external behavior for the Catalog/UoM MVP. It follows existing WMS Catalog/SKU write/action result behavior and read/load error behavior.

## API Route Group

Base route: `/api/wms/catalog`

Tags: `Wms Catalog`

## Payloads

### UnitOfMeasureDetails

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "code": "EA",
  "name": "Each",
  "symbol": "ea",
  "isActive": true,
  "createdAtUtc": "2026-06-09T00:00:00+00:00",
  "updatedAtUtc": null
}
```

On create, `updatedAtUtc` is `null`. It is set only after a successful update, deactivate, or reactivate operation.

### CreateUnitOfMeasureRequest

```json
{
  "code": "EA",
  "name": "Each",
  "symbol": "ea"
}
```

### UpdateUnitOfMeasureDetailsRequest

```json
{
  "name": "Each",
  "symbol": "ea"
}
```

### ListResult<UnitOfMeasureDetails>

```json
{
  "items": [],
  "totalCount": 0,
  "skip": 0,
  "take": 20
}
```

## Endpoints

### Create UoM

`POST /api/wms/catalog/uoms`

**Request**: `CreateUnitOfMeasureRequest`

**Success**: returns `UnitOfMeasureDetails` for the created active UoM.

**Behavior**:

- Stores the normalized UoM code directly in `code`.
- Does not expose or persist a separate `normalizedCode` field.
- Returns `updatedAtUtc: null` for a newly created UoM.

**Failure behavior**:

- Missing or invalid code returns validation ProblemDetails with `code` field details.
- Missing or invalid name returns validation ProblemDetails with `name` field details.
- Overlong symbol returns validation ProblemDetails with `symbol` field details. Symbol uses the same maximum length as WMS business codes.
- Duplicate code returns conflict ProblemDetails with code `UnitOfMeasure.CodeAlreadyExists` and field `code`.

### List UoMs

`GET /api/wms/catalog/uoms`

**Query parameters**:

- `skip`
- `take`
- `searchText`
- `sortBy`
- `sortDescending`
- `includeInactive`

**Success**: returns `ListResult<UnitOfMeasureDetails>`.

**Behavior**:

- Default list excludes inactive UoMs.
- `includeInactive=true` includes inactive UoMs.
- Search matches code, name, and symbol.
- Supported `sortBy` values are `code`, `name`, and `isActive`.
- Unknown sort fields fall back to code ordering.
- `createdAtUtc` and `updatedAtUtc` are not supported sort fields for UoM.
- Sorting must not use provider-specific branching or in-memory ordering workarounds.

### Get UoM By Id

`GET /api/wms/catalog/uoms/{unitOfMeasureId:guid}`

**Success**: returns `UnitOfMeasureDetails` for active or inactive UoMs.

**Failure behavior**:

- Missing UoM returns not-found ProblemDetails with code `UnitOfMeasure.NotFound`.

### Update UoM Details

`PUT /api/wms/catalog/uoms/{unitOfMeasureId:guid}`

**Request**: `UpdateUnitOfMeasureDetailsRequest`

**Success**: returns updated `UnitOfMeasureDetails`.

**Behavior**:

- UoM code is not accepted in the update payload and is not changed.
- Symbol remains a display label only and does not define conversion behavior.

**Failure behavior**:

- Missing UoM returns not-found ProblemDetails with code `UnitOfMeasure.NotFound`.
- Invalid name or symbol returns validation ProblemDetails with field details.

### Deactivate UoM

`POST /api/wms/catalog/uoms/{unitOfMeasureId:guid}/deactivate`

**Success**: returns current `UnitOfMeasureDetails` with `isActive=false`.

**Behavior**:

- Repeating the operation for an inactive UoM succeeds and returns current details.

**Failure behavior**:

- Missing UoM returns not-found ProblemDetails with code `UnitOfMeasure.NotFound`.

### Reactivate UoM

`POST /api/wms/catalog/uoms/{unitOfMeasureId:guid}/reactivate`

**Success**: returns current `UnitOfMeasureDetails` with `isActive=true`.

**Behavior**:

- Repeating the operation for an active UoM succeeds and returns current details.

**Failure behavior**:

- Missing UoM returns not-found ProblemDetails with code `UnitOfMeasure.NotFound`.

## Web API Client Contract

Client path: `Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs`

Existing client support paths:

- `Myrmex.WebApp/Wms/Catalog/ApiResult.cs`
- `Myrmex.WebApp/Wms/Catalog/ApiException.cs`

Expected additional methods:

- `ListUnitsOfMeasureAsync(ListRequest request, CancellationToken cancellationToken = default)`
- `GetUnitOfMeasureByIdAsync(Guid unitOfMeasureId, CancellationToken cancellationToken = default)`
- `TryCreateUnitOfMeasureAsync(CreateUnitOfMeasureRequest request, CancellationToken cancellationToken = default)`
- `TryUpdateUnitOfMeasureDetailsAsync(Guid unitOfMeasureId, UpdateUnitOfMeasureDetailsRequest request, CancellationToken cancellationToken = default)`
- `TryDeactivateUnitOfMeasureAsync(Guid unitOfMeasureId, CancellationToken cancellationToken = default)`
- `TryReactivateUnitOfMeasureAsync(Guid unitOfMeasureId, CancellationToken cancellationToken = default)`

Read/load methods throw the existing API exception shape on failed responses. Write/action methods return the existing API result shape on failed responses.

The Catalog client must reuse existing/local Catalog client error/result conventions and must not introduce a new error/result pattern.

## UI Contract

Route: `/wms/catalog/uoms`

Page components:

- `Index.razor`
- `Index.razor.cs`
- `UomFilters.razor`
- `UomGrid.razor`
- `UomEditDialog.razor`

Expected behavior:

- Page shows UoM title, create action, refresh action, search field, include-inactive switch, and UoM grid.
- Grid shows code, name, symbol, active state, created timestamp, updated timestamp, and actions.
- Grid sorting is limited to supported provider-safe list fields.
- Create dialog accepts code, name, and optional symbol.
- Edit dialog disables or omits code editing and accepts name and symbol.
- Active UoMs offer deactivate.
- Inactive UoMs offer reactivate.
- Successful create, update, deactivate, and reactivate actions show the same snackbar/reload behavior as SKU.
- API errors show user-visible messages without breaking the page.

## Out of Scope Contract

No endpoint, payload, client method, or UI component may expose:

- Conversion rules or factors.
- Base or alternative UoM model.
- SKU-to-UoM binding.
- Packaging levels.
- Barcode support.
- Inventory quantities.
- Receiving flows.
- LPN behavior.
- Picking or shipping behavior.
- Integration state or messages.
- Created/updated timestamp sorting for UoM lists.
- New endpoint/UI test framework behavior.
