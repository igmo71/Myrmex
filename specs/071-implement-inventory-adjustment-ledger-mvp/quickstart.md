# Quickstart: Inventory Adjustment Ledger MVP

This guide documents validation for the implementation phase. Codex must not run build, test, app startup, migration, database update, or infrastructure commands automatically.

## Artifact Checks

Confirm implementation aligns with:

- `specs/071-implement-inventory-adjustment-ledger-mvp/spec.md`
- `specs/071-implement-inventory-adjustment-ledger-mvp/plan.md`
- `specs/071-implement-inventory-adjustment-ledger-mvp/data-model.md`
- `specs/071-implement-inventory-adjustment-ledger-mvp/contracts/inventory-adjustment-api-contract.md`
- `specs/071-implement-inventory-adjustment-ledger-mvp/contracts/inventory-adjustment-ui-contract.md`

## Scope Checks

Verify the final diff does not add:

- Inventory Ledger history UI.
- Inventory Transfer.
- InventoryAccount.
- LPN or handling units.
- Zero-row deletion.
- Event sourcing.
- Generic repository, mediator, transaction abstraction, or speculative framework.
- Direct create or direct quantity-update stock mutation paths.

## Recommended Developer-Controlled Commands

Run these manually after implementation when ready:

```powershell
dotnet build Myrmex.slnx -nologo -v:minimal
dotnet test Myrmex.Tests\Myrmex.Tests.csproj -nologo -v:minimal
dotnet run --project Myrmex.AppHost\Myrmex.AppHost.csproj
```

For targeted tests, use Visual Studio Test Explorer or the test-selection syntax confirmed for the installed Microsoft.Testing.Platform/xUnit version. Do not assume VSTest-style `--filter FullyQualifiedName...` reliably runs only the intended subset in this repository configuration.

Migration commands are intentionally omitted from planning. Generate and apply migrations only when explicitly requested during implementation.

## API Validation Scenarios

### Existing Material Adjustment

1. Load an existing Inventory Balance.
2. Capture `BalanceVersion`.
3. Submit `POST /api/wms/inventory/adjustments` with counted quantity different from current quantity and the captured version.
4. Expect `200 OK`.
5. Confirm returned quantity equals counted quantity.
6. Confirm returned `BalanceVersion` is present and changed.
7. Confirm one transaction and one ledger entry exist with correct before, delta, and after values.

### Existing No-Op

1. Load an existing Inventory Balance.
2. Capture quantity, timestamp, and `BalanceVersion`.
3. Submit counted quantity equal to current quantity and the captured version.
4. Expect `200 OK`.
5. Confirm quantity, timestamp, and `BalanceVersion` are unchanged.
6. Confirm no transaction or ledger entry was created.

### Missing Positive Initialization

1. Select a valid active SKU and valid active storage location with no balance.
2. Submit counted quantity greater than zero with `ExpectedBalanceVersion = null`.
3. Expect `200 OK`.
4. Confirm balance exists with counted quantity.
5. Confirm one transaction and one ledger entry exist from before `0`.

### Missing Zero Initialization

1. Select a valid active SKU and valid active storage location with no balance.
2. Submit counted quantity `0` with `ExpectedBalanceVersion = null`.
3. Expect `200 OK`.
4. Confirm a zero-quantity balance exists.
5. Confirm no transaction or ledger entry was created.

### Validation

Confirm each returns validation failure and no state change:

- Negative counted quantity.
- Missing reason.
- Whitespace-only reason.
- Reason longer than 500 characters after trimming.
- Invalid Base64 expected version.

Confirm missing/not-found and eligibility semantics preserve current Myrmex behavior:

- Missing SKU, storage location, or required related record returns the existing NotFound result, normally HTTP 404.
- Existing but inactive or otherwise ineligible references during missing-balance initialization use the current create-handler validation/conflict convention.

### Concurrency

Confirm each returns `409 InventoryBalance.ConcurrencyConflict` and no partial state:

- Existing balance with stale version.
- Existing balance with `ExpectedBalanceVersion = null`.
- Missing balance with non-null expected version.
- EF rowversion save conflict.
- Concurrent duplicate insert for the SKU/location pair during expected-absence initialization.

For save-time conflicts, confirm the handler returns conflict immediately, does not retry `SaveChangesAsync`, and does not reuse the failed tracked graph for automatic retry.

## WebApp Manual Smoke Scenarios

### Existing Adjustment Dialog

1. Open Inventory Balances page.
2. Open `Adjust` from a balance row.
3. Confirm SKU, warehouse, storage location, base UoM, and current quantity are read-only.
4. Enter counted quantity and reason.
5. Save.
6. Confirm success feedback and grid reload.

### Initial Count Dialog

1. Open create/initial-count action.
2. Select SKU, warehouse, and storage location.
3. Confirm storage locations remain warehouse-scoped.
4. Enter counted quantity and reason.
5. Save.
6. Confirm success feedback and grid reload.

### UI Error Handling

Confirm clear recoverable feedback for:

- Missing/too-long reason.
- Negative counted quantity.
- Duplicate/concurrent missing-balance initialization.
- Concurrency conflict with refresh-and-review guidance.
- Unexpected request failure.
