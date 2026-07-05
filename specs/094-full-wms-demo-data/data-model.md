# Data Model: Full WMS Demo Data Seeding

## Persistence impact

This feature adds no persisted entity and changes no existing persisted shape. It creates and deletes rows through the current `WmsDbContext` model. No EF migration, `HasData` operational seed, demo manifest, or execution-history table is required.

## WmsDemoDataOptions

Non-persisted host configuration bound from `Myrmex:Wms:DemoData`.

### Fields

- `Enabled`: Boolean, default `false`. Both routes are mapped only when true and the environment is not Production.
- `AllowClear`: Boolean, default `false`. Seed is unaffected; clear returns forbidden when false.
- `ClearConfirmation`: nullable string. Clear is unavailable when null/blank and requires an ordinal exact match against the JSON request value.

The configured confirmation value is never returned or logged.

## DemoDataOperationResponse

Public non-persisted success result shared by seed and clear.

### Fields

- `Operation`: `seed` or `clear`.
- `StartedAtUtc`: UTC start timestamp.
- `CompletedAtUtc`: UTC completion timestamp after all stages succeeded and before the transaction is committed.
- `Areas`: ordered collection of `DemoDataAreaSummary`.

A failed or cancelled operation returns ProblemDetails, not this response.

## DemoDataAreaSummary

Public non-persisted per-area accounting record.

### Fields

- `Area`: stable lower-case area key.
- `Created`: records created by seed.
- `Reused`: compatible records/stages reused by seed.
- `Skipped`: optional unsupported definitions deliberately omitted by seed.
- `Deleted`: records deleted by clear.

All counts are non-negative. Seed uses `Deleted=0`; clear uses `Created=Reused=Skipped=0`.

### Area keys

`unitsOfMeasure`, `stockKeepingUnits`, `warehouses`, `zones`, `storageLocations`, `inventoryBalances`, `inventoryTransactions`, `inventoryLedgerEntries`, `inventoryTransfers`, `inventoryTransferLines`, `inventoryTransferMovements`, `inventoryCounts`, `inventoryCountLines`, and `skuBarcodes` where applicable.

## ClearDemoDataRequest

Public non-persisted JSON request.

### Fields

- `Confirmation`: required non-empty string compared exactly to configured `ClearConfirmation`.

## Demo data operation gate

Non-persisted singleton process state with one lease shared by seed and clear.

```text
Idle --Acquire(seed|clear)--> Running
Running --Acquire(seed|clear)--> Conflict (no state change)
Running --Dispose lease--> Idle
```

The gate does not coordinate multiple API-service processes. The database transaction remains the authoritative atomicity boundary.

## Stable identity and reconciliation

| Area | Stable identity | Compatibility check |
|---|---|---|
| Unit of measure | `Code` | normalized name, symbol, active state |
| SKU | `Code` | normalized name, description, base-UoM identity, active state |
| Warehouse | `Code` | normalized name, description, active state |
| Zone | warehouse + `Code` | normalized name, description, active state |
| Storage location | warehouse + `Code` | zone, type, status, name, description, pickable flag, active state |
| Opening adjustment | exact `DEMO-OPEN-*` transaction reason + one expected ledger pair | transaction type, SKU, location, before/after/delta |
| Transfer | `Code` | warehouses, transit location, line SKU/source/destination/requested quantity, expected movement progression |
| Inventory count | exact `DEMO-CNT-*` reason + warehouse | expected current line pairs, snapshots, recorded values, and lifecycle state |

Rules:

- No match creates the missing record or stage.
- Exactly one compatible match is reused.
- Multiple matches, wrong immutable values, wrong parent/reference, or unexpected operational effects are conflicts.
- Any conflict aborts and rolls back the complete seed request.
- Normalized string comparisons follow current domain normalization and database uniqueness rules.
- Existing records linked to 1C by `ExternalRefKey` are compatible only when all demo-owned invariant values also match; seeding never clears or rewrites source identity metadata.

## Demo catalog definition

### Units of measure

| Code | Symbol | Russian name |
|---|---|---|
| `PCS` | `шт` | Штука |
| `PACK` | `упак` | Упаковка |
| `BOX` | `кор` | Коробка |
| `KG` | `кг` | Килограмм |

### Stock keeping units

