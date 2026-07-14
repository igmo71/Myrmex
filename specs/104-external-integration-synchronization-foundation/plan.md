# Implementation Plan: External Integration Synchronization Foundation

**Branch**: `104-external-integration-synchronization-foundation` | **Date**: 2026-07-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/104-external-integration-synchronization-foundation/spec.md`

## Summary

Establish the first durable synchronization foundation for external 1C receiving and shipping change notifications. The implementation will add machine API-key authentication for new notification endpoints, persist provider-neutral synchronization requests in the existing Myrmex database under integration ownership, wake a background processor through a bounded in-process signal, and rely on durable polling, retry scheduling, recovery of abandoned processing claims, and provider-neutral optimistic claims for reliability. The first slice records and processes lifecycle state but does not fetch receiving/shipping documents, add replay administration, add cleanup/retention workers, or change existing user-operated 1C manual import endpoints.

## Technical Context

**Language/Version**: C# on .NET 10.0, matching existing projects.

**Primary Dependencies**: ASP.NET Core Minimal APIs, ASP.NET Core authentication/authorization, EF Core 10, Microsoft.Extensions.Hosting background services, System.Threading.Channels, existing Myrmex internal module and test patterns.

**Storage**: Existing `ConnectionStrings:MyrmexDatabase` SQL Server database, with a new integration-owned `IntegrationDbContext` and `integration` schema. Migrations are planned but generated/applied only when explicitly requested by the developer.

**Testing**: xUnit v3 through `Myrmex.Tests`, Microsoft Testing Platform, focused Minimal API endpoint tests, authorization policy tests, persistence/lifecycle tests, and worker/processor unit tests with test doubles and controlled time.

**Target Platform**: Myrmex ApiService in the existing modular-monolith deployment.

**Project Type**: Backend API and background-processing slice inside the existing modular monolith.

**Performance Goals**: Accept valid notification requests after durable commit with minimal response work; process eligible requests in configurable batches; default fallback polling interval is 60 seconds; retry scheduling must follow configured delays within 5 seconds in acceptance tests.

**Constraints**: Preserve Identity API-session cookie as default authentication; isolate machine API-key auth to notification endpoints; keep 1C transport details under `Myrmex.Integrations/OneC`; do not place synchronization queue ownership in WMS; do not add UI, replay commands, cleanup workers, RabbitMQ, outbox, or receiving/shipping document fetch behavior.

**Scale/Scope**: First slice supports one configured 1C source instance and one active integration API key while persisting `SourceInstance` for future multi-instance support. Acceptance testing covers at least 1,000 eligible requests in a two-instance safe-claim scenario.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Domain Model First**: Pass. The plan names receiving-order and shipping-order document notifications, synchronization request lifecycle invariants, idempotency keys, and retry/recovery rules. `IntegrationSynchronizationRequest` is intentionally a technical integration persistence entity, not a WMS aggregate.
- **Modular Monolith Boundaries**: Pass. Generic synchronization foundation stays in `Myrmex.Integrations`; 1C-specific contracts, endpoints, authentication inputs, and transport names stay under `Myrmex.Integrations/OneC`; WMS does not own queue persistence or processing infrastructure. Cross-boundary behavior remains explicit through ApiService registration and endpoint contracts.
- **Vertical Slice Delivery**: Pass. The slice includes endpoint contracts, auth scheme/policy registration, persistence mapping, intake handler/service behavior, processor lifecycle behavior, background worker registration, diagnostics, and focused tests. There is no UI/client slice in scope.
- **Testing Discipline**: Pass. Tests are risk-based: endpoint tests for HTTP contract and route auth split; policy/auth handler tests for the machine principal boundary; persistence tests for uniqueness, binary data version, concurrency token, and status fields; processor tests for lifecycle, retry, duplicate side effects, abandoned recovery, and multi-instance claim behavior. No duplicated UI/API-client tests are planned because no WebApp client is added.
- **Simplicity and Observability**: Pass. The plan uses existing project/module patterns, one durable SQL-backed queue, a bounded Channel only for wake-up, no broker/outbox/framework expansion, and explicit diagnostics for intake, duplicate detection, claims, retries, deferrals, failures, and recovery.

## Project Structure

### Documentation (this feature)

```text
specs/104-external-integration-synchronization-foundation/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── onec-change-notifications.md
│   └── synchronization-lifecycle.md
└── tasks.md
```

### Source Code (repository root)

```text
Myrmex.ApiService/
└── Program.cs

Myrmex.AspNetCore/
└── Security/
    └── MyrmexAuthorizationPolicies.cs

Myrmex.Integrations/
├── IntegrationModule.cs
├── Authentication/
│   ├── IntegrationApiKeyAuthenticationHandler.cs
│   ├── IntegrationApiKeyAuthenticationOptions.cs
│   └── MyrmexIntegrationAuthenticationSchemes.cs
├── Infrastructure/
│   └── Persistence/
│       ├── IntegrationDbContext.cs
│       └── Configurations/
│           └── IntegrationSynchronizationRequestConfiguration.cs
├── Synchronization/
│   ├── IntegrationSynchronizationOptions.cs
│   ├── IntegrationSynchronizationRequest.cs
│   ├── IntegrationSynchronizationRequestStatus.cs
│   ├── IntegrationSynchronizationTrigger.cs
│   ├── IIntegrationSynchronizationWakeSignal.cs
│   ├── IntegrationSynchronizationWakeSignal.cs
│   ├── IIntegrationSynchronizationHandler.cs
│   ├── IntegrationSynchronizationProcessor.cs
│   └── IntegrationSynchronizationWorker.cs
└── OneC/
    ├── OneCIntegrationModule.cs
    ├── Configuration/
    │   └── OneCOptions.cs
    └── Endpoints/
        └── OneCEndpoints.cs

