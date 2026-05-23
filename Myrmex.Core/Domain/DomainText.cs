namespace Myrmex.Core.Domain;

public static class DomainText
{
    public static string NormalizeCode(string? code)
    {
        return NormalizeRequiredText(code).ToUpperInvariant();
    }

    public static string NormalizeRequiredText(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    public static string? NormalizeOptionalText(string? value)
    {
        string? normalized = value?.Trim();

        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}