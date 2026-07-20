# Refactor 1C reference integration into explicit vertical slices

## Context

Issues #104 and #109 established the external-integration foundation and reference-data synchronization behavior for Myrmex.

The project now supports:

* machine-authenticated change notifications;
* durable synchronization requests stored in SQL;
* idempotent notification intake;
* processing, retry, deferred handling, and abandoned-processing recovery;
* manual full import of Warehouse, Unit of Measure, and Stock Keeping Unit;
* reactive synchronization of those reference types;
* internal synchronize-one operations;
* source-version-aware `Applied` and `Unchanged` behavior;
* bounded Stock Keeping Unit to Unit of Measure repair;
* per-reference-type in-process coordination;
* source-owned field protection for linked WMS references.

The functional behavior is complete, but the 1C reference integration is currently organized primarily around technical concerns:

```text
OneC/
├── Endpoints/
├── Imports/
├── References/
├── Transport/
└── Synchronization/
```

Reference-specific workflows are coordinated through shared services, reference-type switches, generic helper methods, and delegated operations.

Each individual abstraction is understandable, but the complete execution path for one reference type is distributed across several folders and services.

For example, understanding Warehouse synchronization may require tracing:

```text
SynchronizationRequest
→ handler resolution
→ reference synchronization handler
→ shared handler mapping
→ common reference synchronization service
→ reference-type selection
→ generic gate helper
→ source-read delegate
→ mapping delegate
→ command-dispatch delegate
→ result classification
→ durable handler result
```

This makes the implementation harder to read, review, debug, and safely extend.

The purpose of this issue is to simplify the existing implementation before additional integration complexity is introduced.

## Problem

The current 1C reference integration does not sufficiently follow the explicit vertical-slice organization used elsewhere in Myrmex.

Business-specific knowledge is distributed across common technical services:

* `OneCImportService` coordinates all three reference imports;
* `OneCReferenceSynchronizationService` coordinates all three synchronize-one workflows;
* shared orchestration selects reference behavior through type switches;
* generic helper methods receive delegates for source reads, mapping, dispatching, and result classification;
* Stock Keeping Unit dependency repair is embedded in the shared reference synchronization service;
* understanding one reference flow requires navigating through several common technical layers.

As a result:

* the complete flow for one reference cannot be understood from one cohesive slice;
* changes to one reference may require modifying orchestration shared by unrelated reference types;
* generic helpers hide execution order and ownership;
* debugging requires following several levels of result conversion;
* technical reuse is prioritized over local readability;
* the current implementation risks becoming a template for further integration complexity.

The issue is not that the existing behavior is incorrect. The problem is that its structure makes the behavior unnecessarily difficult for a developer to follow.

## Goal

Refactor the 1C reference integration into explicit vertical slices for:

* Warehouse;
* Unit of Measure;
* Stock Keeping Unit.

Each reference slice must own its integration-specific workflows and allow a developer to understand the full path for that reference without tracing through a common all-reference service, a central type switch, and a chain of delegated operations.

The intended conceptual structure is:

```text
Myrmex.Integrations/OneC/
├── Common/
│   └── genuinely shared integration infrastructure
│
├── Warehouses/
│   ├── manual full import
│   ├── reactive synchronization
│   ├── internal synchronize-one
│   ├── source transport and mapping
│   └── focused tests
│
├── UnitsOfMeasure/
│   ├── manual full import
│   ├── reactive synchronization
│   ├── internal synchronize-one
│   ├── source transport and mapping
│   └── focused tests
│
└── StockKeepingUnits/
    ├── manual full import
    ├── reactive synchronization
    ├── internal synchronize-one
    ├── bounded base-UoM repair
    ├── source transport and mapping
    └── focused tests
```

This structure is illustrative.

Exact folder names, class names, interface names, and file boundaries must follow current repository conventions and be resolved during specification and planning.

## Primary Outcome

The primary outcome is not fewer classes or fewer lines of code.

The primary outcome is that the ownership and execution flow for each reference type become explicit, local, and easy to understand.

A developer should be able to explain the import or synchronization path for one reference type by inspecting that reference’s integration slice and the corresponding existing WMS import command.

The developer should not need to mentally execute a generic synchronization framework.

## Architectural Direction

### Preserve the durable synchronization foundation

