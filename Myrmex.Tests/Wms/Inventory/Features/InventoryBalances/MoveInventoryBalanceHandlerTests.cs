using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;
using Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;
using Myrmex.Shared.Wms.Inventory;
using Myrmex.Tests.Wms.Inventory.Testing;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Inventory.Features.InventoryBalances;

public sealed class MoveInventoryBalanceHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenMoveSucceeds_CreatesOneBalancedTransferHistoryWithoutTransferDocument()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededManualInventoryMove seeded = await InventoryBalanceTestData
            .SeedManualInventoryMoveAsync(testDbContext.DbContext);
        MoveInventoryBalance.Handler handler = CreateHandler(testDbContext);

        ServiceResult<MoveInventoryBalanceResult> result = await handler.HandleAsync(
            CreateCommand(seeded, quantity: 4),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        InventoryTransaction transaction = await testDbContext.DbContext.InventoryTransactions
            .Include(x => x.Entries)
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(InventoryTransactionType.Transfer, transaction.TransactionType);
        Assert.Equal("Replenish pick face", transaction.Reason);
        Assert.Equal(result.Value.OccurredAtUtc, transaction.OccurredAtUtc);

        InventoryLedgerEntry[] entries = transaction.Entries
            .OrderBy(x => x.StorageLocationId)
            .ToArray();

        Assert.Equal(2, entries.Length);

        InventoryLedgerEntry sourceEntry = Assert.Single(
            entries,
            x => x.StorageLocationId == seeded.SourceStorageLocation.Id);
        Assert.Equal(seeded.StockKeepingUnit.Id, sourceEntry.StockKeepingUnitId);
        Assert.Equal(-4, sourceEntry.QuantityDelta);
        Assert.Equal(10, sourceEntry.BalanceBefore);
        Assert.Equal(6, sourceEntry.BalanceAfter);

        InventoryLedgerEntry destinationEntry = Assert.Single(
            entries,
            x => x.StorageLocationId == seeded.DestinationStorageLocation.Id);
        Assert.Equal(seeded.StockKeepingUnit.Id, destinationEntry.StockKeepingUnitId);
        Assert.Equal(4, destinationEntry.QuantityDelta);
        Assert.Equal(3, destinationEntry.BalanceBefore);
        Assert.Equal(7, destinationEntry.BalanceAfter);

        Assert.Equal(0, entries.Sum(x => x.QuantityDelta));
        Assert.Equal(
            0,
            await testDbContext.DbContext.InventoryTransfers.CountAsync(
                TestContext.Current.CancellationToken));
        Assert.Equal(
            0,
            await testDbContext.DbContext.InventoryTransferMovements.CountAsync(
                TestContext.Current.CancellationToken));
        Assert.DoesNotContain(
            await testDbContext.DbContext.InventoryTransactions
                .ToListAsync(TestContext.Current.CancellationToken),
            x => x.TransactionType == InventoryTransactionType.Adjustment);
    }

    [Fact]
    public async Task HandleAsync_WhenDestinationExists_MovesQuantityAndReturnsAuthoritativeDetails()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededManualInventoryMove seeded = await InventoryBalanceTestData
            .SeedManualInventoryMoveAsync(testDbContext.DbContext);
        MoveInventoryBalance.Handler handler = CreateHandler(testDbContext);

        ServiceResult<MoveInventoryBalanceResult> result = await handler.HandleAsync(
            CreateCommand(seeded, quantity: 4),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.MovedQuantity);
        Assert.Equal(10, result.Value.SourceQuantityBefore);
        Assert.Equal(6, result.Value.SourceQuantityAfter);
        Assert.Equal(3, result.Value.DestinationQuantityBefore);
        Assert.Equal(7, result.Value.DestinationQuantityAfter);
        Assert.Equal(6, result.Value.SourceBalance.Quantity);
        Assert.Equal(7, result.Value.DestinationBalance.Quantity);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.SourceBalance.BalanceVersion));
        Assert.False(string.IsNullOrWhiteSpace(result.Value.DestinationBalance.BalanceVersion));
    }

    [Fact]
    public async Task HandleAsync_WhenDestinationIsMissing_CreatesDestinationFromZero()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededManualInventoryMove seeded = await InventoryBalanceTestData
            .SeedManualInventoryMoveAsync(
                testDbContext.DbContext,
                destinationQuantity: null);
        MoveInventoryBalance.Handler handler = CreateHandler(testDbContext);

        ServiceResult<MoveInventoryBalanceResult> result = await handler.HandleAsync(
            CreateCommand(seeded, quantity: 4),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.DestinationQuantityBefore);
        Assert.Equal(4, result.Value.DestinationQuantityAfter);
        Assert.Equal(4, result.Value.DestinationBalance.Quantity);
        Assert.Equal(
            seeded.DestinationStorageLocation.Id,
            result.Value.DestinationBalance.StorageLocation.Id);
    }

    [Fact]
    public async Task HandleAsync_WhenMovingFullQuantity_RetainsZeroSourceBalance()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededManualInventoryMove seeded = await InventoryBalanceTestData
            .SeedManualInventoryMoveAsync(testDbContext.DbContext);
        MoveInventoryBalance.Handler handler = CreateHandler(testDbContext);

        ServiceResult<MoveInventoryBalanceResult> result = await handler.HandleAsync(
            CreateCommand(seeded, quantity: 10),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.SourceQuantityAfter);
        Assert.Equal(0, result.Value.SourceBalance.Quantity);
        Assert.True(await testDbContext.DbContext.InventoryBalances.AnyAsync(
            x => x.Id == seeded.SourceBalance.Id,
            TestContext.Current.CancellationToken));
    }

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    internal async Task HandleAsync_WhenCommandIsInvalid_ReturnsValidationFailure(
        MoveInventoryBalance.Command command)
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        MoveInventoryBalance.Handler handler = CreateHandler(testDbContext);

        ServiceResult<MoveInventoryBalanceResult> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_WhenReferenceIsMissing_ReturnsNotFound()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededManualInventoryMove seeded = await InventoryBalanceTestData
            .SeedManualInventoryMoveAsync(testDbContext.DbContext);
        MoveInventoryBalance.Handler handler = CreateHandler(testDbContext);
        Guid missingId = Guid.Parse("018f0000-0000-7000-8000-00000000f999");

        ServiceResult<MoveInventoryBalanceResult>[] results =
        [
            await handler.HandleAsync(
                CreateCommand(seeded) with { StockKeepingUnitId = missingId },
                TestContext.Current.CancellationToken),
            await handler.HandleAsync(
                CreateCommand(seeded) with { SourceStorageLocationId = missingId },
                TestContext.Current.CancellationToken),
            await handler.HandleAsync(
                CreateCommand(seeded) with { DestinationStorageLocationId = missingId },
                TestContext.Current.CancellationToken)
        ];

        Assert.All(results, result =>
        {
            Assert.False(result.IsSuccess);
            Assert.Equal(ServiceErrorType.NotFound, result.Error.Type);
        });
    }

    [Theory]
    [InlineData(ConflictScenario.MissingSource)]
    [InlineData(ConflictScenario.StaleSourceVersion)]
    [InlineData(ConflictScenario.InsufficientQuantity)]
    public async Task HandleAsync_WhenSubmittedSourceStateIsInvalid_ReturnsConflictWithoutChanges(
        ConflictScenario scenario)
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededManualInventoryMove seeded = await InventoryBalanceTestData
            .SeedManualInventoryMoveAsync(testDbContext.DbContext);

        if (scenario == ConflictScenario.MissingSource)
        {
            testDbContext.DbContext.InventoryBalances.Remove(seeded.SourceBalance);
            await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        MoveInventoryBalance.Command command = scenario switch
        {
            ConflictScenario.StaleSourceVersion => CreateCommand(seeded) with
            {
                ExpectedSourceBalanceVersion =
                    Convert.ToBase64String([1, 2, 3, 4, 5, 6, 7, 8])
            },
            ConflictScenario.InsufficientQuantity => CreateCommand(seeded, quantity: 11),
            _ => CreateCommand(seeded)
        };

        MoveInventoryBalance.Handler handler = CreateHandler(testDbContext);
        ServiceResult<MoveInventoryBalanceResult> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Conflict, result.Error.Type);
        Assert.Equal(
            0,
            await testDbContext.DbContext.InventoryTransactions.CountAsync(
                TestContext.Current.CancellationToken));
        Assert.Equal(
            0,
            await testDbContext.DbContext.InventoryLedgerEntries.CountAsync(
                TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(EligibilityScenario.InactiveSku)]
    [InlineData(EligibilityScenario.InactiveSourceLocation)]
    [InlineData(EligibilityScenario.InactiveDestinationLocation)]
    [InlineData(EligibilityScenario.InactiveLocationType)]
    [InlineData(EligibilityScenario.InactiveLocationStatus)]
    [InlineData(EligibilityScenario.CrossWarehouse)]
    [InlineData(EligibilityScenario.TransitSource)]
    [InlineData(EligibilityScenario.TransitDestination)]
    public async Task HandleAsync_WhenReferencesAreIneligible_ReturnsValidationFailure(
        EligibilityScenario scenario)
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededManualInventoryMove seeded = await InventoryBalanceTestData
            .SeedManualInventoryMoveAsync(testDbContext.DbContext);
        MoveInventoryBalance.Command command = CreateCommand(seeded);

        switch (scenario)
        {
            case EligibilityScenario.InactiveSku:
                seeded.StockKeepingUnit.Deactivate();
                break;
            case EligibilityScenario.InactiveSourceLocation:
                seeded.SourceStorageLocation.Deactivate();
                break;
            case EligibilityScenario.InactiveDestinationLocation:
                seeded.DestinationStorageLocation.Deactivate();
                break;
            case EligibilityScenario.InactiveLocationType:
                seeded.RegularStorageLocationType.Deactivate();
                break;
            case EligibilityScenario.InactiveLocationStatus:
                seeded.StorageLocationStatus.Deactivate();
                break;
            case EligibilityScenario.CrossWarehouse:
                command = command with
                {
                    DestinationStorageLocationId =
                        seeded.CrossWarehouseStorageLocation.Id
                };
                break;
            case EligibilityScenario.TransitSource:
                command = command with
                {
                    SourceStorageLocationId =
                        seeded.InternalTransitStorageLocation.Id
                };
                break;
            case EligibilityScenario.TransitDestination:
                command = command with
                {
                    DestinationStorageLocationId =
                        seeded.ExternalTransitStorageLocation.Id
                };
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
        }

        await testDbContext.DbContext.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        MoveInventoryBalance.Handler handler = CreateHandler(testDbContext);
        ServiceResult<MoveInventoryBalanceResult> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_WhenDestinationRowVersionIsStale_ReturnsConflictAtomically()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededManualInventoryMove seeded = await InventoryBalanceTestData
            .SeedManualInventoryMoveAsync(testDbContext.DbContext);

        await using (var concurrentDbContext = testDbContext.CreateDbContext())
        {
            InventoryBalance concurrentDestination = await concurrentDbContext.InventoryBalances
                .SingleAsync(
                    x => x.Id == seeded.DestinationBalance!.Id,
                    TestContext.Current.CancellationToken);
            Assert.True(concurrentDestination.UpdateQuantity(4).IsValid);
            await concurrentDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        MoveInventoryBalance.Handler handler = CreateHandler(testDbContext);
        ServiceResult<MoveInventoryBalanceResult> result = await handler.HandleAsync(
            CreateCommand(seeded),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Conflict, result.Error.Type);

        testDbContext.DbContext.ChangeTracker.Clear();
        InventoryBalance source = await testDbContext.DbContext.InventoryBalances.SingleAsync(
            x => x.Id == seeded.SourceBalance.Id,
            TestContext.Current.CancellationToken);
        InventoryBalance destination = await testDbContext.DbContext.InventoryBalances.SingleAsync(
            x => x.Id == seeded.DestinationBalance!.Id,
            TestContext.Current.CancellationToken);
        Assert.Equal(10, source.Quantity);
        Assert.Equal(4, destination.Quantity);
        Assert.Equal(
            0,
            await testDbContext.DbContext.InventoryTransactions.CountAsync(
                TestContext.Current.CancellationToken));
        Assert.Equal(
            0,
            await testDbContext.DbContext.InventoryLedgerEntries.CountAsync(
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_WhenDestinationCreationConflicts_ReturnsConflictWithoutSourceChange()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededManualInventoryMove seeded = await InventoryBalanceTestData
            .SeedManualInventoryMoveAsync(
                testDbContext.DbContext,
                destinationQuantity: null);
        Assert.True(InventoryBalance.Create(
            seeded.StockKeepingUnit.Id,
            seeded.DestinationStorageLocation.Id,
            quantity: 1,
            out InventoryBalance? competingDestination).IsValid);
        Assert.NotNull(competingDestination);
        competingDestination.ClearDomainEvents();
        testDbContext.DbContext.InventoryBalances.Add(competingDestination);

        MoveInventoryBalance.Handler handler = CreateHandler(testDbContext);
        ServiceResult<MoveInventoryBalanceResult> result = await handler.HandleAsync(
            CreateCommand(seeded),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Conflict, result.Error.Type);

        testDbContext.DbContext.ChangeTracker.Clear();
        InventoryBalance source = await testDbContext.DbContext.InventoryBalances.SingleAsync(
            x => x.Id == seeded.SourceBalance.Id,
            TestContext.Current.CancellationToken);
        Assert.Equal(10, source.Quantity);
        Assert.Equal(
            0,
            await testDbContext.DbContext.InventoryTransactions.CountAsync(
                TestContext.Current.CancellationToken));
        Assert.Equal(
            0,
            await testDbContext.DbContext.InventoryLedgerEntries.CountAsync(
                TestContext.Current.CancellationToken));
    }

    public static IEnumerable<object[]> InvalidCommands()
    {
        Guid skuId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
        Guid sourceId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
        Guid destinationId = Guid.Parse("018f0000-0000-7000-8000-000000000202");
        string version = Convert.ToBase64String([1, 2, 3, 4, 5, 6, 7, 8]);

        yield return [new MoveInventoryBalance.Command(null, sourceId, destinationId, 1, "Move", version)];
        yield return [new MoveInventoryBalance.Command(skuId, null, destinationId, 1, "Move", version)];
        yield return [new MoveInventoryBalance.Command(skuId, sourceId, null, 1, "Move", version)];
        yield return [new MoveInventoryBalance.Command(skuId, sourceId, sourceId, 1, "Move", version)];
        yield return [new MoveInventoryBalance.Command(skuId, sourceId, destinationId, 0, "Move", version)];
        yield return [new MoveInventoryBalance.Command(skuId, sourceId, destinationId, -1, "Move", version)];
        yield return [new MoveInventoryBalance.Command(skuId, sourceId, destinationId, 1, " ", version)];
        yield return [new MoveInventoryBalance.Command(
            skuId,
            sourceId,
            destinationId,
            1,
            new string('x', InventoryTransaction.ReasonMaxLength + 1),
            version)];
        yield return [new MoveInventoryBalance.Command(skuId, sourceId, destinationId, 1, "Move", null)];
        yield return [new MoveInventoryBalance.Command(skuId, sourceId, destinationId, 1, "Move", "invalid")];
        yield return [new MoveInventoryBalance.Command(skuId, sourceId, destinationId, 1, "Move", "AQ==")];
    }

    private static MoveInventoryBalance.Handler CreateHandler(
        TestWmsDbContext testDbContext)
    {
        return new MoveInventoryBalance.Handler(
            testDbContext.DbContext,
            new RecordingDomainEventDispatcher(),
            NullLogger<MoveInventoryBalance.Handler>.Instance);
    }

    private static MoveInventoryBalance.Command CreateCommand(
        SeededManualInventoryMove seeded,
        decimal quantity = 2)
    {
        return new MoveInventoryBalance.Command(
            seeded.StockKeepingUnit.Id,
            seeded.SourceStorageLocation.Id,
            seeded.DestinationStorageLocation.Id,
            quantity,
            "  Replenish pick face  ",
            Convert.ToBase64String(seeded.SourceBalance.RowVersion));
    }

    public enum ConflictScenario
    {
        MissingSource,
        StaleSourceVersion,
        InsufficientQuantity
    }

    public enum EligibilityScenario
    {
        InactiveSku,
        InactiveSourceLocation,
        InactiveDestinationLocation,
        InactiveLocationType,
        InactiveLocationStatus,
        CrossWarehouse,
        TransitSource,
        TransitDestination
    }
}
