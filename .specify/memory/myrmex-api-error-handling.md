# Myrmex API Error Handling

Durable API, result, and error conventions.

## Conventions

Write/action operations return `ApiResult<T>`.

Read/load operations use exception-based flow that remains aware of user-facing ProblemDetails.

## Future Planning

Future API work should state whether a flow is a write/action operation or a read/load operation, then apply the corresponding convention deliberately.

Any change to endpoint behavior, API client behavior, or ProblemDetails mapping requires a separate approved issue.
