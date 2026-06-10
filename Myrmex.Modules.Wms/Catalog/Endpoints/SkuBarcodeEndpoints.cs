using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AspNetCore.Results;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.SkuBarcodes;
using Myrmex.Modules.Wms.Catalog.Features.SkuBarcodes;

namespace Myrmex.Modules.Wms.Catalog.Endpoints;

internal static class SkuBarcodeEndpoints
{
    public static RouteGroupBuilder MapSkuBarcodeEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/sku-barcodes", CreateSkuBarcodeAsync)
            .WithName("CreateSkuBarcode")
            .WithSummary("Create SKU Barcode");

        return group;
    }

    private sealed record CreateSkuBarcodeRequest(
        Guid StockKeepingUnitId,
        string? Value,
        string? Symbology,
        bool IsPrimary);

    private static async Task<IResult> CreateSkuBarcodeAsync(
        CreateSkuBarcodeRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        BarcodeSymbology symbology = Enum.TryParse(
            request.Symbology,
            ignoreCase: false,
            out BarcodeSymbology parsedSymbology)
                ? parsedSymbology
                : (BarcodeSymbology)(-1);

        var command = new CreateSkuBarcode.Command(
            StockKeepingUnitId: request.StockKeepingUnitId,
            Value: request.Value,
            Symbology: symbology,
            IsPrimary: request.IsPrimary);

        var result = await commandDispatcher
            .DispatchAsync<CreateSkuBarcode.Command, ServiceResult<SkuBarcodeDetails>>(command, cancellationToken);

        return result.ToHttpResult();
    }
}
