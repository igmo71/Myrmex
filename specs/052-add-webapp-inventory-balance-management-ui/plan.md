# Implementation Plan: WebApp Inventory Balance Management UI

**Branch**: `52-add-webapp-inventory-balance-management-ui` | **Date**: 2026-06-13 | **Spec**: `specs/052-add-webapp-inventory-balance-management-ui/spec.md`

**Input**: Feature specification from `specs/052-add-webapp-inventory-balance-management-ui/spec.md`, `StakeholderDocs/Wms/Inventory/052 Add WebApp Inventory Balance management UI.md`, `.specify/memory/constitution.md`, durable Myrmex workflow guidance, architecture/testing/API memory, and existing WMS WebApp Catalog, Topology, and Inventory client patterns.

## Summary

Add the first WebApp Inventory management UI for Inventory Balances. The page lets a warehouse operations user view current stock, filter balances by warehouse, storage location, and SKU, create an initial current balance, and update quantity only. The implementation reuses the existing Inventory Balance backend and WebApp API client, Catalog and Topology lookup clients, MudBlazor page/grid/dialog patterns, and existing WebApp error and feedback conventions.

The feature is a UI/client vertical slice over the already implemented Inventory Balance backend. It explicitly avoids inventory transactions, movement history, receiving, putaway, picking, shipping, LPN, reservations, UoM conversion, delete, deactivate/reactivate, bulk editing, import/export, seed/demo data, backend domain redesign, and backend persistence redesign.

## Technical Context

**Language/Version**: C# on the existing Myrmex .NET 10 solution.

**Primary Dependencies**: Existing Blazor WebApp, MudBlazor, `HttpClient` API clients, `Myrmex.WebApp.Wms.Inventory.WmsInventoryApiClient`, `Myrmex.WebApp.Wms.Catalog.WmsCatalogApiClient`, `Myrmex.WebApp.Wms.Topology.WmsTopologyApiClient`, shared `Myrmex.WebApp.Wms.Api` result/list helpers, and xUnit test project.

**Storage**: No new storage. Inventory Balance persistence is owned by the existing backend slice. This feature reads and writes through existing WebApp API clients.

**Testing**: Existing xUnit tests. Add focused API-client/registration tests where needed for WebApp Inventory client availability and request shape. UI/component automation is deferred because the test project has no bUnit or equivalent component-test infrastructure; manual UI smoke validation is required in `quickstart.md`.

**Target Platform**: Existing Myrmex Blazor WebApp and API service in the modular-monolith solution.

**Project Type**: Brownfield web application UI slice over existing WMS Inventory API behavior.

**Performance Goals**: Users can reach the page in at most 3 navigation interactions, identify matching SKU/location quantities in under 30 seconds after filters are applied, create a valid balance in under 2 minutes, and update quantity in under 1 minute.

**Constraints**: Preserve existing WebApp navigation, MudBlazor page, filter, grid, dialog, snackbar, validation, and API-client patterns. Do not introduce a new UI framework, state-management framework, browser test framework, backend domain changes, persistence changes, migrations, seed/demo data, or infrastructure-affecting commands. Storage location selectors remain disabled until a warehouse is selected.

**Scale/Scope**: One WebApp Inventory page, two Inventory Balance dialogs, one filter component, one grid component, WebApp navigation and DI registration, focused client/registration tests, contracts, and manual validation guide.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Domain Model First**: PASS. The plan names Inventory Balance, SKU, warehouse, storage location, quantity, base UoM, SKU/location uniqueness, active reference lookup, and quantity-only update behavior before UI implementation details.
- **Modular Monolith Boundaries**: PASS. Runtime changes stay in `Myrmex.WebApp` and consume existing WMS API clients. Backend Inventory, Catalog, and Topology ownership remains unchanged.
- **Vertical Slice Delivery**: PASS. The slice includes navigation, page, filters, grid, create dialog, update dialog, WebApp API-client integration, and validation guidance. Backend endpoint, handler, domain, and persistence behavior already exists and is reused rather than redesigned.
- **Testing Discipline**: PASS with documented Principle IV UI automation exception below. API-client/registration coverage and manual UI smoke validation are identified; new component automation infrastructure is deferred.
- **Simplicity and Observability**: PASS. The plan reuses existing local WebApp and API-result patterns, avoids new frameworks or broad shared-component refactoring, and surfaces errors through existing alerts, dialog errors, and snackbars.

## Project Structure

### Documentation (this feature)

```text
specs/052-add-webapp-inventory-balance-management-ui/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── inventory-balance-webapp-ui-contract.md
├── checklists/
│   └── requirements.md
└── spec.md
```

### Source Code (repository root)

