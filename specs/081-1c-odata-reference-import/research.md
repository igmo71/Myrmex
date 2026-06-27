# Research: 1C OData Reference Import MVP

## Decision: Separate OneC Integration Adapter Project

**Decision**: Create `Myrmex.Integrations/Myrmex.Integrations.csproj` targeting .NET 10. Put all integration code under `Myrmex.Integrations.OneC`. Reference `Myrmex.Modules.Wms`, `Myrmex.AppDispatching`, `Myrmex.AspNetCore`, `Myrmex.Core`, and `Myrmex.Shared` only where their public types are used. Add the project to `Myrmex.slnx`; reference it from `Myrmex.ApiService` and `Myrmex.Tests`.

**Rationale**: This enforces the required one-way adapter dependency while leaving WMS independent of 1C and keeping the integration inside the modular monolith.

**Alternatives considered**:

- Put OData code in WMS: rejected because source transport concepts would leak into the WMS module.
- Put integration code directly in `Myrmex.ApiService`: rejected because endpoint composition and source transport would become mixed with the host.
- Create a separate deployable service: rejected because the constitution and MVP do not justify distributed-system complexity.

## Decision: Keep Source DTOs and Names Inside the OneC Boundary

**Decision**: Use private/internal DTOs and an OData collection envelope inside `Myrmex.Integrations.OneC.Transport`. DTO properties retain the actual 1C identifiers needed by each source: warehouse includes `Ref_Key`, `DeletionMark`, `IsFolder`, optional `Code`, and `Description`; UoM includes `Ref_Key`, `DeletionMark`, `Code`, `Description`, `НаименованиеПолное`, and `МеждународноеСокращение`; nomenclature includes `Ref_Key`, `DeletionMark`, `IsFolder`, `Code`, `Description`, `НаименованиеПолное`, `Артикул`, and nullable `ЕдиницаИзмерения_Key`. `Ref_Key` is a `Guid`; `ЕдиницаИзмерения_Key` is nullable `Guid` so missing and empty values can be classified. `System.Text.Json` attributes make wire names explicit. DTOs are mapped immediately to neutral WMS import items and never returned by API endpoints.

**Rationale**: Exact source naming makes deserialization auditable without contaminating WMS domain language. `Guid` preserves the immutable identity type supplied by 1C.

**Alternatives considered**:

- Rename every DTO property to English: rejected because it obscures the source contract and increases mapping mistakes.
- Reuse DTOs as WMS commands or public responses: rejected because it violates both module and transport boundaries.
- Use dynamic dictionaries for all source records: rejected because required fields would lose compile-time shape and validation clarity.

## Decision: Apply Source-Specific Field Mapping

**Decision**: Always map `Ref_Key -> ExternalRefKey` and `DeletionMark -> deletion intent`. Trim source codes before WMS normalization. For `Catalog_УпаковкиЕдиницыИзмерения`, use trimmed non-empty `НаименованиеПолное`, otherwise `Description`, as `Name`; use trimmed non-empty `МеждународноеСокращение`, otherwise `Description`, as `Symbol`. For nomenclature, use trimmed non-empty `НаименованиеПолное`, otherwise `Description`, as `Name`; keep `Артикул` transport-only because the current SKU aggregate has no article-number concept. For warehouses, map `Description -> Name`; use a trimmed source `Code` when the publication exposes it, otherwise use uppercase `Ref_Key` in 32-character `N` format as a warehouse-only deterministic code.

**Rationale**: The samples provide semantically stronger full-name and international-symbol fields. The exact 32-character GUID fallback satisfies the existing warehouse code limit without truncation and remains deterministic. It is not appropriate for SKU or UoM because those source codes are required business data.

**Alternatives considered**:

- Map `Артикул` to `StockKeepingUnit.Description`: rejected as semantic data corruption.
- Add a new SKU article-number field: rejected because the approved feature specification does not define that WMS concept or its uniqueness/lifecycle rules.
- Use the warehouse code fallback for UoM or SKU: rejected because only the warehouse publication may omit `Code`; SKU and UoM records without valid codes fail existing WMS validation.
- Truncate or hash `Ref_Key` into a prefixed warehouse code: rejected because `Ref_Key.ToString("N")` already fits the 32-character limit and preserves the complete identity text.

## Decision: Resolve Each SKU Base UoM from `ЕдиницаИзмерения_Key`

