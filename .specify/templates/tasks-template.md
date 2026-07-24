---

description: "Task list template for feature implementation"
---

# Tasks: [FEATURE NAME]

**Input**: `spec.md` and required `plan.md`; optional supporting artifacts only when present.

**Policy**: Generate feature-specific implementation tasks only. Automated tests and
developer-controlled operations are never tasks.

## Format: `[ID] [P?] [Story?] Description with exact repository path`

- **[P]**: Can run in parallel because it affects different files with no unmet dependency.
- **[Story]**: Required for a user-story phase, for example `[US1]`.
- Every generated task MUST name an exact repository path; no generic `src/` paths.

## Conditional Phases

Include **Setup** only when the feature genuinely requires feature-specific project or
configuration setup. Include **Foundational** only for shared prerequisites that block
the feature's user stories. Do not emit empty, ceremonial, or brownfield-inappropriate
phases.

<!--
  Replace all examples. Never retain placeholder tasks in generated tasks.md.
  Do not create tasks for tests, builds, migration generation/application, commits,
  pull requests, or publication.
-->

## Phase 1: User Story 1 - [Title] (Priority: P1)

**Goal**: [Outcome delivered by this story]

**Independent Verification**: [Concise developer-performed observation]

- [ ] T001 [P] [US1] Add [Contract] in Myrmex.Shared/[Capability]/[Contract].cs
- [ ] T002 [P] [US1] Add [Entity] behavior in Myrmex.Modules.Wms/[Capability]/Domain/[Entity].cs
- [ ] T003 [US1] Implement [Handler] in Myrmex.Modules.Wms/[Capability]/Application/[Handler].cs
- [ ] T004 [US1] Map the endpoint in Myrmex.Modules.Wms/[Capability]/Endpoints/[Feature]Endpoints.cs
- [ ] T005 [US1] Implement the UI flow in Myrmex.WebApp/Components/Pages/[Capability]/[Page].razor

**Checkpoint**: [How the developer can observe this story independently]

---

## Phase 2: User Story 2 - [Title] (Priority: P2)

**Goal**: [Outcome delivered by this story]

**Independent Verification**: [Concise developer-performed observation]

- [ ] T006 [P] [US2] Update [Contract] in Myrmex.Shared/[Capability]/[Contract].cs
- [ ] T007 [US2] Implement [Handler] in Myrmex.Modules.Wms/[Capability]/Application/[Handler].cs
- [ ] T008 [US2] Update [Page] in Myrmex.WebApp/Components/Pages/[Capability]/[Page].razor

**Checkpoint**: [How the developer can observe this story independently]

---

[Add only phases required by the specification and plan.]

## Dependencies & Execution Order

- [List real task or story dependencies.]
- [Identify genuine parallel work using task IDs.]
- [Do not assume Setup or Foundational phases exist.]

## Developer Actions

<!--
  Include only applicable actions. These are non-executable handoff notes:
  no task IDs, no checkboxes, and no claim that an agent completed them.
-->

- Build the affected projects or solution.
- Generate and review EF Core migrations.
- Apply database migrations.
- Perform manual acceptance.
- Create the Git commit.
- Create or publish the pull request.

## Notes

- Agents implement only the numbered feature tasks.
- Agents may prepare commands and descriptions for Developer Actions.
- Remove inapplicable Developer Actions from the generated file.
- Avoid vague work, missing paths, and unnecessary cross-story dependencies.