```text
Myrmex.WebApp/
├── Program.cs
├── Components/
│   ├── Layout/
│   │   └── NavMenu.razor
│   └── Pages/Wms/Inventory/InventoryBalancePages/
│       ├── Index.razor
│       ├── Index.razor.cs
│       ├── InventoryBalanceFilters.razor
│       ├── InventoryBalanceGrid.razor
│       ├── CreateInventoryBalanceDialog.razor
│       └── UpdateInventoryBalanceQuantityDialog.razor
└── Wms/
    ├── Catalog/WmsCatalogApiClient.cs
    ├── Inventory/WmsInventoryApiClient.cs
    └── Topology/WmsTopologyApiClient.cs

Myrmex.Tests/
└── Wms/Inventory/
    └── Client/WmsInventoryApiClientTests.cs
```

**Structure Decision**: Add a WebApp Inventory page area under `Myrmex.WebApp/Components/Pages/Wms/Inventory/InventoryBalancePages` to mirror existing Catalog and Topology page conventions. Keep typed API contracts in the existing `Myrmex.WebApp/Wms/Inventory/WmsInventoryApiClient.cs` file unless implementation discovers a narrow client registration gap. Do not touch backend Inventory domain, handlers, endpoints, persistence, or migrations.

## Phase 0: Research Output

Create `research.md` with decisions for:

- Reusing the existing Inventory Balance backend and WebApp API client.
- Adding Inventory navigation under the existing WMS navigation group.
- Creating a new WebApp Inventory page component set rather than extending Catalog or Topology pages.
- Using warehouse-first storage location lookup behavior for filters and create dialog.
- Loading active warehouses, active SKUs, and warehouse-scoped active storage locations through existing clients.
- Preserving list filters and paging after create/update where the updated row remains visible.
- Showing duplicate, validation, not-found, and read/load failures through existing dialog/page/snackbar patterns.
- Registering the Inventory API client in WebApp DI if it is not already registered.
- Deferring UI/component automation due to absent component-test infrastructure and requiring manual smoke validation.
- Rejecting backend redesign, new UI frameworks, new state-management frameworks, and out-of-scope warehouse execution flows.

## Phase 1: Design Outputs

Create `data-model.md` for UI-facing state and contracts:

- `InventoryBalancePageState`
- `InventoryBalanceFilters`
- `CreateInventoryBalanceDialogState`
- `UpdateInventoryBalanceQuantityDialogState`
- `InventoryBalanceGridRow`
- lookup state for warehouses, SKUs, storage locations, and base UoM display context
- validation and state transition rules

Create `contracts/inventory-balance-webapp-ui-contract.md` for:

- route and navigation contract
- page loading/empty/error contract
- filter behavior contract
- create dialog contract
- update quantity dialog contract
- API-client usage contract
- feedback and out-of-scope behavior

Create `quickstart.md` as a validation guide for implementation review. It must include artifact checks, recommended developer-controlled build/test/startup commands, focused API-client checks, manual UI smoke scenarios, scope-boundary checks, and no migration/database commands because this feature does not change persistence.

Update `AGENTS.md` between the Spec Kit markers so active feature work points agents to this plan.

## Post-Design Constitution Check

- **Domain Model First**: PASS. Design artifacts define Inventory Balance UI state in terms of SKU, warehouse, storage location, current quantity, base UoM, and quantity-only correction.
- **Modular Monolith Boundaries**: PASS. Contracts keep UI work in the WebApp and use existing Inventory, Catalog, and Topology clients. Backend module boundaries are unchanged.
- **Vertical Slice Delivery**: PASS. The UI slice covers navigation, page, filters, grid, dialogs, API-client wiring, feedback, tests where supported, and manual validation.
- **Testing Discipline**: PASS with the Principle IV exception below. The plan records targeted automated coverage plus manual UI smoke validation because component automation infrastructure is not present.
- **Simplicity and Observability**: PASS. Research rejects new frameworks and backend redesign; UI errors use existing alert, dialog, snackbar, `ApiResult<T>`, and `ApiException` behavior.

No architecture complexity exceptions are requested.

## Complexity Tracking

No architecture complexity exceptions are requested.

### Principle IV Endpoint/UI Test Exceptions

| Deferred automated test | Why deferred | Lower-level automated coverage | Manual validation required | Follow-up issue needed? |
|-------------------------|--------------|--------------------------------|----------------------------|-------------------------|
| Inventory Balance Blazor component automation | The test project has xUnit and API/client/domain coverage but no bUnit or equivalent component-test infrastructure. Adding a new UI test framework is disproportionate for this MVP page and would be a cross-cutting test-infrastructure decision. | Existing Inventory Balance domain/handler/persistence/client tests from the backend slice, plus focused WebApp Inventory API client/registration tests where implementation changes client wiring. | `quickstart.md` requires manual checks for navigation, list loading, filters, disabled storage-location selectors before warehouse selection, create dialog, update dialog, success feedback, refresh, empty state, and error states. | No. A future cross-cutting UI test-infrastructure issue may be opened if the project adopts component automation by default. |
