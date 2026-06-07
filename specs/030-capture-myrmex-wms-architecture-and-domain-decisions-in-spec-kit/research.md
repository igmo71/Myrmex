# Research: Issue #30 Spec Kit Stabilization

## Decision: Keep issue #30 documentation-only

**Rationale**: The feature spec, stakeholder document, and user instructions define issue #30 as brownfield stabilization. The accepted output is Spec Kit and architecture memory documentation, not runtime behavior.

**Alternatives considered**:

- Change production code to align with documented decisions: rejected because production code changes are forbidden.
- Add tests for uncovered query handlers: rejected because test code changes are forbidden and GetById/List query handler tests are explicitly future work.
- Start Catalog/SKU, Inventory, Receiving, or Integration slices: rejected because those areas are roadmap direction only.

## Decision: Use Codex CLI skill workflow as the supported Spec Kit workflow

**Rationale**: The stakeholder document states that Myrmex uses Codex CLI as the primary coding/review assistant. Spec Kit examples may mention Copilot Chat slash commands, but this project uses Codex skills such as `$speckit-plan`, `$speckit-tasks`, and `$speckit-implement`.

**Alternatives considered**:

- Document Copilot Chat slash commands as required tooling: rejected because the project must not assume Copilot Pro, Copilot Enterprise, or Copilot Chat slash-command capabilities.
- Document both workflows equally: rejected because it would weaken the project-specific workflow guidance.

## Decision: Treat WMS Topology as the current reference vertical slice

**Rationale**: The stakeholder document identifies WMS Topology as the accepted reference slice, with Warehouse, Zone, and StorageLocation as the covered concepts. Future WMS work should compare terminology, UI patterns, API error handling, and testing expectations against this baseline.

**Alternatives considered**:

- Expand the reference slice to Catalog/SKU or Inventory: rejected because those areas are not implemented in issue #30.
- Generalize the reference slice into an abstract WMS pattern: rejected because the project favors explicit local patterns over broad abstractions.

## Decision: Document API error-handling conventions without changing endpoints

**Rationale**: Accepted guidance says write/action operations return `ApiResult<T>`, while read/load operations use exception-based flow that remains ProblemDetails-aware. Issue #30 should preserve this as documentation so future plans can apply it deliberately.

**Alternatives considered**:

- Normalize all operations to `ApiResult<T>`: rejected because that would imply API behavior changes.
- Add new endpoint behavior for ProblemDetails handling: rejected because runtime changes are forbidden.

## Decision: Record issue #28 testing categories as expectations only

**Rationale**: Issue #28 already added regression coverage for domain tests, application/handler tests, and WMS topology API client error-handling tests. Issue #30 documents those categories as accepted expectations without modifying tests.

**Alternatives considered**:

- Add missing GetById/List query handler tests now: rejected because test changes are forbidden and those tests are future work.
- Remove testing guidance from issue #30: rejected because the stakeholder document explicitly asks to capture testing expectations.

## Decision: Capture roadmap vocabulary as future direction only

**Rationale**: Catalog, SKU, Barcode, UoM, Packaging, Inventory, Receiving, and Integration are useful planning vocabulary. For issue #30, they must remain roadmap language and must not become implementation scope.

**Alternatives considered**:

- Create implementation tasks for each roadmap area: rejected because runtime behavior work is forbidden.
- Omit roadmap language entirely: rejected because stakeholders need bounded future direction documented.

## Decision: Prefer small staged documentation outputs

**Rationale**: The user requested small staged outputs. The plan should separate architecture guidance, workflow guidance, topology patterns, API error-handling patterns, testing expectations, and roadmap direction so each can be reviewed independently in later documentation work.

**Alternatives considered**:

- Produce one large architecture document: rejected because it is harder to review and easier to drift into broad refactoring language.
- Create tasks for runtime behavior: rejected because downstream implementation tasks must remain documentation/specification only.

## Clarification Status

All planning unknowns are resolved. No unresolved clarification markers remain.
