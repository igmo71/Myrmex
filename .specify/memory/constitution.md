<!--
Sync Impact Report
- Version change: 1.0.0 → 2.0.0
- Modified principles:
  - IV. Verification Is Part of Delivery → IV. Developer-Controlled Verification
- Preserved principles:
  - I. Domain Integrity First
  - II. Modular Boundaries and Vertical Slices
  - III. Explicit Application Contracts
  - V. Simplicity with Operational Discipline
- Added sections: none
- Removed sections: none
- Changed governance:
  - Removed automated-testing mandates
  - Reserved build, migration, commit, and pull request operations for the developer
  - Required prohibited extension hooks to be skipped even when marked mandatory
- Templates and guidance:
  - ✅ updated: .specify/templates/plan-template.md
  - ✅ updated: .specify/templates/spec-template.md
  - ✅ updated: .specify/templates/tasks-template.md
  - ✅ updated: .specify/templates/constitution-template.md
  - ✅ updated: AGENTS.md
  - ✅ updated: all .agents/skills/speckit-*/SKILL.md files
  - ✅ reviewed, no change required: README.md
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

### IV. Developer-Controlled Verification

Specifications MUST retain clear acceptance scenarios and independently verifiable
outcomes. Automated testing is excluded from the current Myrmex development process.
Agents MUST NOT create or modify test projects, test code, test infrastructure, fixtures,
test packages, coverage configuration, or other test-only artifacts, and MUST NOT execute
tests. Agents MAY document concise manual verification for the developer to perform and
MAY review developer-provided results. Automated testing MAY return only through a future
constitution amendment.

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
- Nullable reference types and implicit usings remain enabled. Agents MUST preserve
  compile-time correctness through inspection and developer-provided build results.
- Schema changes MUST describe persistence and migration impact. The developer exclusively
  generates, reviews, and applies EF Core migrations.
- Shared contracts MUST remain serialization-safe and MUST NOT expose EF Core entities.
- Authentication, authorization, external integration failures, atomicity, and
  observability MUST be addressed wherever a feature touches those concerns.

## Delivery Workflow and Quality Gates

Specifications MUST define acceptance scenarios and independently verifiable outcomes.
Plans MUST pass the Constitution Check and use only the supporting artifacts that add
concrete value. Tasks MUST contain feature-specific implementation work with exact
repository paths. Build, migration generation, database update, Git commit, and pull
request creation or publication are exclusively developer-controlled operations. Plans
and tasks MUST represent applicable developer-controlled operations as non-executable
handoff notes, never as agent tasks, task checkboxes, or agent completion criteria.
Agents MAY prepare commands, migration notes, commit messages, pull request descriptions,
and manual-verification steps, and MAY review results supplied by the developer. Agents
MUST NOT execute these developer-controlled operations or claim that they succeeded.

## Governance

This constitution supersedes conflicting repository guidance and extension hooks. A hook
that would build, run tests, generate or apply migrations, create a commit, or create or
publish a pull request MUST be skipped and reported as developer-controlled, even when
the hook is marked mandatory. Amendments require documented rationale, semantic version
impact, migration implications, and synchronization of dependent templates and guidance.
Versions follow semantic versioning: MAJOR removes or incompatibly redefines governance,
MINOR adds or materially expands obligations, and PATCH clarifies without changing
obligations. Every plan and review MUST verify applicable MUST statements; an unexplained
violation blocks agent completion. `AGENTS.md` provides day-to-day guidance but cannot
weaken this constitution.

**Version**: 2.0.0 | **Ratified**: 2026-07-24 | **Last Amended**: 2026-07-24
