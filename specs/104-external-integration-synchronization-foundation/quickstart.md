# Quickstart: External Integration Synchronization Foundation

This guide describes developer-controlled validation for Issue #104. Do not run builds, tests, application startup, database updates, EF migration generation, or EF migration application automatically.

## Configuration

ApiService uses the existing `ConnectionStrings:MyrmexDatabase` SQL Server connection. The synchronization queue is owned by `IntegrationDbContext` in the `integration` schema.

Required 1C integration identity configuration:

```json
{
  "Myrmex": {
    "Integrations": {
      "OneC": {
        "SourceSystem": "OneC",
        "SourceInstance": "main-infobase",
        "ApiKey": "development-only-key"
      }
    }
  }
}
```

`SourceSystem` defaults to `OneC`, but it is still startup-validated as non-empty and bounded. `SourceInstance` is the server-assigned identity of the external 1C infobase. `ApiKey` is configuration-only secret material; use a disposable local development key and keep production values in protected uncommitted environment or `.env` configuration.

Synchronization worker configuration:

```json
{
  "Myrmex": {
    "Integrations": {
      "Synchronization": {
        "PollingIntervalSeconds": 60,
        "BatchSize": 20,
        "ProcessingAttemptTimeoutSeconds": 30,
        "ProcessingTimeoutSeconds": 300,
        "RetryDelaysSeconds": [10, 30, 120, 600, 1800, 3600, 10800]
      }
    }
  }
}
```

All numeric values must be positive. `RetryDelaysSeconds = []` is valid and permits one initial attempt with no retries.

## Developer-Controlled Migration

Runtime startup must not create or update schema. The integration migration is developer-controlled and lives under `Myrmex.Integrations\Persistence\Migrations`.

Generate and review the migration only when migration work is explicitly requested:

```powershell
dotnet ef migrations add AddSynchronizationRequests `
  --project Myrmex.Integrations\Myrmex.Integrations.csproj `
  --startup-project Myrmex.ApiService\Myrmex.ApiService.csproj `
  --context IntegrationDbContext `
  --output-dir Persistence\Migrations
```

Apply it only after review:

```powershell
dotnet ef database update `
  --project Myrmex.Integrations\Myrmex.Integrations.csproj `
  --startup-project Myrmex.ApiService\Myrmex.ApiService.csproj `
  --context IntegrationDbContext
```

The expected table is `integration.synchronization_requests`; the idempotency unique index is `UX_integration_synchronization_requests_idempotency`.

## SQL-Backed Tests

Provider-specific persistence, duplicate-key, concurrency, lifecycle, and worker tests require a prepared SQL Server test database.

Configure `Myrmex.Tests` user secrets:

```powershell
dotnet user-secrets set `
  --project Myrmex.Tests\Myrmex.Tests.csproj `
  "ConnectionStrings:MyrmexIntegrationTestDatabase" `
  "Server=localhost;Database=MyrmexIntegration_test;Trusted_Connection=True;TrustServerCertificate=True"
```

The database name must end in `_test`. The test host also accepts `MYRMEX_INTEGRATION_TEST_CONNECTION` for local override. Tests verify pending migrations before clearing `integration.synchronization_requests`.

Recommended validation commands, run only when the developer is ready:

```powershell
dotnet build Myrmex.slnx -nologo -v:minimal
dotnet test Myrmex.Tests\Myrmex.Tests.csproj --filter "FullyQualifiedName~Integrations"
```

## AppHost Smoke Tests

AppHost passes these integration settings into ApiService:

- `Myrmex__Integrations__OneC__SourceInstance`
- `Myrmex__Integrations__OneC__ApiKey`

The explicit AppHost smoke test supplies disposable values for those variables. Normal AppHost runs still require valid `Myrmex:Integrations:OneC:SourceInstance`, `Myrmex:Integrations:OneC:ApiKey`, `ConnectionStrings:MyrmexDatabase`, and existing 1C OData settings before ApiService can pass startup validation and health checks.

## Notification Intake

Send a receiving notification:

