# Add WebApp Inventory Balance management UI

## Status

Draft stakeholder requirements.

## Context

Myrmex WMS has an Inventory Balance backend capability that represents the current known quantity of one SKU at one storage location.

The existing backend supports:

* create Inventory Balance;
* get Inventory Balance by id;
* list Inventory Balances;
* filter balances by warehouse, storage location, and SKU;
* update Inventory Balance quantity only.

The WebApp currently does not provide a user interface for viewing or maintaining Inventory Balances.

This feature adds the first Inventory UI without introducing inventory transactions, movement history, receiving, LPN, or other warehouse execution workflows.

## Business Goal

Allow a user to:

* see which SKUs are stored in each warehouse and storage location;
* see where a selected SKU is stored and in what quantity;
* create an initial/current Inventory Balance manually;
* correct the current quantity of an existing Inventory Balance.

This UI represents current stock state only.

It is not an inventory transaction or adjustment document workflow.

## Capability Placement

The page belongs to the WebApp Inventory capability.

Suggested UI structure:

```text
Wms
└── Inventory
    ├── InventoryBalances.razor
    ├── CreateInventoryBalanceDialog.razor
    └── UpdateInventoryBalanceQuantityDialog.razor
```

Exact file names and folder layout must follow existing Myrmex WebApp conventions.

The feature should add an Inventory navigation item with an Inventory Balances child page or equivalent navigation structure consistent with the existing application.

## MVP Scope

The MVP must include:

* Inventory Balances page;
* paged Inventory Balance grid;
* warehouse filter;
* storage location filter;
* SKU filter;
* create Inventory Balance dialog;
* update quantity dialog;
* lookup loading;
* list loading;
* empty state;
* error state;
* successful operation feedback;
* list refresh after create/update.

## Explicitly Out of Scope

Do not implement:

* receiving;
* putaway;
* picking;
* shipping;
* LPN;
* batch/lot tracking;
* expiry date;
* serial numbers;
* reservations;
* inventory transactions;
* inventory movement history;
* adjustment documents;
* UoM conversions;
* packaging;
* cycle counting;
* Inventory Balance delete;
* Inventory Balance deactivate/reactivate;
* changing SKU or storage location of an existing Inventory Balance;
* bulk editing;
* import/export;
* seed/demo data;
* external integrations;
* backend domain redesign;
* backend persistence redesign.

## Functional Requirements

### FR-001 Inventory Navigation

The WebApp must expose the Inventory capability through application navigation.

The navigation must provide access to the Inventory Balances page.

The exact navigation hierarchy and icon must follow existing WebApp conventions.

### FR-002 Inventory Balance List

The Inventory Balances page must display a bounded/paged list of balances.

Each row must include at minimum:

* SKU code;
* SKU name;
* warehouse;
* storage location;
* quantity;
* SKU base UoM code or symbol.

The page may also display created and updated timestamps if consistent with existing grid patterns, but timestamps are not required for the MVP.

Zero quantity balances must remain visible.

### FR-003 Warehouse Filter

The user must be able to filter Inventory Balances by warehouse.

Changing the selected warehouse must reload the list.

Changing warehouse must also clear a selected storage location when that location does not belong to the newly selected warehouse.

### FR-004 Storage Location Filter

The user must be able to filter Inventory Balances by storage location.

When a warehouse is selected, the storage location lookup must show only locations from that warehouse.

When no warehouse is selected, the implementation may either:

* show locations from all warehouses; or
* require warehouse selection before enabling the storage location selector.

The selected behavior must be consistent and clear in the UI.

### FR-005 SKU Filter

The user must be able to filter balances by SKU.

The filter must support the scenario:

> Show where this SKU is stored and in what quantity.

The SKU filter may be combined with the warehouse filter.

### FR-006 Create Inventory Balance

The page must provide an action to open a create dialog.

The create dialog must collect:

* SKU;
* warehouse;
* storage location;
* quantity.

The SKU selector must use active SKUs.

The storage location selector must use valid active storage locations.

The storage location selector must be constrained by the selected warehouse.

The selected SKU base UoM must be displayed as read-only context.

The quantity must be entered in the SKU base UoM.

Quantity must be greater than or equal to zero.

The request must use the existing create Inventory Balance API contract.

### FR-007 Create Conflict Handling

If a balance already exists for the selected SKU and storage location, the backend conflict must be displayed using the existing WebApp error handling pattern.

The UI must not create a second balance for the same SKU and storage location.

The UI is not required to proactively load all balances to detect duplicates before submission.

Backend uniqueness remains the source of truth.

### FR-008 Update Inventory Balance Quantity

Each balance row must provide an action to update quantity.

The update dialog must display as read-only context:

* SKU;
* warehouse;
* storage location;
* base UoM.

The only editable field must be quantity.

The update request must contain only quantity.

The UI must not send or allow changes to:

