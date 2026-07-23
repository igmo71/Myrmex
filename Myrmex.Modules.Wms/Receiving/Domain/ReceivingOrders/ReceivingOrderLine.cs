using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Domain;

namespace Myrmex.Modules.Wms.Receiving.Domain.ReceivingOrders;

internal sealed class ReceivingOrderLine : EntityBase
{
    private ReceivingOrderLine()
    {
    }

    private ReceivingOrderLine(
        Guid receivingOrderId,
        Guid stockKeepingUnitId,
        decimal plannedQuantity)
    {
        ReceivingOrderId = receivingOrderId;
        StockKeepingUnitId = stockKeepingUnitId;
        PlannedQuantity = plannedQuantity;
        ReceivedQuantity = 0;
    }

    public Guid ReceivingOrderId { get; private set; }
    public ReceivingOrder ReceivingOrder { get; private set; } = null!;

    public Guid StockKeepingUnitId { get; private set; }
    public StockKeepingUnit StockKeepingUnit { get; private set; } = null!;

    public decimal PlannedQuantity { get; private set; }

    public decimal ReceivedQuantity { get; private set; }

    public decimal RemainingQuantity => PlannedQuantity - ReceivedQuantity;

    public bool IsFullyReceived => ReceivedQuantity == PlannedQuantity;

    internal static DomainValidationResult Create(
        Guid receivingOrderId,
        Guid? stockKeepingUnitId,
        decimal plannedQuantity,
        out ReceivingOrderLine? line)
    {
        List<DomainValidationFailure> errors = [];

        if (receivingOrderId == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<ReceivingOrderLine>(nameof(ReceivingOrderId)));
        }

        errors.AddRange(ValidatePlan(stockKeepingUnitId, plannedQuantity).Errors);

        DomainValidationResult result = DomainValidationResult.From(errors);
        if (!result.IsValid)
        {
            line = null;
            return result;
        }

        line = new ReceivingOrderLine(
            receivingOrderId,
            stockKeepingUnitId!.Value,
            plannedQuantity);

        return DomainValidationResult.Valid;
    }

    internal static DomainValidationResult ValidatePlan(
        Guid? stockKeepingUnitId,
        decimal plannedQuantity)
    {
        List<DomainValidationFailure> errors = [];

        if (!stockKeepingUnitId.HasValue || stockKeepingUnitId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<ReceivingOrderLine>(nameof(StockKeepingUnitId)));
        }

        if (plannedQuantity <= 0)
        {
            errors.Add(DomainValidationFailure.IncorrectState<ReceivingOrderLine>(nameof(PlannedQuantity)));
        }

        DomainValidationFailure? persistenceFailure =
            WmsQuantityPersistence.Validate<ReceivingOrderLine>(
                plannedQuantity,
                nameof(PlannedQuantity));

        if (persistenceFailure is not null)
        {
            errors.Add(persistenceFailure);
        }

        return DomainValidationResult.From(errors);
    }

    internal void ReplaceDraftPlan(
        Guid stockKeepingUnitId,
        decimal plannedQuantity)
    {
        if (StockKeepingUnitId == stockKeepingUnitId &&
            PlannedQuantity == plannedQuantity)
        {
            return;
        }

        StockKeepingUnitId = stockKeepingUnitId;
        PlannedQuantity = plannedQuantity;
        Touch();
    }

    internal DomainValidationResult Receive(decimal quantity)
    {
        List<DomainValidationFailure> errors = [];

        if (quantity <= 0)
        {
            errors.Add(DomainValidationFailure.IncorrectState<ReceivingOrderLine>(nameof(quantity)));
        }

        DomainValidationFailure? quantityPersistenceFailure =
            WmsQuantityPersistence.Validate<ReceivingOrderLine>(quantity, nameof(quantity));

        if (quantityPersistenceFailure is not null)
        {
            errors.Add(quantityPersistenceFailure);
        }

        decimal accumulatedQuantity = 0;
        try
        {
            accumulatedQuantity = ReceivedQuantity + quantity;
        }
        catch (OverflowException)
        {
            errors.Add(DomainValidationFailure.IncorrectState<ReceivingOrderLine>(nameof(ReceivedQuantity)));
        }

        if (errors.Count == 0)
        {
            DomainValidationFailure? accumulatedPersistenceFailure =
                WmsQuantityPersistence.Validate<ReceivingOrderLine>(
                    accumulatedQuantity,
                    nameof(ReceivedQuantity));

            if (accumulatedPersistenceFailure is not null)
            {
                errors.Add(accumulatedPersistenceFailure);
            }
            else if (accumulatedQuantity > PlannedQuantity)
            {
                errors.Add(DomainValidationFailure.IncorrectState<ReceivingOrderLine>(nameof(ReceivedQuantity)));
            }
        }

        DomainValidationResult result = DomainValidationResult.From(errors);
        if (!result.IsValid)
        {
            return result;
        }

        ReceivedQuantity = accumulatedQuantity;
        Touch();
        return DomainValidationResult.Valid;
    }
}
