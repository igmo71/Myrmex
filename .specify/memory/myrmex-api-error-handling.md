# Myrmex API Error-Handling Memory

This document records accepted API error-handling guidance for Myrmex.

## Accepted Convention

Write/action operations return `ApiResult<T>`.

Read/load operations use exception-based flow that remains aware of user-facing ProblemDetails.

## Documentation Scope

Issue #30 documents this convention only. It must not change endpoints, API clients, ProblemDetails behavior, exception behavior, or service result shapes.

## Future Planning

Future API work should state whether a flow is a write/action operation or a read/load operation, then apply the corresponding convention deliberately.

Any change to endpoint behavior, API client behavior, or ProblemDetails mapping requires a separate approved issue.
