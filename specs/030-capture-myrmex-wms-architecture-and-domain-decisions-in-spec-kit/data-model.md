# Data Model: Issue #30 Documentation Artifacts

This model describes documentation entities only. It does not define runtime domain entities, database tables, API payloads, handlers, UI components, tests, migrations, or production behavior.

## Spec Kit Feature Plan

**Purpose**: Coordinates the documentation-only work for issue #30.

**Fields**:

- `branch`: `30-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit`
- `specPath`: `specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/spec.md`
- `scopeType`: brownfield documentation/specification stabilization
- `allowedOutputs`: Spec Kit docs, architecture memory docs, workflow memory docs, topology pattern docs, API error-handling docs, testing expectation docs, roadmap docs
- `forbiddenOutputs`: production code, test code, runtime implementation, broad refactoring, new frameworks

**Relationships**:

- Uses Architecture Decision Guidance, Workflow Memory, Topology Pattern Memory, API Error-Handling Memory, Testing Expectation Memory, Roadmap Direction Memory, and Scope Exclusion.

**Validation Rules**:

- Must not contain unresolved clarification markers.
- Must not create runtime implementation scope.
- Must identify documentation-only validation.

## Architecture Decision Guidance

**Purpose**: Captures accepted Myrmex architecture decisions for future planning.

**Fields**:

- `architectureStyle`: modular monolith
- `designApproach`: Clean Architecture and DDD-inspired vertical slices
- `dispatching`: internal command/query/handler dispatching
- `frameworkExclusions`: no MediatR and no new architectural frameworks for this issue
- `simplicityRule`: simple explicit code over broad generic abstractions

**Relationships**:

- Governed by `.specify/memory/constitution.md`.
- Referenced by future documentation and planning outputs.

**Validation Rules**:

- Must describe accepted decisions without introducing new architecture decisions.
- Must preserve existing module boundaries.

## Workflow Memory

**Purpose**: Records the supported Spec Kit workflow for Myrmex agents.

**Fields**:

- `primaryAssistant`: Codex CLI
- `supportedInvocation`: Codex skills such as `$speckit-plan`, `$speckit-tasks`, `$speckit-analyze`, and `$speckit-implement`
- `unsupportedAssumption`: Copilot Chat slash-command availability

**Relationships**:

- Updates the Spec Kit pointer in `AGENTS.md`.
- Supports future Spec Kit tasks and implementation phases.

**Validation Rules**:

- Must not require Copilot Pro, Copilot Enterprise, or Copilot Chat slash commands.
- Must point agents to the current feature plan for project context.

## Topology Pattern Memory

**Purpose**: Documents WMS Topology as the current reference vertical slice.

**Fields**:

- `sliceName`: WMS Topology
- `referenceConcepts`: Warehouse, Zone, StorageLocation
- `patternScope`: domain language, UI component patterns, API error handling, and testing expectations

**Relationships**:

- Informs future WMS vertical slices.
- Linked to API Error-Handling Memory and Testing Expectation Memory.

**Validation Rules**:

- Must not expand the reference slice beyond Warehouse, Zone, and StorageLocation for issue #30.
- Must not request UI implementation.

## API Error-Handling Memory

**Purpose**: Captures accepted API error-handling conventions.

**Fields**:

- `writeActionConvention`: write/action operations return `ApiResult<T>`
- `readLoadConvention`: read/load operations use exception-based flow
- `problemDetailsAwareness`: read/load failures remain aware of user-facing ProblemDetails

**Relationships**:

- Applies as documentation guidance for future API work.
- Must remain consistent with current WMS Topology reference guidance.

**Validation Rules**:

- Must not require endpoint or API client changes in issue #30.
- Must distinguish write/action and read/load behavior.

## Testing Expectation Memory

**Purpose**: Records accepted coverage categories from issue #28.

**Fields**:

- `domainCoverage`: domain tests
- `applicationCoverage`: application/handler tests
- `apiClientCoverage`: WMS topology API client error-handling tests
- `futureCoverage`: GetById/List query handler tests may be future work

**Relationships**:

- Derived from issue #28 stabilization context.
- Referenced by quickstart validation and future planning.

**Validation Rules**:

- Must not add or modify test code for issue #30.
- Must mark GetById/List query handler tests as out of scope.

## Roadmap Direction Memory

**Purpose**: Captures future WMS vocabulary without authorizing implementation.

**Fields**:

- `futureAreas`: Catalog, SKU, Barcode, UoM, Packaging, Inventory, Receiving, Integration
- `scopeStatus`: roadmap direction only

**Relationships**:

- May inform future separately approved issues.
- Constrained by Scope Exclusion.

**Validation Rules**:

- Must not create implementation tasks for Catalog/SKU, Inventory, Receiving, or Integration.
- Must not imply runtime behavior changes.

## Scope Exclusion

**Purpose**: Defines hard boundaries for issue #30.

**Fields**:

- `productionCodeChanges`: forbidden
- `testCodeChanges`: forbidden
- `catalogSkuImplementation`: forbidden
- `inventoryImplementation`: forbidden
- `receivingImplementation`: forbidden
- `integrationImplementation`: forbidden
- `broadRefactoring`: forbidden
- `newFrameworks`: forbidden

**Relationships**:

- Constrains all other documentation entities.
- Used by quickstart validation and downstream planning contract.

**Validation Rules**:

- Every generated artifact must remain compatible with these exclusions.
- Any future implementation must be deferred to a separately approved issue.

## State Transitions

Documentation artifacts move through these states:

```text
Draft -> Reviewed -> Accepted for downstream planning
Draft -> Needs correction -> Reviewed
```

An artifact cannot move to `Accepted for downstream planning` if it contains unresolved clarification markers, runtime implementation tasks, production code change instructions, or test code change instructions.
