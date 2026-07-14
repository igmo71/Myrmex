# Contract: 1C Change Notifications

## Authentication

Notification endpoints use:

```http
Authorization: ApiKey <secret>
```

- Scheme name: `Myrmex.IntegrationApiKey`.
- Authorization policy: `MyrmexAuthorizationPolicies.OneCIntegration`.
- The caller is a machine identity, not an ASP.NET Identity user.
- The principal does not require Identity roles or a GUID `NameIdentifier`.
- The active API key is read from application configuration and is not persisted in application data.

## Receiving Order Changed

```http
POST /api/integrations/1c/receiving-orders/changed
Authorization: ApiKey <secret>
Content-Type: application/json
```

### Request Body

```json
{
  "Ref_Key": "80066011-d7c7-11ef-bac8-00155d01d112",
  "DataVersion": "AAAAAAAaKtk=",
  "Number": "UT-00001004",
  "Date": "2025-01-21T10:15:36"
}
```

### Field Rules

| Field | Required | Meaning |
|-------|----------|---------|
| `Ref_Key` | Yes | External 1C object identifier. |
| `DataVersion` | Yes | Base64 source version marker used for idempotency and version tracking. |
| `Number` | No | Diagnostic external document number. |
| `Date` | No | Diagnostic source document date without source offset. |

### Accepted Response

```http
HTTP/1.1 202 Accepted
Content-Length: 0
```

The response body is empty for both new and duplicate accepted notifications. The response does not reveal whether the request was newly inserted or already existed.

## Shipping Order Changed

```http
POST /api/integrations/1c/shipping-orders/changed
Authorization: ApiKey <secret>
Content-Type: application/json
```

The request and response contract is identical to receiving-order notifications. The canonical internal `EntityType` is `ShippingOrder`.

## Validation Failures

The endpoint must not return `202 Accepted` when:

- Authentication is missing or invalid.
- Authorization fails.
- The body is malformed JSON.
- `Ref_Key` is missing.
- `DataVersion` is missing.
- `DataVersion` is not valid Base64.

Validation failures use the repository's normal ProblemDetails-style response behavior and must not expose configured API keys or external credentials.

## Server-Side Values

The notification body never supplies:

- `SourceSystem`
- `SourceInstance`
- canonical `EntityType`

Myrmex resolves these values server-side from the endpoint and configured integration identity.

## Idempotency

Accepted notification idempotency uses:

```text
SourceSystem
+ SourceInstance
+ EntityType
+ Ref_Key
+ decoded DataVersion
```

Duplicate receipt:

- returns empty `202 Accepted`;
- preserves lifecycle state, attempts, retry timing, timestamps, and last error;
- may send only a best-effort wake-up signal when the existing request is `Pending`;
- does not act as replay, repair, or retry reset.
