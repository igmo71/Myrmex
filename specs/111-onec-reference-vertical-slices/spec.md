# Feature Specification: 1C Reference Vertical Slices

**Feature Branch**: `111-onec-reference-vertical-slices` (existing branch; no feature branch created)

**Created**: 2026-07-20

**Status**: Draft

**Input**: User description: `StakeholderDocs/111 Refactor 1C reference integration into explicit vertical slices.md`

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Understand and Change Warehouse Integration Locally (Priority: P1)

As a Myrmex developer, I want Warehouse import and synchronization behavior to have one clear integration owner so that I can understand, review, debug, and change the Warehouse flow without reconstructing a generic all-reference workflow.

**Why this priority**: Warehouse provides the simplest complete reference path and establishes the explicit ownership model required for the rest of the refactoring.

**Independent Test**: Inspect and exercise the Warehouse manual-import and synchronize-one entry paths and verify that source loading, folder handling, mapping, application dispatch, outcome classification, and diagnostics are visible from the Warehouse-owned integration area while existing Warehouse behavior remains unchanged.

**Acceptance Scenarios**:

1. **Given** an authorized operator starts a Warehouse full import, **When** the operation processes current source records, **Then** the Warehouse-owned flow reads, filters, maps, applies, and accounts for them through the existing WMS Warehouse import behavior.
2. **Given** a reactive request or internal call identifies one Warehouse, **When** synchronization runs, **Then** the Warehouse-owned flow coordinates the operation, reads the current object, handles folder, absence, and source-failure outcomes, applies eligible data through the existing WMS Warehouse import behavior, and returns the existing outcome.
3. **Given** a developer investigates Warehouse synchronization, **When** they follow the execution path, **Then** they do not need an all-reference orchestration service, a central reference-type switch, or a chain of business callbacks to identify the sequence and ownership.

---

### User Story 2 - Understand and Change Unit of Measure Integration Locally (Priority: P2)

As a Myrmex developer, I want Unit of Measure import and synchronization behavior to have one clear integration owner so that Unit of Measure changes remain independent from Warehouse and Stock Keeping Unit orchestration.

**Why this priority**: Unit of Measure validates that the explicit ownership model works for a second reference with different source semantics and no folder behavior.

**Independent Test**: Inspect and exercise the Unit of Measure manual-import and synchronize-one entry paths and verify that source loading, mapping, application dispatch, outcome classification, and diagnostics are visible from the Unit of Measure-owned integration area without introducing folder handling or changing observable behavior.

**Acceptance Scenarios**:

1. **Given** an authorized operator starts a Unit of Measure full import, **When** the operation processes current source records, **Then** the Unit of Measure-owned flow reads, maps, applies, and accounts for them through the existing WMS Unit of Measure import behavior.
2. **Given** a reactive request or internal call identifies one Unit of Measure, **When** synchronization runs, **Then** the Unit of Measure-owned flow coordinates the operation, reads the current object, handles absence and source-failure outcomes, applies eligible data through the existing WMS Unit of Measure import behavior, and returns the existing outcome.
3. **Given** a Unit of Measure source record, **When** it is imported or synchronized, **Then** it is not treated as a folder and gains no folder-specific outcome.

---

### User Story 3 - Keep SKU Processing and Dependency Repair Explicit (Priority: P3)

As a Myrmex developer, I want Stock Keeping Unit import, synchronization, and bounded base-Unit-of-Measure repair to be owned by the SKU integration flow so that its distinct paging, batching, partial-result, and dependency behavior is easy to reason about.

**Why this priority**: SKU is the most complex reference flow and must retain its bounded dependency behavior without turning the new organization into another generalized orchestration framework.

**Independent Test**: Exercise SKU full import, synchronize-one, and a missing base-Unit-of-Measure case and verify that the SKU-owned flow preserves whole-operation coordination, paging, batching, committed partial results, folder handling, final outcome mapping, and the one-dependency/one-retry limits.

**Acceptance Scenarios**:

