using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Myrmex.AspNetCore.Security;
using Myrmex.Identity.Infrastructure.Configuration;
using Myrmex.Identity.Persistence;
using Myrmex.Shared.Identity;
using System.Security.Claims;

namespace Myrmex.Identity.Infrastructure.Sessions;

public sealed class IdentityApiSessionTicketIssuer(
    MyrmexIdentityDbContext dbContext,
    SignInManager<MyrmexUser> signInManager,
    IOptionsMonitor<CookieAuthenticationOptions> cookieOptions,
    TimeProvider timeProvider)
    : IIdentityApiSessionTicketIssuer
{
    public async Task<string?> IssueAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);
        cancellationToken.ThrowIfCancellationRequested();

        if (principal.Identity?.IsAuthenticated != true ||
            !TryGetSingleUserId(principal, out Guid userId))
        {
            return null;
        }

        MyrmexUser? user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null ||
            !await signInManager.CanSignInAsync(user) ||
            (user.LockoutEnabled && user.LockoutEnd > timeProvider.GetUtcNow()))
        {
            return null;
        }

        string[] persistedRoles = await (
                from userRole in dbContext.UserRoles.AsNoTracking()
                join role in dbContext.Roles.AsNoTracking()
                    on userRole.RoleId equals role.Id
                where userRole.UserId == user.Id && role.Name != null
                select role.Name!)
            .ToArrayAsync(cancellationToken);

        HashSet<string> supportedRoles =
        [
            IdentityRoleNames.MyrmexAdmin,
            IdentityRoleNames.WmsOperator
        ];
        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            .. persistedRoles
                .Where(supportedRoles.Contains)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .Select(role => new Claim(ClaimTypes.Role, role))
        ];

        ClaimsIdentity identity = new(
            claims,
            MyrmexAuthenticationSchemes.ApiSession,
            ClaimTypes.Name,
            ClaimTypes.Role);
        DateTimeOffset issuedUtc = timeProvider.GetUtcNow();
        AuthenticationProperties properties = new()
        {
            IssuedUtc = issuedUtc,
            ExpiresUtc = issuedUtc.AddMinutes(
                IdentityDataProtectionOptions.RequiredApiSessionLifetimeMinutes),
            IsPersistent = false,
            AllowRefresh = false
        };
        AuthenticationTicket ticket = new(
            new ClaimsPrincipal(identity),
            properties,
            MyrmexAuthenticationSchemes.ApiSession);

        CookieAuthenticationOptions options = cookieOptions.Get(
            MyrmexAuthenticationSchemes.ApiSession);
        return options.TicketDataFormat.Protect(ticket);
    }

    private static bool TryGetSingleUserId(
        ClaimsPrincipal principal,
        out Guid userId)
    {
        string[] values = principal.FindAll(ClaimTypes.NameIdentifier)
            .Select(claim => claim.Value)
            .ToArray();
        return values.Length == 1 &&
            Guid.TryParse(values[0], out userId) &&
            userId != Guid.Empty;
    }
}
