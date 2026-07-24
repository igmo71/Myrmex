# Import external receiving orders manually as local Draft plans

Parent issue: #105

## Context

Myrmex already supports locally created receiving orders with the lifecycle:

```text
Draft → InProgress → Completed
```

The system also contains established patterns for manually synchronizing reference data from 1C, including user-triggered synchronization, external data retrieval, dependency resolution, local persistence, and operator-facing execution results.

This feature introduces the first intentionally limited receiving-order import scenario.

It must be implemented independently from the earlier research branch:

```text
105-1-synchronize-receiving-orders-from-external-systems
```

That branch may be used as research material only. Its broader synchronization mechanisms must not be transferred automatically into this feature.

The implementation must start from the current branch and reuse existing Myrmex architecture and manual reference-synchronization patterns wherever applicable.

## User outcome

An authorized user can select a period in the WebApp and manually import suitable receiving orders from 1C.

For each external document, Myrmex:

1. reads and transforms the external receiving-order data;
2. resolves the required existing Warehouse, SKU, UoM, and other dependencies;
3. creates a new local `Draft ReceivingOrder` when the document has not been imported before;
4. updates the corresponding local `Draft ReceivingOrder` when the external document has already been imported;
5. reports a clear import result to the user.

Expected flow:

```text
Select period in UI
→ read receiving orders from 1C
→ transform external data
→ resolve existing dependencies
→ create or update Draft ReceivingOrder
→ show import result
```

## Functional scope

The feature includes:

* manual import initiated from the WebApp;
* selection of the import period;
* retrieval of suitable receiving-order documents from 1C;
* transformation of an external document into a local receiving plan;
* stable matching between an external document and a local `ReceivingOrder`;
* creation of a new local `ReceivingOrder` in `Draft` status;
* update of an existing matching local `ReceivingOrder` while it remains in `Draft`;
* reconciliation of the Draft header and lines using the current Receiving aggregate rules;
* resolution of required dependencies through existing imported reference data;
* idempotent repeated execution for the same period and unchanged external data;
* an operator-facing result that distinguishes created, updated, skipped, and failed documents;
* reuse of existing Myrmex application, integration, persistence, endpoint, and UI conventions.

## Explicit exclusions

This feature does not include:

* external notifications;
* `SynchronizationRequest` processing;
* background workers;
* scheduled or automatic synchronization;
* automatic reaction to external changes;
* synchronization behavior after a local receiving order leaves `Draft`;
* post-Start conflict handling;
* blocking or changing `Complete`;
* saga orchestration;
* technical receipts or acknowledgement protocols;
* `sp_getapplock`;
* distributed locks;
* distributed transactions;
* a generalized synchronization framework;
* automatic tests or test infrastructure;
* EF Core migration creation;
* database update execution;
* build execution;
* commit creation;
* pull request creation or publication.

Later synchronization stages must be implemented as separate child issues under #105.

## Acceptance criteria

1. An authorized user can open the receiving-order import UI and specify a valid period.

2. Starting the import requests suitable receiving-order documents from 1C for the selected period through the existing 1C integration transport.

3. Each imported document is mapped to a stable external identity that can be used to find the corresponding local `ReceivingOrder`.

4. When no matching local order exists and all required dependencies can be resolved, Myrmex creates a new `ReceivingOrder` in `Draft` status with the imported header and plan lines.

5. When a matching local order exists and remains in `Draft`, Myrmex updates its imported header and plan to reflect the current external document.

6. Updating an existing Draft uses the current Receiving aggregate invariants and does not bypass its supported Draft-editing behavior.

7. Repeating the import with the same external data does not create duplicate local receiving orders or duplicate plan lines.

8. Warehouse, SKU, UoM, and other required references are resolved from existing local imported data. This feature does not introduce a new generalized dependency-synchronization mechanism.

9. Failure to resolve a required dependency does not create an incomplete or invalid receiving order.

10. A failure for one external document does not conceal the outcome of the other processed documents.

11. The execution result clearly reports at least:

```text
Created
Updated
Skipped
Failed
```

12. Failed or skipped results contain enough information for the operator to identify the affected external document and understand the immediate reason.

13. A matching local order that is no longer in `Draft` is not modified by this feature.

14. No post-Draft synchronization, conflict-resolution, locking, receipt, saga, or background-processing behavior is introduced.

15. The implementation follows the current repository constitution and reuses existing Myrmex manual synchronization and UI patterns before introducing feature-specific alternatives.

16. Manual verification steps are documented without creating or running automated tests.

## Dependencies and assumptions

* The local Receiving implementation from #116 is available on `master`.
* `ReceivingOrder` supports creation and reconciliation while in `Draft`.
* Existing local Warehouse, SKU, UoM, and other required reference records have already been synchronized.
* Existing reference entities provide the external identity required to resolve 1C references.
* The current 1C OData transport can be extended or reused to read receiving-order documents and their required details.
* The exact 1C document type, selection predicate, date field, and line mapping must be confirmed during repository and transport research.
* The existing manual reference-synchronization implementation is the primary design reference for:

  * WebApp interaction;
  * application request handling;
  * 1C transport usage;
  * dependency resolution;
  * result presentation;
  * retry and error presentation where applicable.
* External document matching must use durable external identity rather than document number, display name, or period alone.
* The selected period defines which external documents are requested; it does not imply deletion or deactivation of local orders that are absent from a later import result.
* Imported documents are treated as plans for local receiving operations, not as confirmation that inventory was physically received.
* No database schema change should be assumed until repository research confirms that the current external-identity model is insufficient.
* Build, tests, migrations, database updates, commits, and pull requests remain developer-operated actions.
