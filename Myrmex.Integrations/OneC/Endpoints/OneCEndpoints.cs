using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Myrmex.AspNetCore.Results;
using Myrmex.AspNetCore.Security;
using Myrmex.Core.Results;
using Myrmex.Integrations.OneC.Transport;
using Myrmex.Integrations.OneC.Imports;
using Myrmex.Shared.Integrations.OneC;

namespace Myrmex.Integrations.OneC.Endpoints;

public static class OneCEndpoints
{
    public static IEndpointRouteBuilder MapOneCIntegration(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/api/integrations/1c");

        group.MapPost("/connection/test", TestConnectionAsync)
            .WithName("TestOneCConnection")
            .WithSummary("Verify the 1С OData connection");

        group.MapPost("/warehouses/import", ImportWarehousesAsync)
            .WithName("ImportOneCWarehouses")
            .WithSummary("Import warehouses from 1С");

        group.MapPost("/uoms/import", ImportUnitsOfMeasureAsync)
            .WithName("ImportOneCUnitsOfMeasure")
            .WithSummary("Import units of measure from 1С");

        group.MapPost("/skus/import", ImportStockKeepingUnitsAsync)
            .WithName("ImportOneCStockKeepingUnits")
            .WithSummary("Import nomenclature from 1С as SKUs");

        return endpoints;
    }

    private static Task<IResult> ImportWarehousesAsync(
        HttpContext httpContext,
        IOneCImportService importService,
        CancellationToken cancellationToken) =>
        ImportAsync(
            httpContext,
            importService.ImportWarehousesAsync,
            cancellationToken);

    private static Task<IResult> ImportUnitsOfMeasureAsync(
        HttpContext httpContext,
        IOneCImportService importService,
        CancellationToken cancellationToken) =>
        ImportAsync(
            httpContext,
            importService.ImportUnitsOfMeasureAsync,
            cancellationToken);

    private static Task<IResult> ImportStockKeepingUnitsAsync(
        HttpContext httpContext,
        IOneCImportService importService,
        CancellationToken cancellationToken) =>
        ImportAsync(
            httpContext,
            importService.ImportStockKeepingUnitsAsync,
            cancellationToken);

    private static async Task<IResult> ImportAsync(
        HttpContext httpContext,
        Func<CancellationToken, Task<OneCImportResponse>> import,
        CancellationToken cancellationToken)
    {
        if (httpContext.GetActorId() is null)
        {
            return ServiceResult<OneCImportResponse>
                .Fail(ServiceError.Unauthorized())
                .ToHttpResult();
        }

        try
        {
            return TypedResults.Ok(await import(cancellationToken));
        }
        catch (OneCTransportException exception)
        {
            return CreateProblem(exception);
        }
    }

    private static async Task<IResult> TestConnectionAsync(
        HttpContext httpContext,
        IOneCODataClient client,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (httpContext.GetActorId() is null)
        {
            return ServiceResult<OneCConnectionTestResponse>
                .Fail(ServiceError.Unauthorized())
                .ToHttpResult();
        }

        try
        {
            await client.TestConnectionAsync(cancellationToken);
            return TypedResults.Ok(new OneCConnectionTestResponse(
                timeProvider.GetUtcNow(),
                IsReady: true,
                CheckedReferenceTypes: ["warehouses", "uoms", "skus"]));
        }
        catch (OneCTransportException exception)
        {
            return CreateProblem(exception);
        }
    }

    private static IResult CreateProblem(OneCTransportException exception)
    {
        (int status, string code, string title) = exception.Reason switch
        {
            OneCTransportFailureReason.Disabled or OneCTransportFailureReason.InvalidConfiguration =>
                (StatusCodes.Status400BadRequest, "OneC.ConfigurationInvalid", "Invalid 1С configuration"),
            OneCTransportFailureReason.AuthenticationFailed =>
                (StatusCodes.Status502BadGateway, "OneC.AuthenticationFailed", "1С authentication failed"),
            OneCTransportFailureReason.EntitySetUnavailable =>
                (StatusCodes.Status502BadGateway, "OneC.EntitySetUnavailable", "1С entity set unavailable"),
            OneCTransportFailureReason.MalformedResponse =>
                (StatusCodes.Status502BadGateway, "OneC.MalformedResponse", "Invalid 1С response"),
            OneCTransportFailureReason.Timeout =>
                (StatusCodes.Status504GatewayTimeout, "OneC.Timeout", "1С request timed out"),
            _ => (StatusCodes.Status502BadGateway, "OneC.SourceUnavailable", "1С source unavailable")
        };

        ProblemDetails details = new()
        {
            Type = $"https://httpstatuses.com/{status}",
            Title = title,
            Status = status,
            Detail = exception.Message
        };
        details.Extensions["code"] = code;

        return TypedResults.Json(
            details,
            statusCode: status,
            contentType: "application/problem+json");
    }
}
