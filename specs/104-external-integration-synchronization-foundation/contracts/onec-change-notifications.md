# Contract: 1C Change Notifications

## Authentication

Notification endpoints require:

```http
Authorization: ApiKey <secret>
```

The credential authenticates a machine identity through `Myrmex.IntegrationApiKey` and authorizes through `MyrmexAuthorizationPolicies.OneCIntegration`.

The machine principal is not a Myrmex user-session identity and does not require Identity roles or a GUID `NameIdentifier`.

## Endpoints

```http
POST /api/integrations/1c/receiving-orders/changed
POST /api/integrations/1c/shipping-orders/changed
```

Existing endpoints remain WMS-operator endpoints and do not accept the integration API key:

```http
POST /api/integrations/1c/connection/test
POST /api/integrations/1c/warehouses/import
POST /api/integrations/1c/uoms/import
POST /api/integrations/1c/skus/import
```

## Request Body

```json
{
  "Ref_Key": "80066011-d7c7-11ef-bac8-00155d01d112",
  "DataVersion": "AAAAAAAaKtk=",
  "Number": "УТ-00001004",
  "Date": "2025-01-21T10:15:36"
}
```

### Field Rules

| Field | Required | Rule |
|-------|----------|------|
| `Ref_Key` | Yes | External document identity persisted as `ExternalId`. |
| `DataVersion` | Yes | Must be valid Base64 and is persisted as decoded binary `ExternalDataVersion`. |
| `Number` | No | Optional diagnostic value persisted as `ExternalDocumentNumber`. |
| `Date` | No | Optional diagnostic value persisted as `ExternalDocumentDate`; not authoritative UTC. |

`DataVersion` supports notification idempotency and version tracking only. The contract does not claim optimistic concurrency through source `If-Match` behavior.

## Successful Response

Accepted and duplicate notifications return the same response:

```http
202 Accepted
```

The response body is empty.

The response is returned only after durable commit. It never includes synchronization id, queue status, duplicate/new indicator, or internal lifecycle state.

## Duplicate Behavior

Duplicate key:

```text
SourceSystem + SourceInstance + EntityType + ExternalId + ExternalDataVersion
```

Duplicate receipt:

- returns empty `202 Accepted`;
- preserves existing lifecycle state;
- does not reveal whether the request already existed;
- does not schedule retry, reset attempts, clear error data, restart processing, or trigger replay;
- may send a best-effort wake-up signal only when the existing request is `Pending`.

## Failure Responses

Authentication and authorization failures use the standard API 401/403 behavior.

Malformed contract failures do not return `202 Accepted` and do not create a synchronization request. Examples:

- missing `Ref_Key`;
- missing `DataVersion`;
- invalid Base64 `DataVersion`;
- invalid request body shape.

Problem responses must not expose API-key secrets or other protected configuration values.
