# Quickstart: Validate Inventory Counting MVP

## Prerequisites

- Branch `079-inventory-counting-mvp`.
- Inventory Balance, Adjustment Ledger, Inventory Transfer, and new Inventory Count migrations generated and applied by the developer.
- An authenticated principal exposing `sub`, NameIdentifier, or authenticated Name for write operations.
- Active warehouse, SKU/base UoM, regular location, transit location, and test balances.
- `MYRMEX_WMS_TEST_CONNECTION` points to a dedicated SQL Server database ending in `_test`.

Builds, tests, application startup, database updates, migration generation/application, Docker, and infrastructure commands are developer-controlled.

## Recommended developer commands

```powershell
dotnet build
dotnet test
```

Migration commands are required because this feature adds count tables, but must be selected and executed by the developer according to repository practice.

## Create and prepare a count

1. Create a count for an active visible warehouse.
2. Confirm Draft status, creator identity, reason, and version.
3. Add an eligible SKU/location with an existing balance.
4. Confirm system quantity and non-empty expected balance version.
5. Add an eligible pair with no balance and confirm system quantity zero and expected version absent.
6. Confirm adding lines does not leave Draft.
7. Remove a Pending line and confirm it disappears without inventory effects.

## Eligibility and duplicate protection

Confirm rejection for:

- missing/inactive warehouse or SKU;
- missing/inactive location/type/status;
- location in another warehouse;
- internal/external transit location;
- duplicate current SKU/location pair;
- stale count version.

## Record physical quantities

1. Enter a non-negative quantity.
2. Confirm variance equals counted minus system.
3. Confirm counter identity/time.
4. Confirm the first count entry moves Draft to InProgress.
5. Edit an unapplied Counted line and confirm the original system snapshot remains unchanged.
6. Confirm Counted lines cannot be removed.

## Apply zero variance

1. Enter counted quantity equal to system quantity.
2. Apply with the current line version.
3. Confirm Applied status and applier/time.
4. Confirm no balance version/quantity change, no transaction, and no ledger entry.
5. Repeat for an expected-missing zero line and confirm no zero balance is created.

## Apply non-zero variance

1. Apply a positive or negative variance against an unchanged existing balance.
2. Confirm final balance equals counted quantity.
3. Confirm exactly one Adjustment transaction and one ledger entry.
4. Confirm delta equals variance and before/after match the snapshot/count.
5. Confirm line references the transaction and all effects committed together.
6. Apply a positive count to an expected-missing pair and confirm one balance is created.

## Conflict and supersession

1. Change the balance after line snapshot, then apply.
2. Confirm 409, line status Conflict, and no balance/transaction/ledger effect from the count.
3. Confirm the Conflict line is immutable.
4. Supersede it.
5. Confirm the original becomes Superseded and remains visible.
6. Confirm one fresh Pending current line exists with a new snapshot and replacement link.
7. Confirm concurrent replacement attempts produce one replacement and one conflict.

## Completion and cancellation

1. Attempt completion with no lines or unresolved current lines; confirm 409.
2. Apply every current line, complete, and confirm completer/time and read-only state.
3. Create a separate count, apply one line, then cancel.
4. Confirm canceller/time and read-only state.
5. Confirm prior Applied adjustment remains effective after cancellation.

## List and details

1. Confirm list filters by warehouse/status/date.
2. Confirm supported sorting and deterministic paging.
3. Confirm progress totals use current lines only.
4. Confirm details include Superseded lines and actor/timestamp audit.
5. Deactivate a referenced SKU/location and confirm historical labels remain visible.
6. Confirm normal list/detail requests meet the two-second target under representative load.

## Authentication behavior

1. Execute each write with an authenticated stable actor and confirm the actor is persisted from server claims.
2. Execute a write without an authenticated actor and confirm 401.
3. Confirm request JSON cannot override actor identity.

## UI smoke validation

1. Open Inventory Counts from navigation.
2. Create a count and verify list/detail refresh.
3. Add/remove Pending lines.
4. Enter/edit counts and observe variance/status.
5. Apply zero and non-zero lines and open the transaction link.
6. Trigger a conflict, supersede, and continue with the replacement.
7. Complete and cancel counts and verify final read-only behavior.
8. Confirm visible labels/tooltips and non-color-only status/result meaning.

## Completion evidence

- Domain, SQL Server handler/persistence, endpoint, and API-client tests pass.
- Manual UI and authenticated-principal scenarios pass.
- One count migration is reviewed and applied by the developer.
- No inventory freeze, reservation, approval, scanner/mobile, batch/serial/LPN, external integration, or unrelated workflow is introduced.

## Phase 8 static review record

Static review performed on 2026-06-25. No build, test, application startup,
database update, migration application, Docker, infrastructure, or UI/API smoke
command was executed during this review.

### Diagnostics review

- Count write handlers emit structured action and outcome diagnostics.
- Applicable diagnostics include actor, count, line, warehouse, SKU, and
  storage-location identifiers.
- Apply diagnostics distinguish zero variance, adjustment success, and
  conflict outcomes. They include conflict reason and adjustment transaction
  identifiers where applicable.

### Security boundary review

- Public write requests contain business values and expected rowversions only.
- No public Inventory Count request accepts an actor identifier.
- Every write endpoint obtains the actor from `HttpContext.GetActorId()` and
  returns unauthorized before dispatch when no stable authenticated identity is
  available.
- No production authentication provider or count-specific authorization model
  is introduced by this feature.

### Persistence and scope review

- Successful non-zero apply tracks the count line, balance, adjustment
  transaction, and ledger event effects before the single
  `SaveChangesAsync` call in `SaveSuccessfulApplyAsync`.
- Snapshot mismatch is detected before inventory mutation. The conflict path
  marks and saves only the count/line Conflict outcome and returns a conflict
  result.
- The current-line uniqueness index covers count, SKU, and location and is
  filtered to current lines. The generated migration and model snapshot contain
  the same filtered index.
- Count and line actor identities are stored by domain operations and projected
  into list/detail audit views.
- Source review found no inventory freeze, reservation, approval,
  scanner/mobile, count-wave/task generation, batch/serial/LPN, or external
  integration workflow added by Inventory Counting.

### Developer-controlled validation still required

Record the exact command output and result before marking the corresponding
tasks complete:

```powershell
$env:MYRMEX_WMS_TEST_CONNECTION = "<dedicated database ending in _test>"
dotnet build Myrmex.slnx
dotnet test Myrmex.Tests\Myrmex.Tests.csproj
```

The developer must also review/apply the generated Inventory Count migration
using the repository-approved EF workflow, then execute every authenticated
API/UI scenario in this quickstart and record the observed results.
