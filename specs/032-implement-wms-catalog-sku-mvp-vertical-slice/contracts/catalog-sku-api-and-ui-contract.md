# Contract: WMS Catalog/SKU API and UI

This contract defines the expected external behavior for the Catalog/SKU MVP. It follows existing WMS write/action result behavior and read/load error behavior.

## API Route Group

Base route: `/api/wms/catalog`

Tags: `Wms Catalog`

## Payloads

### StockKeepingUnitDetails

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "code": "ITEM-001",
  "name": "Widget",
  "description": "Optional description",
  "isActive": true,
  "createdAtUtc": "2026-06-08T00:00:00+00:00",
  "updatedAtUtc": null
}
```

### CreateStockKeepingUnitRequest

```json
{
  "code": "ITEM-001",
  "name": "Widget",
  "description": "Optional description"
}
```

### UpdateStockKeepingUnitDetailsRequest

```json
{
  "name": "Updated widget",
  "description": "Updated optional description"
}
```

### ListResult<StockKeepingUnitDetails>

```json
{
  "items": [],
  "totalCount": 0,
  "skip": 0,
  "take": 20
}
```

## Endpoints

### Create SKU

`POST /api/wms/catalog/skus`

**Request**: `CreateStockKeepingUnitRequest`

**Success**: returns `StockKeepingUnitDetails` for the created active SKU.

**Failure behavior**:

- Missing or invalid code returns validation ProblemDetails with `code` field details.
- Missing or invalid name returns validation ProblemDetails with `name` field details.
- Overlong description returns validation ProblemDetails with `description` field details.
- Duplicate code returns conflict ProblemDetails with code `StockKeepingUnit.CodeAlreadyExists` and field `code`.

### List SKUs

`GET /api/wms/catalog/skus`

**Query parameters**:

- `skip`
- `take`
- `searchText`
- `sortBy`
- `sortDescending`
- `includeInactive`

**Success**: returns `ListResult<StockKeepingUnitDetails>`.

**Behavior**:

- Default list excludes inactive SKUs.
- `includeInactive=true` includes inactive SKUs.
- Search matches code, name, and description.
- Unknown sort fields fall back to code ordering.

### Get SKU By Id

`GET /api/wms/catalog/skus/{stockKeepingUnitId:guid}`

**Success**: returns `StockKeepingUnitDetails` for active or inactive SKUs.

**Failure behavior**:

- Missing SKU returns not-found ProblemDetails with code `StockKeepingUnit.NotFound`.

### Update SKU Details

`PUT /api/wms/catalog/skus/{stockKeepingUnitId:guid}`

**Request**: `UpdateStockKeepingUnitDetailsRequest`

**Success**: returns updated `StockKeepingUnitDetails`.

**Behavior**:

- SKU code is not accepted in the update payload and is not changed.

**Failure behavior**:

- Missing SKU returns not-found ProblemDetails with code `StockKeepingUnit.NotFound`.
- Invalid name or description returns validation ProblemDetails with field details.

### Deactivate SKU

`POST /api/wms/catalog/skus/{stockKeepingUnitId:guid}/deactivate`

**Success**: returns current `StockKeepingUnitDetails` with `isActive=false`.

**Behavior**:

- Repeating the operation for an inactive SKU succeeds and returns current details.

**Failure behavior**:

- Missing SKU returns not-found ProblemDetails with code `StockKeepingUnit.NotFound`.

### Reactivate SKU

`POST /api/wms/catalog/skus/{stockKeepingUnitId:guid}/reactivate`

**Success**: returns current `StockKeepingUnitDetails` with `isActive=true`.

**Behavior**:

- Repeating the operation for an active SKU succeeds and returns current details.

**Failure behavior**:

- Missing SKU returns not-found ProblemDetails with code `StockKeepingUnit.NotFound`.

## Web API Client Contract

Client path: `Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs`

Client support paths:

- `Myrmex.WebApp/Wms/Catalog/ApiResult.cs`
- `Myrmex.WebApp/Wms/Catalog/ApiException.cs`

Expected methods:

- `ListStockKeepingUnitsAsync(ListRequest request, CancellationToken cancellationToken = default)`
- `GetStockKeepingUnitByIdAsync(Guid stockKeepingUnitId, CancellationToken cancellationToken = default)`
- `TryCreateStockKeepingUnitAsync(CreateStockKeepingUnitRequest request, CancellationToken cancellationToken = default)`
- `TryUpdateStockKeepingUnitDetailsAsync(Guid stockKeepingUnitId, UpdateStockKeepingUnitDetailsRequest request, CancellationToken cancellationToken = default)`
- `TryDeactivateStockKeepingUnitAsync(Guid stockKeepingUnitId, CancellationToken cancellationToken = default)`
- `TryReactivateStockKeepingUnitAsync(Guid stockKeepingUnitId, CancellationToken cancellationToken = default)`

Read/load methods throw the existing API exception shape on failed responses. Write/action methods return the existing API result shape on failed responses.

## UI Contract

Route: `/wms/catalog/skus`

Page components:

- `Index.razor`
- `Index.razor.cs`
- `SkuFilters.razor`
- `SkuGrid.razor`
- `SkuEditDialog.razor`

Expected behavior:

- Page shows SKU title, create action, refresh action, search field, include-inactive switch, and SKU grid.
- Grid shows code, name, description, active state, created timestamp, updated timestamp, and actions.
- Create dialog accepts code, name, and description.
- Edit dialog disables or omits code editing and accepts name and description.
- Active SKUs offer deactivate.
- Inactive SKUs offer reactivate.
- API errors show user-visible messages without breaking the page.

## Out of Scope Contract

No endpoint, payload, client method, or UI component may expose:

- Inventory quantity.
- Barcode.
- Unit of measure.
- Packaging.
- Receiving.
- LPN contents.
- Picking.
- Shipping.
- Integration state or messages.