| Code | Russian name | Base UoM |
|---|---|---|
| `SKU-SCR-GVL-3.9X19` | Саморез ГВЛ 3,9×19 | `PACK` |
| `SKU-SCR-GVL-3.9X25` | Саморез ГВЛ 3,9×25 | `PACK` |
| `SKU-SCR-GVL-3.9X30` | Саморез ГВЛ 3,9×30 | `PACK` |
| `SKU-SCR-UNI-4.0X40` | Шуруп универсальный 4,0×40 | `BOX` |
| `SKU-DWL-6X40` | Дюбель 6×40 | `PACK` |
| `SKU-ANCH-WDG-10X100` | Анкер клиновой 10×100 | `PCS` |
| `SKU-BLT-M8X30` | Болт М8×30 | `PCS` |
| `SKU-NUT-M8` | Гайка М8 | `PCS` |
| `SKU-WSH-M8` | Шайба М8 | `PCS` |
| `SKU-THR-M10X1000` | Шпилька М10×1000 | `PCS` |

Each SKU has a concise Russian description, is active, has no barcode, and has no group/category.

## Demo warehouse topology

### Warehouse

| Code | Russian name |
|---|---|
| `DEMO` | Демонстрационный склад |

### Zones

| Code | Russian name |
|---|---|
| `RCV` | Приёмка |
| `BULK` | Паллетное хранение |
| `PICK` | Отбор |
| `PACK` | Упаковка |
| `SHIP` | Отгрузка |
| `QRT` | Карантин |
| `CART` | Тележки и транзит |

### Existing system reference mapping

| Demo purpose | Existing type code | Default status |
|---|---|---|
| Receiving dock | `DOCK` | `AVAILABLE` |
| Bulk storage | `PALLET_RACK` | `AVAILABLE` |
| Picking face | `SHELF` | `AVAILABLE` |
| Packing station | `STAGING` | `AVAILABLE` |
| Shipping stage | `STAGING` | `AVAILABLE` |
| Quarantine | `FLOOR` | `BLOCKED` |
| Cart/transit | `INTERNAL_TRANSIT` | `AVAILABLE` |

`INVENTORY_CHECK` may be used only for a dedicated non-operational example location. No new type/status record is created, renamed, or deleted.

### Storage locations

| Code | Russian name | Zone | Type | Status | Pickable |
|---|---|---|---|---|---:|
| `RCV-DOCK-01` | Док приёмки 01 | `RCV` | `DOCK` | `AVAILABLE` | No |
| `RCV-DOCK-02` | Док приёмки 02 | `RCV` | `DOCK` | `AVAILABLE` | No |
| `BULK-A-01-01` | Паллетная ячейка A-01-01 | `BULK` | `PALLET_RACK` | `AVAILABLE` | No |
| `BULK-A-01-02` | Паллетная ячейка A-01-02 | `BULK` | `PALLET_RACK` | `AVAILABLE` | No |
| `BULK-B-01-01` | Паллетная ячейка B-01-01 | `BULK` | `PALLET_RACK` | `AVAILABLE` | No |
| `PICK-A-01-01` | Ячейка отбора A-01-01 | `PICK` | `SHELF` | `AVAILABLE` | Yes |
| `PICK-A-01-02` | Ячейка отбора A-01-02 | `PICK` | `SHELF` | `AVAILABLE` | Yes |
| `PICK-B-01-01` | Ячейка отбора B-01-01 | `PICK` | `SHELF` | `AVAILABLE` | Yes |
| `PACK-01` | Упаковочный стол 01 | `PACK` | `STAGING` | `AVAILABLE` | No |
| `PACK-02` | Упаковочный стол 02 | `PACK` | `STAGING` | `AVAILABLE` | No |
| `SHIP-STAGE-01` | Место отгрузки 01 | `SHIP` | `STAGING` | `AVAILABLE` | No |
| `SHIP-STAGE-02` | Место отгрузки 02 | `SHIP` | `STAGING` | `AVAILABLE` | No |
| `QRT-01` | Карантин 01 | `QRT` | `FLOOR` | `BLOCKED` | No |
| `CART-01` | Тележка комплектовщика 01 | `CART` | `INTERNAL_TRANSIT` | `AVAILABLE` | No |
| `CART-02` | Тележка комплектовщика 02 | `CART` | `INTERNAL_TRANSIT` | `AVAILABLE` | No |

## Opening inventory and ledger

Opening stock is created through adjustment behavior. Each non-zero opening balance creates one `Adjustment` transaction and one ledger entry whose reason starts with a unique stable `DEMO-OPEN-*` marker.

