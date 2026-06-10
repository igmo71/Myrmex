# Contract: Catalog/SKU Base UoM API

This contract defines the expected external behavior for the Catalog/SKU Base UoM MVP. It updates existing SKU routes and payloads; it does not add a new route group.

## API Route Group

Base route: `/api/wms/catalog`

Tags: `Wms Catalog`

SKU route prefix: `/api/wms/catalog/skus`

## Payloads

### StockKeepingUnitDetails

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "code": "ITEM-001",
  "name": "Widget",
  "description": "Sellable widget",
  "baseUnitOfMeasureId": "11111111-1111-1111-1111-111111111111",
  "isActive": true,
  "createdAtUtc": "2026-06-10T00:00:00+00:00",
  "updatedAtUtc": null
}
```

On create, `updatedAtUtc` remains `null`. It is set only after existing successful SKU update, deactivate, or reactivate behavior.

### CreateStockKeepingUnitRequest

```json
{
  "code": "ITEM-001",
  "name": "Widget",
  "description": "Sellable widget",
  "baseUnitOfMeasureId": "11111111-1111-1111-1111-111111111111"
}
```

### UpdateStockKeepingUnitDetailsRequest

```json
{
  "name": "Widget",
  "description": "Sellable widget",
  "baseUnitOfMeasureId": "22222222-2222-2222-2222-222222222222"
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

Each list item must include `baseUnitOfMeasureId`.

## Endpoints

### Create SKU

`POST /api/wms/catalog/skus`

**Request**: `CreateStockKeepingUnitRequest`

**Success**: returns `StockKeepingUnitDetails` for the created active SKU.

**Behavior**:

- Requires existing SKU code, name, and description behavior.
- Requires `baseUnitOfMeasureId`.
- Requires the referenced UoM to exist.
- Requires the referenced UoM to be active at assignment time.
- Returns `baseUnitOfMeasureId` in the created SKU details.

**Failure behavior**:

- Missing or empty `baseUnitOfMeasureId` returns validation ProblemDetails with `baseUnitOfMeasureId` field details.
- Nonexistent UoM returns missing-UoM feedback with code `UnitOfMeasure.NotFound` or an equivalent existing missing-UoM error.
- Inactive UoM returns inactive-UoM feedback with code `UnitOfMeasure.Inactive` or an equivalent feature-specific assignment error.
- Existing SKU code/name/description validation and duplicate-code failures remain unchanged.

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

- Existing SKU list behavior remains unchanged.
- Each returned SKU includes `baseUnitOfMeasureId`.
- Default list excludes inactive SKUs.
- `includeInactive=true` includes inactive SKUs.

### Get SKU By Id

`GET /api/wms/catalog/skus/{stockKeepingUnitId:guid}`

**Success**: returns `StockKeepingUnitDetails` for active or inactive SKUs, including `baseUnitOfMeasureId`.

**Failure behavior**:

- Missing SKU returns existing not-found ProblemDetails with code `StockKeepingUnit.NotFound`.

### Update SKU Details

`PUT /api/wms/catalog/skus/{stockKeepingUnitId:guid}`

**Request**: `UpdateStockKeepingUnitDetailsRequest`

**Success**: returns updated `StockKeepingUnitDetails`.

**Behavior**:

- Existing SKU code is not accepted in the update payload and is not changed.
- Existing SKU name and description validation remains unchanged.
- Requires `baseUnitOfMeasureId`.
- Allows changing `baseUnitOfMeasureId` to another existing active UoM.
- Returns the current `baseUnitOfMeasureId` in updated SKU details.

**Failure behavior**:

- Missing SKU returns existing not-found ProblemDetails with code `StockKeepingUnit.NotFound`.
- Missing or empty `baseUnitOfMeasureId` returns validation ProblemDetails with `baseUnitOfMeasureId` field details.
- Nonexistent UoM returns missing-UoM feedback with code `UnitOfMeasure.NotFound` or an equivalent existing missing-UoM error.
- Inactive UoM returns inactive-UoM feedback with code `UnitOfMeasure.Inactive` or an equivalent feature-specific assignment error.
- Existing name/description validation failures remain unchanged.

### Deactivate SKU

`POST /api/wms/catalog/skus/{stockKeepingUnitId:guid}/deactivate`

**Behavior**:

- Existing SKU lifecycle behavior remains unchanged.
- Returned SKU details include the retained `baseUnitOfMeasureId`.

### Reactivate SKU

`POST /api/wms/catalog/skus/{stockKeepingUnitId:guid}/reactivate`

**Behavior**:

- Existing SKU lifecycle behavior remains unchanged.
- Returned SKU details include the retained `baseUnitOfMeasureId`.

## Web API Client Contract

Client path: `Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs`

Expected updated records:

- `StockKeepingUnitDetails` includes `Guid BaseUnitOfMeasureId`.
- `CreateStockKeepingUnitRequest` includes `Guid BaseUnitOfMeasureId`.
- `UpdateStockKeepingUnitDetailsRequest` includes `Guid BaseUnitOfMeasureId`.

Expected existing methods remain:

- `ListStockKeepingUnitsAsync(ListRequest request, CancellationToken cancellationToken = default)`
- `GetStockKeepingUnitByIdAsync(Guid stockKeepingUnitId, CancellationToken cancellationToken = default)`
- `TryCreateStockKeepingUnitAsync(CreateStockKeepingUnitRequest request, CancellationToken cancellationToken = default)`
- `TryUpdateStockKeepingUnitDetailsAsync(Guid stockKeepingUnitId, UpdateStockKeepingUnitDetailsRequest request, CancellationToken cancellationToken = default)`
- `TryDeactivateStockKeepingUnitAsync(Guid stockKeepingUnitId, CancellationToken cancellationToken = default)`
- `TryReactivateStockKeepingUnitAsync(Guid stockKeepingUnitId, CancellationToken cancellationToken = default)`

Read/load methods throw the existing API exception shape on failed responses. Write/action methods return the existing API result shape on failed responses.

## Out of Scope Contract

No endpoint, payload, client method, or contract may expose:

- Alternative UoM assignments.
- UoM conversion factors.
- Packaging levels.
- Inventory quantities.
- Receiving, LPN, picking, or shipping state.
- Seed or demo data management.
- Embedded UoM details on SKU responses.
- New UI pages, navigation, forms, grids, dialogs, or component behavior.
- External integration state or messages.
