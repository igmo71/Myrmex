# Phase 0 Research: Normalized SKU Physical Characteristics

**Date**: 2026-07-21

**Status**: Complete; provisional formula requires an early implementation verification

## Decision: Use the Evidence-Backed Provisional Conversion Formula

**Decision**: For planning, normalize each enabled characteristic as:

`normalized value = source numerator / source denominator × unit numerator / unit denominator`

This is the working 1C source-contract interpretation. Before coding or finalizing the normalizer behavior, verify it against additional representative linked 1C SKU and unit records. If real data contradicts it, stop formula-dependent implementation and update this research, the plan, contracts, and implementation approach.

**Rationale**: Supplied 1C samples establish the expected unit-factor orientation:

| Source unit | Unit factor | Canonical interpretation |
|---|---:|---|
| Kilogram | `1 / 1` | 1 kilogram |
| Tonne | `1000 / 1` | 1,000 kilograms |
| Cubic metre | `1 / 1` | 1 cubic metre |
| Litre | `1 / 1000` | 0.001 cubic metres |
| Cubic centimetre | `1 / 1,000,000` | 0.000001 cubic metres |
| Metre | `1 / 1` | 1 metre |
| Square metre | `1 / 1` | 1 square metre |

The supplied SKU weight sample has source value `0.001 / 1` and kilogram factor `1 / 1`, producing `0.001 kg` under the formula. These examples are sufficient to proceed with Phase 1 planning without requiring samples for every measurement type.

The repository's current source projections do not yet read the physical fields and contain no existing conversion arithmetic. Consequently, additional linked-record comparison remains an implementation prerequisite rather than a claim that every publication variant has already been proven.

**Implementation prerequisite**:

1. Before editing the normalizer, obtain additional read-only linked SKU/unit records through approved access.
2. Include at least one non-unity factor and an independently understood canonical result; all four measurement types are not required.
3. Confirm the formula, actual JSON numeric field types, compatibility with ordinary nullable numeric DTO properties in the existing transport, exact `ТипИзмеряемойВеличины` values, and the planned persistence precision/scale.
4. Record only the conclusion and any contract corrections needed; do not add raw credentials or source dumps.
5. If any result contradicts the rule, update the research and design before continuing formula-dependent code.

**Alternatives considered**:

- Block planning until all four measurement types have additional examples: rejected because the supplied samples already establish the working orientation.
- Infer the rule only from field names: rejected because the supplied factor evidence is the planning basis.
- Implement multiple orientations and choose heuristically: rejected as ambiguous behavior and unnecessary generalization.

## Decision: Keep Normalization Inside the Existing 1C SKU Paths

**Decision**: Extend the current full import and reactive synchronization flows. Read the 16 SKU characteristic fields in the existing SKU source and the three conversion fields in the existing 1C unit source. Use one SKU-specific normalizer inside `Myrmex.Integrations.OneC.StockKeepingUnits`; pass only four nullable canonical decimals into the existing WMS import command.

**Rationale**: This preserves explicit ownership. 1C identifiers, raw values, measurement-type tokens, and conversion factors remain integration concerns, while WMS owns only warehouse-meaningful canonical SKU state.

**Alternatives considered**:

- Persist 1C conversion metadata in WMS: rejected because it leaks source ownership across the module boundary.
- Add a generalized measurements framework: rejected because four fixed characteristics and units do not justify it.
- Add a separate import workflow: rejected because the existing SKU flows already own synchronization.

## Decision: Resolve Unit Definitions Per Synchronization Operation

**Decision**: Extend the integration-only unit source record and projection. For the full SKU import, reuse one `ReadAllAsync` result as an operation-scoped lookup. For reactive synchronization, resolve only the distinct referenced unit keys through the existing current-record read behavior. Do not add physical conversion fields to the WMS `UnitOfMeasure` entity or persist an integration cache.

**Rationale**: The unit catalog is already available through the 1C integration boundary. Operation-scoped reuse avoids repeated reads during one import while keeping transient source metadata out of WMS persistence.

**Alternatives considered**:

- Synchronize conversion factors into WMS units: rejected because WMS does not own 1C conversion rules.
- Add a persistent integration cache: rejected because the current outcome does not require it.

## Decision: Use Existing OData Materialization and Normalize Each Characteristic Independently

**Decision**: Prefer the existing OData transport and ordinary nullable numeric properties when they correctly represent the verified payload. Once the existing transport has materialized a SKU record, normalize each characteristic independently. A false use flag produces absence without a diagnostic. For an enabled characteristic, a missing numeric value, missing/deleted unit, mismatched type, a zero source or unit denominator, a zero unit numerator, arithmetic overflow, or another unresolvable reference produces absence for that characteristic plus a structured issue. It does not reject the SKU or block other characteristics.

