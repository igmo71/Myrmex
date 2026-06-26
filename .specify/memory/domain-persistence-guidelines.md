Document Inventory Counting MVP implementation lessons as general Myrmex development guidelines.

Scope:

* Do not change application behavior.
* Do not change domain logic, handlers, API contracts, UI, migrations, tests, database schema, authentication, authorization, or infrastructure.
* Do not run build, tests, migrations, database update, app startup, Docker, or infrastructure commands.
* Documentation-only change.

Add a concise guideline section to the most appropriate persistent project documentation file, preferably `AGENTS.md` if no dedicated architecture/development-guidelines document exists.

Document these rules:

1. Aggregate child identity

* When an aggregate root creates a child entity that has a required parent FK, the child factory/constructor must receive and set the aggregate root Id immediately.
* Do not rely on EF relationship fixup to populate required parent identity for domain-created child entities.

2. Domain creation vs EF tracking

* Domain methods may create child entities, but EF tracking is an application/persistence concern.
* If a handler receives a newly created child entity from a domain method, the handler must explicitly add it to the relevant DbSet so EF persists it as INSERT.

3. Query/persistence test setup

* Tests that bypass application handlers and directly call domain methods are responsible for modelling persistence setup explicitly.
* If such a test creates child entities through aggregate methods, it must explicitly add those new children to the DbContext.

4. SQL ordering and Guid ordering

* Do not assert SQL Server `uniqueidentifier` ordering by using .NET `Guid.OrderBy(...)`.
* For deterministic list queries, use SQL-side tie-breakers such as `ThenBy(x => x.Id)` / `ThenByDescending(x => x.Id)`, but tests should verify primary ordering and stability without assuming .NET and SQL Server Guid comparison order are identical.

5. Test design

* Avoid combining unrelated assertions in one test.
* Prefer separate tests for sorting, invalid filters, persistence setup, and state transitions.

Also add a short note that these rules were reinforced during issue #79 Inventory Counting MVP.

Report:

* documentation file changed;
* exact section added;
* no code changes;
* recommended developer-controlled validation, if any.
