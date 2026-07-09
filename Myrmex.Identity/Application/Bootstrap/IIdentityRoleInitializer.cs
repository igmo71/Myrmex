namespace Myrmex.Identity.Application.Bootstrap;

public interface IIdentityRoleInitializer
{
    Task EnsureRolesAsync(CancellationToken cancellationToken);
}
