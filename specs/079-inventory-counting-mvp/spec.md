# Feature Specification: Inventory Counting MVP

**Feature Branch**: `079-inventory-counting-mvp`

**Created**: 2026-06-24

**Status**: Draft

**Input**: User description: "StakeholderDocs\Wms\Inventory\079 Inventory Counting MVP.md"

## Clarifications

### Session 2026-06-24

- Q: When should an Inventory Count transition from Draft to In Progress? → A: When the first counted quantity is entered.
- Q: Which warehouses may an inventory operator count? → A: Warehouses permitted by existing Myrmex access rules; if warehouse-level access control is unavailable, any active warehouse visible through the existing lookup. No count-specific warehouse permission is introduced.
- Q: How should an operator recover from a Conflict line? → A: Supersede the immutable Conflict line and add a fresh line for the same SKU/location in the same count with a new system quantity and inventory-state snapshot.
- Q: What user identity should the count audit retain? → A: Retain the acting user for creation, count entry, apply, completion, and cancellation, consistent with the audit direction for inventory-changing operations.
- Q: Can an operator remove an incorrectly added count line? → A: Only while it is Pending; Counted, Applied, Conflict, and Superseded lines remain visible as audit evidence.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create a Warehouse Count (Priority: P1)

As a warehouse operator, I want to create an inventory count for one warehouse and add the SKU/location pairs that must be checked so that each physical count starts from a recorded system snapshot.

**Why this priority**: A count session and its captured expectations are the foundation for every later counting, reconciliation, and audit action.

**Independent Test**: Create a count for an active warehouse, add an eligible SKU/location pair, and verify that the line records the current system quantity and is ready for physical counting.

**Acceptance Scenarios**:

1. **Given** an active warehouse visible to the operator under existing Myrmex access rules, **When** the operator creates a count with an optional reason, **Then** a Draft count is created for that warehouse with its creation time and creator identity and can be opened.
2. **Given** a Draft or In Progress count and an active SKU and eligible active location in the count warehouse, **When** the operator adds the pair, **Then** a Pending line captures the current system quantity and current inventory state without changing a Draft count's status.
3. **Given** no inventory balance for the selected SKU/location pair, **When** the operator adds the line, **Then** its system quantity is captured as zero and its expected state records that no balance existed.
4. **Given** a SKU/location pair already has a current line in the count, **When** the operator tries to add it again, **Then** the duplicate is rejected without changing the count.
5. **Given** a missing, inactive, transit, or out-of-warehouse reference, **When** the operator tries to create a count or add a line using it, **Then** the operation is rejected with a clear reason.
6. **Given** an incorrectly added Pending line, **When** the operator removes it, **Then** the line is deleted without affecting inventory or audit records.

---

### User Story 2 - Record a Physical Count (Priority: P1)

As a warehouse operator, I want to enter the physical quantity for a count line and see its variance so that I can identify discrepancies before inventory is changed.

**Why this priority**: Capturing the observed quantity separately from applying a correction is the core distinction between controlled counting and direct adjustment.

**Independent Test**: Enter and revise a non-negative quantity on a Pending line and verify that the displayed variance equals counted quantity minus the captured system quantity.

**Acceptance Scenarios**:

1. **Given** a Pending line with system quantity 10 in a Draft count, **When** the operator enters counted quantity 12, **Then** the line becomes Counted, shows variance +2, records the counter identity, and the count becomes In Progress.
2. **Given** a Counted but unapplied line, **When** the operator changes the counted quantity, **Then** the variance is recalculated from the original system snapshot.
3. **Given** a Pending or Counted line, **When** the operator enters a negative quantity, **Then** the entry is rejected and the line remains unresolved.
4. **Given** an Applied line or a Completed or Cancelled count, **When** the operator attempts to change the counted quantity or comment, **Then** the change is rejected.

---

### User Story 3 - Apply a Count Result (Priority: P1)

