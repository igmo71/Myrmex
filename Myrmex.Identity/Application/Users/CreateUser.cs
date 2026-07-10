using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Identity.Persistence;
using Myrmex.Shared.Identity;
using System.Net.Mail;

namespace Myrmex.Identity.Application.Users;

public static class CreateUser
{
    private static readonly HashSet<string> SupportedRoles =
    [
        IdentityRoleNames.MyrmexAdmin,
        IdentityRoleNames.WmsOperator
    ];

    public sealed record Command(
        string? Email,
        string? DisplayName,
        string? TemporaryPassword,
        IReadOnlyList<string> Roles)
        : ICommand<ServiceResult<IdentityUserDetails>>;

    public sealed class Handler(
        MyrmexIdentityDbContext dbContext,
        UserManager<MyrmexUser> userManager)
        : ICommandHandler<Command, ServiceResult<IdentityUserDetails>>
    {
        public async Task<ServiceResult<IdentityUserDetails>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);
            cancellationToken.ThrowIfCancellationRequested();

            ValidationState validation = Validate(command);
            if (validation.Errors.Count > 0)
            {
                return Invalid(validation.Errors);
            }

            string normalizedEmail = userManager.NormalizeEmail(validation.Email);
            string normalizedUserName = userManager.NormalizeName(validation.Email);

            bool duplicateExists = await dbContext.Users
                .AsNoTracking()
                .AnyAsync(
                    user =>
                        user.NormalizedEmail == normalizedEmail ||
                        user.NormalizedUserName == normalizedUserName,
                    cancellationToken);
            if (duplicateExists)
            {
                return DuplicateEmail();
            }

            return await dbContext.Database
                .CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    await using var transaction = await dbContext.Database
                        .BeginTransactionAsync(cancellationToken);

                    MyrmexUser user = new()
                    {
                        Id = Guid.NewGuid(),
                        UserName = validation.Email,
                        NormalizedUserName = normalizedUserName,
                        Email = validation.Email,
                        NormalizedEmail = normalizedEmail,
                        EmailConfirmed = true,
                        DisplayName = validation.DisplayName
                    };

                    IdentityResult createResult = await userManager.CreateAsync(
                        user,
                        validation.TemporaryPassword);
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!createResult.Succeeded)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return MapCreateFailure(createResult);
                    }

                    IdentityResult roleResult = await userManager.AddToRolesAsync(
                        user,
                        validation.Roles);
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!roleResult.Succeeded)
                    {
                        await userManager.DeleteAsync(user);
                        await transaction.RollbackAsync(cancellationToken);
                        return RoleAssignmentFailed();
                    }

                    await transaction.CommitAsync(cancellationToken);

                    return ServiceResult<IdentityUserDetails>.Success(new IdentityUserDetails(
                        user.Id,
                        validation.Email,
                        validation.DisplayName,
                        validation.Roles));
                });
        }
    }

    private static ValidationState Validate(Command command)
    {
        List<ServiceError> errors = [];

        string email = command.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add(ValidationError(
                "IdentityUser.EmailRequired",
                "Email is required.",
                nameof(Command.Email)));
        }
        else if (!IsValidEmail(email))
        {
            errors.Add(ValidationError(
                "IdentityUser.EmailInvalid",
                "Email is invalid.",
                nameof(Command.Email)));
        }

        string? displayName = string.IsNullOrWhiteSpace(command.DisplayName)
            ? null
            : command.DisplayName.Trim();
        if (displayName?.Length > MyrmexUser.MaxDisplayNameLength)
        {
            errors.Add(ValidationError(
                "IdentityUser.DisplayNameTooLong",
                $"Display name must be {MyrmexUser.MaxDisplayNameLength} characters or fewer.",
                nameof(Command.DisplayName)));
        }

        string password = command.TemporaryPassword ?? string.Empty;
        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add(ValidationError(
                "IdentityUser.TemporaryPasswordRequired",
                "Temporary password is required.",
                nameof(Command.TemporaryPassword)));
        }

        string[] roles = (command.Roles ?? [])
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (roles.Length == 0)
        {
            errors.Add(ValidationError(
                "IdentityUser.RolesRequired",
                "At least one supported role is required.",
                nameof(Command.Roles)));
        }

        string[] unsupportedRoles = roles
            .Where(role => !SupportedRoles.Contains(role))
            .ToArray();
        if (unsupportedRoles.Length > 0)
        {
            errors.Add(ValidationError(
                "IdentityUser.RoleUnsupported",
                "One or more requested roles are not supported.",
                nameof(Command.Roles)));
        }

        return new ValidationState(email, displayName, password, roles, errors);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            MailAddress address = new(email);
            return string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static ServiceResult<IdentityUserDetails> MapCreateFailure(
        IdentityResult result)
    {
        if (result.Errors.Any(error =>
            string.Equals(error.Code, nameof(IdentityErrorDescriber.DuplicateEmail), StringComparison.Ordinal) ||
            string.Equals(error.Code, nameof(IdentityErrorDescriber.DuplicateUserName), StringComparison.Ordinal)))
        {
            return DuplicateEmail();
        }

        ServiceError[] errors = result.Errors
            .Select(error => ValidationError(
                error.Code,
                error.Description,
                nameof(Command.TemporaryPassword)))
            .ToArray();

        return errors.Length == 0
            ? ServiceResult<IdentityUserDetails>.Fail(ServiceError.Unknown)
            : Invalid(errors);
    }

    private static ServiceResult<IdentityUserDetails> DuplicateEmail() =>
        ServiceResult<IdentityUserDetails>.Fail(new ServiceError(
            ServiceErrorType.Conflict,
            "IdentityUser.Duplicate",
            "An Identity user with this email already exists.",
            nameof(Command.Email)));

    private static ServiceResult<IdentityUserDetails> Invalid(
        IReadOnlyList<ServiceError> errors) =>
        ServiceResult<IdentityUserDetails>.Fail(new ServiceError(
            ServiceErrorType.Invalid,
            "IdentityUser.Validation",
            "One or more validation errors occurred.",
            Details: errors));

    private static ServiceError ValidationError(
        string code,
        string message,
        string property) =>
        new(ServiceErrorType.Invalid, code, message, property);

    private static ServiceResult<IdentityUserDetails> RoleAssignmentFailed() =>
        ServiceResult<IdentityUserDetails>.Fail(new ServiceError(
            ServiceErrorType.Failure,
            "IdentityUser.RoleAssignmentFailed",
            "Identity user could not be assigned to the requested roles."));

    private sealed record ValidationState(
        string Email,
        string? DisplayName,
        string TemporaryPassword,
        IReadOnlyList<string> Roles,
        List<ServiceError> Errors);
}
