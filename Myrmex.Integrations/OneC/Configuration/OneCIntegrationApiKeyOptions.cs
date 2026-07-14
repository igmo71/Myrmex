using Microsoft.Extensions.Options;

namespace Myrmex.Integrations.OneC.Configuration;

internal sealed class OneCIntegrationApiKeyOptions
{
    public const string SectionName = "Myrmex:Integrations:OneC:ApiKey";
    public const string DefaultSourceSystem = "OneC";
    public const int SourceSystemMaxLength = 32;
    public const int SourceInstanceMaxLength = 128;

    public string SourceSystem { get; set; } = DefaultSourceSystem;

    public string? SourceInstance { get; set; }

    public string? ApiKey { get; set; }
}

internal sealed class OneCIntegrationApiKeyOptionsValidator
    : IValidateOptions<OneCIntegrationApiKeyOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        OneCIntegrationApiKeyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        AddRequiredBoundedFailure(
            failures,
            nameof(OneCIntegrationApiKeyOptions.SourceSystem),
            options.SourceSystem,
            OneCIntegrationApiKeyOptions.SourceSystemMaxLength);
        AddRequiredBoundedFailure(
            failures,
            nameof(OneCIntegrationApiKeyOptions.SourceInstance),
            options.SourceInstance,
            OneCIntegrationApiKeyOptions.SourceInstanceMaxLength);

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            failures.Add(
                $"{OneCIntegrationApiKeyOptions.SectionName}:ApiKey must be configured.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void AddRequiredBoundedFailure(
        List<string> failures,
        string propertyName,
        string? value,
        int maximumLength)
    {
        string key = $"{OneCIntegrationApiKeyOptions.SectionName}:{propertyName}";

        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{key} must be configured.");
            return;
        }

        if (value.Length > maximumLength)
        {
            failures.Add($"{key} must not exceed {maximumLength} characters.");
        }
    }
}
