using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AppDispatching.QueryDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Endpoints;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryCounts;
using Myrmex.Modules.Wms.Inventory.Features.InventoryCounts;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Inventory;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Myrmex.Tests.Wms.Inventory.Endpoints;

public sealed class InventoryCountEndpointTests
{
    [Fact]
    public async Task CreateInventoryCountAsync_UsesAuthenticatedActorAndSerializesDetails()
    {
        InventoryCountDetails details = CreateDetails();
        RecordingCommandDispatcher dispatcher = new(details);
        await using WebApplication app = CreateApp(dispatcher, authenticated: true);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient client = CreateClient(app);
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                "/api/wms/inventory/counts",
                new CreateInventoryCountRequest(details.Warehouse.Id, "Cycle count"),
                cancellationToken);

            response.EnsureSuccessStatusCode();
            Assert.NotNull(dispatcher.CreateCommand);
            Assert.Equal("actor-sub", dispatcher.CreateCommand.ActorId);
            Assert.Equal(details.Warehouse.Id, dispatcher.CreateCommand.WarehouseId);

            using JsonDocument json = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));
            Assert.Equal(details.Id, json.RootElement.GetProperty("id").GetGuid());
            Assert.Equal(InventoryCountStatusDetails.Draft, json.RootElement.GetProperty("status").GetString());
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task CreateInventoryCountAsync_WhenUnauthenticated_Returns401WithoutDispatch()
    {
        RecordingCommandDispatcher dispatcher = new(CreateDetails());
        await using WebApplication app = CreateApp(dispatcher, authenticated: false);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient client = CreateClient(app);
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                "/api/wms/inventory/counts",
                new CreateInventoryCountRequest(Guid.NewGuid(), null),
                cancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Null(dispatcher.CreateCommand);
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CreateInventoryCountAsync_WhenActorClaimIsMissingOrEmpty_Returns403WithoutDispatch(
        string? actorId)
    {
        RecordingCommandDispatcher dispatcher = new(CreateDetails());
        await using WebApplication app = CreateApp(
            dispatcher,
            authenticated: true,
            actorId: actorId);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient client = CreateClient(app);
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                "/api/wms/inventory/counts",
                new CreateInventoryCountRequest(Guid.NewGuid(), null),
                cancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Null(dispatcher.CreateCommand);
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task AddAndRemoveInventoryCountLineAsync_BindRouteBodyQueryAndActor()
    {
        InventoryCountDetails details = CreateDetails(includeLine: true);
        InventoryCountLineDetails line = Assert.Single(details.Lines);
        RecordingCommandDispatcher dispatcher = new(details);
        await using WebApplication app = CreateApp(dispatcher, authenticated: true);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient client = CreateClient(app);
            using HttpResponseMessage addResponse = await client.PostAsJsonAsync(
                $"/api/wms/inventory/counts/{details.Id}/lines",
                new AddInventoryCountLineRequest(
                    line.Sku.Id,
                    line.StorageLocation.Id,
                    details.CountVersion),
                cancellationToken);
            addResponse.EnsureSuccessStatusCode();
            Assert.NotNull(dispatcher.AddCommand);
            Assert.Equal("actor-sub", dispatcher.AddCommand.ActorId);
            Assert.Equal(details.Id, dispatcher.AddCommand.InventoryCountId);

            using HttpResponseMessage removeResponse = await client.DeleteAsync(
                $"/api/wms/inventory/counts/{details.Id}/lines/{line.Id}" +
                $"?expectedLineVersion={Uri.EscapeDataString(line.LineVersion)}",
                cancellationToken);
            removeResponse.EnsureSuccessStatusCode();
            Assert.NotNull(dispatcher.RemoveCommand);
            Assert.Equal(line.LineVersion, dispatcher.RemoveCommand.ExpectedLineVersion);
            Assert.Equal("actor-sub", dispatcher.RemoveCommand.ActorId);
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task AddInventoryCountLineAsync_WhenConflict_Returns409ProblemDetails()
    {
        InventoryCountDetails details = CreateDetails(includeLine: true);
        RecordingCommandDispatcher dispatcher = new(
            ServiceResult<InventoryCountDetails>.Fail(InventoryCountErrors.DuplicateLine()));
        await using WebApplication app = CreateApp(dispatcher, authenticated: true);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient client = CreateClient(app);
            InventoryCountLineDetails line = Assert.Single(details.Lines);
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                $"/api/wms/inventory/counts/{details.Id}/lines",
                new AddInventoryCountLineRequest(
                    line.Sku.Id,
                    line.StorageLocation.Id,
                    details.CountVersion),
                cancellationToken);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            using JsonDocument json = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));
            Assert.Equal(
                "InventoryCountLine.DuplicateCurrentPair",
                json.RootElement.GetProperty("code").GetString());
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task RecordInventoryCountLineAsync_BindsBodyRouteAndAuthenticatedActor()
    {
        InventoryCountDetails details = CreateDetails(includeLine: true);
        InventoryCountLineDetails line = Assert.Single(details.Lines);
        RecordingCommandDispatcher dispatcher = new(details);
        await using WebApplication app = CreateApp(dispatcher, authenticated: true);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient client = CreateClient(app);
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                $"/api/wms/inventory/counts/{details.Id}/lines/{line.Id}/count",
                new RecordInventoryCountLineRequest(
                    CountedQuantity: 12,
                    Comment: "Two units behind pallet",
                    ExpectedLineVersion: line.LineVersion),
                cancellationToken);

            response.EnsureSuccessStatusCode();
            Assert.NotNull(dispatcher.RecordCommand);
            Assert.Equal(details.Id, dispatcher.RecordCommand.InventoryCountId);
            Assert.Equal(line.Id, dispatcher.RecordCommand.LineId);
            Assert.Equal(12, dispatcher.RecordCommand.CountedQuantity);
            Assert.Equal("Two units behind pallet", dispatcher.RecordCommand.Comment);
            Assert.Equal(line.LineVersion, dispatcher.RecordCommand.ExpectedLineVersion);
            Assert.Equal("actor-sub", dispatcher.RecordCommand.ActorId);

            using JsonDocument json = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));
            Assert.Equal(details.Id, json.RootElement.GetProperty("id").GetGuid());
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task RecordInventoryCountLineAsync_WhenUnauthenticated_Returns401WithoutDispatch()
    {
        InventoryCountDetails details = CreateDetails(includeLine: true);
        InventoryCountLineDetails line = Assert.Single(details.Lines);
        RecordingCommandDispatcher dispatcher = new(details);
        await using WebApplication app = CreateApp(dispatcher, authenticated: false);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient client = CreateClient(app);
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                $"/api/wms/inventory/counts/{details.Id}/lines/{line.Id}/count",
                new RecordInventoryCountLineRequest(12, null, line.LineVersion),
                cancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Null(dispatcher.RecordCommand);
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Theory]
    [InlineData(ServiceErrorType.Invalid, HttpStatusCode.BadRequest)]
    [InlineData(ServiceErrorType.Conflict, HttpStatusCode.Conflict)]
    public async Task RecordInventoryCountLineAsync_WhenDispatchFails_MapsError(
        ServiceErrorType errorType,
        HttpStatusCode expectedStatus)
    {
        InventoryCountDetails details = CreateDetails(includeLine: true);
        InventoryCountLineDetails line = Assert.Single(details.Lines);
        ServiceError error = errorType == ServiceErrorType.Invalid
            ? ServiceError.Validation<InventoryCountDetails>(
                "Counted quantity is invalid.",
                nameof(RecordInventoryCountLineRequest.CountedQuantity))
            : InventoryCountErrors.LineConcurrency(
                nameof(RecordInventoryCountLineRequest.ExpectedLineVersion));
        RecordingCommandDispatcher dispatcher = new(
            ServiceResult<InventoryCountDetails>.Fail(error));
        await using WebApplication app = CreateApp(dispatcher, authenticated: true);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient client = CreateClient(app);
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                $"/api/wms/inventory/counts/{details.Id}/lines/{line.Id}/count",
                new RecordInventoryCountLineRequest(12, null, line.LineVersion),
                cancellationToken);

            Assert.Equal(expectedStatus, response.StatusCode);
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task ApplyAndSupersedeInventoryCountLineAsync_BindRouteBodyAndActor()
    {
        InventoryCountDetails details = CreateDetails(includeLine: true);
        InventoryCountLineDetails line = Assert.Single(details.Lines);
        RecordingCommandDispatcher dispatcher = new(details);
        await using WebApplication app = CreateApp(dispatcher, authenticated: true);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient client = CreateClient(app);
            using HttpResponseMessage applyResponse = await client.PostAsJsonAsync(
                $"/api/wms/inventory/counts/{details.Id}/lines/{line.Id}/apply",
                new ApplyInventoryCountLineRequest(line.LineVersion),
                cancellationToken);
            applyResponse.EnsureSuccessStatusCode();
            Assert.NotNull(dispatcher.ApplyCommand);
            Assert.Equal(line.LineVersion, dispatcher.ApplyCommand.ExpectedLineVersion);
            Assert.Equal("actor-sub", dispatcher.ApplyCommand.ActorId);

            using HttpResponseMessage supersedeResponse = await client.PostAsJsonAsync(
                $"/api/wms/inventory/counts/{details.Id}/lines/{line.Id}/supersede",
                new SupersedeInventoryCountLineRequest(line.LineVersion),
                cancellationToken);
            supersedeResponse.EnsureSuccessStatusCode();
            Assert.NotNull(dispatcher.SupersedeCommand);
            Assert.Equal(line.LineVersion, dispatcher.SupersedeCommand.ExpectedLineVersion);
            Assert.Equal("actor-sub", dispatcher.SupersedeCommand.ActorId);
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task ApplyInventoryCountLineAsync_WhenConflict_Returns409()
    {
        InventoryCountDetails details = CreateDetails(includeLine: true);
        InventoryCountLineDetails line = Assert.Single(details.Lines);
        RecordingCommandDispatcher dispatcher = new(
            ServiceResult<InventoryCountDetails>.Fail(
                InventoryCountErrors.BalanceSnapshotConflict()));
        await using WebApplication app = CreateApp(dispatcher, authenticated: true);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient client = CreateClient(app);
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                $"/api/wms/inventory/counts/{details.Id}/lines/{line.Id}/apply",
                new ApplyInventoryCountLineRequest(line.LineVersion),
                cancellationToken);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Theory]
    [InlineData("apply")]
    [InlineData("supersede")]
    public async Task ApplyOrSupersedeInventoryCountLineAsync_WhenUnauthenticated_Returns401(
        string action)
    {
        InventoryCountDetails details = CreateDetails(includeLine: true);
        InventoryCountLineDetails line = Assert.Single(details.Lines);
        RecordingCommandDispatcher dispatcher = new(details);
        await using WebApplication app = CreateApp(dispatcher, authenticated: false);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient client = CreateClient(app);
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                $"/api/wms/inventory/counts/{details.Id}/lines/{line.Id}/{action}",
                new ApplyInventoryCountLineRequest(line.LineVersion),
                cancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Null(dispatcher.ApplyCommand);
            Assert.Null(dispatcher.SupersedeCommand);
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task CompleteAndCancelInventoryCountAsync_BindRouteBodyAndActor()
    {
        InventoryCountDetails details = CreateDetails();
        RecordingCommandDispatcher dispatcher = new(details);
        await using WebApplication app = CreateApp(dispatcher, authenticated: true);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient client = CreateClient(app);
            using HttpResponseMessage completeResponse = await client.PostAsJsonAsync(
                $"/api/wms/inventory/counts/{details.Id}/complete",
                new ChangeInventoryCountStatusRequest(details.CountVersion),
                cancellationToken);
            completeResponse.EnsureSuccessStatusCode();
            InventoryCountDetails? completeDetails =
                await completeResponse.Content.ReadFromJsonAsync<InventoryCountDetails>(
                    cancellationToken);
            Assert.Equal(details.Id, completeDetails?.Id);
            Assert.NotNull(dispatcher.CompleteCommand);
            Assert.Equal(details.Id, dispatcher.CompleteCommand.InventoryCountId);
            Assert.Equal(details.CountVersion, dispatcher.CompleteCommand.ExpectedCountVersion);
            Assert.Equal("actor-sub", dispatcher.CompleteCommand.ActorId);

            using HttpResponseMessage cancelResponse = await client.PostAsJsonAsync(
                $"/api/wms/inventory/counts/{details.Id}/cancel",
                new ChangeInventoryCountStatusRequest(details.CountVersion),
                cancellationToken);
            cancelResponse.EnsureSuccessStatusCode();
            InventoryCountDetails? cancelDetails =
                await cancelResponse.Content.ReadFromJsonAsync<InventoryCountDetails>(
                    cancellationToken);
            Assert.Equal(details.Id, cancelDetails?.Id);
            Assert.NotNull(dispatcher.CancelCommand);
            Assert.Equal(details.Id, dispatcher.CancelCommand.InventoryCountId);
            Assert.Equal(details.CountVersion, dispatcher.CancelCommand.ExpectedCountVersion);
            Assert.Equal("actor-sub", dispatcher.CancelCommand.ActorId);
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Theory]
    [InlineData("complete")]
    [InlineData("cancel")]
    public async Task CompleteOrCancelInventoryCountAsync_WhenUnauthenticated_Returns401(
        string action)
    {
        InventoryCountDetails details = CreateDetails();
        RecordingCommandDispatcher dispatcher = new(details);
        await using WebApplication app = CreateApp(dispatcher, authenticated: false);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient client = CreateClient(app);
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                $"/api/wms/inventory/counts/{details.Id}/{action}",
                new ChangeInventoryCountStatusRequest(details.CountVersion),
                cancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Null(dispatcher.CompleteCommand);
            Assert.Null(dispatcher.CancelCommand);
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task CompleteInventoryCountAsync_WhenLifecycleConflict_Returns409()
    {
        InventoryCountDetails details = CreateDetails();
        RecordingCommandDispatcher dispatcher = new(
            ServiceResult<InventoryCountDetails>.Fail(
                InventoryCountErrors.InvalidState(
                    "Every current line must be Applied.",
                    nameof(InventoryCountDetails.Status))));
        await using WebApplication app = CreateApp(dispatcher, authenticated: true);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient client = CreateClient(app);
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                $"/api/wms/inventory/counts/{details.Id}/complete",
                new ChangeInventoryCountStatusRequest(details.CountVersion),
                cancellationToken);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task ListInventoryCountsAsync_BindsQueryAndSerializesList()
    {
        InventoryCountDetails details = CreateDetails();
        InventoryCountListItem listItem = CreateListItem(details);
        RecordingQueryDispatcher queryDispatcher = new(details, listItem);
        await using WebApplication app = CreateApp(
            new RecordingCommandDispatcher(details),
            authenticated: true,
            queryDispatcher: queryDispatcher);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient client = CreateClient(app);
            string url =
                $"/api/wms/inventory/counts?skip=3&take=7&sortBy={InventoryCountSortBy.WarehouseCode}" +
                $"&sortDescending=false&warehouseId={details.Warehouse.Id}" +
                $"&status={InventoryCountStatusDetails.Draft}" +
                "&createdFromUtc=2026-06-20T00%3A00%3A00Z" +
                "&createdToUtc=2026-06-21T00%3A00%3A00Z";
            using HttpResponseMessage response = await client.GetAsync(url, cancellationToken);

            response.EnsureSuccessStatusCode();
            ListResult<InventoryCountListItem>? payload =
                await response.Content.ReadFromJsonAsync<ListResult<InventoryCountListItem>>(
                    cancellationToken);
            Assert.Equal(listItem.Id, Assert.Single(payload!.Items).Id);
            Assert.NotNull(queryDispatcher.ListQuery);
            Assert.Equal(3, queryDispatcher.ListQuery.Skip);
            Assert.Equal(7, queryDispatcher.ListQuery.Take);
            Assert.Equal(details.Warehouse.Id, queryDispatcher.ListQuery.WarehouseId);
            Assert.Equal(InventoryCountStatus.Draft, queryDispatcher.ListQuery.Status);
            Assert.True(queryDispatcher.LastCancellationToken.CanBeCanceled);
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task ListInventoryCountsAsync_WhenValidationFails_Returns400()
    {
        InventoryCountDetails details = CreateDetails();
        RecordingQueryDispatcher queryDispatcher = new(
            details,
            ServiceResult<ListResult<InventoryCountListItem>>.Invalid(
                [DomainValidationFailure.Unsupported<ListInventoryCounts.Query>(
                    nameof(ListInventoryCounts.Query.Status))]));
        await using WebApplication app = CreateApp(
            new RecordingCommandDispatcher(details),
            authenticated: true,
            queryDispatcher: queryDispatcher);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient client = CreateClient(app);
            using HttpResponseMessage response = await client.GetAsync(
                "/api/wms/inventory/counts?status=Unknown",
                cancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task GetInventoryCountByIdAsync_RoutesSerializesAndMapsNotFound()
    {
        InventoryCountDetails details = CreateDetails(includeLine: true);
        RecordingQueryDispatcher successQueries = new(details);
        await using WebApplication successApp = CreateApp(
            new RecordingCommandDispatcher(details),
            authenticated: true,
            queryDispatcher: successQueries);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await successApp.StartAsync(cancellationToken);

        try
        {
            using HttpClient client = CreateClient(successApp);
            using HttpResponseMessage response = await client.GetAsync(
                $"/api/wms/inventory/counts/{details.Id}",
                cancellationToken);
            response.EnsureSuccessStatusCode();
            InventoryCountDetails? payload =
                await response.Content.ReadFromJsonAsync<InventoryCountDetails>(
                    cancellationToken);
            Assert.Equal(details.Id, payload?.Id);
            Assert.Single(payload!.Lines);
            Assert.Equal(details.Id, successQueries.DetailsQuery?.InventoryCountId);
        }
        finally
        {
            await successApp.StopAsync(cancellationToken);
        }

        RecordingQueryDispatcher missingQueries = new(
            details,
            ServiceResult<InventoryCountDetails>.Fail(
                ServiceError.NotFound<InventoryCount>(
                    "InventoryCount not found",
                    nameof(GetInventoryCountById.Query.InventoryCountId))));
        await using WebApplication missingApp = CreateApp(
            new RecordingCommandDispatcher(details),
            authenticated: true,
            queryDispatcher: missingQueries);
        await missingApp.StartAsync(cancellationToken);

        try
        {
            using HttpClient client = CreateClient(missingApp);
            using HttpResponseMessage response = await client.GetAsync(
                $"/api/wms/inventory/counts/{Guid.NewGuid()}",
                cancellationToken);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        finally
        {
            await missingApp.StopAsync(cancellationToken);
        }
    }

    private static WebApplication CreateApp(
        RecordingCommandDispatcher commandDispatcher,
        bool authenticated,
        RecordingQueryDispatcher? queryDispatcher = null,
        string? actorId = "actor-sub")
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<ICommandDispatcher>(commandDispatcher);
        builder.Services.AddSingleton<IQueryDispatcher>(
            queryDispatcher ?? new RecordingQueryDispatcher(CreateDetails()));
        builder.Services.AddTestAuthentication(
            authenticated,
            actorId,
            useSubjectClaim: true);

        WebApplication app = builder.Build();
        app.UseTestAuthentication();
        app.MapGroup("/api/wms/inventory")
            .RequireAuthorization(Myrmex.AspNetCore.Security.MyrmexAuthorizationPolicies.WmsOperator)
            .MapInventoryCountEndpoints();
        return app;
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        string address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.Single();

        return new HttpClient { BaseAddress = new Uri(address) };
    }

    private static InventoryCountDetails CreateDetails(bool includeLine = false)
    {
        Guid countId = Guid.Parse("018f0000-0000-7000-8000-000000000001");
        Guid warehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000301");
        IReadOnlyList<InventoryCountLineDetails> lines = includeLine
            ? [CreateLine()]
            : [];

        return new InventoryCountDetails(
            countId,
            "AAAAAAAAB9E=",
            InventoryCountStatusDetails.Draft,
            "Cycle count",
            DateTimeOffset.Parse("2026-06-24T09:00:00Z"),
            UpdatedAtUtc: null,
            CompletedAtUtc: null,
            CancelledAtUtc: null,
            "actor-sub",
            CompletedByActorId: null,
            CancelledByActorId: null,
            new InventoryCountDetails.WarehouseInfo(warehouseId, "MAIN", "Main Warehouse"),
            lines);
    }

    private static InventoryCountLineDetails CreateLine()
    {
        return new InventoryCountLineDetails(
            Guid.Parse("018f0000-0000-7000-8000-000000000101"),
            "AAAAAAAAB9I=",
            InventoryCountLineStatusDetails.Pending,
            IsCurrent: true,
            SystemQuantity: 10,
            CountedQuantity: null,
            VarianceQuantity: null,
            ExpectedBalanceVersion: "AAAAAAAAB9A=",
            Comment: null,
            CountedByActorId: null,
            CountedAtUtc: null,
            AppliedByActorId: null,
            AppliedAtUtc: null,
            AppliedInventoryTransactionId: null,
            SupersedesInventoryCountLineId: null,
            ReplacementInventoryCountLineId: null,
            DateTimeOffset.Parse("2026-06-24T09:05:00Z"),
            UpdatedAtUtc: null,
            new InventoryCountLineDetails.StockKeepingUnitInfo(
                Guid.Parse("018f0000-0000-7000-8000-000000000201"),
                "SKU-001",
                "Widget",
                new InventoryCountLineDetails.UnitOfMeasureInfo(
                    Guid.Parse("018f0000-0000-7000-8000-000000000211"),
                    "EA",
                    "ea")),
            new InventoryCountLineDetails.StorageLocationInfo(
                Guid.Parse("018f0000-0000-7000-8000-000000000301"),
                "A-01-01",
                "A-01-01"));
    }

    private static InventoryCountListItem CreateListItem(InventoryCountDetails details)
    {
        return new InventoryCountListItem(
            details.Id,
            details.CountVersion,
            details.Status,
            details.Reason,
            details.CreatedAtUtc,
            details.UpdatedAtUtc,
            details.CompletedAtUtc,
            details.CancelledAtUtc,
            details.CreatedByActorId,
            details.CompletedByActorId,
            details.CancelledByActorId,
            details.Lines.Count,
            0,
            details.Lines.Count,
            0,
            new InventoryCountListItem.WarehouseInfo(
                details.Warehouse.Id,
                details.Warehouse.Code,
                details.Warehouse.Name));
    }

    private sealed class RecordingCommandDispatcher : ICommandDispatcher
    {
        private readonly ServiceResult<InventoryCountDetails> _result;

        public RecordingCommandDispatcher(InventoryCountDetails details)
            : this(ServiceResult<InventoryCountDetails>.Success(details))
        {
        }

        public RecordingCommandDispatcher(ServiceResult<InventoryCountDetails> result)
        {
            _result = result;
        }

        public CreateInventoryCount.Command? CreateCommand { get; private set; }
        public AddInventoryCountLine.Command? AddCommand { get; private set; }
        public RemoveInventoryCountLine.Command? RemoveCommand { get; private set; }
        public RecordInventoryCountLine.Command? RecordCommand { get; private set; }
        public ApplyInventoryCountLine.Command? ApplyCommand { get; private set; }
        public SupersedeInventoryCountLine.Command? SupersedeCommand { get; private set; }
        public CompleteInventoryCount.Command? CompleteCommand { get; private set; }
        public CancelInventoryCount.Command? CancelCommand { get; private set; }

        public Task<TResult> DispatchAsync<TCommand, TResult>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>
            where TResult : IServiceResult
        {
            switch (command)
            {
                case CreateInventoryCount.Command create:
                    CreateCommand = create;
                    break;
                case AddInventoryCountLine.Command add:
                    AddCommand = add;
                    break;
                case RemoveInventoryCountLine.Command remove:
                    RemoveCommand = remove;
                    break;
                case RecordInventoryCountLine.Command record:
                    RecordCommand = record;
                    break;
                case ApplyInventoryCountLine.Command apply:
                    ApplyCommand = apply;
                    break;
                case SupersedeInventoryCountLine.Command supersede:
                    SupersedeCommand = supersede;
                    break;
                case CompleteInventoryCount.Command complete:
                    CompleteCommand = complete;
                    break;
                case CancelInventoryCount.Command cancel:
                    CancelCommand = cancel;
                    break;
                default:
                    throw new NotSupportedException(typeof(TCommand).FullName);
            }

            return Task.FromResult((TResult)(object)_result);
        }
    }

    private sealed class RecordingQueryDispatcher : IQueryDispatcher
    {
        private readonly ServiceResult<InventoryCountDetails> _detailsResult;
        private readonly ServiceResult<ListResult<InventoryCountListItem>> _listResult;

        public RecordingQueryDispatcher(InventoryCountDetails details)
            : this(details, CreateListItem(details))
        {
        }

        public RecordingQueryDispatcher(
            InventoryCountDetails details,
            InventoryCountListItem listItem)
            : this(
                details,
                ServiceResult<ListResult<InventoryCountListItem>>.Success(
                    new ListResult<InventoryCountListItem>([listItem], 1, 0, 20)))
        {
        }

        public RecordingQueryDispatcher(
            InventoryCountDetails details,
            ServiceResult<ListResult<InventoryCountListItem>> listResult)
        {
            _detailsResult = ServiceResult<InventoryCountDetails>.Success(details);
            _listResult = listResult;
        }

        public RecordingQueryDispatcher(
            InventoryCountDetails details,
            ServiceResult<InventoryCountDetails> detailsResult)
        {
            _detailsResult = detailsResult;
            _listResult = ServiceResult<ListResult<InventoryCountListItem>>.Success(
                new ListResult<InventoryCountListItem>(
                    [CreateListItem(details)],
                    1,
                    0,
                    20));
        }

        public ListInventoryCounts.Query? ListQuery { get; private set; }
        public GetInventoryCountById.Query? DetailsQuery { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public Task<TResult> DispatchAsync<TQuery, TResult>(
            TQuery query,
            CancellationToken cancellationToken = default)
            where TQuery : IQuery<TResult>
            where TResult : IServiceResult
        {
            LastCancellationToken = cancellationToken;
            object result = query switch
            {
                ListInventoryCounts.Query list => CaptureList(list),
                GetInventoryCountById.Query details => CaptureDetails(details),
                _ => throw new NotSupportedException(typeof(TQuery).FullName)
            };
            return Task.FromResult((TResult)result);
        }

        private ServiceResult<ListResult<InventoryCountListItem>> CaptureList(
            ListInventoryCounts.Query query)
        {
            ListQuery = query;
            return _listResult;
        }

        private ServiceResult<InventoryCountDetails> CaptureDetails(
            GetInventoryCountById.Query query)
        {
            DetailsQuery = query;
            return _detailsResult;
        }
    }
}
