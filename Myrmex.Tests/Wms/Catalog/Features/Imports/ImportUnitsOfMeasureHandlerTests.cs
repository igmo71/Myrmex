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
                new(linkedKey, " pkg ", " Package ", " pc ", false, ImportedAtUtc),
                new(Guid.NewGuid(), " kg ", " Kilogram ", " kg ", false, ImportedAtUtc),
                new(Guid.NewGuid(), "ea", "Must not link", "ea", false, ImportedAtUtc),
                new(Guid.NewGuid(), "", "Missing code", "x", false, ImportedAtUtc)
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
            x => x.ExternalRefKey == linkedKey,
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
                [new(externalRefKey, null, null, null, true, ImportedAtUtc)]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Processed);
        Assert.Equal(0, result.Value.Created);
        Assert.Equal(1, result.Value.Skipped);
        ReferenceImportRecordError error = Assert.Single(result.Value.Errors);
        Assert.Equal(externalRefKey, error.ExternalRefKey);
        Assert.Equal(ReferenceImportRecordErrorReasons.SourceRecordDeletionMarked, error.Reason);
        Assert.False(await testDbContext.DbContext.UnitsOfMeasure.AnyAsync(
            x => x.ExternalRefKey == externalRefKey,
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
                [new(externalRefKey, null, null, new string('x', UnitOfMeasure.MaxSymbolLength + 1), true, deletionImportedAtUtc)]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Processed);
        Assert.Equal(1, result.Value.Updated);
        Assert.Equal(0, result.Value.Failed);
        Assert.Empty(result.Value.Errors);
        UnitOfMeasure saved = await testDbContext.DbContext.UnitsOfMeasure.SingleAsync(
            x => x.ExternalRefKey == externalRefKey,
            TestContext.Current.CancellationToken);
        Assert.False(saved.IsActive);
        Assert.Equal("EA", saved.Code);
        Assert.Equal("Each", saved.Name);
        Assert.Equal("ea", saved.Symbol);
        Assert.Equal(deletionImportedAtUtc, saved.LastImportedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WhenEventDispatchFails_RollsBackWholeBatch()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        ImportUnitsOfMeasure.Handler handler = new(testDbContext.DbContext, new ThrowingDomainEventDispatcher());
        Guid externalRefKey = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            new ImportUnitsOfMeasure.Command(
                [new(externalRefKey, "ROLLBACK", "Rollback", "r", false, ImportedAtUtc)]),
            TestContext.Current.CancellationToken));

        await using var verificationContext = testDbContext.CreateDbContext();
        Assert.False(await verificationContext.UnitsOfMeasure.AnyAsync(
            x => x.ExternalRefKey == externalRefKey,
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
            Assert.True(unit!.ApplyImport(externalRefKey.Value, code, name, symbol, false, ImportedAtUtc).IsValid);
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