As a warehouse operator, I want to apply a counted line so that the recorded inventory agrees with the physical count while preserving a complete audit trail.

**Why this priority**: A count produces business value only when a verified discrepancy can be resolved safely and traceably.

**Independent Test**: Apply one zero-variance line and one non-zero-variance line, then verify the resulting line states, inventory quantities, and audit records.

**Acceptance Scenarios**:

1. **Given** a Counted line whose variance is zero and whose inventory state is unchanged, **When** the operator applies it, **Then** the line becomes Applied and no inventory adjustment or ledger entry is created.
2. **Given** a Counted line with non-zero variance and unchanged inventory state, **When** the operator applies it, **Then** the inventory quantity becomes the counted quantity, exactly one adjustment and one ledger entry record the variance, and the line references the adjustment and records the applier identity.
3. **Given** inventory that has changed since the line snapshot, **When** the operator applies the line, **Then** the apply is rejected as a conflict, the line becomes Conflict, and no inventory, adjustment, or ledger change is made.
4. **Given** no balance existed when the line was added but one exists at apply time, **When** the operator applies the line, **Then** the apply is rejected as a conflict without partial changes.
5. **Given** the same Applied line, **When** an operator repeats the apply action, **Then** no additional inventory or audit record is created.
6. **Given** a line is in Conflict, **When** the operator supersedes it, **Then** the original line becomes immutable Superseded audit evidence and a fresh Pending line for the same SKU/location is added to the same count with a new system quantity and inventory-state snapshot.

---

### User Story 4 - Resolve or Close a Count (Priority: P2)

As a warehouse operator, I want to complete a fully resolved count or cancel an unfinished count so that every count has a clear final state.

**Why this priority**: Final states prevent accidental changes and make the operational meaning of historical count records unambiguous.

**Independent Test**: Complete a count whose lines are all Applied, reject completion of a count with unresolved lines, and cancel a separate unfinished count.

**Acceptance Scenarios**:

1. **Given** a count with one or more lines and every current line Applied, **When** the operator completes it, **Then** the count becomes Completed, records its completion time and completer identity, and becomes read-only.
2. **Given** a count with a Pending, Counted, or non-superseded Conflict line, **When** the operator attempts completion, **Then** completion is rejected and the unresolved lines remain visible.
3. **Given** a count that is not Completed or Cancelled, **When** the operator cancels it, **Then** it becomes Cancelled, records its cancellation time and canceller identity, and becomes read-only.
4. **Given** a count with previously Applied lines, **When** the count is cancelled, **Then** those inventory adjustments remain in effect and remain auditable.

---

### User Story 5 - Review Count History (Priority: P2)

As a warehouse operator, I want to list inventory counts and open their details so that I can monitor progress and audit past counting activity.

**Why this priority**: Operators need visibility into active work and permanent evidence of completed, cancelled, and conflicted counts.

**Independent Test**: Create counts in different states, list them, open each count, and verify that progress totals and line-level snapshot, variance, state, comment, and adjustment information are visible.

**Acceptance Scenarios**:

1. **Given** inventory counts exist, **When** an operator opens the count list, **Then** each result identifies the count, warehouse, status, creation and finalization times, and totals for all, Applied, and unresolved or Conflict lines.
2. **Given** a count exists, **When** the operator opens it, **Then** all lines show SKU, location, system quantity, counted quantity, variance, status, comment, and adjustment reference when present.
3. **Given** a referenced SKU or location was made inactive after a line was added, **When** the operator reviews the count, **Then** the historical line remains understandable and viewable.
4. **Given** a count does not exist, **When** an operator requests its details, **Then** the system reports that the count was not found.

### Edge Cases

