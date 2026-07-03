# Feature Specification: Server-Driven WMS Catalog and Topology Lists

**Feature Branch**: `092-server-driven-wms-lists`

**Created**: 2026-07-03

**Status**: Draft

**Input**: Stakeholder description: `StakeholderDocs/092 Server-Driven WMS Catalog and Topology Lists.md`

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse the Complete Catalog (Priority: P1)

A warehouse user can browse, search, sort, and page through all Stock Keeping Units and Units of Measure, including records beyond any initial page-sized subset.

**Why this priority**: SKU volumes already reach tens of thousands of records. Missing catalog records directly prevents users from finding and managing valid reference data.

**Independent Test**: Populate the catalog with more records than one page can hold, including matches outside the first page, then verify that paging, searching, and sorting expose the complete filtered result set and its correct total.

**Acceptance Scenarios**:

1. **Given** more SKUs exist than fit on one page, **When** a user advances through pages, **Then** every matching SKU is reachable without duplicates or omissions and the displayed total represents the complete filtered dataset.
2. **Given** a matching SKU is outside the first page under the default order, **When** a user searches by an existing code, name, or description, **Then** the SKU is included in the results.
3. **Given** multiple pages of SKUs or Units of Measure, **When** a user changes a supported sort column or direction, **Then** the complete filtered dataset is reordered before the requested page is displayed.
4. **Given** a user changes the search text or inactive-record filter, **When** the updated results load, **Then** the list starts from the first page and shows the total for the new filter.

---

### User Story 2 - Browse and Filter the Complete Topology (Priority: P2)

A warehouse user can browse, search, sort, and page through Warehouses, Zones, and Storage Locations across the complete eligible dataset. Storage Location type and status filters describe the whole filtered result set rather than only the visible page.

**Why this priority**: Correct topology selection and maintenance depend on complete lists, and page-local filtering can present an operationally misleading view of available locations.

**Independent Test**: Populate each topology list with multiple pages, apply supported search and filter combinations, and verify the returned rows and total count against the complete eligible dataset.

**Acceptance Scenarios**:

1. **Given** more Warehouses or Zones exist than fit on one page, **When** a user pages, searches, or sorts the list, **Then** the operation applies to the complete eligible dataset and the correct total is displayed.
2. **Given** a warehouse is selected and its Storage Locations span multiple pages, **When** a user filters by location type or status, **Then** all matching locations are eligible for paging and the total reflects those filters.
3. **Given** no warehouse is selected on the main Storage Locations page, **When** the page is viewed, **Then** it does not load an unrestricted cross-warehouse Storage Location list.
4. **Given** records share the selected primary sort value, **When** a user revisits or advances through pages, **Then** their order remains stable and no record is duplicated or omitted at page boundaries.

---

### User Story 3 - Select a Warehouse Without Preloading All Warehouses (Priority: P3)

A warehouse user can find and select a Warehouse on the Zones and Storage Locations pages without the browser first loading a fixed subset of Warehouses.

**Why this priority**: A fixed warehouse preload reproduces the same completeness defect in selectors and can make valid Warehouses impossible to choose.

**Independent Test**: Create more Warehouses than the selector's bounded result size, search for a Warehouse outside the initial ordered subset, and verify that it can be found and selected on both affected pages.

**Acceptance Scenarios**:

1. **Given** the desired Warehouse is outside the selector's initial bounded results, **When** a user searches using the existing Warehouse search semantics, **Then** the Warehouse appears as a selectable result.
2. **Given** a user rapidly changes Warehouse search text, **When** an obsolete request is cancelled, **Then** no cancellation error is shown and the latest search determines the visible options.
3. **Given** more Warehouses match than the bounded selector can display, **When** results are returned, **Then** they use a stable order and remain bounded without loading all Warehouses into the browser.

---

### User Story 4 - Continue Managing Reference Data (Priority: P4)

A warehouse user can create, edit, deactivate, and reactivate affected reference data while the current list refreshes from the authoritative dataset and retains established routes, messages, and business behavior.

**Why this priority**: The list migration must not make existing maintenance workflows stale or regress their established behavior.

**Independent Test**: Perform each existing mutation from an affected page and verify that the refreshed list and total reflect the change while expected cancellations remain silent and actual failures remain visible.

**Acceptance Scenarios**:

1. **Given** an affected list is displaying server-filtered data, **When** a create, edit, deactivate, or reactivate operation succeeds, **Then** the current list reloads from the authoritative dataset and reflects the mutation.
2. **Given** a list or lookup request is superseded by a newer request, **When** the earlier request is cancelled, **Then** the user does not see an error for the expected cancellation.
3. **Given** a genuine list or lookup failure occurs, **When** the request completes unsuccessfully, **Then** the existing user-facing error convention presents the failure.

### Edge Cases

