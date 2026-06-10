# Contract: Catalog/SKU Barcode API

This contract defines the expected external behavior for the Catalog/SKU Barcode MVP. It follows existing WMS Catalog write/action result behavior and read/load error behavior. UI screens are out of scope for this phase.

## API Route Group

Base route: `/api/wms/catalog`

Tags: `Wms Catalog`

SKU barcode route prefix: `/api/wms/catalog/sku-barcodes`

## Payloads

### SkuBarcodeDetails

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "stockKeepingUnitId": "11111111-1111-1111-1111-111111111111",
  "value": "AbC-123",
  "symbology": "Code128",
  "isPrimary": true,
  "isActive": true,
  "createdAtUtc": "2026-06-09T00:00:00+00:00",
  "updatedAtUtc": null
}
```

On create, `updatedAtUtc` is `null`. It is set only after a successful update, deactivate, or reactivate operation.

### CreateSkuBarcodeRequest

```json
{
  "stockKeepingUnitId": "11111111-1111-1111-1111-111111111111",
  "value": "  AbC-123  ",
  "symbology": "Code128",
  "isPrimary": true
}
```

### UpdateSkuBarcodeDetailsRequest

```json
{
  "value": "AbC-123",
  "symbology": "Code128",
  "isPrimary": false
}
```

### ListResult<SkuBarcodeDetails>

```json
{
  "items": [],
  "totalCount": 0,
  "skip": 0,
  "take": 20
}
```

## Endpoints

### Create SKU Barcode

`POST /api/wms/catalog/sku-barcodes`

**Request**: `CreateSkuBarcodeRequest`

**Success**: returns `SkuBarcodeDetails` for the created active SKU barcode.

**Behavior**:

- Requires an existing SKU.
- Trims leading and trailing whitespace before storing `value`.
- Preserves casing and internal whitespace in `value`.
- Stores the trimmed barcode directly in `value`.
- Does not expose or persist `normalizedValue`.
- Requires a supported `symbology` value.
- When `isPrimary=true`, clears primary status from other active barcodes for the same SKU.
- Returns `updatedAtUtc: null` for a newly created SKU barcode.

**Failure behavior**:

- Missing SKU returns not-found ProblemDetails with code `StockKeepingUnit.NotFound` or an equivalent existing missing-SKU error.
- Missing, blank-after-trim, or overlong value returns validation ProblemDetails with `value` field details.
- Unsupported symbology returns validation ProblemDetails with `symbology` field details.
- Duplicate trimmed value using case-sensitive comparison returns conflict ProblemDetails with code `SkuBarcode.ValueAlreadyExists` and field `value`.

### List SKU Barcodes

`GET /api/wms/catalog/sku-barcodes`

**Query parameters**:

- `skip`
- `take`
- `searchText`
- `sortBy`
- `sortDescending`
- `includeInactive`
- `stockKeepingUnitId`

**Success**: returns `ListResult<SkuBarcodeDetails>`.

**Behavior**:

- Default list excludes inactive SKU barcodes.
- `includeInactive=true` includes inactive SKU barcodes.
- `stockKeepingUnitId` filters results to one SKU when supplied.
- Search matches `value`.
- Supported `sortBy` values are `value`, `symbology`, and `isActive`.
- Unknown sort fields fall back to value ordering.
- Sorting must not use provider-specific branching or in-memory ordering workarounds.

### Get SKU Barcode By Id

`GET /api/wms/catalog/sku-barcodes/{skuBarcodeId:guid}`

**Success**: returns `SkuBarcodeDetails` for active or inactive SKU barcodes.

**Failure behavior**:

- Missing SKU barcode returns not-found ProblemDetails with code `SkuBarcode.NotFound`.

### Update SKU Barcode Details

`PUT /api/wms/catalog/sku-barcodes/{skuBarcodeId:guid}`

**Request**: `UpdateSkuBarcodeDetailsRequest`

**Success**: returns updated `SkuBarcodeDetails`.

**Behavior**:

- Owning SKU is not accepted in the update payload and is not changed.
- Trims leading and trailing whitespace before storing `value`.
- Preserves casing and internal whitespace in `value`.
- Requires a supported `symbology` value.
- When an active barcode is updated with `isPrimary=true`, clears primary status from other active barcodes for the same SKU.
- Updating `isPrimary=false` clears only the requested barcode's primary status.
- Updating an inactive barcode with `isPrimary=true` fails with unsupported primary-change feedback; reactivate first, then explicitly update as primary.

**Failure behavior**:

- Missing SKU barcode returns not-found ProblemDetails with code `SkuBarcode.NotFound`.
- Invalid value or symbology returns validation ProblemDetails with field details.
- Duplicate trimmed value using case-sensitive comparison returns conflict ProblemDetails with code `SkuBarcode.ValueAlreadyExists` and field `value`.
- Unsupported primary change returns ProblemDetails with code `SkuBarcode.UnsupportedPrimaryChange` or an equivalent existing error code.

### Deactivate SKU Barcode

`POST /api/wms/catalog/sku-barcodes/{skuBarcodeId:guid}/deactivate`

**Success**: returns current `SkuBarcodeDetails` with `isActive=false`.

**Behavior**:

- Repeating the operation for an inactive SKU barcode succeeds and returns current details.
- If the barcode was primary, the operation clears `isPrimary`.
- No other barcode is promoted to primary.
- A SKU may have zero active primary barcodes after deactivation.

**Failure behavior**:

- Missing SKU barcode returns not-found ProblemDetails with code `SkuBarcode.NotFound`.

### Reactivate SKU Barcode

`POST /api/wms/catalog/sku-barcodes/{skuBarcodeId:guid}/reactivate`

**Success**: returns current `SkuBarcodeDetails` with `isActive=true` and `isPrimary=false`.

**Behavior**:

- Repeating the operation for an active SKU barcode succeeds and returns current details.
- Reactivated barcodes are non-primary by default.
- To make a reactivated barcode primary, a caller must explicitly update it with `isPrimary=true`.

**Failure behavior**:

- Missing SKU barcode returns not-found ProblemDetails with code `SkuBarcode.NotFound`.

## Web API Client Contract

Client path, if client support is included: `Myrmex.WebApp/Wms/Catalog/WmsCatalogApiClient.cs`

Expected additional methods:

- `ListSkuBarcodesAsync(ListSkuBarcodesRequest request, CancellationToken cancellationToken = default)`
- `GetSkuBarcodeByIdAsync(Guid skuBarcodeId, CancellationToken cancellationToken = default)`
- `TryCreateSkuBarcodeAsync(CreateSkuBarcodeRequest request, CancellationToken cancellationToken = default)`
- `TryUpdateSkuBarcodeDetailsAsync(Guid skuBarcodeId, UpdateSkuBarcodeDetailsRequest request, CancellationToken cancellationToken = default)`
- `TryDeactivateSkuBarcodeAsync(Guid skuBarcodeId, CancellationToken cancellationToken = default)`
- `TryReactivateSkuBarcodeAsync(Guid skuBarcodeId, CancellationToken cancellationToken = default)`

Read/load methods throw the existing API exception shape on failed responses. Write/action methods return the existing API result shape on failed responses.

Client support must reuse existing/local Catalog API primitives and must not introduce a new error/result pattern or UI behavior.

## Out of Scope Contract

No endpoint, payload, client method, or contract may expose:

- BarcodeType reference data.
- Generic barcode ownership.
- Generic `/barcodes` route behavior.
- OwnerType or OwnerId.
- `normalizedValue`.
- Scanning.
- Printing.
- Labels.
- GS1 parsing.
- Check digit validation.
- Packaging.
- SKU/UoM conversion.
- Inventory.
- Receiving.
- LPN behavior.
- Picking or shipping behavior.
- Integration state or messages.
- UI pages, navigation, forms, grids, dialogs, or component behavior.
