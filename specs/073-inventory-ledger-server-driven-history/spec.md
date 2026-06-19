# Feature Specification: Inventory Ledger Server-Driven History

**Feature Branch**: `073-inventory-ledger-server-driven-history`

**Created**: 2026-06-19

**Status**: Draft

**Input**: User description: `Inventory Ledger server-driven history using StakeholderDocs/Wms/Inventory/073 Inventory Ledger server-driven history.md`

## Clarifications

### Session 2026-06-19

- Confirmed stakeholder decision: Inventory Ledger history is read-only and must not introduce create, update, delete, correction, reversal, transfer, InventoryAccount, export, analytics, or generic framework behavior.
- Confirmed stakeholder decision: The primary list is entry-oriented: one row represents one immutable `InventoryLedgerEntry` enriched with its parent `InventoryTransaction` context.
- Confirmed stakeholder decision: Transaction details are transaction-oriented and must support all ledger entries belonging to a transaction, even though current adjustment transactions contain one entry.
- Confirmed stakeholder decision: Server-side filtering, sorting, paging, filtered total count, and deterministic ordering are required for the history list.
- Confirmed stakeholder decision: Filters include SKU, warehouse, storage location, transaction type, and occurrence range.
- Confirmed stakeholder decision: Inventory Balance rows must provide navigation to filtered history for the balance's SKU and storage location.
- Confirmed stakeholder decision: Inactive historical SKU, warehouse, storage-location, and UoM references remain visible in history and searchable for history filtering.
- Repository-specific decision: The user-facing area and page should be named `Inventory Ledger`; public list semantics should use ledger-entry language rather than implying every row is a complete transaction.
- Repository-specific decision: Navigation from Inventory Balance should use a dedicated Ledger page with query parameters for SKU and storage location, making filtered history linkable and browser-navigation friendly.
- Repository-specific decision: Transaction details should open from the Ledger list as a dialog for this MVP, while the details data shape must still support multiple entries.
- Repository-specific decision: Current Blazor pages display timestamp values as UTC-labeled values, so this feature should display `OccurredAtUtc` and `CreatedAtUtc` as UTC values with clear UTC labels.
- Repository-specific decision: Occurrence range filters use exact UTC `DateTimeOffset` boundaries with inclusive lower bound and exclusive upper bound.
- Repository-specific decision: Existing SKU and storage-location lookup behavior can include inactive records when `SelectableOnly` is false; Inventory Ledger filtering should use that behavior or a small history-specific equivalent if planning discovers a gap.
- Q: What should the initial Inventory Ledger page load when opened without navigation/query filters? → A: Initial Ledger page loads unfiltered, newest-first, with paging.
- Repository-specific finding: The current Inventory navigation contains only Inventory Balances and has no Ledger page.
- Repository-specific finding: Current Inventory API/client behavior has balance list/get and adjustment mutation operations, but no ledger read list or transaction details operations.
- Repository-specific finding: The current Inventory Balance warehouse filter loads active warehouses only. Ledger history must not hide inactive historical warehouse references, so planning must account for a history-appropriate warehouse selector/list behavior.
- Repository-specific finding: Current InventoryTransaction and InventoryLedgerEntry write model records adjustment transactions and immutable before/delta/after quantities; this feature exposes that existing history without changing it.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse Inventory Ledger History (Priority: P1)

A warehouse operator or administrator opens Inventory Ledger and sees a server-driven history of inventory quantity changes, with each row showing one ledger entry and enough transaction context to understand what changed.

**Why this priority**: This is the core audit value. Users need to inspect why current stock changed without direct database access.

**Independent Test**: Can be fully tested by opening Inventory Ledger with existing adjustment history and confirming the first page shows entry rows with occurrence time, transaction type, SKU, warehouse, storage location, before quantity, delta, after quantity, and reason.

**Acceptance Scenarios**:

