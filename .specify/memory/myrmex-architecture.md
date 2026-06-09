# Myrmex Architecture

Durable solution and module structure guidance.

## Module Boundaries

Use the existing repository boundaries:

- `Myrmex.Core` for shared kernel code.
- `Myrmex.AppDispatching` for cross-cutting dispatching.
- `Myrmex.AspNetCore` for ASP.NET helpers.
- `Myrmex.Modules.Wms` for WMS capabilities.
- `Myrmex.ApiService`, `Myrmex.WebApp`, and host projects for their existing application roles.

Future module changes must preserve these boundaries unless a separate approved plan documents the reason to diverge.

## Local Pattern Guidance

Use existing internal dispatching patterns for commands, queries, and domain events. Keep cross-module communication explicit through public module registration, API contracts, commands, queries, or events.

Prefer simple explicit code over broad generic abstractions. New abstractions must solve a current WMS problem and match existing local patterns.
