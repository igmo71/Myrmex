## Goal

Implement a narrow WMS Catalog Unit of Measure (UoM) MVP vertical slice.

This issue should add UoM as the next Catalog reference-data entity after SKU, while deliberately avoiding conversions, packaging, barcode, inventory, receiving, and SKU-to-UoM binding.

## Context

Issue #32 implemented Catalog/SKU as the first fully covered reference-data vertical slice after Spec Kit stabilization.

Issue #34 refined the testing strategy for repeated reference-data slices. UoM should be the first feature to apply that policy: reuse the established SKU-style pattern, but avoid duplicating the full SKU-level test matrix unless UoM introduces genuinely new behavior.

Topology already provides the "where" side of WMS. SKU provides the "what" side. UoM should provide the minimal "how quantities are expressed" foundation before later work on SKU base UoM, barcodes, packaging, inventory, or receiving.

## Proposed scope

Add a minimal Catalog UoM reference-data vertical slice following the established Catalog/SKU conventions.

### Domain

Add a `UnitOfMeasure` aggregate/entity with minimal reference-data behavior:

- `Id`
- `Code`
- `Name`
- optional `Symbol` if it fits the existing conventions cleanly
- `IsActive`
- `CreatedAtUtc`
- `UpdatedAtUtc`

Expected behavior:

- create validation;
- update details;
- deactivate/reactivate;
- idempotent deactivate/reactivate without lifecycle event for no-op calls;
- domain events only for real state changes;
- normalized code stored directly in `Code`, consistent with SKU;
- no separate `NormalizedCode` field.

### Persistence

Add EF Core mapping and migration for a new UoM table under the existing WMS schema.

Expected conventions:

- table under `wms` schema;
- unique index on `Code`;
- required fields and max lengths aligned with existing Catalog/SKU style;
- `UpdatedAtUtc` remains null on creation and is set only after update/deactivate/reactivate.

### Application / handlers

Add command/query handlers for:

- create UoM;
- list UoMs;
- get UoM by id;
- update UoM details;
- deactivate UoM;
- reactivate UoM.

### API

Add Catalog UoM endpoints following the SKU endpoint style, for example:

- `POST /api/wms/catalog/uoms`
- `GET /api/wms/catalog/uoms`
- `GET /api/wms/catalog/uoms/{unitOfMeasureId}`
- `PUT /api/wms/catalog/uoms/{unitOfMeasureId}`
- `POST /api/wms/catalog/uoms/{unitOfMeasureId}/deactivate`
- `POST /api/wms/catalog/uoms/{unitOfMeasureId}/reactivate`

Sorting should stay provider-safe. If `DateTimeOffset` ordering is not supported by the SQLite test provider, do not add date sorting or provider-specific branching.

### Web API client

Add UoM client support following the established Catalog/SKU API client conventions:

- write/action operations should use ApiResult-style behavior;
- read/load operations should use exception-style behavior;
- reuse existing/local Catalog client error/result conventions instead of inventing a new pattern.

### UI

Add a minimal Catalog UoM page following the SKU UI pattern:

- Catalog menu item for UoMs;
- `/wms/catalog/uoms` route;
- list/search/sort;
- include inactive;
- create/edit dialog;
- deactivate/reactivate actions;
- snackbar/reload behavior consistent with SKU.

## Testing expectations

Apply the refined testing strategy from issue #34.

Because UoM is a repeated Catalog reference-data slice, avoid copying the full SKU-level test matrix unless UoM introduces genuinely new behavior.

Expected focused coverage:

- domain tests for UoM-specific validation and lifecycle behavior;
- handler tests where UoM behavior differs from the representative SKU pattern or needs entity-specific confidence;
- persistence tests for UoM mapping, required fields, unique `Code`, and table/index shape;
- API client tests for UoM route/DTO/result wiring where not already covered by the representative Catalog client pattern;
- manual UI smoke for the UoM page.

Endpoint/UI automation may be deferred if it would require new frameworks or broad test-host infrastructure. Any deferral must state the lower-level automated coverage and manual validation performed.

## Explicit non-goals

Do not implement:

- UoM conversion rules;
- base/alternative UoM model;
- SKU-to-UoM binding;
- packaging levels;
- barcode support;
- inventory quantities;
- receiving flows;
- LPN behavior;
- picking/shipping behavior;
- provider-specific query branching;
- client-side sorting via `AsEnumerable()` just to support unsupported provider ordering;
- new endpoint/UI test frameworks.

## Acceptance criteria

- [ ] UoM domain model and lifecycle behavior are implemented.
- [ ] UoM EF Core mapping and migration are added.
- [ ] UoM command/query handlers are implemented.
- [ ] UoM API endpoints are exposed under `/api/wms/catalog/uoms`.
- [ ] UoM Web API client support is added using existing Catalog client conventions.
- [ ] UoM UI page is available under `/wms/catalog/uoms`.
- [ ] UoM appears in the Catalog navigation.
- [ ] Sorting is limited to provider-safe fields.
- [ ] Focused automated tests are added according to the repeated reference-data testing policy.
- [ ] Endpoint/UI automation deferral, if any, is explicitly documented in the plan.
- [ ] Manual UI smoke is performed and recorded.
- [ ] `dotnet build Myrmex.slnx -nologo -v:minimal` passes.
- [ ] Focused UoM tests pass.
- [ ] Full regression tests pass.