# Domain Persistence Guidelines

These guidelines capture domain and persistence lessons reinforced during issue #79, **Inventory Counting MVP**.

They apply to Myrmex domain entities, EF Core persistence code, and tests that create or persist aggregate graphs.

## Aggregate child identity

When an aggregate root creates a child entity that has a required parent foreign key, the child factory or constructor must receive and set the aggregate root ID immediately.

Do not rely on EF Core relationship fixup to populate required parent identity for domain-created child entities.

Preferred pattern:

```csharp
DomainValidationResult result = InventoryCountLine.Create(
    inventoryCountId: Id,
    stockKeepingUnitId,
    storageLocationId,
    systemQuantity,
    expectedBalanceVersion,
    out InventoryCountLine? line);
```

The child entity should be valid from the domain perspective immediately after creation:

```csharp
private InventoryCountLine(
    Guid inventoryCountId,
    Guid stockKeepingUnitId,
    Guid storageLocationId,
    decimal systemQuantity,
    byte[]? expectedBalanceVersion)
{
    InventoryCountId = inventoryCountId;
    StockKeepingUnitId = stockKeepingUnitId;
    StorageLocationId = storageLocationId;
    SystemQuantity = systemQuantity;
    ExpectedBalanceVersion = expectedBalanceVersion is null
        ? null
        : [.. expectedBalanceVersion];
}
```

## Domain creation vs EF Core tracking

Domain methods may create child entities, but EF Core tracking is an application and persistence concern.

If an application handler receives a newly created child entity from a domain method, the handler must explicitly add it to the relevant `DbSet` so EF Core persists it as an `INSERT`.

Preferred pattern:

```csharp
DomainValidationResult result = count.AddLine(
    stockKeepingUnitId,
    storageLocationId,
    systemQuantity,
    expectedBalanceVersion,
    out InventoryCountLine? line);

if (!result.IsValid)
{
    return ServiceResult<InventoryCountDetails>.Invalid(result.Errors);
}

dbContext.InventoryCountLines.Add(line!);
```

This keeps responsibilities clear:

* the domain model creates and validates the entity;
* the application/persistence layer decides how the entity is tracked and saved.

Do not depend on EF Core inferring `Added` state from private backing collections, required foreign keys, or client-generated IDs.

## Query and persistence test setup

Tests that bypass application handlers and call domain methods directly are responsible for modelling persistence setup explicitly.

If such a test creates child entities through aggregate methods, it must explicitly add those new child entities to the `DbContext`.

Example:

```csharp
Assert.True(count.AddLine(
    stockKeepingUnitId,
    storageLocationId,
    systemQuantity,
    expectedBalanceVersion,
    out InventoryCountLine? line).IsValid);

dbContext.InventoryCountLines.Add(line!);
```

Application-handler tests should prefer the production flow and let handlers perform persistence tracking.

Query and persistence tests may build fixtures directly, but they must make EF Core entity state explicit.

## SQL ordering and Guid ordering

Do not assert SQL Server `uniqueidentifier` ordering by using .NET `Guid.OrderBy(...)` or `Guid.OrderByDescending(...)`.

SQL Server and .NET may compare GUID values differently. A query may be deterministic in SQL while a test that calculates expected order with .NET `Guid` comparison still fails.

For deterministic list queries, use SQL-side tie-breakers:

```csharp
query.OrderBy(x => x.CreatedAtUtc)
     .ThenBy(x => x.Id);
```

or:

```csharp
query.OrderByDescending(x => x.CreatedAtUtc)
     .ThenByDescending(x => x.Id);
```

Tests should verify:

* primary ordering using distinct primary sort values;
* stable ordering across repeated executions when primary sort values are equal;
* paging consistency.

Tests should not assume that .NET `Guid` ordering is equivalent to SQL Server `uniqueidentifier` ordering.

## Test design

Avoid combining unrelated assertions in one test.

Prefer small tests with one clear reason to fail.

Good separation examples:

* sorting behavior;
* invalid filters;
* paging behavior;
* persistence setup;
* state transitions;
* concurrency behavior;
* domain validation;
* endpoint binding;
* API client transport.

Focused tests make failures easier to diagnose and reduce false conclusions about the failing layer.

## Practical checklist

When adding a domain method that creates a child entity:

1. Verify that the child factory receives the aggregate root ID.
2. Verify that the child constructor sets the required parent foreign key.
3. Verify that the application handler explicitly adds the new child entity to the relevant `DbSet`.
4. Add a domain test proving the child entity receives the parent ID.
5. Add a handler or persistence test proving the child entity is inserted correctly.
6. If query tests build fixtures directly through domain methods, explicitly add created child entities to the `DbContext`.