1. **Given** an authorized operator starts an SKU full import, **When** multiple configured pages and batches are processed, **Then** the SKU-owned flow coordinates the entire operation, preserves committed partial results, and returns the existing aggregate accounting and errors.
2. **Given** a reactive request or internal call identifies one SKU, **When** synchronization runs, **Then** the SKU-owned flow reads the current object, handles folder, absence, and source-failure outcomes, applies eligible data through the existing WMS SKU import behavior, and returns the existing outcome.
3. **Given** an eligible SKU cannot be applied because one required base Unit of Measure is missing or inactive, **When** bounded repair runs, **Then** the SKU-owned flow requests synchronization of at most that one Unit of Measure and applies the same SKU at most one additional time.
4. **Given** the bounded repair does not produce an active valid dependency, **When** SKU synchronization completes, **Then** it returns the appropriate existing failure outcome without recursion, dependency graphs, or further repair attempts.

### Edge Cases

- A Warehouse or SKU current-object read returns a folder; the owning flow returns the existing controlled-skip outcome without applying it. Unit of Measure never gains this rule.
- A current-object read returns no object, malformed source data, a transient source failure, or caller cancellation; the owning flow preserves the distinct existing outcome and cancellation behavior.
- Manual import overlaps with reactive or internal synchronization for the same reference type; existing same-type fail-fast or busy behavior and whole-operation lease scope remain unchanged, while different reference types remain independently coordinated.
- A source record has the same version already stored locally; existing unchanged behavior, including no duplicate mutation or event, remains intact.
- An SKU import commits some batches before a later batch fails or is cancelled; the existing committed partial-result accounting remains visible to the operator.
- An SKU dependency repair encounters a missing, inactive, invalid, deletion-marked, busy, or unavailable Unit of Measure; repair remains bounded and the final SKU outcome retains useful failure information.
- Application shutdown interrupts reactive processing; the existing durable recovery path remains responsible for abandoned processing.
- Removing shared orchestration leaves obsolete registrations, endpoints, or compatibility paths; the refactoring is incomplete until callers use the explicit owning flows and obsolete paths are removed.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST organize the existing 1C reference integration into exactly three explicit ownership areas: Warehouse, Unit of Measure, and Stock Keeping Unit.
- **FR-002**: Each reference ownership area MUST make its manual full-import path and its reactive/internal synchronize-one path independently understandable from entry through source handling, mapping, existing WMS application behavior, outcome classification, and diagnostics.
- **FR-003**: Each reference ownership area MUST own all source-specific knowledge for that reference, including its source fields, source collection, mapping, controlled skips, application operation, and result interpretation.
- **FR-004**: Manual, reactive, and internal flows MUST continue to apply references through the existing WMS import behavior so that external identity, validation, version comparison, lifecycle changes, conflicts, persistence, transactions, and domain events retain one application owner.
- **FR-005**: The Warehouse manual-import flow MUST preserve full-collection loading, source filtering, mapping, operation-level coordination, aggregate result accounting, cancellation, and error behavior.
- **FR-006**: Warehouse synchronize-one MUST preserve same-type coordination, current-object loading, folder handling, not-found handling, source-failure handling, mapping, application, and existing outcome classification.
- **FR-007**: The Unit of Measure manual-import flow MUST preserve full-collection loading, mapping, operation-level coordination, aggregate result accounting, cancellation, and error behavior.
- **FR-008**: Unit of Measure synchronize-one MUST preserve same-type coordination, current-object loading, not-found handling, source-failure handling, mapping, application, and existing outcome classification without adding folder semantics.
- **FR-009**: The SKU manual-import flow MUST preserve whole-operation coordination, configured paging and batching, committed partial results, aggregate accounting, cancellation, and structured error behavior.
- **FR-010**: SKU synchronize-one MUST preserve current-object loading, folder handling, not-found handling, source-failure handling, application, existing outcome classification, and bounded base-Unit-of-Measure repair.
- **FR-011**: SKU repair MUST synchronize at most one required Unit of Measure and MUST apply the same SKU at most twice in total.
- **FR-012**: SKU repair MUST NOT recursively resolve dependencies or introduce a generalized repair mechanism.
- **FR-013**: Reactive synchronization MUST continue to use the existing durable request foundation and its existing boundary for invoking a concrete integration flow.
- **FR-014**: The refactoring MUST NOT introduce a second queue, worker, processor, retry policy, recovery process, request lifecycle, or durable status.
- **FR-015**: The main reference workflows MUST NOT be selected by a common Warehouse/Unit-of-Measure/SKU type switch.
- **FR-016**: The main reference workflows MUST NOT be represented by a generic operation that receives business callbacks for source reading, mapping, application dispatch, and result classification.
- **FR-017**: Shared integration code MUST be limited to mechanisms that are uniform across reference types and MUST NOT own reference-specific business sequence or knowledge.
- **FR-018**: Small shared mechanisms MAY coordinate leases, authenticated source requests, source-version validation, uniform diagnostics, or translation from an internal synchronization outcome to the durable handler result when they do not conceal a reference workflow.
- **FR-019**: Obsolete all-reference orchestration, delegate-based paths, and superseded registrations MUST be removed after callers use the explicit ownership areas; compatibility wrappers MUST NOT preserve parallel workflows.
- **FR-020**: Existing public routes, authorization policies, request and response contracts, operator workflow, and localized text MUST remain unchanged.
- **FR-021**: Existing manual-import accounting for processed, created, updated, unchanged, skipped, and failed records, including structured and bounded returned errors, MUST remain unchanged.
- **FR-022**: Existing internal outcomes—applied, unchanged, controlled skip, not found, busy, transient failure, and permanent failure—MUST remain distinguishable.
- **FR-023**: Existing source-version, source-of-truth, deactivation/reactivation, source-owned field protection, persistence, and domain-event behavior MUST remain unchanged.
- **FR-024**: Existing same-reference coordination and cross-reference independence MUST remain unchanged for manual, reactive, and internal operations.
- **FR-025**: Automated test changes MUST be limited to the minimum required for this behavior-preserving refactoring. Existing #104 and #109 tests MUST be reused or minimally adjusted. Moving, renaming, or splitting production classes MUST NOT by itself justify new tests. A new test MAY be added only for a material regression risk introduced by a genuinely new boundary that existing coverage cannot prove. Duplicate per-reference-type and durable-synchronization-foundation behavior matrices MUST NOT be introduced.

