using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Integrations.OneC.Common.Imports;
using Myrmex.Integrations.OneC.Common.References;
using Myrmex.Integrations.OneC.Common.Transport;
using Myrmex.Integrations.OneC.Configuration;
using Myrmex.Integrations.OneC.StockKeepingUnits;
using Myrmex.Integrations.OneC.UnitsOfMeasure;
using Myrmex.Integrations.OneC.Warehouses;
using Myrmex.Modules.Wms.Catalog.Features.Imports;
using Myrmex.Modules.Wms.Topology.Features.Imports;
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
        Exception? exception = scenario switch
        {
            "transient" => new OneCTransportException(
                OneCTransportFailureReason.SourceUnavailable, "Unavailable."),
            "timeout" => new OneCTransportException(
                OneCTransportFailureReason.Timeout, "Timed out."),
            "permanent" => new OneCTransportException(
                OneCTransportFailureReason.MalformedResponse, "Malformed."),
            _ => null
        };
        RecordingDispatcher dispatcher = new(Batch(scenario));
        OneCImportGate gate = new();
        using IDisposable? heldLease = scenario == "busy"
            ? gate.Acquire(OneCImportGate.Warehouses)
            : null;

        ReferenceSynchronizationResult result;
        int currentReadCount;
        if (scenario == "sku-folder")
        {
            StubStockKeepingUnitSource source = new(
                StockKeepingUnit(isFolder: true),
                exception);
            StockKeepingUnitOneCSynchronizer synchronizer = new(
                source,
                new UnusedUnitOfMeasureSynchronizer(),
                dispatcher,
                gate,
                TimeProvider.System,
                NullLogger<StockKeepingUnitOneCSynchronizer>.Instance);
            result = await synchronizer.SynchronizeAsync(
                ExternalRefKey,
                TestContext.Current.CancellationToken);
            currentReadCount = source.CurrentReadCount;
        }
        else
        {
            StubWarehouseSource source = new(
                scenario == "not-found" ? null : Warehouse(
                    isFolder: scenario == "warehouse-folder",
                    isDeletionMarked: scenario == "unlinked-deletion"),
                exception);
            WarehouseOneCSynchronizer synchronizer = CreateWarehouseSynchronizer(
                source,
                dispatcher,
                gate);
            result = await synchronizer.SynchronizeAsync(
                ExternalRefKey,
                TestContext.Current.CancellationToken);
            currentReadCount = source.CurrentReadCount;
        }

        Assert.Equal(expectedOutcome, result.Outcome);
        Assert.Equal(ExternalRefKey, result.ExternalRefKey);
        Assert.Equal(retrySuitable, result.RetrySuitable);
        bool shouldDispatch = scenario is "applied" or "unchanged" or "unlinked-deletion";
        Assert.Equal(shouldDispatch ? 1 : 0, dispatcher.CallCount);
        Assert.Equal(scenario == "busy" ? 0 : 1, currentReadCount);
    }

    [Fact]
    public async Task UnitOfMeasureSynchronizer_MapsAndDispatchesWithoutFolderBehavior()
    {
        StubUnitOfMeasureSource source = new(UnitOfMeasure());
        RecordingDispatcher dispatcher = new(Batch("applied"));
        UnitOfMeasureOneCSynchronizer synchronizer = new(
            source,
            dispatcher,
            new OneCImportGate(),
            TimeProvider.System,
            NullLogger<UnitOfMeasureOneCSynchronizer>.Instance);

        ReferenceSynchronizationResult result = await synchronizer.SynchronizeAsync(
            ExternalRefKey,
            TestContext.Current.CancellationToken);

        Assert.Equal(ReferenceSynchronizationOutcome.Applied, result.Outcome);
        Assert.Equal(1, source.CurrentReadCount);
        Assert.IsType<ImportUnitsOfMeasure.Command>(dispatcher.LastCommand);
    }

    [Fact]
    public async Task SynchronizeAsync_PropagatesCallerCancellation()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        StubWarehouseSource source = new(
            Warehouse(isFolder: false, isDeletionMarked: false),
            exception: null);
        WarehouseOneCSynchronizer synchronizer = CreateWarehouseSynchronizer(
            source,
            new RecordingDispatcher(Batch("applied")),
            new OneCImportGate());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            synchronizer.SynchronizeAsync(ExternalRefKey, cancellation.Token));
    }

    private static WarehouseOneCSynchronizer CreateWarehouseSynchronizer(
        IWarehouseOneCSource source,
        ICommandDispatcher dispatcher,
        OneCImportGate gate) =>
        new(
            source,
            dispatcher,
            Options.Create(OptionsValue()),
            gate,
            TimeProvider.System,
            NullLogger<WarehouseOneCSynchronizer>.Instance);

    private static OneCOptions OptionsValue() => new()
    {
        Enabled = true,
        BaseUrl = "https://onec.example.test/odata/",
        Username = "operator",
        Password = "secret",
        WarehousesEntitySet = "Catalog_Warehouses",
        UnitsOfMeasureEntitySet = OneCOptions.DefaultUnitsOfMeasureEntitySet,
        NomenclatureEntitySet = "Catalog_Nomenclature"
    };

    private static ReferenceImportBatchResult Batch(string scenario) => scenario switch
    {
        "unchanged" => new(1, 0, 0, 1, 0, 0, []),
        "unlinked-deletion" => new(
            1, 0, 0, 0, 1, 0,
            [new ReferenceImportRecordError(
                ExternalRefKey,
                "WH",
                ReferenceImportRecordErrorReasons.SourceRecordDeletionMarked,
                "Skipped.")]),
        _ => new(1, 1, 0, 0, 0, 0, [])
    };

    private static WarehouseSourceRecord Warehouse(bool isFolder, bool isDeletionMarked) => new()
    {
        Ref_Key = ExternalRefKey,
        DataVersion = [1],
        IsFolder = isFolder,
        Code = "WH",
        Description = "Warehouse",
        DeletionMark = isDeletionMarked
    };

    private static StockKeepingUnitSourceRecord StockKeepingUnit(bool isFolder) => new()
    {
        Ref_Key = ExternalRefKey,
        DataVersion = [1],
        IsFolder = isFolder,
        Code = "SKU",
        Description = "Stock item",
        ЕдиницаИзмерения_Key = Guid.NewGuid()
    };

    private static UnitOfMeasureSourceRecord UnitOfMeasure() => new()
    {
        Ref_Key = ExternalRefKey,
        DataVersion = [1],
        Code = "EA",
        Description = "Each",
        НаименованиеПолное = "Each",
        МеждународноеСокращение = "ea"
    };

    private sealed class StubWarehouseSource(
        WarehouseSourceRecord? record,
        Exception? exception) : IWarehouseOneCSource
    {
        public int CurrentReadCount { get; private set; }

        public Task<IReadOnlyList<WarehouseSourceRecord>> ReadAllAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<WarehouseSourceRecord>>([]);

        public Task<WarehouseSourceRecord?> ReadCurrentAsync(
            Guid externalRefKey,
            CancellationToken cancellationToken)
        {
            CurrentReadCount++;
            Assert.Equal(ExternalRefKey, externalRefKey);
            return exception is null
                ? Task.FromResult(record)
                : Task.FromException<WarehouseSourceRecord?>(exception);
        }

        public Task ProbeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubUnitOfMeasureSource(UnitOfMeasureSourceRecord record)
        : IUnitOfMeasureOneCSource
    {
        public int CurrentReadCount { get; private set; }

        public Task<IReadOnlyList<UnitOfMeasureSourceRecord>> ReadAllAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<UnitOfMeasureSourceRecord>>([]);

        public Task<UnitOfMeasureSourceRecord?> ReadCurrentAsync(
            Guid externalRefKey,
            CancellationToken cancellationToken)
        {
            CurrentReadCount++;
            return Task.FromResult<UnitOfMeasureSourceRecord?>(record);
        }

        public Task ProbeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubStockKeepingUnitSource(
        StockKeepingUnitSourceRecord record,
        Exception? exception) : IStockKeepingUnitOneCSource
    {
        public int CurrentReadCount { get; private set; }

        public async IAsyncEnumerable<IReadOnlyList<StockKeepingUnitSourceRecord>> ReadPagesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }

        public Task<StockKeepingUnitSourceRecord?> ReadCurrentAsync(
            Guid externalRefKey,
            CancellationToken cancellationToken)
        {
            CurrentReadCount++;
            return exception is null
                ? Task.FromResult<StockKeepingUnitSourceRecord?>(record)
                : Task.FromException<StockKeepingUnitSourceRecord?>(exception);
        }

        public Task ProbeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class UnusedUnitOfMeasureSynchronizer : IUnitOfMeasureOneCSynchronizer
    {
        public Task<ReferenceSynchronizationResult> SynchronizeAsync(
            Guid externalRefKey,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Folder handling must not invoke dependency repair.");
    }

    private sealed class RecordingDispatcher(ReferenceImportBatchResult batch) : ICommandDispatcher
    {
        public int CallCount { get; private set; }
        public object? LastCommand { get; private set; }

        public Task<TResult> DispatchAsync<TCommand, TResult>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>
            where TResult : IServiceResult
        {
            CallCount++;
            LastCommand = command;
            ServiceResult<ReferenceImportBatchResult> result =
                ServiceResult<ReferenceImportBatchResult>.Success(batch);
            return Task.FromResult((TResult)(object)result);
        }
    }
}
