using System.Text.Json.Serialization;

namespace Myrmex.Modules.Wms.Catalog.Domain.SkuBarcodes;

[JsonConverter(typeof(JsonStringEnumConverter<BarcodeSymbology>))]
internal enum BarcodeSymbology
{
    Unknown = 0,
    Ean13 = 1,
    Ean8 = 2,
    UpcA = 3,
    Code128 = 4,
    QrCode = 5,
    Internal = 6
}
