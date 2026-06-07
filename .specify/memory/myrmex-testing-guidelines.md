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

When future work changes or introduces domain rules, handlers, persistence behavior, API contracts, or critical UI flows, plans should identify the required tests before implementation tasks that depend on them.
