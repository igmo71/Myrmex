using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Myrmex.Identity.Persistence;
using Myrmex.Shared.Identity;
using System.Collections.Concurrent;

namespace Myrmex.Identity.Application.Bootstrap;

public sealed class IdentityRoleInitializer(
    RoleManager<MyrmexRole> roleManager,
    ILogger<IdentityRoleInitializer> logger)
    : IIdentityRoleInitializer
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> RoleLocks =
        new(StringComparer.Ordinal);

    private static readonly string[] SupportedRoles =
    [
        IdentityRoleNames.MyrmexAdmin,
        IdentityRoleNames.WmsOperator
    ];

    public async Task EnsureRolesAsync(CancellationToken cancellationToken)
    {
        foreach (string roleName in SupportedRoles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureRoleAsync(roleName, cancellationToken);
        }
    }

    private async Task EnsureRoleAsync(
        string roleName,
        CancellationToken cancellationToken)
    {
        SemaphoreSlim roleLock = RoleLocks.GetOrAdd(
            roleName,
            _ => new SemaphoreSlim(1, 1));

        await roleLock.WaitAsync(cancellationToken);
        try
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                logger.LogDebug(
                    "Identity role {RoleName} already exists.",
                    roleName);
                return;
            }

            IdentityResult result = await roleManager.CreateAsync(
                new MyrmexRole(roleName));

            if (result.Succeeded ||
                await roleManager.RoleExistsAsync(roleName))
            {
                logger.LogInformation(
                    "Identity role {RoleName} is available.",
                    roleName);
                return;
            }

            throw new InvalidOperationException(
                $"Failed to create Identity role '{roleName}': " +
                string.Join(
                    "; ",
                    result.Errors.Select(error => error.Code)));
        }
        finally
        {
            roleLock.Release();
        }
    }
}
