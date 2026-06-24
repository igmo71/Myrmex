using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Myrmex.AspNetCore.Security;

public static class HttpContextActorExtensions
{
    public static string? GetActorId(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        ClaimsPrincipal principal = httpContext.User;

        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        string? actorId = principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.Identity.Name;

        return string.IsNullOrWhiteSpace(actorId)
            ? null
            : actorId.Trim();
    }
}
