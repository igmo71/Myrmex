# Feature Specification: Import External Receiving Orders

**Feature Branch**: Developer-selected branch

**Created**: 2026-07-24

**Status**: Draft

**Input**: User description: "issues\121 Import external receiving orders manually as local Draft plans.md"

## User Scenarios & Verification *(mandatory)*

### User Story 1 - Import Receiving Plans for a Period (Priority: P1)

An authorized warehouse user selects a valid period and starts a manual import. The user
receives a per-document result showing whether each suitable external receiving document
became a new local Draft receiving plan, updated an existing Draft plan, was skipped, or
failed.

**Why this priority**: This gives operators a controlled way to turn approved external
planning information into local receiving work without recording physical receipt.

**Independent Verification**: With suitable external documents and all referenced local
data available, a developer can start an import for a period and observe local Draft
receiving plans plus a result for every processed document.

**Acceptance Scenarios**:

1. **Given** an authorized user and a valid import period, **When** the user starts the
   import, **Then** the system requests suitable external receiving documents for that
   period and presents their individual outcomes.
2. **Given** a suitable external document with every required local reference available
   and no matching local receiving order, **When** it is imported, **Then** a new local
   receiving order is created in Draft with the imported plan header and lines.
3. **Given** an external document cannot be used because a required reference is absent
   or invalid, **When** it is processed, **Then** no incomplete receiving order is
   created and the result identifies the document and immediate reason.

---

### User Story 2 - Refresh an Imported Draft Plan (Priority: P2)

An authorized warehouse user can re-import a period to bring an already imported external
document's matching local Draft receiving plan into line with its current external header
and plan lines.

**Why this priority**: Plans can change before warehouse work begins, so operators need
a safe, repeatable way to refresh Draft plans without creating duplicates.

**Independent Verification**: After importing a document, a developer can change its
available external planning data, repeat the import, and observe the same local Draft
receiving order reconciled to that data rather than a second order being created.

**Acceptance Scenarios**:

1. **Given** an external document matches a local receiving order that remains Draft,
   **When** the external header or plan lines have changed and the user re-imports the
   period, **Then** the existing Draft order reflects the current imported information.
2. **Given** unchanged external documents for a period, **When** the user repeats the
   import, **Then** no duplicate local receiving orders or duplicate plan lines are
   created and each outcome explains that no change was needed.
3. **Given** an external document matches a local receiving order that is no longer
   Draft, **When** the user imports the period, **Then** the existing order is not
   modified and the result reports it as skipped with the reason.

---

### User Story 3 - Continue After Individual Document Problems (Priority: P3)

An authorized warehouse user can understand partial import results when one or more
external documents cannot be processed, while other eligible documents in the same run
remain visible and are processed independently.

**Why this priority**: A single data problem must not hide usable plans or leave the
operator unable to identify what needs correction.

**Independent Verification**: With one processable external document and one document
with an unresolved dependency in the selected period, a developer can observe the
successful document's outcome and a separate failure result for the other document.

**Acceptance Scenarios**:

1. **Given** a selected period contains both processable and unprocessable external
   documents, **When** the user starts the import, **Then** each document has a separate
   outcome and the processable document is not concealed by the other's failure.
2. **Given** a document is skipped or fails, **When** the result is shown, **Then** it
   identifies the affected external document and states the immediate reason.

### Edge Cases

- A user supplies an invalid or incomplete period: the import is not started and the
  user is told to provide a valid period.
- The external source returns no suitable documents: the result clearly states that no
  documents were available, without changing local receiving orders.
- A required warehouse, item, unit of measure, or other referenced record cannot be
  resolved locally: that document fails without creating or altering an invalid order.
- A matching local order has progressed beyond Draft: it is left unchanged and reported
  as skipped.
- A later import does not return a document that was previously imported: the existing
  local receiving order is neither deleted nor deactivated by this feature.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow an authorized user to open the receiving-order
  import function, enter a valid period, and manually start an import.
- **FR-002**: For the selected period, the system MUST obtain suitable receiving-order
  documents from the external source.
- **FR-003**: The system MUST assign each processed external document a durable external
  identity and use that identity to locate its corresponding local receiving order.
