# Data Model: Normalized SKU Physical Characteristics

## Ownership Boundaries

- `Myrmex.Integrations` owns all raw 1C field names, source ratios, unit references, measurement-type tokens, unit ratios, parsing, formula application, and normalization issues.
- `Myrmex.Modules.Wms` owns only normalized physical values attached to one SKU base unit.
- `Myrmex.Shared` exposes normalized read values without source identifiers or conversion metadata.
- `Myrmex.WebApp` displays normalized values and does not edit them.

## Persistent Entity: StockKeepingUnit

The existing WMS `StockKeepingUnit` aggregate gains four independent nullable properties.

| Field | Type | Canonical unit | Rules |
|---|---|---|---|
| `WeightKilograms` | `decimal?` | kg | Null means absent; numeric zero is retained. |
| `LengthMetres` | `decimal?` | m | Null means absent; numeric zero is retained. |
| `AreaSquareMetres` | `decimal?` | m² | Null means absent; numeric zero is retained. |
| `VolumeCubicMetres` | `decimal?` | m³ | Null means absent; numeric zero is retained; never derived from length. |

### Persistence

- Plan four nullable `decimal(28,12)` columns on the existing WMS SKU table. Confirm this precision and scale during the early representative-record verification; adjust the feature-specific mapping before migration if verified real values require it.
- Any adjusted mapping remains four ordinary nullable decimal columns and does not introduce a measurement framework.
- Add no table, index, foreign key, or backfill statement.
- Existing rows begin with all four values null and receive values on their next SKU synchronization.
- The migration and model snapshot remain in `Myrmex.Modules.Wms/Infrastructure/Persistence/Migrations/`.

### Aggregate Behavior

- Local SKU creation leaves all characteristics null.
- Local create/update request models do not accept characteristic values.
- The existing import operation supplies all four values together with ordinary imported SKU data.
- Any characteristic change updates the aggregate through the existing import mutation, timestamp, and details-updated event behavior.
- Existing deletion-mark, activation, base-unit, code, name, and description behavior remains unchanged.
- Import idempotency counts a record as unchanged only when its source data version and all four canonical nullable values match the stored values.

## Transient Integration Model: Characteristic Source

Each supported characteristic is mapped to one feature-specific transient shape inside the 1C boundary.

| Field | Shape | Rules |
|---|---|---|
| `Characteristic` | Weight, Length, Area, or Volume | Determines expected measurement type and canonical target property. |
| `Use` | Boolean | False produces absence without resolving remaining fields. |
| `SourceNumerator` | Ordinary nullable numeric field | Required when enabled; zero is a valid known source value. |
| `SourceDenominator` | Ordinary nullable numeric field | Required and nonzero when enabled. |
| `UnitExternalRefKey` | Nullable GUID | Required and non-empty when enabled. |

Use the existing OData transport and ordinary nullable numeric DTO properties when they match the verified payload. Independent characteristic handling begins after the transport successfully materializes the SKU record. A payload the existing transport cannot deserialize follows the existing source-read/operation failure path; do not add a custom resilient parser, token wrapper, or shared parsing framework for this feature.

## Transient Integration Model: Measurement Unit Definition

Existing `Catalog_УпаковкиЕдиницыИзмерения` source records gain transient normalization fields.

| Field | Shape | Rules |
|---|---|---|
| `Ref_Key` | GUID | Matches a characteristic's unit reference. |
| `DeletionMark` | Boolean | A deleted unit cannot resolve an enabled characteristic. |
| `ТипИзмеряемойВеличины` | Source discriminator | Must match the expected characteristic type. Exact wire tokens are verified in the early implementation prerequisite. |
| `Числитель` | Ordinary nullable numeric field | Required and nonzero; zero is an invalid physical conversion definition. |
| `Знаменатель` | Ordinary nullable numeric field | Required and nonzero. |

These fields are never added to the WMS `UnitOfMeasure` entity or persisted in integration storage.

## Transient Result: Normalized Physical Characteristics

The SKU-specific normalizer returns:

- the four nullable canonical decimal values;
- zero or more structured non-fatal issues, each identifying the characteristic and a stable reason; the normalizer does not log.

The provisional calculation is:

`source numerator / source denominator × unit numerator / unit denominator`

Arithmetic uses decimal operations, checks division by zero, rejects a zero measurement-unit numerator, checks overflow, and produces a value compatible with the planned feature-specific persistence scale. A zero source numerator produces a valid numeric-zero result.

## Validation Rules

For each characteristic independently:

1. If `Use` is false, output null without an issue.
2. Read the materialized nullable source numerator and denominator; reject only that characteristic if either is missing or the denominator is zero. A source numerator of zero is valid and produces known numeric zero after a valid unit definition is confirmed.
3. Resolve a non-empty referenced unit from the current operation's unit definitions.
4. Reject only that characteristic if the unit is missing, deleted, malformed, or has a mismatched measurement type.
5. Read the materialized nullable unit numerator and denominator; reject only that characteristic if either is missing, the unit numerator is zero, or the unit denominator is zero.
6. Apply the provisional formula and map the result to its fixed canonical property.
7. Preserve numeric zero distinctly from null.
8. Return any issue to the existing synchronization caller. That caller logs the issue once and continues with the SKU and other characteristics; the normalizer itself does not log or aggregate diagnostics.

A transport-level or deserialization failure while loading SKU or unit records fails/incompletes the existing synchronization operation before dispatch, preserving previously stored values rather than clearing them as if source values were individually invalid.

## State Transitions

| Prior state | Incoming source | Result |
|---|---|---|
| Any | Use flag false | Characteristic becomes null. |
| Null or value | Enabled and resolvable | Characteristic becomes the newly normalized value, including zero. |
| Null or value | Enabled but individually unresolvable | Characteristic becomes null; issue is logged; SKU and other values proceed. |
| Value | Changed source value or factor | Characteristic is replaced even if SKU source data version is unchanged. |
| Value | Unchanged source version and same normalized value | Characteristic and SKU import remain unchanged. |
| Any | Whole source/unit read fails | No WMS import is dispatched for that operation; existing values remain. |

## Read Model

`StockKeepingUnitDetails` gains the same four nullable canonical values. Create/update requests remain unchanged. The existing SKU list/details response may carry the values, but grid columns and lookup contracts remain unchanged. The edit dialog renders them read-only with fixed canonical unit labels.
