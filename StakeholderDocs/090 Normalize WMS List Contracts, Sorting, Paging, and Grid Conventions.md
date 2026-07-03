# Normalize WMS List Contracts, Sorting, Paging, and Grid Conventions

## Issue

GitHub Issue: **#90 Normalize WMS list contracts, sorting, paging, and grid conventions**

Important repository workflow note:

* Use GitHub Issue number **90** when creating the Spec Kit folder.
* Do **not** infer the next issue number from existing branches, pull requests, or local folder names.
* Pull request **#89** already exists, so the Spec Kit folder must use `090`, not `089`.
* The expected Spec Kit folder is:

```text
specs/090-normalize-wms-list-contracts/
```

Branch workflow note:

* A branch for this work will be created manually by the stakeholder before the agent runs.
* The agent must use the **current branch**.
* The agent must **not** create, rename, switch, or delete branches.

## Background

Myrmex has accumulated several WMS vertical slices over time. Many of them implement backend-owned, server-driven list behavior: filtering, sorting, paging, projection, API request/response contracts, API client calls, and WebApp grid integration.

The current pattern has stabilized gradually, but earlier slices were implemented before all conventions became clear. As a result, similar list concerns are now solved inconsistently across WMS features.

The concern is not that the system is broken. The concern is that the same architectural idea is expressed in different ways depending on slice age, feature ownership, and implementation history. This makes future changes harder, increases regression risk, and makes it less clear which implementation should be treated as the reference pattern.

Before making code changes, we want a structured audit that identifies current inconsistencies and helps decide the exact refactoring tasks.

## Current Concern

WMS backend-owned list slices currently appear to differ in several areas:

* Some sort keys are defined as shared constants.
* Some sort keys are raw strings.
* Sort key values may use mixed casing conventions.
* Some WebApp grid `Tag` values use shared sort constants.
* Some WebApp grid `Tag` values use raw strings.
* Some backend sorting logic compares against shared constants.
* Some backend sorting logic compares against raw normalized strings.
* Some public request contracts are placed in `Myrmex.Shared`.
* Some older slices may still use local or internal request shapes.
* Some response contracts may differ in ownership or placement.
* Some list results use shared `ListResult<T>` consistently, while others may not.
* Paging defaults and numeric values may be duplicated.
* Some slices may normalize `Skip` and `Take` consistently, while others may do so locally.
* Some grids may still reflect older client-side or mixed conventions.
* Warehouse-related list columns may inconsistently use warehouse code versus warehouse name.

The intent is to normalize these conventions safely, but not before we have a clear picture of the current state.

## Desired Outcome

The desired long-term outcome is a consistent WMS server-driven list pattern across backend-owned list slices.

After the full initiative is complete, similar list slices should follow the same conventions for:

* public API request DTOs;
* public API response DTOs;
* shared list result contracts;
* sort key constants;
* sort key casing;
* WebApp grid `Tag` values;
* API request mapping;
* backend sorting logic;
* paging normalization;
* deterministic ordering;
* projection ownership;
* server-driven MudBlazor grid behavior;
* warehouse display conventions;
* focused regression test coverage.

However, the first phase must be audit-only.

## Phase 1 Goal

The first phase must produce an audit report only.

The output of Phase 1 should be:

```text
specs/090-normalize-wms-list-contracts/research.md
```

The report should give the team enough information to decide what to normalize first, what can be changed mechanically, what requires focused tests, and what should be deferred.

No production code changes should be made during Phase 1.

## Architectural Baseline

Use the existing project memory files as the baseline for expected conventions:

```text
.specify/memory/myrmex-development-workflow.md
.specify/memory/server-driven-list-slice-pattern.md
.specify/memory/webapp-localization.md
```

The most important baseline for this work is:

```text
.specify/memory/server-driven-list-slice-pattern.md
```

The audit should compare existing slices against this pattern rather than inventing a new abstraction or new convention.

## Scope

Review these WMS backend-owned list slices:

* Warehouses
* Zones
* Storage Locations
* Stock Keeping Units / SKUs
* Units of Measure / UoM
* Inventory Balances
* Inventory Ledger
* Inventory Transfers
* Inventory Counts

For each slice, identify the relevant files and current behavior across:

* WebApp page/component files;
* WebApp grid component files, if any;
* API client methods;
* Minimal API endpoint methods;
* backend feature/query/handler files;
* queryable extension files;
* projection files;
* shared request DTO files;
* shared response DTO files;
* tests, where relevant.

## Audit Requirements

### 1. Files and ownership

For each list slice, report:

* WebApp page/component files involved.
* WebApp grid component, if any.
* API client method involved.
* Minimal API endpoint method involved.
* Backend feature/query/handler files involved.
* Queryable extension/projection files involved.
* Shared request/response DTO files involved.

Prefer precise file paths over broad descriptions.

### 2. Public contracts

For each list slice, report:

* Whether public request DTOs crossing the backend/client boundary are in `Myrmex.Shared`.
* Whether public response DTOs are in `Myrmex.Shared`.
* Whether shared `ListResult<T>` is used.
* Whether any public contract depends on domain, EF Core, Blazor, MudBlazor, infrastructure, handlers, or UI state.

