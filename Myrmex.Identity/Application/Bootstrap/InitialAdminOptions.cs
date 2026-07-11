namespace Myrmex.Identity.Application.Bootstrap;

public sealed class InitialAdminOptions
{
    public const string SectionName = "Myrmex:Identity:InitialAdmin";

    public bool Enabled { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? DisplayName { get; set; }
}
