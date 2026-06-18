using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;
using Myrmex.Modules.Wms.Inventory.Features.InventoryAdjustments;
using Myrmex.Shared.Wms.Inventory;
using Myrmex.Tests.Wms.Inventory.Testing;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Inventory.Features.InventoryAdjustments;

public sealed class AdjustInventoryBalanceHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenExistingBalanceHasMaterialAdjustment_UpdatesBalanceAndCreatesLedger()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        SeededInventoryBalance seeded = await InventoryBalanceTestData.SeedInventoryBalanceAsync(
            testDbContext.DbContext,
            quantity: 10);
        AdjustInventoryBalance.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);
        string expectedVersion = Convert.ToBase64String(seeded.InventoryBalance.RowVersion);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            new AdjustInventoryBalance.Command(
                seeded.StockKeepingUnit.Id,
                seeded.StorageLocation.Id,
                CountedQuantity: 14,
                Reason: "  Cycle count correction  ",
                ExpectedBalanceVersion: expectedVersion),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(14, result.Value.Quantity);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.BalanceVersion));

        InventoryTransaction transaction = await testDbContext.DbContext.InventoryTransactions
            .Include(x => x.Entries)
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(InventoryTransactionType.Adjustment, transaction.TransactionType);
        Assert.Equal("Cycle count correction", transaction.Reason);

        InventoryLedgerEntry entry = Assert.Single(transaction.Entries);
        Assert.Equal(seeded.StockKeepingUnit.Id, entry.StockKeepingUnitId);
        Assert.Equal(seeded.StorageLocation.Id, entry.StorageLocationId);
        Assert.Equal(4, entry.QuantityDelta);
        Assert.Equal(10, entry.BalanceBefore);
        Assert.Equal(14, entry.BalanceAfter);
    }

    [Fact]
    public async Task HandleAsync_WhenMissingBalanceHasPositiveInitialCount_CreatesBalanceAndLedger()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        SeededInventoryReferences seeded = await InventoryBalanceTestData.SeedInventoryReferencesAsync(
            testDbContext.DbContext);
        AdjustInventoryBalance.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            new AdjustInventoryBalance.Command(
                seeded.StockKeepingUnit.Id,
                seeded.StorageLocation.Id,
                CountedQuantity: 7,
                Reason: "Initial physical count",
                ExpectedBalanceVersion: null),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.Quantity);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.BalanceVersion));

        InventoryTransaction transaction = await testDbContext.DbContext.InventoryTransactions
            .Include(x => x.Entries)
            .SingleAsync(TestContext.Current.CancellationToken);

        InventoryLedgerEntry entry = Assert.Single(transaction.Entries);
        Assert.Equal(seeded.StockKeepingUnit.Id, entry.StockKeepingUnitId);
        Assert.Equal(seeded.StorageLocation.Id, entry.StorageLocationId);
        Assert.Equal(7, entry.QuantityDelta);
        Assert.Equal(0, entry.BalanceBefore);
        Assert.Equal(7, entry.BalanceAfter);
    }

    [Fact]
    public async Task HandleAsync_WhenMissingBalanceHasZeroInitialCount_CreatesZeroBalanceWithoutLedger()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        SeededInventoryReferences seeded = await InventoryBalanceTestData.SeedInventoryReferencesAsync(
            testDbContext.DbContext);
        AdjustInventoryBalance.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            new AdjustInventoryBalance.Command(
                seeded.StockKeepingUnit.Id,
                seeded.StorageLocation.Id,
                CountedQuantity: 0,
                Reason: "Initial physical count",
                ExpectedBalanceVersion: null),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Quantity);

        Assert.Equal(1, await testDbContext.DbContext.InventoryBalances.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await testDbContext.DbContext.InventoryTransactions.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await testDbContext.DbContext.InventoryLedgerEntries.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_WhenExistingBalanceIsNoOp_ReturnsSuccessWithoutChangingBalanceOrLedger()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        SeededInventoryBalance seeded = await InventoryBalanceTestData.SeedInventoryBalanceAsync(
            testDbContext.DbContext,
            quantity: 10);
        AdjustInventoryBalance.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);
        string expectedVersion = Convert.ToBase64String(seeded.InventoryBalance.RowVersion);
        DateTimeOffset? updatedAtUtc = seeded.InventoryBalance.UpdatedAtUtc;
        byte[] rowVersion = [.. seeded.InventoryBalance.RowVersion];

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            new AdjustInventoryBalance.Command(
                seeded.StockKeepingUnit.Id,
                seeded.StorageLocation.Id,
                CountedQuantity: 10,
                Reason: "Cycle count confirmation",
                ExpectedBalanceVersion: expectedVersion),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.Quantity);
        Assert.Equal(expectedVersion, result.Value.BalanceVersion);
        Assert.Equal(updatedAtUtc, seeded.InventoryBalance.UpdatedAtUtc);
        Assert.True(seeded.InventoryBalance.RowVersion.SequenceEqual(rowVersion));
        Assert.Equal(0, await testDbContext.DbContext.InventoryTransactions.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await testDbContext.DbContext.InventoryLedgerEntries.CountAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [MemberData(nameof(ValidationCases))]
    internal async Task HandleAsync_WhenRequestIsInvalid_ReturnsValidationFailure(AdjustInventoryBalance.Command command)
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        AdjustInventoryBalance.Handler handler = new(
            testDbContext.DbContext,
            new RecordingDomainEventDispatcher());

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_WhenMissingReferencesAreNotFound_ReturnsNotFound()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        AdjustInventoryBalance.Handler handler = new(
            testDbContext.DbContext,
            new RecordingDomainEventDispatcher());

        ServiceResult<InventoryBalanceDetails> missingSkuResult = await handler.HandleAsync(
            new AdjustInventoryBalance.Command(
                Guid.Parse("018f0000-0000-7000-8000-00000000f101"),
                Guid.Parse("018f0000-0000-7000-8000-00000000f201"),
                CountedQuantity: 1,
                Reason: "Initial physical count",
                ExpectedBalanceVersion: null),
            TestContext.Current.CancellationToken);

        Assert.False(missingSkuResult.IsSuccess);
        Assert.Equal(ServiceErrorType.NotFound, missingSkuResult.Error.Type);
        Assert.Equal("StockKeepingUnit", missingSkuResult.Error.Property);

        SeededInventoryReferences seeded = await InventoryBalanceTestData.SeedInventoryReferencesAsync(
            testDbContext.DbContext);

        ServiceResult<InventoryBalanceDetails> missingLocationResult = await handler.HandleAsync(
            new AdjustInventoryBalance.Command(
                seeded.StockKeepingUnit.Id,
                Guid.Parse("018f0000-0000-7000-8000-00000000f202"),
                CountedQuantity: 1,
                Reason: "Initial physical count",
                ExpectedBalanceVersion: null),
            TestContext.Current.CancellationToken);

        Assert.False(missingLocationResult.IsSuccess);
        Assert.Equal(ServiceErrorType.NotFound, missingLocationResult.Error.Type);
        Assert.Equal("StorageLocation", missingLocationResult.Error.Property);
    }

    [Theory]
    [InlineData(MissingEligibilityInactiveReference.StockKeepingUnit)]
    [InlineData(MissingEligibilityInactiveReference.BaseUnitOfMeasure)]
    [InlineData(MissingEligibilityInactiveReference.StorageLocation)]
    [InlineData(MissingEligibilityInactiveReference.StorageLocationType)]
    [InlineData(MissingEligibilityInactiveReference.StorageLocationStatus)]
    public async Task HandleAsync_WhenMissingBalanceReferenceIsInactive_ReturnsCurrentEligibilityFailure(
        MissingEligibilityInactiveReference inactiveReference)
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryReferences seeded = await InventoryBalanceTestData.SeedInventoryReferencesAsync(
            testDbContext.DbContext);
        Deactivate(seeded, inactiveReference);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        AdjustInventoryBalance.Handler handler = new(
            testDbContext.DbContext,
            new RecordingDomainEventDispatcher());

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            new AdjustInventoryBalance.Command(
                seeded.StockKeepingUnit.Id,
                seeded.StorageLocation.Id,
                CountedQuantity: 1,
                Reason: "Initial physical count",
                ExpectedBalanceVersion: null),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_WhenExistingBalanceReferencesAreInactive_AllowsCorrection()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        SeededInventoryBalance seeded = await InventoryBalanceTestData.SeedInventoryBalanceAsync(
            testDbContext.DbContext,
            quantity: 10);
        string expectedVersion = Convert.ToBase64String(seeded.InventoryBalance.RowVersion);
        seeded.StockKeepingUnit.Deactivate();
        seeded.BaseUnitOfMeasure.Deactivate();
        seeded.StorageLocation.Deactivate();
        seeded.StorageLocationType.Deactivate();
        seeded.StorageLocationStatus.Deactivate();
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        AdjustInventoryBalance.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ServiceResult<InventoryBalanceDetails> result = await handler.HandleAsync(
            new AdjustInventoryBalance.Command(
                seeded.StockKeepingUnit.Id,
                seeded.StorageLocation.Id,
                CountedQuantity: 12,
                Reason: "Cycle count correction",
                ExpectedBalanceVersion: expectedVersion),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(12, result.Value.Quantity);
    }

    public static IEnumerable<object[]> ValidationCases()
    {
        yield return
        [
            new AdjustInventoryBalance.Command(
                null,
                Guid.Parse("018f0000-0000-7000-8000-000000000201"),
                CountedQuantity: 1,
                Reason: "Initial physical count",
                ExpectedBalanceVersion: null)
        ];
        yield return
        [
            new AdjustInventoryBalance.Command(
                Guid.Empty,
                Guid.Parse("018f0000-0000-7000-8000-000000000201"),
                CountedQuantity: 1,
                Reason: "Initial physical count",
                ExpectedBalanceVersion: null)
        ];
        yield return
        [
            new AdjustInventoryBalance.Command(
                Guid.Parse("018f0000-0000-7000-8000-000000000101"),
                null,
                CountedQuantity: 1,
                Reason: "Initial physical count",
                ExpectedBalanceVersion: null)
        ];
        yield return
        [
            new AdjustInventoryBalance.Command(
                Guid.Parse("018f0000-0000-7000-8000-000000000101"),
                Guid.Parse("018f0000-0000-7000-8000-000000000201"),
                CountedQuantity: -1,
                Reason: "Initial physical count",
                ExpectedBalanceVersion: null)
        ];
        yield return
        [
            new AdjustInventoryBalance.Command(
                Guid.Parse("018f0000-0000-7000-8000-000000000101"),
                Guid.Parse("018f0000-0000-7000-8000-000000000201"),
                CountedQuantity: 1,
                Reason: "   ",
                ExpectedBalanceVersion: null)
        ];
        yield return
        [
            new AdjustInventoryBalance.Command(
                Guid.Parse("018f0000-0000-7000-8000-000000000101"),
                Guid.Parse("018f0000-0000-7000-8000-000000000201"),
                CountedQuantity: 1,
                Reason: new string('x', InventoryTransaction.ReasonMaxLength + 1),
                ExpectedBalanceVersion: null)
        ];
        yield return
        [
            new AdjustInventoryBalance.Command(
                Guid.Parse("018f0000-0000-7000-8000-000000000101"),
                Guid.Parse("018f0000-0000-7000-8000-000000000201"),
                CountedQuantity: 1,
                Reason: "Cycle count correction",
                ExpectedBalanceVersion: "not-base64")
        ];
    }

    private static void Deactivate(
        SeededInventoryReferences seeded,
        MissingEligibilityInactiveReference inactiveReference)
    {
        switch (inactiveReference)
        {
            case MissingEligibilityInactiveReference.StockKeepingUnit:
                seeded.StockKeepingUnit.Deactivate();
                break;
            case MissingEligibilityInactiveReference.BaseUnitOfMeasure:
                seeded.BaseUnitOfMeasure.Deactivate();
                break;
            case MissingEligibilityInactiveReference.StorageLocation:
                seeded.StorageLocation.Deactivate();
                break;
            case MissingEligibilityInactiveReference.StorageLocationType:
                seeded.StorageLocationType.Deactivate();
                break;
            case MissingEligibilityInactiveReference.StorageLocationStatus:
                seeded.StorageLocationStatus.Deactivate();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(inactiveReference), inactiveReference, null);
        }
    }

    public enum MissingEligibilityInactiveReference
    {
        StockKeepingUnit,
        BaseUnitOfMeasure,
        StorageLocation,
        StorageLocationType,
        StorageLocationStatus
    }
}
