# Myrmex Testing Guidelines

Durable testing strategy for Myrmex planning and implementation.

## Risk-Based Minimal Testing

Myrmex uses a risk-based minimal testing approach.

Automated tests must protect significant current behavior, not classes,
methods, implementation details, or line-coverage targets.

Each test must identify a concrete regression risk that it protects.

A behavior should normally be tested at the lowest architectural layer that
fully owns it. Add focused integration coverage only for boundaries that cannot
be adequately protected at a lower layer.

Do not duplicate equivalent scenarios across domain, handler, endpoint,
API-client, and UI layers unless each test protects a distinct risk.

Prefer the smallest set of tests that would reliably fail when the protected
behavior regresses.

## Required Coverage

Automated coverage is required when changed behavior introduces or modifies a
meaningful regression risk in one of these areas:

- domain invariants and lifecycle rules;
- command/query behavior where application logic changes;
- persistence mappings, indexes, uniqueness, concurrency, and provider-sensitive behavior;
- public API/client behavior where the contract or mapping changes.

This guidance does not require one test per class, one test per method, one test
per handler by default, one endpoint test and one client test for every
scenario, or any line-coverage target.

## Test Ownership by Layer

Choose the lowest architectural layer that fully owns the protected behavior:

- domain invariants and lifecycle rules belong in domain tests;
- filtering, count-before-paging, sorting, projection, and persistence behavior
  belong in handler/persistence tests;
- Minimal API binding, routing, and JSON serialization belong in focused
  endpoint integration tests when that boundary changes;
- URL/query construction, request bodies, cancellation propagation, and generic
  success/error mapping belong in API-client tests when that client-owned
  behavior changes;
- simple repeated UI patterns usually use manual smoke validation unless new UI
  behavior or a new UI risk justifies automation.

The same business scenario should not be repeated at all layers unless each test
protects a distinct risk.

## Contract and List Testing

Tests must protect current behavior, not obsolete representations.

Successful response fixtures should be constructed from current shared DTO types
when those DTOs are the contract being exercised. API-client tests should
serialize shared DTO fixtures using web JSON conventions instead of manually
maintaining duplicate successful JSON contract shapes.

API-client tests should focus on client-owned URL construction, query
parameters, request bodies for write actions, cancellation propagation,
success/error mapping, and Problem Details behavior when those behaviors change.
Different business error scenarios should not be duplicated at the API-client
level when they exercise the same generic Problem Details mapping.

Endpoint integration tests should verify real Minimal API binding, routing, and
JSON serialization only when endpoint behavior, binding, serialization, routing,
or the public HTTP contract changes and lower-level tests cannot protect that
boundary.

Handler and persistence tests for server-driven list slices should verify
filtering, count-before-paging, paging, supported sorting, deterministic
ordering, backend-owned projection, and domain/application behavior when those
behaviors are introduced or changed. Combine equivalent cases with theories
where appropriate. Do not reproduce the same sorting/filtering matrix through
HTTP endpoint and API-client tests. Prefer fewer strong behavioral tests over
many weak tests that only reproduce framework behavior.

## Reference-Data Slice Coverage

Catalog/SKU is the representative reference-data vertical slice. Future CRUD-style reference-data slices SHOULD reuse that pattern instead of copying the full SKU-level test matrix by default.

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

Add HTTP endpoint integration tests and UI/component tests when suitable project test infrastructure already exists and lower-level tests do not adequately protect the behavior.

Plans may defer endpoint/UI automated tests when they would require new frameworks, broad test-host infrastructure, or setup disproportionate to the scope. Deferrals must record lower-level automated coverage, required manual validation, and whether a follow-up issue is needed.

Manual UI smoke checks are acceptable for simple repeated CRUD pages when the page follows an accepted UI pattern and the plan records the smoke scope and result.
