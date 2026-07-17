using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Integrations.OneC.Configuration;
using Myrmex.Integrations.OneC.Imports;
using Myrmex.Integrations.OneC.References;
using Myrmex.Integrations.OneC.Transport;
using Myrmex.Modules.Wms.Catalog.Features.Imports;
using System.Runtime.CompilerServices;

namespace Myrmex.Tests.Integrations.OneC.References;

public sealed class OneCReferenceSynchronizationServiceTests
{
    private static readonly Guid ExternalRefKey =
        Guid.Parse("018f0000-0000-7000-8000-000000000999");

    [Theory]
    [InlineData("applied", ReferenceSynchronizationOutcome.Applied, false)]
    [InlineData("unchanged", ReferenceSynchronizationOutcome.Unchanged, false)]
    [InlineData("warehouse-folder", ReferenceSynchronizationOutcome.ControlledSkip, false)]
    [InlineData("sku-folder", ReferenceSynchronizationOutcome.ControlledSkip, false)]
    [InlineData("unlinked-deletion", ReferenceSynchronizationOutcome.ControlledSkip, false)]
    [InlineData("not-found", ReferenceSynchronizationOutcome.NotFound, false)]
    [InlineData("busy", ReferenceSynchronizationOutcome.Busy, true)]
    [InlineData("transient", ReferenceSynchronizationOutcome.TransientFailure, true)]
    [InlineData("timeout", ReferenceSynchronizationOutcome.TransientFailure, true)]
    [InlineData("permanent", ReferenceSynchronizationOutcome.PermanentFailure, false)]
    internal async Task SynchronizeOneAsync_ReturnsTheNarrowOperationOutcome(
        string scenario,
        ReferenceSynchronizationOutcome expectedOutcome,
        bool retrySuitable)
    {
        StubODataClient source = new()
        {
            Warehouse = scenario == "not-found"
                ? null
                : Warehouse(
                    isFolder: scenario == "warehouse-folder",
                    isDeletionMarked: scenario == "unlinked-deletion"),
            StockKeepingUnit = StockKeepingUnit(isFolder: scenario == "sku-folder"),
            Exception = scenario switch
            {
                "transient" => new OneCTransportException(
                    OneCTransportFailureReason.SourceUnavailable,
                    "Unavailable."),
                "timeout" => new OneCTransportException(
                    OneCTransportFailureReason.Timeout,
                    "Timed out."),
                "permanent" => new OneCTransportException(
                    OneCTransportFailureReason.MalformedResponse,
                    "Malformed."),
                _ => null
            }
        };
        RecordingDispatcher dispatcher = new(Batch(scenario));
        OneCImportGate gate = new();
        using IDisposable? heldLease = scenario == "busy"
            ? gate.Acquire(OneCImportGate.Warehouses)
            : null;
        OneCReferenceSynchronizationService service = CreateService(source, dispatcher, gate);

        ReferenceSynchronizationResult result = scenario == "sku-folder"
            ? await service.SynchronizeStockKeepingUnitAsync(
                ExternalRefKey,
                TestContext.Current.CancellationToken)
            : await service.SynchronizeWarehouseAsync(
                ExternalRefKey,
                TestContext.Current.CancellationToken);

        Assert.Equal(expectedOutcome, result.Outcome);
        Assert.Equal(ExternalRefKey, result.ExternalRefKey);
        Assert.Equal(retrySuitable, result.RetrySuitable);
        bool shouldDispatch = scenario is "applied" or "unchanged" or "unlinked-deletion";
        Assert.Equal(shouldDispatch ? 1 : 0, dispatcher.CallCount);
        Assert.Equal(scenario == "busy" ? 0 : 1, source.CurrentReadCount);
    }

