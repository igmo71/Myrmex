# Repository Guidelines

## Project Structure & Module Organization

`Myrmex.slnx` contains the .NET 10 modular monolith. `Myrmex.AppHost` orchestrates Redis,
`Myrmex.ApiService`, and `Myrmex.WebApp` through Aspire. Warehouse behavior belongs in
`Myrmex.Modules.Wms`, grouped into vertical slices such as `Catalog`, `Inventory`,
`Receiving`, and `Topology`. `Myrmex.Identity` and `Myrmex.Integrations` own authentication
and external-system concerns. Put reusable transport contracts in `Myrmex.Shared`, core
abstractions in `Myrmex.Core`, and dispatch infrastructure in `Myrmex.AppDispatching`.

## Developer-Controlled Operations

Automated testing is disabled. Agents MUST NOT create, modify, or run unit, integration,
contract, regression, UI, or other tests; test projects; test infrastructure; fixtures;
coverage configuration; or test-only code.

Only the developer runs builds, generates or applies EF Core migrations, creates Git
commits, and creates or publishes pull requests. Agents MUST NOT execute or claim success
for commands such as `dotnet build Myrmex.slnx`, `dotnet ef migrations add <Name>`, or
`dotnet ef database update`. Agents may prepare commands, migration notes, commit messages,
pull request descriptions, and concise manual-verification steps, then review results
provided by the developer.

## Coding Style & Naming Conventions

Use four-space indentation, file-scoped namespaces, braces on new lines, and explicit
types where they improve readability. Use PascalCase for types, methods, and public
members; camelCase for parameters and locals; `_camelCase` for private fields; and the
`Async` suffix for asynchronous methods. Keep nullable reference types clean. Prefer
focused vertical-slice code and the internal command/query dispatchers over new broad
abstractions or MediatR.

## Architecture & Verification

`.specify/memory/constitution.md` governs domain boundaries, application contracts,
verification, simplicity, and operational requirements. Specifications retain
Given/When/Then acceptance scenarios and independently verifiable outcomes. Agents may
inspect code and document developer-performed manual verification, but automated tests
are outside the current process.

## Commit & Pull Request Guidance

Recent commits are concise and imperative, commonly prefixed with an issue number, for
example `#116 Fix receiving order filters.` Agents may suggest messages in that style.
Prepared PR descriptions should link the issue, summarize domain and architectural impact,
identify persistence or configuration changes, list developer-run verification, and
request screenshots for visible Blazor/MudBlazor changes.

## Security & Configuration

Never commit passwords, API keys, connection strings, or staging `.env` files. Use .NET
user secrets or environment variables with the existing `Myrmex__...` naming pattern.
