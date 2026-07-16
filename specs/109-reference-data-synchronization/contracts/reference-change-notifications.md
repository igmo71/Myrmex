# Contract: 1C Reference Change Notifications

## Endpoints

All routes use the existing `Myrmex.OneCIntegration` machine authorization policy and Feature 104 durable intake.

| Reference | Method and route | Stable synchronization entity type |
|---|---|---|
| Warehouse | `POST /api/integrations/1c/warehouses/changed` | `Warehouse` |
| Unit of Measure | `POST /api/integrations/1c/uoms/changed` | `UnitOfMeasure` |
| Stock Keeping Unit | `POST /api/integrations/1c/skus/changed` | `StockKeepingUnit` |

Existing Receiving and Shipping routes and entity types remain unchanged.

## Request

```json
{
  "Ref_Key": "b41f62d6-8b53-4a83-b95d-4ad3a41d07e7",
  "DataVersion": "AQIDBA=="
}
```

Rules:

- `Ref_Key` is a required non-empty GUID.
- `DataVersion` is required Base64 that decodes to 1-128 opaque bytes.
- `Number` and `Date` are not required for reference notifications.
- Additional optional document diagnostics accepted by the shared Feature 104 request do not enter WMS domain/import contracts.

## Acceptance Response

After durable insert or duplicate resolution commits:

```http
HTTP/1.1 202 Accepted
Content-Length: 0
```

Processing remains asynchronous. Duplicate identity continues to be:

```text
SourceSystem + SourceInstance + EntityType + ExternalId + ExternalDataVersion
```

## Validation and Authorization

- Malformed `Ref_Key` or `DataVersion` uses the existing validation Problem Details response.
- Missing/invalid machine authentication uses the existing Feature 104 authentication behavior.
- No operator cookie is required or accepted as a replacement for the machine policy.
- No new intake table, wake-up path, polling loop, or request lifecycle is introduced.

