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

## Issue #32 Catalog/SKU Reference Pattern

Issue #32 implemented `StockKeepingUnit` as the first fully covered Catalog reference-data vertical slice after Spec Kit stabilization.

For current planning purposes, Catalog/SKU is the representative reference-data pattern for:

- domain aggregate validation and lifecycle behavior;
- command/query handlers;
- EF Core mapping and migration expectations;
- API endpoint shape and result conventions;
- WMS API client result/error behavior;
- Blazor list/create/edit/lifecycle UI behavior;
- manual UI smoke validation for a simple CRUD reference page.

Future reference-data slices SHOULD reuse this established pattern unless they introduce new behavior or intentionally change the pattern.

## Representative vs Repeated Reference-Data Slices

A representative slice is the first implementation of a new capability, architectural pattern, API client convention, persistence convention, or UI interaction pattern. Representative slices require broad automated coverage for the behavior they establish.

A repeated reference-data slice is a later CRUD-style entity that follows an already accepted pattern without changing the underlying behavior. Repeated slices SHOULD use focused coverage rather than copying the full representative test matrix.

A repeated slice becomes representative again when it introduces or changes any of the following:

- new domain invariants, state transitions, lifecycle rules, or idempotency behavior;
- new uniqueness, normalization, concurrency, soft-delete, or persistence mapping behavior;
- new API result shapes, ProblemDetails mapping, status-code conventions, or endpoint composition patterns;
- new API client parsing, error handling, retry, cancellation, or result-envelope behavior;
- new UI interaction patterns, validation flows, navigation behavior, or component composition;
- new test infrastructure or a materially different testing approach.

## Minimum Coverage for Repeated Reference-Data Slices

For repeated CRUD/reference entities that reuse the Catalog/SKU-style pattern, plans SHOULD prefer a reduced but explicit test set:

- domain tests only for entity-specific invariants and lifecycle behavior;
- handler tests for create/update/list/get/lifecycle behavior where business behavior differs from the representative slice;
- persistence tests for new table mapping, indexes, required fields, uniqueness, value conversion, or provider-sensitive behavior;
- API client tests only for entity-specific serialization, route construction, or result mapping not already covered by a representative client;
- manual UI smoke checks for simple repeated list/create/edit/lifecycle pages when no new UI pattern is introduced.

Plans MUST still include automated tests for genuinely new domain rules, handler behavior, persistence behavior, or API client behavior.

## API Client Error and ProblemDetails Coverage

API client error handling and ProblemDetails mapping SHOULD be tested once per representative client behavior pattern.

Future API clients do not need to duplicate the same low-level ProblemDetails/error-mapping matrix when all of the following are true:

- the client uses an already tested result/error helper or parsing pattern;
- routes and DTOs are the only entity-specific differences;
- no new status-code convention, error payload shape, cancellation behavior, or exception behavior is introduced.

Repeated clients SHOULD still have focused tests for entity-specific route construction, DTO serialization/deserialization, and success/failure result wiring where those are not trivially covered by shared helpers.

## Endpoint and UI Automation

HTTP endpoint integration tests and UI/component tests are expected when suitable project test infrastructure already exists and lower-level tests cannot adequately protect the behavior.

Plans MAY defer endpoint/UI automated tests when they would require new frameworks, broad test-host infrastructure, or setup disproportionate to the issue scope.

Any endpoint/UI automation deferral MUST state:

- why automated endpoint or UI tests are deferred;
- which lower-level automated tests protect the same business behavior;
- what manual validation is required;
- whether a follow-up issue is needed.

Manual UI smoke checks are acceptable for simple repeated reference-data CRUD pages when:

- the page follows an already accepted UI pattern;
- no new validation, lifecycle, navigation, or component behavior is introduced;
- the plan records the manual smoke scope and result.

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
