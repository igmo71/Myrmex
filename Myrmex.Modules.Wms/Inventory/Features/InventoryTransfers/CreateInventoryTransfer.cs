using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransfers;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryTransfers;

internal static class CreateInventoryTransfer
{
    internal const string InternalTransitLocationTypeCode = "INTERNAL_TRANSIT";
    private const string ExternalTransitLocationTypeCode = "EXTERNAL_TRANSIT";

    internal sealed record Command(
        Guid? SourceWarehouseId,
        Guid? DestinationWarehouseId,
        Guid? TransitStorageLocationId,
        IReadOnlyCollection<Line> Lines) : ICommand<ServiceResult<InventoryTransferDetails>>;

    internal sealed record Line(
        Guid? StockKeepingUnitId,
        Guid? SourceStorageLocationId,
        Guid? DestinationStorageLocationId,
        decimal RequestedQuantity);

    internal sealed class Handler(WmsDbContext dbContext)
        : ICommandHandler<Command, ServiceResult<InventoryTransferDetails>>
    {
        public async Task<ServiceResult<InventoryTransferDetails>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            List<DomainValidationFailure> commandErrors = ValidateCommand(command);

            if (commandErrors.Count > 0)
            {
                return ServiceResult<InventoryTransferDetails>.Invalid(commandErrors);
            }

            ServiceError? headerError = await ValidateHeaderReferencesAsync(command, cancellationToken);

            if (headerError is not null)
            {
                return ServiceResult<InventoryTransferDetails>.Fail(headerError);
            }

            List<InventoryTransferLine> lines = [];

            for (int index = 0; index < command.Lines.Count; index++)
            {
                Line commandLine = command.Lines.ElementAt(index);

                ServiceError? lineReferenceError = await ValidateLineReferencesAsync(
                    commandLine,
                    command.SourceWarehouseId!.Value,
                    index,
                    cancellationToken);

                if (lineReferenceError is not null)
                {
                    return ServiceResult<InventoryTransferDetails>.Fail(lineReferenceError);
                }

                DomainValidationResult lineValidationResult = InventoryTransferLine.Create(
                    commandLine.StockKeepingUnitId,
                    commandLine.SourceStorageLocationId,
                    commandLine.DestinationStorageLocationId,
                    commandLine.RequestedQuantity,
                    out InventoryTransferLine? line);

                if (!lineValidationResult.IsValid)
                {
                    return ServiceResult<InventoryTransferDetails>.Invalid(lineValidationResult.Errors);
                }

                lines.Add(line!);
            }

            DomainValidationResult transferValidationResult = InventoryTransfer.Create(
                GenerateTransferCode(),
                command.SourceWarehouseId,
                command.DestinationWarehouseId,
                command.TransitStorageLocationId,
                lines,
                out InventoryTransfer? transfer);

            if (!transferValidationResult.IsValid)
            {
                return ServiceResult<InventoryTransferDetails>.Invalid(transferValidationResult.Errors);
            }

            InventoryTransfer createdTransfer = transfer
                ?? throw new InvalidOperationException("InventoryTransfer.Create returned a valid result without a transfer.");

            dbContext.InventoryTransfers.Add(createdTransfer);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                ServiceError? mappedError = WmsPersistenceExceptionMapper.TryMap(exception);

                if (mappedError is not null)
                {
                    return ServiceResult<InventoryTransferDetails>.Fail(mappedError);
                }

                throw;
            }

