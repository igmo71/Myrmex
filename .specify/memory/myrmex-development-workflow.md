# Myrmex Development Workflow Memory

This document is durable operational guidance for Myrmex Spec Kit and agent workflow.

## Supported Assistant Workflow

Myrmex uses Codex CLI skills for Spec Kit work:

- `$speckit-constitution`
- `$speckit-specify`
- `$speckit-plan`
- `$speckit-tasks`
- `$speckit-analyze`
- `$speckit-implement`

Do not assume Copilot Pro, Copilot Enterprise, or Copilot Chat slash-command capabilities.

## Active Plan Context

`AGENTS.md` should point agents to durable `.specify/memory/myrmex-*.md` documents. When working on an active Spec Kit feature, also read that feature's current `specs/<feature>/plan.md`.

Do not leave `AGENTS.md` permanently pinned to an old feature plan after that issue is completed or merged.

## Issue #30 Implementation Meaning

For issue #30, `$speckit-implement` is allowed only as documentation execution from a docs-only `tasks.md`.

For issue #30, "implement" means creating or updating documentation artifacts only. It does not allow production code, test code, runtime behavior, migrations, UI, API, persistence, or framework changes.

## Task Generation Guard

Tasks for issue #30 must not include:

- Production code changes.
- Test code changes.
- Catalog/SKU implementation.
- Inventory implementation.
- Receiving implementation.
- Integration implementation.
- Broad refactoring.
- New frameworks.
- GetById/List query handler tests.
- Runtime behavior, migration, UI, API, persistence, or framework changes.
