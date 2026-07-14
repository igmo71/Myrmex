using Microsoft.AspNetCore.Identity;

namespace Myrmex.Identity.Persistence;

public sealed class AppRole : IdentityRole<Guid>
{
    public AppRole()
    {
    }

    public AppRole(string roleName)
        : base(roleName)
    {
    }
}
