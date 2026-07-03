# Phase 2 Data and Ordering Model

Phase 2 changes no entity, DTO, persistence mapping, index, relationship, state transition, schema, or migration.

## Existing Entities in Scope

- **Zone**: existing topology entity; `Id` is the unique secondary ordering value.
- **StorageLocation**: existing topology entity; `Id` is the unique secondary ordering value.
- **StockKeepingUnit**: existing catalog entity; `Id` is the unique secondary ordering value.
- **UnitOfMeasure**: existing catalog entity; `Id` is the unique secondary ordering value.

No fields or validation rules are added or changed.

## Ordering Invariant

For every supported and fallback list order:

1. Preserve the existing primary expression and requested primary direction.
2. Resolve equal primary values by database-ascending entity `Id`.
3. Apply `Skip` and `Take` only after both ordering levels.

Conceptually, each list order is `(existing primary value, Id ascending)`. This creates a total order for unchanged data without redefining primary sorting. SQL Server orders `uniqueidentifier` values differently from .NET's default `Guid` comparer, so persistence-test expectations use `System.Data.SqlTypes.SqlGuid`, following the existing Warehouse list test.

## Test Scenario Model

- Seed at least three valid entities with the same non-unique primary value, preferably Name, and distinct IDs.
- Request ascending and descending primary sorting; because primary values are equal, returned IDs must be ascending in both cases.
- Request adjacent bounded pages and verify their concatenated IDs equal the complete expected ascending-ID sequence without duplication or omission.
- Keep entity-specific required relationships valid: Zones belong to a Warehouse; Storage Locations belong to valid Warehouse/Zone and required topology reference data.

Concurrent inserts, updates, and deletes are outside the invariant; Phase 2 does not add snapshot consistency.