    private static OneCReferenceSynchronizationService CreateService(
        IOneCODataClient source,
        ICommandDispatcher dispatcher,
        OneCImportGate gate) =>
        new(
            source,
            dispatcher,
            Options.Create(new OneCOptions
            {
                Enabled = true,
                BaseUrl = "https://onec.example.test/odata/",
                Username = "operator",
                Password = "secret",
                WarehousesEntitySet = "Catalog_Warehouses",
                UnitsOfMeasureEntitySet = OneCOptions.DefaultUnitsOfMeasureEntitySet,
                NomenclatureEntitySet = "Catalog_Nomenclature"
            }),
            gate,
            TimeProvider.System,
            NullLogger<OneCReferenceSynchronizationService>.Instance);

    private static ReferenceImportBatchResult Batch(string scenario) => scenario switch
    {
        "unchanged" => new(1, 0, 0, 1, 0, 0, []),
        "unlinked-deletion" => new(
            1,
            0,
            0,
            0,
            1,
            0,
            [new ReferenceImportRecordError(
                ExternalRefKey,
                "WH",
                ReferenceImportRecordErrorReasons.SourceRecordDeletionMarked,
                "Skipped.")]),
        _ => new(1, 1, 0, 0, 0, 0, [])
    };

    private static Catalog_Склады Warehouse(bool isFolder, bool isDeletionMarked) => new()
    {
        Ref_Key = ExternalRefKey,
        DataVersion = [1],
        IsFolder = isFolder,
        Code = "WH",
        Description = "Warehouse",
        DeletionMark = isDeletionMarked
    };

    private static Catalog_Номенклатура StockKeepingUnit(bool isFolder) => new()
    {
        Ref_Key = ExternalRefKey,
        DataVersion = [1],
        IsFolder = isFolder,
        Code = "SKU",
        Description = "Stock item",
        ЕдиницаИзмерения_Key = Guid.NewGuid()
    };

    private sealed class StubODataClient : IOneCODataClient
    {
        public Catalog_Склады? Warehouse { get; init; }
        public Catalog_УпаковкиЕдиницыИзмерения? UnitOfMeasure { get; init; }
        public Catalog_Номенклатура? StockKeepingUnit { get; init; }
        public Exception? Exception { get; init; }
        public int CurrentReadCount { get; private set; }

        public void ValidateConfiguration() { }
        public Task TestConnectionAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<Catalog_Склады>> ReadWarehousesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Catalog_Склады>>([]);
        public Task<IReadOnlyList<Catalog_УпаковкиЕдиницыИзмерения>> ReadUnitsOfMeasureAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Catalog_УпаковкиЕдиницыИзмерения>>([]);
        public async IAsyncEnumerable<IReadOnlyList<Catalog_Номенклатура>> ReadNomenclaturePagesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }

        public Task<Catalog_Склады?> ReadWarehouseAsync(
            Guid externalRefKey,
            CancellationToken cancellationToken) => Return(Warehouse);
        public Task<Catalog_УпаковкиЕдиницыИзмерения?> ReadUnitOfMeasureAsync(
            Guid externalRefKey,
            CancellationToken cancellationToken) => Return(UnitOfMeasure);
        public Task<Catalog_Номенклатура?> ReadStockKeepingUnitAsync(
            Guid externalRefKey,
            CancellationToken cancellationToken) => Return(StockKeepingUnit);

        private Task<T?> Return<T>(T? value) where T : class
        {
            CurrentReadCount++;
            return Exception is null ? Task.FromResult(value) : Task.FromException<T?>(Exception);
        }
    }

    private sealed class RecordingDispatcher(ReferenceImportBatchResult batch) : ICommandDispatcher
    {
        public int CallCount { get; private set; }

        public Task<TResult> DispatchAsync<TCommand, TResult>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>
            where TResult : IServiceResult
        {
            CallCount++;
            ServiceResult<ReferenceImportBatchResult> result =
                ServiceResult<ReferenceImportBatchResult>.Success(batch);
            return Task.FromResult((TResult)(object)result);
        }
    }
}
