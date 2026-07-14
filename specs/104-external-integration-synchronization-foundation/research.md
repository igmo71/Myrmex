# Research: External Integration Synchronization Foundation

## Decision: Use an integration-owned persistence context and schema

**Rationale**: The stakeholder document explicitly keeps queue ownership outside the WMS domain and prefers a dedicated integration persistence boundary using the existing Myrmex database connection. This matches the modular-monolith constitution while avoiding a service split or broker.

**Alternatives considered**:
- Store queue records in `WmsDbContext`: rejected because synchronization queue infrastructure is not WMS domain ownership.
- Add a separate database: rejected as unnecessary operational complexity for the first slice.
- Use only in-memory queueing: rejected because accepted notifications must survive restarts and missed wake-up signals.

## Decision: Keep 1C notification contracts inside `Myrmex.Integrations/OneC`

**Rationale**: The notification JSON uses exact 1C names and is called directly by 1C. There is no WebApp or shared client boundary in this feature, so placing these contracts in `Myrmex.Shared` would leak provider-specific transport into shared public contracts unnecessarily.

**Alternatives considered**:
- Add shared DTOs in `Myrmex.Shared`: rejected because no Myrmex client consumes these contracts.
- Reuse OData transport models: rejected because notifications are webhook-style change notifications, not OData document reads.

## Decision: Add a dedicated API-key authentication scheme and OneC integration policy

**Rationale**: The external caller is a machine identity and must not satisfy Identity user-session role or stable GUID user id rules. A dedicated scheme and policy preserve the default Identity API-session cookie for existing user-operated endpoints while allowing notification endpoints to authorize machine calls.

**Alternatives considered**:
- Make API-key authentication the default scheme: rejected because it would risk breaking existing Identity-protected API behavior.
- Use WMS operator roles for 1C machine notifications: rejected because the machine principal is not an Identity user.
- Change existing manual 1C endpoints to API key: rejected by scope; those endpoints remain user-operated WMS actions.

## Decision: Use SQL-backed durable queue plus bounded Channel wake-up

**Rationale**: The database is the source of truth. A bounded Channel can reduce polling latency inside a running process, but durability and recovery come from persisted status, startup scan, fallback polling, and abandoned-processing recovery.

**Alternatives considered**:
- RabbitMQ or other broker: rejected as explicitly out of scope.
- Outbox: rejected because outbound operations are out of scope.
- Channel-only processing: rejected because it cannot guarantee recovery after process failure.

## Decision: Implement lifecycle states `Pending`, `Processing`, `Deferred`, `Completed`, and `Failed`

**Rationale**: These states are accepted in the stakeholder document and cover eligibility, claims, unsupported handlers, successful handler completion, and terminal failures. `Deferred` protects first-slice behavior by preserving requests until receiving/shipping handlers exist.

**Alternatives considered**:
- Mark selected receiving/shipping notifications `Completed` without handlers: rejected because it would falsely claim synchronization success.
- Add `Superseded` now: rejected as a later extension; the first slice has no superseding behavior.

## Decision: Preserve duplicate notification state without implicit replay or repair

**Rationale**: The API contract must be idempotent and opaque. Returning `202` for duplicates while mutating failed/deferred/completed states would make duplicate delivery an accidental administrative command. A duplicate of a `Pending` request may only send a best-effort wake-up signal.

**Alternatives considered**:
- Retry failed requests on duplicate receipt: rejected because replay/repair needs explicit operational control.
- Reopen deferred requests on duplicate receipt: rejected because handler availability, not duplicate receipt, determines replay readiness.

## Decision: Support one active 1C source instance and one active API key in the first slice

**Rationale**: This matches current scope while preserving `SourceInstance` in the idempotency key and persistence model for future multi-infobase support.

**Alternatives considered**:
- Full multi-infobase/key management now: rejected as out of scope and not needed for first accepted behavior.
- Omit `SourceInstance`: rejected because it would make future multi-instance support harder and weaken idempotency.

## Decision: Do not implement cleanup, archival, or retention deletion

**Rationale**: The first slice needs durable auditability for support and later replay. Operational volume and support requirements are not known yet, so automated deletion could remove useful diagnosis or replay data prematurely.

**Alternatives considered**:
- Delete completed records after a fixed period: rejected because no retention policy is accepted.
- Add cleanup worker with disabled default: rejected because it still adds unneeded feature surface and tasks.

## Decision: Use explicit retry delay collection

**Rationale**: Configured delay values are observable in scheduling tests and avoid hidden formulas. The number of attempts can be derived from the collection to avoid contradictory settings.

**Alternatives considered**:
- Exponential backoff formula: rejected because the stakeholder document prefers explicit delays.
- Separate max attempts setting: rejected for first slice to avoid contradiction with delay count.

## Decision: Use application-managed concurrency token and provider-neutral claim behavior

**Rationale**: The stakeholder document explicitly rejects SQL Server-specific rowversion. Application-managed concurrency keeps state transitions and claims testable and portable at the domain/application level even though the normal deployment database is SQL Server.

**Alternatives considered**:
- SQL Server `rowversion`: rejected by accepted decision.
- Process-local locks only: rejected because multiple application instances must coordinate safely.
