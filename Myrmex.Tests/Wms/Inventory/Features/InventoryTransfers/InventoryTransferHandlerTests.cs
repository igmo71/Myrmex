using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransfers;
using Myrmex.Modules.Wms.Inventory.Features.InventoryTransfers;
using Myrmex.Shared.Wms.Inventory;
using Myrmex.Tests.Wms.Inventory.Testing;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Inventory.Features.InventoryTransfers;

public sealed class InventoryTransferHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenSameWarehouseTransferIsValid_CreatesTransferWithLinesAndCreatedStatus()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryTransferReferences references = await InventoryTransferTestData
            .SeedReferencesAsync(testDbContext.DbContext);
        CreateInventoryTransfer.Handler handler = new(testDbContext.DbContext);

        CreateInventoryTransfer.Command command = new(
            references.Warehouse.Id,
            references.Warehouse.Id,
            TransitStorageLocationId: null,
            [
                new CreateInventoryTransfer.Line(
                    references.StockKeepingUnit.Id,
                    references.SourceStorageLocation.Id,
                    references.DestinationStorageLocation.Id,
                    RequestedQuantity: 5)
            ]);

        ServiceResult<InventoryTransferDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(InventoryTransferStatusDetails.Created, result.Value.Status);
        Assert.Null(result.Value.TransitStorageLocation);
        InventoryTransferLineDetails line = Assert.Single(result.Value.Lines);
        Assert.Equal(references.StockKeepingUnit.Id, line.Sku.Id);
        Assert.Equal(references.SourceStorageLocation.Id, line.SourceStorageLocation.Id);
        Assert.Equal(references.DestinationStorageLocation.Id, line.DestinationStorageLocation.Id);
        Assert.Equal(5, line.RequestedQuantity);
        Assert.Empty(result.Value.Movements);

        InventoryTransfer saved = await testDbContext.DbContext.InventoryTransfers
            .Include(x => x.Lines)
            .SingleAsync(x => x.Id == result.Value.Id, TestContext.Current.CancellationToken);

        Assert.Equal(InventoryTransferStatus.Created, saved.Status);
        Assert.Null(saved.TransitStorageLocationId);
        Assert.Single(saved.Lines);
    }

    [Fact]
    public async Task HandleAsync_WhenWarehousesDiffer_RejectsExternalTransfer()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        CreateInventoryTransfer.Handler handler = new(testDbContext.DbContext);

        CreateInventoryTransfer.Command command = new(
            Guid.Parse("018f0000-0000-7000-8000-000000000301"),
            Guid.Parse("018f0000-0000-7000-8000-000000000302"),
            TransitStorageLocationId: null,
            [
                new CreateInventoryTransfer.Line(
                    Guid.Parse("018f0000-0000-7000-8000-000000000101"),
                    Guid.Parse("018f0000-0000-7000-8000-000000000201"),
                    Guid.Parse("018f0000-0000-7000-8000-000000000202"),
                    RequestedQuantity: 5)
            ]);

        ServiceResult<InventoryTransferDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Contains(result.Error.DetailList, error =>
            error.Property == nameof(CreateInventoryTransfer.Command.DestinationWarehouseId) &&
            error.Code == "Unsupported-InventoryTransfer-DestinationWarehouseId");
    }

    [Fact]
    public async Task HandleAsync_ValidatesActiveReferencesInternalTransitRegularLocationsAndPositiveQuantity()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryTransferReferences references = await InventoryTransferTestData
            .SeedReferencesAsync(testDbContext.DbContext);
        CreateInventoryTransfer.Handler handler = new(testDbContext.DbContext);

        CreateInventoryTransfer.Command validTransitCommand = new(
            references.Warehouse.Id,
            references.Warehouse.Id,
            references.InternalTransitStorageLocation.Id,
            [
                new CreateInventoryTransfer.Line(
                    references.StockKeepingUnit.Id,
                    references.SourceStorageLocation.Id,
                    references.DestinationStorageLocation.Id,
                    RequestedQuantity: 5)
            ]);

        ServiceResult<InventoryTransferDetails> validTransitResult = await handler.HandleAsync(
            validTransitCommand,
            TestContext.Current.CancellationToken);

        Assert.True(validTransitResult.IsSuccess);
        Assert.Equal(references.InternalTransitStorageLocation.Id, validTransitResult.Value.TransitStorageLocation?.Id);

        CreateInventoryTransfer.Command invalidTransitCommand = validTransitCommand with
        {
            TransitStorageLocationId = references.ExternalTransitStorageLocation.Id
        };

        ServiceResult<InventoryTransferDetails> externalTransitResult = await handler.HandleAsync(
            invalidTransitCommand,
            TestContext.Current.CancellationToken);

        Assert.False(externalTransitResult.IsSuccess);
        Assert.Equal(ServiceErrorType.Invalid, externalTransitResult.Error?.Type);
        Assert.Equal(nameof(CreateInventoryTransfer.Command.TransitStorageLocationId), externalTransitResult.Error?.Property);

        CreateInventoryTransfer.Command invalidQuantityCommand = validTransitCommand with
        {
            Lines =
            [
                new CreateInventoryTransfer.Line(
                    references.StockKeepingUnit.Id,
                    references.SourceStorageLocation.Id,
                    references.DestinationStorageLocation.Id,
                    RequestedQuantity: 0)
            ]
        };

        ServiceResult<InventoryTransferDetails> invalidQuantityResult = await handler.HandleAsync(
            invalidQuantityCommand,
            TestContext.Current.CancellationToken);

        Assert.False(invalidQuantityResult.IsSuccess);
        Assert.Contains(invalidQuantityResult.Error!.DetailList, error =>
            error.Property == nameof(CreateInventoryTransfer.Line.RequestedQuantity));
    }
}
