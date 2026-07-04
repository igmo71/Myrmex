using Myrmex.Core.Results;
using Myrmex.Modules.Wms.DemoData.Configuration;
using Myrmex.Shared.Wms.DemoData;
using Myrmex.Tests.Wms.DemoData.Testing;
using System.Net.Http.Json;

namespace Myrmex.Tests.Wms.DemoData.Endpoints;

public sealed class DemoDataSafetyEndpointTests
{
    [Fact]
    public async Task Seed_WithoutActor_ReturnsUnauthorizedWithoutDispatch()
    {
        RecordingCommandDispatcher dispatcher = SuccessfulDispatcher();
        await using var app = await DemoDataTestHost.StartAsync(dispatcher, authenticated: false);
        using HttpClient client = app.CreateClient();

        using HttpResponseMessage response = await client.PostAsync(
            "/api/admin/demo-data/seed", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, dispatcher.CallCount);
    }

    [Theory]
    [InlineData(false, "CLEAR-MYRMEX-DEMO", "CLEAR-MYRMEX-DEMO", HttpStatusCode.Forbidden)]
    [InlineData(true, "CLEAR-MYRMEX-DEMO", "wrong", HttpStatusCode.Forbidden)]
    [InlineData(true, "CLEAR-MYRMEX-DEMO", "", HttpStatusCode.BadRequest)]
    [InlineData(true, "", "CLEAR-MYRMEX-DEMO", HttpStatusCode.BadRequest)]
    public async Task Clear_InvalidGuard_ReturnsExpectedStatusWithoutDispatch(
        bool allowClear,
        string configured,
        string supplied,
        HttpStatusCode expected)
    {
        RecordingCommandDispatcher dispatcher = SuccessfulDispatcher();
        var options = new WmsDemoDataOptions
        {
            Enabled = true,
            AllowClear = allowClear,
            ClearConfirmation = configured
        };
        await using var app = await DemoDataTestHost.StartAsync(dispatcher, options);
        using HttpClient client = app.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/admin/demo-data/clear",
            new ClearDemoDataRequest(supplied),
            TestContext.Current.CancellationToken);

        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(0, dispatcher.CallCount);
    }

    private static RecordingCommandDispatcher SuccessfulDispatcher() => new(_ =>
        ServiceResult<DemoDataOperationResponse>.Success(new(
            "seed", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, [])));
}
