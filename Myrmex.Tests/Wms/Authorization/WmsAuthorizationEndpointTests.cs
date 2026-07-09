using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AppDispatching.QueryDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Topology.Endpoints;
using Myrmex.Modules.Wms.Topology.Features.Warehouses;
using Myrmex.Shared.Common;
using Myrmex.Shared.Identity;
using Myrmex.Shared.Wms.Topology;

namespace Myrmex.Tests.Wms.Authorization;

public sealed class WmsAuthorizationEndpointTests
{
    [Fact]
    public async Task WmsEndpoint_WhenAnonymous_Returns401WithoutDispatch()
    {
        RecordingQueryDispatcher dispatcher = new();
        await using WebApplication app = CreateApp(dispatcher);
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpResponseMessage response = await SendLookupAsync(app, cookie: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, dispatcher.CallCount);
    }

    [Fact]
    public async Task WmsEndpoint_WhenUnprivileged_Returns403WithoutDispatch()
    {
        RecordingQueryDispatcher dispatcher = new();
        await using WebApplication app = CreateApp(dispatcher);
        await app.StartAsync(TestContext.Current.CancellationToken);

        string cookie = app.Services.CreateApiSessionCookie(roles: []);
        using HttpResponseMessage response = await SendLookupAsync(app, cookie);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, dispatcher.CallCount);
    }

    [Theory]
    [InlineData(IdentityRoleNames.WmsOperator)]
    [InlineData(IdentityRoleNames.MyrmexAdmin)]
    public async Task WmsEndpoint_WhenEligibleRole_ReturnsSuccessAndDispatches(
        string role)
    {
        RecordingQueryDispatcher dispatcher = new();
        await using WebApplication app = CreateApp(dispatcher);
        await app.StartAsync(TestContext.Current.CancellationToken);

        string cookie = app.Services.CreateApiSessionCookie([role]);
        using HttpResponseMessage response = await SendLookupAsync(app, cookie);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, dispatcher.CallCount);
    }

    private static WebApplication CreateApp(
        RecordingQueryDispatcher dispatcher)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<IQueryDispatcher>(dispatcher);
        builder.Services.AddSingleton<ICommandDispatcher, UnsupportedCommandDispatcher>();
        builder.Services.AddTestApiSessionAuthentication();

        WebApplication app = builder.Build();
        app.UseTestApiSessionAuthentication();
        app.MapTopologyEndpoints();
        return app;
    }

    private static async Task<HttpResponseMessage> SendLookupAsync(
        WebApplication app,
        string? cookie)
    {
        using HttpClient client = CreateClient(app);
        using HttpRequestMessage request = new(
            HttpMethod.Get,
            "/api/wms/topology/warehouses/lookup");
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

    private sealed class RecordingQueryDispatcher : IQueryDispatcher
    {
        public int CallCount { get; private set; }

        public Task<TResult> DispatchAsync<TQuery, TResult>(
            TQuery query,
            CancellationToken cancellationToken = default)
            where TQuery : IQuery<TResult>
            where TResult : IServiceResult
        {
            CallCount++;
            object result = ServiceResult<IReadOnlyList<WarehouseLookupItem>>
                .Success(
                [
                    new WarehouseLookupItem(
                        Guid.Parse("018f0000-0000-7000-8000-000000000101"),
                        "WH-A",
                        "Warehouse A",
                        true)
                ]);
            return Task.FromResult((TResult)result);
        }
    }

    private sealed class UnsupportedCommandDispatcher : ICommandDispatcher
    {
        public Task<TResult> DispatchAsync<TCommand, TResult>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>
            where TResult : IServiceResult =>
            throw new NotSupportedException(
                "Commands are not used by WMS authorization endpoint tests.");
    }
}
