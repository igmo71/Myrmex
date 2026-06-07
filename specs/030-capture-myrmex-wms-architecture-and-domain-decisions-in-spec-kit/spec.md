# Feature Specification: Capture Myrmex WMS Architecture and Domain Decisions in Spec Kit

**Feature Branch**: `030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit`

**Created**: 2026-06-05

**Status**: Draft

**Input**: User description: "Create a Spec Kit specification for issue #30 based on the stakeholder document. This is a brownfield stabilization issue. Do not change production code. Do not change tests. Do not start Catalog, SKU, Inventory, Receiving, or Integration implementation. Expected output: specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/spec.md. The spec must describe documentation/specification work only."

## Clarifications

### Session 2026-06-07

- Q: Should issue #30 produce durable `.specify/memory/myrmex-*.md` files or only feature planning artifacts? -> A: Issue #30 must produce durable `.specify/memory/myrmex-*.md` documents for architecture, development workflow, topology patterns, API error handling, testing guidelines, and roadmap guidance.
- Q: Is `$speckit-implement` allowed for issue #30? -> A: `$speckit-implement` is allowed only as documentation execution from a docs-only `tasks.md`; it must not change production code, test code, runtime behavior, migrations, UI, API, persistence, or frameworks.
- Q: Should `AGENTS.md` permanently point to the issue #30 feature plan after completion? -> A: `AGENTS.md` may point to the issue #30 plan during active work, but before completion or merge it must point to durable `.specify/memory/myrmex-*.md` documents or a current active plan pattern.
- Q: What is the lifecycle of `StakeholderDocs/issue-30-spec-kit-stabilization.md`? -> A: Keep it as historical stakeholder input; operational guidance is superseded by durable `.specify/memory/myrmex-*.md` documents.
- Q: What guardrails must task generation include? -> A: Tasks for issue #30 must not include production code changes, test code changes, Catalog/SKU implementation, Inventory implementation, Receiving implementation, Integration implementation, broad refactoring, new frameworks, or GetById/List query handler tests.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Stabilize Architecture Guidance (Priority: P1)

A project maintainer needs issue #30 captured as a Spec Kit feature so future
planning and implementation work follows the accepted Myrmex WMS architecture,
workflow, and brownfield constraints.

**Why this priority**: Without a stable Spec Kit record, future work can drift into
greenfield assumptions, broad refactoring, unsupported assistant workflows, or
premature domain implementation.

**Independent Test**: Review the specification and confirm it documents only
architecture and specification work for issue #30, with no request to change
production code or tests.

**Acceptance Scenarios**:

1. **Given** the stakeholder document for issue #30, **When** the maintainer reads
   this specification, **Then** the accepted architecture principles are captured
   as documentation scope rather than implementation tasks.
2. **Given** the project uses Codex CLI skills for Spec Kit, **When** future agents
   inspect this specification, **Then** they see Codex skill invocation as the
   supported workflow and do not assume GitHub Copilot Chat slash commands.
3. **Given** Myrmex is brownfield, **When** this specification is used for planning,
   **Then** the plan remains limited to Spec Kit documentation and does not require
   production code, test code, or broad refactoring.
4. **Given** issue #30 has completed, **When** future agents inspect project
   guidance, **Then** they read durable `.specify/memory/myrmex-*.md` documents
   rather than relying on the issue #30 feature plan as permanent context.

---

### User Story 2 - Preserve Reference Slice Decisions (Priority: P2)

A developer or reviewer needs the current WMS Topology reference slice decisions
documented so later vertical slices can compare their domain language, UI patterns,
API error handling, and testing expectations against the accepted baseline.

**Why this priority**: WMS Topology is the current reference slice, and later
domains need a stable description of what has already been accepted.

**Independent Test**: Verify the specification identifies Warehouse, Zone, and
StorageLocation as the reference slice concepts and captures the accepted
write/read error-handling and regression-test expectations.

**Acceptance Scenarios**:

1. **Given** the current WMS Topology slice, **When** the reviewer reads this
   specification, **Then** Warehouse, Zone, and StorageLocation are documented as
   the reference concepts.
2. **Given** accepted API error-handling conventions, **When** future work uses
   this specification, **Then** write/action operations are documented as using
   `ApiResult<T>` and read/load operations are documented as exception-based and
   ProblemDetails-aware.
