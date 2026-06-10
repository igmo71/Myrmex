using Myrmex.Modules.Wms.Catalog.Domain.SkuBarcodes;
using System.Linq.Expressions;

namespace Myrmex.Modules.Wms.Catalog.Features.SkuBarcodes;

internal sealed record SkuBarcodeDetails(
    Guid Id,
    Guid StockKeepingUnitId,
    string Value,
    BarcodeSymbology Symbology,
    bool IsPrimary,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc)
{
    public static SkuBarcodeDetails From(SkuBarcode skuBarcode)
    {
        return new SkuBarcodeDetails(
            skuBarcode.Id,
            skuBarcode.StockKeepingUnitId,
            skuBarcode.Value,
            skuBarcode.Symbology,
            skuBarcode.IsPrimary,
            skuBarcode.IsActive,
            skuBarcode.CreatedAtUtc,
            skuBarcode.UpdatedAtUtc);
    }

    public static Expression<Func<SkuBarcode, SkuBarcodeDetails>> Projection =>
        skuBarcode => new SkuBarcodeDetails(
            skuBarcode.Id,
            skuBarcode.StockKeepingUnitId,
            skuBarcode.Value,
            skuBarcode.Symbology,
            skuBarcode.IsPrimary,
            skuBarcode.IsActive,
            skuBarcode.CreatedAtUtc,
            skuBarcode.UpdatedAtUtc);
}
