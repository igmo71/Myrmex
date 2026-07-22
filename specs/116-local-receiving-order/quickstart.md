# Quickstart: Validate Local Receiving Order MVP

This guide is for implementation and acceptance after the planned changes exist. It does not authorize running commands during planning. It uses the existing migration, build, Aspire, WMS WebApp, API, inventory list/ledger, and operational logging paths and adds no testing or benchmark infrastructure.

## 1. Review the Design Boundaries

Before implementation, use:

- [data-model.md](data-model.md) for ownership, fields, relationships, state, atomicity, and concurrency;
- [Receiving Orders API contract](contracts/receiving-orders-api-contract.md) for routes, shapes, list, errors, and versions;
- [Receiving Orders WebApp contract](contracts/receiving-orders-webapp-contract.md) for pages, eligible lookup, line state, conflict UX, and the 300-line scenario.

Confirm the implementation does not add an external integration, numbering service, generic workflow/posting/location-capability framework, separate ReceivingLocation entity, or new test project.

## 2. Generate and Review the Migration

After the domain and EF configurations are implemented, generate one normal WMS migration from the repository root:

```powershell
dotnet ef migrations add AddReceivingOrders --project Myrmex.Modules.Wms --startup-project Myrmex.ApiService --context WmsDbContext --output-dir Infrastructure/Persistence/Migrations
```

Migration generation and application are user-owned actions. Before applying, verify the generated migration and `WmsDbContextModelSnapshot.cs` contain:

- `wms.receiving_orders` and `wms.receiving_order_lines`;
- restrictive Warehouse, ReceivingLocation, InventoryTransaction, owner, and SKU foreign keys;
- order rowversion and no line rowversion;
- `decimal(18,4)` planned/received quantities;
- unique Number, unique order/SKU, and unique filtered transaction-reference indexes;
- Warehouse, ReceivingLocation, Status, CreatedAtUtc, and SKU indexes;
- one active system StorageLocationType with code `RECEIVING`;
- no ReceivingLocation table, soft-delete columns, cancellation state, source-document table, or generalized capability tables.

## 3. Build and Start the Existing Application

Run only after implementation and migration review:

```powershell
dotnet build Myrmex.slnx -nologo
dotnet ef database update --project Myrmex.Modules.Wms --startup-project Myrmex.ApiService --context WmsDbContext
dotnet run --project Myrmex.AppHost
```

There is no tracked test project or test command in the current solution. Do not restore/create one, add browser/component tests, or add performance/load tooling for this feature.

## 4. Prepare Local Acceptance Data

Through existing topology/catalog/demo workflows, ensure the environment contains:

- an active Warehouse;
- an active StorageLocation in that Warehouse whose active type is `RECEIVING` and whose status/eligibility allow inventory;
- an active non-Receiving location in the same Warehouse for negative validation;
- active SKUs with active base units;
- at least one SKU/location with an existing balance and one without a balance;
- for the large-plan scenario, 300 distinct active SKUs.

The planned demo-data extension should provide at least one `RECEIVING` location without changing legacy DOCK identities. The 300-SKU dataset may be prepared through existing catalog/import/demo facilities; it does not add Receiving import behavior.

## 5. Validate the End-to-End WebApp Workflow

Open `/wms/receiving-orders` as an authorized WMS operator.

1. Navigate to `/wms/receiving-orders/new`.
2. Enter a unique Number, select the active Warehouse, and verify the location lookup offers only eligible `RECEIVING` locations in that Warehouse.
3. Add multiple distinct SKU lines through the focused search dialog and enter positive planned quantities.
4. Save; verify the order is Draft, every received quantity is zero, and Inventory Balances/Ledger show no effect.
5. Start; verify status InProgress, Started timestamp present, header/plan locked, and inventory unchanged.
6. Receive each line in one or more positive increments. Verify remaining quantities, over-receipt rejection, and no inventory effect.
7. Attempt completion while one line remains short; verify conflict and no inventory change.
8. Fully receive every line and complete.
9. Verify status Completed, completion timestamp and InventoryTransaction reference present, all actions read-only, each balance increased exactly once, and the linked transaction is type Receiving with one positive entry per order line.

This complete Draft-to-Completed workflow must be achievable through the WebApp without lower-level system/data intervention.

## 6. Validate Draft Reconciliation and Line Identity

1. Create a Draft with at least three lines and record returned LineIds.
2. Open Edit, change one retained line's SKU or planned quantity, retain another unchanged, omit a third, and add a new null-ID line.
3. Save the complete plan.
4. Verify retained lines kept their IDs, the omitted line is gone, and only the new line has a new ID.
5. Verify duplicate SKUs and an empty plan are rejected without partial change.
6. Through an authenticated API client, submit a line ID belonging to a different Receiving Order and verify 400/409 behavior with the target Draft unchanged.
7. Start the order and verify further plan/header updates are rejected.

