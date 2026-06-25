using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AppDispatching.QueryDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Endpoints;
using Myrmex.Modules.Wms.Inventory.Features.InventoryCounts;
using Myrmex.Shared.Wms.Inventory;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
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

    private static WebApplication CreateApp(
        RecordingCommandDispatcher commandDispatcher,
        bool authenticated)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<ICommandDispatcher>(commandDispatcher);
        builder.Services.AddSingleton<IQueryDispatcher>(
            new RecordingQueryDispatcher(CreateDetails()));

        WebApplication app = builder.Build();

        if (authenticated)
        {
            app.Use(async (context, next) =>
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim("sub", "actor-sub")],
                    authenticationType: "Test"));
                await next();
            });
        }

        app.MapGroup("/api/wms/inventory").MapInventoryCountEndpoints();
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
                default:
                    throw new NotSupportedException(typeof(TCommand).FullName);
            }

            return Task.FromResult((TResult)(object)_result);
        }
    }

    private sealed class RecordingQueryDispatcher(InventoryCountDetails details)
        : IQueryDispatcher
    {
        public Task<TResult> DispatchAsync<TQuery, TResult>(
            TQuery query,
            CancellationToken cancellationToken = default)
            where TQuery : IQuery<TResult>
            where TResult : IServiceResult
        {
            return Task.FromResult((TResult)(object)
                ServiceResult<InventoryCountDetails>.Success(details));
        }
    }
}
