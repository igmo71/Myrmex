using Microsoft.EntityFrameworkCore;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Events;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryBalances;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryBalances;

internal static class MoveInventoryBalance
{
    private const int SqlServerRowVersionLength = 8;
    private const string InternalTransitStorageLocationTypeCode = "INTERNAL_TRANSIT";
    private const string ExternalTransitStorageLocationTypeCode = "EXTERNAL_TRANSIT";

    internal sealed record Command(
        Guid? StockKeepingUnitId,
        Guid? SourceStorageLocationId,
        Guid? DestinationStorageLocationId,
        decimal Quantity,
        string? Reason,
        string? ExpectedSourceBalanceVersion)
        : ICommand<ServiceResult<MoveInventoryBalanceResult>>;

    internal sealed class Handler(
        WmsDbContext dbContext,
        IDomainEventDispatcher domainEventDispatcher)
        : ICommandHandler<Command, ServiceResult<MoveInventoryBalanceResult>>
    {
        public async Task<ServiceResult<MoveInventoryBalanceResult>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            List<DomainValidationFailure> validationErrors = ValidateCommand(
                command,
                out byte[]? expectedSourceVersion);

            if (validationErrors.Count > 0)
            {
                return ServiceResult<MoveInventoryBalanceResult>.Invalid(validationErrors);
            }

            StockKeepingUnit? stockKeepingUnit = await dbContext.StockKeepingUnits
                .SingleOrDefaultAsync(
                    x => x.Id == command.StockKeepingUnitId!.Value,
                    cancellationToken);

            if (stockKeepingUnit is null)
            {
                return ServiceResult<MoveInventoryBalanceResult>.Fail(
                    ServiceError.NotFound<StockKeepingUnit>(
                        "Stock keeping unit not found.",
                        nameof(Command.StockKeepingUnitId)));
            }

            StorageLocation? sourceLocation = await LoadLocationAsync(
                command.SourceStorageLocationId!.Value,
                cancellationToken);

            if (sourceLocation is null)
            {
                return ServiceResult<MoveInventoryBalanceResult>.Fail(
                    ServiceError.NotFound<StorageLocation>(
                        "Source storage location not found.",
                        nameof(Command.SourceStorageLocationId)));
            }

            StorageLocation? destinationLocation = await LoadLocationAsync(
                command.DestinationStorageLocationId!.Value,
                cancellationToken);

            if (destinationLocation is null)
            {
                return ServiceResult<MoveInventoryBalanceResult>.Fail(
                    ServiceError.NotFound<StorageLocation>(
                        "Destination storage location not found.",
                        nameof(Command.DestinationStorageLocationId)));
            }

            ServiceError? eligibilityError = ValidateEligibility(
                stockKeepingUnit,
                sourceLocation,
                destinationLocation);

            if (eligibilityError is not null)
            {
                return ServiceResult<MoveInventoryBalanceResult>.Fail(eligibilityError);
            }

            InventoryBalance? sourceBalance = await dbContext.InventoryBalances
                .SingleOrDefaultAsync(
                    x => x.StockKeepingUnitId == command.StockKeepingUnitId.Value &&
                         x.StorageLocationId == command.SourceStorageLocationId.Value,
                    cancellationToken);

            if (sourceBalance is null ||
                expectedSourceVersion is null ||
                !sourceBalance.RowVersion.SequenceEqual(expectedSourceVersion))
            {
                return ConcurrencyConflict();
            }

            if (sourceBalance.Quantity < command.Quantity)
            {
                return InsufficientQuantityConflict();
            }

            InventoryBalance? destinationBalance = await dbContext.InventoryBalances
                .SingleOrDefaultAsync(
                    x => x.StockKeepingUnitId == command.StockKeepingUnitId.Value &&
                         x.StorageLocationId == command.DestinationStorageLocationId.Value,
                    cancellationToken);

            decimal sourceQuantityBefore = sourceBalance.Quantity;
            decimal sourceQuantityAfter = sourceQuantityBefore - command.Quantity;
            decimal destinationQuantityBefore = destinationBalance?.Quantity ?? 0;
            decimal destinationQuantityAfter = destinationQuantityBefore + command.Quantity;

            DomainValidationResult sourceUpdateResult =
                sourceBalance.UpdateQuantity(sourceQuantityAfter);

            if (!sourceUpdateResult.IsValid)
            {
                return ServiceResult<MoveInventoryBalanceResult>.Invalid(sourceUpdateResult.Errors);
            }

            if (destinationBalance is null)
            {
                DomainValidationResult destinationCreateResult = InventoryBalance.Create(
                    command.StockKeepingUnitId,
                    command.DestinationStorageLocationId,
                    destinationQuantityAfter,
                    out destinationBalance);

                if (!destinationCreateResult.IsValid)
                {
                    return ServiceResult<MoveInventoryBalanceResult>.Invalid(destinationCreateResult.Errors);
                }

                dbContext.InventoryBalances.Add(destinationBalance!);
            }
            else
            {
                DomainValidationResult destinationUpdateResult =
                    destinationBalance.UpdateQuantity(destinationQuantityAfter);

                if (!destinationUpdateResult.IsValid)
                {
                    return ServiceResult<MoveInventoryBalanceResult>.Invalid(destinationUpdateResult.Errors);
                }
            }

            DateTimeOffset occurredAtUtc = DateTimeOffset.UtcNow;

            DomainValidationResult transactionResult = InventoryTransaction.CreateTransfer(
                command.StockKeepingUnitId,
                command.SourceStorageLocationId,
                command.DestinationStorageLocationId,
                sourceQuantityBefore,
                sourceQuantityAfter,
                destinationQuantityBefore,
                destinationQuantityAfter,
                command.Reason,
                occurredAtUtc,
                out InventoryTransaction? inventoryTransaction);

            if (!transactionResult.IsValid)
            {
                return ServiceResult<MoveInventoryBalanceResult>.Invalid(transactionResult.Errors);
            }

            dbContext.InventoryTransactions.Add(
                inventoryTransaction
                ?? throw new InvalidOperationException(
                    "InventoryTransaction.CreateTransfer returned a valid result without a transaction."));

            ServiceResult saveResult = await SaveChangesAsync(cancellationToken);

            if (!saveResult.IsSuccess)
            {
                return ServiceResult<MoveInventoryBalanceResult>.Fail(saveResult.Error);
            }

            InventoryBalanceDetailsData[] balances = await dbContext.InventoryBalances
                .AsNoTracking()
                .Where(x => x.Id == sourceBalance.Id || x.Id == destinationBalance!.Id)
                .ProjectDetailsData()
                .ToArrayAsync(cancellationToken);

            InventoryBalanceDetailsData? sourceDetailsData =
                balances.SingleOrDefault(x => x.Id == sourceBalance.Id);
            InventoryBalanceDetailsData? destinationDetailsData =
                balances.SingleOrDefault(x => x.Id == destinationBalance!.Id);

            if (sourceDetailsData is null || destinationDetailsData is null)
            {
                return ServiceResult<MoveInventoryBalanceResult>.Fail(
                    ServiceError.Failure<InventoryBalance>(
                        "Inventory balances were saved but could not be loaded."));
            }

            return ServiceResult<MoveInventoryBalanceResult>.Success(
                new MoveInventoryBalanceResult(
                    sourceDetailsData.ToDetails(),
                    destinationDetailsData.ToDetails(),
                    command.Quantity,
                    sourceQuantityBefore,
                    sourceQuantityAfter,
                    destinationQuantityBefore,
                    destinationQuantityAfter,
                    occurredAtUtc));
        }