            return await GetDetailsAsync(createdTransfer.Id, cancellationToken);
        }

        private async Task<ServiceResult<InventoryTransferDetails>> GetDetailsAsync(
            Guid inventoryTransferId,
            CancellationToken cancellationToken)
        {
            InventoryTransferDetailsData? detailsData = await dbContext.InventoryTransfers
                .AsNoTracking()
                .Where(x => x.Id == inventoryTransferId)
                .ProjectDetailsData()
                .SingleOrDefaultAsync(cancellationToken);

            return detailsData is null
                ? ServiceResult<InventoryTransferDetails>.Fail(ServiceError.Failure<InventoryTransfer>("InventoryTransfer was saved but could not be loaded"))
                : ServiceResult<InventoryTransferDetails>.Success(detailsData.ToDetails());
        }

        private static List<DomainValidationFailure> ValidateCommand(Command command)
        {
            List<DomainValidationFailure> errors = [];

            if (!command.SourceWarehouseId.HasValue || command.SourceWarehouseId.Value == Guid.Empty)
            {
                errors.Add(DomainValidationFailure.Required<InventoryTransfer>(nameof(Command.SourceWarehouseId)));
            }

            if (!command.DestinationWarehouseId.HasValue || command.DestinationWarehouseId.Value == Guid.Empty)
            {
                errors.Add(DomainValidationFailure.Required<InventoryTransfer>(nameof(Command.DestinationWarehouseId)));
            }

            if (command.SourceWarehouseId.HasValue &&
                command.DestinationWarehouseId.HasValue &&
                command.SourceWarehouseId.Value != command.DestinationWarehouseId.Value)
            {
                errors.Add(DomainValidationFailure.Unsupported<InventoryTransfer>(nameof(Command.DestinationWarehouseId)));
            }

            if (command.TransitStorageLocationId.HasValue &&
                command.TransitStorageLocationId.Value == Guid.Empty)
            {
                errors.Add(DomainValidationFailure.Required<InventoryTransfer>(nameof(Command.TransitStorageLocationId)));
            }

            if (command.Lines.Count == 0)
            {
                errors.Add(DomainValidationFailure.Required<InventoryTransfer>(nameof(Command.Lines)));
            }

            foreach (Line line in command.Lines)
            {
                if (!line.StockKeepingUnitId.HasValue || line.StockKeepingUnitId.Value == Guid.Empty)
                {
                    errors.Add(DomainValidationFailure.Required<InventoryTransferLine>(nameof(Line.StockKeepingUnitId)));
                }

                if (!line.SourceStorageLocationId.HasValue || line.SourceStorageLocationId.Value == Guid.Empty)
                {
                    errors.Add(DomainValidationFailure.Required<InventoryTransferLine>(nameof(Line.SourceStorageLocationId)));
                }

                if (!line.DestinationStorageLocationId.HasValue || line.DestinationStorageLocationId.Value == Guid.Empty)
                {
                    errors.Add(DomainValidationFailure.Required<InventoryTransferLine>(nameof(Line.DestinationStorageLocationId)));
                }

                if (line.RequestedQuantity <= 0)
                {
                    errors.Add(DomainValidationFailure.IncorrectState<InventoryTransferLine>(nameof(Line.RequestedQuantity)));
                }
            }

            return errors;
        }

        private async Task<ServiceError?> ValidateHeaderReferencesAsync(
            Command command,
            CancellationToken cancellationToken)
        {
            Warehouse? sourceWarehouse = await dbContext.Warehouses
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == command.SourceWarehouseId!.Value, cancellationToken);

            Warehouse? destinationWarehouse = await dbContext.Warehouses
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == command.DestinationWarehouseId!.Value, cancellationToken);

            if (sourceWarehouse is null)
            {
                return ServiceError.NotFound<InventoryTransfer>("SourceWarehouse not found", nameof(Command.SourceWarehouseId));
            }

            if (!sourceWarehouse.IsActive)
            {
                return ServiceError.Validation<InventoryTransfer>("SourceWarehouse is inactive", nameof(Command.SourceWarehouseId));
            }

            if (destinationWarehouse is null)
            {
                return ServiceError.NotFound<InventoryTransfer>("DestinationWarehouse not found", nameof(Command.DestinationWarehouseId));
            }

            if (!destinationWarehouse.IsActive)
            {
                return ServiceError.Validation<InventoryTransfer>("DestinationWarehouse is inactive", nameof(Command.DestinationWarehouseId));
            }

            if (command.TransitStorageLocationId is not Guid transitLocationId)
            {
                return null;
            }

            StorageLocation? transitLocation = await dbContext.StorageLocations
                .AsNoTracking()
                .Include(x => x.StorageLocationType)
                .Include(x => x.StorageLocationStatus)
                .FirstOrDefaultAsync(x => x.Id == transitLocationId, cancellationToken);

            if (transitLocation is null)
            {
                return ServiceError.NotFound<InventoryTransfer>("TransitStorageLocation not found", nameof(Command.TransitStorageLocationId));
            }

            if (transitLocation.WarehouseId != command.SourceWarehouseId.Value)
            {
                return ServiceError.Validation<InventoryTransfer>("TransitStorageLocation must belong to the transfer warehouse", nameof(Command.TransitStorageLocationId));
            }

            if (!transitLocation.IsActive)
            {
                return ServiceError.Validation<InventoryTransfer>("TransitStorageLocation is inactive", nameof(Command.TransitStorageLocationId));
            }

            if (!transitLocation.StorageLocationStatus.IsActive)
            {
                return ServiceError.Validation<InventoryTransfer>("TransitStorageLocation status is inactive", nameof(Command.TransitStorageLocationId));
            }

            if (!transitLocation.StorageLocationType.IsActive)
            {
                return ServiceError.Validation<InventoryTransfer>("TransitStorageLocation type is inactive", nameof(Command.TransitStorageLocationId));
            }

            if (transitLocation.StorageLocationType.Code == ExternalTransitLocationTypeCode)
            {
                return ServiceError.Validation<InventoryTransfer>("External transit behavior is out of scope for this MVP", nameof(Command.TransitStorageLocationId));
            }

            if (transitLocation.StorageLocationType.Code != InternalTransitLocationTypeCode)
            {
                return ServiceError.Validation<InventoryTransfer>("TransitStorageLocation must be an internal transit location", nameof(Command.TransitStorageLocationId));
            }

            return null;
        }

        private async Task<ServiceError?> ValidateLineReferencesAsync(
            Line line,
            Guid warehouseId,
            int index,
            CancellationToken cancellationToken)
        {
            StockKeepingUnit? sku = await dbContext.StockKeepingUnits
                .AsNoTracking()
                .Include(x => x.BaseUnitOfMeasure)
                .FirstOrDefaultAsync(x => x.Id == line.StockKeepingUnitId!.Value, cancellationToken);

            if (sku is null)
            {
                return ServiceError.NotFound<InventoryTransferLine>("StockKeepingUnit not found", LineProperty(index, nameof(Line.StockKeepingUnitId)));
            }

            if (!sku.IsActive)
            {
                return ServiceError.Validation<InventoryTransferLine>("StockKeepingUnit is inactive", LineProperty(index, nameof(Line.StockKeepingUnitId)));
            }

            if (!sku.BaseUnitOfMeasure.IsActive)
            {
                return ServiceError.Validation<InventoryTransferLine>("BaseUnitOfMeasure is inactive", LineProperty(index, nameof(Line.StockKeepingUnitId)));
            }

            StorageLocation? sourceLocation = await LoadLocationAsync(line.SourceStorageLocationId!.Value, cancellationToken);
            StorageLocation? destinationLocation = await LoadLocationAsync(line.DestinationStorageLocationId!.Value, cancellationToken);

            ServiceError? sourceError = ValidateRegularLocation(
                sourceLocation,
                warehouseId,
                LineProperty(index, nameof(Line.SourceStorageLocationId)),
                "SourceStorageLocation");

            if (sourceError is not null)
            {
                return sourceError;
            }

            ServiceError? destinationError = ValidateRegularLocation(
                destinationLocation,
                warehouseId,
                LineProperty(index, nameof(Line.DestinationStorageLocationId)),
                "DestinationStorageLocation");

            if (destinationError is not null)
            {
                return destinationError;
            }

            return null;
        }

        private async Task<StorageLocation?> LoadLocationAsync(
            Guid storageLocationId,
            CancellationToken cancellationToken)
        {
            return await dbContext.StorageLocations
                .AsNoTracking()
                .Include(x => x.StorageLocationType)
                .Include(x => x.StorageLocationStatus)
                .FirstOrDefaultAsync(x => x.Id == storageLocationId, cancellationToken);
        }

        private static ServiceError? ValidateRegularLocation(
            StorageLocation? location,
            Guid warehouseId,
            string property,
            string label)
        {
            if (location is null)
            {
                return ServiceError.NotFound<InventoryTransferLine>($"{label} not found", property);
            }

            if (location.WarehouseId != warehouseId)
            {
                return ServiceError.Validation<InventoryTransferLine>($"{label} must belong to the transfer warehouse", property);
            }

            if (!location.IsActive)
            {
                return ServiceError.Validation<InventoryTransferLine>($"{label} is inactive", property);
            }

            if (!location.StorageLocationStatus.IsActive)
            {
                return ServiceError.Validation<InventoryTransferLine>($"{label} status is inactive", property);
            }

            if (!location.StorageLocationType.IsActive)
            {
                return ServiceError.Validation<InventoryTransferLine>($"{label} type is inactive", property);
            }

            if (location.StorageLocationType.Code is InternalTransitLocationTypeCode or ExternalTransitLocationTypeCode)
            {
                return ServiceError.Validation<InventoryTransferLine>($"{label} must be a regular storage location", property);
            }

            return null;
        }

        private static string LineProperty(int index, string property)
        {
            return $"Lines[{index}].{property}";
        }

        private static string GenerateTransferCode()
        {
            string code = $"TR-{Guid.CreateVersion7():N}";

            return code.Length <= InventoryTransfer.CodeMaxLength
                ? code
                : code[..InventoryTransfer.CodeMaxLength];
        }
    }
}
