# Contract: 1C SKU Physical-Characteristic Normalization

## Scope

This contract extends the existing `Catalog_Номенклатура` SKU reads and `Catalog_УпаковкиЕдиницыИзмерения` unit reads. It does not define a new endpoint, import workflow, persistent cache, diagnostics subsystem, packaging model, or generalized units service.

## SKU Source Fields

The existing SKU projection retains its current fields and adds:

| Characteristic | Use flag | Source numerator | Source denominator | Unit reference |
|---|---|---|---|---|
| Weight | `ВесИспользовать` | `ВесЧислитель` | `ВесЗнаменатель` | `ВесЕдиницаИзмерения_Key` |
| Length | `ДлинаИспользовать` | `ДлинаЧислитель` | `ДлинаЗнаменатель` | `ДлинаЕдиницаИзмерения_Key` |
| Area | `ПлощадьИспользовать` | `ПлощадьЧислитель` | `ПлощадьЗнаменатель` | `ПлощадьЕдиницаИзмерения_Key` |
| Volume | `ОбъемИспользовать` | `ОбъемЧислитель` | `ОбъемЗнаменатель` | `ОбъемЕдиницаИзмерения_Key` |

Use ordinary nullable numeric DTO properties through the existing OData transport when they represent the verified payload. Independent characteristic handling applies after the transport successfully materializes the SKU record. A payload the existing transport cannot deserialize remains an existing source-read/operation failure; do not add a custom resilient parser, generalized token wrapper, or shared parsing framework.

## Unit Source Fields

The existing unit projection retains its current fields and adds:

- `ТипИзмеряемойВеличины`
- `Числитель`
- `Знаменатель`

The referenced unit must exist, not be deletion-marked, have the expected measurement type, and provide ordinary numeric numerator/denominator values. Its numerator and denominator must both be nonzero.

## Provisional Formula

For an enabled, resolvable characteristic:

`normalized value = source numerator / source denominator × unit numerator / unit denominator`

Planning evidence includes these unit factors:

- kilogram `1 / 1` kg;
- tonne `1000 / 1` kg;
- cubic metre `1 / 1` m³;
- litre `1 / 1000` m³;
- cubic centimetre `1 / 1,000,000` m³;
- metre `1 / 1` m;
- square metre `1 / 1` m².

A source weight `0.001 / 1` with kilogram factor `1 / 1` normalizes to `0.001 kg`.

Before implementing the normalizer, verify this rule and the exact measurement-type wire values against additional representative linked 1C records. A contradiction requires updating the research/design before formula-dependent implementation continues.

## Resolution Outcomes

| Condition | Canonical output | Diagnostic behavior |
|---|---|---|
| Use flag false | Null | None |
| Enabled and resolvable | Calculated decimal, including zero | None |
| Source numerator zero with valid remaining fields | Numeric zero | None |
| Missing source ratio after successful record materialization | Null for that characteristic | Structured issue returned to caller |
| Source denominator zero | Null for that characteristic | Structured issue returned to caller |
| Missing/empty/unknown/deleted unit reference | Null for that characteristic | Structured issue returned to caller |
| Unit measurement type mismatch | Null for that characteristic | Structured issue returned to caller |
| Missing unit ratio after successful record materialization | Null for that characteristic | Structured issue returned to caller |
| Unit numerator zero | Null for that characteristic | Structured issue returned to caller |
| Unit denominator zero or arithmetic overflow | Null for that characteristic | Structured issue returned to caller |
| Whole SKU/unit source read or deserialization fails | Do not dispatch the SKU operation | Existing operation failure/diagnostics behavior |

Each characteristic resolves independently. A normalization issue does not reject the SKU or suppress other valid characteristics. Volume is never derived.

The normalizer returns structured per-characteristic issues and does not log them. The existing full-import or reactive-synchronizer caller logs each returned issue once. No issue is logged in both layers, aggregated into new infrastructure, or persisted.

## Integration-to-WMS Command Boundary

`ImportStockKeepingUnits.Item` gains only:

- `decimal? WeightKilograms`
- `decimal? LengthMetres`
- `decimal? AreaSquareMetres`
- `decimal? VolumeCubicMetres`

It does not carry 1C unit keys, measurement types, source ratios, or normalization issue objects into WMS.

## Existing Flow Reuse

- Full import: load unit definitions once per existing SKU import operation, normalize each existing SKU page, receive structured issues, log each issue once in the caller, and dispatch the existing WMS batch command.
- Reactive synchronization: resolve the distinct physical-unit keys for the current SKU, normalize, receive structured issues, log each issue once in the caller, and dispatch the existing one-item WMS command. Existing base-UoM repair remains unchanged.
- No resolved unit data survives beyond the synchronization operation.
