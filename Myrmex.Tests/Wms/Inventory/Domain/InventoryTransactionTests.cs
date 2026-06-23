using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;

namespace Myrmex.Tests.Wms.Inventory.Domain;

public sealed class InventoryTransactionTests
{
    private static readonly Guid StockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
    private static readonly Guid StorageLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000301");

    [Fact]
    public void CreateAdjustment_WhenMaterialCorrectionIsValid_CreatesImmutableTransactionAndEntry()
    {
        DateTimeOffset occurredAtUtc = DateTimeOffset.Parse("2026-06-18T12:00:00Z");

        var result = InventoryTransaction.CreateAdjustment(
            StockKeepingUnitId,
            StorageLocationId,
            balanceBefore: 10,
            balanceAfter: 14,
            reason: "  Cycle count correction  ",
            occurredAtUtc,
            out InventoryTransaction? transaction);

        Assert.True(result.IsValid);
        Assert.NotNull(transaction);
        Assert.Equal(InventoryTransactionType.Adjustment, transaction.TransactionType);
        Assert.Equal("Cycle count correction", transaction.Reason);
        Assert.Equal(occurredAtUtc, transaction.OccurredAtUtc);
        Assert.Null(transaction.UpdatedAtUtc);

        InventoryLedgerEntry entry = Assert.Single(transaction.Entries);
        Assert.Equal(StockKeepingUnitId, entry.StockKeepingUnitId);
        Assert.Equal(StorageLocationId, entry.StorageLocationId);
        Assert.Equal(4, entry.QuantityDelta);
        Assert.Equal(10, entry.BalanceBefore);
        Assert.Equal(14, entry.BalanceAfter);
        Assert.Null(entry.UpdatedAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateAdjustment_WhenReasonIsMissing_ReturnsValidationFailure(string reason)
    {
        var result = InventoryTransaction.CreateAdjustment(
            StockKeepingUnitId,
            StorageLocationId,
            balanceBefore: 10,
            balanceAfter: 14,
            reason,
            DateTimeOffset.UtcNow,
            out InventoryTransaction? transaction);

        Assert.False(result.IsValid);
        Assert.Null(transaction);

        Assert.Contains(result.Errors, error =>
            error.Property == nameof(InventoryTransaction.Reason) &&
            error.Code == "Required-InventoryTransaction-Reason");
    }

    [Fact]
    public void CreateAdjustment_WhenReasonIsTooLong_ReturnsValidationFailure()
    {
        var result = InventoryTransaction.CreateAdjustment(
            StockKeepingUnitId,
            StorageLocationId,
            balanceBefore: 10,
            balanceAfter: 14,
            new string('x', InventoryTransaction.ReasonMaxLength + 1),
            DateTimeOffset.UtcNow,
            out InventoryTransaction? transaction);

        Assert.False(result.IsValid);
        Assert.Null(transaction);

        Assert.Contains(result.Errors, error =>
            error.Property == nameof(InventoryTransaction.Reason) &&
            error.Code == "TooLong-InventoryTransaction-Reason");
    }

    [Fact]
    public void CreateAdjustment_WhenDeltaIsZero_ReturnsValidationFailure()
    {
        var result = InventoryTransaction.CreateAdjustment(
            StockKeepingUnitId,
            StorageLocationId,
            balanceBefore: 10,
            balanceAfter: 10,
            reason: "Cycle count correction",
            DateTimeOffset.UtcNow,
            out InventoryTransaction? transaction);

        Assert.False(result.IsValid);
        Assert.Null(transaction);

        Assert.Contains(result.Errors, error =>
            error.Property == nameof(InventoryLedgerEntry.QuantityDelta) &&
            error.Code == "IncorrectState-InventoryLedgerEntry-QuantityDelta");
    }

    [Fact]
    public void CreateTransfer_WhenBalancesAreValid_CreatesTransactionWithExactlyTwoLedgerEntries()
    {
        Guid destinationStorageLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000302");
        DateTimeOffset occurredAtUtc = DateTimeOffset.Parse("2026-06-19T12:00:00Z");

        var result = InventoryTransaction.CreateTransfer(
            StockKeepingUnitId,
            StorageLocationId,
            destinationStorageLocationId,
            fromBalanceBefore: 10,
            fromBalanceAfter: 6,
            toBalanceBefore: 3,
            toBalanceAfter: 7,
            reason: "Internal transfer TR-001",
            occurredAtUtc,
            out InventoryTransaction? transaction);

        Assert.True(result.IsValid);
        Assert.NotNull(transaction);
        Assert.Equal(InventoryTransactionType.Transfer, transaction.TransactionType);
        Assert.Equal("Internal transfer TR-001", transaction.Reason);
        Assert.Equal(occurredAtUtc, transaction.OccurredAtUtc);

        InventoryLedgerEntry[] entries = transaction.Entries.ToArray();
        Assert.Equal(2, entries.Length);

        Assert.Contains(entries, entry =>
            entry.StockKeepingUnitId == StockKeepingUnitId &&
            entry.StorageLocationId == StorageLocationId &&
            entry.QuantityDelta == -4 &&
            entry.BalanceBefore == 10 &&
            entry.BalanceAfter == 6);

        Assert.Contains(entries, entry =>
            entry.StockKeepingUnitId == StockKeepingUnitId &&
            entry.StorageLocationId == destinationStorageLocationId &&
            entry.QuantityDelta == 4 &&
            entry.BalanceBefore == 3 &&
            entry.BalanceAfter == 7);
    }
}
