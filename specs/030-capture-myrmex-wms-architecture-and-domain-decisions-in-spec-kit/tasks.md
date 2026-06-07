# Tasks: Capture Myrmex WMS Architecture and Domain Decisions in Spec Kit

**Input**: Design documents from `specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/documentation-output-contract.md`, `quickstart.md`, `.specify/memory/constitution.md`, `.specify/memory/myrmex-*.md`

**Tests**: No production tests or test code tasks are generated. Issue #30 is documentation-only; independent tests are documentation review and quickstart validation criteria.

**Organization**: Tasks are grouped by user story to enable independent documentation completion and review.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches a different documentation file and has no dependency on an incomplete task.
- **[Story]**: Which user story the task belongs to, for user story phases only.
- Every task references documentation/specification files only.

## Issue #30 Guardrails

Tasks in this file MUST NOT include production code changes, test code changes, runtime behavior changes, migrations, UI changes, API changes, persistence changes, framework changes, Catalog/SKU implementation, Inventory implementation, Receiving implementation, Integration implementation, broad refactoring, or GetById/List query handler tests.

For issue #30, `$speckit-implement` means creating, validating, or refining documentation artifacts only.

---

## Phase 1: Setup (Documentation Baseline)

**Purpose**: Confirm existing documentation inputs and durable memory docs are present before story work begins.

- [ ] T001 Verify issue #30 planning artifacts exist in specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/spec.md, specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/plan.md, specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/research.md, specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/data-model.md, specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/contracts/documentation-output-contract.md, and specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/quickstart.md
- [ ] T002 Verify durable memory documents exist in .specify/memory/myrmex-architecture.md, .specify/memory/myrmex-development-workflow.md, .specify/memory/myrmex-topology-patterns.md, .specify/memory/myrmex-api-error-handling.md, .specify/memory/myrmex-testing-guidelines.md, and .specify/memory/myrmex-roadmap.md
- [ ] T003 [P] Review AGENTS.md to confirm it points to durable .specify/memory/myrmex-*.md guidance and the current active plan pattern
- [ ] T004 [P] Review StakeholderDocs/issue-30-spec-kit-stabilization.md as historical input only and note any remaining alignment gaps in specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/research.md

---

## Phase 2: Foundational (Scope Guardrails)

**Purpose**: Establish non-runtime documentation guardrails that block all story refinement work.

**CRITICAL**: No user story documentation refinement should begin until this phase confirms the docs-only boundary.

- [ ] T005 Confirm forbidden-work language is complete in specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/spec.md
- [ ] T006 Confirm task-generation guard language is complete in specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/contracts/documentation-output-contract.md
- [ ] T007 Confirm docs-only `$speckit-implement` meaning is captured in specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/plan.md
- [ ] T008 Confirm durable memory document requirements are captured in specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/data-model.md

**Checkpoint**: Scope guardrails are documented before any story-specific refinement.

---

## Phase 3: User Story 1 - Stabilize Architecture Guidance (Priority: P1) MVP

**Goal**: Ensure future planning follows accepted Myrmex WMS architecture, Codex workflow, and brownfield documentation-only constraints.

**Independent Test**: A reviewer can read the issue #30 spec, plan, AGENTS.md, and the architecture/workflow memory docs and confirm there is no request for production code, test code, broad refactoring, unsupported assistant workflow, or stale permanent issue-plan context.

### Documentation for User Story 1

- [ ] T009 [US1] Refine accepted architecture guidance in .specify/memory/myrmex-architecture.md
- [ ] T010 [US1] Refine Codex Spec Kit workflow and docs-only implementation guidance in .specify/memory/myrmex-development-workflow.md
- [ ] T011 [US1] Align architecture and workflow requirements in specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/spec.md
- [ ] T012 [US1] Align architecture and workflow execution boundaries in specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/plan.md
- [ ] T013 [US1] Align AGENTS.md with durable memory guidance and current active plan pattern

**Checkpoint**: User Story 1 can be reviewed independently against SC-001, SC-002, SC-006, SC-007, and SC-008.

---

## Phase 4: User Story 2 - Preserve Reference Slice Decisions (Priority: P2)

**Goal**: Preserve WMS Topology reference slice decisions for Warehouse, Zone, StorageLocation, API error handling, UI pattern documentation, and testing expectations.

**Independent Test**: A reviewer can read topology, API error-handling, and testing memory docs and confirm Warehouse, Zone, and StorageLocation are the only reference concepts; write/action and read/load conventions are documented; issue #28 coverage expectations are captured; and GetById/List query handler tests remain future work only.

### Documentation for User Story 2

- [ ] T014 [P] [US2] Refine WMS Topology reference slice guidance in .specify/memory/myrmex-topology-patterns.md
- [ ] T015 [P] [US2] Refine write/action and read/load API error-handling guidance in .specify/memory/myrmex-api-error-handling.md
- [ ] T016 [P] [US2] Refine issue #28 testing expectations and GetById/List future-work boundary in .specify/memory/myrmex-testing-guidelines.md
- [ ] T017 [US2] Align WMS Topology, API error-handling, and testing guidance in specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/spec.md
- [ ] T018 [US2] Align WMS Topology, API error-handling, and testing guidance in specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/contracts/documentation-output-contract.md
- [ ] T019 [US2] Align WMS Topology, API error-handling, and testing guidance in specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/data-model.md