1. **Given** adjustment ledger entries exist for multiple SKUs and locations, **When** a user opens Inventory Ledger, **Then** the page shows a paged list where each row represents one ledger entry with transaction context and before/delta/after quantities.
2. **Given** a ledger entry has a positive or negative delta, **When** the row is displayed, **Then** the delta preserves its sign and the row also shows the persisted before and after quantities.
3. **Given** no ledger entries match the current filters, **When** the list loads, **Then** the page shows an empty result with total count zero and does not treat the result as an error.
4. **Given** a reason is longer than the grid can comfortably show, **When** the row is displayed, **Then** the list may shorten the visible reason while transaction details still show the full persisted reason.

---

### User Story 2 - Filter and Sort Ledger History (Priority: P2)

A warehouse operator or administrator narrows history to the SKU, warehouse, storage location, transaction type, and occurrence period being investigated, while paging and sorting remain server-driven.

**Why this priority**: Audit history is useful only when users can reduce it to the operational question they are answering.

**Independent Test**: Can be fully tested by applying each supported filter and sort to a known ledger dataset, then confirming the results, total count, ordering, and page contents are produced by the server.

**Acceptance Scenarios**:

1. **Given** history exists for multiple SKUs, **When** a user filters by one SKU, **Then** the list shows only entries for that SKU and the total count reflects the filtered result before paging.
2. **Given** history exists across warehouses and storage locations, **When** a user filters by warehouse, **Then** the list shows only entries whose storage location belongs to that warehouse.
3. **Given** a warehouse is selected, **When** the user searches storage locations for the storage-location filter, **Then** lookup results are scoped to the selected warehouse.
4. **Given** a storage location is selected, **When** the user changes to an incompatible warehouse, **Then** the storage-location selection is cleared before the history reloads.
5. **Given** history exists across transaction types, **When** a user filters by transaction type, **Then** the list shows only matching transaction types. For the current MVP, `Adjustment` is the only selectable type.
6. **Given** history exists across occurrence times, **When** a user applies an occurrence range, **Then** entries are included when `OccurredAtUtc` is at or after the lower bound and before the upper bound.
7. **Given** two or more entries have the same primary sort value, **When** the user pages through sorted results, **Then** the entry order remains stable and deterministic across repeated requests.

---

### User Story 3 - Inspect Transaction Details (Priority: P3)

A warehouse operator or administrator opens a ledger row to inspect the complete transaction, including all entries recorded under that transaction.

**Why this priority**: A list row answers most adjustment questions, but transaction-level detail is needed for the full immutable record and future multi-entry transactions.

**Independent Test**: Can be fully tested by opening details for an adjustment transaction and for a transaction fixture containing multiple entries, then confirming the transaction header and every entry are visible.

**Acceptance Scenarios**:

1. **Given** a ledger row is visible, **When** a user opens transaction details, **Then** the details show transaction ID, transaction type, reason, occurrence time, creation time, and all ledger entries for the transaction.
2. **Given** a transaction contains one adjustment entry, **When** details are opened, **Then** the entry shows SKU, base UoM, warehouse, storage location, balance before, quantity delta, and balance after.
3. **Given** a transaction contains multiple ledger entries, **When** details are opened, **Then** all entries are shown in a deterministic order without collapsing them into one row.
4. **Given** a transaction no longer exists or cannot be found, **When** details are requested, **Then** the user receives the existing not-found style and no mutation action is offered.

---

### User Story 4 - Open Filtered History from Inventory Balance (Priority: P4)

A warehouse operator or administrator starts from a current Inventory Balance row and opens the ledger already filtered to that balance's SKU and storage location.

**Why this priority**: Users investigating a current quantity need a direct path from the current snapshot to its audit trail.

**Independent Test**: Can be fully tested by opening history from an Inventory Balance row and confirming the Ledger page loads with SKU and storage-location filters active and editable.

**Acceptance Scenarios**:

1. **Given** an Inventory Balance row is visible, **When** the user chooses its history action, **Then** the Inventory Ledger page opens with that row's SKU and storage location filters applied.
2. **Given** the Ledger page was opened from a balance, **When** the page loads, **Then** it clearly shows the active SKU and storage-location filters.
3. **Given** a user opened filtered history from a balance, **When** the user clears or changes filters, **Then** the page continues as normal Inventory Ledger browsing.
4. **Given** a filtered history link is copied or reopened, **When** the page loads, **Then** the same SKU and storage-location filters are applied from the link.

### Edge Cases

- Ledger list results can be empty; this returns an empty item list and total count zero, not not-found.
- Transaction details for a missing transaction return not-found using current Myrmex conventions.
- Unsupported transaction type filter values are rejected using current validation conventions.
- Malformed identifier query parameters are rejected through normal request binding and validation behavior.
- Invalid occurrence range where the lower bound is after the upper bound is rejected with clear validation feedback.
- Inactive SKUs, warehouses, storage locations, storage-location type/status records, and UoMs referenced by history remain visible in list rows and details.
- Historical lookup/filtering must not silently hide inactive referenced records that still have ledger history.
- If a referenced record is unexpectedly missing despite restrictive relationships, the failure must be visible rather than fabricating incomplete history.
- Paging is reset to the first page when filters change.
- Rapid filter, lookup, or grid changes may cancel prior requests; expected cancellation is not shown as a user-facing error.
- Current InventoryBalance rows are not required for ledger history to remain available.
- The UI must not provide edit, delete, correction, reversal, transfer, export, or analytics actions from list or details.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST expose read-only Inventory Ledger history to warehouse operators and administrators.
- **FR-002**: System MUST list history as ledger-entry rows enriched with parent transaction context.
- **FR-003**: Each list row MUST show occurrence time, transaction type, SKU code and name, warehouse code and name, storage-location code and name, balance before, quantity delta, balance after, and reason.
- **FR-004**: System MUST preserve persisted `BalanceBefore`, `QuantityDelta`, and `BalanceAfter` values from the ledger entry and MUST NOT recalculate them from current balance rows.
- **FR-005**: System MUST support server-side filtering by SKU, warehouse, storage location, transaction type, and occurrence range.
- **FR-006**: System MUST apply filters before calculating total count, sorting, paging, and projection.
- **FR-007**: System MUST support server-side sorting for occurrence time, transaction type, SKU code, SKU name, warehouse code, warehouse name, storage-location code, balance before, quantity delta, balance after, and reason.
- **FR-008**: System MUST apply deterministic ordering for default sorting and every supported requested sort.
- **FR-009**: Default ordering MUST show newest occurrences first, then use stable transaction and entry tie-breakers.
- **FR-010**: System MUST support server-side paging and MUST NOT rely on client-side paging over a fully loaded history dataset.
- **FR-011**: System MUST calculate `TotalCount` after filters and before paging.
- **FR-012**: When opened without navigation or query filters, the Inventory Ledger page MUST load unfiltered history using default newest-first ordering and server-side paging.
- **FR-013**: System MUST use exact SKU and storage-location identity filters.
- **FR-014**: Warehouse filtering MUST include entries through the ledger entry's storage-location warehouse relationship.
- **FR-015**: Occurrence range filtering MUST use `OccurredAtUtc` with an inclusive lower bound and exclusive upper bound.
- **FR-016**: Occurrence range validation MUST reject a lower bound later than the upper bound.
- **FR-017**: SKU and storage-location filter selection MUST support inactive references that have historical ledger entries.
- **FR-018**: Storage-location lookup MUST be warehouse-scoped when a warehouse filter is selected.
- **FR-019**: Changing the selected warehouse MUST clear any selected storage location that does not belong to the new warehouse.
- **FR-020**: Transaction type filtering MUST support `Adjustment` for this MVP and must not block adding future transaction types to the read model.
- **FR-021**: Users MUST be able to open transaction details from a ledger list row.
- **FR-022**: Transaction details MUST show transaction ID, transaction type, reason, `OccurredAtUtc`, `CreatedAtUtc`, and all ledger entries belonging to the transaction.
- **FR-023**: Transaction details MUST show each entry's SKU, base UoM, warehouse, storage location, balance before, quantity delta, and balance after.
- **FR-024**: Transaction details MUST support multiple entries for a transaction, even though current adjustment transactions have one entry.
- **FR-025**: Inventory Balance rows MUST provide a history action that opens Inventory Ledger filtered by the row's SKU and storage location.
- **FR-026**: Inventory Ledger opened from Inventory Balance MUST clearly show active filters and allow users to clear or change them.
- **FR-027**: Inventory Ledger history MUST remain available independently of current InventoryBalance snapshot rows.
- **FR-028**: System MUST resolve current reference labels for SKU, UoM, warehouse, and storage location for this MVP; it MUST NOT introduce historical name snapshots.
- **FR-029**: System MUST NOT introduce any ledger mutation operation, including create, update, delete, correction, reversal, or rebuild.
- **FR-030**: System MUST NOT introduce Inventory Transfer, receiving, picking, shipping, returns, reservations, InventoryAccount, transit inventory, LPN or handling units, lot/batch/serial/expiry history, cycle-count workflow, export, dashboards, analytics, user/actor identity, event sourcing, or generic reporting/lookup/grid frameworks.
- **FR-031**: System MUST preserve current Inventory Adjustment write behavior and current Inventory Balance behavior except for adding read-only navigation to history.

