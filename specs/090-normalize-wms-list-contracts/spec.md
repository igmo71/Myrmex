# Feature Specification: Normalize WMS List Conventions

**Feature Branch**: `090-normalize-wms-list-contracts`

**Created**: 2026-07-02

**Status**: Draft

**Input**: User description: `StakeholderDocs/090 Normalize WMS List Contracts, Sorting, Paging, and Grid Conventions.md`

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Understand Current List-Slice Consistency (Priority: P1)

As a maintainer, I need a verified inventory of how each in-scope WMS list handles public contracts, sorting, paging, projection, client integration, and user-facing grid behavior so that normalization decisions are based on repository evidence rather than assumptions.

**Why this priority**: The initiative cannot safely prioritize or implement changes until the current behavior and ownership boundaries are known for every in-scope slice.

**Independent Test**: Review the audit report and confirm that each of the nine named slices has precise file references and findings for every applicable audit category.

**Acceptance Scenarios**:

1. **Given** the current repository and durable list-slice conventions, **When** the audit is completed, **Then** Warehouses, Zones, Storage Locations, SKUs, UoM, Inventory Balances, Inventory Ledger, Inventory Transfers, and Inventory Counts each have a compact summary and detailed evidence-backed findings.
2. **Given** a finding about contract ownership, sort behavior, paging, projection, grid behavior, or tests, **When** a reviewer examines the report, **Then** the reviewer can trace the finding to precise repository paths.
3. **Given** an in-scope slice with no applicable component or test at a reviewed boundary, **When** the report records that boundary, **Then** it distinguishes confirmed absence from an unreviewed area.

---

### User Story 2 - Prioritize Safe Normalization Work (Priority: P2)

As a technical lead, I need inconsistencies grouped by risk and implementation dependency so that the team can schedule mechanical cleanup separately from behavior-sensitive changes and deliberate deferrals.

**Why this priority**: A raw discrepancy list does not provide enough information to sequence work without creating regressions or unnecessary abstractions.

**Independent Test**: Use only the report to assign each identified normalization candidate to safe mechanical work, focused-test work, or deferred work, and identify the recommended implementation phases.

**Acceptance Scenarios**:

1. **Given** all cross-slice findings, **When** the report summarizes inconsistencies, **Then** every proposed normalization item is classified as safe mechanical, requiring focused tests, or deferred.
2. **Given** multiple inconsistent approaches to the same concern, **When** the report recommends a direction, **Then** it compares them against existing durable project conventions and local accepted patterns rather than proposing an unproven abstraction.
3. **Given** a proposed change with meaningful regression risk, **When** it is prioritized, **Then** the report identifies the behavior that requires protection and the lowest appropriate validation boundary.

---

### User Story 3 - Preserve Existing Behavior During Audit (Priority: P3)

As a stakeholder, I need Phase 1 to remain a static audit so that the decision base can be reviewed without changing production behavior, contracts, data, or tests.

**Why this priority**: The audit is intended to define later work; combining it with implementation would obscure scope, evidence, and risk.

**Independent Test**: Compare the resulting change set with the allowed outputs and confirm that only the feature specification artifacts and the planned audit report are introduced or updated.

**Acceptance Scenarios**:

1. **Given** Phase 1 is in progress, **When** repository inspection is performed, **Then** no production code, test code, resource files, project files, routes, domain behavior, database artifacts, or integration behavior are changed.
2. **Given** a useful build or focused test is identified, **When** the report is written, **Then** the command may be recommended but is not executed as part of the audit.
3. **Given** issue-specific or historical context, **When** findings are documented, **Then** only current, verified behavior and relevant risk are carried into the decision report.

### Edge Cases