**Checkpoint**: User Story 2 can be reviewed independently against SC-003 and the issue #28 testing-expectation requirements without adding tests.

---

## Phase 5: User Story 3 - Bound Future Roadmap Language (Priority: P3)

**Goal**: Keep Catalog, SKU, Barcode, UoM, Packaging, Inventory, Receiving, and Integration as roadmap direction only.

**Independent Test**: A reviewer can read the roadmap memory doc, spec, contract, and quickstart and confirm roadmap terms are future direction only and no Catalog/SKU, Inventory, Receiving, or Integration implementation tasks are present.

### Documentation for User Story 3

- [ ] T020 [US3] Refine roadmap direction and non-implementation boundaries in .specify/memory/myrmex-roadmap.md
- [ ] T021 [US3] Align roadmap direction language in specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/spec.md
- [ ] T022 [US3] Align roadmap direction language in specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/contracts/documentation-output-contract.md
- [ ] T023 [US3] Align roadmap validation steps in specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/quickstart.md

**Checkpoint**: User Story 3 can be reviewed independently against SC-004 and the forbidden roadmap implementation guardrails.

---

## Phase 6: Polish & Cross-Cutting Documentation Validation

**Purpose**: Validate consistency across all issue #30 documentation artifacts.

- [ ] T024 Check for unresolved clarification markers in specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/spec.md, specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/plan.md, specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/research.md, specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/data-model.md, specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/contracts/documentation-output-contract.md, specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/quickstart.md, .specify/memory/myrmex-architecture.md, .specify/memory/myrmex-development-workflow.md, .specify/memory/myrmex-topology-patterns.md, .specify/memory/myrmex-api-error-handling.md, .specify/memory/myrmex-testing-guidelines.md, and .specify/memory/myrmex-roadmap.md
- [ ] T025 Check that specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/tasks.md references documentation/specification files only
- [ ] T026 Check that specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/tasks.md contains no forbidden production, test, runtime, migration, UI, API, persistence, framework, roadmap implementation, or GetById/List query handler test tasks
- [ ] T027 Run the validation guide in specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/quickstart.md
- [ ] T028 Confirm specs/030-capture-myrmex-wms-architecture-and-domain-decisions-in-spec-kit/checklists/requirements.md remains passing after documentation updates

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; confirms current documentation state.
- **Foundational (Phase 2)**: Depends on Setup completion; blocks all user story documentation refinement.
- **User Story 1 (Phase 3)**: Depends on Foundational completion; MVP scope.
- **User Story 2 (Phase 4)**: Depends on Foundational completion; can run after or alongside US1 once shared guardrails are stable.
- **User Story 3 (Phase 5)**: Depends on Foundational completion; can run after or alongside US1/US2 once shared guardrails are stable.
- **Polish (Phase 6)**: Depends on selected user story phases being complete.

### User Story Dependencies

- **User Story 1 (P1)**: No dependency on other stories after Foundational.
- **User Story 2 (P2)**: No dependency on US3; should align with US1 workflow and architecture language.
- **User Story 3 (P3)**: No dependency on US2; should align with US1 scope and workflow guardrails.

### Within Each User Story

- Refine durable memory docs first.
- Align feature-local Spec Kit docs after durable memory docs are reviewed.
- Validate each story against its independent test before moving to polish.

---

## Parallel Opportunities

- T003 and T004 can run in parallel after T001 and T002.
- T014, T015, and T016 can run in parallel because they refine different memory docs.
- US2 and US3 can proceed in parallel after Phase 2 if US1 guardrail language is already stable.

## Parallel Example: User Story 2

```text
Task: "Refine WMS Topology reference slice guidance in .specify/memory/myrmex-topology-patterns.md"
Task: "Refine write/action and read/load API error-handling guidance in .specify/memory/myrmex-api-error-handling.md"
Task: "Refine issue #28 testing expectations and GetById/List future-work boundary in .specify/memory/myrmex-testing-guidelines.md"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational guardrails.
3. Complete Phase 3: User Story 1.
4. Stop and validate architecture/workflow guidance independently.

### Incremental Delivery

1. Complete Setup and Foundational guardrails.
2. Deliver US1 architecture/workflow stabilization.
3. Deliver US2 reference slice, API error-handling, and testing guidance.
4. Deliver US3 roadmap bounds.
5. Complete polish validation.

### Parallel Documentation Strategy

After Phase 2, separate reviewers can refine different durable memory docs in parallel as long as each task only edits the file named in the task and preserves the documentation-only guardrails.

---

## Notes

- [P] tasks touch different documentation files and have no dependency on incomplete tasks.
- No tasks reference production or test project files.
- No tasks create runtime behavior, tests, migrations, UI, API, persistence, framework, or domain implementation work.
- The six durable memory documents already exist; tasks validate, refine, and complete them rather than recreating them.
