# Add reactive and on-demand reference-data synchronization

## Context

Myrmex already supports operator-triggered full import of reference data from 1C for:

* `Warehouse`;
* `UnitOfMeasure`;
* `StockKeepingUnit`.

The existing implementation includes:

* `OneCImportService`;
* 1C OData transport DTOs;
* full collection loading for Warehouses and Units of Measure;
* paged loading for Stock Keeping Units;
* source-specific transport-to-application mapping;
* `ImportWarehouses`;
* `ImportUnitsOfMeasure`;
* `ImportStockKeepingUnits`;
* batch-based application handlers;
* source identity linking through `ExternalRefKey`;
* create and update behavior;
* deletion-mark inactivation;
* reactivation;
* validation and structured record errors;
* transactional batch processing and savepoints;
* SKU base-unit lookup through `BaseUnitOfMeasureExternalRefKey`;
* per-reference-type manual import gate;
* operator-facing manual import endpoints and responses.

Issue #104 established the external synchronization foundation:

* machine-to-machine API-key authentication;
* durable SQL synchronization requests;
* notification idempotency by source identity and `DataVersion`;
* background processing;
* handler resolution by stable internal `EntityType`;
* retry scheduling;
* processing-attempt timeout;
* abandoned `Processing` recovery;
* startup scanning;
* fallback SQL polling;
* best-effort wake-up signaling;
* `Pending`, `Processing`, `Deferred`, `Completed`, and `Failed` lifecycle states.

Issue #109 is intentionally implemented before Receiving and Shipping document synchronization.

The purpose of this issue is to implement and stabilize a complete business-processing path on the simpler existing reference entities before Issues #105 and #106 depend on reference-data synchronization.

## Goal

Provide three entry points into the same application import logic:

```text
reactive change notification
manual full import
on-demand synchronization of one reference
        ↓
shared reference-data application logic
```

The entry points may have different transport and orchestration behavior, but they must not implement separate rules for:

* source identity linking;
* create;
* update;
* validation;
* deactivation;
* reactivation;
* dependency lookup;
* persistence.

The existing `Import*.Command` handlers must remain the owners of these application rules.

## Supported reference types

This issue supports exactly:

* `Warehouse`;
* `UnitOfMeasure`;
* `StockKeepingUnit`.

Add stable internal synchronization entity types:

```text
Warehouse
UnitOfMeasure
StockKeepingUnit
```

1C OData entity-set names, Russian property names, and transport DTOs must remain inside the 1C integration adapter.

## Scope

Implement:

* reactive change-notification endpoints for all three supported reference types;
* durable synchronization requests through the existing Issue #104 foundation;
* loading one reference object from 1C by external identity;
* loading and storing source `DataVersion`;
* version-aware application outcomes;
* owned external import state for the three WMS reference aggregates;
* reuse and extension of the existing `Import*.Command` handlers;
* an internal synchronize-one reference capability;
* bounded SKU base-UoM dependency repair;
* protection of source-owned fields from local modification;
* preservation of the existing manual full-import operations;
* additive `Unchanged` reporting for manual import;
* only the minimum automated tests needed to prove the new behavior.

## Existing behavior to preserve

The following behavior must remain available:

* separate manual imports for Warehouses, Units of Measure, and SKUs;
* current operator authorization for manual import endpoints;
* current machine authorization for notification endpoints;
* SKU paging and deterministic source ordering;
* folder filtering and controlled folder skips;
* current code-conflict behavior;
* no automatic linking by mutable business code;
* transaction and savepoint behavior;
* committed earlier SKU batches remaining committed if a later batch fails;
* bounded structured error reporting;
* existing manual import routes;
* existing operator-facing import page.

Manual full import remains required for:

* initial loading;
* reconciliation;
* retry after source-data correction;
* operational repair.

## External import state

Replace the separate external import metadata concept with an owned value object named:

```csharp
ExternalImportState
```

Its normal initialized state contains:

```csharp
Guid RefKey
byte[] DataVersion
DateTimeOffset ImportedAtUtc
```

