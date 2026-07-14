# Implementation Plan: External Integration Synchronization Foundation

**Branch**: `104-external-integration-synchronization-foundation` | **Date**: 2026-07-14 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/104-external-integration-synchronization-foundation/spec.md`

## Summary

Establish a durable integration-owned synchronization queue for 1C receiving and shipping document change notifications. The first slice adds API-key-authenticated notification endpoints, an integration persistence boundary using the existing Myrmex database connection and a separate `integration` schema, idempotent request intake, lifecycle state tracking, a background processor with SQL polling and best-effort wake-up signaling, and focused tests for contract, auth, persistence, lifecycle, and recovery behavior. It deliberately excludes OData document loading, WMS receiving/shipping domain entities, replay UI/endpoints, cleanup, key rotation, RabbitMQ, and generalized ERP abstractions.

## Technical Context

**Language/Version**: C# / .NET 10.0

**Primary Dependencies**: ASP.NET Core Minimal APIs, ASP.NET Core authentication/authorization, EF Core 10 SQL Server provider, `System.Threading.Channels`, hosted background services, existing Myrmex command/query/result conventions where they fit, and existing ServiceDefaults health-check infrastructure.

**Storage**: Existing `ConnectionStrings:MyrmexDatabase` SQL Server database; new integration-owned `IntegrationDbContext` with default schema `integration`; no WMS `DbContext` ownership of synchronization requests. Following repository naming conventions, the table is `integration.synchronization_requests` and the idempotency unique index is `UX_integration_synchronization_requests_idempotency`. The index uses bounded SQL Server columns: `SourceSystem nvarchar(32)`, `SourceInstance nvarchar(128)`, `EntityType nvarchar(32)`, `ExternalId nvarchar(128)`, and `ExternalDataVersion varbinary(128)`, for a maximum key width of 768 bytes. Diagnostic fields are also bounded: `ExternalDocumentNumber nvarchar(64)`, `ExternalDocumentDate datetime2`, and `LastError nvarchar(2048)`.

**Testing**: xUnit v3 through `Myrmex.Tests`, existing ASP.NET Core in-process endpoint test style, EF Core SQL Server persistence tests where provider-specific uniqueness/concurrency behavior matters, and lower-layer unit tests for contract parsing/lifecycle decisions.

**Target Platform**: Myrmex ApiService in the existing Aspire-composed modular-monolith deployment.

**Project Type**: Backend web service feature inside existing modular monolith; no WebApp UI.

**Performance Goals**: Valid notification intake commits durable state and returns empty `202 Accepted`; processor discovers pending work within one configured polling interval after startup or wake-up; batch size and retry delays are configurable. A bounded capacity-1 channel coalesces wake-up signals only. Integration readiness participates in the existing platform `/health` readiness endpoint without adding a separate integration health endpoint.

**Constraints**: Preserve `Myrmex.ApiSession` as default ApiService authentication; add `Myrmex.IntegrationApiKey` only for notification endpoints; preserve existing WMS operator protection for current 1C connection-test and manual import endpoints; do not persist active API keys in application data; do not add distributed processor-coordination requirements. Missing or empty configured API keys fail startup options validation, and presented plaintext keys are compared with configured plaintext keys using constant-time comparison without logging, persisting, placing in claims, or exposing the key.

**Scale/Scope**: First slice supports one configured 1C source instance and one active API key, while persisting `SourceInstance` in the idempotency key for future support for multiple external source instances.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Domain Model First**: Pass. The plan names integration-specific concepts before implementation details: configured 1C source instance, change notification, synchronization request, external document version, lifecycle state, retry schedule, and background synchronization processor. WMS receiving/shipping documents remain future domain concepts and are explicitly out of scope.
- **Modular Monolith Boundaries**: Pass. Generic synchronization foundation stays in `Myrmex.Integrations`; 1C endpoint/auth/contract details stay under `Myrmex.Integrations/OneC`; WMS persistence and domain remain untouched; shared transport contracts are avoided unless a WebApp/backend boundary appears later.
- **Vertical Slice Delivery**: Pass. The feature is planned as endpoint/authentication, request contract validation, persistence mapping, lifecycle application behavior, processor behavior, and diagnostics. There is no UI/client slice.
- **Testing Discipline**: Pass. Tests are risk-based: endpoint/auth tests for HTTP boundary behavior, persistence tests for idempotent uniqueness and mappings, lifecycle tests for state transitions and duplicate preservation, worker tests for polling/retry/recovery. Duplicate API-client/UI tests are intentionally omitted because no client/UI is added.
- **Simplicity and Observability**: Pass. The plan uses existing module registration, Minimal API, EF Core, options, `TimeProvider`, and hosted service patterns. It avoids new brokers, outbox, generalized ERP framework, replay UI, and key-rotation infrastructure while adding diagnostics for intake, validation, duplicate detection, lifecycle transitions, retries, and recovery.

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
└── tasks.md              # Generated later by /speckit-tasks
```

### Source Code (repository root)

