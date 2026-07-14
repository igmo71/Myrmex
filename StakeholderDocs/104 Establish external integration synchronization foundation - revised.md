**Establish external integration synchronization foundation**

Repository:
`igmo71/Myrmex`

Issue:
`https://github.com/igmo71/Myrmex/issues/104`

Create:

```text
StakeholderDocs/104 Establish external integration synchronization foundation.md
```

Use the current branch.

Do not create, rename, switch, or delete branches.

Inspect the current implementation, especially:

```text
Myrmex.ApiService/Program.cs
Myrmex.AspNetCore/Security/MyrmexAuthorizationPolicies.cs
Myrmex.Identity/Infrastructure/IdentityApiAuthenticationExtensions.cs
Myrmex.Integrations/Myrmex.Integrations.csproj
Myrmex.Integrations/OneC/OneCIntegrationModule.cs
Myrmex.Integrations/OneC/Endpoints/OneCEndpoints.cs
Myrmex.Modules.Wms/Infrastructure/Persistence/WmsDbContext.cs
Myrmex.Modules.Wms/WmsModule.cs
```

The stakeholder document must preserve the scope and accepted decisions from Issue #104 and make the following repository-specific decisions explicit.

## Ownership and boundaries

* Place the generic synchronization foundation in the existing `Myrmex.Integrations` project.
* Keep 1C-specific authentication, notification contracts, endpoint paths, and transport details under `Myrmex.Integrations/OneC`.
* Do not put integration queue entities or processing infrastructure into the WMS domain.
* Prefer a dedicated `IntegrationDbContext` using the existing `ConnectionStrings:MyrmexDatabase` connection and a separate `integration` schema.
* Do not modify `WmsDbContext` to own the synchronization queue.
* `IntegrationSynchronizationRequest` is a technical persistence entity, not a WMS aggregate root.
* Do not introduce a generalized external-link entity or universal ERP framework.

## Authentication and authorization

* Add a named authentication scheme:

```text
Myrmex.IntegrationApiKey
```

* Add a dedicated authorization policy:

```text
MyrmexAuthorizationPolicies.OneCIntegration
```

* The external caller is a machine identity, not an ASP.NET Identity user.
* The request uses:

```http
Authorization: ApiKey <secret>
```

* Preserve the existing Identity API-session cookie as the default authentication scheme.
* Do not require Identity roles or a GUID `NameIdentifier` for the 1C machine principal.
* Do not change existing administrative 1C endpoints to ApiKey authentication.
* Resolve `SourceSystem` and `SourceInstance` server-side from the configured integration identity; do not accept them from the notification body.
* The first slice supports one configured 1C source instance and one active API key.
* Split routing so that:

  * current connection-test and manual import endpoints remain protected by `WmsOperator`;
  * new receiving/shipping change-notification endpoints use `OneCIntegration`.

## Incoming endpoints

Define:

```text
POST /api/integrations/1c/receiving-orders/changed
POST /api/integrations/1c/shipping-orders/changed
```

The 1C configuration invokes these endpoints from a `ПриЗаписи` subscription. The notification means only that the external object changed; it does not classify the change as posting, unposting, deletion, or status transition.

The notification contract uses exact 1C JSON names:

```json
{
  "Ref_Key": "80066011-d7c7-11ef-bac8-00155d01d112",
  "DataVersion": "AAAAAAAaKtk=",
  "Number": "УТ-00001004",
  "Date": "2025-01-21T10:15:36"
}
```

* `Ref_Key` and `DataVersion` are required.
* `Number` and `Date` are optional diagnostic values.
* `Date` has no source offset and must not be treated as an authoritative UTC timestamp.
* `DataVersion` is guaranteed by the source.
* Decode valid Base64 `DataVersion` into binary persistence.
* Invalid Base64 is a contract validation failure.
* `DataVersion` supports notification idempotency and version tracking only.
* Do not claim optimistic concurrency support through `If-Match`; the actual 1C implementation ignores it.

## Durable synchronization request

Model a provider-neutral technical entity conceptually containing:

```text
Id
SourceSystem
SourceInstance
EntityType
ExternalId
ExternalDataVersion
ExternalDocumentNumber?
ExternalDocumentDate?
Trigger
Status
ReceivedAtUtc
ProcessingStartedAtUtc?
CompletedAtUtc?
AttemptCount
NextAttemptAtUtc?
LastError?
```

Use stable internal `EntityType` values such as:

```text
ReceivingOrder
ShippingOrder
```

Do not persist OData entity-set names as the canonical entity type.