The value object:

* has no independent identity;
* has no independent lifecycle;
* is owned by the reference aggregate;
* represents the single currently supported external source link.

Do not introduce:

* `ExternalEntityLink`;
* a shared external-link table;
* provider polymorphism;
* multi-source identity inheritance;
* a generalized external-link framework.

Keep explicit SQL column names:

```text
ExternalRefKey
ExternalDataVersion
LastImportedAtUtc
```

The existing unique filtered indexes on `ExternalRefKey` must be preserved.

### Legacy linked records

Existing linked rows do not yet have `ExternalDataVersion`.

For migration compatibility:

* `ExternalDataVersion` may be `NULL` only for an existing linked record whose version has not yet been established;
* the existing `ExternalRefKey` and `LastImportedAtUtc` values must be preserved;
* the first successful version-aware synchronization must store the current source `DataVersion`;
* an empty byte array must not be used as a sentinel value;
* after a successful version-aware synchronization, a linked record must have a non-empty `ExternalDataVersion`.

`DataVersion` is synchronization metadata.

It is not:

* an EF Core concurrency token;
* a SQL row version;
* outbound optimistic concurrency support.

Binary version values must be compared by content and copied defensively when stored or exposed by the domain value object.

## Source ownership

After a reference record becomes linked to 1C, fields imported from 1C are source-owned and must not be changed through normal local edit operations.

### Warehouse

Source-owned:

* `Code`;
* `Name`;
* active/inactive state originating from `DeletionMark`;
* external import state.

WMS-owned:

* `Description`.

### UnitOfMeasure

Source-owned:

* `Code`;
* `Name`;
* `Symbol`;
* active/inactive state originating from `DeletionMark`;
* external import state.

### StockKeepingUnit

Source-owned:

* `Code`;
* `Name`;
* `BaseUnitOfMeasureId`;
* active/inactive state originating from `DeletionMark`;
* external import state.

WMS-owned:

* `Description`.

Local application operations must:

* reject attempts to modify source-owned fields of linked records;
* continue to permit modification of explicitly WMS-owned fields;
* preserve current behavior for records that are not linked to an external source.

This rule is required so that same-version synchronization can safely return `Unchanged` without comparing and restoring every source-owned field.

## DataVersion semantics

The object returned by the current OData GET is the source of truth.

The `DataVersion` received in a notification is used for:

* durable notification idempotency;
* diagnostics;
* identification of the notified source change.

The notification version is not a request to load historical source state.

A reactive handler must always load the current source object.

### Same current version

```text
local ExternalDataVersion == current OData DataVersion
→ do not invoke aggregate import mutation
→ do not change LastImportedAtUtc
→ do not change UpdatedAtUtc
→ do not emit domain events
→ return Unchanged
→ complete the synchronization request successfully
```

### Different or previously unknown version

```text
local ExternalDataVersion differs or is NULL
→ validate and apply current source state
→ save current OData DataVersion
→ update LastImportedAtUtc
→ return Applied
```

### Notification older than the current object

```text
notification V10
current OData GET returns V12
→ apply V12
→ save V12
→ complete request V10 successfully
```

A later synchronization request for V12 may then load the current object and complete as `Unchanged`.

Do not attempt to retrieve the historical notification version.

### Changed version without changed WMS fields

When `DataVersion` changed but all WMS-relevant values remained equal:

* store the new external version;
* update `LastImportedAtUtc`;
* report the record as applied/updated for import accounting;
* do not emit business-detail or activation domain events unless the corresponding business state actually changed.

`DomainValidationResult` must not be expanded into a universal outcome framework.

Introduce only a focused reference-import outcome sufficient to distinguish:

* applied;
* unchanged;
* controlled skip;
* validation or business failure.

## DeletionMark semantics

### Linked local record

```text
DeletionMark = true
+ linked local record exists
+ source version differs or is unknown
→ save current external version
→ update LastImportedAtUtc
→ deactivate the record if active
→ do not physically delete it
→ Applied
```

If the linked record is already inactive but the source version changed:

* save the new external version;
* update `LastImportedAtUtc`;
* do not emit another deactivation event.

### Reactivation

```text
DeletionMark = false
+ linked local record is inactive
→ validate and apply current source values
→ reactivate the record
→ save current external version
→ Applied
```

### Unlinked deletion-marked record

```text
DeletionMark = true
+ no linked local record exists
→ do not create an inactive local record
→ return a controlled skipped outcome
```

The controlled skip completes a reactive synchronization request successfully because retrying the same source state cannot create a valid local record.

Deletion processing must occur before validation of:

* reference detail fields;
* SKU base-unit dependencies.

Physical deletion is not used.

## Reactive notification endpoints

Add machine-authenticated endpoints:

```text
POST /api/integrations/1c/warehouses/changed
POST /api/integrations/1c/uoms/changed
POST /api/integrations/1c/skus/changed
```

Reference notification bodies contain:

```json
{
  "Ref_Key": "...",
  "DataVersion": "..."
}
```

`Number` and `Date` are document diagnostics and are not required for reference notifications.

The endpoints must reuse the existing Issue #104 intake path:

```text
authenticate
→ validate
→ create SynchronizationRequest
→ durable insert or duplicate resolution
→ commit
→ best-effort wake-up
→ empty 202 Accepted
```

Do not introduce another:

* synchronization queue;
* database table;
* worker;
* processor;
* polling loop;
* wake-up channel.

Existing Receiving and Shipping notification endpoints must remain unchanged.

## Single-object OData loading

Extend the 1C transport adapter with explicit operations equivalent to:

```csharp
ReadWarehouseAsync(Guid externalRefKey, ...)
ReadUnitOfMeasureAsync(Guid externalRefKey, ...)
ReadStockKeepingUnitAsync(Guid externalRefKey, ...)
```

Exact names may follow current project conventions.

Each operation must:

* search by `Ref_Key`;
* request only fields required for synchronization;
* include `DataVersion`;
* return the current source object;
* distinguish object found from object absent;
* distinguish object absence from transport failure;
* distinguish malformed response from source unavailability;
* honor configured timeout and cancellation;
* keep WMS domain types out of the transport layer.

Full collection and paged reads must also request and map `DataVersion`.

The application import items must receive decoded neutral binary version data rather than 1C JSON representations.

### Object absent

When a single-object GET does not find the requested object:

* do not create a local record;
* do not deactivate an existing linked record without an explicit current `DeletionMark`;
* return an explicit not-found result from the synchronize-one capability.

For a reactive synchronization request, object absence is a permanent failure with clear diagnostics.

For a direct internal on-demand call, object absence is returned explicitly as `NotFound`.

## Shared application import logic

Extend the existing import item contracts with external version data.

Preferred processing paths:

```text
Manual full import:
read collection or pages
→ map to Import*.Item[]
→ existing Import*.Command
```

```text
Reactive:
SynchronizationRequest handler
→ synchronize-one orchestration
→ read one object
→ map to one Import*.Item
→ existing Import*.Command([item])
```

```text
On-demand:
internal synchronize-one operation
→ same read-one orchestration
→ same Import*.Command([item])
```

Do not create separate Warehouse, SKU, or UoM upsert handlers that duplicate existing import rules.

The existing batch commands remain responsible for:

* lookup by external identity;
* prevention of automatic linking by code;
* create;
* update;
* code-conflict detection;
* deletion-mark behavior;
* reactivation;
* domain validation;
* dependency lookup;
* persistence;
* domain-event dispatch;
* transaction and savepoint behavior.

## Import outcomes and counts

The batch result must add an `Unchanged` count.

The count invariant becomes:

```text
Processed =
    Created
  + Updated
  + Unchanged
  + Skipped
  + Failed
```

For single-item synchronization:

* `Created` maps to `Applied`;
* `Updated` maps to `Applied`;
* `Unchanged` maps to `Unchanged`;
* source folders map to a controlled skip;
* unlinked deletion-marked records map to a controlled skip;
* validation and business conflicts remain explicit failures.