| SKU | Location | Opening quantity |
|---|---|---:|
| `SKU-SCR-GVL-3.9X19` | `BULK-A-01-01` | 500 |
| `SKU-SCR-GVL-3.9X19` | `PICK-A-01-01` | 100 |
| `SKU-SCR-GVL-3.9X25` | `BULK-A-01-02` | 400 |
| `SKU-SCR-GVL-3.9X25` | `PICK-A-01-02` | 80 |
| `SKU-SCR-GVL-3.9X30` | `BULK-B-01-01` | 300 |
| `SKU-SCR-GVL-3.9X30` | `PICK-B-01-01` | 40 |
| `SKU-SCR-UNI-4.0X40` | `BULK-B-01-01` | 250 |
| `SKU-DWL-6X40` | `PICK-A-01-01` | 120 |
| `SKU-WSH-M8` | `QRT-01` | 50 |
| `SKU-THR-M10X1000` | `PACK-01` | 12 |

Transfer effects create or update additional destination/cart balances. Final balance count remains within 10–20, and ledger entries remain within 10–20.

## Demo transfers

The existing model stores code but no transfer name/description.

| Code | Transit | State | Lines/effect |
|---|---|---|---|
| `DEMO-TRF-DIRECT-001` | None | `Completed` | Move 20 packs of `SKU-SCR-GVL-3.9X19` from `BULK-A-01-01` to `PICK-A-01-01`. |
| `DEMO-TRF-CART-001` | `CART-01` | `Completed` | Pick and place 15 packs of `SKU-SCR-GVL-3.9X30` from `BULK-B-01-01` to `PICK-B-01-01`. |
| `DEMO-TRF-CART-002` | `CART-01` | `InProgress` | Pick 10 boxes of `SKU-SCR-UNI-4.0X40` from `BULK-B-01-01`; quantity remains at `CART-01`. |
| `DEMO-TRF-DIRECT-002` | None | `Created` | Request 25 packs of `SKU-SCR-GVL-3.9X25` from `BULK-A-01-02` to `PICK-A-01-02`; no movement yet. |

### Current transfer state rules

```text
Create no-transit -> Created
Move full requested quantity -> Completed
Create with transit -> Created
Pick positive quantity -> InProgress
Place all picked quantity -> Completed
```

Every movement creates one `Transfer` inventory transaction, two ledger entries, and one transfer-movement record. Balance deltas and movement quantities must agree.

## Demo inventory counts

Inventory counts have no code field, so the exact reason prefix is their logical demo identity.

### `DEMO-CNT-OPEN-001 — Инвентаризация зоны отбора`

- Warehouse: `DEMO`.
- State: `InProgress`.
- Actor: stable server-side demo actor derived from the requesting actor context for orchestration diagnostics; count audit uses `demo-data-seeder` to remain deterministic.
- Three current Counted lines in picking locations:
  - one counted quantity equal to system quantity (zero variance);
  - one below system quantity (shortage);
  - one above system quantity (surplus).
- At least two SKUs and two locations.
- Lines are not applied, so inventory and ledger remain unchanged.

### `DEMO-CNT-CLOSED-001 — Завершённая инвентаризация паллетной зоны`

- Warehouse: `DEMO`.
- State: `Completed`.
- Two bulk-location lines for different SKUs.
- Counted quantities equal captured system quantities.
- Lines transition `Pending -> Counted -> Applied`; the aggregate then transitions to `Completed`.
- Zero variance creates no balance, transaction, or ledger change.

## Existing persisted relationships affected

- `StockKeepingUnit` requires one `UnitOfMeasure`; delete restricted.
- `Zone` requires one `Warehouse`; delete restricted.
- `StorageLocation` requires warehouse, zone, type, and status; delete restricted.
- `InventoryBalance` uniquely identifies `(SKU, StorageLocation)` and uses rowversion.
- `InventoryTransaction` owns immutable `InventoryLedgerEntry` children.
- `InventoryTransfer` owns lines and movements; movements reference transactions and locations.
- `InventoryCount` owns lines; lines reference SKU, location, optional applied transaction, and optional superseded line.

These relationships determine both seed order and clear order.

## Clear order and preservation

Delete all rows from these sets in order:

1. `InventoryCountLines`
2. `InventoryTransferMovements`
3. `InventoryTransferLines`
4. `InventoryCounts`
5. `InventoryTransfers`
6. `InventoryLedgerEntries`
7. `InventoryTransactions`
8. `InventoryBalances`
9. `SkuBarcodes`
10. `StorageLocations`
11. `Zones`
12. `StockKeepingUnits`
13. `UnitsOfMeasure`
14. `Warehouses`

Preserve:

- all `StorageLocationTypes`;
- all `StorageLocationStatuses`;
- `wms` schema and every table/index/constraint;
- `__EFMigrationsHistory`;
- database identity and configuration.

The clear response includes each delete count, including zero counts. Any failed delete rolls back every prior delete in the request.
