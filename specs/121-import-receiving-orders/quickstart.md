# Manual Verification: Receiving-Order Import

Developer-performed verification only. Do not create or run automated tests.

## Preparation

1. Configure the existing 1C connection and the receiving-order entity-set setting using
   secure local configuration.
2. Confirm imported, active Warehouse and SKU references exist for the selected source
   documents. Confirm each SKU's base UoM is active.
3. As a WmsOperator, open each applicable Warehouse's existing edit dialog, select an
   active/selectable Receiving storage location from its warehouse-scoped lookup, and
   save it as the default receiving location. Confirm the saved selection is shown when
   the Warehouse is reopened.
4. Select a source period containing known eligible documents from
   `Document_ПриходныйОрдерНаТовары`.

## Acceptance scenarios

1. Start an import for a valid period and confirm the result reports every processed
   document with Created, Updated, Skipped, or Failed.
2. Confirm an eligible document with available dependencies creates one local Draft
   ReceivingOrder with its Warehouse, configured receiving location, document number, and
   planned quantities.
3. Change a mapped source header value (number or Warehouse) or a `Товары.Количество`
   value, repeat the import, and confirm the same local Draft order is reconciled without
   duplicate SKU lines. Confirm its number, Warehouse, derived receiving location, SKU
   set, and planned quantities now match the source; Draft status and received quantities
   remain unchanged.
4. Repeat the unchanged import and confirm no duplicate local order or plan lines appear;
   confirm the document is reported as Skipped.
5. Remove or invalidate one dependency or default receiving-location configuration and
   confirm that document fails with its identity and immediate reason, while another valid
   document in the same period still produces an observable outcome.
6. Move a previously imported order out of Draft through the normal warehouse workflow,
   then re-import its source document. Confirm the existing order is not changed and is
   reported as Skipped.
7. Import a later period that does not return an earlier document. Confirm the existing
   local receiving order remains present and unchanged.
8. Select a location from another Warehouse, a non-selectable location, or a location not
   of Receiving type through an API/manual developer check. Confirm the Warehouse setting
   is rejected; then clear the default and confirm an otherwise valid document fails with
   `ReceivingLocationNotConfigured` without creating an order.

## Developer-controlled handoff

After implementation, the developer should build the affected projects, generate/review
and apply the required EF Core migration, then perform the scenarios above. Builds,
migration generation/application, commits, and pull-request actions are not agent-run
steps.
