# Phase 0 Research: Reactive and On-Demand Reference-Data Synchronization

**Feature**: `109-add-reactive-and-on-demand-reference-data-synchronization`  
**Date**: 2026-07-16

## Decision 1: Extend the Feature 104 synchronization foundation

**Decision**: Add `Warehouse`, `UnitOfMeasure`, and `StockKeepingUnit` to the existing stable synchronization entity types; add three thin `ISynchronizationHandler` implementations; register them with the existing resolver, processor, worker, request store, retry policy, polling loop, and wake-up signal. Extend the existing `OneCNotificationEndpoints` group with the three approved routes and reuse `OneCChangeNotificationRequest`, its validator, `SynchronizationRequestFactory`, and `SynchronizationRequestStore`.

**Rationale**: Feature 104 already owns durable intake, idempotency, lifecycle transitions, retry, abandoned-processing recovery, polling, and wake-up. The existing notification request already requires `Ref_Key` and `DataVersion` while treating document number/date as optional.

**Alternatives considered**: A reference-specific queue, processor, lifecycle, or notification contract was rejected because it would duplicate Feature 104 and create incompatible operational behavior.

## Decision 2: Reuse the three import handlers as the application boundary

**Decision**: Extend `ImportWarehouses`, `ImportUnitsOfMeasure`, and `ImportStockKeepingUnits` items with the decoded non-empty source version. Extend their existing handlers to detect equal versions, apply changed or legacy-unversioned source state, preserve deletion-first behavior, and return `Unchanged` in `ReferenceImportBatchResult`. Manual full import dispatches the same batch commands; reactive and on-demand flows dispatch a one-item command.

**Rationale**: These handlers already own external-identity lookup, code-conflict prevention, create/update, validation, deletion/reactivation, SKU base-UoM lookup, transactions/savepoints, persistence, and domain-event dispatch. Extending them keeps one implementation of the business rules across all entry points.

**Alternatives considered**: Separate reactive upsert handlers and a generalized import/outcome framework were rejected as duplicated logic and unnecessary abstraction. `DomainValidationResult` remains a validation result rather than becoming a universal synchronization outcome.

## Decision 3: Model external import state as an optional owned value object

**Decision**: Introduce one internal WMS-domain `ExternalImportState` value object used as an optional owned part of each supported aggregate. It contains `RefKey`, nullable legacy `DataVersion`, and `ImportedAtUtc`; it has no identity or independent lifecycle. New successful imports require a non-empty version. Version input is copied, any exposed value is copied, and content equality is used.

Map the owned values into the existing aggregate tables with exact columns `ExternalRefKey`, `ExternalDataVersion`, and `LastImportedAtUtc`. `ExternalDataVersion` is nullable `varbinary(128)`. Preserve the existing unique filtered indexes on non-null `ExternalRefKey` with their current names and filters.

**Rationale**: This captures the single supported source link without a separate entity/table, satisfies binary immutability and legacy null-version semantics, and preserves the physical persistence contract.

**Alternatives considered**: Keeping three unrelated scalar fields on every aggregate was rejected because the specification defines a cohesive identity-less import state. A shared external-link table, provider hierarchy, concurrency token, SQL row version, or polymorphic link abstraction was rejected as out of scope.

## Decision 4: Define the developer-generated additive migration shape

**Decision**: The agent modifies EF mappings only. The developer generates, reviews, and applies the migration. The expected migration shape is exactly three nullable `ExternalDataVersion` columns—one each on `wms.warehouses`, `wms.units_of_measure`, and `wms.stock_keeping_units`—with no changes to existing `ExternalRefKey` or `LastImportedAtUtc` columns or their filtered unique indexes. The agent does not create or edit migration `.cs`, `.Designer.cs`, or `WmsDbContextModelSnapshot` files.

**Rationale**: Existing identities and import timestamps are production data. The CLR ownership refactor must not become a physical data migration.

**Alternatives considered**: Backfilling an empty version, making the new column non-null, or recreating legacy columns/indexes was rejected because legacy version is genuinely unknown and existing values must be preserved.

## Decision 5: Add explicit current-object reads to the 1C adapter