3. **Given** issue #28 already added regression coverage, **When** this issue is
   planned, **Then** domain tests, application/handler tests, and WMS topology API
   client error-handling tests are captured as existing expectations without
   adding new tests in this issue.

---

### User Story 3 - Bound Future Roadmap Language (Priority: P3)

A stakeholder needs future WMS areas recorded as roadmap direction only so the
project can discuss Catalog, SKU, Barcode, UoM, Packaging, Inventory, Receiving,
and Integration without starting their implementation in issue #30.

**Why this priority**: Roadmap vocabulary is useful for alignment, but starting
new domain work would violate the brownfield stabilization scope.

**Independent Test**: Confirm the specification lists future areas only as
roadmap direction and explicitly forbids implementation for Catalog, SKU,
Inventory, Receiving, and Integration in this issue.

**Acceptance Scenarios**:

1. **Given** roadmap domains are named in stakeholder guidance, **When** this
   specification is reviewed, **Then** they are recorded as future direction only.
2. **Given** a future plan references this issue, **When** it proposes Catalog,
   SKU, Inventory, Receiving, or Integration implementation, **Then** that proposal
   is out of scope for issue #30.

### Edge Cases

- If existing project guidance conflicts with the stakeholder document, the issue
  #30 stakeholder document and project constitution define the stabilization scope.
- If Spec Kit examples mention Copilot slash commands, this specification must
  translate the workflow to Codex CLI skill usage for Myrmex.
- If a roadmap term appears to imply feature work, it must be treated as
  documentation-only language unless a later issue explicitly approves
  implementation.
- If production code or tests appear to need changes while documenting decisions,
  those changes must be deferred to a separate approved issue.
- If query handler tests for GetById/List are discussed, they must be recorded as
  intentionally out of scope and possible future work.
- If `$speckit-implement` is invoked for issue #30, it must execute only
  documentation tasks from a docs-only `tasks.md`.
- If task generation includes runtime, test, or roadmap implementation work, those
  tasks must be rejected as out of scope for issue #30.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The specification MUST define issue #30 as a brownfield
  documentation/specification stabilization effort only.
- **FR-002**: The specification MUST state that production code changes, test code
  changes, broad refactoring, and new architectural frameworks are out of scope.
- **FR-003**: The specification MUST capture the accepted Myrmex architecture:
  modular monolith, Clean Architecture and DDD-inspired structure, vertical
  slices, no MediatR, internal command/query/handler dispatching, and simple
  explicit code over broad generic abstractions.
- **FR-004**: The specification MUST capture Codex CLI skill invocation as the
  project workflow for Spec Kit and MUST NOT assume Copilot Pro, Copilot
  Enterprise, or Copilot Chat slash-command capabilities.
- **FR-005**: The specification MUST identify WMS Topology as the current reference
  vertical slice and document Warehouse, Zone, and StorageLocation as its covered
  concepts.
- **FR-006**: The specification MUST capture accepted UI component patterns as
  documentation scope for the current WMS Topology experience, without requesting
  UI implementation.
- **FR-007**: The specification MUST capture the accepted API error-handling
  convention: write/action operations return `ApiResult<T>`; read/load operations
  use exception-based, ProblemDetails-aware flow.
- **FR-008**: The specification MUST capture testing expectations from issue #28:
  domain tests, application/handler tests, and WMS topology API client
  error-handling tests are expected coverage categories.
- **FR-009**: The specification MUST record query handler tests for GetById/List as
  intentionally out of scope for issue #30 and as possible future work.
- **FR-010**: The specification MUST record roadmap direction only for Catalog,
  SKU, Barcode, UoM, Packaging, Inventory, Receiving, and Integration.
- **FR-011**: The specification MUST explicitly forbid starting Catalog/SKU,
  Inventory, Receiving, or Integration implementation in issue #30.
- **FR-012**: The specification MUST remain suitable for downstream Spec Kit
  planning by separating accepted decisions, forbidden work, assumptions, and
  measurable outcomes.
- **FR-013**: Issue #30 MUST produce durable `.specify/memory/myrmex-*.md`
  documents for architecture, development workflow, topology patterns, API error
  handling, testing guidelines, and roadmap guidance.
- **FR-014**: `$speckit-implement` MAY be used for issue #30 only to execute
  documentation tasks from a docs-only `tasks.md`.
- **FR-015**: Issue #30 implementation tasks MUST NOT change production code, test
  code, runtime behavior, migrations, UI, API, persistence, or frameworks.