- A filtered result contains zero records: the page shows an empty result and a total of zero without retaining rows from the previous request.
- A requested page becomes empty after a mutation or filter change: the user is returned to a valid page and sees current results.
- Multiple records have identical values for the selected sort field: a stable secondary order prevents duplicates or omissions across page boundaries.
- Paging inputs are absent or outside supported bounds: safe defaults and limits are applied consistently and reported in the result metadata.
- Search text is blank or contains surrounding whitespace: existing slice-specific search semantics are preserved and applied consistently.
- The inactive-record filter changes while the user is on a later page: the list resets to the first page before showing the new result set.
- The selected Warehouse has no Zones or Storage Locations: the dependent list displays an empty result rather than data from another Warehouse.
- Storage Location type or status filters are combined with Warehouse, Zone, search, or inactive filters: the total and rows reflect the complete intersection of all selected filters.
- A user changes page, sort, or filter values before a prior load completes: only the latest request controls the displayed rows, total, and errors.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide server-driven paging, filtering, sorting, and total counts for the Stock Keeping Unit, Unit of Measure, Warehouse, Zone, and Storage Location list pages.
- **FR-002**: Each affected list MUST apply all active filters to the complete eligible dataset before calculating the total count.
- **FR-003**: Each affected list MUST calculate the total count before selecting the requested page.
- **FR-004**: Each affected list MUST apply a deterministic supported order before selecting the requested page, including a stable tie-breaker when primary sort values match.
- **FR-005**: Search MUST preserve each slice's existing matching semantics and fields: SKU code, name, and description; Unit of Measure code, name, and currently supported symbol; and Warehouse, Zone, and Storage Location code, name, and description.
- **FR-006**: The affected pages MUST request new authoritative results when page, page size, sort column, sort direction, search text, or applicable filter values change.
- **FR-007**: The affected pages MUST display the total number of records matching all active filters, not merely the number on the current page.
- **FR-008**: Search and filter changes MUST reset the affected list to its first page before results are displayed.
- **FR-009**: Successful create, edit, deactivate, and reactivate operations MUST refresh the affected authoritative list without reverting to a stale browser-held collection.
- **FR-010**: The Storage Location list MUST support Warehouse, Zone, Storage Location Type, Storage Location Status, search text, inactive-record, paging, and sorting criteria across the complete eligible dataset.
- **FR-011**: The main Storage Location page MUST NOT request an unrestricted cross-warehouse location list when no Warehouse is selected.
- **FR-012**: Storage Location Type and Status filters MUST be applied before total count and paging, including when combined with other supported filters.
- **FR-013**: The Zones and Storage Locations pages MUST provide bounded, searchable Warehouse selection using existing Warehouse display and search semantics.
- **FR-014**: Warehouse selection results MUST use deterministic ordering, support cancellation, and avoid requiring all Warehouses to be loaded into the browser.
- **FR-015**: The Warehouse list page MUST use the same complete-dataset list behavior as the other affected pages; selector behavior MUST remain a separate bounded lookup experience.
- **FR-016**: The system MUST preserve existing routes, localized labels and messages, domain rules, validation behavior, search meaning, and create/update/activation behavior.
- **FR-017**: The feature MUST NOT change database schema, import behavior, warehouse-code meaning, domain entities, or domain validation rules.
- **FR-018**: The feature MUST NOT introduce full-text, fuzzy, or relevance-ranked search; a generic list or lookup framework; unrelated Inventory workflow changes; or a visual redesign.
- **FR-019**: Server-driven Zone selection on the Storage Locations page is excluded unless planning demonstrates that it is necessary to safely provide the required Storage Location filtering; otherwise the existing limitation MUST be recorded as deferred follow-up work.

### Contract and Boundary Requirements *(mandatory when feature exposes API, client, or UI behavior)*

- **CB-001**: Public request and response contracts crossing the backend/client boundary for the five affected lists MUST be shared by both sides and owned by the responsible Catalog or Topology capability.
- **CB-002**: Each affected list MUST have an explicit feature-specific public request containing only its supported paging, search, sorting, inactive-record, and slice-specific filter values.
- **CB-003**: Public response shapes MUST retain the established `StockKeepingUnitDetails`, `UnitOfMeasureDetails`, `WarehouseDetails`, `ZoneDetails`, and `StorageLocationDetails` concepts unless planning establishes a genuine difference between list and detail data.
- **CB-004**: Duplicate browser-local declarations of affected public request, response, and mutation contracts MUST be removed once the shared contract is authoritative.
- **CB-005**: Public sort keys MUST be explicit, use PascalCase values, and cover at minimum: Code, Name, CreatedAtUtc, UpdatedAtUtc, and IsActive for SKUs and Units of Measure; Name, CreatedAtUtc, UpdatedAtUtc, and user-visible IsActive for Warehouses; Code, Name, CreatedAtUtc, UpdatedAtUtc, and IsActive for Zones; and Code, Name, IsPickable, CreatedAtUtc, UpdatedAtUtc, and IsActive for Storage Locations.
- **CB-006**: Existing route paths MUST remain unchanged, and each public list request MUST be mapped to a separate explicit internal query owned by its Catalog or Topology slice.
- **CB-007**: Filtering, count calculation, deterministic ordering, paging, and response projection MUST remain backend-owned and occur in that order.
- **CB-008**: Each list result MUST contain the page items, full filtered total count, and normalized paging values.
- **CB-009**: Shared contracts MUST NOT contain domain entities, persistence expressions, internal handlers or queries, infrastructure dependencies, browser grid state, or user-interface framework types.
- **CB-010**: Catalog MUST own SKU and Unit of Measure list contracts and behavior; Topology MUST own Warehouse list and lookup, Zone list, Storage Location list, and Storage Location type/status filtering; Inventory MUST NOT proxy these requests.
- **CB-011**: Public list contracts and bounded lookup contracts MUST remain distinct; the feature MUST NOT introduce a universal list or lookup abstraction.
- **CB-012**: Cancellation MUST propagate across the list and Warehouse lookup boundaries so obsolete work can stop without being reported as a user-facing failure.

