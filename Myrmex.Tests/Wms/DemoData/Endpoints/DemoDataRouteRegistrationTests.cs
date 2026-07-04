using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms;
using Myrmex.Modules.Wms.DemoData.Configuration;
using Myrmex.Shared.Wms.DemoData;
using Myrmex.Tests.Wms.DemoData.Testing;

namespace Myrmex.Tests.Wms.DemoData.Endpoints;

public sealed class DemoDataRouteRegistrationTests
{
    [Theory]
    [InlineData(false, "Development", HttpStatusCode.NotFound)]
    [InlineData(true, "Production", HttpStatusCode.NotFound)]
    [InlineData(true, "Development", HttpStatusCode.Unauthorized)]
    public async Task MapWmsModule_RegistersDemoRoutesOnlyWhenAllowed(
        bool enabled,
        string environment,
        HttpStatusCode expected)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.WebHost.UseEnvironment(environment);
        builder.Services.AddSingleton<IOptions<WmsDemoDataOptions>>(
            Options.Create(new WmsDemoDataOptions { Enabled = enabled }));
        builder.Services.AddSingleton<ICommandDispatcher>(new RecordingCommandDispatcher(_ =>
            ServiceResult<DemoDataOperationResponse>.Success(new(
                "seed", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, []))));
        await using WebApplication app = builder.Build();
        app.MapWmsModule();
        await app.StartAsync(TestContext.Current.CancellationToken);

        try
        {
            string address = app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using HttpClient client = new() { BaseAddress = new Uri(address) };
            using HttpResponseMessage response = await client.PostAsync(
                "/api/admin/demo-data/seed", null, TestContext.Current.CancellationToken);
            Assert.Equal(expected, response.StatusCode);
        }
        finally
        {
            await app.StopAsync(TestContext.Current.CancellationToken);
        }
    }
}