        private Task<StorageLocation?> LoadLocationAsync(
            Guid storageLocationId,
            CancellationToken cancellationToken)
        {
            return dbContext.StorageLocations
                .Include(x => x.StorageLocationType)
                .Include(x => x.StorageLocationStatus)
                .SingleOrDefaultAsync(x => x.Id == storageLocationId, cancellationToken);
        }

        private async Task<ServiceResult> SaveChangesAsync(CancellationToken cancellationToken)
        {
            List<AggregateRoot> aggregateRoots = dbContext.ChangeTracker
                .Entries<AggregateRoot>()
                .Select(x => x.Entity)
                .Where(x => x.DomainEvents.Count > 0)
                .ToList();

            List<IDomainEvent> domainEvents = aggregateRoots
                .SelectMany(x => x.DomainEvents)
                .ToList();

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);

                foreach (AggregateRoot aggregateRoot in aggregateRoots)
                {
                    aggregateRoot.ClearDomainEvents();
                }

                return ServiceResult.Success();
            }
            catch (DbUpdateConcurrencyException)
            {
                return ServiceResult.Fail(ConcurrencyConflictError());
            }
            catch (DbUpdateException exception)
                when (WmsPersistenceExceptionMapper.IsInventoryBalanceSkuLocationDuplicate(exception))
            {
                return ServiceResult.Fail(ConcurrencyConflictError());
            }
            catch (DbUpdateException exception)
            {
                ServiceError? error = WmsPersistenceExceptionMapper.TryMap(exception);

                if (error is not null)
                {
                    return ServiceResult.Fail(error);
                }

                throw;
            }
        }
    }

    private static List<DomainValidationFailure> ValidateCommand(
        Command command,
        out byte[]? expectedSourceVersion)
    {
        List<DomainValidationFailure> errors = [];
        expectedSourceVersion = null;

        if (!command.StockKeepingUnitId.HasValue ||
            command.StockKeepingUnitId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<InventoryBalance>(
                nameof(Command.StockKeepingUnitId)));
        }

        if (!command.SourceStorageLocationId.HasValue ||
            command.SourceStorageLocationId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<InventoryBalance>(
                nameof(Command.SourceStorageLocationId)));
        }

        if (!command.DestinationStorageLocationId.HasValue ||
            command.DestinationStorageLocationId.Value == Guid.Empty)
        {
            errors.Add(DomainValidationFailure.Required<InventoryBalance>(
                nameof(Command.DestinationStorageLocationId)));
        }

        if (command.SourceStorageLocationId.HasValue &&
            command.DestinationStorageLocationId.HasValue &&
            command.SourceStorageLocationId.Value == command.DestinationStorageLocationId.Value)
        {
            errors.Add(DomainValidationFailure.IncorrectState<InventoryBalance>(
                nameof(Command.DestinationStorageLocationId)));
        }

        if (command.Quantity <= 0)
        {
            errors.Add(DomainValidationFailure.IncorrectState<InventoryBalance>(
                nameof(Command.Quantity)));
        }

        string reason = command.Reason?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(reason))
        {
            errors.Add(DomainValidationFailure.Required<InventoryTransaction>(
                nameof(Command.Reason)));
        }
        else if (reason.Length > InventoryTransaction.ReasonMaxLength)
        {
            errors.Add(DomainValidationFailure.TooLong<InventoryTransaction>(
                nameof(Command.Reason),
                InventoryTransaction.ReasonMaxLength));
        }

        if (string.IsNullOrWhiteSpace(command.ExpectedSourceBalanceVersion))
        {
            errors.Add(DomainValidationFailure.Required<InventoryBalance>(
                nameof(Command.ExpectedSourceBalanceVersion)));
        }
        else
        {
            try
            {
                byte[] parsedVersion =
                    Convert.FromBase64String(command.ExpectedSourceBalanceVersion);

                if (parsedVersion.Length != SqlServerRowVersionLength)
                {
                    errors.Add(DomainValidationFailure.IncorrectState<InventoryBalance>(
                        nameof(Command.ExpectedSourceBalanceVersion)));
                }
                else
                {
                    expectedSourceVersion = parsedVersion;
                }
            }
            catch (FormatException)
            {
                errors.Add(DomainValidationFailure.IncorrectState<InventoryBalance>(
                    nameof(Command.ExpectedSourceBalanceVersion)));
            }
        }

        return errors;
    }

    private static ServiceError? ValidateEligibility(
        StockKeepingUnit stockKeepingUnit,
        StorageLocation sourceLocation,
        StorageLocation destinationLocation)
    {
        if (!stockKeepingUnit.IsActive)
        {
            return ValidationError(
                "InventoryBalance.InactiveStockKeepingUnit",
                "Stock keeping unit must be active.",
                nameof(Command.StockKeepingUnitId));
        }

        ServiceError? sourceError = ValidateLocation(
            sourceLocation,
            "source",
            nameof(Command.SourceStorageLocationId));

        if (sourceError is not null)
        {
            return sourceError;
        }

        ServiceError? destinationError = ValidateLocation(
            destinationLocation,
            "destination",
            nameof(Command.DestinationStorageLocationId));

        if (destinationError is not null)
        {
            return destinationError;
        }

        if (sourceLocation.WarehouseId != destinationLocation.WarehouseId)
        {
            return ValidationError(
                "InventoryBalance.CrossWarehouseMove",
                "Source and destination storage locations must belong to the same warehouse.",
                nameof(Command.DestinationStorageLocationId));
        }

        return null;
    }

    private static ServiceError? ValidateLocation(
        StorageLocation location,
        string role,
        string property)
    {
        if (!location.IsActive)
        {
            return ValidationError(
                "InventoryBalance.InactiveStorageLocation",
                $"{role} storage location must be active.",
                property);
        }

        if (!location.StorageLocationType.IsActive)
        {
            return ValidationError(
                "InventoryBalance.InactiveStorageLocationType",
                $"{role} storage location type must be active.",
                property);
        }

        if (!location.StorageLocationStatus.IsActive)
        {
            return ValidationError(
                "InventoryBalance.InactiveStorageLocationStatus",
                $"{role} storage location status must be active.",
                property);
        }

        if (location.StorageLocationType.Code is
            InternalTransitStorageLocationTypeCode or
            ExternalTransitStorageLocationTypeCode)
        {
            return ValidationError(
                "InventoryBalance.TransitStorageLocation",
                $"{role} storage location must be a regular storage location.",
                property);
        }

        return null;
    }

    private static ServiceError ValidationError(
        string code,
        string message,
        string property)
    {
        return new ServiceError(
            ServiceErrorType.Invalid,
            code,
            message,
            property);
    }

    internal static ServiceResult<MoveInventoryBalanceResult> ConcurrencyConflict()
    {
        return ServiceResult<MoveInventoryBalanceResult>.Fail(ConcurrencyConflictError());
    }

    internal static ServiceError ConcurrencyConflictError()
    {
        return new ServiceError(
            ServiceErrorType.Conflict,
            "InventoryBalance.ConcurrencyConflict",
            "Inventory balance changed while the move was being committed. Refresh and retry.",
            nameof(Command.ExpectedSourceBalanceVersion));
    }

    internal static ServiceResult<MoveInventoryBalanceResult> InsufficientQuantityConflict()
    {
        return ServiceResult<MoveInventoryBalanceResult>.Fail(
            new ServiceError(
                ServiceErrorType.Conflict,
                "InventoryBalance.InsufficientQuantity",
                "Source storage location does not have enough inventory for the move.",
                nameof(Command.Quantity)));
    }
}