Feature #104 remains the common infrastructure owner for:

* machine notification intake;
* durable synchronization-request persistence;
* duplicate resolution;
* polling and wake-up;
* handler resolution;
* request processing;
* retry scheduling;
* deferred handling;
* abandoned-processing recovery;
* durable lifecycle transitions.

This issue must not create:

* a reference-specific queue;
* a second worker;
* a second processor;
* a reference-specific retry policy;
* a new synchronization-request lifecycle;
* new durable synchronization statuses.

The existing `ISynchronizationHandler` boundary remains the connection between durable infrastructure and a concrete integration slice.

### Keep existing WMS import handlers as the application boundary

The existing WMS commands remain the single implementation of reference application rules:

* `ImportWarehouses`;
* `ImportUnitsOfMeasure`;
* `ImportStockKeepingUnits`.

Manual, reactive, and on-demand integration flows must continue to dispatch these existing commands.

The refactoring must not duplicate:

* external-identity lookup;
* create and update behavior;
* source-version comparison;
* same-version no-op behavior;
* deactivation and reactivation;
* validation;
* code-conflict handling;
* SKU base-UoM validation;
* transaction and savepoint behavior;
* persistence;
* domain-event dispatch.

The target dependency remains:

```text
1C integration slice
→ read and map source data
→ dispatch existing WMS import command
→ WMS application and domain behavior
```

The 1C integration layer remains an adapter and orchestrator. It must not become a second implementation of WMS business rules.

### Replace common reference orchestration with explicit slices

The current shared reference orchestration should be decomposed into explicit workflows such as:

```text
SynchronizeWarehouse
SynchronizeUnitOfMeasure
SynchronizeStockKeepingUnit
```

Each synchronize-one workflow should explicitly show:

```text
acquire the matching reference lease
→ read the current source object
→ handle absent, folder, or source failure
→ map the source object
→ dispatch the existing WMS import command
→ classify the command result
→ return the internal synchronization outcome
```

A developer reading the Warehouse synchronization slice should not need to inspect a generic reference-type switch to discover:

* which OData operation is used;
* which source fields are mapped;
* which WMS command is dispatched;
* which folder rules apply;
* how the application result is interpreted.

### Remove delegate-based business orchestration

Generic helpers that own the complete reference workflow through injected delegates should be removed or substantially reduced.

The following style should not remain the primary representation of a reference workflow:

```text
RunAsync(
    referenceType,
    readDelegate,
    mappingDelegate,
    dispatchDelegate,
    classificationDelegate)
```

Small technical helpers remain acceptable when they do not hide the business sequence.

Examples of acceptable shared helpers include:

* acquiring and releasing a lease;
* executing an authenticated OData request;
* decoding and validating `DataVersion`;
* constructing uniform diagnostics;
* mapping a common internal synchronization outcome to a durable handler result.

A helper must not require a developer to reconstruct the actual Warehouse, UoM, or SKU workflow from a collection of passed callbacks.

### Split manual import orchestration by reference type

The current common `OneCImportService` must be reviewed and decomposed into explicit reference import operations.

Each reference-specific manual import flow should own:

* its source-loading strategy;
* its source-to-command mapping;
* its lease scope;
* dispatch of its WMS import command;
* aggregation of its operator-facing result;
* its paging and batching behavior where applicable;
* its cancellation and partial-result behavior.

Warehouse and Unit of Measure full-collection imports may share small technical code where the shared code clearly improves readability.

Stock Keeping Unit import must remain explicit because it owns materially different:

* paging;
* batching;
* committed partial results;
* dependency behavior;
* error aggregation.

Existing API endpoints may depend on explicit reference-specific import operations rather than one service containing all reference types.

### Keep Stock Keeping Unit repair inside the SKU slice

The bounded SKU-to-UoM repair belongs to Stock Keeping Unit synchronization.

The dependency should be visible as:

```text
Stock Keeping Unit synchronization
→ explicit Unit of Measure synchronize-one capability
→ retry the same SKU application at most once
```

The SKU slice may depend on an explicit internal Unit of Measure synchronization contract.

The repair must not be represented as recursive invocation of a generic reference synchronization service.

Existing limits remain unchanged:

* synchronize at most one Unit of Measure;
* apply the SKU at most twice in total;
* perform no recursive dependency resolution;
* build no dependency graph;
* add no generalized repair engine.

