using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.DemoData.Configuration;
using Myrmex.Modules.Wms.DemoData.Endpoints;
using Myrmex.Modules.Wms.DemoData.Features;
using Myrmex.Shared.Identity;
using Myrmex.Shared.Wms.DemoData;

namespace Myrmex.Tests.Wms.DemoData;

public sealed class WmsDemoDataAuthorizationTests
{
    [Fact]
    public async Task DemoDataEndpoint_WhenAnonymous_Returns401WithoutDispatch()
    {
        RecordingCommandDispatcher dispatcher = new();
        await using WebApplication app = CreateApp(dispatcher);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpResponseMessage response = await SendSeedAsync(app, cookie: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, dispatcher.CallCount);
    }

    [Fact]
    public async Task DemoDataEndpoint_WhenUnprivileged_Returns403WithoutDispatch()
    {
        RecordingCommandDispatcher dispatcher = new();
        await using WebApplication app = CreateApp(dispatcher);
        await app.StartAsync(TestContext.Current.CancellationToken);

        string cookie = app.Services.CreateApiSessionCookie(roles: []);
        using HttpResponseMessage response = await SendSeedAsync(app, cookie);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, dispatcher.CallCount);
    }

    [Theory]
    [InlineData(IdentityRoleNames.WmsOperator)]
    [InlineData(IdentityRoleNames.MyrmexAdmin)]
    public async Task DemoDataEndpoint_WhenEligibleRole_ReturnsSuccessAndDispatches(
        string role)
    {
        Guid userId = Guid.NewGuid();
        RecordingCommandDispatcher dispatcher = new();
        await using WebApplication app = CreateApp(dispatcher);
        await app.StartAsync(TestContext.Current.CancellationToken);

        string cookie = app.Services.CreateApiSessionCookie([role], userId);
        using HttpResponseMessage response = await SendSeedAsync(app, cookie);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, dispatcher.CallCount);
        Assert.Equal(userId.ToString(), dispatcher.ActorId);
    }

    private static WebApplication CreateApp(
        RecordingCommandDispatcher dispatcher)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<ICommandDispatcher>(dispatcher);
        builder.Services.AddSingleton<IOptions<WmsDemoDataOptions>>(
            Options.Create(new WmsDemoDataOptions
            {
                Enabled = true,
                AllowClear = true,
                ClearConfirmation = "clear"
            }));
        builder.Services.AddTestApiSessionAuthentication();

        WebApplication app = builder.Build();
        app.UseTestApiSessionAuthentication();
        app.MapDemoDataAdminEndpoints();
        return app;
    }

    private static async Task<HttpResponseMessage> SendSeedAsync(
        WebApplication app,
        string? cookie)
    {
        using HttpClient client = CreateClient(app);
        using HttpRequestMessage request = new(
            HttpMethod.Post,
            "/api/admin/demo-data/seed");
        if (cookie is not null)
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        }

        return await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        string address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.Single();
        return new HttpClient { BaseAddress = new Uri(address) };
    }

    private sealed class RecordingCommandDispatcher : ICommandDispatcher
    {
        public int CallCount { get; private set; }

        public string? ActorId { get; private set; }

        public Task<TResult> DispatchAsync<TCommand, TResult>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>
            where TResult : IServiceResult
        {
            CallCount++;
            if (command is SeedWmsDemoData.Command seedCommand)
            {
                ActorId = seedCommand.ActorId;
            }

            object result = ServiceResult<DemoDataOperationResponse>.Success(
                new DemoDataOperationResponse(
                    "seed",
                    DateTimeOffset.Parse("2026-07-09T10:00:00Z"),
                    DateTimeOffset.Parse("2026-07-09T10:01:00Z"),
                    []));
            return Task.FromResult((TResult)result);
        }
    }
}
