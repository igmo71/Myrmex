# Myrmex Development Workflow

Durable workflow guidance for planning and implementation.

## Spec Kit Workflow

Use the repository's Spec Kit workflow for:

- constitution updates;
- feature specifications;
- implementation plans;
- task lists;
- cross-artifact analysis;
- task execution.

Do not assume product-specific slash-command support.

## Feature Context

`AGENTS.md` is only an entry point. Durable rules live in `.specify/memory/`.
Feature-specific context belongs in `specs/<feature>/`.

When working on a feature, read the current `specs/<feature>/spec.md`,
`plan.md`, `tasks.md`, and related artifacts when present. Do not pin durable
memory or `AGENTS.md` to a completed feature.

## Execution Boundaries

Builds, tests, application startup, database updates, EF migration application,
and infrastructure-affecting commands are performed manually by the developer.
Do not run them automatically.

Report recommended commands instead of executing them.

EF migration generation is a separate explicit step. Do not generate migrations
unless the user or an approved task asks for that step directly.

## Task Planning

Tasks must be small, reviewable, and ordered by dependency. Identify required
tests before implementation tasks that depend on them.

Separate UI implementation into its own phase when practical.

Documentation-only work must not include production code, test code, runtime
behavior, persistence, API, UI, or framework changes.

Prefer deleting obsolete guidance over carrying historical issue context in
durable memory.
