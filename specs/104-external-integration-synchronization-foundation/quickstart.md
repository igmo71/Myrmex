# Quickstart: Validate External Integration Synchronization Foundation

This guide describes validation scenarios for the implementation phase. It does not run builds, tests, migrations, application startup, or database updates automatically.

## Prerequisites

- Feature implementation completed for Issue #104.
- Development database schema prepared by developer-controlled migration workflow.
- Integration API-key secret configured outside repository files.
- One configured source instance for the current 1C infobase.

## Recommended Validation Commands

Run only when the developer is ready:

```powershell
dotnet build Myrmex.slnx -nologo -v:minimal
dotnet test Myrmex.Tests/Myrmex.Tests.csproj --no-build
```

Migration generation and database updates remain developer-controlled and are not part of this quickstart.

## Scenario 1: Accept a Receiving Notification

1. Start ApiService with prepared configuration.
2. Send:

   ```http
   POST /api/integrations/1c/receiving-orders/changed
   Authorization: ApiKey <configured-secret>
   Content-Type: application/json
   ```

   ```json
   {
     "Ref_Key": "80066011-d7c7-11ef-bac8-00155d01d112",
     "DataVersion": "AAAAAAAaKtk=",
     "Number": "УТ-00001004",
     "Date": "2025-01-21T10:15:36"
   }
   ```

3. Expected result: empty `202 Accepted` after durable commit.
4. Expected data: one synchronization request with entity type `ReceivingOrder`, decoded binary data version, source instance, and `Pending` lifecycle state unless processing already claimed it.

## Scenario 2: Accept a Shipping Notification

Repeat Scenario 1 against:

```http
POST /api/integrations/1c/shipping-orders/changed
```

Expected data uses entity type `ShippingOrder`.

## Scenario 3: Duplicate Notification Is Opaque and Side-Effect Limited

1. Submit a valid notification.
2. Capture the persisted request status, attempt count, timestamps, next attempt time, and last error.
3. Submit the exact same notification again.
4. Expected result: empty `202 Accepted`.
5. Expected data: duplicate does not change lifecycle state, attempt count, retry timing, processing timestamps, completion time, or last error. If the existing request is `Pending`, only a best-effort wake-up signal may occur.

Repeat for existing requests in `Pending`, `Processing`, `Deferred`, `Completed`, and `Failed`.

## Scenario 4: Contract Validation Rejects Bad Requests

Submit each invalid request:

- missing `Ref_Key`;
- missing `DataVersion`;
- invalid Base64 `DataVersion`;
- malformed JSON body.

Expected result: no `202 Accepted`, no synchronization request created, and a non-secret ProblemDetails-style error where applicable.

## Scenario 5: Authentication Boundary

1. Call notification endpoint with no API key.
2. Call notification endpoint with invalid API key.
3. Call notification endpoint with an Identity API-session cookie but no API key.
4. Call existing manual 1C import endpoint with the integration API key.
5. Call existing manual 1C import endpoint with an authorized WMS operator cookie.

Expected results:

- notification endpoint accepts only valid integration API key;
- notification endpoint does not require Identity role or GUID user id;
- manual endpoints remain WMS-operator protected and do not accept integration API key.

## Scenario 6: Processor Lifecycle

Create or accept requests covering:

- no registered handler;
- registered handler success;
- transient failure with retries remaining;
- transient failure after retry delays exhausted;
- permanent processing failure.

Expected states:

- no handler -> `Deferred`;
- handler success -> `Completed`;
- transient with retry -> `Pending` with `NextAttemptAtUtc`;
- retries exhausted -> `Failed`;
- permanent failure -> `Failed`.

## Scenario 7: Startup Scan, Polling, and Missed Wake-Up

1. Persist an accepted request.
2. Simulate or force missing the in-process wake-up signal.
3. Start or continue the worker.

Expected result: startup scan or fallback polling discovers and processes the request.

## Scenario 8: Abandoned Processing Recovery

1. Persist a request in `Processing` older than the configured processing timeout.
2. Run recovery scan.

Expected result: request becomes eligible for a safe claim without concurrent duplicate processing.

## Scenario 9: Multi-Instance Safe Claims

1. Prepare at least 1,000 eligible requests.
2. Run two application instances or two processor instances against the same prepared store.
3. Allow both to scan and claim work.

Expected result: instances may divide work, but no request is processed concurrently by more than one instance.

## Scenario 10: Retention and Replay Scope

Verify the implementation does not expose or run:

- replay endpoint;
- replay UI;
- scheduled replay;
- administrative replay command;
- cleanup, archival, or deletion worker for completed, deferred, or failed requests.
