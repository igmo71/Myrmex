using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AspNetCore.Results;
using Myrmex.AspNetCore.Security;
using Myrmex.Core.Results;
using Myrmex.Identity.Application.Users;
using Myrmex.Shared.Identity;

namespace Myrmex.Identity.Infrastructure.Endpoints;

internal static class IdentityUserEndpoints
{
    public static IEndpointRouteBuilder MapIdentityUserEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints
            .MapGroup("/api/identity")
            .RequireAuthorization(MyrmexAuthorizationPolicies.MyrmexAdmin)
            .WithTags("Identity");

        group.MapPost("/users", CreateUserAsync)
            .WithName("CreateIdentityUser")
            .WithSummary("Create an Identity user");

        return endpoints;
    }

    private static async Task<IResult> CreateUserAsync(
        CreateIdentityUserRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var command = new CreateUser.Command(
            request.Email,
            request.DisplayName,
            request.TemporaryPassword,
            request.Roles);

        ServiceResult<IdentityUserDetails> result = await commandDispatcher
            .DispatchAsync<CreateUser.Command, ServiceResult<IdentityUserDetails>>(
                command,
                cancellationToken);

        return result.IsSuccess
            ? TypedResults.Created(
                $"/api/identity/users/{result.Value.Id}",
                result.Value)
            : ServiceResult<IdentityUserDetails>.Fail(result.Error!).ToHttpResult();
    }
}
