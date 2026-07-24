# Contracts: Manual Receiving-Order Import

## WebApp to Integration API

`POST /api/integrations/1c/receiving-orders/import`

Authorization: existing `WmsOperator` policy.

Request:

| Field | Type | Rules |
|---|---|---|
| `startDate` | date | Required, inclusive source-calendar start date. |
| `endDate` | date | Required, inclusive source-calendar end date; must not precede `startDate`. |

Response:

| Field | Meaning |
|---|---|
| `processed`, `created`, `updated`, `skipped`, `failed` | Counts of document outcomes. |
| `startedAtUtc`, `completedAtUtc` | Operator-visible execution bounds. |
| `operationError` | One request-wide source/configuration failure, when document processing could not complete. |
| `results` | One result for each document reached by the import operation. |

Each result contains the external document key, document number/date where available,
one of `Created`, `Updated`, `Skipped`, or `Failed`, and a stable reason plus a concise
operator-facing explanation for skipped/failed outcomes.

The existing reference-import response remains unchanged because it uses `Unchanged` and
has a different record shape.

## Integration to WMS application boundary

Expose a public WMS command for one fully mapped external receiving document. The command
accepts only source-neutral values:

- document external key, opaque data version, display number, source date, and warehouse
  external key;
- mapped line identity, SKU external key, and planned quantity;
- import timestamp and actor context for diagnostics.

It returns an outcome and reason suitable for conversion to the API result. The Integration
module never receives WMS entities or accesses `WmsDbContext`.

The command does not accept or expose raw 1C field names, transport DTOs, EF entities,
or synchronization-request state.