### Contract and Boundary Requirements *(mandatory when feature exposes API, client, or UI behavior)*

- **CB-001**: Existing Warehouse, Unit of Measure, and SKU manual-import routes MUST retain their current requests, responses, authorization, cancellation result, and error semantics.
- **CB-002**: Existing reference change-notification routes MUST retain machine authentication, validation, durable insert-or-duplicate resolution before acceptance, stable entity-type identity, and empty accepted-response behavior.
- **CB-003**: Internal synchronize-one capability MUST remain internal and MUST NOT gain a public endpoint or operator-facing action.
- **CB-004**: The durable synchronization foundation MUST remain provider-neutral, while 1C source transport and mapping knowledge remains outside WMS domain and application contracts.
- **CB-005**: Each concrete reactive flow MUST remain reachable through the existing durable handler boundary without moving durable lifecycle ownership into a reference-specific area.
- **CB-006**: The integration layer MUST depend on existing WMS application operations and MUST NOT duplicate WMS create, update, deactivate, reactivate, validation, transaction, persistence, or event rules.
- **CB-007**: The refactoring MUST NOT change the database schema, persistence mappings, durable synchronization statuses, or external-reference state model.

### Observability & Error Handling *(mandatory when feature exposes runtime behavior)*

- **OE-001**: Diagnostics for a reactive reference request MUST continue to associate the request identity, notified source version, reference type, external identity, current-source outcome, failure reason, and retry suitability.
- **OE-002**: Reference-specific diagnostics MUST originate from the corresponding reference ownership area so that failures can be located without interpreting an all-reference workflow.
- **OE-003**: Existing distinctions among controlled skip, not found, busy, transient failure, permanent failure, cancellation, and successful applied or unchanged outcomes MUST be preserved.
- **OE-004**: The refactoring MUST NOT reduce existing error detail or expose credentials, source secrets, or source payloads in diagnostics.

### Scope Boundaries

