# Data Model: Manual External Receiving-Order Import

## Ownership

```text
Warehouse ── optional default ──> StorageLocation
                                  (active, selectable, Receiving type)

ReceivingOrder ── owns ──> ExternalImportState
      │                         RefKey, DataVersion, ImportedAtUtc
      └── ReceivingOrderLine *
```

All new persistent data belongs to WMS. Integration owns only the transient 1C DTO and
source-neutral import mapping for the duration of a manual request.

## ReceivingOrder external import state

Reuse the existing `ExternalImportState` value-object pattern used by imported Warehouse,
SKU, and UoM records.

| Field | Type | Rules |
|---|---|---|
| `RefKey` | `Guid?` | The 1C document `Ref_Key`; null for locally created orders; unique when present. |
| `DataVersion` | `byte[]?` | Opaque non-empty 1C `DataVersion`, up to the existing 128-byte limit. |
| `ImportedAtUtc` | `DateTimeOffset?` | Time of the most recent successful create or Draft update. |

Rules:

- A non-null external key maps to exactly one ReceivingOrder through a filtered unique
  database index.
- A matching external key and matching data version is a Skipped, non-mutating result.
- Import state is created/updated only in the same WMS transaction as the corresponding
  Draft aggregate change.
- A local order without import state is never selected by document number as an imported
  document match.

## Warehouse default receiving location

| Field | Type | Rules |
|---|---|---|
| `DefaultReceivingLocationId` | `Guid?` | Optional FK to `StorageLocation`, restrictive delete behavior. |

Rules:

- A warehouse may remain unconfigured for manual operations.
- An imported document requires a configured value.
- The referenced location must belong to the warehouse, be active/selectable, and use the
  Receiving location type.
- Import fails the affected document with a precise configuration reason if this invariant
  is not met.

## Transient source mapping

The following source values are not persisted as a generalized external snapshot in this
limited feature:

- Header: `Ref_Key`, `DataVersion`, `Number`, `Date`, `Posted`, `DeletionMark`,
  `Склад_Key`, and `Статус`.
- Lines: parent `Ref_Key`, `LineNumber`, `Номенклатура_Key`, `Упаковка_Key`, and
  `Количество`.

The stable diagnostic line identity is `<document Ref_Key>:<LineNumber>`. It is used to
identify validation failures. Multiple valid source lines resolving to one local SKU are
aggregated because a ReceivingOrder stores one line per SKU.

`КоличествоУпаковок`, characteristics, purposes, serials, source order/sender, premise,
and receiving zone do not need persistence for Draft plan import and are not introduced as
new data structures.
