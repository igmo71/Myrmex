using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Events;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.Imports;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Features.Imports;

public sealed class ImportStockKeepingUnitsHandlerTests
{
    private static readonly DateTimeOffset ImportedAtUtc = DateTimeOffset.Parse("2026-06-27T12:00:00Z");

    [Fact]
    public async Task HandleAsync_ResolvesEachBaseUnitOnlyByExternalRefKeyAndUpsertsSkus()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        Guid eachExternalKey = Guid.NewGuid();
        Guid packageExternalKey = Guid.NewGuid();
        UnitOfMeasure each = CreateUnit("EA", "Each", eachExternalKey);
        UnitOfMeasure package = CreateUnit("PKG", "Package", packageExternalKey);
        Guid linkedExternalKey = Guid.NewGuid();
        StockKeepingUnit linked = CreateSku("OLD", "Old", each.Id, linkedExternalKey);
        StockKeepingUnit local = CreateSku("LOCAL", "Local", each.Id);
        testDbContext.DbContext.AddRange(each, package, linked, local);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        testDbContext.DbContext.ChangeTracker.Clear();
        ImportStockKeepingUnits.Handler handler = new(
            testDbContext.DbContext,
            new RecordingDomainEventDispatcher());
        Guid createdExternalKey = Guid.NewGuid();

        ServiceResult<ReferenceImportBatchResult> result = await handler.HandleAsync(
            new ImportStockKeepingUnits.Command(
            [
                new(linkedExternalKey, [2], " updated ", "Updated", packageExternalKey, false, ImportedAtUtc),
                new(createdExternalKey, [1], " new ", "New", eachExternalKey, false, ImportedAtUtc),
                new(Guid.NewGuid(), [1], "local", "Must not link", eachExternalKey, false, ImportedAtUtc)
            ]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Processed);
        Assert.Equal(1, result.Value.Created);
        Assert.Equal(1, result.Value.Updated);
        Assert.Equal(1, result.Value.Skipped);
        StockKeepingUnit updated = await testDbContext.DbContext.StockKeepingUnits.SingleAsync(
            sku => sku.ImportState != null && sku.ImportState.RefKey == linkedExternalKey,
            TestContext.Current.CancellationToken);
        Assert.Equal("UPDATED", updated.Code);
        Assert.Equal(package.Id, updated.BaseUnitOfMeasureId);
        Assert.Equal("Local description", updated.Description);
        Assert.Null((await testDbContext.DbContext.StockKeepingUnits.SingleAsync(
            sku => sku.Id == local.Id,
            TestContext.Current.CancellationToken)).ExternalRefKey);
    }

    [Fact]
    public async Task HandleAsync_ClassifiesMissingNotImportedAndInactiveBaseUnitsWithoutCodeFallback()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        Guid activeExternalKey = Guid.NewGuid();
        UnitOfMeasure active = CreateUnit("MATCHING-CODE", "Matching Code", activeExternalKey);
        Guid inactiveExternalKey = Guid.NewGuid();
        UnitOfMeasure inactive = CreateUnit("INACTIVE", "Inactive", inactiveExternalKey);
        inactive.Deactivate();
        testDbContext.DbContext.UnitsOfMeasure.AddRange(active, inactive);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        testDbContext.DbContext.ChangeTracker.Clear();
        ImportStockKeepingUnits.Handler handler = new(
            testDbContext.DbContext,
            new RecordingDomainEventDispatcher());

