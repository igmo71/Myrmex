using Microsoft.AspNetCore.Http;
using Myrmex.Core.Application.Security;
using System.Security.Claims;

namespace Myrmex.AspNetCore.Security;

public sealed class HttpContextActorContext(IHttpContextAccessor httpContextAccessor) : IActorContext
{
    public string ActorId
    {
        get
        {
            HttpContext httpContext = httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException(
                    "Actor context is unavailable outside an active HTTP request.");
            ClaimsPrincipal principal = httpContext.User;

            if (principal.Identity?.IsAuthenticated != true)
            {
                throw new InvalidOperationException(
                    "Actor context requires an authenticated principal.");
            }

            string[] userIds = principal.FindAll(ClaimTypes.NameIdentifier)
                .Select(claim => claim.Value)
                .ToArray();
            if (userIds.Length != 1 ||
                !Guid.TryParse(userIds[0], out Guid userId) ||
                userId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "The authenticated principal does not contain one valid Identity user ID.");
            }

            return userId.ToString();
        }
    }
}
