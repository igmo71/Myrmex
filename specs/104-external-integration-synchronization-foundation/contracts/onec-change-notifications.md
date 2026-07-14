# Contract: 1C Change Notifications

## Authentication

Notification endpoints use:

```http
Authorization: ApiKey <secret>
```

- Scheme name: `Myrmex.IntegrationApiKey`.
- Authorization policy: `MyrmexAuthorizationPolicies.OneCIntegration`.
- `OneCIntegration` authenticates only through `Myrmex.IntegrationApiKey`.
- An Identity API-session cookie alone cannot satisfy `OneCIntegration`.
- Registering `Myrmex.IntegrationApiKey` must not change the existing default authentication scheme.
- The caller is a machine identity, not an ASP.NET Identity user.
- The principal does not require Identity roles or a GUID `NameIdentifier`.
- The active API key is read from application configuration and is not persisted in application data.
- Missing or empty configured API-key values fail startup options validation.
- The presented plaintext key is compared with the configured plaintext key using constant-time comparison.
- The key is not logged, persisted, placed in claims, or exposed in errors.
- API-key hashing and key-rotation infrastructure are not introduced in the first slice.

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
| `Ref_Key` | Yes | External 1C object identifier; must be a valid non-empty GUID. |
| `DataVersion` | Yes | Base64 source version marker used for idempotency and version tracking; decoded value must be non-empty and no larger than 128 bytes. |
| `Number` | No | Diagnostic external document number; maximum 64 characters. |
| `Date` | No | Diagnostic source document date without source offset; malformed values are rejected. |

Exact JSON names are canonical contract names enforced through explicit JSON property mapping for this contract. Do not change global ApiService JSON case-sensitivity. Unknown JSON properties are ignored.

Accepted `Date` values are represented as source-local `DateTime` values with `Kind = Unspecified`, persisted as SQL Server `datetime2`, used only as diagnostic data, and never automatically converted to UTC.

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
- `Ref_Key` is missing, empty, or not a valid non-empty GUID.
- `DataVersion` is missing, empty, not valid Base64, decodes to an empty value, or exceeds 128 decoded bytes.
- `Number` exceeds 64 characters.
- `Date` is malformed.

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
+ canonical Ref_Key GUID string
+ decoded DataVersion
```

Duplicate receipt:

- returns empty `202 Accepted`;
- preserves lifecycle state, attempts, retry timing, timestamps, and last error;
- may send only a best-effort wake-up signal when the existing request is `Pending`;
- does not act as replay, repair, or retry reset.

The database unique constraint is authoritative. Duplicate handling first verifies a SQL Server duplicate-key error category, then verifies the failure identifies `UX_integration_synchronization_requests_idempotency`. After a duplicate insert failure, the failed `Added` entity is detached or otherwise cleared from EF tracking before the existing record is loaded, and the failed insert is not retried. No other persistence failure is treated as successful duplicate intake.