**Decision**: Add `DataVersion` to the Warehouse, UoM, and SKU transport DTOs and to all full-read projections. Add three explicit `Ref_Key`-filtered current-object reads on `IOneCODataClient`; use the existing OData collection envelope, authentication, configured timeout, and error taxonomy. Zero rows is an explicit absent result. Multiple rows, malformed Base64/binary version, an empty version, or an invalid envelope is malformed source data. Only Warehouse and SKU carry `IsFolder` and can produce a folder skip.

**Rationale**: Type-specific methods match the current explicit adapter and preserve source transport details outside WMS commands. Reusing the transport machinery preserves caller cancellation versus timeout/source/malformed distinctions.

**Alternatives considered**: Historical version reads, metadata-driven projections, a generic provider client, and adding folder fields to UoM were rejected by the approved scope.

## Decision 6: Add a narrow synchronize-one orchestration service

**Decision**: Add an internal 1C reference synchronization service limited to the exact three supported reference kinds and an external GUID. It acquires the existing per-type gate, reads one current object, maps it to the corresponding existing import command, dispatches the one-item command, and returns a focused result: `Applied`, `Unchanged`, `ControlledSkip`, `NotFound`, `Busy`, `TransientFailure`, or `PermanentFailure`, plus a bounded diagnostic reason where relevant.

The synchronize-one service returns `NotFound` for source-object absence; the reactive Feature 104 handler maps that outcome to its existing `PermanentFailure` result. The service's own `PermanentFailure` outcome is limited to invalid/disabled configuration, authentication rejection, unavailable entity set, malformed data, validation failure, or unresolved conflict. Source timeout/unavailability and gate contention map to `TransientFailure`; applied/unchanged/applicable controlled-skip outcomes map to `Completed`. These are mappings to existing durable lifecycle behavior, not new durable statuses.

**Rationale**: Later Receiving and Shipping slices need a single internal call, but no public endpoint or general synchronization framework is needed.

**Alternatives considered**: A public synchronize-one API, provider-neutral synchronization engine, arbitrary reference-type registry, or new durable statuses was rejected as out of scope.

## Decision 7: Extend the existing singleton gate

**Decision**: Reuse `OneCImportGate` and its three independent `SemaphoreSlim(1,1)` instances. Manual imports keep the current fail-fast `Acquire` behavior and current lease scope. Add a non-waiting acquisition path for synchronize-one so contention becomes an explicit `Busy` outcome. A manual lease begins before configuration/source read and remains through the entire operation; the SKU lease covers every page and committed batch. A synchronize-one lease begins before the object read and remains through its import-command commit.

**Rationale**: The existing singleton already provides exactly the approved single-application-instance, per-reference-type coordination. Different keys remain independent.

**Alternatives considered**: Waiting locks, a generalized coordinator, SQL application locks, distributed locks, and cross-process coordination were rejected.

## Decision 8: Keep SKU-to-UoM repair bounded and one-directional

**Decision**: While holding the SKU lease, apply the SKU once. Only `BaseUnitOfMeasureNotImported` or `BaseUnitOfMeasureInactive` with a valid source key may trigger one UoM synchronize-one call. If that produces an active applicable UoM, apply the same SKU command once more. No other result triggers repair, and no path synchronizes more than one UoM or applies the SKU more than twice. UoM busy/temporary failure remains transient; absent, invalid, deletion-skipped, or still-inactive UoM produces an explicit permanent/business failure.

**Rationale**: This repairs the only modeled external dependency while keeping calls and failure behavior deterministic. The dependency direction is SKU to UoM only, so no recursive cycle or graph is introduced.

**Alternatives considered**: Preloading dependency graphs, recursive synchronize-one, arbitrary repair chains, and per-SKU repair during manual full import were rejected.

## Decision 9: Enforce source ownership on actual local changes

**Decision**: Keep external import state absent from public edit contracts. For linked records, compare normalized requested source-owned values with current values before mutation: Warehouse rejects an actual Name change but allows Description changes; UoM rejects actual Name or Symbol changes and accepts identical resubmission; SKU rejects actual Name or BaseUnitOfMeasureId changes but allows Description changes. Code is not locally editable today. Normal deactivate/reactivate handlers reject only an actual linked state transition and permit a redundant no-op. Import application uses its dedicated domain path and remains able to update source-owned state.

For a linked SKU with an unchanged base UoM, skip reassignment validation so Description-only editing is not blocked by a source-owned dependency that is currently inactive. Emit detail/lifecycle events only when their business state changes; a metadata-only version update remains an applied import without a business event.

