using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransfers;
using Myrmex.Modules.Wms.Inventory.Features.InventoryTransfers;
using Myrmex.Shared.Common;
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

    [Fact]
    public async Task PickAsync_WhenTransitPickIsValid_CreatesMovementTransactionLedgerAndUpdatesBalances()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
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
            quantity: 10);
        testDbContext.DbContext.ChangeTracker.Clear();
        PickInventoryTransferLine.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ServiceResult<InventoryTransferDetails> result = await handler.HandleAsync(
            new PickInventoryTransferLine.Command(
                transfer.Id,
                transfer.Lines[0].Id,
                Quantity: 4),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(InventoryTransferStatusDetails.InProgress, result.Value.Status);

        InventoryTransferLineDetails line = Assert.Single(result.Value.Lines);
        Assert.Equal(0, line.MovedQuantity);
        Assert.Equal(4, line.PickedQuantity);
        Assert.Equal(0, line.PlacedQuantity);
        Assert.Equal(4, line.InTransitQuantity);

        InventoryTransferMovementDetails movement = Assert.Single(result.Value.Movements);
        Assert.Equal(line.Id, movement.LineId);
        Assert.Equal(references.StockKeepingUnit.Id, movement.Sku.Id);
        Assert.Equal(references.SourceStorageLocation.Id, movement.FromStorageLocation.Id);
        Assert.Equal(references.InternalTransitStorageLocation.Id, movement.ToStorageLocation.Id);
        Assert.Equal(4, movement.Quantity);

        InventoryTransaction transaction = await testDbContext.DbContext.InventoryTransactions
            .Include(x => x.Entries)
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(InventoryTransactionType.Transfer, transaction.TransactionType);
        Assert.Equal(2, transaction.Entries.Count);

        InventoryLedgerEntry sourceEntry = transaction.Entries.Single(x =>
            x.StorageLocationId == references.SourceStorageLocation.Id);
        Assert.Equal(10, sourceEntry.BalanceBefore);
        Assert.Equal(-4, sourceEntry.QuantityDelta);
        Assert.Equal(6, sourceEntry.BalanceAfter);

        InventoryLedgerEntry transitEntry = transaction.Entries.Single(x =>
            x.StorageLocationId == references.InternalTransitStorageLocation.Id);
        Assert.Equal(0, transitEntry.BalanceBefore);
        Assert.Equal(4, transitEntry.QuantityDelta);
        Assert.Equal(4, transitEntry.BalanceAfter);

        Assert.Equal(6, await GetBalanceQuantityAsync(
            testDbContext.DbContext,
            references.StockKeepingUnit.Id,
            references.SourceStorageLocation.Id));
        Assert.Equal(4, await GetBalanceQuantityAsync(
            testDbContext.DbContext,
            references.StockKeepingUnit.Id,
            references.InternalTransitStorageLocation.Id));
    }

    [Fact]
    public async Task PlaceAsync_WhenTransitPlaceIsValid_CreatesMovementTransactionLedgerAndUpdatesBalances()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
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
            quantity: 10);
        await PickAsync(testDbContext.DbContext, domainEventDispatcher, transfer, quantity: 4);
        testDbContext.DbContext.ChangeTracker.Clear();
        PlaceInventoryTransferLine.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ServiceResult<InventoryTransferDetails> result = await handler.HandleAsync(
            new PlaceInventoryTransferLine.Command(
                transfer.Id,
                transfer.Lines[0].Id,
                Quantity: 2),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(InventoryTransferStatusDetails.InProgress, result.Value.Status);

        InventoryTransferLineDetails line = Assert.Single(result.Value.Lines);
        Assert.Equal(0, line.MovedQuantity);
        Assert.Equal(4, line.PickedQuantity);
        Assert.Equal(2, line.PlacedQuantity);
        Assert.Equal(2, line.InTransitQuantity);
        Assert.Equal(2, result.Value.Movements.Count);

        InventoryTransferMovementDetails movement = result.Value.Movements.Last();
        Assert.Equal(line.Id, movement.LineId);
        Assert.Equal(references.StockKeepingUnit.Id, movement.Sku.Id);
        Assert.Equal(references.InternalTransitStorageLocation.Id, movement.FromStorageLocation.Id);
        Assert.Equal(references.DestinationStorageLocation.Id, movement.ToStorageLocation.Id);
        Assert.Equal(2, movement.Quantity);

        Assert.Equal(2, await testDbContext.DbContext.InventoryTransactions.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(4, await testDbContext.DbContext.InventoryLedgerEntries.CountAsync(TestContext.Current.CancellationToken));

        InventoryTransaction placeTransaction = await testDbContext.DbContext.InventoryTransactions
            .Include(x => x.Entries)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstAsync(TestContext.Current.CancellationToken);

        Assert.Equal(InventoryTransactionType.Transfer, placeTransaction.TransactionType);
        Assert.Equal(2, placeTransaction.Entries.Count);

        InventoryLedgerEntry transitEntry = placeTransaction.Entries.Single(x =>
            x.StorageLocationId == references.InternalTransitStorageLocation.Id);
        Assert.Equal(4, transitEntry.BalanceBefore);
        Assert.Equal(-2, transitEntry.QuantityDelta);
        Assert.Equal(2, transitEntry.BalanceAfter);

        InventoryLedgerEntry destinationEntry = placeTransaction.Entries.Single(x =>
            x.StorageLocationId == references.DestinationStorageLocation.Id);
        Assert.Equal(0, destinationEntry.BalanceBefore);
        Assert.Equal(2, destinationEntry.QuantityDelta);
        Assert.Equal(2, destinationEntry.BalanceAfter);

        Assert.Equal(6, await GetBalanceQuantityAsync(
            testDbContext.DbContext,
            references.StockKeepingUnit.Id,
            references.SourceStorageLocation.Id));
        Assert.Equal(2, await GetBalanceQuantityAsync(
            testDbContext.DbContext,
            references.StockKeepingUnit.Id,
            references.InternalTransitStorageLocation.Id));
        Assert.Equal(2, await GetBalanceQuantityAsync(
            testDbContext.DbContext,
            references.StockKeepingUnit.Id,
            references.DestinationStorageLocation.Id));
    }

    [Fact]
    public async Task PickAsync_WhenPickExceedsRemainingRequested_RejectsWithoutPersistence()
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
        PickInventoryTransferLine.Handler handler = new(
            testDbContext.DbContext,
            new RecordingDomainEventDispatcher());

        ServiceResult<InventoryTransferDetails> result = await handler.HandleAsync(
            new PickInventoryTransferLine.Command(
                transfer.Id,
                transfer.Lines[0].Id,
                Quantity: 6),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Conflict, result.Error?.Type);
        Assert.Equal("InventoryTransfer.OverPick", result.Error?.Code);
        Assert.Equal(0, await testDbContext.DbContext.InventoryTransferMovements.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await testDbContext.DbContext.InventoryTransactions.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await testDbContext.DbContext.InventoryLedgerEntries.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PickAsync_WhenSourceBalanceIsInsufficient_RejectsWithoutPersistence()
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
            quantity: 2);
        testDbContext.DbContext.ChangeTracker.Clear();
        PickInventoryTransferLine.Handler handler = new(
            testDbContext.DbContext,
            new RecordingDomainEventDispatcher());

        ServiceResult<InventoryTransferDetails> result = await handler.HandleAsync(
            new PickInventoryTransferLine.Command(
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
    public async Task PlaceAsync_WhenPlaceExceedsInTransitQuantity_RejectsWithoutAdditionalPersistence()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
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
            quantity: 10);
        await PickAsync(testDbContext.DbContext, domainEventDispatcher, transfer, quantity: 2);
        testDbContext.DbContext.ChangeTracker.Clear();
        PlaceInventoryTransferLine.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ServiceResult<InventoryTransferDetails> result = await handler.HandleAsync(
            new PlaceInventoryTransferLine.Command(
                transfer.Id,
                transfer.Lines[0].Id,
                Quantity: 3),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Conflict, result.Error?.Type);
        Assert.Equal("InventoryTransfer.OverPlace", result.Error?.Code);
        Assert.Equal(1, await testDbContext.DbContext.InventoryTransferMovements.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, await testDbContext.DbContext.InventoryTransactions.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, await testDbContext.DbContext.InventoryLedgerEntries.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PickAndPlaceAsync_WhenTransferIsDirect_RejectWrongMovementPattern()
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
            quantity: 10);
        testDbContext.DbContext.ChangeTracker.Clear();

        PickInventoryTransferLine.Handler pickHandler = new(
            testDbContext.DbContext,
            new RecordingDomainEventDispatcher());
        PlaceInventoryTransferLine.Handler placeHandler = new(
            testDbContext.DbContext,
            new RecordingDomainEventDispatcher());

        ServiceResult<InventoryTransferDetails> pickResult = await pickHandler.HandleAsync(
            new PickInventoryTransferLine.Command(
                transfer.Id,
                transfer.Lines[0].Id,
                Quantity: 1),
            TestContext.Current.CancellationToken);
        ServiceResult<InventoryTransferDetails> placeResult = await placeHandler.HandleAsync(
            new PlaceInventoryTransferLine.Command(
                transfer.Id,
                transfer.Lines[0].Id,
                Quantity: 1),
            TestContext.Current.CancellationToken);

        Assert.False(pickResult.IsSuccess);
        Assert.Equal("InventoryTransfer.PickRequiresTransitTransfer", pickResult.Error?.Code);
        Assert.False(placeResult.IsSuccess);
        Assert.Equal("InventoryTransfer.PlaceRequiresTransitTransfer", placeResult.Error?.Code);
        Assert.Equal(0, await testDbContext.DbContext.InventoryTransferMovements.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await testDbContext.DbContext.InventoryTransactions.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await testDbContext.DbContext.InventoryLedgerEntries.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetByIdAsync_WhenTransferHasTransitMovements_ProjectsProgressAndReadOnlyHistory()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
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
            quantity: 10);
        await PickAsync(testDbContext.DbContext, domainEventDispatcher, transfer, quantity: 4);
        await PlaceAsync(testDbContext.DbContext, domainEventDispatcher, transfer, quantity: 2);
        testDbContext.DbContext.ChangeTracker.Clear();
        GetInventoryTransferById.Handler handler = new(testDbContext.DbContext);

        ServiceResult<InventoryTransferDetails> result = await handler.HandleAsync(
            new GetInventoryTransferById.Query(transfer.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        InventoryTransferLineDetails line = Assert.Single(result.Value.Lines);
        Assert.Equal(5, line.RequestedQuantity);
        Assert.Equal(0, line.MovedQuantity);
        Assert.Equal(4, line.PickedQuantity);
        Assert.Equal(2, line.PlacedQuantity);
        Assert.Equal(2, line.InTransitQuantity);
        Assert.Equal(1, line.RemainingToPickQuantity);
        Assert.Equal(2, line.RemainingToPlaceQuantity);

        Assert.Equal(2, result.Value.Movements.Count);
        InventoryTransferMovementDetails pickMovement = result.Value.Movements[0];
        Assert.Equal("Pick", pickMovement.MovementMeaning);
        Assert.Equal(line.Id, pickMovement.LineId);
        Assert.Equal(references.StockKeepingUnit.Id, pickMovement.Sku.Id);
        Assert.Equal(references.SourceStorageLocation.Id, pickMovement.FromStorageLocation.Id);
        Assert.Equal(references.InternalTransitStorageLocation.Id, pickMovement.ToStorageLocation.Id);
        Assert.NotEqual(Guid.Empty, pickMovement.InventoryTransactionId);

        InventoryTransferMovementDetails placeMovement = result.Value.Movements[1];
        Assert.Equal("Place", placeMovement.MovementMeaning);
        Assert.Equal(references.StockKeepingUnit.Id, placeMovement.Sku.Id);
        Assert.Equal(references.InternalTransitStorageLocation.Id, placeMovement.FromStorageLocation.Id);
        Assert.Equal(references.DestinationStorageLocation.Id, placeMovement.ToStorageLocation.Id);
        Assert.NotEqual(Guid.Empty, placeMovement.InventoryTransactionId);
        Assert.Equal(2, await testDbContext.DbContext.InventoryTransferMovements.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ListAsync_WhenFiltersSortingAndPagingApplied_ReturnsCountBeforePagingAndProgressAggregates()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
        SeededInventoryTransferReferences references = await InventoryTransferTestData
            .SeedReferencesAsync(testDbContext.DbContext);
        InventoryTransferDetails firstTransitTransfer = await CreateTransferAsync(
            testDbContext.DbContext,
            references,
            references.InternalTransitStorageLocation.Id,
            requestedQuantity: 5);
        InventoryTransferDetails secondTransitTransfer = await CreateTransferAsync(
            testDbContext.DbContext,
            references,
            references.InternalTransitStorageLocation.Id,
            requestedQuantity: 7);
        await CreateTransferAsync(
            testDbContext.DbContext,
            references,
            transitStorageLocationId: null,
            requestedQuantity: 3);
        await SeedBalanceAsync(
            testDbContext.DbContext,
            references.StockKeepingUnit.Id,
            references.SourceStorageLocation.Id,
            quantity: 50);
        await PickAsync(testDbContext.DbContext, domainEventDispatcher, firstTransitTransfer, quantity: 4);
        await PlaceAsync(testDbContext.DbContext, domainEventDispatcher, firstTransitTransfer, quantity: 2);
        await PickAsync(testDbContext.DbContext, domainEventDispatcher, secondTransitTransfer, quantity: 1);
        testDbContext.DbContext.ChangeTracker.Clear();
        ListInventoryTransfers.Handler handler = new(testDbContext.DbContext);

        ServiceResult<ListResult<InventoryTransferListItem>> result = await handler.HandleAsync(
            new ListInventoryTransfers.Query
            {
                Skip = 0,
                Take = 1,
                SortBy = InventoryTransferSortBy.TotalInTransitQuantity,
                SortDescending = true,
                WarehouseId = references.Warehouse.Id,
                StockKeepingUnitId = references.StockKeepingUnit.Id,
                SourceStorageLocationId = references.SourceStorageLocation.Id,
                HasTransitLocation = true
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(0, result.Value.Skip);
        Assert.Equal(1, result.Value.Take);
        InventoryTransferListItem item = Assert.Single(result.Value.Items);
        Assert.Equal(firstTransitTransfer.Id, item.Id);
        Assert.Equal(5, item.TotalRequestedQuantity);
        Assert.Equal(4, item.TotalPickedQuantity);
        Assert.Equal(2, item.TotalPlacedQuantity);
        Assert.Equal(2, item.TotalInTransitQuantity);
        Assert.Equal(references.Warehouse.Id, item.SourceWarehouse.Id);
        Assert.Equal(references.InternalTransitStorageLocation.Id, item.TransitStorageLocation?.Id);
    }

    [Fact]
    public async Task MoveAsync_WhenFinalDirectQuantityIsMoved_CompletesTransfer()
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
            quantity: 10);
        testDbContext.DbContext.ChangeTracker.Clear();
        MoveInventoryTransferLine.Handler handler = new(
            testDbContext.DbContext,
            new RecordingDomainEventDispatcher());

        ServiceResult<InventoryTransferDetails> result = await handler.HandleAsync(
            new MoveInventoryTransferLine.Command(
                transfer.Id,
                transfer.Lines[0].Id,
                Quantity: 5),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(InventoryTransferStatusDetails.Completed, result.Value.Status);
        InventoryTransferLineDetails line = Assert.Single(result.Value.Lines);
        Assert.Equal(5, line.MovedQuantity);
        Assert.Equal(5, line.PickedQuantity);
        Assert.Equal(5, line.PlacedQuantity);
        Assert.Equal(0, line.InTransitQuantity);

        InventoryTransfer savedTransfer = await testDbContext.DbContext.InventoryTransfers
            .SingleAsync(x => x.Id == transfer.Id, TestContext.Current.CancellationToken);
        Assert.Equal(InventoryTransferStatus.Completed, savedTransfer.Status);
    }

    [Fact]
    public async Task PickAndPlaceAsync_WhenTransitTransferIsFullyPlaced_CompletesOnlyAfterInTransitIsZero()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();
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
            quantity: 10);

        PickInventoryTransferLine.Handler pickHandler = new(
            testDbContext.DbContext,
            domainEventDispatcher);
        ServiceResult<InventoryTransferDetails> pickResult = await pickHandler.HandleAsync(
            new PickInventoryTransferLine.Command(
                transfer.Id,
                transfer.Lines[0].Id,
                Quantity: 5),
            TestContext.Current.CancellationToken);

        Assert.True(pickResult.IsSuccess);
        Assert.Equal(InventoryTransferStatusDetails.InProgress, pickResult.Value.Status);
        InventoryTransferLineDetails pickedLine = Assert.Single(pickResult.Value.Lines);
        Assert.Equal(5, pickedLine.PickedQuantity);
        Assert.Equal(0, pickedLine.PlacedQuantity);
        Assert.Equal(5, pickedLine.InTransitQuantity);

        testDbContext.DbContext.ChangeTracker.Clear();
        PlaceInventoryTransferLine.Handler placeHandler = new(
            testDbContext.DbContext,
            domainEventDispatcher);
        ServiceResult<InventoryTransferDetails> partialPlaceResult = await placeHandler.HandleAsync(
            new PlaceInventoryTransferLine.Command(
                transfer.Id,
                transfer.Lines[0].Id,
                Quantity: 4),
            TestContext.Current.CancellationToken);

        Assert.True(partialPlaceResult.IsSuccess);
        Assert.Equal(InventoryTransferStatusDetails.InProgress, partialPlaceResult.Value.Status);
        InventoryTransferLineDetails partiallyPlacedLine = Assert.Single(partialPlaceResult.Value.Lines);
        Assert.Equal(5, partiallyPlacedLine.PickedQuantity);
        Assert.Equal(4, partiallyPlacedLine.PlacedQuantity);
        Assert.Equal(1, partiallyPlacedLine.InTransitQuantity);

        testDbContext.DbContext.ChangeTracker.Clear();
        ServiceResult<InventoryTransferDetails> finalPlaceResult = await placeHandler.HandleAsync(
            new PlaceInventoryTransferLine.Command(
                transfer.Id,
                transfer.Lines[0].Id,
                Quantity: 1),
            TestContext.Current.CancellationToken);

        Assert.True(finalPlaceResult.IsSuccess);
        Assert.Equal(InventoryTransferStatusDetails.Completed, finalPlaceResult.Value.Status);
        InventoryTransferLineDetails completedLine = Assert.Single(finalPlaceResult.Value.Lines);
        Assert.Equal(5, completedLine.PickedQuantity);
        Assert.Equal(5, completedLine.PlacedQuantity);
        Assert.Equal(0, completedLine.InTransitQuantity);

        InventoryTransfer savedTransfer = await testDbContext.DbContext.InventoryTransfers
            .SingleAsync(x => x.Id == transfer.Id, TestContext.Current.CancellationToken);
        Assert.Equal(InventoryTransferStatus.Completed, savedTransfer.Status);
    }

    [Fact]
    public async Task MovePickAndPlaceAsync_WhenTransferIsCompleted_RejectWithoutChangingBalancesOrHistory()
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
        await MoveAsync(testDbContext.DbContext, domainEventDispatcher, transfer, quantity: 5);
        testDbContext.DbContext.ChangeTracker.Clear();

        int movementCountBefore = await testDbContext.DbContext.InventoryTransferMovements.CountAsync(TestContext.Current.CancellationToken);
        int transactionCountBefore = await testDbContext.DbContext.InventoryTransactions.CountAsync(TestContext.Current.CancellationToken);
        int ledgerEntryCountBefore = await testDbContext.DbContext.InventoryLedgerEntries.CountAsync(TestContext.Current.CancellationToken);
        decimal sourceBalanceBefore = await GetBalanceQuantityAsync(
            testDbContext.DbContext,
            references.StockKeepingUnit.Id,
            references.SourceStorageLocation.Id);
        decimal destinationBalanceBefore = await GetBalanceQuantityAsync(
            testDbContext.DbContext,
            references.StockKeepingUnit.Id,
            references.DestinationStorageLocation.Id);

        MoveInventoryTransferLine.Handler moveHandler = new(
            testDbContext.DbContext,
            domainEventDispatcher);
        PickInventoryTransferLine.Handler pickHandler = new(
            testDbContext.DbContext,
            domainEventDispatcher);
        PlaceInventoryTransferLine.Handler placeHandler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        ServiceResult<InventoryTransferDetails> moveResult = await moveHandler.HandleAsync(
            new MoveInventoryTransferLine.Command(
                transfer.Id,
                transfer.Lines[0].Id,
                Quantity: 1),
            TestContext.Current.CancellationToken);
        ServiceResult<InventoryTransferDetails> pickResult = await pickHandler.HandleAsync(
            new PickInventoryTransferLine.Command(
                transfer.Id,
                transfer.Lines[0].Id,
                Quantity: 1),
            TestContext.Current.CancellationToken);
        ServiceResult<InventoryTransferDetails> placeResult = await placeHandler.HandleAsync(
            new PlaceInventoryTransferLine.Command(
                transfer.Id,
                transfer.Lines[0].Id,
                Quantity: 1),
            TestContext.Current.CancellationToken);

        Assert.False(moveResult.IsSuccess);
        Assert.Equal("InventoryTransfer.Completed", moveResult.Error?.Code);
        Assert.False(pickResult.IsSuccess);
        Assert.Equal("InventoryTransfer.Completed", pickResult.Error?.Code);
        Assert.False(placeResult.IsSuccess);
        Assert.Equal("InventoryTransfer.Completed", placeResult.Error?.Code);
        Assert.Equal(movementCountBefore, await testDbContext.DbContext.InventoryTransferMovements.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(transactionCountBefore, await testDbContext.DbContext.InventoryTransactions.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(ledgerEntryCountBefore, await testDbContext.DbContext.InventoryLedgerEntries.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(sourceBalanceBefore, await GetBalanceQuantityAsync(
            testDbContext.DbContext,
            references.StockKeepingUnit.Id,
            references.SourceStorageLocation.Id));
        Assert.Equal(destinationBalanceBefore, await GetBalanceQuantityAsync(
            testDbContext.DbContext,
            references.StockKeepingUnit.Id,
            references.DestinationStorageLocation.Id));
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

    private static async Task MoveAsync(
        WmsDbContext dbContext,
        RecordingDomainEventDispatcher domainEventDispatcher,
        InventoryTransferDetails transfer,
        decimal quantity)
    {
        MoveInventoryTransferLine.Handler handler = new(
            dbContext,
            domainEventDispatcher);

        ServiceResult<InventoryTransferDetails> result = await handler.HandleAsync(
            new MoveInventoryTransferLine.Command(
                transfer.Id,
                transfer.Lines[0].Id,
                quantity),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    private static async Task PickAsync(
        WmsDbContext dbContext,
        RecordingDomainEventDispatcher domainEventDispatcher,
        InventoryTransferDetails transfer,
        decimal quantity)
    {
        PickInventoryTransferLine.Handler handler = new(
            dbContext,
            domainEventDispatcher);

        ServiceResult<InventoryTransferDetails> result = await handler.HandleAsync(
            new PickInventoryTransferLine.Command(
                transfer.Id,
                transfer.Lines[0].Id,
                quantity),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    private static async Task PlaceAsync(
        WmsDbContext dbContext,
        RecordingDomainEventDispatcher domainEventDispatcher,
        InventoryTransferDetails transfer,
        decimal quantity)
    {
        PlaceInventoryTransferLine.Handler handler = new(
            dbContext,
            domainEventDispatcher);

        ServiceResult<InventoryTransferDetails> result = await handler.HandleAsync(
            new PlaceInventoryTransferLine.Command(
                transfer.Id,
                transfer.Lines[0].Id,
                quantity),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    private static async Task<decimal> GetBalanceQuantityAsync(
        WmsDbContext dbContext,
        Guid stockKeepingUnitId,
        Guid storageLocationId)
    {
        InventoryBalance balance = await dbContext.InventoryBalances.SingleAsync(
            x => x.StockKeepingUnitId == stockKeepingUnitId &&
                 x.StorageLocationId == storageLocationId,
            TestContext.Current.CancellationToken);

        return balance.Quantity;
    }
}
