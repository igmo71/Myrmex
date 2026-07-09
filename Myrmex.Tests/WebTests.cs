using Microsoft.Extensions.Logging;

namespace Myrmex.Tests;

public class WebTests
{
    private static readonly TimeSpan AppHostStartupTimeout = TimeSpan.FromMinutes(5);

    [Trait("Category", "Integration")]
    [Trait("Category", "InfrastructureSmoke")]
    [Trait("Category", "AppHostSmoke")]
    [Fact(Explicit = true)]
    public async Task AppHostSmoke_GetWebResourceRootReturnsOkStatusCode()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Myrmex_AppHost>(cancellationToken);
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            // Override the logging filters from the app's configuration
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
            // To output logs to the xUnit.net ITestOutputHelper, consider adding a package from https://www.nuget.org/packages?q=xunit+logging
        });
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(AppHostStartupTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(AppHostStartupTimeout, cancellationToken);

        // Act
        var httpClient = app.CreateHttpClient("webapp");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("webapp", cancellationToken).WaitAsync(AppHostStartupTimeout, cancellationToken);
        var response = await httpClient.GetAsync("/", cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
