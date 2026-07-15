using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Myrmex.AspNetCore.Security;
using Myrmex.Integrations.OneC.Imports;
using Myrmex.Integrations.OneC.Transport;
using Myrmex.Shared.Integrations.OneC;

namespace Myrmex.Integrations.OneC.Endpoints;

public static class OneCEndpoints
{
    public static IEndpointRouteBuilder MapOneCIntegration(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints
            .MapGroup("/api/integrations/1c")
            .WithTags("Integrations 1C")
            .RequireAuthorization(MyrmexAuthorizationPolicies.WmsOperator);

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
        IOneCImportService importService,
        CancellationToken cancellationToken) =>
        ImportAsync(
            importService.ImportWarehousesAsync,
            cancellationToken);

    private static Task<IResult> ImportUnitsOfMeasureAsync(
        IOneCImportService importService,
        CancellationToken cancellationToken) =>
        ImportAsync(
            importService.ImportUnitsOfMeasureAsync,
            cancellationToken);

    private static Task<IResult> ImportStockKeepingUnitsAsync(
        IOneCImportService importService,
        CancellationToken cancellationToken) =>
        ImportAsync(
            importService.ImportStockKeepingUnitsAsync,
            cancellationToken);

    private static async Task<IResult> ImportAsync(
        Func<CancellationToken, Task<OneCImportResponse>> import,
        CancellationToken cancellationToken)
    {
        try
        {
            return TypedResults.Ok(await import(cancellationToken));
        }
        catch (OneCImportAlreadyInProgressException exception)
        {
            return CreateAlreadyInProgressProblem(exception);
        }
        catch (OneCTransportException exception)
        {
            return CreateProblem(exception);
        }
    }

    private static async Task<IResult> TestConnectionAsync(
        IOneCODataClient client,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
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

    private static IResult CreateAlreadyInProgressProblem(
        OneCImportAlreadyInProgressException exception)
    {
        const int status = StatusCodes.Status409Conflict;
        ProblemDetails details = new()
        {
            Type = $"https://httpstatuses.com/{status}",
            Title = "1С import already in progress",
            Status = status,
            Detail = exception.Message
        };
        details.Extensions["code"] = "OneCImport.AlreadyInProgress";

        return TypedResults.Json(
            details,
            statusCode: status,
            contentType: "application/problem+json");
    }
}
