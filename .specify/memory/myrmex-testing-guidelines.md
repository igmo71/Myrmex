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
