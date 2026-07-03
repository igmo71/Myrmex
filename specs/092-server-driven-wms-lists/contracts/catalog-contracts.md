# Catalog Public Contracts

## Routes Preserved

| Operation | Method and route | Request | Response |
|-----------|------------------|---------|----------|
| List SKUs | `GET /api/wms/catalog/skus` | `ListStockKeepingUnitsRequest` | `ListResult<StockKeepingUnitDetails>` |
| Get SKU | `GET /api/wms/catalog/skus/{stockKeepingUnitId}` | Route ID | `StockKeepingUnitDetails` |
| Create SKU | `POST /api/wms/catalog/skus` | `CreateStockKeepingUnitRequest` | `StockKeepingUnitDetails` |
| Update SKU | `PUT /api/wms/catalog/skus/{stockKeepingUnitId}` | `UpdateStockKeepingUnitDetailsRequest` | `StockKeepingUnitDetails` |
| List UoMs | `GET /api/wms/catalog/uoms` | `ListUnitsOfMeasureRequest` | `ListResult<UnitOfMeasureDetails>` |
| Get UoM | `GET /api/wms/catalog/uoms/{unitOfMeasureId}` | Route ID | `UnitOfMeasureDetails` |
| Create UoM | `POST /api/wms/catalog/uoms` | `CreateUnitOfMeasureRequest` | `UnitOfMeasureDetails` |
| Update UoM | `PUT /api/wms/catalog/uoms/{unitOfMeasureId}` | `UpdateUnitOfMeasureDetailsRequest` | `UnitOfMeasureDetails` |

Existing deactivate/reactivate routes and response types remain unchanged.

## List Requests

Both feature list requests expose:

| Field | Type | Meaning |
|-------|------|---------|
| Skip | nullable integer | Filtered, ordered rows to skip |
| Take | nullable integer | Page size normalized by existing limits |
| SearchText | nullable string | Existing slice-specific contains search |
| SortBy | nullable string | Explicit public sort key |
| SortDescending | nullable boolean | Descending primary sort when true |
| IncludeInactive | nullable boolean | Include inactive records when true |

## Sort Keys

- `StockKeepingUnitSortBy`: Code, Name, CreatedAtUtc, UpdatedAtUtc, IsActive.
- `UnitOfMeasureSortBy`: Code, Name, CreatedAtUtc, UpdatedAtUtc, IsActive.

Values are PascalCase. Backend comparison remains case-insensitive for compatibility. Missing/unknown values fall back to Code then ID.

## Details and Mutation Shapes

- `StockKeepingUnitDetails`: Id, Code, Name, Description, BaseUnitOfMeasureId, IsActive, CreatedAtUtc, UpdatedAtUtc.
- `CreateStockKeepingUnitRequest`: Code, Name, Description, BaseUnitOfMeasureId.
- `UpdateStockKeepingUnitDetailsRequest`: Name, Description, BaseUnitOfMeasureId.
- `UnitOfMeasureDetails`: Id, Code, Name, Symbol, IsActive, CreatedAtUtc, UpdatedAtUtc.
- `CreateUnitOfMeasureRequest`: Code, Name, Symbol.
- `UpdateUnitOfMeasureDetailsRequest`: Name, Symbol.

No ListItem variants are introduced because list/get/write responses intentionally share these details shapes.

## Boundary Rules

- Types live in `Myrmex.Shared.Wms.Catalog` and contain transport data only.
- Endpoints map shared requests to internal commands/queries.
- Domain conversion and EF projections remain internal to Catalog.
- Search remains Code/Name/Description for SKUs and Code/Name/Symbol for UoMs.
- Filtering precedes count; count precedes ordering/paging; ordering precedes paging.

