using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Events;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.Imports;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Features.Imports;

public sealed class ImportUnitsOfMeasureHandlerTests
{
    private static readonly DateTimeOffset ImportedAtUtc = DateTimeOffset.Parse("2026-06-27T12:00:00Z");

    [Fact]
    public async Task HandleAsync_UpsertsByExternalIdentityAndDoesNotLinkByCode()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        Guid linkedKey = Guid.NewGuid();
        UnitOfMeasure linked = CreateUnit("OLD", "Old", "old", linkedKey);
        linked.Deactivate();
        UnitOfMeasure local = CreateUnit("EA", "Each", "ea");
        testDbContext.DbContext.UnitsOfMeasure.AddRange(linked, local);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        testDbContext.DbContext.ChangeTracker.Clear();
        ImportUnitsOfMeasure.Handler handler = new(
            testDbContext.DbContext,
            new RecordingDomainEventDispatcher());

        ServiceResult<ReferenceImportBatchResult> result = await handler.HandleAsync(
            new ImportUnitsOfMeasure.Command(
            [
                new(linkedKey, [2], " pkg ", " Package ", " pc ", false, ImportedAtUtc),
                new(Guid.NewGuid(), [1], " kg ", " Kilogram ", " kg ", false, ImportedAtUtc),
                new(Guid.NewGuid(), [1], "ea", "Must not link", "ea", false, ImportedAtUtc),
                new(Guid.NewGuid(), [1], "", "Missing code", "x", false, ImportedAtUtc)
            ]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.Processed);
        Assert.Equal(1, result.Value.Created);
        Assert.Equal(1, result.Value.Updated);
        Assert.Equal(1, result.Value.Skipped);
        Assert.Equal(1, result.Value.Failed);
        Assert.True(result.Value.HasConsistentCounts);

        UnitOfMeasure updated = await testDbContext.DbContext.UnitsOfMeasure.SingleAsync(
            x => x.ImportState != null && x.ImportState.RefKey == linkedKey,
            TestContext.Current.CancellationToken);
        Assert.Equal("PKG", updated.Code);
        Assert.Equal("Package", updated.Name);
        Assert.Equal("pc", updated.Symbol);
        Assert.True(updated.IsActive);
        Assert.Null((await testDbContext.DbContext.UnitsOfMeasure.SingleAsync(
            x => x.Id == local.Id,
            TestContext.Current.CancellationToken)).ExternalRefKey);
    }

    [Fact]
    public async Task HandleAsync_WhenUnlinkedRecordIsDeletionMarked_SkipsAndReportsIt()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        Guid externalRefKey = Guid.NewGuid();
        ImportUnitsOfMeasure.Handler handler = new(testDbContext.DbContext, new RecordingDomainEventDispatcher());

