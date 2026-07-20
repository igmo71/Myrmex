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

public sealed class StockKeepingUnitReferenceRepairTests
{
    private static readonly Guid StockKeepingUnitExternalRefKey =
        Guid.Parse("018f0000-0000-7000-8000-000000000911");
    private static readonly Guid UnitOfMeasureExternalRefKey =
        Guid.Parse("018f0000-0000-7000-8000-000000000912");

    [Fact]
    public async Task SynchronizeStockKeepingUnitAsync_RepairsOneUnitOfMeasureAndRetriesOnce()
    {
        RepairODataClient source = new()
        {
            StockKeepingUnit = StockKeepingUnit(),
            UnitOfMeasure = UnitOfMeasure()
        };
        RepairDispatcher dispatcher = new(
            firstStockKeepingUnitResult: BaseUnitOfMeasureFailure(
                ReferenceImportRecordErrorReasons.BaseUnitOfMeasureNotImported),
            secondStockKeepingUnitResult: Applied(),
            unitOfMeasureResult: Applied());
        OneCReferenceSynchronizationService service = CreateService(source, dispatcher);

        ReferenceSynchronizationResult result = await service.SynchronizeStockKeepingUnitAsync(
            StockKeepingUnitExternalRefKey,
            TestContext.Current.CancellationToken);

        Assert.Equal(ReferenceSynchronizationOutcome.Applied, result.Outcome);
        Assert.Equal(1, source.StockKeepingUnitReadCount);
        Assert.Equal(1, source.UnitOfMeasureReadCount);
        Assert.Equal(2, dispatcher.StockKeepingUnitDispatchCount);
        Assert.Equal(1, dispatcher.UnitOfMeasureDispatchCount);
    }

    [Fact]
    public async Task SynchronizeStockKeepingUnitAsync_StopsAfterFailedUnitOfMeasureRepair()
    {
        RepairODataClient source = new()
        {
            StockKeepingUnit = StockKeepingUnit(),
            UnitOfMeasure = UnitOfMeasure()
        };
        RepairDispatcher dispatcher = new(
            firstStockKeepingUnitResult: BaseUnitOfMeasureFailure(
                ReferenceImportRecordErrorReasons.BaseUnitOfMeasureNotImported),
            secondStockKeepingUnitResult: BaseUnitOfMeasureFailure(
                ReferenceImportRecordErrorReasons.BaseUnitOfMeasureInactive),
            unitOfMeasureResult: Applied());
        OneCReferenceSynchronizationService service = CreateService(source, dispatcher);

        ReferenceSynchronizationResult result = await service.SynchronizeStockKeepingUnitAsync(
            StockKeepingUnitExternalRefKey,
            TestContext.Current.CancellationToken);

        Assert.Equal(ReferenceSynchronizationOutcome.PermanentFailure, result.Outcome);
        Assert.Equal(1, source.StockKeepingUnitReadCount);
        Assert.Equal(1, source.UnitOfMeasureReadCount);
        Assert.Equal(2, dispatcher.StockKeepingUnitDispatchCount);
        Assert.Equal(1, dispatcher.UnitOfMeasureDispatchCount);
    }

    private static OneCReferenceSynchronizationService CreateService(
        IOneCODataClient source,
        ICommandDispatcher dispatcher) =>
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
            new OneCImportGate(),
            TimeProvider.System,
            NullLogger<OneCReferenceSynchronizationService>.Instance);

    private static Catalog_Номенклатура StockKeepingUnit() => new()
    {
        Ref_Key = StockKeepingUnitExternalRefKey,
        DataVersion = [1],
        Code = "SKU-001",
        Description = "Stock item",
        НаименованиеПолное = "Stock item",
        ЕдиницаИзмерения_Key = UnitOfMeasureExternalRefKey
    };

    private static Catalog_УпаковкиЕдиницыИзмерения UnitOfMeasure() => new()
    {
        Ref_Key = UnitOfMeasureExternalRefKey,
        DataVersion = [1],
        Code = "EA",
        Description = "Each",
        НаименованиеПолное = "Each",
        МеждународноеСокращение = "ea"
    };

    private static ReferenceImportBatchResult BaseUnitOfMeasureFailure(string reason) => new(
        1,
        0,
        0,
        0,
        0,
        1,
        [new ReferenceImportRecordError(
            StockKeepingUnitExternalRefKey,
            "SKU-001",
            reason,
            "The base unit of measure is missing or inactive.")]);

    private static ReferenceImportBatchResult Applied() => new(1, 1, 0, 0, 0, 0, []);

    private sealed class RepairODataClient : IOneCODataClient
    {
        public Catalog_УпаковкиЕдиницыИзмерения? UnitOfMeasure { get; init; }
        public Catalog_Номенклатура? StockKeepingUnit { get; init; }
        public int UnitOfMeasureReadCount { get; private set; }
        public int StockKeepingUnitReadCount { get; private set; }

        public void ValidateConfiguration() { }
        public Task TestConnectionAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<Catalog_Склады>> ReadWarehousesAsync(
            CancellationToken cancellationToken) =>
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
            CancellationToken cancellationToken) =>
            Task.FromResult<Catalog_Склады?>(null);

        public Task<Catalog_УпаковкиЕдиницыИзмерения?> ReadUnitOfMeasureAsync(
            Guid externalRefKey,
            CancellationToken cancellationToken)
        {
            UnitOfMeasureReadCount++;
            Assert.Equal(UnitOfMeasureExternalRefKey, externalRefKey);
            return Task.FromResult(UnitOfMeasure);
        }

        public Task<Catalog_Номенклатура?> ReadStockKeepingUnitAsync(
            Guid externalRefKey,
            CancellationToken cancellationToken)
        {
            StockKeepingUnitReadCount++;
            Assert.Equal(StockKeepingUnitExternalRefKey, externalRefKey);
            return Task.FromResult(StockKeepingUnit);
        }
    }

    private sealed class RepairDispatcher(
        ReferenceImportBatchResult firstStockKeepingUnitResult,
        ReferenceImportBatchResult secondStockKeepingUnitResult,
        ReferenceImportBatchResult unitOfMeasureResult)
        : ICommandDispatcher
    {
        public int StockKeepingUnitDispatchCount { get; private set; }
        public int UnitOfMeasureDispatchCount { get; private set; }

        public Task<TResult> DispatchAsync<TCommand, TResult>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>
            where TResult : IServiceResult
        {
            ReferenceImportBatchResult batch = command switch
            {
                ImportStockKeepingUnits.Command _ => StockKeepingUnitDispatchCount++ == 0
                    ? firstStockKeepingUnitResult
                    : secondStockKeepingUnitResult,
                ImportUnitsOfMeasure.Command _ => IncrementUnitOfMeasureDispatch(),
                _ => throw new InvalidOperationException(
                    $"Unexpected command type {typeof(TCommand).Name}.")
            };
            ServiceResult<ReferenceImportBatchResult> result =
                ServiceResult<ReferenceImportBatchResult>.Success(batch);
            return Task.FromResult((TResult)(object)result);
        }

        private ReferenceImportBatchResult IncrementUnitOfMeasureDispatch()
        {
            UnitOfMeasureDispatchCount++;
            return unitOfMeasureResult;
        }
    }
}
