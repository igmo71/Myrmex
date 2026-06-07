# Contract: Issue #30 Documentation Outputs

This contract defines what downstream work may produce from issue #30. It is a documentation contract, not a runtime API contract.

## Scope

Documentation derived from issue #30 MAY create or update:

- Spec Kit documentation.
- Architecture memory documents.
- Workflow memory documents.
- Topology pattern documentation.
- API error-handling documentation.
- Testing expectation documentation.
- Roadmap documentation.

Documentation derived from issue #30 MUST NOT create or update:

- Production code.
- Test code.
- Catalog/SKU implementation.
- Inventory implementation.
- Receiving implementation.
- Integration implementation.
- Broad refactoring plans.
- New framework adoption plans.

## Required Source Alignment

Each output MUST align with:

- `.specify/memory/constitution.md`
- `specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/spec.md`
- `StakeholderDocs/issue-30-spec-kit-stabilization.md` when present
- `README.md`
- `AGENTS.md`

If these sources conflict, the issue #30 stakeholder document and project constitution define the stabilization scope.

## Required Documentation Sections

Any issue #30 memory or planning document SHOULD be small and focused. A valid staged output SHOULD cover one of these subjects:

- Architecture principles.
- Codex/Spec Kit workflow.
- WMS ubiquitous language.
- WMS Topology patterns.
- UI component patterns.
- API error-handling patterns.
- Testing expectations.
- Roadmap direction.

## Required Language Boundaries

Outputs MUST describe accepted decisions using documentation language such as:

- "document"
- "capture"
- "record"
- "reference"
- "future direction"
- "out of scope"
- "possible future work"

Outputs MUST NOT use language that instructs runtime implementation for this issue, such as:

- "implement Catalog"
- "add Inventory"
- "change endpoint behavior"
- "modify test coverage"
- "refactor modules"
- "replace dispatcher"
- "introduce framework"

## WMS Topology Reference Contract

When WMS Topology is referenced, the output MUST:

- Identify Warehouse, Zone, and StorageLocation as the current reference concepts.
- Treat WMS Topology as the baseline for later slices.
- Avoid expanding the current slice in issue #30.

## API Error-Handling Contract

When API error handling is referenced, the output MUST:

- Document write/action operations as returning `ApiResult<T>`.
- Document read/load operations as exception-based and ProblemDetails-aware.
- State that issue #30 does not change endpoint, API client, or ProblemDetails behavior.

## Testing Expectation Contract

When tests are referenced, the output MUST:

- Record issue #28 coverage categories: domain tests, application/handler tests, and WMS topology API client error-handling tests.
- State that issue #30 does not add or modify automated tests.
- Mark GetById/List query handler tests as out of scope and possible future work.

## Roadmap Contract

When roadmap vocabulary is referenced, the output MUST:

- List Catalog, SKU, Barcode, UoM, Packaging, Inventory, Receiving, and Integration as future direction only.
- State that implementation requires a separate approved issue.

## Acceptance Criteria

An output satisfies this contract when:

- It can be reviewed without opening production or test files.
- It contains no unresolved clarification markers.
- It preserves the brownfield documentation-only scope.
- It does not create implementation tasks for runtime behavior.
