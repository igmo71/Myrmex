using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Events;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Features.Imports;
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
                new(linkedKey, " linked ", "Linked Warehouse", false, ImportedAtUtc),
                new(createdKey, createdKey.ToString("N").ToUpperInvariant(), "Generated Code", false, ImportedAtUtc),
                new(Guid.NewGuid(), "local", "Must not link", false, ImportedAtUtc),
                new(Guid.NewGuid(), "INVALID", null, false, ImportedAtUtc)
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
            x => x.ExternalRefKey == linkedKey,
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
                [new(externalRefKey, null, null, true, ImportedAtUtc)]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Processed);
        Assert.Equal(0, result.Value.Created);
        Assert.Equal(1, result.Value.Skipped);
        ReferenceImportRecordError error = Assert.Single(result.Value.Errors);
        Assert.Equal(externalRefKey, error.ExternalRefKey);
        Assert.Equal(ReferenceImportRecordErrorReasons.SourceRecordDeletionMarked, error.Reason);
        Assert.False(await testDbContext.DbContext.Warehouses.AnyAsync(
            x => x.ExternalRefKey == externalRefKey,
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
                [new(externalRefKey, null, null, true, deletionImportedAtUtc)]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Processed);
        Assert.Equal(1, result.Value.Updated);
        Assert.Equal(0, result.Value.Failed);
        Assert.Empty(result.Value.Errors);
        Warehouse saved = await testDbContext.DbContext.Warehouses.SingleAsync(
            x => x.ExternalRefKey == externalRefKey,
            TestContext.Current.CancellationToken);
        Assert.False(saved.IsActive);
        Assert.Equal("WH", saved.Code);
        Assert.Equal("Warehouse", saved.Name);
        Assert.Equal(deletionImportedAtUtc, saved.LastImportedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WhenEventDispatchFails_RollsBackWholeBatch()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        ImportWarehouses.Handler handler = new(testDbContext.DbContext, new ThrowingDomainEventDispatcher());
        Guid externalRefKey = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            new ImportWarehouses.Command(
                [new(externalRefKey, "ROLLBACK", "Rollback", false, ImportedAtUtc)]),
            TestContext.Current.CancellationToken));

        await using var verificationContext = testDbContext.CreateDbContext();
        Assert.False(await verificationContext.Warehouses.AnyAsync(
            x => x.ExternalRefKey == externalRefKey,
            TestContext.Current.CancellationToken));
    }

    private static Warehouse CreateWarehouse(string code, string name, Guid? externalRefKey = null)
    {
        Assert.True(Warehouse.Create(code, name, null, out Warehouse? warehouse).IsValid);
        if (externalRefKey.HasValue)
        {
            Assert.True(warehouse!.ApplyImport(externalRefKey.Value, code, name, false, ImportedAtUtc).IsValid);
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