- Receiving and Shipping processes, document import, document synchronization, snapshots, conflict resolution, demand-versus-execution models, and outbound status updates are out of scope.
- New reference types, multiple external providers, generalized external-link models, metadata-driven mapping, generic synchronization engines, recursive dependency resolution, and distributed coordination are out of scope.
- New durable statuses, operator synchronization UI, public synchronize-one endpoints, database redesign, and unrelated WMS refactoring are out of scope.
- Designing a future document-integration folder or abstraction model is out of scope.
- Build, test, migration, database, application-startup, container, and other environment-changing command execution is developer-controlled and is not part of this specification workflow.

### Key Entities *(include if feature involves data)*

- **Reference Integration Ownership Area**: The cohesive integration behavior for one supported reference type, including manual import, reactive and internal synchronization, source access and mapping, outcome interpretation, and reference-specific diagnostics.
- **Warehouse Reference**: A 1C-owned warehouse record synchronized into the existing WMS Warehouse model; source folders are controlled skips.
- **Unit of Measure Reference**: A 1C-owned measurement-unit record synchronized into the existing WMS Unit of Measure model; it has no folder semantics.
- **Stock Keeping Unit Reference**: A 1C-owned product reference synchronized into the existing WMS SKU model; it supports folder skips and requires an active base Unit of Measure.
- **Synchronization Request**: The existing durable record that coordinates reactive work, retry, deferred handling, failure, completion, and abandoned-processing recovery.
- **Synchronization Outcome**: The existing internal result distinguishing applied, unchanged, controlled skip, not found, busy, transient failure, and permanent failure before translation to durable processing behavior.
- **Manual Import Result**: The existing operator-facing aggregate of processed, created, updated, unchanged, skipped, and failed records plus bounded structured errors and cancellation information.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All seven named flows—Warehouse import, Warehouse synchronize-one, Unit of Measure import, Unit of Measure synchronize-one, SKU import, SKU synchronize-one, and SKU base-Unit-of-Measure repair—have exactly one evident reference-specific owner.
- **SC-002**: For each of the three reference types, a developer can locate the source read, mapping, WMS application boundary, and outcome handling for either entry path within 10 minutes without tracing through an all-reference switch or reconstructing a callback chain.
- **SC-003**: 100% of existing acceptance scenarios for manual imports, reactive synchronization, internal synchronize-one, source-version behavior, source ownership, coordination, and durable lifecycle continue to produce the same observable outcomes after the refactoring.
- **SC-004**: Representative compatibility checks show no changes to any existing public route, authorization decision, request or response field, operator workflow, localized text, durable status, persistence schema, or domain behavior.
- **SC-005**: Every tested SKU repair attempt synchronizes no more than one Unit of Measure and applies the SKU no more than twice in total, including failure cases.
- **SC-006**: Code review finds zero remaining production paths in which a common service selects among the three reference workflows or a generic helper owns the complete workflow through source-read, mapping, application, and classification callbacks.
- **SC-007**: Existing tests are preserved or minimally relocated/adjusted, and zero duplicate full behavior matrices are added solely because reference-specific production classes move or split.
- **SC-008**: For representative applied, unchanged, controlled-skip, not-found, busy, transient-failure, permanent-failure, cancellation, and partial-import cases, diagnostics and caller-visible results retain all previously available information.

## Assumptions

- Features #104 and #109 are complete and their current behavior and tests form the compatibility baseline for this refactoring.
- The existing WMS import operations remain the authoritative application boundary for Warehouse, Unit of Measure, and SKU rules.
- The existing durable synchronization foundation remains the sole owner of intake, persistence, processing, retry, deferred handling, and abandoned-processing recovery.
- The current repository conventions will determine exact folder, file, class, and interface names during planning; the conceptual ownership areas in this specification do not prescribe those names.
- Controlled duplication is acceptable when it makes a complete reference workflow locally understandable and does not duplicate WMS business rules.
- Specification and subsequent implementation work remain on the already checked-out branch; no branch is created, switched, or renamed for Issue #111.
- All Issue #111 specification artifacts remain under `specs/111-onec-reference-vertical-slices/`.
- Command-based validation remains developer-controlled and will be performed only when explicitly requested.