### Keep only genuinely shared infrastructure in common locations

Common folders and services should contain mechanisms that are independent of a specific reference type.

Appropriate shared concerns include:

* 1C connection and authentication configuration;
* generic OData HTTP execution;
* common source error taxonomy;
* notification intake infrastructure;
* durable synchronization processing infrastructure;
* import gate primitives;
* common result or diagnostic primitives where they materially improve readability;
* endpoint-group infrastructure.

Code that knows about any of the following belongs to the corresponding reference slice:

* Warehouse fields or folders;
* Unit of Measure symbol;
* Stock Keeping Unit base UoM;
* a specific 1C entity set;
* a specific WMS import command;
* a reference-specific source mapping;
* a reference-specific controlled skip;
* SKU dependency repair.

### Prefer explicit local code over hidden reuse

Controlled duplication is acceptable.

Three short and explicit workflows are preferable to one highly generic workflow when the generic workflow:

* hides processing order;
* requires several delegates;
* relies on reference-type switches;
* spreads one business flow across several technical services;
* makes debugging and review harder.

The refactoring should optimize for:

* comprehension;
* local reasoning;
* change locality;
* visible dependency direction;
* straightforward debugging.

Minimizing line count is not a goal.

## Required Vertical Slices

### Warehouse

The Warehouse integration slice must make the following paths explicit.

Manual full import:

```text
read Warehouse collection
→ filter and map source records
→ dispatch ImportWarehouses.Command
→ aggregate operator result
```

Reactive and on-demand synchronize-one:

```text
acquire Warehouse lease
→ read current Warehouse by external key
→ handle folder, not-found, and source failure
→ map one import item
→ dispatch ImportWarehouses.Command
→ map application result
```

### Unit of Measure

The Unit of Measure integration slice must make the following paths explicit.

Manual full import:

```text
read Unit of Measure collection
→ map source records
→ dispatch ImportUnitsOfMeasure.Command
→ aggregate operator result
```

Reactive and on-demand synchronize-one:

```text
acquire Unit of Measure lease
→ read current Unit of Measure by external key
→ handle not-found and source failure
→ map one import item
→ dispatch ImportUnitsOfMeasure.Command
→ map application result
```

Unit of Measure must not gain folder semantics.

### Stock Keeping Unit

The Stock Keeping Unit integration slice must make the following paths explicit.

Manual full import:

```text
acquire SKU lease for the entire operation
→ read all configured pages
→ process configured batches
→ dispatch ImportStockKeepingUnits.Command
→ preserve committed partial results
→ aggregate operator result
```

Reactive and on-demand synchronize-one:

```text
acquire SKU lease
→ read current SKU by external key
→ handle folder, not-found, and source failure
→ dispatch ImportStockKeepingUnits.Command
→ optionally synchronize one required Unit of Measure
→ retry the same SKU command once
→ map the final result
```

## Behavioral Compatibility

This is a behavior-preserving refactoring.

### Manual imports

Preserve:

* existing HTTP routes;
* existing `WmsOperator` authorization;
* existing request and response contracts;
* `Processed`, `Created`, `Updated`, `Unchanged`, `Skipped`, and `Failed` accounting;
* structured record and operation errors;
* returned-error limits;
* Warehouse and UoM full-collection behavior;
* SKU paging and batching;
* partial committed SKU results;
* transaction and savepoint behavior;
* caller-facing cancellation result;
* fail-fast busy behavior;
* current WebApp workflow;
* current localization.

### Reactive synchronization

Preserve:

* existing notification routes;
* machine authentication;
* durable insert or duplicate resolution before `202 Accepted`;
* stable entity-type values;
* notified `DataVersion` idempotency;
* current-object source reads;
* the current source object as the source of truth;
* existing durable request statuses;
* retry and permanent-failure classification;
* shutdown cancellation;
* abandoned-processing recovery;
* structured diagnostics.

### Internal synchronize-one

Preserve the existing internal outcomes:

* `Applied`;
* `Unchanged`;
* `ControlledSkip`;
* `NotFound`;
* `Busy`;
* `TransientFailure`;
* `PermanentFailure`.

Preserve:

