# Myrmex Development Workflow

Durable workflow guidance for planning and implementation.

## Spec Kit Workflow

Use repository `$speckit-*` workflows for constitution, specification, plan, task, analysis, and implementation work. Do not assume product-specific slash commands.

## Feature Context

`AGENTS.md` is only an entry point. Durable rules live in `.specify/memory/`.
Feature-specific context belongs in `specs/<feature>/`.

When working on a feature, start with the current `specs/<feature>/plan.md` and `tasks.md`. Read `spec.md`, `research.md`, `data-model.md`, contracts, quickstarts, and checklists only when directly relevant to the task.

For WebApp work that adds or changes user-facing text, follow [WebApp localization conventions](webapp-localization.md).

For backend-owned WebApp/API lists with filtering, sorting, paging, and total counts, follow the [server-driven list slice pattern](server-driven-list-slice-pattern.md).

Do not pin durable memory or `AGENTS.md` to a completed feature.

## Execution Boundaries

Builds, tests, application startup, database updates, EF migration application, EF migration generation, and infrastructure-affecting commands are developer-controlled. Do not run them automatically.

Report recommended commands instead of executing them.

Migration work may happen only when the user explicitly requests it.

## Development Database Workflow

The normal development database is an external or deployed SQL Server database supplied through `ConnectionStrings:MyrmexDatabase`, typically with .NET User Secrets. The AppHost references this existing connection string and passes the same `MyrmexDatabase` resource to WebApp and ApiService.

EF migrations are developer-controlled. Runtime startup must not apply migrations, call `EnsureCreated`, or silently create Identity/Data Protection schema. Identity and WMS schema changes must be generated, reviewed, and applied explicitly by the developer.

An Aspire-managed SQL Server container may be used only as an explicitly isolated sandbox path, not as the default foundation for normal development. If a sandbox path is added, it must not replace or obscure the external `MyrmexDatabase` connection-string workflow.

AppHost smoke tests are infrastructure smoke tests. They require prepared database state, valid connection-string secrets, and any required Data Protection certificate configuration before execution; failures in those prerequisites are not Identity foundation failures.

## Identity/Auth Test Classification

When stabilizing Identity authentication work, classify failures before changing security behavior:

- Compile/build errors: fix directly.
- Identity boundary tests: preserve the WebApp application-cookie to ApiService `Myrmex.ApiSession` boundary and exact GUID actor contract.
- Invalid or missing `ClaimTypes.NameIdentifier`: update test principals only when the test is meant to represent an authenticated stable Identity user; otherwise keep the negative case.
- Missing `WmsOperator`/`MyrmexAdmin` roles: add explicit role claims only where the test is meant to exercise a protected endpoint as an authorized user.
- AppHost/infrastructure smoke tests: keep marked as infrastructure smoke and validate only with prepared external database/infrastructure state.

## Task Planning

Tasks must be small, reviewable, and ordered by dependency. Identify required tests before implementation tasks that depend on them.

Task lists should state recommended validation commands for build, test, startup, database, migration, and infrastructure checks.

Separate UI implementation into its own phase when practical.

Documentation-only work must not include production code, test code, runtime behavior, persistence, API, UI, or framework changes.

Prefer deleting obsolete guidance over carrying historical issue context in durable memory.
