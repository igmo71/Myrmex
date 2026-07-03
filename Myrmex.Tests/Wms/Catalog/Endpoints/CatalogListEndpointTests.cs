using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AppDispatching.QueryDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Endpoints;
using Myrmex.Modules.Wms.Catalog.Features.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Features.UnitsOfMeasure;
using Myrmex.Shared.Common;
using Myrmex.Shared.Wms.Catalog;
using System.Text.Json;

namespace Myrmex.Tests.Wms.Catalog.Endpoints;

public sealed class CatalogListEndpointTests
{
    [Fact]
    public async Task ListStockKeepingUnitsAsync_BindsFeatureRequestAndSerializesSharedDetails()
    {
        StockKeepingUnitDetails details = new(
            Guid.Parse("018f0000-0000-7000-8000-000000000001"),
            "ITEM-001",
            "Widget",
            "Sellable widget",
            Guid.Parse("018f0000-0000-7000-8000-000000000111"),
            true,
            DateTimeOffset.Parse("2026-06-10T00:00:00Z"),
            DateTimeOffset.Parse("2026-06-11T00:00:00Z"));
        RecordingQueryDispatcher dispatcher = new(details);
        await using WebApplication app = CreateCatalogEndpointApp(dispatcher);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient client = CreateHttpClient(app);
            using HttpResponseMessage response = await client.GetAsync(
                "/api/wms/catalog/skus?skip=7&take=13&searchText=widget" +
                $"&sortBy={StockKeepingUnitSortBy.CreatedAtUtc}" +
                "&sortDescending=true&includeInactive=true",
                cancellationToken);

            response.EnsureSuccessStatusCode();
            ListStockKeepingUnits.Query query = Assert.IsType<ListStockKeepingUnits.Query>(
                dispatcher.CapturedQuery);
            Assert.Equal(7, query.Skip);
            Assert.Equal(13, query.Take);
            Assert.Equal("widget", query.SearchText);
            Assert.Equal(StockKeepingUnitSortBy.CreatedAtUtc, query.SortBy);
            Assert.True(query.SortDescending);
            Assert.True(query.IncludeInactive);

            using JsonDocument json = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));
            Assert.Equal(details.Id, json.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
            Assert.Equal(details.BaseUnitOfMeasureId, json.RootElement.GetProperty("items")[0]
                .GetProperty("baseUnitOfMeasureId").GetGuid());
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task ListUnitsOfMeasureAsync_BindsFeatureRequestAndSerializesSharedDetails()
    {
        UnitOfMeasureDetails details = new(
            Guid.Parse("018f0000-0000-7000-8000-000000000002"),
            "EA",
            "Each",
            "ea",
            true,
            DateTimeOffset.Parse("2026-06-09T00:00:00Z"),
            null);
        RecordingQueryDispatcher dispatcher = new(details);
        await using WebApplication app = CreateCatalogEndpointApp(dispatcher);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await app.StartAsync(cancellationToken);

        try
        {
            using HttpClient client = CreateHttpClient(app);
            using HttpResponseMessage response = await client.GetAsync(
                "/api/wms/catalog/uoms?skip=3&take=25&searchText=each" +
                $"&sortBy={UnitOfMeasureSortBy.UpdatedAtUtc}" +
                "&sortDescending=false&includeInactive=false",
                cancellationToken);

            response.EnsureSuccessStatusCode();
            ListUnitsOfMeasure.Query query = Assert.IsType<ListUnitsOfMeasure.Query>(
                dispatcher.CapturedQuery);
            Assert.Equal(3, query.Skip);
            Assert.Equal(25, query.Take);
            Assert.Equal("each", query.SearchText);
            Assert.Equal(UnitOfMeasureSortBy.UpdatedAtUtc, query.SortBy);
            Assert.False(query.SortDescending);
            Assert.False(query.IncludeInactive);

            using JsonDocument json = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));
            Assert.Equal("EA", json.RootElement.GetProperty("items")[0].GetProperty("code").GetString());
            Assert.Equal("ea", json.RootElement.GetProperty("items")[0].GetProperty("symbol").GetString());
        }
        finally
        {
            await app.StopAsync(cancellationToken);
        }
    }

    private static WebApplication CreateCatalogEndpointApp(RecordingQueryDispatcher dispatcher)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<IQueryDispatcher>(dispatcher);
        builder.Services.AddSingleton<ICommandDispatcher, UnsupportedCommandDispatcher>();

        WebApplication app = builder.Build();
        var group = app.MapGroup("/api/wms/catalog");
        group.MapStockKeepingUnitEndpoints();
        group.MapUnitOfMeasureEndpoints();

        return app;
    }

    private static HttpClient CreateHttpClient(WebApplication app)
    {
        string address = app.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>()!
            .Addresses
            .Single();

        return new HttpClient { BaseAddress = new Uri(address) };
    }

    private sealed class RecordingQueryDispatcher
        : IQueryDispatcher
    {
        private readonly StockKeepingUnitDetails? _stockKeepingUnit;
        private readonly UnitOfMeasureDetails? _unitOfMeasure;

        public RecordingQueryDispatcher(StockKeepingUnitDetails stockKeepingUnit)
        {
            _stockKeepingUnit = stockKeepingUnit;
        }

        public RecordingQueryDispatcher(UnitOfMeasureDetails unitOfMeasure)
        {
            _unitOfMeasure = unitOfMeasure;
        }

        public object? CapturedQuery { get; private set; }

        public Task<TResult> DispatchAsync<TQuery, TResult>(
            TQuery query,
            CancellationToken cancellationToken = default)
            where TQuery : IQuery<TResult>
            where TResult : IServiceResult
        {
            CapturedQuery = query;

            if (query is ListStockKeepingUnits.Query skuQuery &&
                typeof(TResult) == typeof(ServiceResult<ListResult<StockKeepingUnitDetails>>))
            {
                ServiceResult<ListResult<StockKeepingUnitDetails>> result =
                    ServiceResult<ListResult<StockKeepingUnitDetails>>.Success(
                        new ListResult<StockKeepingUnitDetails>(
                            [_stockKeepingUnit!],
                            1,
                            skuQuery.Skip,
                            skuQuery.Take));
                return Task.FromResult((TResult)(object)result);
            }

            if (query is ListUnitsOfMeasure.Query uomQuery &&
                typeof(TResult) == typeof(ServiceResult<ListResult<UnitOfMeasureDetails>>))
            {
                ServiceResult<ListResult<UnitOfMeasureDetails>> result =
                    ServiceResult<ListResult<UnitOfMeasureDetails>>.Success(
                        new ListResult<UnitOfMeasureDetails>(
                            [_unitOfMeasure!],
                            1,
                            uomQuery.Skip,
                            uomQuery.Take));
                return Task.FromResult((TResult)(object)result);
            }

            throw new NotSupportedException($"Unexpected query type {typeof(TQuery).FullName}.");
        }
    }

    private sealed class UnsupportedCommandDispatcher : ICommandDispatcher
    {
        public Task<TResult> DispatchAsync<TCommand, TResult>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>
            where TResult : IServiceResult
        {
            throw new NotSupportedException("Commands are not used by Catalog list endpoint tests.");
        }
    }
}
