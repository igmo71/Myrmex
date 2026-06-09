# Myrmex Development Workflow

Durable workflow guidance for planning and implementation.

## Spec Kit Workflow

Use repository `$speckit-*` workflows for constitution, specification, plan,
task, analysis, and implementation work. Do not assume product-specific slash
commands.

## Feature Context

`AGENTS.md` is only an entry point. Durable rules live in `.specify/memory/`.
Feature-specific context belongs in `specs/<feature>/`.

When working on a feature, start with the current `specs/<feature>/plan.md` and
`tasks.md`. Read `spec.md`, `research.md`, `data-model.md`, contracts,
quickstarts, and checklists only when directly relevant to the task.

Do not pin durable memory or `AGENTS.md` to a completed feature.

## Execution Boundaries

Builds, tests, application startup, database updates, EF migration application,
EF migration generation, and infrastructure-affecting commands are
developer-controlled. Do not run them automatically.

Report recommended commands instead of executing them.

Migration work may happen only when the user explicitly requests it.

## Task Planning

Tasks must be small, reviewable, and ordered by dependency. Identify required
tests before implementation tasks that depend on them.

Task lists should state recommended validation commands for build, test,
startup, database, migration, and infrastructure checks. Report those commands
instead of executing them.

Separate UI implementation into its own phase when practical.

Documentation-only work must not include production code, test code, runtime
behavior, persistence, API, UI, or framework changes.

Prefer deleting obsolete guidance over carrying historical issue context in
durable memory.
