# Myrmex Testing Guidelines Memory

This document records accepted testing expectations for Myrmex planning.

## Existing Issue #28 Expectations

Issue #28 added regression coverage for the WMS Topology vertical slice. The accepted coverage categories are:

- Domain tests.
- Application/handler tests.
- WMS topology API client error-handling tests.

## Issue #30 Scope

Issue #30 documents testing expectations only. It must not add, remove, or modify automated tests.

GetById/List query handler tests are intentionally out of scope for issue #30 and may become future work in a separate approved issue.

## Reference-Data Slice Coverage

Catalog/SKU from issue #32 is the current representative reference-data vertical slice. Future CRUD-style reference-data slices SHOULD reuse the established pattern instead of duplicating the full SKU-level test matrix by default.

A repeated reference-data slice SHOULD use focused coverage when it follows an already accepted domain, handler, persistence, API client, and UI pattern. It MUST still add targeted automated tests for genuinely new behavior.

Treat a repeated slice as a new representative pattern when it introduces or changes:

- domain invariants, lifecycle rules, or idempotency behavior;
- uniqueness, normalization, concurrency, soft-delete, or persistence mapping behavior;
- API result shapes, ProblemDetails/status-code conventions, or endpoint composition patterns;
- API client parsing, error handling, result-envelope behavior, or cancellation behavior;
- UI validation, navigation, lifecycle actions, or component interaction patterns;
- test infrastructure or materially different testing approach.

For repeated reference-data slices, plans SHOULD prefer a reduced but explicit test set:

- domain tests for entity-specific rules;
- handler tests where behavior differs from the representative slice;
- persistence tests for new mapping, index, uniqueness, conversion, or provider-sensitive behavior;
- API client tests only for entity-specific behavior not already covered by a representative client;
- manual UI smoke checks for simple repeated CRUD pages when no new UI pattern is introduced.

API client ProblemDetails/error-mapping tests SHOULD be representative rather than copied for every entity when the same tested helper or parsing pattern is reused.

## Endpoint and UI Automation

HTTP endpoint integration tests and UI/component tests are expected when suitable project test infrastructure already exists and lower-level tests cannot adequately protect the behavior.

Plans MAY defer endpoint/UI automated tests when they would require new frameworks, broad test-host infrastructure, or setup disproportionate to the issue scope.

Any endpoint/UI automation deferral MUST state:

- why automated endpoint or UI tests are deferred;
- which lower-level automated tests protect the same business behavior;
- what manual validation is required;
- whether a follow-up issue is needed.

Manual UI smoke checks are acceptable for simple repeated reference-data CRUD pages when the page follows an already accepted UI pattern and the plan records the manual smoke scope and result.

## Constitution Alignment

The current constitution v1.0.1 already allows endpoint/UI automation deferrals with explicit plan exceptions and keeps automated tests mandatory for domain invariants, handlers, persistence mappings, and API clients when changed or introduced.

This guideline refines how to apply that policy to repeated reference-data slices. It does not change project-level mandatory testing requirements and does not require a constitution amendment by itself.

A future constitution amendment is required only if the project decides to change mandatory test categories or make endpoint/UI automation mandatory by default.

## Future Planning

When future work changes or introduces domain rules, command/query handlers,
persistence mappings, or API clients, plans MUST identify automated tests before
implementation tasks that depend on them.

HTTP endpoint integration tests and UI/component tests are expected when suitable
project test infrastructure already exists and lower-level tests cannot
adequately protect the behavior. Plans may defer endpoint/UI automated tests when
they would require new frameworks, broad test-host infrastructure, or setup
disproportionate to the issue scope. Any deferral must state the lower-level
automated coverage, required manual validation, and whether a follow-up issue is
needed.