### Domain Rules *(mandatory when feature changes domain behavior)*

- **DR-001**: `InventoryTransaction` represents one completed inventory operation.
- **DR-002**: `InventoryLedgerEntry` represents one immutable quantity change within a transaction.
- **DR-003**: A transaction may contain one or more ledger entries; list rows are entries and transaction details group entries by transaction.
- **DR-004**: Every ledger entry must preserve the invariant `BalanceAfter = BalanceBefore + QuantityDelta`.
- **DR-005**: Ledger transactions and entries are immutable historical records. Incorrect history is corrected by later business operations, not by editing ledger records in this feature.
- **DR-006**: Current `InventoryBalance` is a materialized snapshot and is not the source of historical truth.
- **DR-007**: For this MVP, `Adjustment` is the only currently recorded transaction type.
- **DR-008**: Historical records remain visible when related reference data later becomes inactive.
- **DR-009**: Historical reference labels reflect current reference data for this MVP; historical snapshots of labels are out of scope.

### Contract and Boundary Requirements *(mandatory when feature exposes API, client, or UI behavior)*

- **CB-001**: Public list request behavior MUST include skip, take, one active sort key, sort direction, SKU ID, warehouse ID, storage-location ID, transaction type, occurrence-from UTC, and occurrence-to UTC.
- **CB-002**: Public list response behavior MUST return a shared paged result with page items, filtered total count, normalized skip, and normalized take.
- **CB-003**: Public list item behavior MUST represent a ledger entry plus parent transaction context and MUST NOT expose domain entities.
- **CB-004**: Public transaction details behavior MUST represent one transaction plus a collection of entry details.
- **CB-005**: Internal queries, handlers, persistence projections, and domain entities MUST remain inside the owning WMS module boundary.
- **CB-006**: Backend-owned projections MUST return only data required for the list or details view.
- **CB-007**: Public sort keys MUST be explicit Inventory Ledger contract values and must map to supported business fields only.
- **CB-008**: Unsupported sort keys MUST follow the established Inventory Balance server-driven list behavior and still produce deterministic ordering.
- **CB-009**: Inventory Ledger page state MUST support query parameters for SKU and storage location so balance-to-history navigation is linkable.
- **CB-010**: Expected request cancellation from server-driven list or lookup interactions MUST propagate and must not be shown as an error.

### Observability & Error Handling *(mandatory when feature exposes runtime behavior)*

- **OE-001**: System MUST provide clear validation feedback for unsupported transaction type, invalid occurrence range, invalid identifiers, and unsupported sort inputs.
- **OE-002**: System MUST return not-found feedback when transaction details are requested for a transaction that does not exist.
- **OE-003**: System MUST distinguish empty filtered history from failed history loading.
- **OE-004**: Operational failures in list and details reads MUST be diagnosable through existing Myrmex route, error, and logging conventions.
- **OE-005**: System MUST NOT log full free-text reason values as structured operational metadata unless an existing Myrmex policy explicitly permits it.