- **FR-016**: Before issue #30 is completed or merged, `AGENTS.md` MUST stop
  permanently pointing at the issue #30 feature plan and MUST instead reference
  durable `.specify/memory/myrmex-*.md` documents or a current active plan pattern.
- **FR-017**: `StakeholderDocs/issue-30-spec-kit-stabilization.md` MUST remain
  historical stakeholder input, with operational guidance superseded by durable
  `.specify/memory/myrmex-*.md` documents.
- **FR-018**: Task generation for issue #30 MUST NOT include production code
  changes, test code changes, Catalog/SKU implementation, Inventory
  implementation, Receiving implementation, Integration implementation, broad
  refactoring, new frameworks, or GetById/List query handler tests.

### Documentation Scope Rules

- **DSR-001**: Issue #30 MUST NOT change runtime domain behavior, persistence
  behavior, UI behavior, API behavior, or automated test behavior.
- **DSR-002**: WMS domain terms captured in this specification MUST be treated as
  ubiquitous language and planning context, not as instructions to implement new
  domain features.
- **DSR-003**: The current reference slice is limited to Warehouse, Zone, and
  StorageLocation.
- **DSR-004**: Catalog, SKU, Barcode, UoM, Packaging, Inventory, Receiving, and
  Integration MUST remain roadmap direction only for this issue.
- **DSR-005**: For issue #30, implementation means creating or updating
  documentation artifacts only.

### API Error-Handling Documentation Requirements

- **AEH-001**: The specification MUST document write/action operations as returning
  structured operation results.
- **AEH-002**: The specification MUST document read/load operations as using
  exception-based flow that remains aware of user-facing problem details.
- **AEH-003**: The specification MUST describe these conventions as accepted
  documentation, not as a request to change endpoint behavior.

### Key Entities

- **Spec Kit Feature Specification**: The issue #30 `spec.md` artifact that
  records scope, user scenarios, requirements, success criteria, and assumptions.
- **Architecture Decision Guidance**: The documented set of accepted Myrmex
  architecture principles and workflow constraints.
- **WMS Topology Reference Slice**: The existing Warehouse, Zone, and
  StorageLocation vertical slice used as the baseline for future WMS work.
- **Roadmap Domain Vocabulary**: Future WMS areas captured for alignment only:
  Catalog, SKU, Barcode, UoM, Packaging, Inventory, Receiving, and Integration.
- **Durable Myrmex Memory Documents**: The `.specify/memory/myrmex-*.md`
  artifacts that supersede issue #30 stakeholder notes as operational guidance.
- **Documentation Execution Task**: A task generated for issue #30 that creates or
  updates documentation artifacts only and does not touch runtime or test files.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A reviewer can verify in under 5 minutes that issue #30 is limited
  to documentation/specification work and excludes production and test changes.
- **SC-002**: The specification names 100% of accepted architecture decisions from
  the stakeholder document without introducing new architecture decisions.
- **SC-003**: The specification names all three current WMS Topology reference
  concepts: Warehouse, Zone, and StorageLocation.
- **SC-004**: The specification names all eight roadmap direction areas while
  clearly marking them as non-implementation scope for issue #30.
- **SC-005**: The specification contains zero unresolved clarification markers and
  zero requirements that instruct production feature implementation.
- **SC-006**: Future Spec Kit planning can derive documentation-only tasks from
  this specification without needing to inspect production code.
- **SC-007**: A reviewer can confirm all six required durable Myrmex memory
  documents exist under `.specify/memory/`.
- **SC-008**: A generated `tasks.md` for issue #30 contains zero production code,
  test code, runtime behavior, migration, UI, API, persistence, framework,
  roadmap implementation, or GetById/List query handler test tasks.

## Assumptions

- Issue #30 is intended to stabilize Spec Kit documentation after issue #28 added
  regression coverage for the WMS Topology vertical slice.
- The stakeholder document is the authoritative input for issue #30 scope.
- The project constitution applies to this issue, but test expectations are
  documented rather than expanded because the user explicitly forbids test changes.
- Current README guidance remains valid: Myrmex is an experimental WMS /
  fulfillment project focused on a clear, extensible, domain-oriented architecture.
- Any implementation of Catalog, SKU, Inventory, Receiving, Integration, or other
  roadmap areas requires a separate approved issue.
- `StakeholderDocs/issue-30-spec-kit-stabilization.md` remains useful for
  provenance, but durable `.specify/memory/myrmex-*.md` files become the
  operational guidance source.
