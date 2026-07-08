namespace Myrmex.Shared.Identity;

public sealed record IdentityUserDetails(
    Guid Id,
    string Email,
    string? DisplayName,
    IReadOnlyList<string> Roles);
