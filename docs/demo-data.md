# WMS Demo Data Operations

Myrmex exposes two API-only administrative actions for creating and resetting a bounded WMS demonstration dataset. They are unavailable by default and are never registered in the `Production` environment.

## Enable locally

Configure these values outside source control:

```text
Myrmex__Wms__DemoData__Enabled=true
Myrmex__Wms__DemoData__AllowClear=true
Myrmex__Wms__DemoData__ClearConfirmation=<local-secret>
```

`Enabled` registers both routes in a non-production host. `AllowClear` separately permits destructive clearing. `ClearConfirmation` must be non-empty and must exactly match the clear request body. Both actions require the existing authenticated actor claim.

## Operations

Seed with `POST /api/admin/demo-data/seed` and no body. The operation creates or reuses four units of measure, ten Russian fastener SKUs, one warehouse, seven zones, fifteen locations, opening inventory and ledger history, four transfer scenarios, and two inventory counts. Repeated calls reconcile stable identities and do not repeat inventory effects.

Clear with `POST /api/admin/demo-data/clear` and JSON:

```json
{ "confirmation": "<local-secret>" }
```

Clear removes every mutable WMS application record, including user-created records. It preserves system storage-location types/statuses, the database schema, and migration history. To reset the demonstration, clear and then seed again.

Successful responses contain `operation`, timestamps, and per-area `created`, `reused`, `skipped`, and `deleted` counts. A second seed normally reports zero created records and non-zero reused records. Clear normally reports only deleted counts.

## Safety and troubleshooting

- A disabled feature or Production host returns normal 404 responses because routes are absent.
- Missing actor identity returns 401.
- Disabled clear or an incorrect confirmation returns 403.
- Missing confirmation or invalid confirmation configuration returns 400.
- An overlapping seed/clear request returns 409 with `DemoData.OperationInProgress`.
- An incompatible stable demo identity returns 409 with `DemoData.IdentityConflict`; no seed changes commit.
- An unavailable or migration-stale database returns a safe failure without attempting schema changes.

Each operation uses one database transaction. Failures and cancellation roll back and clear EF tracking. Logs include operation, actor, environment, outcome, duration, category, and summary counts; confirmation values are never logged.

The complete developer-controlled test commands, failure matrix, and twelve-step WebApp walkthrough are in [the feature quickstart](../specs/094-full-wms-demo-data/quickstart.md).
