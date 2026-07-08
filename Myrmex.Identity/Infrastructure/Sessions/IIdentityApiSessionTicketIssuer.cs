using System.Security.Claims;

namespace Myrmex.Identity.Infrastructure.Sessions;

public interface IIdentityApiSessionTicketIssuer
{
    Task<string?> IssueAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);
}
