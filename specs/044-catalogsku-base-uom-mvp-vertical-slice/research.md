# Research: Catalog/SKU Base UoM MVP Vertical Slice

## Decision: Extend the Existing SKU Aggregate

**Decision**: Add base UoM assignment to the existing `StockKeepingUnit` aggregate and SKU vertical slice.

**Rationale**: Issue #44 changes required SKU master data. A base UoM is an invariant of a SKU, not an independent lifecycle record or separate workflow.

**Alternatives considered**:

- Separate SKU Base UoM aggregate: rejected because it adds lifecycle and ownership complexity that the MVP does not need.
- Separate Base UoM endpoint group: rejected because create/update/get/list SKU operations are the required user-facing surface.

## Decision: Store Required `BaseUnitOfMeasureId` on SKU

**Decision**: Add required `BaseUnitOfMeasureId` to SKU create/update inputs, SKU details, list projections, and persistence.

**Rationale**: The requirements state that every SKU references exactly one base UoM and that create/update/get/list contracts expose the identity.

**Alternatives considered**:

- Nullable transition: rejected because the database is development-only and the issue explicitly does not require production-safe preservation.
- Returning full UoM details in SKU results: rejected because the source requirements require `BaseUnitOfMeasureId`, not embedded UoM read models.

## Decision: Validate Existing Active UoM at Assignment Time

**Decision**: SKU create and update handlers must confirm the supplied base UoM exists and is active before saving.

**Rationale**: The stakeholder source requires referenced UoM validation and active UoM assignment unless existing Catalog behavior suggests otherwise. Existing Catalog reference data hides inactive records by default and treats lifecycle as meaningful, so active assignment is the consistent default.

**Alternatives considered**:

- Allow inactive UoMs for assignment: rejected because it would let new SKU master data depend on deactivated reference data.
- Validate existence only through database foreign key failure: rejected because users need clear missing/inactive UoM errors through existing result conventions.

## Decision: Existing Assigned UoM Can Later Become Inactive Without Cascading SKU Changes

**Decision**: If an assigned UoM is later deactivated through existing UoM lifecycle behavior, the SKU keeps its stored base UoM identity and get/list still return it.

**Rationale**: Issue #44 is limited to assignment validation on SKU create/update. Cascading SKU lifecycle changes or blocking UoM deactivation would broaden existing UoM behavior.

**Alternatives considered**:

- Prevent deactivation of any assigned UoM: rejected as a new UoM lifecycle rule outside issue #44.
- Automatically clear or change SKU base UoM when a UoM is deactivated: rejected because a SKU must always have exactly one base UoM and this feature has no replacement-selection workflow.

## Decision: Reuse Existing SKU Commands and Details Contract

**Decision**: Extend `CreateStockKeepingUnit.Command`, `UpdateStockKeepingUnitDetails.Command`, `StockKeepingUnitDetails`, `CreateStockKeepingUnitRequest`, `UpdateStockKeepingUnitDetailsRequest`, and WebApp client request/response records with `BaseUnitOfMeasureId`.

**Rationale**: The user-facing behavior is SKU create/update/get/list with the required base UoM identity present. Reusing existing SKU commands preserves the accepted Catalog/SKU vertical-slice pattern.

**Alternatives considered**:

- Add a separate `ChangeStockKeepingUnitBaseUnitOfMeasure` command: rejected for this MVP because update is already the specified flow and a separate command would add another user-facing operation not requested.
- Add UoM lookup data to every SKU response: rejected as unnecessary for this increment.

## Decision: EF Core Required Relationship and Index

**Decision**: Configure a required relationship from `StockKeepingUnit.BaseUnitOfMeasureId` to `UnitOfMeasure.Id` and add an index for the base UoM identity.

**Rationale**: The domain requires a valid UoM reference, and persistence must enforce the relationship. An index supports future filtered review or diagnostics without adding new behavior.

**Alternatives considered**:

- Store base UoM code instead of identity: rejected because the requirements name `BaseUnitOfMeasureId` and existing entities use stable identities for relationships.
- Use a shadow property only: rejected because commands, details, domain rules, and tests need explicit SKU-level access to the base UoM identity.

## Decision: Developer-Controlled Migration Workflow

**Decision**: Plan for EF model/configuration changes and document exact migration/database commands, but do not run migration generation or database update automatically.

**Rationale**: Project workflow and issue #44 both require build, test, startup, migration generation, migration application, and database update to remain developer-controlled.

**Alternatives considered**:

- Generate migration during planning: rejected by explicit workflow boundary.
- Defer migration command guidance until implementation: rejected because the plan should make the expected developer-controlled migration step visible before tasks are generated.

## Decision: Focused Test Scope

**Decision**: Add focused tests for the new base UoM invariant, assignment validation, projections, persistence relationship, API/client contract changes, and regressions for existing Catalog behavior.

**Rationale**: This is a change to an existing representative SKU slice, not a new repeated CRUD entity. The new risk is the required relationship and validation, so tests should concentrate there.

**Alternatives considered**:

- Copy the full SKU/UoM/SKU Barcode test matrix: rejected as redundant and broader than the issue.
- Rely only on manual API checks: rejected because changed domain rules, handlers, persistence mappings, and API client contracts require automated coverage under the constitution.

## Decision: No New UI Phase

**Decision**: Do not add a new UI page or Base UoM selection workflow in this feature. Existing SKU UI/client records may be adjusted only as needed for request/response compatibility.

**Rationale**: The stakeholder source explicitly excludes UI implementation. The feature scope is required SKU Base UoM binding through existing Catalog/SKU contracts.

**Alternatives considered**:

- Add a Base UoM dropdown to the SKU page: rejected as UI implementation outside issue #44.
- Add UoM selection dialogs or lookup screens: rejected as new UI workflow not requested.

## Decision: Explicit Non-Goals

**Decision**: Exclude alternative UoMs, conversions, packaging, inventory, receiving, LPN, picking, shipping, seed/demo data, external integrations, and broad refactoring.

**Rationale**: The source requirements list these as out of scope, and the roadmap requires separate specs/plans for each future area.

**Alternatives considered**:

- Prepare generic conversion-ready abstractions now: rejected because they do not solve the current required binding problem and would violate pragmatic simplicity.
