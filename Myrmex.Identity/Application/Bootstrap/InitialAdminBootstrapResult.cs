namespace Myrmex.Identity.Application.Bootstrap;

public sealed record InitialAdminBootstrapResult(
    InitialAdminBootstrapStatus Status,
    Guid? UserId,
    string? Email);

public enum InitialAdminBootstrapStatus
{
    Disabled,
    Created,
    ExistingUserAssignedAdminRole,
    ExistingUserAlreadyAdmin
}