Do not classify an unchanged source version as:

* updated;
* skipped;
* failed.

## Internal synchronize-one capability

Provide an internal capability to synchronize one supported reference by:

* supported reference type;
* external identity.

The capability is intended for:

* reactive synchronization handlers in this issue;
* Receiving dependency repair in Issue #105;
* Shipping dependency repair in Issue #106.

No new public operator HTTP endpoint is required.

No new WebApp operation for synchronizing one object is required.

Manual full import remains the operator-facing repair operation.

Do not implement a generic polymorphic synchronization framework.

## Bounded SKU base-UoM repair

`StockKeepingUnit` currently has one modeled external dependency:

```text
StockKeepingUnit
→ Base UnitOfMeasure
```

Reactive and on-demand SKU synchronization must support one bounded repair attempt:

```text
read current SKU
→ base UoM is missing or inactive
→ synchronize that UoM once
→ retry SKU application once
```

It is also acceptable to detect and synchronize the base UoM before the first SKU application attempt.

The resulting behavior must remain bounded:

* synchronize at most one UoM for the current SKU operation;
* retry SKU application at most once;
* do not recurse;
* do not construct a dependency graph;
* do not construct a generic dependency resolver;
* do not create unlimited retry chains.

If the UoM:

* does not exist;
* cannot be loaded;
* is deletion-marked and unlinked;
* remains inactive;
* fails validation;

then SKU synchronization returns an explicit failure.

Manual full SKU import is not required to perform one OData request for every missing UoM.

Its current batch-oriented behavior may continue to require Units of Measure to be imported first.

## Synchronization request handlers

Register reference synchronization handlers through the existing `ISynchronizationHandler` resolution mechanism.

Handlers must remain thin.

Their responsibilities are limited to:

* validating the synchronization request identity;
* invoking the appropriate synchronize-one capability;
* mapping its result to the existing synchronization handler result.

Result mapping:

* successful `Applied` → `Completed`;
* successful `Unchanged` → `Completed`;
* controlled source-folder skip → `Completed`;
* controlled unlinked-deletion skip → `Completed`;
* source timeout or temporary unavailability → `TransientFailure`;
* temporary synchronization gate contention → `TransientFailure`;
* malformed source response → `PermanentFailure`;
* source object absent → `PermanentFailure`;
* application validation failure → `PermanentFailure`;
* unresolved business conflict → `PermanentFailure`.

Do not add the following synchronization-request lifecycle statuses:

* `Applied`;
* `Unchanged`;
* `Skipped`;
* `NotFound`.

They are operation outcomes, not durable queue states.

## At-least-once behavior

WMS persistence and synchronization-request persistence use separate contexts and transactions.

Reference handlers must therefore be safe for at-least-once execution.

Example:

```text
WMS reference update committed
→ application stops before synchronization request becomes Completed
→ request is recovered and retried
→ current GET returns the already stored DataVersion
→ application returns Unchanged
→ no duplicate mutation or domain event occurs
→ request becomes Completed
```

A distributed transaction between WMS persistence and synchronization-request persistence must not be introduced.

## Concurrency between manual and reactive synchronization

Manual full import and reactive/on-demand synchronization must not apply stale source observations concurrently for the same reference type.

Use a simple shared in-process coordination gate per supported reference type.

The gate must cover the source read and corresponding application commit so that the following cannot occur in one application instance:

```text
manual import reads V10
reactive synchronization applies V12
manual import later applies stale V10
```

Required behavior:

* only one read-and-apply synchronization operation for the same reference type may run at a time;
* different reference types may run concurrently;
* a manual import retains its current fail-fast operator behavior when the same reference type is already being synchronized;
* a reactive handler encountering a busy gate returns a transient failure and relies on the existing synchronization retry policy;
* internal on-demand synchronization returns an explicit transient/busy outcome to its caller;
* cancellation must be honored;
* opaque `DataVersion` values must not be compared numerically or lexicographically.

Do not introduce:

