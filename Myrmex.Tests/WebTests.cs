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
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        const string sourceInstanceVariable = "Myrmex__Integrations__OneC__SourceInstance";
        const string apiKeyVariable = "Myrmex__Integrations__OneC__ApiKey";

        string? previousSourceInstance = Environment.GetEnvironmentVariable(sourceInstanceVariable);
        string? previousApiKey = Environment.GetEnvironmentVariable(apiKeyVariable);

        try
        {
            Environment.SetEnvironmentVariable(sourceInstanceVariable, "apphost-smoke-test");
            Environment.SetEnvironmentVariable(apiKeyVariable, "apphost-smoke-test-key");

            var appHost = await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.Myrmex_AppHost>(cancellationToken);

            appHost.Services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
                logging.AddFilter("Aspire.", LogLevel.Debug);
            });

            appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
            {
                clientBuilder.AddStandardResilienceHandler();
            });

            await using var app =
                await appHost.BuildAsync(cancellationToken)
                    .WaitAsync(AppHostStartupTimeout, cancellationToken);

            await app.StartAsync(cancellationToken)
                .WaitAsync(AppHostStartupTimeout, cancellationToken);

            await app.ResourceNotifications
                .WaitForResourceHealthyAsync("apiservice", cancellationToken)
                .WaitAsync(AppHostStartupTimeout, cancellationToken);

            await app.ResourceNotifications
                .WaitForResourceHealthyAsync("webapp", cancellationToken)
                .WaitAsync(AppHostStartupTimeout, cancellationToken);

            using HttpClient httpClient = app.CreateHttpClient("webapp");

            using HttpResponseMessage response = await httpClient.GetAsync("/", cancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            Environment.SetEnvironmentVariable(sourceInstanceVariable, previousSourceInstance);
            Environment.SetEnvironmentVariable(apiKeyVariable, previousApiKey);
        }
    }
}