* caller cancellation propagation;
* same-type coordination;
* no public synchronize-one endpoint;
* no WebApp synchronize-one action.

### Domain and persistence

Preserve:

* `ExternalImportState`;
* external identity semantics;
* `DataVersion` semantics;
* same-version no-op behavior;
* source lifecycle handling;
* source-owned field protection;
* existing domain events;
* existing database schema;
* existing EF mappings;
* existing migration and model-snapshot behavior.

No domain or persistence redesign is expected.

## Observability

Reference-specific logging and diagnostics should originate from the corresponding reference slice.

For a reactive request, diagnostics must continue to make it possible to associate:

* synchronization request identity;
* notified source version;
* reference type;
* external identity;
* current-source outcome;
* failure reason;
* retry suitability.

Moving or decomposing code must not reduce existing observability.

## Testing

This is a behavior-preserving refactoring.

Automated test changes must be kept to the minimum necessary to protect against regressions introduced by moving and decomposing the existing code.

The implementation should:

* preserve and reuse existing #104 and #109 tests wherever possible;
* relocate or rename tests only when required by the new source structure;
* update existing tests when constructor, dependency, namespace, or operation boundaries change;
* add a new test only when the refactoring creates a genuinely new boundary or when existing coverage cannot prove preserved behavior;
* prefer one representative or parameterized test over equivalent tests for Warehouse, Unit of Measure, and Stock Keeping Unit;
* avoid reproducing complete manual, reactive, synchronize-one, gate, retry, cancellation, or outcome matrices for every reference type;
* avoid adding tests merely because production classes were split or moved;
* not recreate Feature #104 durable intake, processor, retry, polling, recovery, or lifecycle coverage.

Existing coverage must continue to protect:

* source reads and source mapping;
* dispatch of existing WMS import commands;
* per-reference-type lease scope;
* synchronization outcome mapping;
* SKU-to-UoM repair limits;
* manual import result accounting;
* cancellation and error behavior.

The target is the smallest test adjustment that gives confidence that observable behavior has not changed.

Increasing the number of tests is not a goal of this issue.

## Branch and Specification Identity

This issue must be specified and implemented on the Git branch that is already checked out when the work begins.

The agent must:

* use the current Git branch;
* not create a new branch;
* not switch to another branch;
* not rename the current branch;
* not invoke any Spec Kit behavior that creates or checks out a feature branch.

The GitHub Issue number is **111** and must remain the authoritative feature number throughout the specification workflow.

All feature documentation must be created under a directory beginning with the current Issue number:

```text
specs/111-<feature-slug>/
```

The expected directory is:

```text
specs/111-onec-reference-vertical-slices/
```

A minor slug adjustment is acceptable only when required by existing repository naming conventions.

The `111-` prefix must not change.

The agent must not:

* calculate the next available feature number;
* infer a number from existing directories under `specs/`;
* use the next sequential number;
* replace `111` with a number proposed by Spec Kit or another tool;
* create a second specification directory for the same Issue.

Before creating specification artifacts, the agent must verify the current branch and explicitly use Issue **#111** as the feature identity.

## Acceptance Criteria

The issue is complete when all of the following are true.

1. Warehouse, Unit of Measure, and Stock Keeping Unit each have an explicit 1C integration vertical slice.

2. The complete manual import flow for a reference can be understood by navigating within that reference slice and its existing WMS import command.

3. The complete reactive and on-demand synchronization flow for a reference can be understood by navigating within that reference slice and Feature #104’s common handler boundary.

4. No common service selects Warehouse, Unit of Measure, or Stock Keeping Unit synchronization behavior through a central reference-type switch.

5. The main reference synchronization workflow is not expressed through a generic helper receiving source-read, mapping, dispatch, and classification delegates.

6. Stock Keeping Unit to Unit of Measure repair is visibly owned by the SKU slice and calls an explicit UoM synchronize-one capability.

7. Shared code contains only infrastructure or genuinely uniform primitives and does not own reference-specific business flow.

8. Manual, reactive, and on-demand flows continue to use the existing WMS `Import*.Command` handlers as the common application boundary.

9. Existing public routes, authorization policies, response contracts, durable lifecycle, domain behavior, persistence schema, UI workflow, logging, and error semantics remain unchanged.

