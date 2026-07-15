using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Myrmex.AspNetCore.Security;
using Myrmex.Identity.Infrastructure;
using Myrmex.Integrations.OneC;

namespace Myrmex.Tests.Integrations.Authorization;

public sealed class IntegrationApiKeyAuthenticationTests
{
    [Fact]
    public async Task AddOneCIntegration_PreservesApiSessionAuthenticationDefaults()
    {
        IConfiguration configuration = CreateConfiguration();
        ServiceCollection services = new();
        services.AddMyrmexIdentityApiAuthentication(configuration);
        services.AddOneCIntegration(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        AuthenticationOptions options = provider
            .GetRequiredService<IOptions<AuthenticationOptions>>()
            .Value;
        IAuthenticationSchemeProvider schemes = provider
            .GetRequiredService<IAuthenticationSchemeProvider>();

        Assert.Equal(MyrmexAuthenticationSchemes.ApiSession, options.DefaultScheme);
        Assert.Equal(MyrmexAuthenticationSchemes.ApiSession, options.DefaultAuthenticateScheme);
        Assert.Equal(MyrmexAuthenticationSchemes.ApiSession, options.DefaultChallengeScheme);
        Assert.Equal(MyrmexAuthenticationSchemes.ApiSession, options.DefaultForbidScheme);
        Assert.Equal(MyrmexAuthenticationSchemes.ApiSession, options.DefaultSignInScheme);
        Assert.Equal(MyrmexAuthenticationSchemes.ApiSession, options.DefaultSignOutScheme);
        Assert.NotNull(await schemes.GetSchemeAsync(
            MyrmexAuthenticationSchemes.IntegrationApiKey));
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MyrmexDatabase"] = "Server=(localdb)\\MSSQLLocalDB;Database=MyrmexTests;Trusted_Connection=True",
                ["Myrmex:Identity:ApiSession:LifetimeMinutes"] = "2",
                ["Myrmex:Integrations:OneC:SourceSystem"] = "OneC",
                ["Myrmex:Integrations:OneC:SourceInstance"] = "main-infobase",
                ["Myrmex:Integrations:OneC:ApiKey"] = "development-only-key"
            })
            .Build();
}
