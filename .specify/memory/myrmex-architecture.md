# Myrmex Architecture Memory

This document is durable operational guidance for Myrmex agents and maintainers. It supersedes issue #30 stakeholder notes as day-to-day architecture guidance while preserving those notes as historical input.

## Architecture Baseline

Myrmex is a brownfield .NET WMS / fulfillment project. It uses:

- Modular monolith architecture.
- Clean Architecture and DDD-inspired structure.
- Vertical slices.
- ASP.NET Core Minimal APIs.
- EF Core.
- Aspire.
- Blazor and MudBlazor.
- Internal command/query/handler dispatching.

Myrmex does not use MediatR. Do not introduce MediatR or another architectural framework without a separate approved issue and plan.

## Module Boundaries

Use the existing repository boundaries:

- `Myrmex.Core` for shared kernel code.
- `Myrmex.AppDispatching` for cross-cutting dispatching.
- `Myrmex.AspNetCore` for ASP.NET helpers.
- `Myrmex.Modules.Wms` for WMS capabilities.
- `Myrmex.ApiService`, `Myrmex.WebApp`, and host projects for their existing application roles.

Future module changes must preserve these boundaries unless a separate approved plan documents the reason to diverge.

## Style

Prefer simple explicit code over broad generic abstractions. New abstractions must solve a current WMS problem and match existing local patterns.

## Issue #30 Guardrails

Issue #30 is documentation-only. It must not change production code, test code, runtime behavior, migrations, UI, API, persistence, or frameworks.
