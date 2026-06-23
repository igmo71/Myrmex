using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;
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

    [Fact]
    public async Task MoveAsync_WhenDirectMovementIsValid_CreatesMovementTransactionLedgerAndUpdatesBalances()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        SeededInventoryTransferReferences references = await InventoryTransferTestData
            .SeedReferencesAsync(testDbContext.DbContext);
        InventoryTransferDetails transfer = await CreateTransferAsync(
            testDbContext.DbContext,
            references,
            transitStorageLocationId: null,
            requestedQuantity: 5);
        await SeedBalanceAsync(
            testDbContext.DbContext,
            references.StockKeepingUnit.Id,
            references.SourceStorageLocation.Id,
            quantity: 10);
        await SeedBalanceAsync(
            testDbContext.DbContext,
            references.StockKeepingUnit.Id,
            references.DestinationStorageLocation.Id,
            quantity: 2);
        testDbContext.DbContext.ChangeTracker.Clear();

        MoveInventoryTransferLine.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ServiceResult<InventoryTransferDetails> result = await handler.HandleAsync(
            new MoveInventoryTransferLine.Command(
                transfer.Id,
                transfer.Lines[0].Id,
                Quantity: 3),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(InventoryTransferStatusDetails.InProgress, result.Value.Status);

        InventoryTransferLineDetails line = Assert.Single(result.Value.Lines);
        Assert.Equal(3, line.MovedQuantity);
        Assert.Equal(3, line.PickedQuantity);
        Assert.Equal(3, line.PlacedQuantity);
        Assert.Equal(0, line.InTransitQuantity);

        InventoryTransferMovementDetails movement = Assert.Single(result.Value.Movements);
        Assert.Equal(line.Id, movement.LineId);
        Assert.Equal(references.StockKeepingUnit.Id, movement.Sku.Id);
        Assert.Equal(references.SourceStorageLocation.Id, movement.FromStorageLocation.Id);
        Assert.Equal(references.DestinationStorageLocation.Id, movement.ToStorageLocation.Id);
        Assert.Equal(3, movement.Quantity);

        InventoryTransaction transaction = await testDbContext.DbContext.InventoryTransactions
            .Include(x => x.Entries)
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(InventoryTransactionType.Transfer, transaction.TransactionType);
        Assert.Equal(2, transaction.Entries.Count);

        InventoryLedgerEntry sourceEntry = transaction.Entries.Single(x =>
            x.StorageLocationId == references.SourceStorageLocation.Id);
        Assert.Equal(references.StockKeepingUnit.Id, sourceEntry.StockKeepingUnitId);
        Assert.Equal(10, sourceEntry.BalanceBefore);
        Assert.Equal(-3, sourceEntry.QuantityDelta);
        Assert.Equal(7, sourceEntry.BalanceAfter);

        InventoryLedgerEntry destinationEntry = transaction.Entries.Single(x =>
            x.StorageLocationId == references.DestinationStorageLocation.Id);
        Assert.Equal(references.StockKeepingUnit.Id, destinationEntry.StockKeepingUnitId);
        Assert.Equal(2, destinationEntry.BalanceBefore);
        Assert.Equal(3, destinationEntry.QuantityDelta);
        Assert.Equal(5, destinationEntry.BalanceAfter);

        InventoryBalance sourceBalance = await testDbContext.DbContext.InventoryBalances.SingleAsync(
            x => x.StockKeepingUnitId == references.StockKeepingUnit.Id &&
                 x.StorageLocationId == references.SourceStorageLocation.Id,
            TestContext.Current.CancellationToken);
        Assert.Equal(7, sourceBalance.Quantity);

        InventoryBalance destinationBalance = await testDbContext.DbContext.InventoryBalances.SingleAsync(
            x => x.StockKeepingUnitId == references.StockKeepingUnit.Id &&
                 x.StorageLocationId == references.DestinationStorageLocation.Id,
            TestContext.Current.CancellationToken);
        Assert.Equal(5, destinationBalance.Quantity);

        InventoryTransferMovement savedMovement = await testDbContext.DbContext.InventoryTransferMovements
            .SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(transaction.Id, savedMovement.InventoryTransactionId);
    }

    [Fact]
    public async Task MoveAsync_WhenDirectMoveExceedsRemainingQuantity_RejectsWithoutPersistence()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryTransferReferences references = await InventoryTransferTestData
            .SeedReferencesAsync(testDbContext.DbContext);
        InventoryTransferDetails transfer = await CreateTransferAsync(
            testDbContext.DbContext,
            references,
            transitStorageLocationId: null,
            requestedQuantity: 5);
        await SeedBalanceAsync(
            testDbContext.DbContext,
            references.StockKeepingUnit.Id,
            references.SourceStorageLocation.Id,
            quantity: 20);
        testDbContext.DbContext.ChangeTracker.Clear();
        MoveInventoryTransferLine.Handler handler = new(
            testDbContext.DbContext,
            new RecordingDomainEventDispatcher());

        ServiceResult<InventoryTransferDetails> result = await handler.HandleAsync(
            new MoveInventoryTransferLine.Command(
                transfer.Id,
                transfer.Lines[0].Id,
                Quantity: 6),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Conflict, result.Error?.Type);
        Assert.Equal("InventoryTransfer.OverMove", result.Error?.Code);
        Assert.Equal(0, await testDbContext.DbContext.InventoryTransferMovements.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await testDbContext.DbContext.InventoryTransactions.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await testDbContext.DbContext.InventoryLedgerEntries.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MoveAsync_WhenSourceBalanceIsInsufficient_RejectsWithoutPersistence()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryTransferReferences references = await InventoryTransferTestData
            .SeedReferencesAsync(testDbContext.DbContext);
        InventoryTransferDetails transfer = await CreateTransferAsync(
            testDbContext.DbContext,
            references,
            transitStorageLocationId: null,
            requestedQuantity: 5);
        await SeedBalanceAsync(
            testDbContext.DbContext,
            references.StockKeepingUnit.Id,
            references.SourceStorageLocation.Id,
            quantity: 2);
        testDbContext.DbContext.ChangeTracker.Clear();
        MoveInventoryTransferLine.Handler handler = new(
            testDbContext.DbContext,
            new RecordingDomainEventDispatcher());

        ServiceResult<InventoryTransferDetails> result = await handler.HandleAsync(
            new MoveInventoryTransferLine.Command(
                transfer.Id,
                transfer.Lines[0].Id,
                Quantity: 3),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Conflict, result.Error?.Type);
        Assert.Equal("InventoryTransfer.InsufficientSourceBalance", result.Error?.Code);
        Assert.Equal(0, await testDbContext.DbContext.InventoryTransferMovements.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await testDbContext.DbContext.InventoryTransactions.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await testDbContext.DbContext.InventoryLedgerEntries.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MoveAsync_WhenTransferUsesTransit_RejectsDirectMovement()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryTransferReferences references = await InventoryTransferTestData
            .SeedReferencesAsync(testDbContext.DbContext);
        InventoryTransferDetails transfer = await CreateTransferAsync(
            testDbContext.DbContext,
            references,
            references.InternalTransitStorageLocation.Id,
            requestedQuantity: 5);
        await SeedBalanceAsync(
            testDbContext.DbContext,
            references.StockKeepingUnit.Id,
            references.SourceStorageLocation.Id,
            quantity: 20);
        testDbContext.DbContext.ChangeTracker.Clear();
        MoveInventoryTransferLine.Handler handler = new(
            testDbContext.DbContext,
            new RecordingDomainEventDispatcher());

        ServiceResult<InventoryTransferDetails> result = await handler.HandleAsync(
            new MoveInventoryTransferLine.Command(
                transfer.Id,
                transfer.Lines[0].Id,
                Quantity: 3),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Conflict, result.Error?.Type);
        Assert.Equal("InventoryTransfer.DirectMovementRequiresDirectTransfer", result.Error?.Code);
        Assert.Equal(0, await testDbContext.DbContext.InventoryTransferMovements.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await testDbContext.DbContext.InventoryTransactions.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await testDbContext.DbContext.InventoryLedgerEntries.CountAsync(TestContext.Current.CancellationToken));
    }

    private static async Task<InventoryTransferDetails> CreateTransferAsync(
        WmsDbContext dbContext,
        SeededInventoryTransferReferences references,
        Guid? transitStorageLocationId,
        decimal requestedQuantity)
    {
        CreateInventoryTransfer.Handler handler = new(dbContext);

        ServiceResult<InventoryTransferDetails> result = await handler.HandleAsync(
            new CreateInventoryTransfer.Command(
                references.Warehouse.Id,
                references.Warehouse.Id,
                transitStorageLocationId,
                [
                    new CreateInventoryTransfer.Line(
                        references.StockKeepingUnit.Id,
                        references.SourceStorageLocation.Id,
                        references.DestinationStorageLocation.Id,
                        requestedQuantity)
                ]),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        return result.Value;
    }

    private static async Task SeedBalanceAsync(
        WmsDbContext dbContext,
        Guid stockKeepingUnitId,
        Guid storageLocationId,
        decimal quantity)
    {
        DomainValidationResult validationResult = InventoryBalance.Create(
            stockKeepingUnitId,
            storageLocationId,
            quantity,
            out InventoryBalance? inventoryBalance);

        Assert.True(validationResult.IsValid);
        Assert.NotNull(inventoryBalance);

        inventoryBalance.ClearDomainEvents();
        dbContext.InventoryBalances.Add(inventoryBalance);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
