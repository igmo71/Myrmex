namespace Myrmex.Tests.Identity;

public sealed class IdentitySessionArchitectureTests
{
    [Fact]
    public void ProductionSessionBoundary_ContainsNoForbiddenIdentityTransport()
    {
        string root = FindRepositoryRoot();
        string handler = File.ReadAllText(Path.Combine(
            root,
            "Myrmex.WebApp",
            "Identity",
            "IdentityApiAuthenticationHandler.cs"));
        string issuer = File.ReadAllText(Path.Combine(
            root,
            "Myrmex.Identity",
            "Infrastructure",
            "Sessions",
            "IdentityApiSessionTicketIssuer.cs"));
        string clientFactory = File.ReadAllText(Path.Combine(
            root,
            "Myrmex.WebApp",
            "Identity",
            "ProtectedApiClientFactory.cs"));
        string apiProgram = File.ReadAllText(Path.Combine(
            root,
            "Myrmex.ApiService",
            "Program.cs"));
        string boundary = string.Join(
            Environment.NewLine,
            handler,
            issuer,
            clientFactory,
            apiProgram);

        Assert.DoesNotContain("IHttpContextAccessor", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("Request.Cookies", boundary, StringComparison.Ordinal);
        Assert.DoesNotContain("AddJwtBearer", boundary, StringComparison.Ordinal);
        Assert.DoesNotContain("JwtBearer", boundary, StringComparison.Ordinal);
        Assert.DoesNotContain("X-User-Id", boundary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("X-Role", boundary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("X-Actor", boundary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AllowAnonymous", apiProgram, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null &&
            !File.Exists(Path.Combine(directory.FullName, "Myrmex.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException(
            "Could not locate the Myrmex repository root.");
    }
}
