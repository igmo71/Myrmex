using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Integrations.OneC.Configuration;
using Myrmex.Integrations.OneC.Imports;
using Myrmex.Integrations.OneC.Transport;
using Myrmex.Modules.Wms.Catalog.Features.Imports;
using Myrmex.Modules.Wms.Topology.Features.Imports;
using Myrmex.Shared.Integrations.OneC;
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
                new Catalog_Склады { Ref_Key = warehouseKey, DataVersion = [1, 2], Code = null, Description = " Main Warehouse " }
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
        Assert.Equal(new byte[] { 1, 2 }, item.ExternalDataVersion);
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
                    Ref_Key = Guid.NewGuid(), DataVersion = [2, 3], Code = " 796 ", Description = "Штука",
                    НаименованиеПолное = " Штука полная ", МеждународноеСокращение = " PCE "
                },
                new Catalog_УпаковкиЕдиницыИзмерения
                {
                    Ref_Key = Guid.NewGuid(), DataVersion = [3, 4], Code = " 166 ", Description = " Килограмм ",
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
        Assert.Equal(new byte[] { 2, 3 }, items[0].ExternalDataVersion);
        Assert.Equal("Килограмм", items[1].Name);
        Assert.Equal("Килограмм", items[1].Symbol);
    }

    [Fact]
    public async Task ImportWarehousesAsync_WhenRepeatedWithSameDataVersion_ReportsUnchanged()
    {
        Guid warehouseKey = Guid.NewGuid();
        StubODataClient source = new()
        {
            Warehouses =
            [
                new Catalog_Склады
                {
                    Ref_Key = warehouseKey,
                    DataVersion = [9, 8, 7],
                    Code = "WH-1",
                    Description = "Warehouse"
                }
            ]
        };
        VersionAwareWarehouseDispatcher dispatcher = new();
        OneCImportService service = CreateService(source, dispatcher);

        OneCImportResponse first = await service.ImportWarehousesAsync(
            TestContext.Current.CancellationToken);
        OneCImportResponse repeated = await service.ImportWarehousesAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(1, first.Created);
        Assert.Equal(0, first.Unchanged);
        Assert.Equal(1, repeated.Processed);
        Assert.Equal(0, repeated.Updated);
        Assert.Equal(1, repeated.Unchanged);
        Assert.Equal(new byte[] { 9, 8, 7 }, dispatcher.LastDataVersion);
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
                        Ref_Key = Guid.NewGuid(), DataVersion = [4, 5], Code = " SKU-1 ", Description = "Fallback",
                        НаименованиеПолное = " Full Name ", Артикул = "TRANSPORT-ONLY",
                        ЕдиницаИзмерения_Key = unitKey
                    },
                    new Catalog_Номенклатура
                    {
                        Ref_Key = Guid.NewGuid(), DataVersion = [5, 6], Code = "SKU-2", Description = " Fallback Name ",
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
        Assert.Equal(new byte[] { 4, 5 }, items[0].ExternalDataVersion);
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
        SkuDispatcher dispatcher = new(
            failOnCall: 2,
            fixedResult: new ReferenceImportBatchResult(
                1,
                Created: 0,
                Updated: 0,
                Unchanged: 1,
                Skipped: 0,
                Failed: 0,
                Errors: []));
        OneCImportService service = CreateService(source, dispatcher);

        var response = await service.ImportStockKeepingUnitsAsync(TestContext.Current.CancellationToken);

        Assert.False(response.IsComplete);
        Assert.Equal(1, response.Processed);
        Assert.Equal(0, response.Created);
        Assert.Equal(1, response.Unchanged);
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

    [Fact]
    public async Task ImportWarehousesAsync_WhenSourceFails_DoesNotExposeCredentialsInOperationOrLogState()
    {
        const string username = "credential-user-sentinel";
        const string password = "credential-password-sentinel";
        StubODataClient source = new()
        {
            WarehouseException = new OneCTransportException(
                OneCTransportFailureReason.SourceUnavailable,
                $"Unsafe upstream detail containing {username} and {password}.")
        };
        RecordingLogger<OneCImportService> logger = new();
        OneCImportService service = CreateService(
            source,
            new RecordingDispatcher(new ReferenceImportBatchResult(0, 0, 0, 0, 0, [])),
            logger: logger,
            username: username,
            password: password);

        OneCImportResponse response = await service.ImportWarehousesAsync(
            TestContext.Current.CancellationToken);

        Assert.False(response.IsComplete);
        OneCImportOperationError operationError = Assert.IsType<OneCImportOperationError>(response.OperationError);
        Assert.Equal("SourceUnavailable", operationError.Reason);
        Assert.DoesNotContain(username, operationError.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(password, operationError.Message, StringComparison.Ordinal);
        string structuredState = logger.StructuredState;
        Assert.DoesNotContain(username, structuredState, StringComparison.Ordinal);
        Assert.DoesNotContain(password, structuredState, StringComparison.Ordinal);
        Assert.Contains("ReferenceType=warehouses", structuredState, StringComparison.Ordinal);
        Assert.Contains("FailureCategory=SourceUnavailable", structuredState, StringComparison.Ordinal);
        Assert.Contains("Processed=0", structuredState, StringComparison.Ordinal);
        Assert.Contains("DurationMilliseconds=", structuredState, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportWarehousesAsync_LogsCompletionCountsWithoutSourcePayload()
    {
        const string sourcePayload = "source-payload-sentinel";
        StubODataClient source = new()
        {
            Warehouses =
            [
                new Catalog_Склады
                {
                    Ref_Key = Guid.NewGuid(),
                    Code = "LOG-WH",
                    Description = sourcePayload
                }
            ]
        };
        RecordingLogger<OneCImportService> logger = new();
        OneCImportService service = CreateService(
            source,
            new RecordingDispatcher(new ReferenceImportBatchResult(1, 1, 0, 0, 0, [])),
            logger: logger);

        OneCImportResponse response = await service.ImportWarehousesAsync(
            TestContext.Current.CancellationToken);

        Assert.True(response.IsComplete);
        string structuredState = logger.StructuredState;
        Assert.Contains("ReferenceType=warehouses", structuredState, StringComparison.Ordinal);
        Assert.Contains("Processed=1", structuredState, StringComparison.Ordinal);
        Assert.Contains("Created=1", structuredState, StringComparison.Ordinal);
        Assert.Contains("Updated=0", structuredState, StringComparison.Ordinal);
        Assert.Contains("Unchanged=0", structuredState, StringComparison.Ordinal);
        Assert.Contains("Skipped=0", structuredState, StringComparison.Ordinal);
        Assert.Contains("Failed=0", structuredState, StringComparison.Ordinal);
        Assert.DoesNotContain(sourcePayload, structuredState, StringComparison.Ordinal);
        Assert.DoesNotContain("LOG-WH", structuredState, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportStockKeepingUnitsAsync_RejectsSameTypeWithoutWaiting_AllowsOtherTypes_AndReleasesGate()
    {
        StubODataClient source = new()
        {
            NomenclaturePages =
            [
                [Nomenclature("SKU-1")],
                [Nomenclature("SKU-2")]
            ]
        };
        OneCImportGate gate = new();
        BlockingSecondSkuDispatcher dispatcher = new();
        OneCImportService service = CreateService(source, dispatcher, importGate: gate);

        Task<OneCImportResponse> running = service.ImportStockKeepingUnitsAsync(
            TestContext.Current.CancellationToken);
        await dispatcher.SecondCallStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        using CancellationTokenSource duplicateCancellation = new();
        duplicateCancellation.Cancel();

        await Assert.ThrowsAsync<OneCImportAlreadyInProgressException>(() =>
            service.ImportStockKeepingUnitsAsync(duplicateCancellation.Token));
        Assert.Equal(1, source.NomenclatureReadCount);
        Assert.Null(gate.TryAcquire(OneCImportGate.StockKeepingUnits));

        using IDisposable? otherTypeLease = gate.TryAcquire(OneCImportGate.Warehouses);
        Assert.NotNull(otherTypeLease);
        otherTypeLease!.Dispose();
        OneCImportResponse otherType = await service.ImportWarehousesAsync(
            TestContext.Current.CancellationToken);
        Assert.True(otherType.IsComplete);

        dispatcher.ReleaseSecondCall.SetResult(true);
        Assert.True((await running).IsComplete);
        Assert.Equal(2, dispatcher.CallCount);
        Assert.True((await service.ImportStockKeepingUnitsAsync(
            TestContext.Current.CancellationToken)).IsComplete);
        Assert.Equal(2, source.NomenclatureReadCount);
    }

    [Fact]
    public async Task ImportStockKeepingUnitsAsync_WhenCancelled_ReleasesGateInFinally()
    {
        TaskCompletionSource<bool> started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StubODataClient source = new()
        {
            NomenclatureReadStarted = started,
            NomenclatureReadRelease = release.Task
        };
        OneCImportService service = CreateService(
            source,
            new RecordingDispatcher(new ReferenceImportBatchResult(0, 0, 0, 0, 0, [])),
            importGate: new OneCImportGate());
        using CancellationTokenSource cancellation = new();

        Task<OneCImportResponse> running = service.ImportStockKeepingUnitsAsync(cancellation.Token);
        await started.Task.WaitAsync(TestContext.Current.CancellationToken);
        cancellation.Cancel();

        OneCImportResponse cancelled = await running;
        Assert.False(cancelled.IsComplete);
        Assert.Equal("Cancelled", cancelled.OperationError?.Reason);

        release.SetResult(true);
        OneCImportResponse retry = await service.ImportStockKeepingUnitsAsync(
            TestContext.Current.CancellationToken);
        Assert.True(retry.IsComplete);
        Assert.Equal(2, source.NomenclatureReadCount);
    }

    [Fact]
    public async Task ImportStockKeepingUnitsAsync_RetryAfterPartialFailureRevisitsCommittedIdentityWithoutDuplicate()
    {
        Guid firstExternalRefKey = Guid.NewGuid();
        Guid secondExternalRefKey = Guid.NewGuid();
        StubODataClient source = new()
        {
            NomenclaturePages =
            [
                [Nomenclature("SKU-1", firstExternalRefKey)],
                [Nomenclature("SKU-2", secondExternalRefKey)]
            ]
        };
        IdempotentSkuDispatcher dispatcher = new(failOnceOnCall: 2);
        OneCImportService service = CreateService(source, dispatcher);

        OneCImportResponse partial = await service.ImportStockKeepingUnitsAsync(
            TestContext.Current.CancellationToken);
        OneCImportResponse retry = await service.ImportStockKeepingUnitsAsync(
            TestContext.Current.CancellationToken);

        Assert.False(partial.IsComplete);
        Assert.Equal(1, partial.Processed);
        Assert.Equal(1, partial.Created);
        Assert.True(retry.IsComplete);
        Assert.Equal(2, retry.Processed);
        Assert.Equal(1, retry.Created);
        Assert.Equal(1, retry.Updated);
        Assert.Equal(2, dispatcher.ExternalRefKeys.Count);
    }

    private static OneCImportService CreateService(
        StubODataClient source,
        ICommandDispatcher dispatcher,
        bool warehouseCodeAvailable = true,
        OneCImportGate? importGate = null,
        ILogger<OneCImportService>? logger = null,
        string username = "operator",
        string password = "secret")
    {
        OneCOptions options = new()
        {
            Enabled = true,
            BaseUrl = "https://onec.example.test/odata/",
            Username = username,
            Password = password,
            WarehousesEntitySet = "Catalog_Склады",
            UnitsOfMeasureEntitySet = OneCOptions.DefaultUnitsOfMeasureEntitySet,
            NomenclatureEntitySet = "Catalog_Номенклатура",
            WarehouseCodeAvailable = warehouseCodeAvailable
        };
        return new OneCImportService(
            source,
            dispatcher,
            Options.Create(options),
            importGate ?? new OneCImportGate(),
            new FixedTimeProvider(Now),
            logger ?? NullLogger<OneCImportService>.Instance);
    }

    private sealed class StubODataClient : IOneCODataClient
    {
        private int _nomenclatureReadCount;

        public IReadOnlyList<Catalog_Склады> Warehouses { get; init; } = [];
        public IReadOnlyList<Catalog_УпаковкиЕдиницыИзмерения> UnitsOfMeasure { get; init; } = [];
        public IReadOnlyList<IReadOnlyList<Catalog_Номенклатура>> NomenclaturePages { get; init; } = [];
        public Exception? WarehouseException { get; init; }
        public Exception? ExceptionAfterPages { get; init; }
        public Action? AfterPages { get; init; }
        public TaskCompletionSource<bool>? NomenclatureReadStarted { get; init; }
        public Task? NomenclatureReadRelease { get; init; }
        public int NomenclatureReadCount => Volatile.Read(ref _nomenclatureReadCount);

        public void ValidateConfiguration() { }
        public Task TestConnectionAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<Catalog_Склады>> ReadWarehousesAsync(CancellationToken cancellationToken) =>
            WarehouseException is null
                ? Task.FromResult(Warehouses)
                : Task.FromException<IReadOnlyList<Catalog_Склады>>(WarehouseException);
        public Task<Catalog_Склады?> ReadWarehouseAsync(
            Guid externalRefKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(Warehouses.SingleOrDefault(record => record.Ref_Key == externalRefKey));
        public Task<IReadOnlyList<Catalog_УпаковкиЕдиницыИзмерения>> ReadUnitsOfMeasureAsync(CancellationToken cancellationToken) =>
            Task.FromResult(UnitsOfMeasure);
        public Task<Catalog_УпаковкиЕдиницыИзмерения?> ReadUnitOfMeasureAsync(
            Guid externalRefKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(UnitsOfMeasure.SingleOrDefault(record => record.Ref_Key == externalRefKey));
        public async IAsyncEnumerable<IReadOnlyList<Catalog_Номенклатура>> ReadNomenclaturePagesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _nomenclatureReadCount);
            NomenclatureReadStarted?.TrySetResult(true);
            if (NomenclatureReadRelease is not null)
            {
                await NomenclatureReadRelease.WaitAsync(cancellationToken);
            }

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
        public Task<Catalog_Номенклатура?> ReadStockKeepingUnitAsync(
            Guid externalRefKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(NomenclaturePages.SelectMany(page => page)
                .SingleOrDefault(record => record.Ref_Key == externalRefKey));
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

    private sealed class BlockingSecondSkuDispatcher : ICommandDispatcher
    {
        public TaskCompletionSource<bool> SecondCallStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> ReleaseSecondCall { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int CallCount { get; private set; }

        public async Task<TResult> DispatchAsync<TCommand, TResult>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>
            where TResult : IServiceResult
        {
            ImportStockKeepingUnits.Command skuCommand = Assert.IsType<ImportStockKeepingUnits.Command>(command);
            CallCount++;
            if (CallCount == 2)
            {
                SecondCallStarted.SetResult(true);
                await ReleaseSecondCall.Task.WaitAsync(cancellationToken);
            }

            ServiceResult<ReferenceImportBatchResult> result =
                ServiceResult<ReferenceImportBatchResult>.Success(
                    new ReferenceImportBatchResult(
                        skuCommand.Items.Count,
                        skuCommand.Items.Count,
                        Updated: 0,
                        Skipped: 0,
                        Failed: 0,
                        Errors: []));
            return (TResult)(object)result;
        }
    }

    private static Catalog_Номенклатура Nomenclature(string code) => new()
    {
        Ref_Key = Guid.NewGuid(),
        Code = code,
        Description = code,
        ЕдиницаИзмерения_Key = Guid.NewGuid()
    };

    private static Catalog_Номенклатура Nomenclature(string code, Guid externalRefKey) => new()
    {
        Ref_Key = externalRefKey,
        Code = code,
        Description = code,
        ЕдиницаИзмерения_Key = Guid.NewGuid()
    };

    private sealed class IdempotentSkuDispatcher(int failOnceOnCall) : ICommandDispatcher
    {
        private int _callCount;
        private bool _hasFailed;

        public HashSet<Guid> ExternalRefKeys { get; } = [];

        public Task<TResult> DispatchAsync<TCommand, TResult>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>
            where TResult : IServiceResult
        {
            ImportStockKeepingUnits.Command skuCommand = Assert.IsType<ImportStockKeepingUnits.Command>(command);
            _callCount++;
            if (!_hasFailed && _callCount == failOnceOnCall)
            {
                _hasFailed = true;
                ServiceResult<ReferenceImportBatchResult> failure = ServiceResult<ReferenceImportBatchResult>.Fail(
                    ServiceError.Failure<IdempotentSkuDispatcher>("Commit failed."));
                return Task.FromResult((TResult)(object)failure);
            }

            int created = 0;
            int updated = 0;
            foreach (ImportStockKeepingUnits.Item item in skuCommand.Items)
            {
                if (ExternalRefKeys.Add(item.ExternalRefKey))
                {
                    created++;
                }
                else
                {
                    updated++;
                }
            }

            ServiceResult<ReferenceImportBatchResult> success = ServiceResult<ReferenceImportBatchResult>.Success(
                new ReferenceImportBatchResult(
                    skuCommand.Items.Count,
                    created,
                    updated,
                    Skipped: 0,
                    Failed: 0,
                    Errors: []));
            return Task.FromResult((TResult)(object)success);
        }
    }

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

    private sealed class VersionAwareWarehouseDispatcher : ICommandDispatcher
    {
        private byte[]? _storedDataVersion;

        public byte[]? LastDataVersion { get; private set; }

        public Task<TResult> DispatchAsync<TCommand, TResult>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>
            where TResult : IServiceResult
        {
            ImportWarehouses.Item item = Assert.Single(
                Assert.IsType<ImportWarehouses.Command>(command).Items);
            LastDataVersion = item.ExternalDataVersion.ToArray();
            bool unchanged = _storedDataVersion is not null &&
                _storedDataVersion.AsSpan().SequenceEqual(item.ExternalDataVersion);
            _storedDataVersion = item.ExternalDataVersion.ToArray();
            ReferenceImportBatchResult batch = new(
                Processed: 1,
                Created: unchanged ? 0 : 1,
                Updated: 0,
                Unchanged: unchanged ? 1 : 0,
                Skipped: 0,
                Failed: 0,
                Errors: []);
            ServiceResult<ReferenceImportBatchResult> result =
                ServiceResult<ReferenceImportBatchResult>.Success(batch);
            return Task.FromResult((TResult)(object)result);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        private readonly List<IReadOnlyDictionary<string, object?>> _entries = [];

        public string StructuredState => string.Join(
            "|",
            _entries.SelectMany(entry => entry.Select(property => $"{property.Key}={property.Value}")));

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> properties)
            {
                _entries.Add(properties.ToDictionary(property => property.Key, property => property.Value));
            }
        }
    }
}
