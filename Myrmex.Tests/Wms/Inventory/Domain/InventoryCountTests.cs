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

    [Fact]
    public void ApplyLine_WhenCounted_RecordsAuditAndTransaction()
    {
        InventoryCount count = CreateCount();
        InventoryCountLine line = AddAndRecordLine(count, 10, 12);
        Guid transactionId = Guid.NewGuid();
        DateTimeOffset appliedAtUtc = DateTimeOffset.Parse("2026-06-25T11:00:00Z");

        var result = count.ApplyLine(line.Id, transactionId, " operator-2 ", appliedAtUtc);

        Assert.True(result.IsValid);
        Assert.Equal(InventoryCountLineStatus.Applied, line.Status);
        Assert.Equal("operator-2", line.AppliedByActorId);
        Assert.Equal(appliedAtUtc, line.AppliedAtUtc);
        Assert.Equal(transactionId, line.AppliedInventoryTransactionId);
        Assert.True(line.IsCurrent);
    }

    [Fact]
    public void MarkConflictAndSupersede_PreservesHistoryAndCreatesReplacement()
    {
        InventoryCount count = CreateCount();
        InventoryCountLine conflictLine = AddAndRecordLine(count, 10, 12);
        Assert.True(count.MarkLineConflict(conflictLine.Id).IsValid);
        byte[] freshVersion = [8, 7, 6, 5, 4, 3, 2, 1];

        var result = count.SupersedeLine(
            conflictLine.Id,
            freshSystemQuantity: 11,
            freshVersion,
            out InventoryCountLine? replacement);

        Assert.True(result.IsValid);
        Assert.NotNull(replacement);
        Assert.Equal(InventoryCountLineStatus.Superseded, conflictLine.Status);
        Assert.False(conflictLine.IsCurrent);
        Assert.Equal(InventoryCountLineStatus.Pending, replacement.Status);
        Assert.True(replacement.IsCurrent);
        Assert.Equal(11, replacement.SystemQuantity);
        Assert.True(freshVersion.SequenceEqual(replacement.ExpectedBalanceVersion!));
        Assert.Equal(conflictLine.Id, replacement.SupersedesInventoryCountLineId);

        var duplicate = count.SupersedeLine(
            conflictLine.Id,
            11,
            freshVersion,
            out InventoryCountLine? duplicateReplacement);

        Assert.False(duplicate.IsValid);
        Assert.Null(duplicateReplacement);
    }

    [Fact]
    public void ConflictAndAppliedLines_RejectFurtherMutation()
    {
        InventoryCount conflictCount = CreateCount();
        InventoryCountLine conflictLine = AddAndRecordLine(conflictCount, 10, 12);
        conflictCount.MarkLineConflict(conflictLine.Id);

        Assert.False(conflictCount.RecordLineCount(
            conflictLine.Id,
            13,
            null,
            "operator-2",
            DateTimeOffset.UtcNow).IsValid);
        Assert.False(conflictCount.ApplyLine(
            conflictLine.Id,
            Guid.NewGuid(),
            "operator-2",
            DateTimeOffset.UtcNow).IsValid);

        InventoryCount appliedCount = CreateCount();
        InventoryCountLine appliedLine = AddAndRecordLine(appliedCount, 10, 12);
        appliedCount.ApplyLine(
            appliedLine.Id,
            Guid.NewGuid(),
            "operator-2",
            DateTimeOffset.UtcNow);

        Assert.False(appliedCount.ApplyLine(
            appliedLine.Id,
            Guid.NewGuid(),
            "operator-3",
            DateTimeOffset.UtcNow).IsValid);
    }

    [Fact]
    public void Complete_WhenAllCurrentLinesApplied_RecordsAuditAndMakesCountReadOnly()
    {
        InventoryCount count = CreateCount();
        InventoryCountLine line = AddAndRecordLine(count, 10, 12);
        count.ApplyLine(
            line.Id,
            Guid.NewGuid(),
            "operator-2",
            DateTimeOffset.Parse("2026-06-25T11:00:00Z"));
        DateTimeOffset completedAtUtc = DateTimeOffset.Parse("2026-06-25T12:00:00Z");

        var result = count.Complete(" supervisor-1 ", completedAtUtc);

        Assert.True(result.IsValid);
        Assert.Equal(InventoryCountStatus.Completed, count.Status);
        Assert.Equal("supervisor-1", count.CompletedByActorId);
        Assert.Equal(completedAtUtc, count.CompletedAtUtc);
        Assert.Null(count.CancelledByActorId);
        Assert.Null(count.CancelledAtUtc);
        Assert.False(count.AddLine(
            Guid.NewGuid(),
            Guid.NewGuid(),
            0,
            null,
            out _).IsValid);
        Assert.False(count.RemovePendingLine(line.Id, out _).IsValid);
        Assert.False(count.Cancel("operator-3", DateTimeOffset.UtcNow).IsValid);
    }

    [Fact]
    public void Complete_WhenEmptyOrCurrentLineUnresolved_IsInvalid()
    {
        InventoryCount empty = CreateCount();
        InventoryCount pending = CreateCount();
        pending.AddLine(Guid.NewGuid(), Guid.NewGuid(), 0, null, out _);
        InventoryCount conflict = CreateCount();
        InventoryCountLine conflictLine = AddAndRecordLine(conflict, 10, 12);
        conflict.MarkLineConflict(conflictLine.Id);

        Assert.False(empty.Complete("operator-1", DateTimeOffset.UtcNow).IsValid);
        Assert.False(pending.Complete("operator-1", DateTimeOffset.UtcNow).IsValid);
        Assert.False(conflict.Complete("operator-1", DateTimeOffset.UtcNow).IsValid);
        Assert.Equal(InventoryCountStatus.Draft, empty.Status);
        Assert.Equal(InventoryCountStatus.Draft, pending.Status);
        Assert.Equal(InventoryCountStatus.InProgress, conflict.Status);
    }

    [Fact]
    public void Cancel_WhenActive_RecordsAuditAndPreservesAppliedEvidence()
    {
        InventoryCount count = CreateCount();
        InventoryCountLine appliedLine = AddAndRecordLine(count, 10, 12);
        Guid transactionId = Guid.NewGuid();
        count.ApplyLine(
            appliedLine.Id,
            transactionId,
            "operator-2",
            DateTimeOffset.Parse("2026-06-25T11:00:00Z"));
        count.AddLine(Guid.NewGuid(), Guid.NewGuid(), 0, null, out InventoryCountLine? pending);
        DateTimeOffset cancelledAtUtc = DateTimeOffset.Parse("2026-06-25T12:00:00Z");

        var result = count.Cancel(" supervisor-1 ", cancelledAtUtc);

        Assert.True(result.IsValid);
        Assert.Equal(InventoryCountStatus.Cancelled, count.Status);
        Assert.Equal("supervisor-1", count.CancelledByActorId);
        Assert.Equal(cancelledAtUtc, count.CancelledAtUtc);
        Assert.Equal(InventoryCountLineStatus.Applied, appliedLine.Status);
        Assert.Equal(transactionId, appliedLine.AppliedInventoryTransactionId);
        Assert.Equal(InventoryCountLineStatus.Pending, pending!.Status);
        Assert.False(count.RemovePendingLine(pending.Id, out _).IsValid);
        Assert.False(count.Complete("operator-3", DateTimeOffset.UtcNow).IsValid);
    }

    private static InventoryCountLine AddAndRecordLine(
        InventoryCount count,
        decimal systemQuantity,
        decimal countedQuantity)
    {
        count.AddLine(
            Guid.NewGuid(),
            Guid.NewGuid(),
            systemQuantity,
            [1, 2, 3, 4, 5, 6, 7, 8],
            out InventoryCountLine? line);
        count.RecordLineCount(
            line!.Id,
            countedQuantity,
            null,
            "operator-1",
            DateTimeOffset.Parse("2026-06-25T10:00:00Z"));
        return line;
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
