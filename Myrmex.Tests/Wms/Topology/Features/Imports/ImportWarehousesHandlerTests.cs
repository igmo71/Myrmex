using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Events;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Features.Imports;
using Myrmex.Modules.Wms.Domain;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Features.Imports;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Topology.Features.Imports;

public sealed class ImportWarehousesHandlerTests
{
    private static readonly DateTimeOffset ImportedAtUtc = DateTimeOffset.Parse("2026-06-27T12:00:00Z");

    [Fact]
    public async Task HandleAsync_UpsertsByExternalIdentityAndReconcilesRecordOutcomes()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        Guid linkedKey = Guid.NewGuid();
        Warehouse linked = CreateWarehouse("OLD", "Old", linkedKey);
        linked.Deactivate();
        Warehouse local = CreateWarehouse("LOCAL", "Local");
        testDbContext.DbContext.Warehouses.AddRange(linked, local);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        testDbContext.DbContext.ChangeTracker.Clear();
        Guid createdKey = Guid.NewGuid();
        ImportWarehouses.Handler handler = new(
            testDbContext.DbContext,
            new RecordingDomainEventDispatcher());

        ServiceResult<ReferenceImportBatchResult> result = await handler.HandleAsync(
            new ImportWarehouses.Command(
            [
                new(linkedKey, [2], " linked ", "Linked Warehouse", false, ImportedAtUtc),
                new(createdKey, [1], createdKey.ToString("N").ToUpperInvariant(), "Generated Code", false, ImportedAtUtc),
                new(Guid.NewGuid(), [1], "local", "Must not link", false, ImportedAtUtc),
                new(Guid.NewGuid(), [1], "INVALID", null, false, ImportedAtUtc)
            ]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.Processed);
        Assert.Equal(1, result.Value.Created);
        Assert.Equal(1, result.Value.Updated);
        Assert.Equal(1, result.Value.Skipped);
        Assert.Equal(1, result.Value.Failed);
        Assert.True(result.Value.HasConsistentCounts);
        Assert.Contains(result.Value.Errors, error =>
            error.Reason == ReferenceImportRecordErrorReasons.CodeAlreadyExistsWithoutExternalRefKey);
        Assert.Contains(result.Value.Errors, error =>
            error.Reason == ReferenceImportRecordErrorReasons.InvalidSourceRecord);

        Warehouse updated = await testDbContext.DbContext.Warehouses.SingleAsync(
            x => x.ImportState != null && x.ImportState.RefKey == linkedKey,
            TestContext.Current.CancellationToken);
        Assert.Equal("LINKED", updated.Code);
        Assert.Equal("Linked Warehouse", updated.Name);
        Assert.True(updated.IsActive);
        Assert.Equal(ImportedAtUtc, updated.LastImportedAtUtc);
        Assert.Null((await testDbContext.DbContext.Warehouses.SingleAsync(
            x => x.Id == local.Id,
            TestContext.Current.CancellationToken)).ExternalRefKey);
    }

    [Fact]
    public async Task HandleAsync_WhenUnlinkedRecordIsDeletionMarked_SkipsAndReportsIt()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        Guid externalRefKey = Guid.NewGuid();
        ImportWarehouses.Handler handler = new(testDbContext.DbContext, new RecordingDomainEventDispatcher());