**Decision**: Map nullable 1C `ЕдиницаИзмерения_Key` directly to `ImportStockKeepingUnits.Item.BaseUnitOfMeasureExternalRefKey`. The WMS SKU import handler resolves each non-empty key to one active imported `UnitOfMeasure` by `ExternalRefKey`. A missing/null/empty key fails that record as `BaseUnitOfMeasureExternalRefKeyMissing`; an unmatched key fails as `BaseUnitOfMeasureNotImported`; an inactive match fails as `BaseUnitOfMeasureInactive`. Other SKU records in the batch continue. UoM import precedes SKU import in validation guidance.

**Rationale**: The actual nomenclature sample supplies the source relationship needed by the existing SKU invariant. External identity produces the correct per-SKU local relationship without code matching or environment-specific local IDs.

**Alternatives considered**:

- Configure one default UoM for all SKUs: rejected because it discards the actual per-nomenclature relationship and can assign incorrect operational units.
- Configure a local `BaseUnitOfMeasureId`: rejected because local database identities are environment-specific and expose WMS persistence details in source configuration.
- Match the base UoM by code: rejected because code matching is explicitly not an identity/linking mechanism.
- Make `BaseUnitOfMeasureId` optional for imported SKUs: rejected because it breaks the existing SKU invariant and downstream inventory behavior.

## Decision: Explicit OData Queries and Deterministic Offset Paging

**Decision**: Build query parameters explicitly and URL-encode entity/property names. Warehouses select `Ref_Key,DeletionMark,IsFolder,Code,Description` when source `Code` is configured as available, or omit `Code` otherwise. UoM uses entity set `Catalog_УпаковкиЕдиницыИзмерения` and selects `Ref_Key,DeletionMark,Code,Description,НаименованиеПолное,МеждународноеСокращение`. Nomenclature selects `Ref_Key,DeletionMark,IsFolder,Code,Description,НаименованиеПолное,Артикул,ЕдиницаИзмерения_Key` plus `$skip={offset}` and `$top={BatchSize}`. All reads use `$format=json` and `$orderby=Ref_Key`. Prefer `$filter=IsFolder eq false` for warehouse and nomenclature when the publication supports it; otherwise omit the filter and skip `IsFolder=true` records with stable reason `SourceFolder`. Advance offset by returned count and stop when page count is below batch size. Default `BatchSize` is 1,000, valid range 1–5,000.

**Rationale**: Stable source-identity ordering is required for repeatable offset paging, while explicit selection bounds transfer and deserialization work. The stop rule handles empty catalogs and exact-multiple final pages.

**Alternatives considered**:

- Follow arbitrary server ordering: rejected because `$skip`/`$top` can omit or duplicate records without stable ordering.
- Load all nomenclature in one request: rejected because the feature must support more than 15,000 records with bounded memory.
- Add delta tokens or continuation-link infrastructure: rejected because the target contract specifies offset paging and the MVP is manual full import.
- Require `$filter=IsFolder eq false` on every publication: rejected because compatibility may vary; client-side folder skipping remains the required fallback.

## Decision: Per-Request Timeout and No Automatic Retry

**Decision**: Use a typed `HttpClient` with a configurable per-source-request timeout, default 30 seconds. Propagate the caller cancellation token. Do not add automatic retries in the MVP; an operator reruns the idempotent import after correcting a transient issue.

**Rationale**: GET retries can interact with a changing offset-paged source and can extend a synchronous request unpredictably. Manual retry keeps behavior visible and uses already-required idempotency.

**Alternatives considered**:

- Unlimited HTTP timeout: rejected because a synchronous action could hang indefinitely.
- Automatic retry/resilience policies: deferred until target-server behavior and retry budgets are measured.
- Background continuation after caller cancellation: rejected by the synchronous execution decision.

## Decision: Explicit Publication-Compatibility Options

**Decision**: Keep `UnitsOfMeasureEntitySet` configurable but set/document `Catalog_УпаковкиЕдиницыИзмерения` as the target value. Add `WarehouseCodeAvailable` to decide whether warehouse `$select` includes `Code`; when false or when a returned code is empty, use the warehouse-only GUID code fallback. Add `UseFolderFilter` to prefer `$filter=IsFolder eq false` for warehouse and nomenclature; when false because the publication rejects that filter, fetch `IsFolder` and skip folders client-side. These are deployment configuration only and do not enter WMS commands except through mapped item values.

