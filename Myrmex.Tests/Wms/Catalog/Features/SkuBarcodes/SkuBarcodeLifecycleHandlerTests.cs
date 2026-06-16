using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.SkuBarcodes;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Catalog.Features.SkuBarcodes;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Catalog.Features.SkuBarcodes;

public sealed class SkuBarcodeLifecycleHandlerTests
{

    [Fact]
    public async Task DeactivateHandleAsync_WhenBarcodeIsPrimary_ClearsOnlyTargetPrimaryAndPromotesNoOtherBarcode()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);
        SkuBarcode primaryBarcode = await AddSkuBarcodeAsync(
            testDbContext,
            stockKeepingUnit.Id,
            "PRIMARY",
            isPrimary: true);
        SkuBarcode otherBarcode = await AddSkuBarcodeAsync(
            testDbContext,
            stockKeepingUnit.Id,
            "OTHER",
            isPrimary: false);

        DeactivateSkuBarcode.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        DeactivateSkuBarcode.Command command = new(primaryBarcode.Id);

        ServiceResult<SkuBarcodeDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);
        Assert.False(result.Value.IsPrimary);
        Assert.NotNull(result.Value.UpdatedAtUtc);

        SkuBarcode refreshedOtherBarcode = await testDbContext.DbContext.SkuBarcodes
            .SingleAsync(x => x.Id == otherBarcode.Id, TestContext.Current.CancellationToken);

        Assert.True(refreshedOtherBarcode.IsActive);
        Assert.False(refreshedOtherBarcode.IsPrimary);

        int activePrimaryCount = await testDbContext.DbContext.SkuBarcodes
            .CountAsync(
                x => x.StockKeepingUnitId == stockKeepingUnit.Id &&
                     x.IsActive &&
                     x.IsPrimary,
                TestContext.Current.CancellationToken);

        Assert.Equal(0, activePrimaryCount);

        var dispatchedEvent = Assert.Single(domainEventDispatcher.DispatchedEvents);
        Assert.IsType<SkuBarcodeDeactivatedDomainEvent>(dispatchedEvent);
    }

    [Fact]
    public async Task DeactivateHandleAsync_WhenSkuBarcodeIsAlreadyInactive_ReturnsSuccessWithoutDispatchingNewDomainEvent()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);
        SkuBarcode skuBarcode = await AddSkuBarcodeAsync(testDbContext, stockKeepingUnit.Id, "INACTIVE");
        skuBarcode.Deactivate();
        DateTimeOffset? updatedAtUtc = skuBarcode.UpdatedAtUtc;
        skuBarcode.ClearDomainEvents();
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        DeactivateSkuBarcode.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        DeactivateSkuBarcode.Command command = new(skuBarcode.Id);

        ServiceResult<SkuBarcodeDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);
        Assert.False(result.Value.IsPrimary);
        Assert.Equal(updatedAtUtc, result.Value.UpdatedAtUtc);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task ReactivateHandleAsync_WhenSkuBarcodeIsInactive_ReactivatesAsNonPrimary()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);
        SkuBarcode skuBarcode = await AddSkuBarcodeAsync(
            testDbContext,
            stockKeepingUnit.Id,
            "REACTIVATE",
            isPrimary: true);
        skuBarcode.Deactivate();
        skuBarcode.ClearDomainEvents();
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        ReactivateSkuBarcode.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ReactivateSkuBarcode.Command command = new(skuBarcode.Id);

        ServiceResult<SkuBarcodeDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);
        Assert.False(result.Value.IsPrimary);
        Assert.NotNull(result.Value.UpdatedAtUtc);

        SkuBarcode persistedSkuBarcode = await testDbContext.DbContext.SkuBarcodes.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.True(persistedSkuBarcode.IsActive);
        Assert.False(persistedSkuBarcode.IsPrimary);

        var dispatchedEvent = Assert.Single(domainEventDispatcher.DispatchedEvents);
        Assert.IsType<SkuBarcodeReactivatedDomainEvent>(dispatchedEvent);
    }

    [Fact]
    public async Task ReactivateHandleAsync_WhenSkuBarcodeIsAlreadyActive_ReturnsSuccessWithoutDispatchingNewDomainEvent()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        StockKeepingUnit stockKeepingUnit = await AddStockKeepingUnitAsync(testDbContext);
        SkuBarcode skuBarcode = await AddSkuBarcodeAsync(
            testDbContext,
            stockKeepingUnit.Id,
            "ACTIVE",
            isPrimary: true);
        DateTimeOffset? updatedAtUtc = skuBarcode.UpdatedAtUtc;

        ReactivateSkuBarcode.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ReactivateSkuBarcode.Command command = new(skuBarcode.Id);

        ServiceResult<SkuBarcodeDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);
        Assert.True(result.Value.IsPrimary);
        Assert.Equal(updatedAtUtc, result.Value.UpdatedAtUtc);
        Assert.Empty(domainEventDispatcher.DispatchedEvents);
    }

    private static async Task<StockKeepingUnit> AddStockKeepingUnitAsync(TestWmsDbContext testDbContext)
    {
        UnitOfMeasure baseUnitOfMeasure = CreateUnitOfMeasure();

        testDbContext.DbContext.UnitsOfMeasure.Add(baseUnitOfMeasure);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = StockKeepingUnit.Create(
            code: "ITEM-001",
            name: "Widget",
            description: null,
            baseUnitOfMeasureId: baseUnitOfMeasure.Id,
            out StockKeepingUnit? stockKeepingUnit);

        Assert.True(result.IsValid);
        Assert.NotNull(stockKeepingUnit);

        testDbContext.DbContext.StockKeepingUnits.Add(stockKeepingUnit);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        stockKeepingUnit.ClearDomainEvents();

        return stockKeepingUnit;
    }

    private static UnitOfMeasure CreateUnitOfMeasure()
    {
        var result = UnitOfMeasure.Create(
            code: "EA",
            name: "Each",
            symbol: "ea",
            out UnitOfMeasure? unitOfMeasure);

        Assert.True(result.IsValid);
        Assert.NotNull(unitOfMeasure);

        unitOfMeasure.ClearDomainEvents();

        return unitOfMeasure;
    }

    private static async Task<SkuBarcode> AddSkuBarcodeAsync(
        TestWmsDbContext testDbContext,
        Guid stockKeepingUnitId,
        string value,
        bool isPrimary = false)
    {
        var result = SkuBarcode.Create(
            stockKeepingUnitId,
            value,
            BarcodeSymbology.Code128,
            isPrimary,
            out SkuBarcode? skuBarcode);

        Assert.True(result.IsValid);
        Assert.NotNull(skuBarcode);

        skuBarcode.ClearDomainEvents();
        testDbContext.DbContext.SkuBarcodes.Add(skuBarcode);
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return skuBarcode;
    }
}
