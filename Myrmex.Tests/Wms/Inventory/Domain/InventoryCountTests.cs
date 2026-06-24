using Myrmex.Modules.Wms.Inventory.Domain.InventoryCounts;

namespace Myrmex.Tests.Wms.Inventory.Domain;

public sealed class InventoryCountTests
{
    [Fact]
    public void Create_WhenValid_NormalizesReasonAndCreatesDraft()
    {
        Guid warehouseId = Guid.NewGuid();

        var result = InventoryCount.Create(
            warehouseId,
            "  Monthly count  ",
            " operator-1 ",
            out InventoryCount? count);

        Assert.True(result.IsValid);
        Assert.NotNull(count);
        Assert.Equal(warehouseId, count.WarehouseId);
        Assert.Equal("Monthly count", count.Reason);
        Assert.Equal("operator-1", count.CreatedByActorId);
        Assert.Equal(InventoryCountStatus.Draft, count.Status);
        Assert.Empty(count.Lines);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenActorMissing_IsInvalid(string? actorId)
    {
        var result = InventoryCount.Create(
            Guid.NewGuid(),
            reason: null,
            actorId,
            out InventoryCount? count);

        Assert.False(result.IsValid);
        Assert.Null(count);
    }

    [Fact]
    public void AddLine_CapturesSnapshotAndRejectsDuplicateCurrentPair()
    {
        InventoryCount count = CreateCount();
        Guid skuId = Guid.NewGuid();
        Guid locationId = Guid.NewGuid();
        byte[] version = [1, 2, 3, 4, 5, 6, 7, 8];

        var firstResult = count.AddLine(
            skuId,
            locationId,
            systemQuantity: 12,
            version,
            out InventoryCountLine? line);
        var duplicateResult = count.AddLine(
            skuId,
            locationId,
            systemQuantity: 15,
            expectedBalanceVersion: null,
            out InventoryCountLine? duplicate);

        Assert.True(firstResult.IsValid);
        Assert.NotNull(line);
        Assert.Equal(12, line.SystemQuantity);
        Assert.True(version.SequenceEqual(line.ExpectedBalanceVersion!));
        Assert.Equal(InventoryCountLineStatus.Pending, line.Status);
        Assert.True(line.IsCurrent);
        Assert.False(duplicateResult.IsValid);
        Assert.Null(duplicate);
        Assert.Single(count.Lines);
        Assert.Equal(InventoryCountStatus.Draft, count.Status);
    }

    [Fact]
    public void RemovePendingLine_RemovesPreparationData()
    {
        InventoryCount count = CreateCount();
        count.AddLine(Guid.NewGuid(), Guid.NewGuid(), 0, null, out InventoryCountLine? line);

        var result = count.RemovePendingLine(line!.Id, out InventoryCountLine? removed);

        Assert.True(result.IsValid);
        Assert.Same(line, removed);
        Assert.Empty(count.Lines);
    }

    private static InventoryCount CreateCount()
    {
        var result = InventoryCount.Create(
            Guid.NewGuid(),
            reason: null,
            "operator-1",
            out InventoryCount? count);

        Assert.True(result.IsValid);
        Assert.NotNull(count);
        return count;
    }
}