## 7. Validate Draft-Only Deletion and Number Reuse

1. Create a Draft, capture its OrderVersion, and delete it from the list/details UI.
2. Verify the order and all lines are gone and no inventory/ledger effect exists.
3. Create another Draft with the same normalized Number; verify it succeeds.
4. Attempt delete with a stale version; verify conflict and no partial line deletion.
5. Attempt deletion after Start and after completion; verify both are rejected.
6. Confirm no archive, soft-delete, or cancellation state is presented.

## 8. Validate Receiving Location Enforcement

For Create, Update Draft, and Start separately, verify rejection when:

- Warehouse is inactive or missing;
- location is missing/inactive;
- location belongs to another Warehouse;
- location status is inactive;
- location type is inactive;
- location type is not exactly `RECEIVING` (including legacy `DOCK` and `STAGING`);
- existing Inventory Balance eligibility rules reject the location or SKU/base UOM.

Also submit an ineligible location through an authenticated client, bypassing WebApp lookup, and verify the same server rule is enforced.

## 9. Validate Aggregate Concurrency

Use two browser sessions or authenticated clients that load the same OrderVersion.

- Save two different Draft revisions: one succeeds; the other receives 409 and must reload. Retained local edits are not silently overwritten.
- Start versus Draft update: at most one mutation succeeds; the stale operation conflicts.
- Receive two line increments from the same version: at most one succeeds; refresh shows the aggregate-wide current quantities.
- Delete versus another Draft mutation: at most one succeeds; no partial line deletion remains.
- Verify no line version appears in the contract or UI.

The UI must reload/show guidance and require deliberate retry rather than automatically replaying a mutation.

## 10. Validate Completion Idempotency and Atomic Posting

### Repeated completion

1. Complete a fully received order.
2. Submit completion again, including from a client holding an older version.
3. Verify current Completed details are returned and balances, transaction count, and entry count remain unchanged.

### Concurrent completion of one order

1. Load the same fully received InProgress order and version in two authenticated clients.
2. issue Complete concurrently.
3. Verify exactly one inventory posting exists.
4. Verify the losing request observes and returns the current Completed result when the winner completed it; it never posts again.

### Inventory conflict/no partial save

Exercise the existing controlled concurrency-validation approach by racing completion with another change to the same balance or with creation of the same missing SKU/location balance. Verify:

- a non-winning order remains InProgress unless another request completed that same order;
- no partial order completion, partial balance set, orphan transaction, or partial ledger entries exist;
- a true conflict is 409 with refresh guidance;
- the handler performs no automatic posting retry.

Use the order details, Inventory Balances page, Inventory Ledger page, and structured logs to inspect the outcome; do not add failure-injection or benchmark infrastructure.

## 11. Validate List, Details, and Errors

- Search by partial/exact Number.
- Filter by Warehouse and each status.
- Verify supported sorting and paging, deterministic totals, and empty results.
- Open details for Draft, InProgress, and Completed orders and verify timestamps, OrderVersion, lines, planned/received/remaining quantities, status, available actions, and transaction reference.
- Verify 400 for malformed input/version, 404 for absent records, and 409 for lifecycle, uniqueness, over-receipt, incomplete completion, and concurrency failures using existing Problem Details with `code` and `property`.
- Confirm structured logs include action/outcome and relevant order, line, warehouse, location, SKU, quantity, and transaction identifiers without a new logging subsystem.

## 12. Validate the 300-Line Functional Dataset

Using exactly 300 distinct active SKUs:

1. Create and save one 300-line Draft through the WebApp.
2. Reopen it and confirm all 300 lines and IDs.
3. Search/filter the current page line set and confirm clearing the filter restores all lines.
4. Revise retained lines, remove and add lines, save the complete plan, and reopen it.
5. Confirm no filtered-out line was lost and the order was not split.
6. Start, receive all lines, and complete.
7. Verify all quantities, one transaction, 300 positive ledger entries, and no duplicate inventory effect.

Record only functional pass/fail. Do not record response times, throughput, browser memory, concurrent load, or formal usability-study results.

## 13. Confirm Scope Containment

Confirm the implementation added none of the following:

- 1C/external synchronization or identity;
- numbering service or year rules;
- supplier, purchase order, ASN, dock/door/staging, multiple receiving category, or putaway behavior;
- packaging/conversion, lot/serial/expiry, quality/quarantine/damage, shortage/excess/discrepancy, partial completion, correction, or reversal;
- scanner/mobile, printing, or notifications;
- generalized workflow/state-machine, inventory-posting, source-document, idempotency, or location-capability infrastructure;
- soft delete, archive, or cancellation;
- test project, UI test framework, performance benchmark, or load test.
