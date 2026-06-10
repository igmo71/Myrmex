using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AppDispatching.QueryDispatching;
using Myrmex.AspNetCore.Results;
using Myrmex.Core.Application.Queries;
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

        group.MapGet("/sku-barcodes/{skuBarcodeId:guid}", GetSkuBarcodeByIdAsync)
            .WithName("GetSkuBarcodeById")
            .WithSummary("Get SKU Barcode By Id");

        group.MapGet("/sku-barcodes", ListSkuBarcodesAsync)
            .WithName("ListSkuBarcodes")
            .WithSummary("List SKU Barcodes");

        group.MapPut("/sku-barcodes/{skuBarcodeId:guid}", UpdateSkuBarcodeDetailsAsync)
            .WithName("UpdateSkuBarcodeDetails")
            .WithSummary("Update SKU Barcode Details");

        group.MapPost("/sku-barcodes/{skuBarcodeId:guid}/deactivate", DeactivateSkuBarcodeAsync)
            .WithName("DeactivateSkuBarcode")
            .WithSummary("Deactivate SKU Barcode");

        group.MapPost("/sku-barcodes/{skuBarcodeId:guid}/reactivate", ReactivateSkuBarcodeAsync)
            .WithName("ReactivateSkuBarcode")
            .WithSummary("Reactivate SKU Barcode");

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

    private sealed record UpdateSkuBarcodeDetailsRequest(
        string? Value,
        string? Symbology,
        bool IsPrimary);

    private static async Task<IResult> UpdateSkuBarcodeDetailsAsync(
        Guid skuBarcodeId,
        UpdateSkuBarcodeDetailsRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        BarcodeSymbology symbology = Enum.TryParse(
            request.Symbology,
            ignoreCase: false,
            out BarcodeSymbology parsedSymbology)
                ? parsedSymbology
                : (BarcodeSymbology)(-1);

        var command = new UpdateSkuBarcodeDetails.Command(
            SkuBarcodeId: skuBarcodeId,
            Value: request.Value,
            Symbology: symbology,
            IsPrimary: request.IsPrimary);

        var result = await commandDispatcher
            .DispatchAsync<UpdateSkuBarcodeDetails.Command, ServiceResult<SkuBarcodeDetails>>(command, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> DeactivateSkuBarcodeAsync(
        Guid skuBarcodeId,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var command = new DeactivateSkuBarcode.Command(skuBarcodeId);

        var result = await commandDispatcher
            .DispatchAsync<DeactivateSkuBarcode.Command, ServiceResult<SkuBarcodeDetails>>(command, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> ReactivateSkuBarcodeAsync(
        Guid skuBarcodeId,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var command = new ReactivateSkuBarcode.Command(skuBarcodeId);

        var result = await commandDispatcher
            .DispatchAsync<ReactivateSkuBarcode.Command, ServiceResult<SkuBarcodeDetails>>(command, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> GetSkuBarcodeByIdAsync(
        Guid skuBarcodeId,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken)
    {
        var query = new GetSkuBarcodeById.Query(skuBarcodeId);

        var result = await queryDispatcher
            .DispatchAsync<GetSkuBarcodeById.Query, ServiceResult<SkuBarcodeDetails>>(query, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> ListSkuBarcodesAsync(
        int? skip,
        int? take,
        string? searchText,
        string? sortBy,
        bool? sortDescending,
        bool? includeInactive,
        Guid? stockKeepingUnitId,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken)
    {
        var query = new ListSkuBarcodes.Query
        {
            Skip = skip ?? 0,
            Take = take ?? ListQuery.DefaultTake,
            SearchText = searchText,
            SortBy = sortBy,
            SortDescending = sortDescending ?? false,
            IncludeInactive = includeInactive ?? false,
            StockKeepingUnitId = stockKeepingUnitId
        };

        var result = await queryDispatcher
            .DispatchAsync<ListSkuBarcodes.Query, ServiceResult<ListResult<SkuBarcodeDetails>>>(query, cancellationToken);

        return result.ToHttpResult();
    }
}
