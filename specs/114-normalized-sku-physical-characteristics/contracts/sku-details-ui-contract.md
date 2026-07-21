# Contract: SKU Details and Read-Only Physical Characteristics

## Existing API Surface

No endpoint is added. Extend the existing SKU response used by:

- `GET /api/wms/catalog/skus/{stockKeepingUnitId}`;
- existing SKU list/create/update responses that already use `StockKeepingUnitDetails`.

`StockKeepingUnitDetails` gains four nullable JSON numbers:

```json
{
  "weightKilograms": 0.001,
  "lengthMetres": null,
  "areaSquareMetres": 2.5,
  "volumeCubicMetres": 0
}
```

Semantics:

- null means the characteristic is absent;
- zero is a known value and must serialize as numeric zero;
- values are already normalized and expose no 1C identifiers or factors;
- canonical units are fixed by the property names.

Create and update request contracts remain unchanged and contain no physical-characteristic properties. Lookup contracts remain unchanged.

Because list and detail operations currently share the response type, list payloads may carry the four nullable values. The SKU grid must not add columns or other presentation for them.

## Existing WebApp Surface

Extend `Myrmex.WebApp/Components/Pages/Wms/Catalog/SkuPages/SkuEditDialog.razor` only when `IsEditMode` is true.

The dialog displays a read-only physical-characteristics section with:

| Label | Value | Unit |
|---|---|---|
| Weight | `WeightKilograms` | kg |
| Length | `LengthMetres` | m |
| Area | `AreaSquareMetres` | m² |
| Volume | `VolumeCubicMetres` | m³ |

- Render values as text, not bound or disabled input controls.
- Display numeric zero as `0` with its unit.
- Display null using the existing `Common.NotAvailable` localization.
- Use culture-aware formatting and retain meaningful fractional digits so a nonzero value is never rendered as zero.
- Add labels/headings to the existing `SharedResource.resx`, `SharedResource.en-US.resx`, and `SharedResource.ru-RU.resx` resources.
- Do not modify `SkuGrid.razor`, SKU lookup UI/contracts, navigation, or add another screen/dialog.

## Acceptance Contract

1. An SKU with all four values shows all four canonical values read-only in the edit dialog.
2. An SKU with a mixture of null and zero shows “Not available” for null and `0 <unit>` for zero.
3. An SKU with only volume shows volume without requiring length or another dimension.
4. Saving ordinary editable SKU fields does not send or alter physical characteristics.
5. Existing SKU grids and lookups retain their current columns and behavior.
