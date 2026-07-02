# Audit Information Model

This feature changes no runtime or persistence data model. These records define the audit report.

## Slice Audit

- **Slice name**: one of the nine in-scope WMS lists.
- **Boundary inventory**: WebApp grid/page, API client operation, public request/response, endpoint, internal query/handler, projection, and tests.
- **Contract ownership**: location and dependency purity of cross-boundary types.
- **Backend pipeline**: filter, count, sort, page, project, materialize, and result-envelope behavior.
- **Sort behavior**: supported keys and exact casing, default order, direction, stable tie-breaker, and UI mapping.
- **WebApp behavior**: server-data use, request mapping, paging, reload/reset, cancellation, visible warehouse value, and error flow.
- **Test protection**: existing coverage, material gaps, and lowest risk-owning layer.
- **Disposition**: compliant, inconsistent, or not applicable, with evidence paths.

## Normalization Candidate

- **Finding**: current inconsistency and consequence.
- **Affected slices**: concrete scope.
- **Target convention**: existing durable or accepted local pattern.
- **Risk class**: safe mechanical, focused tests required, or deferred.
- **Protection needed**: behavior and lowest validation layer.
- **Dependencies**: prerequisite contract or UI decisions.

Each slice audit may produce multiple candidates. A candidate spans slices only when the same established convention applies. A confirmed absent boundary is recorded explicitly and is not confused with an unreviewed area.
