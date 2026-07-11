using Microsoft.AspNetCore.Routing;
using Myrmex.Identity.Infrastructure.Endpoints;

namespace Myrmex.Identity.Infrastructure;

public static class IdentityEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMyrmexIdentityEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapIdentityUserEndpoints();

        return endpoints;
    }
}
