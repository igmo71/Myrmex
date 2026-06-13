# Research: WebApp Inventory Balance Management UI

## Decision: Reuse Existing Inventory Balance Backend and Client

**Decision**: Build the UI on the existing Inventory Balance backend capability and `WmsInventoryApiClient`.

**Rationale**: The backend slice already supports list, get by id, create, warehouse/storage-location/SKU filters, and quantity-only update. Reusing it keeps this feature scoped to WebApp behavior and preserves the modular-monolith boundary.

**Alternatives considered**:

- Add new backend endpoints for UI-specific behavior. Rejected because the current contracts already support the MVP.
- Redesign Inventory Balance domain or persistence. Rejected because it is explicitly out of scope and would duplicate issue #48 work.

## Decision: Add Inventory Navigation Under WMS

**Decision**: Add an Inventory navigation group or equivalent Inventory entry under the existing WMS nav group, with an Inventory Balances child page.

**Rationale**: Topology and Catalog already live under WMS navigation. Inventory Balance is a WMS capability, so users should find it alongside related warehouse operations pages.

**Alternatives considered**:

- Add Inventory Balances at top-level navigation. Rejected because it weakens the WMS capability grouping.
- Place Inventory Balances under Catalog or Topology. Rejected because Inventory Balance is current stock state, not reference data.

## Decision: Use a Dedicated Inventory Balance Page Component Set

**Decision**: Add a page, filters, grid, create dialog, and update quantity dialog under a WebApp Inventory page folder.

**Rationale**: Existing WMS pages use small page-specific components. The Inventory Balance UI has list/filter/create/update behavior that is related but distinct from existing Catalog and Topology CRUD pages.

**Alternatives considered**:

- Put all UI in one Razor file. Rejected because it would create a large component with mixed responsibilities.
- Create broad shared grid or dialog abstractions. Rejected because the current need is narrow and existing local page-specific patterns are sufficient.

## Decision: Require Warehouse Selection Before Storage Location Selection

**Decision**: Storage location selectors in filters and create flow remain unavailable until a warehouse is selected.

**Rationale**: The clarified spec requires warehouse-first lookup behavior. This keeps lookup results bounded and avoids showing locations from unrelated warehouses.

**Alternatives considered**:

- Show storage locations from all warehouses before warehouse selection. Rejected by clarification and likely to create large ambiguous lists.
- Let filters and dialogs differ. Rejected because inconsistent behavior would increase user confusion and test complexity.

## Decision: Use Existing Lookup Clients for Active Reference Data

**Decision**: Use the Topology client for warehouses and warehouse-scoped storage locations, and the Catalog client for active SKUs and base UoM context.

**Rationale**: These clients already encapsulate WebApp read/load behavior and list query construction. Backend validation remains authoritative for eligibility rules that lookups cannot fully express.

**Alternatives considered**:

- Duplicate lookup DTOs or raw HTTP calls in Razor components. Rejected because it bypasses existing client patterns.
- Add new lookup endpoints for this UI. Rejected unless implementation discovers an actual missing capability during planning or tasks.

## Decision: Preserve Filters and Paging After Mutations Where Practical

**Decision**: After successful create or quantity update, close the dialog, show success feedback, reload the list, and preserve active filters and paging unless doing so would hide the updated result or mislead the user.

**Rationale**: The spec asks for preserved state where practical. This behavior supports efficient repetitive corrections while still allowing the UI to keep users oriented after a create.

**Alternatives considered**:

- Always reset filters and paging after every mutation. Rejected because it disrupts focused warehouse/SKU work.
- Never adjust paging after create. Rejected because a newly created row could be hidden and make success feedback harder to verify.

## Decision: Use Existing Feedback and Error Patterns

**Decision**: Use page alerts for list/load failures, dialog-local errors for create/update validation and API failures, and snackbars for successful operations.

**Rationale**: Existing WMS WebApp pages use alerts, dialog error messages, and snackbars. Keeping those patterns avoids a second error/feedback model.

**Alternatives considered**:

- Add a new global error handling mechanism. Rejected by the spec.
- Convert all failures to snackbars. Rejected because dialog-local validation should remain close to the submitted form.

## Decision: Register Inventory API Client in WebApp DI

**Decision**: Ensure `WmsInventoryApiClient` is registered with the same base-address/service-discovery pattern as existing WMS Catalog and Topology clients.

**Rationale**: The client file already exists, but `Program.cs` currently registers Topology and Catalog clients only. The UI page needs the Inventory client from dependency injection.

**Alternatives considered**:

- Instantiate the client directly in components. Rejected because existing WebApp patterns use DI-registered typed clients.
- Merge Inventory methods into another WMS client. Rejected because Inventory already has a dedicated client boundary.

## Decision: Defer UI Component Automation

**Decision**: Do not add bUnit or another component-test framework for this feature. Use existing automated client/domain coverage and manual UI smoke checks.

**Rationale**: The test project contains xUnit and existing WMS client/domain/handler/persistence tests, but no component-test infrastructure. Adding a UI test framework is a cross-cutting decision beyond this MVP page.

**Alternatives considered**:

- Add component-test infrastructure now. Rejected as disproportionate to the feature and contrary to pragmatic simplicity.
- Skip all validation. Rejected because manual smoke checks are required to prove navigation, filters, dialogs, and feedback behavior.

## Decision: Keep Backend and Operational Scope Unchanged

**Decision**: Do not add backend domain behavior, persistence changes, migrations, transaction history, movement workflows, delete, deactivate/reactivate, bulk editing, import/export, or external integrations.

**Rationale**: The feature is a management UI over current stock state only. Extending operational inventory behavior would change product scope and require separate domain planning.

**Alternatives considered**:

- Add adjustment documents for quantity correction. Rejected because the MVP explicitly corrects current quantity only.
- Add movement or history display. Rejected because current stock state has no movement ledger in scope.
