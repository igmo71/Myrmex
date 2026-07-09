using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Myrmex.Identity.Persistence;
using Myrmex.Shared.Identity;
using System.Collections.Concurrent;

namespace Myrmex.Identity.Application.Bootstrap;

public sealed class InitialAdminSeeder(
    IOptions<InitialAdminOptions> options,
    MyrmexIdentityDbContext dbContext,
    UserManager<MyrmexUser> userManager,
    IInitialAdminRoleAssigner roleAssigner,
    ILogger<InitialAdminSeeder> logger)
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> EmailLocks =
        new(StringComparer.Ordinal);

    public async Task<InitialAdminBootstrapResult> SeedAsync(
        CancellationToken cancellationToken)
    {
        InitialAdminOptions bootstrap = options.Value;
        if (!bootstrap.Enabled)
        {
            logger.LogInformation("Initial administrator bootstrap is disabled.");
            return new InitialAdminBootstrapResult(
                InitialAdminBootstrapStatus.Disabled,
                UserId: null,
                Email: null);
        }

        string email = bootstrap.Email?.Trim()
            ?? throw new InvalidOperationException(
                "Initial administrator email is required.");
        string password = bootstrap.Password
            ?? throw new InvalidOperationException(
                "Initial administrator password is required.");
        string normalizedEmail = userManager.NormalizeEmail(email);

        SemaphoreSlim emailLock = EmailLocks.GetOrAdd(
            normalizedEmail,
            _ => new SemaphoreSlim(1, 1));

        await emailLock.WaitAsync(cancellationToken);
        try
        {
            return await SeedLockedAsync(
                bootstrap,
                email,
                password,
                cancellationToken);
        }
        finally
        {
            emailLock.Release();
        }
    }

    private async Task<InitialAdminBootstrapResult> SeedLockedAsync(
        InitialAdminOptions bootstrap,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction =
                await dbContext.Database.BeginTransactionAsync(cancellationToken);
            MyrmexUser? createdUser = null;

            try
            {
                MyrmexUser? user = await userManager.FindByEmailAsync(email);
                cancellationToken.ThrowIfCancellationRequested();

                if (user is null)
                {
                    user = new MyrmexUser
                    {
                        Id = Guid.NewGuid(),
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        DisplayName = TrimToNull(bootstrap.DisplayName)
                    };

                    IdentityResult createUser = await userManager.CreateAsync(
                        user,
                        password);
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!createUser.Succeeded)
                    {
                        throw CreateIdentityFailure(
                            "create the initial administrator",
                            createUser);
                    }

                    createdUser = user;

                    IdentityResult addRole = await roleAssigner.AddToRoleAsync(
                        user,
                        IdentityRoleNames.MyrmexAdmin,
                        cancellationToken);

                    if (!addRole.Succeeded)
                    {
                        throw CreateIdentityFailure(
                            "assign the initial administrator role",
                            addRole);
                    }

                    await transaction.CommitAsync(cancellationToken);

                    logger.LogInformation(
                        "Initial administrator {UserId} was created for {Email}.",
                        user.Id,
                        email);

                    return new InitialAdminBootstrapResult(
                        InitialAdminBootstrapStatus.Created,
                        user.Id,
                        email);
                }

                bool isAdmin = await userManager.IsInRoleAsync(
                    user,
                    IdentityRoleNames.MyrmexAdmin);
                cancellationToken.ThrowIfCancellationRequested();

                if (isAdmin)
                {
                    await transaction.CommitAsync(cancellationToken);

                    logger.LogInformation(
                        "Initial administrator {UserId} already exists for {Email}.",
                        user.Id,
                        email);

                    return new InitialAdminBootstrapResult(
                        InitialAdminBootstrapStatus.ExistingUserAlreadyAdmin,
                        user.Id,
                        email);
                }

                IdentityResult assignExisting = await roleAssigner.AddToRoleAsync(
                    user,
                    IdentityRoleNames.MyrmexAdmin,
                    cancellationToken);

                if (!assignExisting.Succeeded)
                {
                    throw CreateIdentityFailure(
                        "assign the initial administrator role",
                        assignExisting);
                }

                await transaction.CommitAsync(cancellationToken);

                logger.LogInformation(
                    "Existing Identity user {UserId} for {Email} was assigned the administrator role.",
                    user.Id,
                    email);

                return new InitialAdminBootstrapResult(
                    InitialAdminBootstrapStatus.ExistingUserAssignedAdminRole,
                    user.Id,
                    email);
            }
            catch
            {
                if (createdUser is not null)
                {
                    await userManager.DeleteAsync(createdUser);
                }

                throw;
            }
        });
    }

    private static string? TrimToNull(string? value)
    {
        string? trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static InvalidOperationException CreateIdentityFailure(
        string action,
        IdentityResult result)
    {
        string errorCodes = string.Join(
            "; ",
            result.Errors.Select(error => error.Code));
        return new InvalidOperationException(
            $"Failed to {action}. Identity error codes: {errorCodes}");
    }
}
