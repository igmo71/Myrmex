using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransfers;

namespace Myrmex.Tests.Wms.Inventory.Domain;

public sealed class InventoryTransferTests
{
    private static readonly Guid WarehouseId = Guid.Parse("018f0000-0000-7000-8000-000000000301");
    private static readonly Guid TransitStorageLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000302");
    private static readonly Guid StockKeepingUnitId = Guid.Parse("018f0000-0000-7000-8000-000000000101");
    private static readonly Guid SourceStorageLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000201");
    private static readonly Guid DestinationStorageLocationId = Guid.Parse("018f0000-0000-7000-8000-000000000202");

    [Fact]
    public void Create_WhenTransitLocationIsNull_CreatesDirectTransferWithoutPersistedExecutionMode()
    {
        InventoryTransferLine line = CreateLine();

        var result = InventoryTransfer.Create(
            "TR-001",
            WarehouseId,
            WarehouseId,
            transitStorageLocationId: null,
            [line],
            out InventoryTransfer? transfer);

        Assert.True(result.IsValid);
        Assert.NotNull(transfer);
        Assert.Null(transfer.TransitStorageLocationId);
        Assert.False(transfer.UsesTransit);
        Assert.Equal(InventoryTransferStatus.Created, transfer.Status);
        Assert.Single(transfer.Lines);

        Assert.DoesNotContain(
            typeof(InventoryTransfer).GetProperties(),
            property => property.Name == "TransferExecutionMode");
    }

    [Fact]
    public void Create_WhenTransitLocationIsProvided_KeepsNullableTransitStorageLocationId()
    {
        InventoryTransferLine line = CreateLine();

        var result = InventoryTransfer.Create(
            "TR-002",
            WarehouseId,
            WarehouseId,
            TransitStorageLocationId,
            [line],
            out InventoryTransfer? transfer);

        Assert.True(result.IsValid);
        Assert.NotNull(transfer);
        Assert.Equal(TransitStorageLocationId, transfer.TransitStorageLocationId);
        Assert.True(transfer.UsesTransit);
    }

    [Fact]
    public void InventoryTransferMovement_WhenCreated_StoresFactFieldsWithoutMovementTypeOrScannerState()
    {
        Guid lineId = Guid.Parse("018f0000-0000-7000-8000-000000000401");
        Guid transactionId = Guid.Parse("018f0000-0000-7000-8000-000000000501");
        DateTimeOffset occurredAtUtc = DateTimeOffset.Parse("2026-06-19T13:00:00Z");

        var result = InventoryTransferMovement.Create(
            lineId,
            transactionId,
            StockKeepingUnitId,
            SourceStorageLocationId,
            DestinationStorageLocationId,
            quantity: 4,
            occurredAtUtc,
            out InventoryTransferMovement? movement);

        Assert.True(result.IsValid);
        Assert.NotNull(movement);
        Assert.Equal(transactionId, movement.InventoryTransactionId);
        Assert.Equal(StockKeepingUnitId, movement.StockKeepingUnitId);
        Assert.Equal(SourceStorageLocationId, movement.FromStorageLocationId);
        Assert.Equal(DestinationStorageLocationId, movement.ToStorageLocationId);
        Assert.Equal(4, movement.Quantity);
        Assert.Equal(occurredAtUtc, movement.OccurredAtUtc);

        string[] propertyNames = typeof(InventoryTransferMovement)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain("MovementType", propertyNames);
        Assert.DoesNotContain("ScannerState", propertyNames);
    }

    private static InventoryTransferLine CreateLine()
    {
        var result = InventoryTransferLine.Create(
            StockKeepingUnitId,
            SourceStorageLocationId,
            DestinationStorageLocationId,
            requestedQuantity: 5,
            out InventoryTransferLine? line);

        Assert.True(result.IsValid);
        Assert.NotNull(line);

        return line;
    }
}