Myrmex.Tests/
├── AspNetCore/
│   └── Security/
│       └── MyrmexAuthorizationPolicyTests.cs
├── Integrations/
│   ├── Authorization/
│   │   └── IntegrationAuthorizationEndpointTests.cs
│   ├── Synchronization/
│   │   ├── IntegrationSynchronizationPersistenceTests.cs
│   │   ├── IntegrationSynchronizationProcessorTests.cs
│   │   └── IntegrationSynchronizationWorkerTests.cs
│   └── OneC/
│       └── Endpoints/
│           └── OneCNotificationEndpointTests.cs
└── Testing/
    ├── TestIntegrationApiKeyAuthentication.cs
    └── TestIntegrationDbContext.cs
```

**Structure Decision**: Extend the existing backend modular-monolith structure. Keep reusable integration synchronization foundation in `Myrmex.Integrations/Synchronization` and `Myrmex.Integrations/Infrastructure/Persistence`; keep 1C endpoint and JSON contract handling in `Myrmex.Integrations/OneC`; register services from ApiService through integration module extension methods.

## Architectural Design Notes

- **Domain concepts first**: The business-facing concepts are receiving-order and shipping-order external document changes. The owned technical concept is `IntegrationSynchronizationRequest`, with lifecycle states `Pending`, `Processing`, `Deferred`, `Completed`, and `Failed`. Idempotency is defined by `SourceSystem + SourceInstance + EntityType + ExternalId + ExternalDataVersion`.
- **Shared contract boundary**: No `Myrmex.Shared` contract is required for this first slice because 1C calls the ApiService directly and there is no WebApp/API client surface. 1C notification request records should remain in `Myrmex.Integrations/OneC`, avoiding WMS or shared DTO leakage.
- **Internal request boundary**: Intake should flow through explicit integration-owned services or commands from endpoint to persistence. Document-specific handlers use an internal `IIntegrationSynchronizationHandler` style boundary so later receiving/shipping implementations can register handlers without changing the intake contract.
- **Backend-owned projection**: No list or read projection is exposed. Internal diagnostics may log request identity, status, attempt count, and failure category without exposing secrets.
- **Server-driven list behavior**: Not applicable; there is no UI or list endpoint in this feature.
- **Client/grid behavior**: Not applicable; no WebApp client, API client, or grid behavior is in scope.
- **Cancellation and errors**: Notification endpoints honor request cancellation until durable commit starts. Contract validation errors use normal ProblemDetails-style responses and never return `202`; auth failures use ASP.NET Core 401/403 behavior; accepted and duplicate notifications return an empty `202 Accepted` only after durable commit.
- **Risk-based testing**: Endpoint tests protect JSON binding, required field validation, Base64 validation, empty 202 response, duplicate 202 behavior, and WMS-operator vs API-key route split. Persistence tests protect binary data version storage, unique idempotency key, source instance persistence, status fields, and application-managed concurrency. Processor tests protect lifecycle state transitions, duplicate side-effect limits, retry delay scheduling, deferred outcomes, failed terminal outcomes, abandoned processing recovery, and safe claims. Authorization tests protect the machine principal boundary without Identity roles or GUID user id.
- **Existing pattern precedence**: Reuse existing Minimal API endpoint mapping style from `OneCEndpoints`, existing policy registration style in `MyrmexAuthorizationPolicies`, existing module registration style from `WmsModule` and `OneCIntegrationModule`, and existing focused endpoint/authorization test style from `Myrmex.Tests/Integrations`.

## Complexity Tracking

No constitution violations or endpoint/UI automated-test exceptions are planned.

### Principle IV Endpoint/UI Test Exceptions

| Deferred automated test | Why deferred | Lower-level automated coverage | Manual validation required | Follow-up issue needed? |
|-------------------------|--------------|--------------------------------|----------------------------|-------------------------|
| None | N/A | N/A | N/A | No |

## Phase 0 Research Summary

See [research.md](research.md). All planning decisions are resolved; no unresolved planning questions remain.

## Phase 1 Design Summary

See [data-model.md](data-model.md), [contracts/onec-change-notifications.md](contracts/onec-change-notifications.md), [contracts/synchronization-lifecycle.md](contracts/synchronization-lifecycle.md), and [quickstart.md](quickstart.md).

## Post-Design Constitution Check

- **Domain Model First**: Pass. Design artifacts preserve receiving/shipping document notification language and keep synchronization lifecycle invariants explicit.
- **Modular Monolith Boundaries**: Pass. Artifacts keep generic integration synchronization in `Myrmex.Integrations`, 1C-specific contracts in `Myrmex.Integrations/OneC`, and WMS untouched for queue ownership.
- **Vertical Slice Delivery**: Pass. Design covers endpoint contract, authentication boundary, persistence model, processing lifecycle, worker behavior, diagnostics, and validation scenarios.
- **Testing Discipline**: Pass. Quickstart and planned tests focus on distinct risks at the lowest owning layer.
- **Simplicity and Observability**: Pass. No broker, outbox, replay UI, cleanup worker, or generalized ERP framework is introduced; diagnostics are scoped to operationally important events.