**Rationale**: Field/filter capability is a property of the target publication. Explicit options keep query construction deterministic and avoid using an OData failure as normal control flow.

**Alternatives considered**:

- Hard-code every publication capability: rejected because warehouse `Code` and folder filtering can differ.
- Probe and retry failed queries automatically: rejected because compatibility errors become ambiguous and connection testing should identify configuration deliberately.

## Decision: WMS Owns Neutral Batch Upsert Commands

**Decision**: Add public WMS command shells `ImportWarehouses`, `ImportUnitsOfMeasure`, and `ImportStockKeepingUnits` with nested neutral `Item` records. Keep handlers and EF access internal. Each handler preloads records by source identity and normalized code, applies domain validation/lifecycle rules, and returns a neutral `ReferenceImportBatchResult` with counts and uncapped internal errors. `IsFolder` never enters WMS items: the OneC mapper holds `SourceFolder` skips as pending mapping outcomes and merges them only after the corresponding WMS source batch completes.

**Rationale**: The integration project needs a compile-time WMS boundary without access to domain entities. Explicit commands match the existing dispatcher and constitution.

**Alternatives considered**:

- Public repository interfaces: rejected because they expose persistence rather than use cases.
- A single generic reference importer: rejected because warehouse, UoM, and SKU invariants differ, especially SKU base UoM.
- Calling existing create/update endpoints: rejected because it adds HTTP inside the monolith and cannot provide atomic batch behavior.

## Decision: Imported Identity, Lifecycle, and Field Ownership

**Decision**: Add nullable `Guid ExternalRefKey` and nullable `DateTimeOffset LastImportedAtUtc` to all three aggregates. For folder-bearing sources, `IsFolder=true` is skipped as `SourceFolder` before upsert. A non-folder, non-deleted valid source record creates or updates by external identity and aligns imported fields plus active state; this reactivates a previously source-deactivated linked record. A deletion-marked linked record is deactivated and refreshes `LastImportedAtUtc` without validating or applying source code/name/symbol/base-UoM fields. An unlinked deletion-marked record is skipped and reported as `SourceRecordDeletionMarked`, and no record is physically deleted. Successful unchanged re-imports refresh `LastImportedAtUtc` and count as updated.

**Rationale**: One source is authoritative for fields it imports. Aligning active state makes deletion removal reversible and repeatable. Refreshing the timestamp records successful observation of the source.

**Alternatives considered**:

- Never reactivate automatically: rejected because clearing the 1C deletion mark would not restore source state.
- Treat code as imported identity: rejected by the specification.
- Physically delete deletion-marked records: rejected because references may already participate in WMS history.

## Decision: Filtered Unique External Identity Indexes

**Decision**: Configure one unique filtered SQL Server index on `ExternalRefKey` for each reference table, with filter `[ExternalRefKey] IS NOT NULL` and explicit names in `WmsDatabaseNames`. Keep the existing unique code indexes. Generate one developer-controlled WMS migration and update the model snapshot.

**Rationale**: Nullable identity preserves local records; filtered uniqueness enforces idempotency under races. Existing code uniqueness independently protects business codes.

**Alternatives considered**:

- Application-only uniqueness: rejected because concurrent writes can bypass prechecks.
- Composite `(ExternalSystem, ExternalRefKey)`: rejected because `ExternalSystem` and multiple sources are out of scope.
- Require source identity for all local records: rejected because existing/manual reference data remains valid.

## Decision: One Explicit Transaction Per WMS Batch

**Decision**: A WMS batch handler opens one explicit database transaction, applies all accepted changes, invokes one existing save/domain-event dispatch unit, and commits only after it succeeds. Record validation/conflict outcomes remain in the committed batch result. A persistence or dispatch failure rolls back the whole batch and returns no batch counts. Earlier batches are unaffected.

**Rationale**: This implements the clarified atomic boundary and prevents a post-save event failure from being reported as uncommitted when persistence actually committed.

**Alternatives considered**:

- One transaction for the full 15,000+ import: rejected because it holds locks too long and conflicts with retained progress.
- One transaction per record: rejected because it increases round trips and violates batch atomicity.
- Rely only on implicit `SaveChanges` transaction: rejected because domain-event dispatch occurs after `SaveChanges` in the existing helper.

## Decision: Aggregate Counts Only After Batch Commit

