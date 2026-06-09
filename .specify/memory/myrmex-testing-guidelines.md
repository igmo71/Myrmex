# Myrmex Testing Guidelines

Durable testing strategy for Myrmex planning and implementation.

## Required Coverage

When changed or introduced, the following need automated tests:

- domain invariants and lifecycle behavior;
- command and query handlers;
- persistence mappings, indexes, uniqueness, and provider-sensitive behavior;
- API clients, result envelopes, ProblemDetails mapping, and error handling.

## Reference-Data Slice Coverage

Catalog/SKU is the representative reference-data vertical slice. Future
CRUD-style reference-data slices SHOULD reuse that pattern instead of copying
the full SKU-level test matrix by default.

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

Add HTTP endpoint integration tests and UI/component tests when suitable project
test infrastructure already exists and lower-level tests do not adequately
protect the behavior.

Plans may defer endpoint/UI automated tests when they would require new
frameworks, broad test-host infrastructure, or setup disproportionate to the
scope. Deferrals must record lower-level automated coverage, required manual
validation, and whether a follow-up issue is needed.

Manual UI smoke checks are acceptable for simple repeated CRUD pages when the
page follows an accepted UI pattern and the plan records the smoke scope and
result.
