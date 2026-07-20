using Microsoft.Extensions.Logging.Abstractions;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Integrations.OneC.Common.Imports;
using Myrmex.Integrations.OneC.Common.References;
using Myrmex.Integrations.OneC.StockKeepingUnits;
using Myrmex.Integrations.OneC.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.Imports;
using System.Runtime.CompilerServices;

namespace Myrmex.Tests.Integrations.OneC.References;

public sealed class StockKeepingUnitReferenceRepairTests
{
    private static readonly Guid StockKeepingUnitExternalRefKey =
        Guid.Parse("018f0000-0000-7000-8000-000000000911");
    private static readonly Guid UnitOfMeasureExternalRefKey =
        Guid.Parse("018f0000-0000-7000-8000-000000000912");

    [Theory]
    [InlineData(ReferenceSynchronizationOutcome.Applied)]
    [InlineData(ReferenceSynchronizationOutcome.Unchanged)]
    internal async Task SynchronizeAsync_WhenUnitOfMeasureRepairSucceeds_RetriesSkuOnce(
        ReferenceSynchronizationOutcome unitOfMeasureOutcome)
    {
        StubStockKeepingUnitSource source = new(StockKeepingUnit());
        StubUnitOfMeasureSynchronizer dependency = new(unitOfMeasureOutcome);
        RepairDispatcher dispatcher = new(
            BaseUnitOfMeasureFailure(ReferenceImportRecordErrorReasons.BaseUnitOfMeasureNotImported),
            Applied());
        StockKeepingUnitOneCSynchronizer synchronizer = CreateSynchronizer(
            source,
            dependency,
            dispatcher);

        ReferenceSynchronizationResult result = await synchronizer.SynchronizeAsync(
            StockKeepingUnitExternalRefKey,
            TestContext.Current.CancellationToken);

        Assert.Equal(ReferenceSynchronizationOutcome.Applied, result.Outcome);
        Assert.Equal(1, source.ReadCount);
        Assert.Equal(1, dependency.CallCount);
        Assert.Equal(UnitOfMeasureExternalRefKey, dependency.ExternalRefKey);
        Assert.Equal(2, dispatcher.StockKeepingUnitDispatchCount);
    }

    [Fact]
    public async Task SynchronizeAsync_WhenUnitOfMeasureSucceedsButSkuRetryStillNeedsRepair_StopsPermanently()
    {
        StubStockKeepingUnitSource source = new(StockKeepingUnit());
        StubUnitOfMeasureSynchronizer dependency =
            new(ReferenceSynchronizationOutcome.Applied);
        RepairDispatcher dispatcher = new(
            BaseUnitOfMeasureFailure(ReferenceImportRecordErrorReasons.BaseUnitOfMeasureNotImported),
            BaseUnitOfMeasureFailure(ReferenceImportRecordErrorReasons.BaseUnitOfMeasureInactive));
        StockKeepingUnitOneCSynchronizer synchronizer = CreateSynchronizer(
            source,
            dependency,
            dispatcher);

        ReferenceSynchronizationResult result = await synchronizer.SynchronizeAsync(
            StockKeepingUnitExternalRefKey,
            TestContext.Current.CancellationToken);

        Assert.Equal(ReferenceSynchronizationOutcome.PermanentFailure, result.Outcome);
        Assert.Equal(ReferenceSynchronizationReasons.BaseUnitOfMeasureRepairFailed, result.Reason);
        Assert.Equal(1, source.ReadCount);
        Assert.Equal(1, dependency.CallCount);
        Assert.Equal(2, dispatcher.StockKeepingUnitDispatchCount);
    }

    [Theory]
    [InlineData(ReferenceSynchronizationOutcome.Busy, ReferenceSynchronizationOutcome.TransientFailure)]
    [InlineData(ReferenceSynchronizationOutcome.TransientFailure, ReferenceSynchronizationOutcome.TransientFailure)]
    [InlineData(ReferenceSynchronizationOutcome.NotFound, ReferenceSynchronizationOutcome.PermanentFailure)]
    [InlineData(ReferenceSynchronizationOutcome.ControlledSkip, ReferenceSynchronizationOutcome.PermanentFailure)]
    [InlineData(ReferenceSynchronizationOutcome.PermanentFailure, ReferenceSynchronizationOutcome.PermanentFailure)]
    internal async Task SynchronizeAsync_WhenUnitOfMeasureRepairFails_MapsOutcomeWithoutSkuRetry(
        ReferenceSynchronizationOutcome unitOfMeasureOutcome,
        ReferenceSynchronizationOutcome expectedSkuOutcome)
    {
        StubStockKeepingUnitSource source = new(StockKeepingUnit());
        StubUnitOfMeasureSynchronizer dependency = new(unitOfMeasureOutcome);
        RepairDispatcher dispatcher = new(
            BaseUnitOfMeasureFailure(ReferenceImportRecordErrorReasons.BaseUnitOfMeasureNotImported),
            Applied());
        StockKeepingUnitOneCSynchronizer synchronizer = CreateSynchronizer(
            source,
            dependency,
            dispatcher);

        ReferenceSynchronizationResult result = await synchronizer.SynchronizeAsync(
            StockKeepingUnitExternalRefKey,
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedSkuOutcome, result.Outcome);
        Assert.Equal(expectedSkuOutcome == ReferenceSynchronizationOutcome.TransientFailure,
            result.RetrySuitable);
        Assert.Equal(1, source.ReadCount);
        Assert.Equal(1, dependency.CallCount);
        Assert.Equal(1, dispatcher.StockKeepingUnitDispatchCount);
    }

