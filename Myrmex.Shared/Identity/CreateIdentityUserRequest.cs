namespace Myrmex.Shared.Identity;

public sealed record CreateIdentityUserRequest(
    string? Email,
    string? DisplayName,
    string? TemporaryPassword,
    IReadOnlyList<string> Roles);
