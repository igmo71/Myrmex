using Myrmex.Core.Results;
using Myrmex.Modules.Wms.DemoData.Features;
using Myrmex.Shared.Wms.DemoData;
using Myrmex.Tests.Wms.DemoData.Testing;
using System.Text.Json;

namespace Myrmex.Tests.Wms.DemoData.Endpoints;

public sealed class DemoDataSeedEndpointTests
{
    [Fact]
    public async Task Post_UsesAuthenticatedActorAndSerializesSummary()
    {
        DateTimeOffset now = DateTimeOffset.Parse("2026-07-04T09:00:00Z");
        var expected = new DemoDataOperationResponse(
            "seed", now, now.AddSeconds(1),
            [new DemoDataAreaSummary("warehouses", 1, 0, 0, 0)]);
        RecordingCommandDispatcher dispatcher = new(_ =>
            ServiceResult<DemoDataOperationResponse>.Success(expected));
        await using var app = await DemoDataTestHost.StartAsync(dispatcher);
        using HttpClient client = app.CreateClient();

        using HttpResponseMessage response = await client.PostAsync(
            "/api/admin/demo-data/seed", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SeedWmsDemoData.Command command = Assert.IsType<SeedWmsDemoData.Command>(dispatcher.Command);
        Assert.Equal(DemoDataTestHost.ActorId, command.ActorId);
        Assert.True(dispatcher.CancellationToken.CanBeCanceled);
        using JsonDocument json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("seed", json.RootElement.GetProperty("operation").GetString());
        Assert.Equal(1, json.RootElement.GetProperty("areas")[0].GetProperty("created").GetInt32());
    }

    [Fact]
    public async Task Post_MapsServiceFailureToProblemDetails()
    {
        RecordingCommandDispatcher dispatcher = new(_ =>
            ServiceResult<DemoDataOperationResponse>.Fail(WmsDemoDataErrors.OperationInProgress()));
        await using var app = await DemoDataTestHost.StartAsync(dispatcher);
        using HttpClient client = app.CreateClient();

        using HttpResponseMessage response = await client.PostAsync(
            "/api/admin/demo-data/seed", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
