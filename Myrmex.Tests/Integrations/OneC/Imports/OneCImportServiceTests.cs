using Microsoft.Extensions.Options;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Integrations.OneC.Configuration;
using Myrmex.Integrations.OneC.Imports;
using Myrmex.Integrations.OneC.Transport;
using Myrmex.Modules.Wms.Catalog.Features.Imports;
using Myrmex.Modules.Wms.Topology.Features.Imports;
using System.Runtime.CompilerServices;

namespace Myrmex.Tests.Integrations.OneC.Imports;

public sealed class OneCImportServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-27T12:00:00Z");

    [Fact]
    public async Task ImportWarehousesAsync_MapsDescriptionFallbackCodeAndCommittedFolderSkip()
    {
        Guid warehouseKey = Guid.NewGuid();
        Guid folderKey = Guid.NewGuid();
        StubODataClient source = new()
        {
            Warehouses =
            [
                new Catalog_Склады { Ref_Key = folderKey, IsFolder = true, Code = "GROUP", Description = "Group" },
                new Catalog_Склады { Ref_Key = warehouseKey, Code = null, Description = " Main Warehouse " }
            ]
        };
        RecordingDispatcher dispatcher = new(new ReferenceImportBatchResult(1, 1, 0, 0, 0, []));
        OneCImportService service = CreateService(source, dispatcher, warehouseCodeAvailable: false);

        var response = await service.ImportWarehousesAsync(TestContext.Current.CancellationToken);

        Assert.True(response.IsComplete);
        Assert.Equal(2, response.Processed);
        Assert.Equal(1, response.Created);
        Assert.Equal(1, response.Skipped);
        Assert.Equal(ReferenceImportRecordErrorReasons.SourceFolder, Assert.Single(response.Errors).Reason);
        ImportWarehouses.Item item = Assert.Single(dispatcher.WarehouseCommand!.Items);
        Assert.Equal(warehouseKey.ToString("N").ToUpperInvariant(), item.Code);
        Assert.Equal("Main Warehouse", item.Name);
    }

    [Fact]
    public async Task ImportUnitsOfMeasureAsync_UsesFullNameAndSymbolWithDescriptionFallbacks()
    {
        StubODataClient source = new()
        {
            UnitsOfMeasure =
            [
                new Catalog_УпаковкиЕдиницыИзмерения
                {
                    Ref_Key = Guid.NewGuid(), Code = " 796 ", Description = "Штука",
                    НаименованиеПолное = " Штука полная ", МеждународноеСокращение = " PCE "
                },
                new Catalog_УпаковкиЕдиницыИзмерения
                {
                    Ref_Key = Guid.NewGuid(), Code = " 166 ", Description = " Килограмм ",
                    НаименованиеПолное = " ", МеждународноеСокращение = null
                }
            ]
        };
        RecordingDispatcher dispatcher = new(new ReferenceImportBatchResult(2, 2, 0, 0, 0, []));
        OneCImportService service = CreateService(source, dispatcher);

        var response = await service.ImportUnitsOfMeasureAsync(TestContext.Current.CancellationToken);

        Assert.True(response.IsComplete);
        Assert.Equal(2, response.Processed);
        ImportUnitsOfMeasure.Item[] items = dispatcher.UnitCommand!.Items.ToArray();
        Assert.Equal("796", items[0].Code);
        Assert.Equal("Штука полная", items[0].Name);
        Assert.Equal("PCE", items[0].Symbol);
        Assert.Equal("Килограмм", items[1].Name);
        Assert.Equal("Килограмм", items[1].Symbol);
    }

    [Fact]
    public async Task ImportWarehousesAsync_WhenBatchFails_DiscardsPendingFolderCounts()
    {
        StubODataClient source = new()
        {
            Warehouses =
            [
                new Catalog_Склады { Ref_Key = Guid.NewGuid(), IsFolder = true, Description = "Group" },
                new Catalog_Склады { Ref_Key = Guid.NewGuid(), Code = "WH", Description = "Warehouse" }
            ]
        };
        RecordingDispatcher dispatcher = new(ServiceResult<ReferenceImportBatchResult>.Fail(
            ServiceError.Failure<OneCImportServiceTests>("Commit failed.")));
        OneCImportService service = CreateService(source, dispatcher);

        var response = await service.ImportWarehousesAsync(TestContext.Current.CancellationToken);

        Assert.False(response.IsComplete);
        Assert.Equal(0, response.Processed);
        Assert.Empty(response.Errors);
        Assert.Equal("BatchCommitFailed", response.OperationError?.Reason);
    }

    [Fact]
    public async Task ImportWarehousesAsync_WhenBatchContainsOnlyFolders_CompletesWithoutDispatch()
    {
        StubODataClient source = new()
        {
            Warehouses =
            [
                new Catalog_Склады { Ref_Key = Guid.NewGuid(), IsFolder = true, Code = "G1", Description = "Group 1" },
                new Catalog_Склады { Ref_Key = Guid.NewGuid(), IsFolder = true, Code = "G2", Description = "Group 2" }
            ]
        };
        RecordingDispatcher dispatcher = new(new ReferenceImportBatchResult(0, 0, 0, 0, 0, []));
        OneCImportService service = CreateService(source, dispatcher);

        var response = await service.ImportWarehousesAsync(TestContext.Current.CancellationToken);

        Assert.True(response.IsComplete);
        Assert.Equal(2, response.Processed);
        Assert.Equal(2, response.Skipped);
        Assert.Equal(2, response.Errors.Count);
        Assert.Null(dispatcher.WarehouseCommand);
    }

    [Fact]
    public async Task ImportUnitsOfMeasureAsync_CapsReturnedErrorsAfterCalculatingCounts()
    {
        StubODataClient source = new()
        {
            UnitsOfMeasure = Enumerable.Range(0, 60)
                .Select(index => new Catalog_УпаковкиЕдиницыИзмерения
                {
                    Ref_Key = Guid.NewGuid(), Code = $"U{index}", Description = $"Unit {index}"
                })
                .ToArray()
        };
        ReferenceImportRecordError[] errors = Enumerable.Range(0, 60)
            .Select(index => new ReferenceImportRecordError(
                Guid.NewGuid(), $"U{index}", ReferenceImportRecordErrorReasons.InvalidSourceRecord, "Invalid."))
            .ToArray();
        RecordingDispatcher dispatcher = new(new ReferenceImportBatchResult(60, 0, 0, 0, 60, errors));
        OneCImportService service = CreateService(source, dispatcher);

        var response = await service.ImportUnitsOfMeasureAsync(TestContext.Current.CancellationToken);

        Assert.Equal(60, response.Processed);
        Assert.Equal(60, response.Failed);
        Assert.Equal(50, response.Errors.Count);
    }

    [Fact]
    public async Task ImportStockKeepingUnitsAsync_MapsNamesFoldersAndPerRecordBaseUnitKeys()
    {
        Guid unitKey = Guid.NewGuid();
        StubODataClient source = new()
        {
            NomenclaturePages =
            [
                [
                    new Catalog_Номенклатура { Ref_Key = Guid.NewGuid(), IsFolder = true, Code = "GROUP" },
                    new Catalog_Номенклатура
                    {
                        Ref_Key = Guid.NewGuid(), Code = " SKU-1 ", Description = "Fallback",
                        НаименованиеПолное = " Full Name ", Артикул = "TRANSPORT-ONLY",
                        ЕдиницаИзмерения_Key = unitKey
                    },
                    new Catalog_Номенклатура
                    {
                        Ref_Key = Guid.NewGuid(), Code = "SKU-2", Description = " Fallback Name ",
                        ЕдиницаИзмерения_Key = null
                    }
                ]
            ]
        };
        SkuDispatcher dispatcher = new();
        OneCImportService service = CreateService(source, dispatcher);

        var response = await service.ImportStockKeepingUnitsAsync(TestContext.Current.CancellationToken);

        Assert.True(response.IsComplete);
        Assert.Equal(3, response.Processed);
        Assert.Equal(2, response.Created);
        Assert.Equal(1, response.Skipped);
        Assert.Equal(ReferenceImportRecordErrorReasons.SourceFolder, Assert.Single(response.Errors).Reason);
        ImportStockKeepingUnits.Item[] items = Assert.Single(dispatcher.Commands).Items.ToArray();
        Assert.Equal("SKU-1", items[0].Code);
        Assert.Equal("Full Name", items[0].Name);
        Assert.Equal(unitKey, items[0].BaseUnitOfMeasureExternalRefKey);
        Assert.Equal("Fallback Name", items[1].Name);
        Assert.Null(items[1].BaseUnitOfMeasureExternalRefKey);
    }

    [Fact]
    public async Task ImportStockKeepingUnitsAsync_ProcessesMoreThanFifteenThousandRecordsInSourceBatches()
    {
        Catalog_Номенклатура[] records = Enumerable.Range(0, 15001)
            .Select(index => new Catalog_Номенклатура
            {
                Ref_Key = Guid.NewGuid(), Code = $"SKU-{index}", Description = $"Item {index}",
                ЕдиницаИзмерения_Key = Guid.NewGuid()
            })
            .ToArray();
        StubODataClient source = new()
        {
            NomenclaturePages = records.Chunk(1000)
                .Select(page => (IReadOnlyList<Catalog_Номенклатура>)page)
                .ToArray()
        };
        SkuDispatcher dispatcher = new();
        OneCImportService service = CreateService(source, dispatcher);

        var response = await service.ImportStockKeepingUnitsAsync(TestContext.Current.CancellationToken);

        Assert.True(response.IsComplete);
        Assert.Equal(15001, response.Processed);
        Assert.Equal(15001, response.Created);
        Assert.Equal(16, dispatcher.Commands.Count);
    }

    [Fact]
    public async Task ImportStockKeepingUnitsAsync_WhenLaterBatchFails_RetainsOnlyPriorCommittedCounts()
    {
        StubODataClient source = new()
        {
            NomenclaturePages =
            [
                [Nomenclature("SKU-1")],
                [Nomenclature("SKU-2"), new Catalog_Номенклатура { Ref_Key = Guid.NewGuid(), IsFolder = true }]
            ]
        };
        SkuDispatcher dispatcher = new(failOnCall: 2);
        OneCImportService service = CreateService(source, dispatcher);

        var response = await service.ImportStockKeepingUnitsAsync(TestContext.Current.CancellationToken);

        Assert.False(response.IsComplete);
        Assert.Equal(1, response.Processed);
        Assert.Equal(1, response.Created);
        Assert.Equal(0, response.Skipped);
        Assert.Equal("BatchCommitFailed", response.OperationError?.Reason);
        Assert.Empty(response.Errors);
    }

    [Fact]
    public async Task ImportStockKeepingUnitsAsync_WhenLaterSourceReadFails_RetainsCommittedCounts()
    {
        StubODataClient source = new()
        {
            NomenclaturePages = [[Nomenclature("SKU-1")]],
            ExceptionAfterPages = new OneCTransportException(
                OneCTransportFailureReason.SourceUnavailable,
                "Source unavailable.")
        };
        SkuDispatcher dispatcher = new();
        OneCImportService service = CreateService(source, dispatcher);

        var response = await service.ImportStockKeepingUnitsAsync(TestContext.Current.CancellationToken);

        Assert.False(response.IsComplete);
        Assert.Equal(1, response.Processed);
        Assert.Equal(1, response.Created);
        Assert.Equal("SourceUnavailable", response.OperationError?.Reason);
    }

    [Fact]
    public async Task ImportStockKeepingUnitsAsync_WhenCancelledAfterCommittedPage_ReturnsIncompleteCommittedCounts()
    {
        using CancellationTokenSource cancellation = new();
        StubODataClient source = new()
        {
            NomenclaturePages = [[Nomenclature("SKU-1")]],
            AfterPages = cancellation.Cancel
        };
        SkuDispatcher dispatcher = new();
        OneCImportService service = CreateService(source, dispatcher);

        var response = await service.ImportStockKeepingUnitsAsync(cancellation.Token);

        Assert.False(response.IsComplete);
        Assert.Equal(1, response.Processed);
        Assert.Equal("Cancelled", response.OperationError?.Reason);
    }

    [Fact]
    public async Task ImportStockKeepingUnitsAsync_CapsErrorsWithoutChangingFailedCount()
    {
        Catalog_Номенклатура[] records = Enumerable.Range(0, 60)
            .Select(index => Nomenclature($"SKU-{index}"))
            .ToArray();
        ReferenceImportRecordError[] recordErrors = records
            .Select(record => new ReferenceImportRecordError(
                record.Ref_Key,
                record.Code,
                ReferenceImportRecordErrorReasons.BaseUnitOfMeasureNotImported,
                "Not imported."))
            .ToArray();
        StubODataClient source = new() { NomenclaturePages = [records] };
        SkuDispatcher dispatcher = new(fixedResult: new ReferenceImportBatchResult(
            60, Created: 0, Updated: 0, Skipped: 0, Failed: 60, Errors: recordErrors));
        OneCImportService service = CreateService(source, dispatcher);

        var response = await service.ImportStockKeepingUnitsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(60, response.Processed);
        Assert.Equal(60, response.Failed);
        Assert.Equal(50, response.Errors.Count);
    }

    private static OneCImportService CreateService(
        StubODataClient source,
        ICommandDispatcher dispatcher,
        bool warehouseCodeAvailable = true)
    {
        OneCOptions options = new()
        {
            Enabled = true,
            BaseUrl = "https://onec.example.test/odata/",
            Username = "operator",
            Password = "secret",
            WarehousesEntitySet = "Catalog_Склады",
            UnitsOfMeasureEntitySet = OneCOptions.DefaultUnitsOfMeasureEntitySet,
            NomenclatureEntitySet = "Catalog_Номенклатура",
            WarehouseCodeAvailable = warehouseCodeAvailable
        };
        return new OneCImportService(source, dispatcher, Options.Create(options), new FixedTimeProvider(Now));
    }

    private sealed class StubODataClient : IOneCODataClient
    {
        public IReadOnlyList<Catalog_Склады> Warehouses { get; init; } = [];
        public IReadOnlyList<Catalog_УпаковкиЕдиницыИзмерения> UnitsOfMeasure { get; init; } = [];
        public IReadOnlyList<IReadOnlyList<Catalog_Номенклатура>> NomenclaturePages { get; init; } = [];
        public Exception? ExceptionAfterPages { get; init; }
        public Action? AfterPages { get; init; }

        public void ValidateConfiguration() { }
        public Task TestConnectionAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<Catalog_Склады>> ReadWarehousesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Warehouses);
        public Task<IReadOnlyList<Catalog_УпаковкиЕдиницыИзмерения>> ReadUnitsOfMeasureAsync(CancellationToken cancellationToken) =>
            Task.FromResult(UnitsOfMeasure);
        public async IAsyncEnumerable<IReadOnlyList<Catalog_Номенклатура>> ReadNomenclaturePagesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (IReadOnlyList<Catalog_Номенклатура> page in NomenclaturePages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return page;
                await Task.Yield();
            }
            AfterPages?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            if (ExceptionAfterPages is not null)
            {
                throw ExceptionAfterPages;
            }
        }
    }

    private sealed class SkuDispatcher(
        int? failOnCall = null,
        ReferenceImportBatchResult? fixedResult = null) : ICommandDispatcher
    {
        public List<ImportStockKeepingUnits.Command> Commands { get; } = [];

        public Task<TResult> DispatchAsync<TCommand, TResult>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>
            where TResult : IServiceResult
        {
            ImportStockKeepingUnits.Command skuCommand = Assert.IsType<ImportStockKeepingUnits.Command>(command);
            Commands.Add(skuCommand);
            ServiceResult<ReferenceImportBatchResult> result = failOnCall == Commands.Count
                ? ServiceResult<ReferenceImportBatchResult>.Fail(
                    ServiceError.Failure<SkuDispatcher>("Commit failed."))
                : ServiceResult<ReferenceImportBatchResult>.Success(
                    fixedResult ?? new ReferenceImportBatchResult(
                        skuCommand.Items.Count,
                        skuCommand.Items.Count,
                        Updated: 0,
                        Skipped: 0,
                        Failed: 0,
                        Errors: []));
            return Task.FromResult((TResult)(object)result);
        }
    }

    private static Catalog_Номенклатура Nomenclature(string code) => new()
    {
        Ref_Key = Guid.NewGuid(),
        Code = code,
        Description = code,
        ЕдиницаИзмерения_Key = Guid.NewGuid()
    };

    private sealed class RecordingDispatcher : ICommandDispatcher
    {
        private readonly ServiceResult<ReferenceImportBatchResult> _result;

        public RecordingDispatcher(ReferenceImportBatchResult result)
            : this(ServiceResult<ReferenceImportBatchResult>.Success(result)) { }

        public RecordingDispatcher(ServiceResult<ReferenceImportBatchResult> result)
        {
            _result = result;
        }

        public ImportWarehouses.Command? WarehouseCommand { get; private set; }
        public ImportUnitsOfMeasure.Command? UnitCommand { get; private set; }

        public Task<TResult> DispatchAsync<TCommand, TResult>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>
            where TResult : IServiceResult
        {
            if (command is ImportWarehouses.Command warehouse)
            {
                WarehouseCommand = warehouse;
            }
            else if (command is ImportUnitsOfMeasure.Command unit)
            {
                UnitCommand = unit;
            }
            else
            {
                throw new NotSupportedException(typeof(TCommand).FullName);
            }
            return Task.FromResult((TResult)(object)_result);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