Use a uniqueness constraint equivalent to:

```text
SourceSystem
+ SourceInstance
+ EntityType
+ ExternalId
+ ExternalDataVersion
```

The first slice supports one configured 1C source instance and one active API key.

`SourceInstance` is assigned by Myrmex from server-side configuration, persisted on every accepted synchronization request, and included in the idempotency key. It is never accepted from the notification body.

The persistence model should remain compatible with future support for more than one 1C infobase, but simultaneous multi-infobase configuration, multiple active API keys, and key-rotation workflows are outside Issue #104.

## Processor lifecycle

Use the following states unless repository analysis identifies a concrete reason to adjust them:

```text
Pending
Processing
Deferred
Completed
Failed
```

Reserve `Superseded` as a possible later extension rather than requiring its implementation in the first slice.

Semantics:

* `Pending`: eligible for processing now or after `NextAttemptAtUtc`.
* `Processing`: currently being processed by the background synchronization processor.
* `Deferred`: the infrastructure processed the request but no document-specific handler is registered; the request remains replayable.
* `Completed`: a registered handler completed successfully.
* `Failed`: terminal technical failure after configured retries or a permanent contract/processing error.

The first slice must not mark receiving/shipping notifications `Completed` merely because the processor stub selected them.

## Worker and Channel

* SQL persistence is the durable queue and source of truth.
* Use a bounded Channel only as an in-process wake-up signal.
* The Channel does not carry reliability guarantees and does not replace SQL polling.
* Endpoint order:

```text
validate
→ persist
→ commit
→ best-effort Channel signal
→ empty 202 Accepted
```

* No ordering guarantee is required between the worker waking and the HTTP response completing.
* Scan immediately on application startup.
* Use configurable fallback polling in seconds, with a preliminary default of 60 seconds.
* Process available records in configurable batches.
* Recover abandoned `Processing` records after a configurable processing timeout, including records left behind by application failure or restart.

## Retry configuration

Make retry behavior configurable.

Prefer explicit delays in seconds rather than a hidden formula, for example:

```json
{
  "PollingIntervalSeconds": 60,
  "BatchSize": 20,
  "RequestTimeoutSeconds": 30,
  "ProcessingTimeoutSeconds": 300,
  "RetryDelaysSeconds": [10, 30, 120, 600, 1800, 3600, 10800]
}
```

The final number of attempts may be derived from the retry-delay collection to avoid contradictory settings.

Differentiate transient technical failures from permanent validation or unsupported-handler outcomes.

## Response behavior

* Return an empty `202 Accepted` only after durable commit.
* Duplicate notification of the same external version also returns empty `202 Accepted`.
* Do not expose whether the request was newly inserted or already existed.
* Duplicate receipt preserves the existing request lifecycle state.
* A duplicate of a `Pending` request may send only a best-effort wake-up signal.
* Duplicate receipt must not schedule retry, reset attempts or errors, restart processing, transition status, or act as implicit replay or repair.
* Authentication and malformed-contract failures must not return `202`.

## Scope boundaries

The first issue must not implement:

* OData GET of receiving or shipping documents;
* Receiving or Shipping domain entities;
* document status mapping;
* period-based document loading;
* reference-data dependency repair;
* outbound `PATCH`;
* actual quantity synchronization;
* RabbitMQ;
* an Outbox for outbound operations;
* UI or administration pages;
* replay endpoints, replay UI, scheduled replay, or administrative replay commands;
* automatic cleanup, archival, or deletion of `Completed`, `Deferred`, or `Failed` synchronization requests;
* simultaneous multi-infobase configuration, multiple active integration API keys, or key-rotation management;
* a generalized ERP integration framework.

The first slice must preserve enough synchronization-request information for later controlled replay, but it does not implement replay itself.

## Required stakeholder-document result

The document must include:

1. Issue and branch workflow notes.
2. Business and technical background.
3. Current repository observations with precise file paths.
4. Accepted architectural decisions.
5. Incoming endpoint contracts.
6. Authentication and authorization boundary.
7. Persistence model and idempotency rules.
8. Processor state machine.
9. Channel, polling, retry, and crash-recovery behavior.
10. Duplicate-delivery behavior and lifecycle preservation.
11. Explicit non-goals.
12. Remaining research questions that genuinely cannot be resolved from the repository.

Do not create a Spec Kit feature folder yet.

Do not implement the feature.

At the end, report only:

* the created stakeholder document path;
* the main decisions captured;
* any truly blocking unresolved questions.