**Decision**: `OneCImportService` aggregates a WMS batch result only after the batch command returns committed success. Pending `SourceFolder` mapping skips from that same source batch are added at the same point. A folder-only source batch completes without WMS mutations. A failed/uncommitted batch, its pending mapping skips, and unread later source records contribute no processed/created/updated/skipped/failed counts. The public response sets `IsComplete=false` and adds one operation error. Returned record errors are capped at 50 after total counts are calculated.

**Rationale**: Public counts remain reconcilable with persisted outcomes and satisfy the clarification decision.

**Alternatives considered**:

- Count attempted rolled-back records as failed: rejected because the summary would not describe persisted state.
- Suppress all partial counts: rejected because successfully committed progress is operationally useful.

## Decision: Process-Local Non-Waiting Same-Type Gate

**Decision**: Register a singleton keyed gate with three fixed `SemaphoreSlim` instances. Import acquisition uses a zero-timeout wait. Failure maps to `409 Conflict` with code `OneCImport.AlreadyInProgress`; successful acquisition is released in `finally`. Connection checks are not gated and different reference types may run concurrently.

**Rationale**: This prevents overlapping same-type writes with minimal MVP complexity and gives the user immediate feedback.

**Alternatives considered**:

- Queue duplicate imports: rejected by clarification and because no job status exists.
- Coalesce callers onto the running request: rejected because request ownership/cancellation becomes ambiguous.
- Distributed lock: deferred; the MVP documents a single-instance `Myrmex.ApiService` deployment requirement.

## Decision: Synchronous API and Complete/Incomplete Response Contract

**Decision**: Expose no-body POST operations at `/api/integrations/1c/connection/test`, `/warehouses/import`, `/uoms/import`, and `/skus/import`. Import requests remain open until completion, cancellation, or timeout. A started import returns `OneCImportResponse` for both complete and incomplete outcomes. Pre-start configuration, actor, and already-running failures use ProblemDetails. Public records live in `Myrmex.Shared.Integrations.OneC`.

**Rationale**: The page needs one stable response model while partial committed state must remain visible. Existing WebApp `ApiResult<T>` and ProblemDetails support can be reused.

**Alternatives considered**:

- Durable background jobs and polling: rejected by scope.
- Persistent import history: rejected by scope.
- Return OData records or WMS entities: rejected by boundary rules.

## Decision: Existing Actor Check, Future Policy Name Only

**Decision**: Use `HttpContext.GetActorId()` at each endpoint and return existing unauthorized ProblemDetails when no authenticated actor exists. Do not add authentication schemes, authorization services, policies, or token forwarding. Document `Wms.Integrations.OneC.Import` as the future policy name.

**Rationale**: The repository has actor-based endpoint precedent but no application authorization baseline. This meets the explicit non-goal while preventing anonymous import calls where an actor is available.

**Alternatives considered**:

- Implement the preferred policy now: rejected because policy infrastructure is outside scope.
- Leave endpoints unrestricted: rejected because the specification requires authorization.

## Decision: Minimal Russian WebApp Page

**Decision**: Add one page and navigation group with literal Russian labels for the four actions, progress, counts, and errors. Use a dedicated `OneCIntegrationApiClient`, disable only the active same-type action while awaiting the synchronous call, and keep the latest result in component state. Do not add localization resources or a persistent UI history model.

**Rationale**: This delivers the approved user flow without broad localization or state infrastructure.

**Alternatives considered**:

- Localize the full application: rejected by scope.
- Reuse WMS catalog pages for import controls: rejected because integration operations and errors need their own operational surface.

## Decision: Risk-Based Automated Coverage

**Decision**: Test WMS invariants/handlers/persistence at their owning layer; exact OData URL and JSON behavior with stub handlers; orchestration, locking, cancellation, and partial aggregation with fakes; new HTTP routes/status/serialization with the existing Minimal API test host; and WebApp route/ProblemDetails mapping with the existing client-test pattern. Use manual UI smoke validation and avoid repeating the full reference matrix at every layer.

**Rationale**: The feature introduces material new identity, transaction, external transport, and public contract risks, while the page itself follows existing simple action-page patterns.

**Alternatives considered**:

- UI component framework adoption: rejected as disproportionate for this feature.
- End-to-end live 1C tests in the regular suite: rejected because they would be environment-dependent; quickstart provides opt-in live validation.
- One test per class/method: rejected by project testing guidance.
