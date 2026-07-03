# Data Model: Server-Driven WMS Catalog and Topology Lists

## Persistence Impact

No domain entity, value object, relationship, persistence configuration, table, column, index, seed, or migration changes are required. Existing identity, lifecycle, validation, and ownership rules remain authoritative.

## Existing Domain Entities

### Stock Keeping Unit

- **Identity**: Id
- **Search**: Code, Name, Description
- **List/reference fields**: BaseUnitOfMeasureId, IsActive, CreatedAtUtc, UpdatedAtUtc
- **Relationship**: References one Unit of Measure as its base unit.
- **Lifecycle**: Existing create, update, deactivate, and reactivate behavior is unchanged.

### Unit of Measure

- **Identity**: Id
- **Search**: Code, Name, Symbol
- **List fields**: IsActive, CreatedAtUtc, UpdatedAtUtc
- **Lifecycle**: Existing create, update, deactivate, and reactivate behavior is unchanged.

### Warehouse

- **Identity**: Id
- **List/lookup search**: Code, Name, Description
- **List fields**: IsActive, CreatedAtUtc, UpdatedAtUtc
- **Relationships**: Owns Zones and Storage Locations.
- **Lifecycle**: Existing create, update, deactivate, and reactivate behavior is unchanged.

### Zone

- **Identity**: Id
- **Parent**: Required WarehouseId
- **Search**: Code, Name, Description
- **List fields**: IsActive, CreatedAtUtc, UpdatedAtUtc
- **Relationship**: Groups Storage Locations within one Warehouse.
- **Lifecycle**: Existing create, update, deactivate, and reactivate behavior is unchanged.

### Storage Location

- **Identity**: Id
- **References**: Required WarehouseId, ZoneId, StorageLocationTypeId, StorageLocationStatusId
- **Search**: Code, Name, Description
- **List/filter fields**: IsPickable, IsActive, TypeId, StatusId, WarehouseId, ZoneId
- **Audit fields**: CreatedAtUtc, UpdatedAtUtc
- **Validation**: Existing Warehouse existence, Zone existence, and Warehouse/Zone consistency rules remain unchanged.
- **Lifecycle**: Existing create, update, deactivate, and reactivate behavior is unchanged.

### Storage Location Type and Status

- **Identity**: Id
- **Fields**: Code, Name, Description, IsSystem, IsActive, SortOrder
- **Role**: Existing reference values populate filters; selected IDs become server-side list criteria.

## Transport Models

### Feature List Requests

All properties are nullable at the transport boundary so endpoints can preserve omitted-value defaults.

| Request | Common fields | Slice-specific fields |
|---------|---------------|-----------------------|
| ListStockKeepingUnitsRequest | Skip, Take, SearchText, SortBy, SortDescending, IncludeInactive | None |
| ListUnitsOfMeasureRequest | Skip, Take, SearchText, SortBy, SortDescending, IncludeInactive | None |
| ListWarehousesRequest | Skip, Take, SearchText, SortBy, SortDescending, IncludeInactive | None |
| ListZonesRequest | Skip, Take, SearchText, SortBy, SortDescending, IncludeInactive | WarehouseId |
| ListStorageLocationsRequest | Skip, Take, SearchText, SortBy, SortDescending, IncludeInactive | WarehouseId, ZoneId, StorageLocationTypeId, StorageLocationStatusId |

### List Result

- **Items**: Shared details DTOs for the requested page.
- **TotalCount**: Count after filters and before paging.
- **Skip/Take**: Normalized values applied by the handler.
- **Invariant**: Items are deterministically ordered before paging; equal primary values resolve by entity ID.

### Warehouse Lookup

- **Request**: SearchText, Take, SelectableOnly.
- **Item**: Id, Code, Name, IsActive.
- **Bounds**: Default 20, maximum 20.
- **Filter**: SelectableOnly returns active Warehouses.
- **Search**: Existing Code, Name, Description semantics.
- **Order**: Name, Code, ID.

## Query Processing Invariants

1. Normalize paging or lookup bounds.
2. Validate route-owned Warehouse/Zone context with existing rules.
3. Apply all active filters.
4. Calculate TotalCount for lists.
5. Apply deterministic supported sorting and ID tie resolution.
6. Apply Skip/Take for lists or bounded Take for lookup.
7. Project domain data to shared DTOs in the owning backend feature.
8. Materialize with cancellation.

## WebApp State

Grid state remains WebApp-only. Each page grid request contains Skip, Take, SortBy, and SortDescending; page state adds search and filters when mapping to the shared request.

Storage Locations return empty grid data without a list call when WarehouseId is null. Warehouse changes clear Zone selection. Search, inactive, type, status, Warehouse, and Zone changes reset page zero.

