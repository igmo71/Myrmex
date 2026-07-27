using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Myrmex.Core.Application;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Receiving.Domain.ReceivingOrders;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Features.StorageLocations;
using Myrmex.Shared.Wms.Topology;

namespace Myrmex.Modules.Wms.Receiving.Features.ReceivingOrders;

public static class ImportExternalReceivingOrder
{
    public sealed record Line(string ExternalLineId, Guid StockKeepingUnitExternalRefKey, decimal PlannedQuantity);
    public sealed record Document(Guid ExternalRefKey, byte[] DataVersion, string Number, DateTime SourceDate, Guid WarehouseExternalRefKey, IReadOnlyList<Line> Lines);
    public sealed record Result(string Outcome, string? Reason, string? Message);
    public sealed record Command(Document Document, DateTimeOffset ImportedAtUtc) : ICommand<ServiceResult<Result>>;

    internal sealed class Handler(WmsDbContext dbContext, ILogger<Handler> logger)
        : ICommandHandler<Command, ServiceResult<Result>>
    {
        public async Task<ServiceResult<Result>> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            Document source = command.Document;

            Warehouse? warehouse = await dbContext.Warehouses
                .SingleOrDefaultAsync(x => x.ImportState != null && x.ImportState.RefKey == source.WarehouseExternalRefKey, cancellationToken);

            if (warehouse is null || !warehouse.IsActive)
            {
                return Fail("WarehouseNotImported", "The source Warehouse is not available locally.");
            }
            if (warehouse.DefaultReceivingLocationId is not Guid locationId)
            {
                return Fail("ReceivingLocationNotConfigured", "The Warehouse has no default Receiving location.");
            }

            StorageLocation? location = await dbContext.StorageLocations
                .Include(x => x.StorageLocationType)
                .Include(x => x.StorageLocationStatus)
                .SingleOrDefaultAsync(x => x.Id == locationId, cancellationToken);

            if (location is null ||
                location.WarehouseId != warehouse.Id ||
                location.StorageLocationType is null ||
                !StorageLocationEligibility.Evaluate(location).IsSelectable ||
                !string.Equals(location.StorageLocationType.Code, StorageLocationTypeCodes.Receiving, StringComparison.Ordinal))
            {
                return Fail("ReceivingLocationNotConfigured", "The Warehouse default Receiving location is invalid.");
            }

            Guid[] skuKeys = source.Lines
                .Select(x => x.StockKeepingUnitExternalRefKey)
                .Distinct()
                .ToArray();

            Dictionary<Guid, StockKeepingUnit> skus = await dbContext.StockKeepingUnits
                .Include(x => x.BaseUnitOfMeasure)
                .Where(x => x.ImportState != null && skuKeys.Contains(x.ImportState.RefKey))
                .ToDictionaryAsync(x => x.ImportState!.RefKey, cancellationToken);

            if (skus.Count != skuKeys.Length || skus.Values.Any(x => !x.IsActive || x.BaseUnitOfMeasure is null || !x.BaseUnitOfMeasure.IsActive))
            {
                return Fail("SkuNotImported", "One or more source SKUs or their base units are unavailable locally.");
            }

            List<(Guid StockKeepingUnitId, decimal PlannedQuantity)> mappedLines = source.Lines
                .GroupBy(x => skus[x.StockKeepingUnitExternalRefKey].Id)
                .Select(x => (
                    StockKeepingUnitId: x.Key,
                    PlannedQuantity: x.Sum(line => line.PlannedQuantity)))
                .ToList();

            ReceivingOrder? existing = await dbContext.ReceivingOrders
                .Include(x => x.Lines)
                .SingleOrDefaultAsync(x => x.ImportState != null && x.ImportState.RefKey == source.ExternalRefKey, cancellationToken);

            if (existing is not null)
            {
                if (existing.Status != ReceivingOrderStatus.Draft)
                {
                    return ServiceResult<Result>.Success(new("Skipped", "NonDraft", "The matching receiving order is no longer Draft."));
                }

                Dictionary<Guid, Guid> existingLineIdBySku = existing.Lines
                    .ToDictionary(line => line.StockKeepingUnitId, line => line.Id);

                List<ReceivingOrder.DraftLine> lines = mappedLines
                    .Select(line =>
                    {
                        Guid? lineId = existingLineIdBySku.TryGetValue(
                            line.StockKeepingUnitId,
                            out Guid existingLineId)
                            ? existingLineId
                            : null;

                        return new ReceivingOrder.DraftLine(
                            lineId,
                            line.StockKeepingUnitId,
                            line.PlannedQuantity);
                    })
                    .ToList();

                bool equal = existing.Number == source.Number &&
                     existing.WarehouseId == warehouse.Id &&
                     existing.ReceivingLocationId == location.Id &&
                     existing.Lines.Count == lines.Count &&
                     existing.Lines.All(line =>
                        lines.Any(candidate =>
                            candidate.LineId == line.Id &&
                            candidate.StockKeepingUnitId == line.StockKeepingUnitId &&
                            candidate.PlannedQuantity == line.PlannedQuantity));
                if (equal)
                {
                    existing.RecordExternalImport(source.ExternalRefKey, source.DataVersion, command.ImportedAtUtc);

                    ServiceError? metadataError = await SaveAsync(dbContext, cancellationToken);

                    if (metadataError is not null)
                        return ServiceResult<Result>.Fail(metadataError);

                    return ServiceResult<Result>.Success(new("Skipped", "Unchanged", "The mapped Draft plan is unchanged."));
                }

                Dictionary<Guid, Guid> persistedSkuByLineId = existing.Lines.ToDictionary(line => line.Id, line => line.StockKeepingUnitId);


                var replacement = ReceivingOrderDraftReconciler
                    .Replace(existing, source.Number, warehouse.Id, location.Id, lines, out IReadOnlyList<ReceivingOrderLine> removed);

                if (!replacement.IsValid)
                    return ServiceResult<Result>.Invalid(replacement.Errors);

                existing.RecordExternalImport(source.ExternalRefKey, source.DataVersion, command.ImportedAtUtc);

                ServiceError? persistenceError = await ReceivingOrderDraftReconciler
                    .PersistAsync(dbContext, existing, persistedSkuByLineId, removed, nameof(Command), cancellationToken);

                if (persistenceError is not null)
                    return ServiceResult<Result>.Fail(persistenceError);

                return ServiceResult<Result>.Success(new("Updated", null, null));
            }

            List<ReceivingOrder.DraftLine> creationLines = mappedLines
                .Select(line => new ReceivingOrder.DraftLine(null, line.StockKeepingUnitId, line.PlannedQuantity))
                .ToList();

            var creation = ReceivingOrder.Create(source.Number, warehouse.Id, location.Id, creationLines, out ReceivingOrder? order);

            if (!creation.IsValid)
                return ServiceResult<Result>.Invalid(creation.Errors);

            order!.RecordExternalImport(source.ExternalRefKey, source.DataVersion, command.ImportedAtUtc);

            dbContext.ReceivingOrders.Add(order);

            ServiceError? creationError = await SaveAsync(dbContext, cancellationToken);

            if (creationError is not null)
                return ServiceResult<Result>.Fail(creationError);

            logger.LogInformation("External receiving order {ExternalRefKey} created local order {ReceivingOrderId}.", source.ExternalRefKey, order.Id);

            return ServiceResult<Result>.Success(new("Created", null, null));
        }

        private static ServiceResult<Result> Fail(string reason, string message) =>
            ServiceResult<Result>.Success(new("Failed", reason, message));

        private static async Task<ServiceError?> SaveAsync(WmsDbContext dbContext, CancellationToken cancellationToken)
        {
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return null;
            }
            catch (DbUpdateException exception)
            {
                dbContext.ChangeTracker.Clear();
                ServiceError? error = WmsPersistenceExceptionMapper.TryMap(exception);
                if (error is not null)
                {
                    return error;
                }

                throw;
            }
        }
    }
}