**Rationale**: Same-version no-op is safe only when local operations cannot diverge source-owned values. Comparing actual changes preserves WMS-owned editing and avoids rejecting ordinary form resubmission.

**Alternatives considered**: Rejecting every edit request containing source-owned values or exposing import metadata for local edits was rejected by the specification.

## Decision 10: Preserve cancellation and recovery ownership

**Decision**: Direct internal on-demand caller cancellation propagates to that caller and is not translated into an operation outcome. During reactive processing, change the existing Feature 104 processor catch so it logs and rethrows `OperationCanceledException` as shutdown cancellation only when the processor/application stopping token is cancelled, after leaving the durable request in `Processing`. Source timeout and non-shutdown failures continue through their existing transient/permanent classification. The existing worker consumes its stopping-token cancellation, and the existing abandoned-processing recovery later requeues or fails the request under the existing retry policy. Manual import retains its current caller-facing incomplete `Cancelled` response behavior. No durable cancelled status is added.

**Rationale**: This propagates shutdown cancellation without inventing a durable status or duplicating recovery. It is the smallest change to the foundation behavior introduced by Feature 109.

**Alternatives considered**: Treating every `OperationCanceledException` as application shutdown, mapping shutdown cancellation to completed/transient/permanent failure, adding a cancelled status, or immediately rewriting durable state during shutdown was rejected.

## Decision 11: Make manual response/UI changes additive

**Decision**: Add integer `Unchanged` to `ReferenceImportBatchResult` and public `OneCImportResponse`, update aggregation and the count invariant, and display it on the existing WebApp import page. Add `Common.Unchanged` to neutral, `en-US`, and `ru-RU` resources. Preserve all routes, authorization, existing response/error fields, error cap, paging, partial committed counts, and WebApp actions.

**Rationale**: The operator workflow remains unchanged while reporting version-aware no-ops accurately.

**Alternatives considered**: A new page, public synchronize-one action, replacing Updated semantics, or changing the structured error contract was rejected.

## Decision 12: Use a risk-based minimal test set

**Decision**: Extend current test source rather than create new parallel suites. Add minimal same-version smoke coverage for each explicit handler—`ImportWarehouses`, `ImportUnitsOfMeasure`, and `ImportStockKeepingUnits`—using one parameterized theory or one compact existing-handler test per type. Each smoke case proves only `same current DataVersion -> Unchanged -> no timestamp mutation -> no aggregate mutation or domain event`. Keep Warehouse as the representative slice for legacy-version, changed-version, and lifecycle coverage; add only UoM symbol and SKU base-UoM/repair cases where rules differ; cover Warehouse/SKU folders only. Add one value-object defensive-copy test and parameterized EF metadata compatibility coverage. Update the existing Feature 104 cancellation test source for stopping-token propagation and rely on its existing abandoned recovery tests. Extend existing manual endpoint/client/import tests for `Unchanged`; retain manual localized UI acceptance expectations because no component-test framework exists.

Do not retest Feature 104 queue uniqueness, retry schedules, polling, wake-up, authentication, generic notification validation, duplicate handling, or recovery algorithms. Do not clone an identical transport, handler, endpoint, or client suite for each reference type.

**Rationale**: Each planned test protects a new Feature 109 regression risk at the lowest owning layer and follows the durable Myrmex testing guidance.

**Alternatives considered**: Full per-type matrices and end-to-end duplication of the Feature 104 processor suite were rejected as high-maintenance tests without distinct risk coverage.

## Decision 13: Keep command execution developer-controlled

**Decision**: The agent may modify domain/application source code, test code, and EF mappings. The developer generates, reviews, and applies migrations. The agent does not generate, create, or edit migration `.cs`, `.Designer.cs`, or `WmsDbContextModelSnapshot` files, and planning and later task generation do not schedule migration-generation, database-update, build, test, AppHost, Docker, application-startup, or other environment-changing command execution unless explicitly requested.

**Rationale**: Planning must distinguish implementation artifacts from execution against the developer's environment and database.

**Alternatives considered**: Treating command execution as an automatic implementation or validation step was rejected because it exceeds the approved workflow boundary.

## Clarification Status

No unresolved planning questions remain. Repository inspection resolved ordinary implementation choices, and no stakeholder contradiction blocks design.
