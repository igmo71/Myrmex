using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Myrmex.Identity.Persistence;
using System.Net.Mail;

namespace Myrmex.Identity.Application.Bootstrap;

public sealed class InitialAdminOptionsValidator(
    IHostEnvironment environment,
    IOptions<IdentityOptions> identityOptions)
    : IValidateOptions<InitialAdminOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        InitialAdminOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        List<string> failures = [];

        string? email = options.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            failures.Add(
                $"{InitialAdminOptions.SectionName}:Email must be configured " +
                "when initial administrator bootstrap is enabled.");
        }
        else if (!IsValidEmail(email))
        {
            failures.Add(
                $"{InitialAdminOptions.SectionName}:Email must be a valid email address.");
        }

        if (string.IsNullOrEmpty(options.Password))
        {
            failures.Add(
                $"{InitialAdminOptions.SectionName}:Password must be supplied " +
                "from a secret configuration source when initial administrator " +
                "bootstrap is enabled.");
        }
        else
        {
            AddPasswordPolicyFailures(
                options.Password,
                identityOptions.Value.Password,
                failures);
        }

        if (!string.IsNullOrWhiteSpace(options.DisplayName) &&
            options.DisplayName.Trim().Length > MyrmexUser.MaxDisplayNameLength)
        {
            failures.Add(
                $"{InitialAdminOptions.SectionName}:DisplayName must not exceed " +
                $"{MyrmexUser.MaxDisplayNameLength} characters.");
        }

        if (environment.IsProduction() &&
            string.IsNullOrEmpty(options.Password))
        {
            failures.Add(
                $"{InitialAdminOptions.SectionName}:Password is required in Production " +
                "and must come from a deployment secret, user secret, or environment variable.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            MailAddress address = new(email);
            return string.Equals(
                address.Address,
                email,
                StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static void AddPasswordPolicyFailures(
        string password,
        PasswordOptions passwordOptions,
        List<string> failures)
    {
        string key = $"{InitialAdminOptions.SectionName}:Password";

        if (password.Length < passwordOptions.RequiredLength)
        {
            failures.Add(
                $"{key} must be at least {passwordOptions.RequiredLength} characters.");
        }

        if (passwordOptions.RequireDigit && !password.Any(char.IsDigit))
        {
            failures.Add($"{key} must contain a digit.");
        }

        if (passwordOptions.RequireLowercase && !password.Any(char.IsLower))
        {
            failures.Add($"{key} must contain a lowercase character.");
        }

        if (passwordOptions.RequireUppercase && !password.Any(char.IsUpper))
        {
            failures.Add($"{key} must contain an uppercase character.");
        }

        if (passwordOptions.RequireNonAlphanumeric &&
            password.All(char.IsLetterOrDigit))
        {
            failures.Add($"{key} must contain a non-alphanumeric character.");
        }

        if (passwordOptions.RequiredUniqueChars > 1 &&
            password.Distinct().Count() < passwordOptions.RequiredUniqueChars)
        {
            failures.Add(
                $"{key} must contain at least " +
                $"{passwordOptions.RequiredUniqueChars} unique characters.");
        }
    }
}