A zero source characteristic numerator produces a valid known numeric zero. A zero measurement-unit numerator is an invalid conversion definition for weight, length, area, or volume and makes only that characteristic unresolved. A payload that the existing transport cannot deserialize remains an existing source-read/operation failure; this feature does not add a custom resilient OData parser, generalized token wrapper, or shared parsing framework. Volume is taken only from the independent volume fields.

**Rationale**: This directly preserves the specification's observable behavior and avoids conflating absence with zero.

**Alternatives considered**:

- Fail the whole SKU when one characteristic is bad: rejected because it discards independently valid warehouse data.
- Reject a zero source numerator: rejected because source zero and absence have distinct meanings.
- Accept a zero measurement-unit numerator: rejected because it does not define a valid conversion factor for a physical characteristic.
- Add resilient numeric-token infrastructure: rejected because ordinary nullable numeric fields and existing source-read failure behavior are sufficient for the verified contract.
- Derive volume from linear values: rejected because 1C provides volume independently.

## Decision: Reuse Existing Synchronization Logging

**Decision**: The normalizer returns structured per-characteristic issues without logging. The existing full-import or reactive-synchronizer caller emits each issue once through its current `ILogger`, then continues dispatching the SKU with valid values and nulls for unresolvable characteristics. Do not log an issue in both layers, aggregate issues into new infrastructure, persist them, or add a diagnostics workflow.

A whole unit-catalog or SKU-source transport failure remains an operation failure and does not dispatch nulls that would clear previously valid values.

**Rationale**: Existing logging meets the diagnostics requirement without misclassifying a successfully synchronized SKU or adding a subsystem.

**Alternatives considered**:

- Fail the synchronization request for one bad characteristic: rejected because the SKU and other characteristics must continue.
- Add a diagnostics table, endpoint, or UI: rejected as out of scope.

## Decision: Store Four Nullable Feature-Specific Decimals

**Decision**: Add `WeightKilograms`, `LengthMetres`, `AreaSquareMetres`, and `VolumeCubicMetres` as nullable decimals on the existing SKU aggregate. Plan four ordinary nullable `decimal(28,12)` columns on the existing SKU table, subject to confirmation during the early representative-record verification. If verified real values require different precision or scale, adjust the feature-specific decimal mapping before migration while retaining ordinary nullable decimal columns. Do not add a physical-characteristics entity or value framework.

**Rationale**: Twelve fractional digits preserve the supplied cubic-centimetre factor and small source values without expanding into a general units model. Sixteen integer digits are ample for SKU base-unit physical values. Null represents absence and `0` remains a stored value.

**Alternatives considered**:

- Reuse the inventory `decimal(18,4)` convention: rejected because it cannot preserve a `1 / 1,000,000` cubic-metre factor reliably.
- Store source ratios in WMS: rejected because WMS owns only normalized values.
- Create a separate table: rejected because the four values have the same lifecycle and identity as the SKU.

## Decision: Reuse the Existing SKU Aggregate, Response, and Edit Dialog

**Decision**: Extend `ImportStockKeepingUnits.Item`, the existing SKU aggregate/table, mapping, and `StockKeepingUnitDetails` response with the four nullable canonical values. Display them as a read-only section of `SkuEditDialog.razor` only in edit mode. Keep create/update requests, lookup contracts, grid columns, endpoints, and navigation unchanged.

Because the existing list and detail endpoints share `StockKeepingUnitDetails`, list responses may carry the values, but the SKU grid does not display or depend on them. This avoids a new response hierarchy or screen solely to suppress four nullable fields.

The WMS import handler's current data-version early exit must compare the four canonical values as well as source data version. Otherwise existing rows would not receive values after migration and a changed unit factor could not refresh a SKU whose own source version is unchanged.

**Rationale**: This is the smallest end-to-end extension of established ownership and navigation paths.

**Alternatives considered**:

- Add a physical-characteristics endpoint or screen: rejected because the values belong on the existing SKU view.
- Add grid or lookup presentation: rejected because the required user outcome is satisfied in the edit/details view.
- Add editable request fields: rejected because 1C owns the values for this feature.
- Split list and detail DTOs only for payload minimization: rejected as unnecessary complexity for four optional fields.

## Decision: Use Proportional Verification Only

**Decision**: Do not create a test project, fixture framework, performance benchmark, or load test. Complete the early formula prerequisite, then use focused build and end-to-end smoke checks of normalization, refresh/clear behavior, logging, persistence, the existing SKU response, and the read-only edit dialog.

**Rationale**: This matches the constitution's outcome-first simplicity principle and the repository's current lack of a tracked test project.

**Alternatives considered**:

- Add testing infrastructure for this feature: rejected as disproportionate and explicitly out of scope.
- Add synchronization performance baselines: rejected because the specification intentionally excludes performance work.