* distributed locks;
* SQL application locks;
* cross-process lock infrastructure;
* a generalized synchronization coordinator.

This issue guarantees serialization for the current single-application-instance deployment model.

Cross-instance synchronization coordination is outside scope and must not be implied by the implementation.

## Manual import compatibility

Existing manual import routes must remain unchanged.

Existing operator authorization must remain unchanged.

Existing response fields must remain available.

Add `Unchanged` as an additive response field.

The operator-facing import page must display the new count consistently with:

* created;
* updated;
* skipped;
* failed.

The existing structured error contract and maximum returned-error limit must remain unchanged.

Repeated full import of unchanged source versions must report those records as `Unchanged`, not `Updated`.

## Minimal automated testing

Add only tests that prove behavior newly introduced by this issue.

Do not duplicate broad infrastructure coverage already provided by Issue #104.

Prefer focused or parameterized tests where the same rule applies to all three reference types.

The minimum required coverage is:

1. **External import state and application outcome**

   * an existing linked record with `ExternalDataVersion = NULL` stores the first loaded version;
   * the same version returns `Unchanged` without changing timestamps or emitting domain events;
   * a different version applies current source state and stores the new version.

2. **Reference lifecycle**

   * a linked deletion-marked record is deactivated;
   * an unlinked deletion-marked record is skipped without creation;
   * active source data reactivates a linked inactive record.

3. **Source ownership**

   * linked records reject local changes to source-owned fields;
   * explicitly WMS-owned fields remain editable.

4. **Single-object transport**

   * one representative successful read and mapping test per transport shape;
   * object absence;
   * malformed response;
   * transient source failure.

5. **Reactive processing**

   * notification routing creates the correct stable reference `EntityType`;
   * one representative synchronization handler test proves `Applied` and `Unchanged` both complete the durable request;
   * transient and permanent result mapping is covered without repeating the entire processor test suite.

6. **SKU dependency repair**

   * missing base UoM is synchronized once and SKU application succeeds;
   * failed repair does not recurse or retry SKU more than once.

7. **Manual import compatibility**

   * `Unchanged` is included in batch and public response counts;
   * the existing manual import route and structured error shape remain compatible.

8. **Concurrency**

   * the per-reference-type gate prevents simultaneous same-type read-and-apply operations;
   * different reference types are not unnecessarily serialized.

Tests should use existing SQL-backed conventions where persistence or transaction behavior is material.

Do not add separate duplicate tests for every reference type when a parameterized or representative test proves identical infrastructure behavior.

## Out of scope

Do not implement:

* Receiving document synchronization;
* Shipping document synchronization;
* external document snapshots;
* document conflict models;
* outbound OData `PATCH`;
* partner, sender, recipient, or polymorphic party master data;
* characteristics;
* packaging;
* series;
* dimensions;
* volume;
* gross or net weight;
* generalized `ExternalEntityLink`;
* multi-provider integration abstractions;
* arbitrary source systems;
* metadata-driven OData mapping;
* a generic polymorphic synchronization engine;
* arbitrary reference types;
* a recursive dependency resolver;
* a dependency DAG engine;
* distributed locking;
* administrative replay UI;
* a public synchronize-one endpoint;
* unrelated WMS or integration refactoring.

## Relationship to subsequent issues

Issue #109 provides the internal capability needed by Issues #105 and #106:

```text
document synchronization
→ required Warehouse, SKU, or UoM is missing
→ invoke synchronize-one reference capability
→ retry document mapping in a bounded manner
```

Issue #105 will implement:

* Receiving document OData GET;
* canonical external Receiving snapshot;
* Receiving aggregate synchronization;
* plan lines;
* source conflict handling;
* use of the reference synchronize-one capability.

Issue #106 will implement:

* Shipping document OData GET;
* demand and execution models;
* Shipping aggregate synchronization;
* source conflict handling;
* use of the reference synchronize-one capability.

Issues #107 and #108 will later implement durable outbound status and execution-fact synchronization.

Issue #109 must not introduce a common document/reference inheritance hierarchy.
