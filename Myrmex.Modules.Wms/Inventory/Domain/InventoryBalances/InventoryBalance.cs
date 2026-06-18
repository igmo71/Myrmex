using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;

namespace Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;

internal sealed class InventoryBalance : AggregateRoot
{
    private InventoryBalance() { }

    private InventoryBalance(
        Guid stockKeepingUnitId,
        Guid storageLocationId,
        decimal quantity)
    {
        StockKeepingUnitId = stockKeepingUnitId;
        StorageLocationId = storageLocationId;
        Quantity = quantity;
    }

    public Guid StockKeepingUnitId { get; private set; }
    public StockKeepingUnit StockKeepingUnit { get; private set; } = null!;

    public Guid StorageLocationId { get; private set; }
    public StorageLocation StorageLocation { get; private set; } = null!;

    public decimal Quantity { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static DomainValidationResult Create(
        Guid? stockKeepingUnitId,
        Guid? storageLocationId,
        decimal quantity,
        out InventoryBalance? inventoryBalance)
    {
        DomainValidationResult validationResult = ValidateCreate(
            stockKeepingUnitId,
            storageLocationId,
            quantity);

        if (!validationResult.IsValid)
        {
            inventoryBalance = null;
            return validationResult;
        }

        inventoryBalance = new InventoryBalance(
            stockKeepingUnitId!.Value,
            storageLocationId!.Value,
            quantity);

        inventoryBalance.AddDomainEvent(
            new InventoryBalanceCreatedDomainEvent(
                inventoryBalance.Id,
                inventoryBalance.StockKeepingUnitId,
                inventoryBalance.StorageLocationId,
                inventoryBalance.Quantity));

        return DomainValidationResult.Valid;
    }

    public DomainValidationResult UpdateQuantity(decimal quantity)
    {
        DomainValidationResult validationResult = ValidateQuantity(quantity);

        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        Quantity = quantity;
        Touch();
        AddDomainEvent(new InventoryBalanceQuantityUpdatedDomainEvent(Id, Quantity));

        return DomainValidationResult.Valid;
    }

    public DomainValidationResult ApplyCountedQuantityAdjustment(decimal countedQuantity)
    {
        DomainValidationResult validationResult = ValidateQuantity(countedQuantity);

        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        Quantity = countedQuantity;
        Touch();

        return DomainValidationResult.Valid;
    }

    public static DomainValidationResult ValidateCreate(
        Guid? stockKeepingUnitId,
        Guid? storageLocationId,
        decimal quantity)
    {
        List<DomainValidationFailure> errors = [];

        if (!stockKeepingUnitId.HasValue || stockKeepingUnitId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<InventoryBalance>(nameof(StockKeepingUnitId)));
        }

        if (!storageLocationId.HasValue || storageLocationId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<InventoryBalance>(nameof(StorageLocationId)));
        }

        errors.AddRange(ValidateQuantity(quantity).Errors);

        return DomainValidationResult.From(errors);
    }

    private static DomainValidationResult ValidateQuantity(decimal quantity)
    {
        List<DomainValidationFailure> errors = [];

        if (quantity < 0)
        {
            errors.Add(DomainValidationFailure.MustBeNonNegative<InventoryBalance>(nameof(Quantity)));
        }

        return DomainValidationResult.From(errors);
    }
}
