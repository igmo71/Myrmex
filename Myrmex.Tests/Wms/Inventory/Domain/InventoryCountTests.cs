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

    [Fact]
    public void RecordLineCount_WhenPending_CapturesEvidenceAndMovesCountToInProgress()
    {
        InventoryCount count = CreateCount();
        byte[] expectedBalanceVersion = [1, 2, 3, 4, 5, 6, 7, 8];
        count.AddLine(
            Guid.NewGuid(),
            Guid.NewGuid(),
            systemQuantity: 10,
            expectedBalanceVersion,
            out InventoryCountLine? line);
        DateTimeOffset countedAtUtc = DateTimeOffset.Parse("2026-06-25T10:00:00Z");

        var result = count.RecordLineCount(
            line!.Id,
            countedQuantity: 12,
            comment: "  Two units behind pallet  ",
            actorId: " operator-2 ",
            countedAtUtc: countedAtUtc);

        Assert.True(result.IsValid);
        Assert.Equal(InventoryCountStatus.InProgress, count.Status);
        Assert.Equal(InventoryCountLineStatus.Counted, line.Status);
        Assert.Equal(12, line.CountedQuantity);
        Assert.Equal(2, line.VarianceQuantity);
        Assert.Equal("Two units behind pallet", line.Comment);
        Assert.Equal("operator-2", line.CountedByActorId);
        Assert.Equal(countedAtUtc, line.CountedAtUtc);
        Assert.Equal(10, line.SystemQuantity);
        Assert.True(expectedBalanceVersion.SequenceEqual(line.ExpectedBalanceVersion!));
    }

    [Fact]
    public void RecordLineCount_WhenCounted_ReplacesLatestEvidenceWithoutChangingSnapshot()
    {
        InventoryCount count = CreateCount();
        byte[] expectedBalanceVersion = [8, 7, 6, 5, 4, 3, 2, 1];
        count.AddLine(
            Guid.NewGuid(),
            Guid.NewGuid(),
            systemQuantity: 10,
            expectedBalanceVersion,
            out InventoryCountLine? line);
        count.RecordLineCount(
            line!.Id,
            countedQuantity: 12,
            comment: "First pass",
            actorId: "operator-1",
            countedAtUtc: DateTimeOffset.Parse("2026-06-25T10:00:00Z"));
        DateTimeOffset recountedAtUtc = DateTimeOffset.Parse("2026-06-25T10:05:00Z");

        var result = count.RecordLineCount(
            line.Id,
            countedQuantity: 9,
            comment: null,
            actorId: "operator-2",
            countedAtUtc: recountedAtUtc);

        Assert.True(result.IsValid);
        Assert.Equal(10, line.SystemQuantity);
        Assert.True(expectedBalanceVersion.SequenceEqual(line.ExpectedBalanceVersion!));
        Assert.Equal(9, line.CountedQuantity);
        Assert.Equal(-1, line.VarianceQuantity);
        Assert.Null(line.Comment);
        Assert.Equal("operator-2", line.CountedByActorId);
        Assert.Equal(recountedAtUtc, line.CountedAtUtc);
    }

    [Fact]
    public void RecordLineCount_WhenQuantityNegativeOrCommentTooLong_IsInvalid()
    {
        InventoryCount count = CreateCount();
        count.AddLine(Guid.NewGuid(), Guid.NewGuid(), 10, null, out InventoryCountLine? line);

        var negativeResult = count.RecordLineCount(
            line!.Id,
            countedQuantity: -1,
            comment: null,
            actorId: "operator-1",
            countedAtUtc: DateTimeOffset.UtcNow);
        var commentResult = count.RecordLineCount(
            line.Id,
            countedQuantity: 10,
            comment: new string('x', InventoryCountLine.CommentMaxLength + 1),
            actorId: "operator-1",
            countedAtUtc: DateTimeOffset.UtcNow);

        Assert.False(negativeResult.IsValid);
        Assert.False(commentResult.IsValid);
        Assert.Equal(InventoryCountStatus.Draft, count.Status);
        Assert.Equal(InventoryCountLineStatus.Pending, line.Status);
        Assert.Null(line.CountedQuantity);
    }

    [Theory]
    [InlineData(InventoryCountStatus.Completed)]
    [InlineData(InventoryCountStatus.Cancelled)]
    internal void RecordLineCount_WhenCountFinal_IsInvalid(InventoryCountStatus status)
    {
        InventoryCount count = CreateCount();
        count.AddLine(Guid.NewGuid(), Guid.NewGuid(), 10, null, out InventoryCountLine? line);
        typeof(InventoryCount)
            .GetProperty(nameof(InventoryCount.Status))!
            .SetValue(count, status);

        var result = count.RecordLineCount(
            line!.Id,
            countedQuantity: 10,
            comment: null,
            actorId: "operator-1",
            countedAtUtc: DateTimeOffset.UtcNow);

        Assert.False(result.IsValid);
        Assert.Equal(InventoryCountLineStatus.Pending, line.Status);
        Assert.Null(line.CountedQuantity);
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
