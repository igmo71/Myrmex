<!--
Sync Impact Report
- Version change: unratified template → 1.0.0
- Modified principles:
  - Placeholder Principle 1 → I. Domain Integrity First
  - Placeholder Principle 2 → II. Modular Boundaries and Vertical Slices
  - Placeholder Principle 3 → III. Explicit Application Contracts
  - Placeholder Principle 4 → IV. Verification Is Part of Delivery
  - Placeholder Principle 5 → V. Simplicity with Operational Discipline
- Added sections:
  - Architecture and Technology Constraints
  - Delivery Workflow and Quality Gates
- Removed sections: none; template placeholders were instantiated
- Templates and guidance:
  - ✅ updated: .specify/templates/plan-template.md
  - ✅ updated: .specify/templates/spec-template.md
  - ✅ updated: .specify/templates/tasks-template.md
  - ✅ updated: .agents/skills/speckit-tasks/SKILL.md
  - ✅ updated: AGENTS.md
  - ✅ reviewed, no change required: README.md
  - ✅ reviewed, no change required: remaining .agents/skills/speckit-*/SKILL.md files
- Follow-up TODOs: none
-->
# Myrmex Constitution

## Core Principles

### I. Domain Integrity First

The domain model MUST be the authoritative expression of warehouse concepts, invariants,
and state transitions. Business rules MUST live in domain or application code, not in UI,
HTTP endpoint, or persistence adapters. State-changing operations, especially inventory
posting and quantity movement, MUST validate invariants and commit atomically. Names in
code, contracts, and specifications MUST use the same domain language. This keeps
warehouse behavior correct and understandable independently of delivery technology.

### II. Modular Boundaries and Vertical Slices

Myrmex MUST remain a modular monolith organized around business capabilities. A feature
MUST be implemented in its owning module and vertical slice, keeping domain, application,
endpoint, and infrastructure responsibilities distinct. Modules MUST NOT read or mutate
another module's persistence directly; cross-module behavior MUST use explicit contracts,
commands, queries, or domain events. `Myrmex.Shared` MUST contain transport contracts, not
business logic, and `Myrmex.Core` MUST contain only genuinely cross-cutting abstractions.
These boundaries allow capabilities to evolve without accidental coupling.

### III. Explicit Application Contracts

State changes MUST be represented as explicit commands and reads as explicit queries,
dispatched through the repository's internal dispatchers. Minimal API endpoints and
Blazor components MUST remain thin orchestrators. Public DTOs MUST be distinct from
persistence entities and domain internals. Contract changes MUST document compatibility
impact, and breaking changes MUST include a migration path. New mediation or repository
frameworks MUST NOT replace the existing direct patterns without a documented,
constitution-approved need.

### IV. Verification Is Part of Delivery

Every behavior change MUST have repeatable verification tied to acceptance scenarios.
Automated tests MUST cover new or changed domain invariants and application handlers;
integration tests MUST cover affected EF Core mappings, migrations, module boundaries,
and HTTP contracts. A defect fix MUST add a regression test when the failure is
reproducible. Tests MUST be created before or alongside implementation and MUST run
through `dotnet test Myrmex.slnx`. Until a test project exists for an affected area, its
creation is a required foundational task, not optional follow-up work.

### V. Simplicity with Operational Discipline

Implementations MUST choose the smallest design that satisfies current requirements.
Abstractions, dependencies, and infrastructure MUST have a demonstrated use case; future
possibilities alone are not justification. Services MUST preserve health checks,
structured diagnostics, and Aspire service defaults at operational boundaries. Secrets
MUST come from user secrets, environment variables, or managed configuration and MUST
NOT enter source control. Simplicity reduces accidental complexity while operational
signals and secure configuration keep the system supportable.

## Architecture and Technology Constraints

- The baseline stack is .NET 10, ASP.NET Core Minimal APIs, EF Core with SQL Server,
  Aspire, and Blazor with MudBlazor. A plan MAY deviate only with explicit rationale.
- Nullable reference types and implicit usings remain enabled. New code MUST compile
  without introducing warnings in touched projects.
- Schema changes MUST include reviewed EF Core migrations and a safe upgrade path.
- Shared contracts MUST remain serialization-safe and MUST NOT expose EF Core entities.
- Authentication, authorization, external integration failures, atomicity, and
  observability MUST be addressed wherever a feature touches those concerns.

## Delivery Workflow and Quality Gates

Features MUST begin with a specification containing prioritized, independently testable
user stories and measurable acceptance outcomes. Plans MUST pass the Constitution Check
before research and again after design. Tasks MUST be dependency ordered, include exact
repository paths, and include required automated verification before implementation.
Pull requests MUST explain the domain and architectural impact, link the governing issue,
identify migrations or configuration changes, and report `dotnet build Myrmex.slnx` and
`dotnet test Myrmex.slnx` results. UI changes MUST include visual evidence. Any justified
exception MUST be recorded in the plan's Complexity Tracking table before implementation.

## Governance

This constitution supersedes conflicting repository guidance. Amendments require a pull
request that states the rationale, semantic version impact, migration implications, and
updates to dependent Spec Kit templates and runtime guidance. Versions follow semantic
versioning: MAJOR removes or incompatibly redefines governance, MINOR adds or materially
expands obligations, and PATCH clarifies without changing obligations. Every plan and
pull request review MUST verify applicable MUST statements; an unexplained violation
blocks approval. `AGENTS.md` provides day-to-day contributor guidance but cannot weaken
this constitution.

**Version**: 1.0.0 | **Ratified**: 2026-07-24 | **Last Amended**: 2026-07-24
