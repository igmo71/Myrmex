# Topology Public Contracts

## Routes Preserved or Added

| Operation | Method and route | Request | Response |
|-----------|------------------|---------|----------|
| List Warehouses | `GET /api/wms/topology/warehouses` | `ListWarehousesRequest` | `ListResult<WarehouseDetails>` |
| Lookup Warehouses | `GET /api/wms/topology/warehouses/lookup` | `LookupWarehousesRequest` | `IReadOnlyList<WarehouseLookupItem>` |
| List Zones | `GET /api/wms/topology/warehouses/{warehouseId}/zones` | `ListZonesRequest` | `ListResult<ZoneDetails>` |
| List locations by Warehouse | `GET /api/wms/topology/warehouses/{warehouseId}/locations` | `ListStorageLocationsRequest` | `ListResult<StorageLocationDetails>` |
| List locations by Zone | `GET /api/wms/topology/zones/{zoneId}/locations` | `ListStorageLocationsRequest` | `ListResult<StorageLocationDetails>` |

All existing get/create/update/deactivate/reactivate and type/status routes remain unchanged. Warehouse lookup is the only new route.

## List Requests

`ListWarehousesRequest` exposes nullable Skip, Take, SearchText, SortBy, SortDescending, and IncludeInactive.

`ListZonesRequest` adds nullable WarehouseId; the nested route binds that value.

`ListStorageLocationsRequest` exposes:

| Field | Type | Meaning |
|-------|------|---------|
| WarehouseId | nullable GUID | Warehouse route/filter context |
| ZoneId | nullable GUID | Zone route/selected filter |
| StorageLocationTypeId | nullable GUID | Server-side type filter |
| StorageLocationStatusId | nullable GUID | Server-side status filter |
| SearchText | nullable string | Existing Code/Name/Description search |
| IncludeInactive | nullable boolean | Include inactive locations when true |
| Skip | nullable integer | Rows skipped after filtering/order |
| Take | nullable integer | Requested page size |
| SortBy | nullable string | Explicit sort key |
| SortDescending | nullable boolean | Descending primary sort when true |

## Warehouse Lookup

`LookupWarehousesRequest` exposes nullable SearchText, Take, and SelectableOnly. SelectableOnly defaults true.

`WarehouseLookupItem` contains Id, Code, Name, and IsActive. Results are bounded to 20, search Code/Name/Description, and order by Name, Code, then ID.

## Sort Keys

- `WarehouseSortBy`: Code (compatibility), Name, CreatedAtUtc, UpdatedAtUtc, IsActive.
- `ZoneSortBy`: Code, Name, CreatedAtUtc, UpdatedAtUtc, IsActive.
- `StorageLocationSortBy`: Code, Name, IsPickable, CreatedAtUtc, UpdatedAtUtc, IsActive.

Values are PascalCase with case-insensitive backend compatibility. Warehouse defaults to Name then ID; Zone and Storage Location default to Code then ID.

## Details and Mutation Shapes

- `WarehouseDetails`: Id, Code, Name, Description, IsActive, CreatedAtUtc, UpdatedAtUtc.
- `CreateWarehouseRequest`: Code, Name, Description.
- `UpdateWarehouseDetailsRequest`: Name, Description.
- `ZoneDetails`: Id, WarehouseId, Code, Name, Description, IsActive, CreatedAtUtc, UpdatedAtUtc.
- `CreateZoneRequest`: Code, Name, Description.
- `UpdateZoneDetailsRequest`: Name, Description.
- `StorageLocationDetails`: Id, WarehouseId, ZoneId, StorageLocationTypeId, StorageLocationStatusId, Code, Name, Description, IsPickable, IsActive, CreatedAtUtc, UpdatedAtUtc.
- `CreateStorageLocationRequest`: StorageLocationTypeId, StorageLocationStatusId, Code, Name, Description, IsPickable.
- `UpdateStorageLocationDetailsRequest`: Name, Description, IsPickable.
- `StorageLocationTypeDetails` and `StorageLocationStatusDetails`: Id, Code, Name, Description, IsSystem, IsActive, SortOrder.

## Boundary Rules

- Shared types contain transport data only; queries and projections remain in Topology.
- Storage Location filters apply before count and paging.
- Route-bound WarehouseId/ZoneId retain current not-found and mismatch validation.
- Main-page Storage Location requests require a selected Warehouse.
- Warehouse lookup and paged Warehouse list remain separate contracts.
- The Storage Location Zone selector remains deferred in this feature.