- A count cannot be completed with no lines because no physical inventory verification has occurred.
- A storage location that belongs to another warehouse or represents transit inventory is ineligible even when active.
- If an existing balance disappears, appears, or changes after the snapshot, applying the related line results in Conflict.
- A line in Conflict cannot be edited or directly applied again; it may become Superseded when replaced by a fresh line for the same SKU/location pair in the same count.
- A Superseded line remains visible for audit but no longer blocks count completion; its current replacement line must be Applied.
- Only a Pending line may be removed; Counted, Applied, Conflict, and Superseded lines cannot be deleted.
- Counted quantity zero is valid and may reduce an existing balance to zero through an audited adjustment.
- Repeated complete, cancel, count-entry, and apply requests against final or incompatible states do not create duplicate effects.
- Cancelling a count never reverses inventory changes from lines that were already Applied.
- Historical count details remain viewable when current reference data is inactive.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow an operator to create an inventory count for exactly one active warehouse, with an optional reason or description.
- **FR-001a**: Warehouse selection MUST follow existing Myrmex warehouse visibility and access rules; if no warehouse-level restriction exists, every active warehouse returned by the existing warehouse lookup MUST be selectable.
- **FR-002**: Each count MUST have a stable identity, lifecycle status, creation time, latest-change time, creator identity, and completion or cancellation time and acting-user identity when applicable.
- **FR-003**: The system MUST allow an operator to add count lines while a count is Draft or In Progress.
- **FR-004**: Each line MUST identify one active SKU and one eligible storage location whose location and location type are active and that belongs to the count warehouse.
- **FR-005**: Transit storage locations MUST NOT be eligible for count lines in this MVP.
- **FR-006**: The system MUST prevent more than one current line for the same SKU/location pair in a count; a new line for the pair is allowed only when it supersedes an existing Conflict line.
- **FR-007**: When a line is added, the system MUST capture the current system quantity and sufficient inventory-state information to detect any subsequent change before apply.
- **FR-008**: If no balance exists when a line is added, the system quantity MUST be captured as zero and the expected state MUST record that the balance was absent.
- **FR-009**: The system MUST accept non-negative counted quantities and an optional line comment for Pending or Counted lines.
- **FR-010**: The system MUST calculate variance as counted quantity minus the captured system quantity.
- **FR-011**: An operator MUST be able to revise the counted quantity and comment until the line is Applied or the count reaches a final state.
- **FR-012**: The system MUST allow a Counted line to be applied only when the current inventory state matches the line's captured state.
- **FR-013**: Applying a zero-variance line MUST mark it Applied without creating an inventory adjustment or inventory ledger entry.
- **FR-014**: Applying a non-zero-variance line MUST set the inventory balance to the counted quantity through one auditable inventory adjustment, create one corresponding ledger entry, and retain the adjustment reference on the count line.
- **FR-015**: An apply operation MUST either complete all line, inventory, adjustment, and ledger changes together or make none of them.
- **FR-016**: When the current inventory state differs from the captured state, apply MUST make no inventory or audit changes and MUST mark the line Conflict.
- **FR-017**: The system MUST prevent duplicate application of an Applied line.
- **FR-018**: The system MUST allow completion only for a non-empty count whose current lines are all Applied and that has no remaining Conflict lines.
- **FR-019**: The system MUST allow cancellation of a count that is not already Completed or Cancelled.
- **FR-020**: Completion and cancellation MUST make the count and its lines read-only.
- **FR-021**: Cancelling a count MUST NOT reverse adjustments from lines already Applied.
- **FR-022**: The system MUST provide a count list with identity, warehouse, status, creation and finalization times, total line count, Applied line count, unresolved line count, and Conflict line count.
- **FR-023**: The system MUST provide count details containing the count metadata and all line snapshot, counted quantity, variance, status, comment, and applied-adjustment information.
- **FR-024**: Historical count and line details MUST remain viewable and understandable if referenced SKUs, warehouses, or locations later become inactive.
- **FR-025**: The web application MUST provide an Inventory Counts area for listing counts, creating counts, opening details, adding lines, entering counts, applying lines, completing counts, and cancelling counts according to current state.
- **FR-026**: The add-line interaction MUST restrict location choices to eligible locations in the count warehouse and exclude transit locations.
- **FR-027**: The count-entry interaction MUST show the SKU, location, captured system quantity, base unit of measure, counted quantity, calculated variance, and optional comment.
- **FR-028**: After apply, the operator MUST be told whether the result had zero variance, created an adjustment, or encountered a conflict requiring a superseding line with a fresh snapshot.
- **FR-029**: Each line MUST record when its counted quantity was last entered and when it was Applied, when those events occur.
- **FR-030**: The system MUST allow an operator to supersede a Conflict line by adding a fresh Pending line for the same SKU/location pair within the same count.
- **FR-031**: A Superseded line MUST remain immutable and linked to its replacement line for audit.
- **FR-032**: Each count line MUST retain the acting-user identity for its latest count entry and for apply when those actions occur.
- **FR-033**: The system MUST allow an operator to remove an incorrectly added line only while it is Pending.
- **FR-034**: Counted, Applied, Conflict, and Superseded lines MUST NOT be deleted and MUST remain visible for audit.

