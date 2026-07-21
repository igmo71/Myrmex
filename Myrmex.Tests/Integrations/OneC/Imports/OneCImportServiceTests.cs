using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Integrations.OneC.Common.Imports;
using Myrmex.Integrations.OneC.Common.Transport;
using Myrmex.Integrations.OneC.Configuration;
using Myrmex.Integrations.OneC.StockKeepingUnits;
using Myrmex.Integrations.OneC.UnitsOfMeasure;
using Myrmex.Integrations.OneC.Warehouses;
using Myrmex.Modules.Wms.Catalog.Features.Imports;
using Myrmex.Modules.Wms.Topology.Features.Imports;
using Myrmex.Shared.Integrations.OneC;
using System.Runtime.CompilerServices;

namespace Myrmex.Tests.Integrations.OneC.Imports;

public sealed class OneCImportServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-27T12:00:00Z");

    [Fact]
    public void ResponseFactory_ConstructsResponsesConvertsErrorsAndCapsTheSharedErrorList()
    {
        OneCImportResponseFactory factory = CreateResponseFactory();
        ReferenceImportRecordError[] sourceErrors = Enumerable.Range(0, 60)
            .Select(index => new ReferenceImportRecordError(
                Guid.NewGuid(),
                $"U{index}",
                ReferenceImportRecordErrorReasons.InvalidSourceRecord,
                "Invalid."))
            .ToArray();
        ReferenceImportBatchResult batch = new(
            Processed: 60,
            Created: 0,
            Updated: 0,
            Unchanged: 0,
            Skipped: 0,
            Failed: 60,
            Errors: sourceErrors);

        OneCImportResponse complete = factory.CompleteFromBatch("uoms", Now, batch, []);
        OneCImportResponse incomplete = factory.IncompleteFromTransport(
            "uoms",
            Now,
            OneCTransportFailureReason.AuthenticationFailed,
            processed: 7,
            failed: 2,
            errors: OneCImportResponseFactory.ConvertErrors(sourceErrors).ToArray());

        Assert.True(complete.IsComplete);
        Assert.Equal(60, complete.Processed);
        Assert.Equal(60, complete.Failed);
        Assert.Equal(50, complete.Errors.Count);
        Assert.Equal(sourceErrors[0].ExternalRefKey, complete.Errors[0].ExternalRefKey);
        Assert.Equal(sourceErrors[0].Reason, complete.Errors[0].Reason);
        Assert.False(incomplete.IsComplete);
        Assert.Equal(7, incomplete.Processed);
        Assert.Equal(2, incomplete.Failed);
        Assert.Equal(50, incomplete.Errors.Count);
        Assert.Equal("AuthenticationFailed", incomplete.OperationError?.Reason);
        Assert.Equal("1С rejected the configured credentials.", incomplete.OperationError?.Message);
    }

    [Fact]
    public async Task WarehouseImport_MapsDescriptionFallbackCodeAndCommittedFolderSkip()
    {
        Guid warehouseKey = Guid.NewGuid();
        Guid folderKey = Guid.NewGuid();
        StubWarehouseSource source = new()
        {
            Records =
            [
                new WarehouseSourceRecord
                {
                    Ref_Key = folderKey,
                    IsFolder = true,
                    Code = "GROUP",
                    Description = "Group"
                },
                new WarehouseSourceRecord
                {
                    Ref_Key = warehouseKey,
                    DataVersion = [1, 2],
                    Code = null,
                    Description = " Main Warehouse "
                }
            ]
        };
        RecordingDispatcher dispatcher = new(Batch(processed: 1, created: 1));
        WarehouseOneCImport import = CreateWarehouseImport(
            source,
            dispatcher,
            warehouseCodeAvailable: false);

        OneCImportResponse response = await import.ImportAsync(TestContext.Current.CancellationToken);

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
    public async Task UnitOfMeasureImport_UsesFullNameAndSymbolWithDescriptionFallbacks()
    {
        StubUnitOfMeasureSource source = new()
        {
            Records =
            [
                new UnitOfMeasureSourceRecord
                {
                    Ref_Key = Guid.NewGuid(),
                    DataVersion = [2, 3],
                    Code = " 796 ",
                    Description = "Штука",
                    НаименованиеПолное = " Штука полная ",
                    МеждународноеСокращение = " PCE "
                },
                new UnitOfMeasureSourceRecord
                {
                    Ref_Key = Guid.NewGuid(),
                    DataVersion = [3, 4],
                    Code = " 166 ",
                    Description = " Килограмм ",
                    НаименованиеПолное = " ",
                    МеждународноеСокращение = null
                }
            ]
        };
        RecordingDispatcher dispatcher = new(Batch(processed: 2, created: 2));
        UnitOfMeasureOneCImport import = CreateUnitOfMeasureImport(source, dispatcher);

        OneCImportResponse response = await import.ImportAsync(TestContext.Current.CancellationToken);

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
    public async Task WarehouseImport_WhenRepeatedWithSameDataVersion_ReportsUnchanged()
    {
        StubWarehouseSource source = new()
        {
            Records =
            [
                new WarehouseSourceRecord
                {
                    Ref_Key = Guid.NewGuid(),
                    DataVersion = [9, 8, 7],
                    Code = "WH-1",
                    Description = "Warehouse"
                }
            ]
        };
        VersionAwareWarehouseDispatcher dispatcher = new();
        WarehouseOneCImport import = CreateWarehouseImport(source, dispatcher);

        OneCImportResponse first = await import.ImportAsync(TestContext.Current.CancellationToken);
        OneCImportResponse repeated = await import.ImportAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, first.Created);
        Assert.Equal(0, first.Unchanged);
        Assert.Equal(1, repeated.Processed);
        Assert.Equal(0, repeated.Updated);
        Assert.Equal(1, repeated.Unchanged);
        Assert.Equal(new byte[] { 9, 8, 7 }, dispatcher.LastDataVersion);
    }

    [Fact]
    public async Task WarehouseImport_WhenBatchFails_DiscardsPendingFolderCounts()
    {
        StubWarehouseSource source = new()
        {
            Records =
            [
                new WarehouseSourceRecord
                {
                    Ref_Key = Guid.NewGuid(),
                    IsFolder = true,
                    Description = "Group"
                },
                new WarehouseSourceRecord
                {
                    Ref_Key = Guid.NewGuid(),
                    Code = "WH",
                    Description = "Warehouse"
                }
            ]
        };
        RecordingDispatcher dispatcher = new(ServiceResult<ReferenceImportBatchResult>.Fail(
            ServiceError.Failure<OneCImportServiceTests>("Commit failed.")));
        WarehouseOneCImport import = CreateWarehouseImport(source, dispatcher);

        OneCImportResponse response = await import.ImportAsync(TestContext.Current.CancellationToken);

        Assert.False(response.IsComplete);
        Assert.Equal(0, response.Processed);
        Assert.Empty(response.Errors);
        Assert.Equal("BatchCommitFailed", response.OperationError?.Reason);
    }

    [Fact]
    public async Task WarehouseImport_WhenBatchContainsOnlyFolders_CompletesWithoutDispatch()
    {
        StubWarehouseSource source = new()
        {
            Records =
            [
                new WarehouseSourceRecord
                {
                    Ref_Key = Guid.NewGuid(),
                    IsFolder = true,
                    Code = "G1",
                    Description = "Group 1"
                },
                new WarehouseSourceRecord
                {
                    Ref_Key = Guid.NewGuid(),
                    IsFolder = true,
                    Code = "G2",
                    Description = "Group 2"
                }
            ]
        };
        RecordingDispatcher dispatcher = new(Batch());
        WarehouseOneCImport import = CreateWarehouseImport(source, dispatcher);

        OneCImportResponse response = await import.ImportAsync(TestContext.Current.CancellationToken);

        Assert.True(response.IsComplete);
        Assert.Equal(2, response.Processed);
        Assert.Equal(2, response.Skipped);
        Assert.Equal(2, response.Errors.Count);
        Assert.Null(dispatcher.WarehouseCommand);
    }

    [Fact]
    public async Task StockKeepingUnitImport_MapsNamesFoldersAndPerRecordBaseUnitKeys()
    {
        Guid unitKey = Guid.NewGuid();
        StubStockKeepingUnitSource source = new()
        {
            Pages =
            [
                [
                    new StockKeepingUnitSourceRecord
                    {
                        Ref_Key = Guid.NewGuid(),
                        IsFolder = true,
                        Code = "GROUP"
                    },
                    new StockKeepingUnitSourceRecord
                    {
                        Ref_Key = Guid.NewGuid(),
                        DataVersion = [4, 5],
                        Code = " SKU-1 ",
                        Description = "Fallback",
                        НаименованиеПолное = " Full Name ",
                        Артикул = "TRANSPORT-ONLY",
                        ЕдиницаИзмерения_Key = unitKey
                    },
                    new StockKeepingUnitSourceRecord
                    {
                        Ref_Key = Guid.NewGuid(),
                        DataVersion = [5, 6],
                        Code = "SKU-2",
                        Description = " Fallback Name ",
                        ЕдиницаИзмерения_Key = null
                    }
                ]
            ]
        };
        SkuDispatcher dispatcher = new();
        StockKeepingUnitOneCImport import = CreateStockKeepingUnitImport(source, dispatcher);

        OneCImportResponse response = await import.ImportAsync(TestContext.Current.CancellationToken);

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
    public async Task StockKeepingUnitImport_ProcessesMoreThanFifteenThousandRecordsInSourceBatches()
    {
        StockKeepingUnitSourceRecord[] records = Enumerable.Range(0, 15001)
            .Select(index => new StockKeepingUnitSourceRecord
            {
                Ref_Key = Guid.NewGuid(),
                Code = $"SKU-{index}",
                Description = $"Item {index}",
                ЕдиницаИзмерения_Key = Guid.NewGuid()
            })
            .ToArray();
        StubStockKeepingUnitSource source = new()
        {
            Pages = records.Chunk(1000)
                .Select(page => (IReadOnlyList<StockKeepingUnitSourceRecord>)page)
                .ToArray()
        };
        SkuDispatcher dispatcher = new();
        StockKeepingUnitOneCImport import = CreateStockKeepingUnitImport(source, dispatcher);

        OneCImportResponse response = await import.ImportAsync(TestContext.Current.CancellationToken);

        Assert.True(response.IsComplete);
        Assert.Equal(15001, response.Processed);
        Assert.Equal(15001, response.Created);
        Assert.Equal(16, dispatcher.Commands.Count);
    }

    [Fact]
    public async Task StockKeepingUnitImport_WhenLaterBatchFails_RetainsOnlyPriorCommittedCounts()
    {
        StubStockKeepingUnitSource source = new()
        {
            Pages =
            [
                [Nomenclature("SKU-1")],
                [
                    Nomenclature("SKU-2"),
                    new StockKeepingUnitSourceRecord
                    {
                        Ref_Key = Guid.NewGuid(),
                        IsFolder = true
                    }
                ]
            ]
        };
        SkuDispatcher dispatcher = new(
            failOnCall: 2,
            fixedResult: Batch(processed: 1, unchanged: 1));
        StockKeepingUnitOneCImport import = CreateStockKeepingUnitImport(source, dispatcher);

        OneCImportResponse response = await import.ImportAsync(TestContext.Current.CancellationToken);

        Assert.False(response.IsComplete);
        Assert.Equal(1, response.Processed);
        Assert.Equal(0, response.Created);
        Assert.Equal(1, response.Unchanged);
        Assert.Equal(0, response.Skipped);
        Assert.Equal("BatchCommitFailed", response.OperationError?.Reason);
        Assert.Empty(response.Errors);
    }

    [Fact]
    public async Task StockKeepingUnitImport_WhenLaterSourceReadFails_RetainsCommittedCounts()
    {
        StubStockKeepingUnitSource source = new()
        {
            Pages = [[Nomenclature("SKU-1")]],
            ExceptionAfterPages = new OneCTransportException(
                OneCTransportFailureReason.SourceUnavailable,
                "Source unavailable.")
        };
        SkuDispatcher dispatcher = new();
        StockKeepingUnitOneCImport import = CreateStockKeepingUnitImport(source, dispatcher);

        OneCImportResponse response = await import.ImportAsync(TestContext.Current.CancellationToken);

        Assert.False(response.IsComplete);
        Assert.Equal(1, response.Processed);
        Assert.Equal(1, response.Created);
        Assert.Equal("SourceUnavailable", response.OperationError?.Reason);
    }

    [Fact]
    public async Task StockKeepingUnitImport_WhenCancelledAfterCommittedPage_ReturnsIncompleteCommittedCounts()
    {
        using CancellationTokenSource cancellation = new();
        StubStockKeepingUnitSource source = new()
        {
            Pages = [[Nomenclature("SKU-1")]],
            AfterPages = cancellation.Cancel
        };
        SkuDispatcher dispatcher = new();
        StockKeepingUnitOneCImport import = CreateStockKeepingUnitImport(source, dispatcher);

        OneCImportResponse response = await import.ImportAsync(cancellation.Token);

        Assert.False(response.IsComplete);
        Assert.Equal(1, response.Processed);
        Assert.Equal("Cancelled", response.OperationError?.Reason);
    }

    [Fact]
    public async Task WarehouseImport_WhenSourceFails_DoesNotExposeCredentialsInOperationOrLogState()
    {
        const string username = "credential-user-sentinel";
        const string password = "credential-password-sentinel";
        StubWarehouseSource source = new()
        {
            Exception = new OneCTransportException(
                OneCTransportFailureReason.SourceUnavailable,
                $"Unsafe upstream detail containing {username} and {password}.")
        };
        RecordingLogger<WarehouseOneCImport> logger = new();
        WarehouseOneCImport import = CreateWarehouseImport(
            source,
            new RecordingDispatcher(Batch()),
            logger: logger,
            username: username,
            password: password);

        OneCImportResponse response = await import.ImportAsync(TestContext.Current.CancellationToken);

        Assert.False(response.IsComplete);
        Assert.Equal("SourceUnavailable", response.OperationError?.Reason);
        Assert.DoesNotContain(username, response.OperationError?.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(password, response.OperationError?.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(username, logger.StructuredState, StringComparison.Ordinal);
        Assert.DoesNotContain(password, logger.StructuredState, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WarehouseImport_LogsCompletionCountsWithoutSourcePayload()
    {
        const string sourcePayload = "source-payload-sentinel";
        StubWarehouseSource source = new()
        {
            Records =
            [
                new WarehouseSourceRecord
                {
                    Ref_Key = Guid.NewGuid(),
                    Code = "LOG-WH",
                    Description = sourcePayload
                }
            ]
        };
        RecordingLogger<WarehouseOneCImport> logger = new();
        WarehouseOneCImport import = CreateWarehouseImport(
            source,
            new RecordingDispatcher(Batch(processed: 1, created: 1)),
            logger: logger);

        OneCImportResponse response = await import.ImportAsync(TestContext.Current.CancellationToken);

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
    public async Task StockKeepingUnitImport_RejectsSameTypeWithoutWaiting_AllowsOtherTypes_AndReleasesGate()
    {
        StubStockKeepingUnitSource source = new()
        {
            Pages =
            [
                [Nomenclature("SKU-1")],
                [Nomenclature("SKU-2")]
            ]
        };
        OneCImportGate gate = new();
        BlockingSecondSkuDispatcher dispatcher = new();
        StockKeepingUnitOneCImport import = CreateStockKeepingUnitImport(source, dispatcher, gate);
        WarehouseOneCImport warehouseImport = CreateWarehouseImport(
            new StubWarehouseSource(),
            new RecordingDispatcher(Batch()),
            importGate: gate);

        Task<OneCImportResponse> running = import.ImportAsync(TestContext.Current.CancellationToken);
        await dispatcher.SecondCallStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        using CancellationTokenSource duplicateCancellation = new();
        duplicateCancellation.Cancel();

        await Assert.ThrowsAsync<OneCImportAlreadyInProgressException>(() =>
            import.ImportAsync(duplicateCancellation.Token));
        Assert.Equal(1, source.ReadCount);
        Assert.Null(gate.TryAcquire(OneCImportGate.StockKeepingUnits));

        using IDisposable? otherTypeLease = gate.TryAcquire(OneCImportGate.Warehouses);
        Assert.NotNull(otherTypeLease);
        otherTypeLease!.Dispose();
        OneCImportResponse otherType = await warehouseImport.ImportAsync(
            TestContext.Current.CancellationToken);
        Assert.True(otherType.IsComplete);

        dispatcher.ReleaseSecondCall.SetResult(true);
        Assert.True((await running).IsComplete);
        Assert.Equal(2, dispatcher.CallCount);
        Assert.True((await import.ImportAsync(TestContext.Current.CancellationToken)).IsComplete);
        Assert.Equal(2, source.ReadCount);
    }

    [Fact]
    public async Task StockKeepingUnitImport_WhenCancelled_ReleasesGateInFinally()
    {
        TaskCompletionSource<bool> started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StubStockKeepingUnitSource source = new()
        {
            ReadStarted = started,
            ReadRelease = release.Task
        };
        StockKeepingUnitOneCImport import = CreateStockKeepingUnitImport(
            source,
            new RecordingDispatcher(Batch()),
            new OneCImportGate());
        using CancellationTokenSource cancellation = new();

        Task<OneCImportResponse> running = import.ImportAsync(cancellation.Token);
        await started.Task.WaitAsync(TestContext.Current.CancellationToken);
        cancellation.Cancel();

        OneCImportResponse cancelled = await running;
        Assert.False(cancelled.IsComplete);
        Assert.Equal("Cancelled", cancelled.OperationError?.Reason);

        release.SetResult(true);
        OneCImportResponse retry = await import.ImportAsync(TestContext.Current.CancellationToken);
        Assert.True(retry.IsComplete);
        Assert.Equal(2, source.ReadCount);
    }

    [Fact]
    public async Task StockKeepingUnitImport_RetryAfterPartialFailureRevisitsCommittedIdentityWithoutDuplicate()
    {
        Guid firstExternalRefKey = Guid.NewGuid();
        Guid secondExternalRefKey = Guid.NewGuid();
        StubStockKeepingUnitSource source = new()
        {
            Pages =
            [
                [Nomenclature("SKU-1", firstExternalRefKey)],
                [Nomenclature("SKU-2", secondExternalRefKey)]
            ]
        };
        IdempotentSkuDispatcher dispatcher = new(failOnceOnCall: 2);
        StockKeepingUnitOneCImport import = CreateStockKeepingUnitImport(source, dispatcher);

        OneCImportResponse partial = await import.ImportAsync(TestContext.Current.CancellationToken);
        OneCImportResponse retry = await import.ImportAsync(TestContext.Current.CancellationToken);

        Assert.False(partial.IsComplete);
        Assert.Equal(1, partial.Processed);
        Assert.Equal(1, partial.Created);
        Assert.True(retry.IsComplete);
        Assert.Equal(2, retry.Processed);
        Assert.Equal(1, retry.Created);
        Assert.Equal(1, retry.Updated);
        Assert.Equal(2, dispatcher.ExternalRefKeys.Count);
    }

    private static WarehouseOneCImport CreateWarehouseImport(
        StubWarehouseSource source,
        ICommandDispatcher dispatcher,
        bool warehouseCodeAvailable = true,
        OneCImportGate? importGate = null,
        ILogger<WarehouseOneCImport>? logger = null,
        string username = "operator",
        string password = "secret") =>
        new(
            source,
            new StubTransport(),
            dispatcher,
            Options.Create(CreateOptions(warehouseCodeAvailable, username, password)),
            importGate ?? new OneCImportGate(),
            CreateResponseFactory(),
            new FixedTimeProvider(Now),
            logger ?? NullLogger<WarehouseOneCImport>.Instance);

    private static UnitOfMeasureOneCImport CreateUnitOfMeasureImport(
        StubUnitOfMeasureSource source,
        ICommandDispatcher dispatcher,
        OneCImportGate? importGate = null) =>
        new(
            source,
            new StubTransport(),
            dispatcher,
            importGate ?? new OneCImportGate(),
            CreateResponseFactory(),
            new FixedTimeProvider(Now),
            NullLogger<UnitOfMeasureOneCImport>.Instance);

    private static StockKeepingUnitOneCImport CreateStockKeepingUnitImport(
        StubStockKeepingUnitSource source,
        ICommandDispatcher dispatcher,
        OneCImportGate? importGate = null) =>
        new(
            source,
            new StubTransport(),
            dispatcher,
            importGate ?? new OneCImportGate(),
            CreateResponseFactory(),
            new FixedTimeProvider(Now),
            NullLogger<StockKeepingUnitOneCImport>.Instance);

    private static OneCOptions CreateOptions(
        bool warehouseCodeAvailable,
        string username,
        string password) =>
        new()
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

    private static OneCImportResponseFactory CreateResponseFactory() =>
        new(new FixedTimeProvider(Now));

    private static ReferenceImportBatchResult Batch(
        int processed = 0,
        int created = 0,
        int updated = 0,
        int unchanged = 0,
        int skipped = 0,
        int failed = 0,
        IReadOnlyList<ReferenceImportRecordError>? errors = null) =>
        new(processed, created, updated, unchanged, skipped, failed, errors ?? []);

    private sealed class StubTransport : IOneCODataTransport
    {
        public void ValidateConfiguration() { }

        public Task<IReadOnlyList<T>> ReadCollectionAsync<T>(
            string entitySet,
            IEnumerable<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken)
            where T : class =>
            throw new NotSupportedException("Slice import tests provide their source directly.");
    }

    private sealed class StubWarehouseSource : IWarehouseOneCSource
    {
        public IReadOnlyList<WarehouseSourceRecord> Records { get; init; } = [];
        public Exception? Exception { get; init; }

        public Task<IReadOnlyList<WarehouseSourceRecord>> ReadAllAsync(
            CancellationToken cancellationToken) =>
            Exception is null
                ? Task.FromResult(Records)
                : Task.FromException<IReadOnlyList<WarehouseSourceRecord>>(Exception);

        public Task<WarehouseSourceRecord?> ReadCurrentAsync(
            Guid externalRefKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(Records.SingleOrDefault(record => record.Ref_Key == externalRefKey));

        public Task ProbeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubUnitOfMeasureSource : IUnitOfMeasureOneCSource
    {
        public IReadOnlyList<UnitOfMeasureSourceRecord> Records { get; init; } = [];

        public Task<IReadOnlyList<UnitOfMeasureSourceRecord>> ReadAllAsync(
            CancellationToken cancellationToken) => Task.FromResult(Records);

        public Task<UnitOfMeasureSourceRecord?> ReadCurrentAsync(
            Guid externalRefKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(Records.SingleOrDefault(record => record.Ref_Key == externalRefKey));

        public Task ProbeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubStockKeepingUnitSource : IStockKeepingUnitOneCSource
    {
        private int _readCount;

        public IReadOnlyList<IReadOnlyList<StockKeepingUnitSourceRecord>> Pages { get; init; } = [];
        public Exception? ExceptionAfterPages { get; init; }
        public Action? AfterPages { get; init; }
        public TaskCompletionSource<bool>? ReadStarted { get; init; }
        public Task? ReadRelease { get; init; }
        public int ReadCount => Volatile.Read(ref _readCount);

        public async IAsyncEnumerable<IReadOnlyList<StockKeepingUnitSourceRecord>> ReadPagesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _readCount);
            ReadStarted?.TrySetResult(true);
            if (ReadRelease is not null)
            {
                await ReadRelease.WaitAsync(cancellationToken);
            }

            foreach (IReadOnlyList<StockKeepingUnitSourceRecord> page in Pages)
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

        public Task<StockKeepingUnitSourceRecord?> ReadCurrentAsync(
            Guid externalRefKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(Pages.SelectMany(page => page)
                .SingleOrDefault(record => record.Ref_Key == externalRefKey));

        public Task ProbeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
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
            ImportStockKeepingUnits.Command skuCommand =
                Assert.IsType<ImportStockKeepingUnits.Command>(command);
            Commands.Add(skuCommand);
            ServiceResult<ReferenceImportBatchResult> result = failOnCall == Commands.Count
                ? ServiceResult<ReferenceImportBatchResult>.Fail(
                    ServiceError.Failure<SkuDispatcher>("Commit failed."))
                : ServiceResult<ReferenceImportBatchResult>.Success(
                    fixedResult ?? Batch(
                        processed: skuCommand.Items.Count,
                        created: skuCommand.Items.Count));
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
            ImportStockKeepingUnits.Command skuCommand =
                Assert.IsType<ImportStockKeepingUnits.Command>(command);
            CallCount++;
            if (CallCount == 2)
            {
                SecondCallStarted.SetResult(true);
                await ReleaseSecondCall.Task.WaitAsync(cancellationToken);
            }

            ServiceResult<ReferenceImportBatchResult> result =
                ServiceResult<ReferenceImportBatchResult>.Success(Batch(
                    processed: skuCommand.Items.Count,
                    created: skuCommand.Items.Count));
            return (TResult)(object)result;
        }
    }

    private static StockKeepingUnitSourceRecord Nomenclature(string code) => new()
    {
        Ref_Key = Guid.NewGuid(),
        Code = code,
        Description = code,
        ЕдиницаИзмерения_Key = Guid.NewGuid()
    };

    private static StockKeepingUnitSourceRecord Nomenclature(
        string code,
        Guid externalRefKey) =>
        new()
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
            ImportStockKeepingUnits.Command skuCommand =
                Assert.IsType<ImportStockKeepingUnits.Command>(command);
            _callCount++;
            if (!_hasFailed && _callCount == failOnceOnCall)
            {
                _hasFailed = true;
                ServiceResult<ReferenceImportBatchResult> failure =
                    ServiceResult<ReferenceImportBatchResult>.Fail(
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

            ServiceResult<ReferenceImportBatchResult> success =
                ServiceResult<ReferenceImportBatchResult>.Success(Batch(
                    processed: skuCommand.Items.Count,
                    created: created,
                    updated: updated));
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
            ServiceResult<ReferenceImportBatchResult> result =
                ServiceResult<ReferenceImportBatchResult>.Success(Batch(
                    processed: 1,
                    created: unchanged ? 0 : 1,
                    unchanged: unchanged ? 1 : 0));
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
            _entries.SelectMany(entry =>
                entry.Select(property => $"{property.Key}={property.Value}")));

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
                _entries.Add(properties.ToDictionary(
                    property => property.Key,
                    property => property.Value));
            }
        }
    }
}