        ServiceResult<ReferenceImportBatchResult> result = await handler.HandleAsync(
            new ImportStockKeepingUnits.Command(
            [
                new(Guid.NewGuid(), [1], "SKU-NULL", "Null", null, false, ImportedAtUtc),
                new(Guid.NewGuid(), [1], "SKU-EMPTY", "Empty", Guid.Empty, false, ImportedAtUtc),
                new(Guid.NewGuid(), [1], "MATCHING-CODE", "Unknown external key", Guid.NewGuid(), false, ImportedAtUtc),
                new(Guid.NewGuid(), [1], "SKU-INACTIVE", "Inactive", inactiveExternalKey, false, ImportedAtUtc)
            ]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.Failed);
        Assert.Equal(2, result.Value.Errors.Count(error =>
            error.Reason == ReferenceImportRecordErrorReasons.BaseUnitOfMeasureExternalRefKeyMissing));
        Assert.Contains(result.Value.Errors, error =>
            error.Reason == ReferenceImportRecordErrorReasons.BaseUnitOfMeasureNotImported);
        Assert.Contains(result.Value.Errors, error =>
            error.Reason == ReferenceImportRecordErrorReasons.BaseUnitOfMeasureInactive);
        Assert.Empty(await testDbContext.DbContext.StockKeepingUnits.ToListAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_AppliesDeletionSemanticsBeforeDetailAndBaseUnitValidation()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        Guid unitExternalKey = Guid.NewGuid();
        UnitOfMeasure unit = CreateUnit("EA", "Each", unitExternalKey);
        Guid linkedExternalKey = Guid.NewGuid();
        StockKeepingUnit linked = CreateSku("SKU", "Linked", unit.Id, linkedExternalKey);
        testDbContext.DbContext.AddRange(unit, linked);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        testDbContext.DbContext.ChangeTracker.Clear();
        ImportStockKeepingUnits.Handler handler = new(
            testDbContext.DbContext,
            new RecordingDomainEventDispatcher());
        Guid unlinkedExternalKey = Guid.NewGuid();
        DateTimeOffset deletionImportedAtUtc = ImportedAtUtc.AddMinutes(5);

        ServiceResult<ReferenceImportBatchResult> result = await handler.HandleAsync(
            new ImportStockKeepingUnits.Command(
            [
                new(linkedExternalKey, [2], null, null, null, true, deletionImportedAtUtc),
                new(unlinkedExternalKey, [1], null, null, null, true, deletionImportedAtUtc)
            ]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Processed);
        Assert.Equal(1, result.Value.Updated);
        Assert.Equal(1, result.Value.Skipped);
        Assert.Equal(0, result.Value.Failed);
        ReferenceImportRecordError error = Assert.Single(result.Value.Errors);
        Assert.Equal(unlinkedExternalKey, error.ExternalRefKey);
        Assert.Equal(ReferenceImportRecordErrorReasons.SourceRecordDeletionMarked, error.Reason);
        StockKeepingUnit saved = await testDbContext.DbContext.StockKeepingUnits.SingleAsync(
            sku => sku.ImportState != null && sku.ImportState.RefKey == linkedExternalKey,
            TestContext.Current.CancellationToken);
        Assert.False(saved.IsActive);
        Assert.Equal("SKU", saved.Code);
        Assert.Equal("Linked", saved.Name);
        Assert.Equal(unit.Id, saved.BaseUnitOfMeasureId);
        Assert.Equal("Local description", saved.Description);
        Assert.Equal(deletionImportedAtUtc, saved.LastImportedAtUtc);
        Assert.False(await testDbContext.DbContext.StockKeepingUnits.AnyAsync(
            sku => sku.ImportState != null && sku.ImportState.RefKey == unlinkedExternalKey,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_WhenExternalIdentityIsImportedAgain_UpdatesWithoutCreatingDuplicate()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        Guid unitExternalRefKey = Guid.NewGuid();
        UnitOfMeasure unit = CreateUnit("EA", "Each", unitExternalRefKey);
        testDbContext.DbContext.UnitsOfMeasure.Add(unit);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        ImportStockKeepingUnits.Handler handler = new(
            testDbContext.DbContext,
            new RecordingDomainEventDispatcher());
        Guid externalRefKey = Guid.NewGuid();
        DateTimeOffset repeatedAtUtc = ImportedAtUtc.AddMinutes(10);

        ServiceResult<ReferenceImportBatchResult> first = await handler.HandleAsync(
            new ImportStockKeepingUnits.Command(
                [new(externalRefKey, [1], "SKU-1", "SKU 1", unitExternalRefKey, false, ImportedAtUtc)]),
            TestContext.Current.CancellationToken);
        Guid internalId = await testDbContext.DbContext.StockKeepingUnits
            .Where(sku => sku.ImportState != null && sku.ImportState.RefKey == externalRefKey)
            .Select(sku => sku.Id)
            .SingleAsync(TestContext.Current.CancellationToken);

        ServiceResult<ReferenceImportBatchResult> repeated = await handler.HandleAsync(
            new ImportStockKeepingUnits.Command(
                [new(externalRefKey, [2], "SKU-2", "SKU 2", unitExternalRefKey, false, repeatedAtUtc)]),
            TestContext.Current.CancellationToken);

        Assert.True(first.IsSuccess);
        Assert.Equal(1, first.Value.Created);
        Assert.True(repeated.IsSuccess);
        Assert.Equal(0, repeated.Value.Created);
        Assert.Equal(1, repeated.Value.Updated);
        StockKeepingUnit saved = await testDbContext.DbContext.StockKeepingUnits.SingleAsync(
            sku => sku.ImportState != null && sku.ImportState.RefKey == externalRefKey,
            TestContext.Current.CancellationToken);
        Assert.Equal(internalId, saved.Id);
        Assert.Equal(externalRefKey, saved.ExternalRefKey);
        Assert.Equal("SKU-2", saved.Code);
        Assert.Equal("SKU 2", saved.Name);
        Assert.Equal(unit.Id, saved.BaseUnitOfMeasureId);
        Assert.Equal(repeatedAtUtc, saved.LastImportedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_WhenDataVersionIsCurrent_ReturnsUnchangedWithoutResolvingNewBaseUnitOrMutating()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        Guid unitExternalKey = Guid.NewGuid();
        UnitOfMeasure unit = CreateUnit("EA", "Each", unitExternalKey);
        Guid skuExternalKey = Guid.NewGuid();
        StockKeepingUnit linked = CreateSku("SKU", "Stock item", unit.Id, skuExternalKey);
        testDbContext.DbContext.AddRange(unit, linked);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        DateTimeOffset? updatedAtUtc = linked.UpdatedAtUtc;
        testDbContext.DbContext.ChangeTracker.Clear();
        RecordingDomainEventDispatcher dispatcher = new();
        ImportStockKeepingUnits.Handler handler = new(testDbContext.DbContext, dispatcher);

        ServiceResult<ReferenceImportBatchResult> result = await handler.HandleAsync(
            new ImportStockKeepingUnits.Command(
                [new(skuExternalKey, [1], "CHANGED", "Changed", Guid.NewGuid(), true, ImportedAtUtc.AddHours(1))]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Unchanged);
        Assert.Equal(0, result.Value.Updated);
        StockKeepingUnit saved = await testDbContext.DbContext.StockKeepingUnits.SingleAsync(
            sku => sku.ImportState != null && sku.ImportState.RefKey == skuExternalKey,
            TestContext.Current.CancellationToken);
        Assert.Equal("SKU", saved.Code);
        Assert.Equal("Stock item", saved.Name);
        Assert.Equal("Local description", saved.Description);
        Assert.Equal(unit.Id, saved.BaseUnitOfMeasureId);
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
        Guid unitExternalKey = Guid.NewGuid();
        UnitOfMeasure unit = CreateUnit("EA", "Each", unitExternalKey);
        testDbContext.DbContext.UnitsOfMeasure.Add(unit);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        testDbContext.DbContext.ChangeTracker.Clear();
        ImportStockKeepingUnits.Handler handler = new(
            testDbContext.DbContext,
            new ThrowingDomainEventDispatcher());
        Guid skuExternalKey = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            new ImportStockKeepingUnits.Command(
                [new(skuExternalKey, [1], "ROLLBACK", "Rollback", unitExternalKey, false, ImportedAtUtc)]),
            TestContext.Current.CancellationToken));

        await using var verificationContext = testDbContext.CreateDbContext();
        Assert.False(await verificationContext.StockKeepingUnits.AnyAsync(
            sku => sku.ImportState != null && sku.ImportState.RefKey == skuExternalKey,
            TestContext.Current.CancellationToken));
    }

    private static UnitOfMeasure CreateUnit(string code, string name, Guid externalRefKey)
    {
        Assert.True(UnitOfMeasure.Create(code, name, code, out UnitOfMeasure? unit).IsValid);
        Assert.True(unit!.ApplyImport(externalRefKey, [1], code, name, code, false, ImportedAtUtc).IsValid);
        unit.ClearDomainEvents();
        return unit;
    }

    private static StockKeepingUnit CreateSku(
        string code,
        string name,
        Guid baseUnitOfMeasureId,
        Guid? externalRefKey = null)
    {
        Assert.True(StockKeepingUnit.Create(
            code, name, "Local description", baseUnitOfMeasureId, out StockKeepingUnit? sku).IsValid);
        if (externalRefKey.HasValue)
        {
            Assert.True(sku!.ApplyImport(
                externalRefKey.Value, [1], code, name, baseUnitOfMeasureId, false, ImportedAtUtc).IsValid);
        }
        sku!.ClearDomainEvents();
        return sku;
    }

    private sealed class ThrowingDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException("Dispatch failed."));

        public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException("Dispatch failed."));
    }
}