- **FR-004**: When no matching local order exists and all required dependencies resolve,
  the system MUST create one local receiving order in Draft with the imported header and
  plan lines.
- **FR-005**: When a matching local order exists and remains Draft, the system MUST
  reconcile its imported header and plan lines to the current external document using
  the receiving order's supported Draft editing rules.
- **FR-006**: The system MUST resolve the warehouse, items, units of measure, and other
  required references from existing local imported reference data.
- **FR-007**: If a required dependency cannot be resolved, the system MUST not create or
  leave an incomplete or invalid receiving order for that document.
- **FR-008**: Repeating an import for unchanged external data MUST not create duplicate
  local receiving orders or duplicate plan lines.
- **FR-009**: A matching local receiving order that is not Draft MUST not be modified by
  this feature and MUST be reported as skipped.
- **FR-010**: The import result MUST record a Created, Updated, Skipped, or Failed
  outcome for every processed external document, with the affected external document and
  immediate reason for skipped and failed outcomes.
- **FR-011**: A failed document MUST not conceal the independently determined outcomes
  of other documents in the same import.
- **FR-012**: The selected period MUST limit which external documents are requested; a
  document absent from a later result MUST NOT cause its existing local order to be
  deleted or deactivated.

### Domain Rules & State Transitions *(include when state changes)*

- **DR-001**: Imported receiving documents are local receiving plans and do not confirm
  that inventory was physically received.
- **DR-002**: A newly imported local receiving order starts in Draft.
- **DR-003**: Only a Draft receiving order may be reconciled by this feature; orders in
  progress or completed remain unchanged.
- **DR-004**: Header and line reconciliation must preserve the existing receiving
  aggregate's invariants for Draft orders.
- **DR-005**: External matching is based on durable external identity, not a document
  number, display name, or the selected period alone.

### Quality Attributes *(include only supplied or accepted requirements)*

- **QA-001 Security**: Only authorized users may initiate an import or view its
  document-level results.
- **QA-002 Reliability**: Each document's outcome is isolated so that a dependency or
  data failure for one document does not hide the outcome of other documents.
- **QA-003 Observability**: The user-facing result must provide enough document identity
  and reason detail for an operator to diagnose skipped and failed records.

### Key Entities *(include if feature involves data)*

- **External Receiving Document**: A suitable external planning document selected by a
  period, identified by a durable external identity, with a header and plan lines.
- **Receiving Order**: The local receiving plan matched to an external document; it is
  created or reconciled only while in Draft.
- **Receiving Plan Line**: A planned item quantity within a receiving order, reconciled
  as part of the Draft order's plan.
- **Imported Reference**: An existing local representation of a required warehouse,
  item, unit of measure, or other dependency, identified by its external identity.
- **Import Result**: The per-document outcome of a manual import, including status,
  affected external document, and reason where processing did not create or update it.

## Verification Outcomes *(mandatory)*

- **VO-001**: An authorized user can select a valid period, start a manual import, and
  see an outcome for each processed external receiving document.
- **VO-002**: A suitable document with resolvable references and no local match results
  in one new Draft receiving order containing its imported plan.
- **VO-003**: Re-importing an existing document while its local order is Draft updates
  that same order to reflect current external planning information.
- **VO-004**: Repeating the import with unchanged external data leaves one matching
  local order and no duplicate plan lines.
- **VO-005**: A document with unresolved required references creates no invalid local
  order, while results for other documents in the same import remain observable.
- **VO-006**: A matching order that has left Draft is unchanged and its result identifies
  it as skipped with the reason.

## Assumptions

- The local receiving behavior that supports Draft creation and reconciliation is
  available before this feature is implemented.
- Required local warehouse, item, unit-of-measure, and related reference data has
  already been imported and retains the external identities needed for resolution.
- The exact external document type, eligibility rule, date field, and line mapping will
  be confirmed during implementation research without widening this feature's scope.
- Existing manual reference-import interaction and result-reporting conventions are the
  primary user-experience reference.
- This feature excludes automated or scheduled synchronization, background processing,
  generalized synchronization infrastructure, post-Draft conflict handling, locks,
  distributed transactions, receipt acknowledgements, and saga behavior.
- Any persistence impact is assessed during planning; database migration creation and
  application remain developer-operated work.