10. Existing tests are preserved or minimally adjusted for the new boundaries. New tests are added only for material regression risks not covered by the existing #104 and #109 suites.

11. No duplicate per-reference-type or synchronization-foundation test matrices are introduced.

12. Obsolete common orchestration and delegate-based paths are removed rather than retained behind compatibility wrappers.

13. A developer can explain the path for one reference type without tracing through an all-reference orchestration service, a type switch, and a chain of delegated callbacks.

14. Specification artifacts use Issue number **111** and are located under `specs/111-<feature-slug>/`.

15. No new branch is created or checked out as part of specification or implementation work.

## Out of Scope

This issue does not implement or design:

* Receiving business processes;
* Shipping business processes;
* Receiving or Shipping document import;
* Receiving or Shipping document synchronization;
* document snapshots;
* document conflict resolution;
* demand-versus-execution models;
* outbound status updates;
* new reference types;
* multiple external providers;
* generalized external-link models;
* metadata-driven OData mapping;
* generic synchronization engines;
* recursive dependency resolution;
* distributed or cross-process coordination;
* new durable synchronization statuses;
* new operator synchronization UI;
* public synchronize-one endpoints;
* database redesign;
* unrelated WMS refactoring.

The organization established by this issue may later inform other integration slices, but no future document integration structure is to be designed in advance.

## Implementation Constraints

* Follow the existing modular-monolith and vertical-slice conventions.
* Preserve dependency direction from integration adapters to WMS application use cases.
* Keep source-specific transport concepts outside WMS domain and application contracts.
* Do not duplicate existing WMS create, update, deactivate, or reactivate logic.
* Do not replace the current generic orchestration with another generalized abstraction.
* Do not introduce infrastructure solely for hypothetical future functionality.
* Prefer deletion of obsolete orchestration code over compatibility wrappers that preserve two parallel paths.
* Keep shared code small and limited to genuinely uniform mechanisms.
* Prefer explicit local orchestration over hidden generic reuse.
* Keep test additions to the minimum required for behavior-preserving refactoring.
* Moving or splitting a class does not by itself justify a new test suite.
* Work only on the currently checked-out branch.
* Do not create, switch, or rename Git branches.
* Create specification artifacts only under `specs/111-<feature-slug>/`.
* Do not allocate or infer another feature number.
* Do not automatically run build, tests, migration generation, database update, AppHost, Docker, application startup, or other environment-changing commands.
* Command execution remains developer-controlled and requires an explicit request.

## Proposed Work Process

1. Inspect the current #104 and #109 implementation and map the exact call chains for:

   * Warehouse manual import;
   * Warehouse synchronize-one;
   * Unit of Measure manual import;
   * Unit of Measure synchronize-one;
   * Stock Keeping Unit manual import;
   * Stock Keeping Unit synchronize-one;
   * SKU-to-UoM repair.

2. Identify:

   * genuinely shared infrastructure;
   * reference-specific orchestration currently located in shared services;
   * delegate-based helpers;
   * central reference-type switches;
   * current DI registrations;
   * current endpoints;
   * current tests and contracts affected by relocation.

3. Produce the feature specification under:

```text
specs/111-onec-reference-vertical-slices/
```

4. Do not create or switch branches during the specification workflow.

5. During planning, define:

   * concrete vertical-slice folders;
   * explicit per-reference operation boundaries;
   * retained common primitives;
   * migration sequence from the existing shared services;
   * DI and endpoint adjustments;
   * minimum required test adjustments;
   * obsolete code to remove.

6. Implement incrementally:

   * shared prerequisites only where necessary;
   * Warehouse slice;
   * Unit of Measure slice;
   * Stock Keeping Unit slice;
   * bounded SKU repair;
   * endpoint and DI alignment;
   * removal of obsolete common orchestration;
   * minimal test relocation or adjustment.

7. Preserve runtime behavior throughout the refactoring.

8. Leave all command-based validation to the developer.

## Stakeholder Outcome

After this issue, Myrmex must retain all reference integration capabilities introduced by #104 and #109 while presenting them through explicit and readable integration vertical slices.

The ownership and execution flow for Warehouse, Unit of Measure, and Stock Keeping Unit must be obvious and local.

The resulting implementation should be easier to understand, review, debug, and maintain without introducing a new abstraction framework or anticipating the design of future warehouse processes.
