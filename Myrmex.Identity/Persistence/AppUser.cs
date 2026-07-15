using Microsoft.AspNetCore.Identity;

namespace Myrmex.Identity.Persistence;

public sealed class AppUser : IdentityUser<Guid>
{
    public const int MaxDisplayNameLength = 200;

    public string? DisplayName { get; set; }
}
