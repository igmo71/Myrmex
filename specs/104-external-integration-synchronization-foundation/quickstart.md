# Quickstart: External Integration Synchronization Foundation

This guide describes developer-controlled validation for the planned feature. Do not run builds, tests, application startup, database updates, EF migration generation, or EF migration application automatically.

## Prerequisites

- Development database available through `ConnectionStrings:MyrmexDatabase`.
- Integration schema migration generated, reviewed, and applied by the developer.
- Disposable development-only integration API key supplied through local development configuration.
- Existing Identity/API-session configuration remains valid for current WMS operator 1C administration endpoints.

## Recommended Validation Commands

Run only when the developer is ready:

```powershell
dotnet build Myrmex.slnx -nologo -v:minimal
dotnet test Myrmex.Tests\Myrmex.Tests.csproj --filter "FullyQualifiedName~Integrations"
```

If migration work is explicitly requested later, generate and apply integration migrations through the repository's normal EF workflow; runtime startup must not silently create or update schema.

## Scenario 1: Accepted Receiving Notification

1. Start ApiService with a disposable development API key.
2. Send:

   ```http
   POST /api/integrations/1c/receiving-orders/changed
   Authorization: ApiKey <development-secret>
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

3. Expect empty `202 Accepted`.
4. Verify one durable synchronization request exists with `EntityType = ReceivingOrder`, configured `SourceSystem`, configured `SourceInstance`, decoded binary data version, optional diagnostics, and `Pending` or later processor-owned lifecycle state.

## Scenario 2: Duplicate Notification

1. Repeat the same request at least five times.
2. Expect empty `202 Accepted` every time.
3. Verify duplicate delivery does not change lifecycle state, attempt count, retry schedule, timestamps, or last error.

## Scenario 3: Contract Validation Failure

1. Send a notification with invalid Base64 `DataVersion`, an empty decoded `DataVersion`, an oversized decoded `DataVersion`, an invalid `Ref_Key`, an over-length `Number`, and a malformed `Date`.
2. Expect a non-`202` validation response.
3. Verify no synchronization request is created.

## Scenario 4: Authentication Boundary

1. Call a notification endpoint with no API key or a wrong API key.
2. Expect authentication/authorization failure and no synchronization request.
3. Call a notification endpoint with only an Identity API-session cookie.
4. Expect `OneCIntegration` authorization to reject it.
5. Start with missing or empty configured API-key values.
6. Expect startup options validation failure.
7. Call `/api/integrations/1c/connection/test` with the integration API key.
8. Expect the existing WMS operator route to reject the machine credential.
9. Call the same connection-test endpoint with an eligible WMS operator or administrator API session.
10. Expect the existing route behavior to remain intact.

## Scenario 5: Processor Lifecycle

1. Accept a receiving or shipping notification with no document-specific handler registered.
2. Let the processor run.
3. Verify the request becomes `Deferred`, not `Completed`.
4. Simulate a registered handler completing successfully.
5. Verify the request becomes `Completed` and records completion time.
6. Simulate transient and permanent failures.
7. Verify retry schedule, exhausted retries, and terminal `Failed` behavior.
8. Verify `AttemptCount` increments when an attempt starts, the first attempt is `1`, `N` retry delays allow `N + 1` attempts, and `Deferred` outcomes do not consume retry delays.
9. Verify the `Pending` to `Processing` transition, incremented `AttemptCount`, and `ProcessingStartedAtUtc` are committed before handler invocation.
10. Verify `ProcessingAttemptTimeoutSeconds` is treated as a transient failure, while host-shutdown cancellation leaves the durable record `Processing` for abandoned recovery without scheduling a normal handler retry.
11. Configure `RetryDelaysSeconds = []` and verify one initial attempt is allowed, no retry is scheduled, and a transient failure becomes terminal `Failed`.

## Scenario 6: Wake-Up and Restart Recovery

1. Accept a notification and suppress or ignore the wake-up signal.
2. Verify fallback SQL polling still discovers the request within one polling interval.
3. Fill the bounded capacity-1 wake-up channel.
4. Verify additional wake-up writes are dropped/coalesced without losing SQL-backed work.
5. Verify the processor drains eligible SQL batches until no immediately eligible work remains after a wake-up.
6. Leave a request in `Processing` and restart the application after the processing timeout.
7. Verify the abandoned attempt remains included in `AttemptCount`.
8. Verify startup and fallback-polling passes invoke abandoned `Processing` recovery before querying and processing currently eligible requests.
9. Verify the request returns to immediately eligible `Pending` with `ProcessingStartedAtUtc` cleared when retry opportunities remain.
10. Verify the request becomes `Failed` with bounded non-secret `LastError` when retry opportunities are exhausted.

## Scenario 7: Concurrent Duplicate Intake

1. Send duplicate HTTP notifications for the same source/version concurrently.
2. Verify the database unique constraint is authoritative and only one synchronization request exists.
3. Verify only SQL Server duplicate-key failures that identify `UX_integration_synchronization_requests_idempotency` are treated as duplicate intake.
4. Verify the failed Added entity is detached or otherwise cleared from EF tracking before loading the existing record and the failed insert is not retried.
5. Verify unrelated persistence failures are surfaced as failures and are not returned as successful duplicates.

## Scenario 8: Platform Readiness Participation

1. Use the existing ApiService `/health` readiness endpoint supplied by `Myrmex.ServiceDefaults`; do not add a separate integration-specific public health endpoint.
2. Verify integration readiness covers `IntegrationDbContext` persistence reachability, required integration options validation, and `IntegrationSynchronizationWorker` registration/loop readiness.
3. Verify `/alive` remains the platform liveness check and does not depend on integration persistence.
4. Verify readiness responses do not expose API keys, connection strings, external credentials, queue contents, synchronization-request details, or internal exception details.

## Expected Artifacts

- See [data-model.md](./data-model.md) for entities, fields, uniqueness, and lifecycle rules.
- See [contracts/onec-change-notifications.md](./contracts/onec-change-notifications.md) for external HTTP contracts.
- See [contracts/synchronization-lifecycle.md](./contracts/synchronization-lifecycle.md) for processor and retry behavior.
