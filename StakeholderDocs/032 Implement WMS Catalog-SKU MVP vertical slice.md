## Goal

Implement the first real WMS Catalog/SKU vertical slice after Spec Kit stabilization.

This issue should produce working functionality, not only documentation.

## Context

Issue #30 / PR #31 stabilized the Spec Kit + Codex CLI workflow and captured Myrmex architecture and domain decisions as durable memory docs.

The current reference implementation pattern is WMS Topology: Warehouse, Zone, and StorageLocation.

Catalog/SKU is the next foundational WMS capability because it will later support Barcode, UoM, Packaging, Inventory, Receiving, LPN contents, Picking, and Shipping.

## Scope

Implement a minimal but real Catalog/SKU vertical slice.

### Domain

Add a `StockKeepingUnit` aggregate/entity with at least:

- `Id`
- `Code`
- `Name`
- optional `Description`
- `IsActive`
- `CreatedAtUtc`
- `UpdatedAtUtc`

Rules:

- `Code` is required.
- `Code` is normalized consistently with existing project text normalization patterns.
- `Name` is required.
- `Code` must be unique within Catalog/SKU scope.
- SKU can be deactivated/reactivated.
- Do not model Inventory, Barcode, UoM, Packaging, Receiving, or LPN contents in this issue.

### Application / Handlers

Add explicit command/query handlers, following the existing internal dispatcher pattern:

- Create SKU
- Update SKU details
- Deactivate SKU
- Reactivate SKU
- Get SKU by id
- List SKUs

### API

Add Minimal API endpoints under a clear WMS Catalog route, for example:

- `GET /api/wms/catalog/skus`
- `GET /api/wms/catalog/skus/{skuId}`
- `POST /api/wms/catalog/skus`
- `PUT /api/wms/catalog/skus/{skuId}`
- `POST /api/wms/catalog/skus/{skuId}/deactivate`
- `POST /api/wms/catalog/skus/{skuId}/reactivate`

Follow the accepted API error-handling convention:

- write/action operations return `ApiResult<T>`
- read/load operations remain exception-based and ProblemDetails-aware

### Persistence

Add EF Core mapping and migration for SKU storage.

Expected persistence constraints:

- primary key on `Id`
- unique index on normalized `Code`
- active/inactive state persisted
- timestamps persisted

### Web UI / Client

Add a minimal Blazor/MudBlazor Catalog/SKU page following existing WMS Topology UI/client patterns.

Minimum UI capability:

- list SKUs
- create SKU
- update SKU details
- deactivate/reactivate SKU
- display expected API errors in the existing UI style

### Tests

Add regression tests appropriate for the first Catalog/SKU slice:

- domain tests for SKU creation/validation/state transitions
- application/handler tests for create/update/deactivate/reactivate/list/get where practical
- API client error-handling tests mirroring the WMS Topology client pattern

## Out of Scope

Do not implement:

- Inventory
- Barcode model
- UoM model/conversion
- Packaging hierarchy
- Receiving
- LPN contents
- Picking/Shipping
- external integration
- broad refactoring
- MediatR or new framework adoption

## Spec Kit / Codex Workflow

Use the stabilized Spec Kit workflow from issue #30.

Before implementation, Codex CLI must read:

- `AGENTS.md`
- `.specify/memory/constitution.md`
- `.specify/memory/myrmex-architecture.md`
- `.specify/memory/myrmex-development-workflow.md`
- `.specify/memory/myrmex-topology-patterns.md`
- `.specify/memory/myrmex-api-error-handling.md`
- `.specify/memory/myrmex-testing-guidelines.md`
- `.specify/memory/myrmex-roadmap.md`
- existing WMS Topology code and tests

Use Spec Kit commands through Codex CLI skills:

- `$speckit-specify`
- `$speckit-plan`
- `$speckit-tasks`
- `$speckit-analyze`
- `$speckit-implement`

This issue may include production code changes because it is an implementation issue, unlike issue #30.

## Acceptance Criteria

- [ ] Spec Kit feature artifacts exist for this issue.
- [ ] `StockKeepingUnit` domain model exists with required invariants.
- [ ] SKU persistence mapping and migration exist.
- [ ] Create/update/deactivate/reactivate/get/list handlers exist.
- [ ] API endpoints exist and follow accepted error-handling conventions.
- [ ] Web client/API client supports the minimal SKU workflow.
- [ ] Minimal Blazor/MudBlazor UI allows listing and maintaining SKUs.
- [ ] Regression tests cover domain rules, handlers, and API client error handling where appropriate.
- [ ] `dotnet test` passes.
- [ ] Manual UI/API smoke test is completed.
- [ ] No Inventory, Barcode, UoM, Packaging, Receiving, LPN, Picking, Shipping, or Integration implementation is included.

## Suggested Branch

`032-implement-wms-catalog-sku-mvp-vertical-slice`
