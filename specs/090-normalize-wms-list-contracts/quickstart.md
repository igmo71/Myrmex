# Static Audit Quickstart

## Inputs

1. Read `spec.md` and `plan.md` in this feature directory.
2. Use `.specify/memory/server-driven-list-slice-pattern.md` as the normative convention.
3. Use `docs/architecture/server-driven-list-slice-pattern.md` only for supporting detail.
4. Inspect current files cited by `research.md`; current code overrides historical notes.

## Review

For each slice, trace the public request through endpoint mapping, internal query, filters, filtered count, deterministic ordering, normalized paging, backend projection, shared result, API client, and grid. Record exact sort-key values and casing, default order, tie-breakers, cancellation, error conventions, visible warehouse values, and relevant tests.

Classify normalization as mechanical, focused-test work, or deferred. Prefer an accepted local pattern over a new abstraction.

## Static Validation

- Verify all report sections and all nine slices are present.
- Verify material claims cite precise paths.
- Verify no unresolved placeholders remain.
- Confirm the Git diff contains no application, test, resource, contract, schema, or migration changes.
- Do not build, test, run services, start infrastructure, or access/update the database.

Use `research.md` as decision input for later specification and task generation.