- A slice may use a shared contract for one boundary while retaining local types at another; the report must describe each boundary independently rather than assigning one overall compliance label.
- A list may be server-paged without using the current grid convention; backend and WebApp findings must remain separate.
- A visible field may differ from its active sort key; the report must identify both values and the user-facing consequence.
- A sort order may appear stable in sample data but lack an explicit tie-breaker; only explicit deterministic ordering counts as compliant.
- A missing test is reported only when an identified behavior has a meaningful regression risk and is not protected elsewhere.
- Duplicate numeric values are not automatically abstraction candidates; the report must first determine whether an existing convention already owns the value.
- A file or behavior may have changed since the stakeholder brief was written; current repository evidence takes precedence and discrepancies are noted.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Phase 1 MUST produce `research.md` in the feature directory as its sole concrete audit deliverable beyond specification workflow artifacts.
- **FR-002**: The audit MUST cover Warehouses, Zones, Storage Locations, SKUs, UoM, Inventory Balances, Inventory Ledger, Inventory Transfers, and Inventory Counts.
- **FR-003**: For each slice, the audit MUST identify applicable WebApp pages and grids, client operations, endpoint operations, backend queries and handlers, filtering/sorting/projection logic, public contracts, and relevant tests using precise repository paths.
- **FR-004**: For each slice, the audit MUST determine public request and response ownership, use of the shared list result envelope, and whether cross-boundary contracts contain dependencies outside transport concerns.
- **FR-005**: For each slice, the audit MUST record supported sort keys, their exact values and casing, their ownership, and how they are used by visible grids, client requests, and backend ordering.
- **FR-006**: The audit MUST identify missing user-facing sort capabilities, including warehouse-name sorting where warehouse names are displayed, without proposing removal of warehouse code from persisted or integration-facing data.
- **FR-007**: For each backend list pipeline, the audit MUST verify filter, count, sort, paging, projection, and materialization order; normalization of paging inputs; deterministic ordering; and returned paging metadata.
- **FR-008**: For each applicable WebApp grid, the audit MUST verify server-driven loading, grid-state mapping, request mapping, page-reset and reload behavior, cancellation propagation, explicit default sort alignment, and warehouse-name display conventions.
- **FR-009**: The audit MUST identify duplicated paging defaults, limits, page-size values, and normalization logic, while distinguishing reuse opportunities from coincidental equal values.
- **FR-010**: For each slice, the audit MUST identify existing behavioral protection for filtering, count-before-paging, paging, deterministic sorting, projection, endpoint boundaries, client request construction, cancellation, and error mapping where applicable.
- **FR-011**: Missing tests MUST be reported only for concrete regression risks not already protected at a lower or equivalent boundary.
- **FR-012**: The report MUST contain an executive summary, compact per-slice findings table, detailed per-slice findings, cross-cutting inconsistencies, prioritized normalization plan, explicit non-goals, suggested implementation phases, and risk notes.
- **FR-013**: Every normalization recommendation MUST be classified as safe mechanical work, work requiring focused tests, or deferred work.
- **FR-014**: Recommendations MUST use durable project conventions and accepted local patterns as the baseline and MUST NOT introduce a new abstraction during Phase 1.
- **FR-015**: The audit MUST distinguish verified compliance, verified inconsistency, confirmed absence, and areas that could not be determined through static inspection.
- **FR-016**: Phase 1 MUST use static repository inspection only and MUST NOT execute applications, infrastructure, migrations, database updates, builds, or tests.
- **FR-017**: Phase 1 MUST NOT modify production code, test code, resource files, project files, domain behavior, API routes or contracts, database schema, migrations, import behavior, or WebApp design.

### Contract and Boundary Requirements *(mandatory when feature exposes API, client, or UI behavior)*

- **CB-001**: The audit MUST evaluate cross-boundary list contracts as transport concerns independently from internal application requests and handlers.
- **CB-002**: The audit MUST evaluate whether public response shapes prevent domain entities and persistence-specific expressions from crossing service boundaries.
- **CB-003**: The audit MUST evaluate whether projections remain owned by the backend slice and occur before result materialization.
- **CB-004**: The audit MUST evaluate server-driven list behavior as one end-to-end concern while retaining separate findings for shared contracts, endpoints, internal queries, backend processing, clients, and grids.
- **CB-005**: The audit MUST preserve existing stable identifiers and contracts; any proposed contract change belongs to a later implementation phase and must be identified as such.

### Key Entities

- **List Slice**: One in-scope WMS list capability and its cross-boundary path from user-facing list through transport and backend query behavior.
- **Audit Finding**: A verified statement about current behavior, ownership, consistency, risk, or missing protection, supported by repository evidence.
- **Normalization Candidate**: A current inconsistency that may be addressed later, classified by implementation safety and test needs.
- **Validation Coverage**: Existing or recommended protection for a concrete behavior or architectural boundary.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 9 in-scope list slices have findings for 100% of applicable audit categories, with confirmed absences explicitly identified.
- **SC-002**: Every material finding and recommendation includes at least one precise repository path or an explicit statement that static inspection could not determine the answer.
- **SC-003**: 100% of normalization candidates are assigned to exactly one priority group: safe mechanical, focused-test, or deferred.
- **SC-004**: The report identifies explicit current behavior for filtering, count timing, paging, deterministic sorting, projection, and result metadata for all 9 backend list pipelines.
- **SC-005**: The report identifies explicit loading, sorting, reload, and warehouse-display behavior for 100% of applicable WebApp grids.
- **SC-006**: Reviewers can select the first follow-up implementation phase using the report without requiring a second repository-wide discovery pass.
- **SC-007**: Static change review confirms zero modifications to production code, tests, resources, contracts, routes, schema, migrations, or integration behavior during Phase 1.
- **SC-008**: The completed report contains no unqualified claims based only on historical issue context and no unresolved clarification markers.

## Assumptions

- Current repository contents are the source of truth when they differ from the stakeholder brief or older feature artifacts.
- The durable server-driven list, localization, architecture, API error, testing, and development workflow guidance remains authoritative for evaluating current slices.
- Phase 1 produces analysis and recommendations only; implementation specifications, plans, tasks, and code changes follow after stakeholder review.
- Existing stable warehouse code values remain valid business and integration data even where warehouse name is the intended user-facing list value.
- Static inspection is sufficient to identify the requested architecture and test-coverage facts; commands that would execute code may be recommended for later use.
- The current branch was prepared by the stakeholder and remains unchanged by this workflow.