* SKU id;
* storage location id;
* warehouse id;
* base UoM id.

Quantity must be greater than or equal to zero.

### FR-009 Refresh After Mutation

After a successful create or quantity update:

* the dialog must close;
* success feedback should follow existing WebApp conventions;
* the Inventory Balance list must refresh;
* active filters and paging state should be preserved where practical.

### FR-010 Error Handling

API validation, not-found, conflict, and unexpected errors must follow existing Myrmex WebApp error handling patterns.

The feature must not introduce a new global error handling mechanism.

The user must receive meaningful feedback when:

* quantity is invalid;
* referenced SKU or storage location is no longer valid;
* a duplicate balance is submitted;
* an Inventory Balance no longer exists;
* the API request fails unexpectedly.

### FR-011 Loading and Empty States

The page must show a loading state while balances are being loaded.

Lookup controls must show or handle their loading state consistently with existing dialogs.

When no balances match the selected filters, the page must show a clear empty state rather than an error.

## Lookup Requirements

### Warehouse Lookup

Use the existing Topology client/pattern for warehouse selection.

Only active warehouses should be selectable where the existing lookup contract supports lifecycle filtering.

### Storage Location Lookup

Use the existing Topology client/pattern for storage locations.

The create dialog must permit only storage locations that are eligible for Inventory Balance creation:

* active StorageLocation;
* active StorageLocationType;
* active StorageLocationStatus.

`IsPickable` must not restrict Inventory Balance creation.

Storage location type code must not restrict Inventory Balance creation.

If existing lookup APIs cannot express all eligibility rules, backend create validation remains authoritative.

### SKU Lookup

Use the existing Catalog client/pattern.

Only active SKUs should be selectable.

The selected SKU must have a base UoM.

The base UoM must be displayed but must not be editable in this feature.

## UI Behavior

### Grid

The grid should follow existing WebApp data-grid conventions.

Suggested columns:

* SKU;
* warehouse;
* storage location;
* quantity;
* base UoM;
* actions.

The exact presentation may combine SKU code/name and location code/name in single cells if consistent with other pages.

### Filtering

Filters should be placed above the grid or in the existing standard filter area.

Filter changes should trigger reload behavior consistent with existing pages.

The feature must not introduce a second competing filtering pattern.

### Create Dialog

Suggested control order:

1. SKU;
2. base UoM display;
3. warehouse;
4. storage location;
5. quantity.

Changing warehouse must clear an incompatible selected storage location.

Changing SKU must refresh the displayed base UoM context.

### Update Dialog

Suggested display:

* SKU code/name;
* warehouse;
* storage location;
* base UoM;
* quantity input.

Only quantity is editable.

## API Usage

Use the existing `WmsInventoryApiClient` operations:

* list Inventory Balances;
* create Inventory Balance;
* update Inventory Balance quantity;
* get by id only if required by the selected UI flow.

Reuse existing Catalog and Topology clients for lookup data.

Do not duplicate API request/response DTOs inside Razor components.

## Non-Functional Requirements

* Follow existing Myrmex WebApp patterns.
* Keep component responsibilities narrow.
* Do not introduce new UI frameworks.
* Do not introduce new state-management frameworks.
* Do not introduce broad shared-component refactoring unless strictly required.
* Preserve backend API contracts.
* Keep the feature independently testable.
* Maintain clear loading, disabled, validation, and error states.
* Build, tests, app startup, EF migration generation, database update, migration application, and infrastructure commands are developer-controlled and must not be executed by the agent.

## Test Expectations

Tests should cover at minimum:

* Inventory client list query construction;
* create client request contains SKU, storage location, and quantity;
* update request contains quantity only;
* warehouse filter is passed to the list request;
* storage location filter is passed to the list request;
* SKU filter is passed to the list request;
* create validation rejects negative quantity;
* update validation rejects negative quantity;
* changing warehouse clears incompatible storage location selection where component testing patterns permit;
* successful create refreshes the list where practical;
* successful update refreshes the list where practical;
* API conflict/error handling follows existing test patterns.

Do not introduce a new browser/end-to-end test framework for this feature.

## Acceptance Criteria

* Inventory navigation is available.
* Inventory Balances page loads successfully.
* Balances are shown with SKU, warehouse, storage location, quantity, and base UoM.
* Zero quantity balances are visible.
* The list can be filtered by warehouse.
* The list can be filtered by storage location.
* The list can be filtered by SKU.
* SKU and warehouse filters can be combined.
* Storage locations are constrained by the selected warehouse.
* A user can create an Inventory Balance.
* The selected SKU base UoM is shown during creation.
* Quantity accepts zero and rejects negative values.
* Duplicate SKU/location conflict is displayed.
* A user can update quantity only.
* Existing SKU and storage location cannot be changed during update.
* Successful mutations refresh the grid.
* The feature does not introduce inventory transactions, movement history, delete, deactivate/reactivate, or warehouse execution workflows.