### Observability & Error Handling *(mandatory when feature exposes runtime behavior)*

- **OE-001**: Expected cancellation caused by superseded list or lookup requests MUST NOT be presented as a user-facing error.
- **OE-002**: Timeouts, unavailable services, invalid requests, and other genuine failures MUST remain visible through the existing error conventions with enough context to identify the affected operation.
- **OE-003**: Operational diagnostics MUST distinguish successful, cancelled, and failed list or lookup requests without exposing sensitive record data.

### Key Entities *(include if feature involves data)*

- **Stock Keeping Unit**: Catalog reference data identified and searched through its code, name, and description, with lifecycle and audit attributes available for supported sorting.
- **Unit of Measure**: Catalog reference data identified and searched through its code, name, and currently supported symbol, with lifecycle and audit attributes available for supported sorting.
- **Warehouse**: Topology reference data used both as a managed list record and as a parent selection for Zones and Storage Locations.
- **Zone**: Warehouse-owned topology reference data used as a managed list record and an optional Storage Location filter.
- **Storage Location**: Warehouse topology reference data associated with a Warehouse and relevant Zone, Type, Status, pickability, lifecycle, and audit attributes.
- **Paged List Request**: The user-selected page, page size, search text, sort choice, direction, inactive-record choice, and slice-specific filters for one affected list.
- **Paged List Result**: The authoritative page items, complete filtered total, and normalized paging values returned for an affected list request.
- **Warehouse Lookup Result**: A bounded, deterministically ordered set of Warehouses matching selector search text.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In a representative dataset containing at least 35,000 SKUs, users can find an existing SKU by supported search fields regardless of its position in the default ordering, with 100% of expected matches eligible for retrieval.
- **SC-002**: Across all five affected lists, traversing consecutive pages under unchanged filters and sorting yields every expected record exactly once, including when primary sort values are duplicated.
- **SC-003**: For every tested combination of supported filters, the displayed total equals the number of records in the complete filtered dataset in 100% of acceptance cases.
- **SC-004**: At least 95% of page, sort, and filter interactions on representative datasets of up to 50,000 records display the resulting page within 2 seconds under the agreed validation environment.
- **SC-005**: A user can locate and select a Warehouse that is outside the initial bounded selector results on both the Zones and Storage Locations pages in under 10 seconds.
- **SC-006**: In acceptance testing, at least 90% of representative warehouse users complete a browse-search-sort-page task on their first attempt without encountering missing records or misleading totals.
- **SC-007**: All existing create, edit, deactivate, and reactivate acceptance scenarios for the affected records continue to pass, and a successful mutation is reflected by the refreshed list within 2 seconds in at least 95% of trials.
- **SC-008**: Expected request cancellation produces zero user-facing error notifications, while 100% of simulated genuine request failures remain visibly reported through existing conventions.
- **SC-009**: Existing route, domain validation, schema, and import compatibility checks report zero intentional behavioral changes outside the defined list and Warehouse lookup scope.

## Assumptions

- Existing authorization and access rules remain authoritative; this feature does not add roles or change record visibility.
- Existing slice-specific search behavior is the baseline for compatibility, including Unit of Measure symbol matching only where it already applies.
- Current paging defaults and maximum page-size limits remain valid unless planning identifies a documented compatibility or safety issue.
- The representative performance environment and dataset will be agreed during planning so the response-time outcomes are reproducible.
- The existing Warehouse display value is sufficient for selector results; this feature does not redesign Warehouse identity or codes.
- The current Zone selector on the Storage Locations page can remain as-is unless planning proves it prevents safe completion of server-side Storage Location filtering.
- Existing Inventory consumers may require namespace or contract-reference updates when public data shapes move, but unrelated Inventory behavior remains out of scope.
- No persistence migration, new search infrastructure, or external service is required.