### Domain Rules *(mandatory when feature changes domain behavior)*

- **DR-001**: An Inventory Count belongs to exactly one warehouse and moves only through Draft, In Progress, Completed, or Cancelled states.
- **DR-002**: Adding count lines does not change a Draft count's status; entering the first counted quantity moves the count to In Progress.
- **DR-003**: An Inventory Count Line moves through Pending, Counted, Applied, Conflict, or Superseded states; Applied and Superseded are final.
- **DR-004**: A Conflict line is immutable and cannot be counted or applied again; when replaced by a fresh Pending line for the same SKU/location pair in the same count, it becomes Superseded.
- **DR-004a**: The superseding line becomes the current line for its SKU/location pair and captures a new system quantity and inventory-state snapshot.
- **DR-005**: System quantity is an immutable snapshot captured when the line is added; editing counted quantity never changes that snapshot.
- **DR-006**: Counted quantity MUST be zero or greater.
- **DR-007**: Variance quantity MUST always equal counted quantity minus system quantity.
- **DR-008**: A non-zero count variance MUST be represented as an inventory adjustment and ledger entry; direct unaudited balance changes are prohibited.
- **DR-009**: A zero count variance resolves the line without creating inventory movement records.
- **DR-010**: Inventory state changes after snapshot invalidate apply, including a changed or missing expected balance and the appearance of a balance that was expected to be absent.
- **DR-011**: Completed and Cancelled counts are immutable but remain available for audit.
- **DR-012**: Count processing does not reserve, freeze, transfer, receive, ship, or otherwise affect inventory availability beyond an Applied variance adjustment.
- **DR-013**: Creation, count entry, apply, completion, and cancellation MUST retain the identity of the acting user as part of the count-process audit.
- **DR-014**: Pending lines are preparation data and may be removed; entering counted quantity establishes permanent counting evidence that cannot be deleted.

### Contract and Boundary Requirements *(mandatory when feature exposes API, client, or UI behavior)*

- **CB-001**: Public operations MUST support listing counts, creating a count, loading count details, adding or removing a Pending line, recording a counted quantity, applying or superseding a line, completing a count, and cancelling a count.
- **CB-002**: Creation, line addition, count entry, line apply, completion, and cancellation MUST be distinct business requests with state-appropriate validation and outcomes.
- **CB-003**: Count list and detail information MUST be supplied as count-specific views owned by the inventory counting capability rather than requiring clients to reconstruct status or progress.
- **CB-004**: Count list results MUST use deterministic ordering, defaulting to newest creation time first with count identity as a tie-breaker.
- **CB-005**: User-facing actions MUST be available only when valid for the current count and line states; the system's lifecycle rules remain authoritative when an interface is displaying stale information.
- **CB-006**: Missing resources, invalid references or quantities, duplicate lines, stale inventory state, and invalid lifecycle transitions MUST produce distinguishable outcomes suitable for clear operator messages.

