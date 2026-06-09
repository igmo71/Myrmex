<!--
Sync Impact Report
Version change: 1.0.2 -> 1.0.3
Modified principles:
- I. Domain Model First: updated rationale for operational WMS use
- II. Modular Monolith Boundaries: updated rationale for maintainability
- III. Vertical Slices with Explicit Requests: removed migration execution detail
- IV. Tests Protect Domain and Integration Behavior: reduced to principle-level rule
Added sections:
- None
Removed sections:
- None
Templates requiring updates:
- Not reviewed in this documentation cleanup
Follow-up TODOs:
- None
-->

# Myrmex Constitution

## Core Principles

### I. Domain Model First
Myrmex features MUST start from explicit WMS and fulfillment domain concepts before technical implementation details are chosen. Entities, value objects, commands, queries, and events MUST use business language from warehouse topology, receiving, storage, picking, shipping, inventory, and analytics processes. Domain invariants MUST live in the domain model or application handlers, not in UI-only validation or database-only constraints.

Rationale: Myrmex is a coherent WMS and fulfillment platform for real operational use. Hidden domain rules make warehouse workflows harder to operate, maintain, and extend safely.

### II. Modular Monolith Boundaries
The system MUST remain a modular monolith unless a feature plan documents a constitution-approved reason to split a capability. Modules MUST communicate through explicit commands, queries, events, public module registration, or API contracts. Direct dependencies that bypass these boundaries MUST be justified in the feature plan.

Rationale: clear module boundaries keep Myrmex maintainable and extensible while avoiding unnecessary distributed-system complexity.

### III. Vertical Slices with Explicit Requests
User-facing behavior MUST be delivered as independently understandable vertical slices: endpoint, request/response contracts, command or query handler, domain logic, persistence mapping, and UI/client integration where applicable. Commands and queries MUST be explicit types handled through the internal dispatcher pattern. New slices MUST preserve existing API and UI behavior unless the plan identifies a breaking change and migration path.

Rationale: vertical slices keep WMS workflows small enough to test and evolve while still exercising the full architecture.

### IV. Tests Protect Domain and Integration Behavior
Changed domain rules, handlers, persistence mappings, and API clients MUST have appropriate automated coverage. Detailed coverage expectations belong in durable testing guidelines and feature plans.

Rationale: warehouse workflows depend on stable state transitions, reference data, and integration contracts.

### V. Pragmatic Simplicity and Observability
Implementation MUST avoid broad abstractions, external frameworks, or service splits that do not solve a current WMS problem. Existing local patterns take precedence over new architectural styles. Public endpoints and operationally important handlers MUST expose clear result shapes, meaningful errors, health checks, and logging or diagnostics sufficient to troubleshoot failures.

Rationale: the project deliberately minimizes accidental complexity while still needing enough observability to diagnose WMS operations as the system grows.

## Architecture Constraints

Myrmex is a .NET modular-monolith WMS and fulfillment platform for operational use. The default stack is .NET, ASP.NET Core Minimal APIs, EF Core, Aspire, Blazor, xUnit, and the project's internal command/query/domain-event dispatchers. Feature plans MUST use these technologies and repository conventions unless they document a concrete reason to diverge.

Persistence changes MUST include EF Core model configuration when schema changes are required. Public API changes MUST preserve consistent service result and problem-details behavior. UI changes MUST use the existing Blazor structure and API client patterns unless the plan approves a replacement.

## Development Workflow

Specifications MUST describe independently testable user stories and measurable success criteria. Plans MUST complete the Constitution Check before research and again after design. Tasks MUST be small, reviewable, dependency-ordered, and grouped by user story.

## Governance

This constitution supersedes conflicting project guidance for architecture, planning, and implementation. Amendments MUST update this file, include a Sync Impact Report, and propagate any changed rules to Spec Kit templates and runtime guidance. Changes MUST use semantic versioning:

- MAJOR: incompatible principle removals or redefinitions.
- MINOR: new principles, new governance sections, or materially expanded guidance.
- PATCH: clarifications, wording improvements, or non-semantic corrections.

Every feature plan and review MUST verify compliance with the Core Principles. Any exception MUST be documented in the plan's Complexity Tracking table with the reason, rejected simpler alternative, and migration or rollback expectation.

**Version**: 1.0.3 | **Ratified**: 2026-06-04 | **Last Amended**: 2026-06-09
