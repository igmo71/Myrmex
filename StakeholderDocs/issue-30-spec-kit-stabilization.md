# Issue 30: Spec Kit Stabilization for Myrmex WMS

## Context

Myrmex is an existing brownfield .NET WMS / fulfillment project.

The previous milestone completed issue #28:
"Add regression tests for WMS Topology vertical slice".

## Goal

Capture accepted Myrmex WMS architecture and domain decisions in Spec Kit.

## Important Tooling Context

Spec Kit examples often use GitHub Copilot Chat slash commands.

Myrmex uses Codex CLI as the primary coding/review assistant.

Spec Kit commands for Codex CLI are invoked through skills:

- `$speckit-constitution`
- `$speckit-specify`
- `$speckit-plan`
- `$speckit-tasks`
- `$speckit-analyze`
- `$speckit-implement`

Do not assume Copilot Pro or Copilot Enterprise features.

## Brownfield Rules

This is not greenfield development.

Do not start Catalog, SKU, Inventory, Receiving, or Integration implementation.

Do not change production code unless explicitly approved.

Do not perform broad refactoring.

## Accepted Architecture

- .NET / ASP.NET Core / Blazor / MudBlazor
- Modular Monolith
- Clean Architecture / DDD-inspired
- Vertical slices
- No MediatR
- Internal command/query/handler dispatching
- Simple explicit code over broad generic abstractions

## Current Reference Slice

WMS Topology is the current reference vertical slice.

Covered concepts:

- Warehouse
- Zone
- StorageLocation

## API Error Handling

Accepted convention:

- write/action operations return `ApiResult<T>`
- read/load operations use exception-based flow, ProblemDetails-aware

## Testing Expectations

Issue #28 added regression coverage for:

- domain tests
- application/handler tests
- WMS topology API client error-handling tests

Query handler tests for GetById/List are intentionally out of scope and may become a future issue.

## Desired Spec Kit Stabilization Outputs

Capture:

- architecture principles
- development workflow
- WMS ubiquitous language
- WMS Topology patterns
- UI component patterns
- API error-handling patterns
- testing expectations
- roadmap direction

Roadmap direction only:

- Catalog
- SKU
- Barcode
- UoM
- Packaging
- Inventory
- Receiving
- Integration

## Forbidden in This Issue

- production feature implementation
- Catalog/SKU implementation
- Inventory implementation
- Receiving implementation
- Integration implementation
- large rewrite
- unrelated refactoring
- new architectural frameworks