        ServiceResult<ReferenceImportBatchResult> result = await handler.HandleAsync(
            new ImportWarehouses.Command(
                [new(externalRefKey, [1], null, null, true, ImportedAtUtc)]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Processed);
        Assert.Equal(0, result.Value.Created);
        Assert.Equal(1, result.Value.Skipped);
        ReferenceImportRecordError error = Assert.Single(result.Value.Errors);
        Assert.Equal(externalRefKey, error.ExternalRefKey);
        Assert.Equal(ReferenceImportRecordErrorReasons.SourceRecordDeletionMarked, error.Reason);
        Assert.False(await testDbContext.DbContext.Warehouses.AnyAsync(
            x => x.ImportState != null && x.ImportState.RefKey == externalRefKey,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_WhenLinkedDeletionMarkedFieldsAreInvalid_DeactivatesWithoutUpdatingDetails()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        Guid externalRefKey = Guid.NewGuid();
        Warehouse linked = CreateWarehouse("WH", "Warehouse", externalRefKey);
        testDbContext.DbContext.Warehouses.Add(linked);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        testDbContext.DbContext.ChangeTracker.Clear();
        ImportWarehouses.Handler handler = new(testDbContext.DbContext, new RecordingDomainEventDispatcher());
        DateTimeOffset deletionImportedAtUtc = ImportedAtUtc.AddMinutes(5);

        ServiceResult<ReferenceImportBatchResult> result = await handler.HandleAsync(
            new ImportWarehouses.Command(
                [new(externalRefKey, [2], null, null, true, deletionImportedAtUtc)]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Processed);
        Assert.Equal(1, result.Value.Updated);
        Assert.Equal(0, result.Value.Failed);
        Assert.Empty(result.Value.Errors);
        Warehouse saved = await testDbContext.DbContext.Warehouses.SingleAsync(
            x => x.ImportState != null && x.ImportState.RefKey == externalRefKey,
            TestContext.Current.CancellationToken);
        Assert.False(saved.IsActive);
        Assert.Equal("WH", saved.Code);
        Assert.Equal("Warehouse", saved.Name);
        Assert.Equal(deletionImportedAtUtc, saved.LastImportedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WhenExternalIdentityIsImportedAgain_UpdatesWithoutCreatingDuplicate()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        ImportWarehouses.Handler handler = new(
            testDbContext.DbContext,
            new RecordingDomainEventDispatcher());
        Guid externalRefKey = Guid.NewGuid();
        DateTimeOffset repeatedAtUtc = ImportedAtUtc.AddMinutes(10);

        ServiceResult<ReferenceImportBatchResult> first = await handler.HandleAsync(
            new ImportWarehouses.Command(
                [new(externalRefKey, [1], "WH-1", "Warehouse 1", false, ImportedAtUtc)]),
            TestContext.Current.CancellationToken);
        Guid internalId = await testDbContext.DbContext.Warehouses
            .Where(warehouse => warehouse.ImportState != null && warehouse.ImportState.RefKey == externalRefKey)
            .Select(warehouse => warehouse.Id)
            .SingleAsync(TestContext.Current.CancellationToken);

        ServiceResult<ReferenceImportBatchResult> repeated = await handler.HandleAsync(
            new ImportWarehouses.Command(
                [new(externalRefKey, [2], "WH-2", "Warehouse 2", false, repeatedAtUtc)]),
            TestContext.Current.CancellationToken);

        Assert.True(first.IsSuccess);
        Assert.Equal(1, first.Value.Created);
        Assert.True(repeated.IsSuccess);
        Assert.Equal(0, repeated.Value.Created);
        Assert.Equal(1, repeated.Value.Updated);
        Warehouse saved = await testDbContext.DbContext.Warehouses.SingleAsync(
            warehouse => warehouse.ImportState != null && warehouse.ImportState.RefKey == externalRefKey,
            TestContext.Current.CancellationToken);
        Assert.Equal(internalId, saved.Id);
        Assert.Equal(externalRefKey, saved.ExternalRefKey);
        Assert.Equal("WH-2", saved.Code);
        Assert.Equal("Warehouse 2", saved.Name);
        Assert.Equal(repeatedAtUtc, saved.LastImportedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WhenDataVersionIsCurrent_ReturnsUnchangedWithoutMutationOrEvent()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        Guid externalRefKey = Guid.NewGuid();
        Warehouse linked = CreateWarehouse("WH", "Warehouse", externalRefKey);
        testDbContext.DbContext.Warehouses.Add(linked);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DateTimeOffset? updatedAtUtc = linked.UpdatedAtUtc;
        testDbContext.DbContext.ChangeTracker.Clear();
        RecordingDomainEventDispatcher dispatcher = new();
        ImportWarehouses.Handler handler = new(testDbContext.DbContext, dispatcher);

        ServiceResult<ReferenceImportBatchResult> result = await handler.HandleAsync(
            new ImportWarehouses.Command(
                [new(externalRefKey, [1], "CHANGED", "Changed", true, ImportedAtUtc.AddHours(1))]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Unchanged);
        Assert.Equal(0, result.Value.Updated);
        Warehouse saved = await testDbContext.DbContext.Warehouses.SingleAsync(
            x => x.ImportState != null && x.ImportState.RefKey == externalRefKey,
            TestContext.Current.CancellationToken);
        Assert.Equal("WH", saved.Code);
        Assert.Equal("Warehouse", saved.Name);
        Assert.True(saved.IsActive);
        Assert.Equal(ImportedAtUtc, saved.LastImportedAtUtc);
        Assert.Equal(updatedAtUtc, saved.UpdatedAtUtc);
        Assert.Empty(saved.DomainEvents);
        Assert.Empty(dispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task HandleAsync_WhenLegacyVersionIsUnknown_AppliesCurrentSourceVersion()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        Guid externalRefKey = Guid.NewGuid();
        Assert.True(Warehouse.Create("OLD", "Old", null, out Warehouse? linked).IsValid);
        linked!.ImportState = ExternalImportState.Restore(externalRefKey, null, ImportedAtUtc);
        linked.ClearDomainEvents();
        testDbContext.DbContext.Warehouses.Add(linked);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        testDbContext.DbContext.ChangeTracker.Clear();
        ImportWarehouses.Handler handler = new(testDbContext.DbContext, new RecordingDomainEventDispatcher());

        ServiceResult<ReferenceImportBatchResult> result = await handler.HandleAsync(
            new ImportWarehouses.Command(
                [new(externalRefKey, [2], "NEW", "New", false, ImportedAtUtc.AddMinutes(1))]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Updated);
        Warehouse saved = await testDbContext.DbContext.Warehouses.SingleAsync(
            x => x.ImportState != null && x.ImportState.RefKey == externalRefKey,
            TestContext.Current.CancellationToken);
        Assert.Equal(new byte[] { 2 }, saved.ExternalDataVersion);
        Assert.Equal("NEW", saved.Code);
    }

    [Fact]
    public async Task HandleAsync_WhenEventDispatchFails_RollsBackWholeBatch()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        ImportWarehouses.Handler handler = new(testDbContext.DbContext, new ThrowingDomainEventDispatcher());
        Guid externalRefKey = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            new ImportWarehouses.Command(
                [new(externalRefKey, [1], "ROLLBACK", "Rollback", false, ImportedAtUtc)]),
            TestContext.Current.CancellationToken));

        await using var verificationContext = testDbContext.CreateDbContext();
        Assert.False(await verificationContext.Warehouses.AnyAsync(
            x => x.ImportState != null && x.ImportState.RefKey == externalRefKey,
            TestContext.Current.CancellationToken));
    }

    private static Warehouse CreateWarehouse(string code, string name, Guid? externalRefKey = null)
    {
        Assert.True(Warehouse.Create(code, name, null, out Warehouse? warehouse).IsValid);
        if (externalRefKey.HasValue)
        {
            Assert.True(warehouse!.ApplyImport(externalRefKey.Value, [1], code, name, false, ImportedAtUtc).IsValid);
        }
        warehouse!.ClearDomainEvents();
        return warehouse;
    }

    private sealed class ThrowingDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException("Dispatch failed."));

        public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException("Dispatch failed."));
    }
}
