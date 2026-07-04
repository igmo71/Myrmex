using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AspNetCore.Results;
using Myrmex.AspNetCore.Security;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.DemoData.Configuration;
using Myrmex.Modules.Wms.DemoData.Features;
using Myrmex.Shared.Wms.DemoData;

namespace Myrmex.Modules.Wms.DemoData.Endpoints;

internal static class DemoDataAdminEndpoints
{
    public static IEndpointRouteBuilder MapDemoDataAdminEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints
            .MapGroup("/api/admin/demo-data")
            .WithTags("WMS Demo Data");

        group.MapPost("/seed", SeedAsync)
            .WithName("SeedWmsDemoData")
            .WithSummary("Seed the complete WMS demonstration dataset");

        group.MapPost("/clear", ClearAsync)
            .WithName("ClearWmsDemoData")
            .WithSummary("Clear mutable WMS application and demo data");

        return endpoints;
    }

    private static async Task<IResult> SeedAsync(
        HttpContext httpContext,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken = default)
    {
        string? actorId = httpContext.GetActorId();
        if (actorId is null)
        {
            return UnauthorizedResult();
        }

        ServiceResult<DemoDataOperationResponse> result = await commandDispatcher
            .DispatchAsync<SeedWmsDemoData.Command, ServiceResult<DemoDataOperationResponse>>(
                new SeedWmsDemoData.Command(actorId),
                cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ClearAsync(
        ClearDemoDataRequest request,
        HttpContext httpContext,
        ICommandDispatcher commandDispatcher,
        IOptions<WmsDemoDataOptions> options,
        CancellationToken cancellationToken = default)
    {
        string? actorId = httpContext.GetActorId();
        if (actorId is null)
        {
            return UnauthorizedResult();
        }

        ServiceError? guardError = ClearWmsDemoData.Handler.Validate(
            options.Value,
            request.Confirmation);
        if (guardError is not null)
        {
            return ServiceResult<DemoDataOperationResponse>
                .Fail(guardError)
                .ToHttpResult();
        }

        ServiceResult<DemoDataOperationResponse> result = await commandDispatcher
            .DispatchAsync<ClearWmsDemoData.Command, ServiceResult<DemoDataOperationResponse>>(
                new ClearWmsDemoData.Command(actorId, request.Confirmation),
                cancellationToken);
        return result.ToHttpResult();
    }

    private static IResult UnauthorizedResult() =>
        ServiceResult<DemoDataOperationResponse>
            .Fail(ServiceError.Unauthorized())
            .ToHttpResult();
}