        ServiceResult<ReferenceImportBatchResult> result = await handler.HandleAsync(
            new ImportUnitsOfMeasure.Command(
                [new(externalRefKey, [1], null, null, null, true, ImportedAtUtc)]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Processed);
        Assert.Equal(0, result.Value.Created);
        Assert.Equal(1, result.Value.Skipped);
        ReferenceImportRecordError error = Assert.Single(result.Value.Errors);
        Assert.Equal(externalRefKey, error.ExternalRefKey);
        Assert.Equal(ReferenceImportRecordErrorReasons.SourceRecordDeletionMarked, error.Reason);
        Assert.False(await testDbContext.DbContext.UnitsOfMeasure.AnyAsync(
            x => x.ImportState != null && x.ImportState.RefKey == externalRefKey,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_WhenLinkedDeletionMarkedFieldsAreInvalid_DeactivatesWithoutUpdatingDetails()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        Guid externalRefKey = Guid.NewGuid();
        UnitOfMeasure linked = CreateUnit("EA", "Each", "ea", externalRefKey);
        testDbContext.DbContext.UnitsOfMeasure.Add(linked);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        testDbContext.DbContext.ChangeTracker.Clear();
        ImportUnitsOfMeasure.Handler handler = new(testDbContext.DbContext, new RecordingDomainEventDispatcher());
        DateTimeOffset deletionImportedAtUtc = ImportedAtUtc.AddMinutes(5);

        ServiceResult<ReferenceImportBatchResult> result = await handler.HandleAsync(
            new ImportUnitsOfMeasure.Command(
                [new(externalRefKey, [2], null, null, new string('x', UnitOfMeasure.MaxSymbolLength + 1), true, deletionImportedAtUtc)]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Processed);
        Assert.Equal(1, result.Value.Updated);
        Assert.Equal(0, result.Value.Failed);
        Assert.Empty(result.Value.Errors);
        UnitOfMeasure saved = await testDbContext.DbContext.UnitsOfMeasure.SingleAsync(
            x => x.ImportState != null && x.ImportState.RefKey == externalRefKey,
            TestContext.Current.CancellationToken);
        Assert.False(saved.IsActive);
        Assert.Equal("EA", saved.Code);
        Assert.Equal("Each", saved.Name);
        Assert.Equal("ea", saved.Symbol);
        Assert.Equal(deletionImportedAtUtc, saved.LastImportedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WhenExternalIdentityIsImportedAgain_UpdatesWithoutCreatingDuplicate()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        ImportUnitsOfMeasure.Handler handler = new(
            testDbContext.DbContext,
            new RecordingDomainEventDispatcher());
        Guid externalRefKey = Guid.NewGuid();
        DateTimeOffset repeatedAtUtc = ImportedAtUtc.AddMinutes(10);

        ServiceResult<ReferenceImportBatchResult> first = await handler.HandleAsync(
            new ImportUnitsOfMeasure.Command(
                [new(externalRefKey, [1], "EA", "Each", "ea", false, ImportedAtUtc)]),
            TestContext.Current.CancellationToken);
        Guid internalId = await testDbContext.DbContext.UnitsOfMeasure
            .Where(unit => unit.ImportState != null && unit.ImportState.RefKey == externalRefKey)
            .Select(unit => unit.Id)
            .SingleAsync(TestContext.Current.CancellationToken);

        ServiceResult<ReferenceImportBatchResult> repeated = await handler.HandleAsync(
            new ImportUnitsOfMeasure.Command(
                [new(externalRefKey, [2], "EA", "Each", "each", false, repeatedAtUtc)]),
            TestContext.Current.CancellationToken);

        Assert.True(first.IsSuccess);
        Assert.Equal(1, first.Value.Created);
        Assert.True(repeated.IsSuccess);
        Assert.Equal(0, repeated.Value.Created);
        Assert.Equal(1, repeated.Value.Updated);
        UnitOfMeasure saved = await testDbContext.DbContext.UnitsOfMeasure.SingleAsync(
            unit => unit.ImportState != null && unit.ImportState.RefKey == externalRefKey,
            TestContext.Current.CancellationToken);
        Assert.Equal(internalId, saved.Id);
        Assert.Equal(externalRefKey, saved.ExternalRefKey);
        Assert.Equal("EA", saved.Code);
        Assert.Equal("Each", saved.Name);
        Assert.Equal("each", saved.Symbol);
        Assert.Equal(repeatedAtUtc, saved.LastImportedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WhenDataVersionIsCurrent_ReturnsUnchangedWithoutMutationOrEvent()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        Guid externalRefKey = Guid.NewGuid();
        UnitOfMeasure linked = CreateUnit("EA", "Each", "ea", externalRefKey);
        testDbContext.DbContext.UnitsOfMeasure.Add(linked);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DateTimeOffset? updatedAtUtc = linked.UpdatedAtUtc;
        testDbContext.DbContext.ChangeTracker.Clear();
        RecordingDomainEventDispatcher dispatcher = new();
        ImportUnitsOfMeasure.Handler handler = new(testDbContext.DbContext, dispatcher);

        ServiceResult<ReferenceImportBatchResult> result = await handler.HandleAsync(
            new ImportUnitsOfMeasure.Command(
                [new(externalRefKey, [1], "KG", "Kilogram", "kg", true, ImportedAtUtc.AddHours(1))]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Unchanged);
        Assert.Equal(0, result.Value.Updated);
        UnitOfMeasure saved = await testDbContext.DbContext.UnitsOfMeasure.SingleAsync(
            x => x.ImportState != null && x.ImportState.RefKey == externalRefKey,
            TestContext.Current.CancellationToken);
        Assert.Equal("EA", saved.Code);
        Assert.Equal("Each", saved.Name);
        Assert.Equal("ea", saved.Symbol);
        Assert.True(saved.IsActive);
        Assert.Equal(ImportedAtUtc, saved.LastImportedAtUtc);
        Assert.Equal(updatedAtUtc, saved.UpdatedAtUtc);
        Assert.Empty(saved.DomainEvents);
        Assert.Empty(dispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenEventDispatchFails_RollsBackWholeBatch()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        ImportUnitsOfMeasure.Handler handler = new(testDbContext.DbContext, new ThrowingDomainEventDispatcher());
        Guid externalRefKey = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            new ImportUnitsOfMeasure.Command(
                [new(externalRefKey, [1], "ROLLBACK", "Rollback", "r", false, ImportedAtUtc)]),
            TestContext.Current.CancellationToken));

        await using var verificationContext = testDbContext.CreateDbContext();
        Assert.False(await verificationContext.UnitsOfMeasure.AnyAsync(
            x => x.ImportState != null && x.ImportState.RefKey == externalRefKey,
            TestContext.Current.CancellationToken));
    }

    private static UnitOfMeasure CreateUnit(
        string code,
        string name,
        string symbol,
        Guid? externalRefKey = null)
    {
        Assert.True(UnitOfMeasure.Create(code, name, symbol, out UnitOfMeasure? unit).IsValid);
        if (externalRefKey.HasValue)
        {
            Assert.True(unit!.ApplyImport(externalRefKey.Value, [1], code, name, symbol, false, ImportedAtUtc).IsValid);
        }
        unit!.ClearDomainEvents();
        return unit;
    }

    private sealed class ThrowingDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException("Dispatch failed."));

        public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException("Dispatch failed."));
    }
}
