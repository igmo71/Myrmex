<!--
Sync Impact Report
Version change: 1.0.0 -> 1.0.1
Modified principles:
- IV. Tests Protect Domain and Integration Behavior: clarified endpoint/UI automation expectations and exception requirements
Added sections:
- None
Removed sections:
- None
Templates requiring updates:
- ✅ updated: .specify/templates/plan-template.md
- ✅ verified: .specify/templates/spec-template.md
- ✅ updated: .specify/templates/tasks-template.md
- ✅ verified: .specify/templates/checklist-template.md
- ✅ not present: .specify/templates/commands/
- ✅ updated: .specify/memory/myrmex-testing-guidelines.md
Follow-up TODOs:
- None
-->

# Myrmex Constitution

## Core Principles

### I. Domain Model First
Myrmex features MUST start from explicit WMS and fulfillment domain concepts before
technical implementation details are chosen. Entities, value objects, commands,
queries, and events MUST use business language from warehouse topology, receiving,
storage, picking, shipping, inventory, and analytics processes. Domain invariants
MUST live in the domain model or application handlers, not in UI-only validation or
database-only constraints.

Rationale: the project exists to explore a clear, extensible WMS domain model.
Technical shortcuts that hide domain rules make later fulfillment flows harder to
extend safely.

### II. Modular Monolith Boundaries
The system MUST remain a modular monolith unless a feature plan documents a
constitution-approved reason to split a capability. Shared kernel code belongs in
`Myrmex.Core`, cross-cutting dispatching in `Myrmex.AppDispatching`, ASP.NET
helpers in `Myrmex.AspNetCore`, and WMS capabilities in `Myrmex.Modules.Wms`.
Modules MUST communicate through explicit commands, queries, events, public
module registration, or API contracts. Direct dependencies that bypass these
boundaries MUST be justified in the feature plan.

Rationale: Myrmex is intended to validate Clean Architecture, DDD, modular
monolith, and vertical-slice practices without introducing distributed-system
complexity prematurely.

### III. Vertical Slices with Explicit Requests
User-facing behavior MUST be delivered as independently understandable vertical
slices: endpoint, request/response contracts, command or query handler, domain
logic, persistence mapping, and UI/client integration where applicable. Commands
and queries MUST be explicit types handled through the internal dispatcher pattern.
New slices MUST preserve existing API and UI behavior unless the plan identifies a
breaking change and migration path.

Rationale: vertical slices keep WMS workflows small enough to test and evolve while
still exercising the full architecture.

### IV. Tests Protect Domain and Integration Behavior
Domain invariants, command/query handlers, persistence mappings, and API clients
MUST have automated tests when changed or introduced. Tests MUST cover invalid
inputs, inactive/reactivated states, identity and uniqueness rules, and persistence
error mapping where those behaviors apply.

HTTP endpoint integration tests and UI/component tests SHOULD be included when
suitable project test infrastructure already exists and the behavior cannot be
adequately protected by lower-level automated tests. This principle does not
require bUnit, Playwright, WebApplicationFactory, or any new test framework by
default.

A feature plan MAY document an exception for endpoint or UI automated tests when
those tests would require new frameworks, broad test-host infrastructure, or
setup disproportionate to the issue scope. The exception MUST state why automated
endpoint or UI tests are deferred, which lower-level automated tests protect the
same business behavior, what manual validation is required, and whether a
follow-up issue is needed.

Rationale: warehouse workflows depend on stable state transitions and reference
data. Tests are the guardrail that allows iterative expansion without weakening
existing topology behavior.

### V. Pragmatic Simplicity and Observability
Implementation MUST avoid broad abstractions, external frameworks, or service
splits that do not solve a current WMS problem. Existing local patterns take
precedence over new architectural styles. Public endpoints and operationally
important handlers MUST expose clear result shapes, meaningful errors, health
checks, and logging or diagnostics sufficient to troubleshoot failures.

Rationale: the project deliberately minimizes accidental complexity while still
needing enough observability to diagnose WMS operations as the system grows.

## Architecture Constraints

Myrmex is a .NET modular-monolith WMS and fulfillment platform. The default stack
is .NET, ASP.NET Core Minimal APIs, EF Core, Aspire, Blazor, xUnit, and the
project's internal command/query/domain-event dispatchers. Feature plans MUST use
these technologies and repository conventions unless they document a concrete
reason to diverge.

Persistence changes MUST include EF Core model configuration and migrations when
schema changes are required. Public API changes MUST preserve consistent service
result and problem-details behavior. UI changes MUST use the existing Blazor
structure and API client patterns unless the plan approves a replacement.

## Development Workflow

Specifications MUST describe independently testable user stories and measurable
success criteria. Plans MUST complete the Constitution Check before research and
again after design. Tasks MUST be grouped by user story, include exact file paths,
and identify test tasks required by Principle IV before implementation tasks that
depend on them.

Implementation MUST proceed in small, reviewable increments. Each increment MUST
build successfully, preserve existing tests, and include new tests for changed
domain rules, handlers, persistence behavior, and API clients. Endpoint and UI
test coverage MUST follow Principle IV's infrastructure and exception rules.

## Governance

This constitution supersedes conflicting project guidance for architecture,
planning, and implementation. Amendments MUST update this file, include a Sync
Impact Report, and propagate any changed rules to Spec Kit templates and runtime
guidance. Changes MUST use semantic versioning:

- MAJOR: incompatible principle removals or redefinitions.
- MINOR: new principles, new governance sections, or materially expanded guidance.
- PATCH: clarifications, wording improvements, or non-semantic corrections.

Every feature plan and review MUST verify compliance with the Core Principles.
Any exception MUST be documented in the plan's Complexity Tracking table with the
reason, rejected simpler alternative, and migration or rollback expectation.

**Version**: 1.0.1 | **Ratified**: 2026-06-04 | **Last Amended**: 2026-06-08
