# Repository Guidelines

## Project Structure & Module Organization

`Myrmex.slnx` contains the .NET 10 modular monolith. `Myrmex.AppHost` is the Aspire development entry point and orchestrates Redis, `Myrmex.ApiService`, and `Myrmex.WebApp`. Warehouse behavior lives in `Myrmex.Modules.Wms`, grouped by vertical slices such as `Catalog`, `Inventory`, `Receiving`, and `Topology`; keep domain, application, endpoint, and persistence concerns in the appropriate slice. `Myrmex.Identity` and `Myrmex.Integrations` own authentication and external-system concerns. Reusable contracts belong in `Myrmex.Shared`, core abstractions in `Myrmex.Core`, and dispatch infrastructure in `Myrmex.AppDispatching`. Deployment and migration helpers are under `scripts/`.

## Build, Test, and Development Commands

- `dotnet restore Myrmex.slnx` restores NuGet dependencies.
- `dotnet build Myrmex.slnx` compiles the complete solution with nullable analysis enabled.
- `dotnet run --project Myrmex.AppHost` starts the local Aspire stack and its dashboard.
- `dotnet format Myrmex.slnx --verify-no-changes` checks SDK formatting before review.
- `dotnet test Myrmex.slnx` runs all test projects once tests are present.

Configure the `MyrmexDatabase` connection string before running the stack.

## Coding Style & Naming Conventions

Follow established C# style: four-space indentation, file-scoped namespaces, braces on new lines, and explicit types where they improve readability. Use PascalCase for types, methods, and public members; camelCase for parameters and locals; `_camelCase` for private fields; and the `Async` suffix for asynchronous methods. Keep nullable reference types clean rather than suppressing warnings. Prefer focused vertical-slice code and the repository's internal command/query dispatchers over introducing broad abstractions or MediatR.

## Testing Guidelines

No test project or framework is currently configured. The constitution requires automated verification, so create affected-area test infrastructure as foundational work. Use projects named `Myrmex.<Area>.Tests`, mirror production namespaces, and name tests after observable behavior. Cover domain invariants and application handlers first; use integration tests for EF Core mappings, migrations, and HTTP endpoints. Ensure `dotnet test Myrmex.slnx` passes before submitting.

## Architecture Governance

`.specify/memory/constitution.md` is the authority for domain boundaries, application contracts, verification, simplicity, and operational requirements. Plans and pull requests must pass its applicable gates; document justified exceptions in the plan's Complexity Tracking table.

## Commit & Pull Request Guidelines

Recent commits are concise and imperative, commonly prefixed with an issue number, for example `#116 Fix receiving order filters.` Keep each commit focused. Pull requests should link the issue, summarize behavior and architectural impact, list verification commands, and call out schema migrations or configuration changes. Include screenshots for visible Blazor/MudBlazor changes.

## Security & Configuration

Do not commit passwords, API keys, connection strings, or staging `.env` files. Store local secrets with .NET user secrets or environment variables using the existing `Myrmex__...` naming pattern.
