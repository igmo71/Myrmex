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

            string? actorId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? principal.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(actorId))
            {
                throw new InvalidOperationException(
                    "The authenticated principal does not contain an actor identifier claim.");
            }

            return actorId.Trim();
        }
    }
}
