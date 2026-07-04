using Myrmex.Core.Results;
using Myrmex.Modules.Wms.DemoData.Features;
using Myrmex.Shared.Wms.DemoData;
using Myrmex.Tests.Wms.DemoData.Testing;
using System.Net.Http.Json;

namespace Myrmex.Tests.Wms.DemoData.Endpoints;

public sealed class DemoDataClearEndpointTests
{
    [Fact]
    public async Task Post_BindsJsonAndDispatchesAuthenticatedActor()
    {
        DateTimeOffset now = DateTimeOffset.Parse("2026-07-04T09:00:00Z");
        RecordingCommandDispatcher dispatcher = new(_ =>
            ServiceResult<DemoDataOperationResponse>.Success(new(
                "clear", now, now, [new("warehouses", 0, 0, 0, 1)])));
        await using var app = await DemoDataTestHost.StartAsync(dispatcher);
        using HttpClient client = app.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/admin/demo-data/clear",
            new ClearDemoDataRequest("CLEAR-MYRMEX-DEMO"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ClearWmsDemoData.Command command = Assert.IsType<ClearWmsDemoData.Command>(dispatcher.Command);
        Assert.Equal(DemoDataTestHost.ActorId, command.ActorId);
        Assert.Equal("CLEAR-MYRMEX-DEMO", command.Confirmation);
        Assert.True(dispatcher.CancellationToken.CanBeCanceled);
    }
}
