# Feature Specification: [FEATURE NAME]

**Feature Branch**: `[###-feature-name]`

**Created**: [DATE]

**Status**: Draft

**Input**: User description: "$ARGUMENTS"

## User Scenarios & Verification *(mandatory)*

<!--
  Describe user journeys in priority order when the feature contains more than one.
  Each scenario must be independently verifiable and provide a useful outcome.
  Do not require or imply automated tests.
-->

### User Story 1 - [Brief Title] (Priority: P1)

[Describe the user journey in plain language]

**Why this priority**: [Explain its user or domain value]

**Independent Verification**: [Concise developer-performed observation that proves the
outcome without prescribing an automated test]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]
2. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

### User Story 2 - [Brief Title] (Priority: P2)

[Describe the user journey in plain language]

**Why this priority**: [Explain its user or domain value]

**Independent Verification**: [How the outcome can be observed independently]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

[Add more user stories only when required by the supplied feature scope]

### Edge Cases

- What happens when [boundary condition]?
- How does the system handle [error scenario]?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST [specific capability]
- **FR-002**: System MUST [specific validation or behavior]
- **FR-003**: Authorized users MUST be able to [key interaction]

Use `[NEEDS CLARIFICATION: specific question]` only when no safe repository-informed
default exists.

### Domain Rules & State Transitions *(include when state changes)*

- **DR-001**: [Invariant or transition in domain language]
- **DR-002**: [Atomicity, concurrency, or failure behavior]

### Quality Attributes *(include only supplied or accepted requirements)*

- **QA-001 Security**: [Authorization or sensitive-data requirement]
- **QA-002 Reliability**: [Failure, atomicity, or consistency requirement]
- **QA-003 Observability**: [Required operational signal]

Do not invent performance targets, satisfaction percentages, business KPIs, adoption
metrics, or SLAs. Include a numeric or service-level target only when the user supplied it
or explicitly accepted it as a requirement.

### Key Entities *(include if feature involves data)*

- **[Entity 1]**: [Meaning, key attributes, and relationships]

## Verification Outcomes *(mandatory)*

Define observable, technology-agnostic outcomes that trace to the acceptance scenarios.
Do not manufacture metrics.

- **VO-001**: [Observable outcome supported by a supplied or accepted requirement]
- **VO-002**: [Observable failure or boundary outcome]

## Assumptions

- [Repository-informed scope or environment assumption]
- [Dependency on an existing module or external system]