```http
POST /api/integrations/1c/receiving-orders/changed
Authorization: ApiKey <key>
Content-Type: application/json
```

```json
{
  "Ref_Key": "80066011-d7c7-11ef-bac8-00155d01d112",
  "DataVersion": "AAAAAAAaKtk=",
  "Number": "UT-00001004",
  "Date": "2025-01-21T10:15:36"
}
```

Expected response after durable commit:

```http
HTTP/1.1 202 Accepted
Content-Length: 0
```

The shipping route is:

```http
POST /api/integrations/1c/shipping-orders/changed
Authorization: ApiKey <key>
Content-Type: application/json
```

Valid new and accepted duplicate notifications both return the same empty `202 Accepted` response. The response does not expose whether a request was inserted or treated as a duplicate.

Malformed requests return normal validation problem details identifying `Ref_Key`, `DataVersion`, `Number`, or `Date` as applicable. Validation responses must not expose API keys, decoded data versions, connection strings, queue contents, or internal exception details.

## Expected Startup and Worker Outcomes

- Missing or empty `ApiKey`, missing/empty/over-length `SourceSystem`, missing/empty/over-length `SourceInstance`, non-positive polling/batch/timeout values, null retry-delay collection, or non-positive retry-delay elements fail startup options validation.
- Valid notification intake persists a `Pending` synchronization request and emits a best-effort wake-up only after commit.
- The wake-up channel has capacity 1, drops writes when full, carries no request payload, and only wakes the worker.
- Startup and fallback polling first recover abandoned `Processing` records, then process currently eligible requests.
- Wake-up handling drains eligible SQL batches until no immediately eligible work remains.
- Fallback polling still discovers work when wake-up signals are lost or coalesced.
- If no document-specific handler is registered, the request transitions directly from `Pending` to `Deferred` without incrementing `AttemptCount`, setting `ProcessingStartedAtUtc`, or consuming a retry delay.
- A registered handler starts a durable attempt by committing `Pending` to `Processing`, incrementing `AttemptCount`, and setting `ProcessingStartedAtUtc` before invocation.
- `ProcessingAttemptTimeoutSeconds` is a transient failure and follows retry policy.
- Host-shutdown cancellation leaves the durable record `Processing`; abandoned recovery handles it after `ProcessingTimeoutSeconds`.
- Abandoned recovery preserves `AttemptCount`, requeues immediately as `Pending` with cleared `ProcessingStartedAtUtc` when retries remain, or marks `Failed` when retries are exhausted.
- Persisted `LastError` values are bounded diagnostics and must not contain secrets.

## Readiness

Use the existing ApiService `/health` readiness endpoint supplied by `Myrmex.ServiceDefaults`; do not add a separate integration-specific public health endpoint.

- `/health` includes `IntegrationDbContext` persistence reachability.
- `/alive` remains platform liveness and does not depend on integration SQL availability.
- Health output must not expose API keys, connection strings, external credentials, queue contents, synchronization-request details, or internal exception details.

## Operational Review Checklist

- Notification endpoint names are `AcceptOneCReceivingOrderChanged` and `AcceptOneCShippingOrderChanged`.
- Notification OpenAPI summaries identify receiving and shipping change notifications.
- Notification OpenAPI descriptions state that a valid request returns an empty `202 Accepted` after durable commit and malformed requests return validation problem details.
- Integration logs may include entity type, synchronization request id, source identity fields, lifecycle state, attempt count, retry time, and counts.
- Integration logs and persisted diagnostics must not include API keys, plaintext authorization headers, decoded `DataVersion`, external credentials, connection strings, queue contents, or internal exception details. Full exception details are allowed only in logs for unexpected handler failures, not in persisted `LastError` or HTTP responses.

## Expected Artifacts

- See [data-model.md](./data-model.md) for entities, fields, uniqueness, and lifecycle rules.
- See [contracts/onec-change-notifications.md](./contracts/onec-change-notifications.md) for external HTTP contracts.
- See [contracts/synchronization-lifecycle.md](./contracts/synchronization-lifecycle.md) for processor and retry behavior.