### Current Production Behavior to Add or Reconcile

- Current Inventory navigation has an Inventory Balances link but no Inventory Ledger link.
- Current Inventory Balance rows have adjustment actions but no history action.
- Current Inventory API and WebApp client expose balance list/get and adjustment mutation behavior but no ledger read list or transaction details behavior.
- Current Inventory Balance filter UI uses active warehouse loading; Inventory Ledger filtering must account for inactive historical warehouse references.
- Current SKU and storage-location lookup behavior can include inactive references when configured for non-selectable history lookup, and Ledger filtering should preserve that behavior.
- Current Blazor timestamp displays are UTC-oriented; Ledger timestamps should use clear UTC labels unless a later product-wide time-display convention changes.

### Key Entities *(include if feature involves data)*

- **InventoryTransaction**: One completed inventory operation with transaction type, reason, occurrence time, creation time, and ledger entries.
- **InventoryLedgerEntry**: One immutable quantity change with SKU, storage location, quantity delta, balance before, and balance after.
- **InventoryBalance**: Current materialized quantity snapshot for one SKU at one storage location; provides navigation context but is not required for history availability.
- **StockKeepingUnit**: Catalog item referenced by ledger entries and used for filtering/display, including inactive historical references.
- **UnitOfMeasure**: SKU base unit context displayed with transaction entry details.
- **Warehouse**: Topology context derived through storage location and used for filtering/display, including inactive historical references.
- **StorageLocation**: Physical location referenced by ledger entries and used for filtering/display, including inactive historical references.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can open Inventory Ledger from application navigation in no more than 3 interactions.
- **SC-002**: 100% of visible list rows show occurrence time, transaction type, SKU, warehouse, storage location, before quantity, delta, after quantity, and reason.
- **SC-003**: Users can filter ledger history by SKU, warehouse, storage location, transaction type, and occurrence range and see matching results after each filter change.
- **SC-004**: 100% of list requests calculate total count after filtering and before paging.
- **SC-005**: Repeating the same paged, sorted ledger request returns the same row order when underlying history has not changed.
- **SC-006**: Users can open transaction details from a list row in under 10 seconds and see the transaction header plus all entries.
- **SC-007**: 100% of transaction details responses support multiple entries without changing the public details shape.
- **SC-008**: Users can open filtered history from an Inventory Balance row in one action.
- **SC-009**: 100% of balance-to-history navigations apply the originating SKU and storage-location filters and display those filters as active.
- **SC-010**: 100% of inactive referenced SKUs, warehouses, storage locations, and UoMs that still have ledger history remain visible in returned history.
- **SC-011**: Empty matching history returns an empty list and total count zero rather than not-found.
- **SC-012**: The delivered feature exposes no ledger mutation, transfer, InventoryAccount, export, analytics, or generic framework behavior.

## Assumptions

- Existing Myrmex authentication, authorization, validation, result, error, and diagnostics conventions apply.
- Existing Inventory Adjustment write behavior is available and remains the source of ledger transactions and entries.
- Existing Inventory Balance list behavior remains the current-stock view and provides the starting point for filtered history navigation.
- Existing shared list result and server-driven grid conventions remain the default for list behavior.
- Existing SKU and storage-location lookup contracts can be used with inactive-inclusive behavior for history filtering, or planning may define a small history-specific lookup behavior if needed.
- Existing database relationships normally prevent missing referenced SKU, storage-location, warehouse, and UoM records for ledger entries.
- Manual UI smoke validation is acceptable for the Ledger page and transaction details unless planning identifies a new UI automation risk.

## Decisions Required Before `/speckit-plan`

- None. The stakeholder document plus repository inspection resolved the open specification questions for naming, list granularity, navigation, date/time behavior, inactive lookups, sort coverage, and details presentation.
