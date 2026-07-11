using Microsoft.AspNetCore.Identity;

namespace Myrmex.Identity.Persistence;

public sealed class MyrmexRole : IdentityRole<Guid>
{
    public MyrmexRole()
    {
    }

    public MyrmexRole(string roleName)
        : base(roleName)
    {
    }
}