Public contracts should remain transport-oriented and should not depend on UI or infrastructure concerns.

### 3. Sort contracts

For each list slice, report:

* Current `SortBy` constants, if any.
* Namespace and file location of sort constants.
* Exact constant values.
* Casing style of sort key values.
* Whether WebApp grid `Tag` values use shared sort constants or raw strings.
* Whether API request `SortBy` values use shared constants.
* Whether backend `ApplySorting` compares against shared constants or raw strings.
* Whether currently needed user-facing sort keys are missing.
* Whether `WarehouseName` exists where warehouse name is displayed and sortable.
* Whether legacy `WarehouseCode` usage remains in inventory count, transfer, or related list grids and backend sorting.

Warehouse code must not be removed from the domain, database, import, API, or DTOs during this initiative. The concern is only about visible list/grid conventions and user-facing sorting where warehouse name is the intended display value.

### 4. Server-driven list pipeline

For each backend list handler/query pipeline, verify whether:

* filters are applied before `CountAsync`;
* `TotalCount` is calculated after filtering and before paging;
* sorting is applied before paging;
* sorting is deterministic;
* sorting includes a stable secondary order, usually `ThenBy(x => x.Id)`;
* `Skip` and `Take` are normalized;
* paging is applied after sorting;
* DTO projection is backend-owned;
* projection happens before materialization;
* the result is returned as `ListResult<T>` with page items, filtered total count, normalized skip, and normalized take.

### 5. WebApp grid behavior

For each WebApp server-driven grid, verify whether:

* `MudDataGrid.ServerData` is used for backend-owned lists;
* full backend result sets are not loaded for client-side filtering/paging;
* MudBlazor `GridState` is mapped to a UI-specific grid request;
* the UI-specific grid request is mapped to a shared API request;
* MudBlazor types do not enter `Myrmex.Shared`;
* filter changes reset to first page;
* refresh and successful mutations reload current grid state unless reset is required;
* default sort is explicit and aligned with backend default;
* visible warehouse columns display warehouse name only, not warehouse code.

### 6. Paging and defaults

Identify:

* duplicated numeric constants for `Skip`;
* duplicated numeric constants for `Take`;
* duplicated page size values;
* duplicated max take values;
* duplicated default take values;
* duplicated page size option arrays;
* whether existing `ListQuery.NormalizeSkip` is used consistently;
* whether existing `ListQuery.NormalizeTake` is used consistently.

Do not propose a new abstraction if an existing one already covers the need.

### 7. Tests

For each slice, identify existing tests that protect meaningful regression risks, including:

* filtering;
* count-before-paging behavior;
* paging;
* deterministic sorting;
* projection;
* API binding/serialization;
* API client query string construction;
* cancellation propagation;
* ProblemDetails/error mapping.

Report missing tests only where there is a real regression risk.

Do not propose broad duplicate test matrices across handler, endpoint, and API client layers unless there is a concrete reason.

## Required Report Structure

Create or update:

```text
specs/090-normalize-wms-list-contracts/research.md
```

The report should include:

1. Executive summary.
2. Compact findings table by slice.
3. Detailed findings section by slice.
4. Cross-cutting inconsistencies.
5. Prioritized normalization plan:

   * safe mechanical changes;
   * changes requiring focused tests;
   * deferred changes.
6. Explicit non-goals.
7. Suggested next implementation phases.
8. Risk notes.

The report should be detailed enough to guide implementation, but it should avoid unnecessary verbosity.

## Non-goals

Do not implement normalization during Phase 1.

Do not change:

* production code;
* test code;
* resource files;
* domain model;
* API routes;
* database schema;
* migrations;
* 1C import behavior;
* WebApp UI design beyond documenting current inconsistencies.

Do not:

* remove `Warehouse.Code` from domain, API, database, import, or DTOs;
* introduce new abstractions during the audit;
* redesign list screens;
* run the WebApp;
* start AppHost;
* run Docker or infrastructure commands;
* run migrations;
* run database update;
* execute broad test suites.

If a build or focused test command seems useful, only recommend the command in the report. Do not run it during Phase 1.

## Validation Mode

Phase 1 validation is static repository inspection only.

Allowed:

* inspect source files;
* inspect existing tests;
* inspect Spec Kit memory files;
* inspect existing specifications;
* inspect project structure;
* document concrete findings.

Not allowed:

* modifying source files outside the required Spec Kit output;
* modifying tests;
* modifying resource files;
* modifying project files;
* creating migrations;
* applying migrations;
* starting infrastructure;
* running broad test suites.

## Expected First Agent Action

The agent should first run the Spec Kit specify workflow using this stakeholder brief.

The generated Spec Kit folder should be:

```text
specs/090-normalize-wms-list-contracts/
```

The agent must use the current branch and must not create a new branch.

## Expected Phase 1 Deliverable

After the specify/plan setup, the first concrete deliverable should be:

```text
specs/090-normalize-wms-list-contracts/research.md
```

This document should become the decision base for follow-up implementation tasks.
