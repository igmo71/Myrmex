# Contract: Manual 1C Reference Import Compatibility

## Preserved Routes and Authorization

- `POST /api/integrations/1c/warehouses/import`
- `POST /api/integrations/1c/uoms/import`
- `POST /api/integrations/1c/skus/import`
- Existing `WmsOperator` authorization policy.
- Existing `200`, `409`, and Problem Details/error behavior.

## Additive Response

All existing fields remain and `Unchanged` is added as an integer count.

```json
{
  "referenceType": "warehouses",
  "isComplete": true,
  "processed": 3,
  "created": 0,
  "updated": 1,
  "unchanged": 2,
  "skipped": 0,
  "failed": 0,
  "startedAtUtc": "2026-07-16T09:00:00Z",
  "completedAtUtc": "2026-07-16T09:00:01Z",
  "operationError": null,
  "errors": []
}
```

Invariant:

```text
Processed = Created + Updated + Unchanged + Skipped + Failed
```

## Preserved Semantics

- Full Warehouse/UoM collection reads and paged SKU reads remain in source order.
- The manual per-type lease remains fail-fast and spans the whole operation, including every SKU page and batch.
- Existing transaction/savepoint, committed-page accounting, code-conflict, folder/deletion skip, error reason, 50-returned-error cap, and incomplete-operation behavior remain.
- Manual cancellation retains the existing caller-facing incomplete response with reason `Cancelled` when cancellation occurs inside the import operation.
- Repeating a successfully imported current version increments `Unchanged`, not `Updated`, and does not change timestamps or events.

## WebApp Compatibility

The existing `/integrations/1c` page keeps the same three actions and displays localized `Unchanged` beside Processed, Created, Updated, Skipped, and Failed. No synchronize-one button or page is added.