### Observability & Error Handling *(mandatory when feature exposes runtime behavior)*

- **OE-001**: The system MUST clearly distinguish not-found, validation, duplicate, stale-inventory, already-applied, and final-state failures.
- **OE-002**: Conflict responses MUST tell the operator that inventory changed after the snapshot and offer recovery by superseding the Conflict line with a fresh line in the same count.
- **OE-003**: Operational diagnostics MUST identify the acting user, count, line, warehouse, SKU, location, requested action, outcome, and related adjustment when one is created.
- **OE-004**: Failed apply operations MUST expose enough diagnostic context to confirm that no partial inventory or audit change occurred, without exposing sensitive internal data to operators.

### Key Entities *(include if feature involves data)*

- **Inventory Count**: A warehouse-level physical verification session with identity, warehouse, status, reason, lifecycle timestamps, creator, completer and canceller identities when applicable, and a collection of count lines.
- **Inventory Count Line**: One SKU/location verification within a count, including the immutable system snapshot, expected inventory state, entered count, variance, status, comment, counter and applier identities and times, optional applied-adjustment reference, and optional link to a superseding replacement line.
- **Inventory Balance**: The current quantity for a SKU at a storage location whose state is compared with the count-line snapshot before apply.
- **Inventory Adjustment**: The auditable inventory correction created for one Applied non-zero variance line.
- **Inventory Ledger Entry**: The before-and-after inventory history created by the adjustment, with a quantity delta equal to the line variance.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In an operational usability check, at least 90% of warehouse operators can create a count, add a line, enter a quantity, and reach a clear apply result on their first attempt without assistance.
- **SC-002**: Excluding the physical counting time, an operator can create a count and prepare its first eligible line in under 2 minutes.
- **SC-003**: For 100% of successfully Applied non-zero variance lines, the final inventory quantity equals the counted quantity and exactly one adjustment and one ledger entry record the variance.
- **SC-004**: For 100% of successfully Applied zero-variance lines, the line is resolved and no inventory adjustment or ledger entry is created.
- **SC-005**: In 100% of tested stale-inventory cases, apply reports a conflict and leaves inventory and audit records unchanged.
- **SC-006**: Operators can determine a count's warehouse, lifecycle state, progress, unresolved lines, and adjustment references from the list and detail views without consulting separate inventory records.
- **SC-007**: Completed and Cancelled counts remain fully reviewable, while 100% of attempted modifications to them are rejected.
- **SC-008**: A count list or count detail requested under normal warehouse operating load becomes usable within 2 seconds for at least 95% of requests.

## Assumptions

- Existing user authentication and authorization determine who is a warehouse operator; this MVP introduces no new role model.
- Inventory Counting introduces no count-specific warehouse permission or authorization model.
- Acting-user audit identity follows the existing authenticated user identity representation and establishes the same direction expected for future inventory-changing operations.
- Quantities use the SKU's existing base unit of measure and precision rules.
- A Conflict line is retained for audit and may only be recovered through the limited supersede-and-replace flow defined for this MVP; broader recount and approval workflows remain excluded.
- The MVP uses the existing inventory adjustment and ledger behavior as the authoritative path for non-zero corrections.
- Count identifiers follow existing Myrmex document identity conventions; a separate human-readable numbering scheme is not required unless already standard.
- List paging and optional filtering may follow existing inventory list conventions; the minimum required list content and deterministic ordering are defined here.
- UTC is the authoritative basis for recorded lifecycle timestamps, while presentation may use the user's display conventions.
- Mobile, scanner-assisted counting, blind counts, recount approvals, inventory freeze, reservation changes, lot/serial/expiry/LPN counting, count waves, automatic task generation, multi-operator assignment, external integrations, dashboards, valuation, printing, and import/export are outside this MVP.
- This feature does not create transfer, manual-move, receiving, shipping, or inter-warehouse artifacts.