    private static StockKeepingUnitOneCSynchronizer CreateSynchronizer(
        IStockKeepingUnitOneCSource source,
        IUnitOfMeasureOneCSynchronizer dependency,
        ICommandDispatcher dispatcher) =>
        new(
            source,
            dependency,
            dispatcher,
            new OneCImportGate(),
            TimeProvider.System,
            NullLogger<StockKeepingUnitOneCSynchronizer>.Instance);

    private static StockKeepingUnitSourceRecord StockKeepingUnit() => new()
    {
        Ref_Key = StockKeepingUnitExternalRefKey,
        DataVersion = [1],
        Code = "SKU-001",
        Description = "Stock item",
        НаименованиеПолное = "Stock item",
        ЕдиницаИзмерения_Key = UnitOfMeasureExternalRefKey
    };

    private static ReferenceImportBatchResult BaseUnitOfMeasureFailure(string reason) => new(
        1, 0, 0, 0, 0, 1,
        [new ReferenceImportRecordError(
            StockKeepingUnitExternalRefKey,
            "SKU-001",
            reason,
            "The base unit of measure is missing or inactive.")]);

    private static ReferenceImportBatchResult Applied() => new(1, 1, 0, 0, 0, 0, []);

    private sealed class StubStockKeepingUnitSource(StockKeepingUnitSourceRecord record)
        : IStockKeepingUnitOneCSource
    {
        public int ReadCount { get; private set; }

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
            ReadCount++;
            Assert.Equal(StockKeepingUnitExternalRefKey, externalRefKey);
            return Task.FromResult<StockKeepingUnitSourceRecord?>(record);
        }

        public Task ProbeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubUnitOfMeasureSynchronizer(ReferenceSynchronizationOutcome outcome)
        : IUnitOfMeasureOneCSynchronizer
    {
        public int CallCount { get; private set; }
        public Guid? ExternalRefKey { get; private set; }

        public Task<ReferenceSynchronizationResult> SynchronizeAsync(
            Guid externalRefKey,
            CancellationToken cancellationToken)
        {
            CallCount++;
            ExternalRefKey = externalRefKey;
            ReferenceSynchronizationResult result = outcome is
                ReferenceSynchronizationOutcome.Applied or
                ReferenceSynchronizationOutcome.Unchanged or
                ReferenceSynchronizationOutcome.ControlledSkip
                    ? ReferenceSynchronizationResult.Success(
                        OneCReferenceType.UnitOfMeasure,
                        externalRefKey,
                        outcome,
                        outcome.ToString())
                    : ReferenceSynchronizationResult.Failure(
                        OneCReferenceType.UnitOfMeasure,
                        externalRefKey,
                        outcome,
                        outcome.ToString(),
                        "Expected repair result.",
                        retrySuitable: outcome is ReferenceSynchronizationOutcome.Busy or
                            ReferenceSynchronizationOutcome.TransientFailure);
            return Task.FromResult(result);
        }
    }

    private sealed class RepairDispatcher(
        ReferenceImportBatchResult firstResult,
        ReferenceImportBatchResult secondResult) : ICommandDispatcher
    {
        public int StockKeepingUnitDispatchCount { get; private set; }

        public Task<TResult> DispatchAsync<TCommand, TResult>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>
            where TResult : IServiceResult
        {
            Assert.IsType<ImportStockKeepingUnits.Command>(command);
            ReferenceImportBatchResult batch = StockKeepingUnitDispatchCount++ == 0
                ? firstResult
                : secondResult;
            ServiceResult<ReferenceImportBatchResult> result =
                ServiceResult<ReferenceImportBatchResult>.Success(batch);
            return Task.FromResult((TResult)(object)result);
        }
    }
}