```text
Myrmex.ApiService/
└── Program.cs

Myrmex.AspNetCore/
└── Security/
    ├── MyrmexAuthenticationSchemes.cs
    └── MyrmexAuthorizationPolicies.cs

Myrmex.Integrations/
├── IntegrationDbContext and synchronization foundation files
└── OneC/
    ├── Configuration/
    ├── Endpoints/
    └── notification contract and intake files

Myrmex.Tests/
└── Integrations/
    ├── Authorization/
    └── OneC/
        ├── Endpoints/
        └── Synchronization/
```

**Structure Decision**: Keep all durable synchronization infrastructure in `Myrmex.Integrations`, with 1C-specific endpoint/auth/configuration code under `Myrmex.Integrations/OneC`. Register the integration services from `AddOneCIntegration`/ApiService composition without adding WMS dependencies or WebApp surface area.

## Architectural Design Notes

- **Domain concepts first**: The core concepts are external source identity, 1C change notification, provider-neutral synchronization request, idempotency identity, lifecycle state, retry schedule, wake-up signal, and synchronization processor. The request is technical integration state, not a WMS aggregate.
- **Shared contract boundary**: Do not add notification DTOs to `Myrmex.Shared` for this slice. The contracts are external HTTP intake contracts owned by `Myrmex.Integrations/OneC`; no WebApp or cross-project client consumes them.
- **Internal request boundary**: Persist/intake/processor operations may use internal commands or services in `Myrmex.Integrations`. Internal synchronization request models, EF entities, options, and handlers must not become public transport contracts.
- **Backend-owned projection**: No list or UI projection is added. Future administration/replay views will require a separate backend-owned projection and plan.
- **Server-driven list behavior**: Not applicable; no list endpoint is introduced.
- **Client/grid behavior**: Not applicable; no WebApp client or grid is introduced.
- **Cancellation and errors**: Notification endpoints are write/action operations. Valid new and duplicate notifications return empty `202 Accepted` only after durable commit. Authentication/authorization failures use the normal auth pipeline. Malformed contracts use existing ProblemDetails-style validation behavior and must not expose secrets. `ProcessingAttemptTimeoutSeconds` is a transient processing failure; host-shutdown cancellation leaves durable records `Processing` for abandoned recovery rather than marking them failed or scheduling a normal handler retry.
- **Health/readiness**: ApiService already calls `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()`. `Myrmex.ServiceDefaults` maps `/health` as the readiness endpoint and `/alive` as the liveness endpoint in Development/Staging; AppHost already uses `.WithHttpHealthCheck("/health")` for ApiService. The integration slice must reuse `/health` by registering integration readiness checks from `AddOneCIntegration`; `/alive` remains the platform self liveness check. Integration readiness must verify `IntegrationDbContext` persistence reachability, required integration configuration validation, and synchronization worker registration/loop readiness without exposing API keys, connection strings, external credentials, queue contents, synchronization-request details, or internal exception details.
- **Risk-based testing**: Protect exact JSON field binding through explicit JSON property mapping, required fields, GUID `Ref_Key` validation, non-empty bounded `DataVersion` decoding, field-identifying ProblemDetails-style validation responses, malformed Date rejection, unknown-property tolerance, API-key auth separation, existing WMS operator route preservation, durable uniqueness, duplicate lifecycle preservation, Date diagnostic handling, retry/lifecycle transitions including valid empty `RetryDelaysSeconds` with one attempt and no retries, processing-attempt timeout versus host-shutdown cancellation, startup scan, coalescing wake-up fallback, abandoned `Processing` recovery, and `/health` readiness integration. Omit WebApp/API-client tests because no client/UI is added.
- **Existing pattern precedence**: Follow existing Minimal API endpoint grouping in `OneCEndpoints`, existing policy constants in `MyrmexAuthorizationPolicies`, existing scheme constants in `MyrmexAuthenticationSchemes`, existing options binding style in `OneCOptions`, existing `TimeProvider` injection, and WMS `DbContext` registration style while keeping the new context integration-owned.

## Complexity Tracking

No constitution violations are planned.

### Principle IV Endpoint/UI Test Exceptions

| Deferred automated test | Why deferred | Lower-level automated coverage | Manual validation required | Follow-up issue needed? |
|-------------------------|--------------|--------------------------------|----------------------------|-------------------------|
| WebApp UI and API-client tests | No WebApp UI or first-party API client is introduced in this slice | Endpoint, persistence, lifecycle, and worker tests cover the feature-owned behavior | Quickstart HTTP notification checks after developer-controlled startup | No |

## Post-Design Constitution Check

- **Domain Model First**: Pass. Design artifacts keep synchronization concepts separate from WMS receiving/shipping domain behavior.
- **Modular Monolith Boundaries**: Pass. `Myrmex.Integrations` owns the queue and processor, 1C details stay under `OneC`, and WMS/Identity boundaries remain explicit.
- **Vertical Slice Delivery**: Pass. Contracts, persistence model, lifecycle, endpoint behavior, and validation guide are defined for independently testable notification intake and queue processing.
- **Testing Discipline**: Pass. The plan identifies focused tests at the owning layer and records the only endpoint/UI exception as non-applicable UI/client coverage.
- **Simplicity and Observability**: Pass. The selected design uses SQL as the durable queue and a bounded channel only as a wake-up signal; no broker, outbox, generalized ERP framework, replay surface, cleanup process, key hashing, or rotation infrastructure is added.
