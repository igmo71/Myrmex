using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Myrmex.AppDispatching.CommandDispatching;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransactions;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryTransfers;
using Myrmex.Modules.Wms.Inventory.Features.InventoryAdjustments;
using Myrmex.Modules.Wms.Inventory.Features.InventoryCounts;
using Myrmex.Modules.Wms.Inventory.Features.InventoryTransfers;
using Myrmex.Modules.Wms.Topology.Domain.StorageLocations;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;
using Myrmex.Modules.Wms.Topology.Domain.Zones;
using Myrmex.Shared.Wms.DemoData;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.DemoData.Features;

internal sealed class WmsDemoDataSeeder(
    WmsDbContext dbContext,
    ICommandDispatcher commandDispatcher,
    IDomainEventDispatcher domainEventDispatcher,
    TimeProvider timeProvider,
    IWmsDemoDataStageHook stageHook,
    IHostEnvironment hostEnvironment,
    ILogger<WmsDemoDataSeeder> logger)
{
    private const string Savepoint = "SeedWmsDemoData";

    private static readonly string[] AreaOrder =
    [
        "unitsOfMeasure",
        "stockKeepingUnits",
        "warehouses",
        "zones",
        "storageLocations",
        "inventoryBalances",
        "inventoryTransactions",
        "inventoryLedgerEntries",
        "inventoryTransfers",
        "inventoryTransferLines",
        "inventoryTransferMovements",
        "inventoryCounts",
        "inventoryCountLines",
        "skuBarcodes"
    ];

    public async Task<ServiceResult<DemoDataOperationResponse>> SeedAsync(
        string actorId,
        CancellationToken cancellationToken)
    {
        DateTimeOffset startedAtUtc = timeProvider.GetUtcNow();

        logger.LogInformation(
            "WMS demo data seed attempted by actor {ActorId} in {Environment}.",
            actorId,
            hostEnvironment.EnvironmentName);

        if (!await IsDatabaseReadyAsync(cancellationToken))
        {
            logger.LogWarning(
                "WMS demo data seed rejected for actor {ActorId} in {Environment}; database is not ready.",
                actorId,
                hostEnvironment.EnvironmentName);
            return ServiceResult<DemoDataOperationResponse>.Fail(
                WmsDemoDataErrors.DatabaseNotReady());
        }

        Dictionary<string, int> before = await ReadAreaCountsAsync(cancellationToken);
        Dictionary<string, int> reused = AreaOrder.ToDictionary(x => x, _ => 0);
        Dictionary<string, int> skipped = AreaOrder.ToDictionary(x => x, _ => 0);

        await using WmsDemoDataTransaction transaction =
            await WmsDemoDataTransaction.BeginAsync(
                dbContext,
                Savepoint,
                cancellationToken);

        try
        {
            SeedContext context = await SeedReferenceDataAsync(reused, cancellationToken);
            await PersistDomainChangesAsync(cancellationToken);
            await StageCompletedAsync("referenceData", cancellationToken);

            await SeedOpeningsAsync(context, reused, cancellationToken);
            await StageCompletedAsync("openingInventory", cancellationToken);

            await SeedTransfersAsync(context, reused, cancellationToken);
            await StageCompletedAsync("transfers", cancellationToken);

            await SeedCountsAsync(context, actorId, reused, cancellationToken);
            await StageCompletedAsync("counts", cancellationToken);

            Dictionary<string, int> after = await ReadAreaCountsAsync(cancellationToken);
            DateTimeOffset completedAtUtc = timeProvider.GetUtcNow();
            DemoDataOperationResponse response = new(
                "seed",
                startedAtUtc,
                completedAtUtc,
                BuildAreas(before, after, reused, skipped));

            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "WMS demo data seed completed for actor {ActorId} in {Environment} after {DurationMilliseconds} ms. Created {Created}; reused {Reused}; skipped {Skipped}.",
                actorId,
                hostEnvironment.EnvironmentName,
                (completedAtUtc - startedAtUtc).TotalMilliseconds,
                response.Areas.Sum(x => x.Created),
                response.Areas.Sum(x => x.Reused),
                response.Areas.Sum(x => x.Skipped));

            return ServiceResult<DemoDataOperationResponse>.Success(response);
        }
        catch (DemoDataIdentityConflictException exception)
        {
            await transaction.RollbackAsync();
            dbContext.ChangeTracker.Clear();
            logger.LogWarning(
                "WMS demo data seed rejected for actor {ActorId} in {Environment}. Area {Area}; identity {Identity}.",
                actorId,
                hostEnvironment.EnvironmentName,
                exception.Area,
                exception.Identity);
            return ServiceResult<DemoDataOperationResponse>.Fail(
                WmsDemoDataErrors.IdentityConflict(exception.Area, exception.Identity));
        }
        catch (DemoDataCommandException exception)
        {
            await transaction.RollbackAsync();
            dbContext.ChangeTracker.Clear();
            logger.LogWarning(
                "WMS demo data seed rolled back for actor {ActorId} in {Environment}. Failure {FailureCode}.",
                actorId,
                hostEnvironment.EnvironmentName,
                exception.Error.Code);
            return ServiceResult<DemoDataOperationResponse>.Fail(exception.Error);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await transaction.RollbackAsync();
            dbContext.ChangeTracker.Clear();
            logger.LogWarning(
                "WMS demo data seed cancelled for actor {ActorId} in {Environment}.",
                actorId,
                hostEnvironment.EnvironmentName);
            throw;
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            dbContext.ChangeTracker.Clear();
            logger.LogError(
                exception,
                "WMS demo data seed failed and rolled back for actor {ActorId} in {Environment}.",
                actorId,
                hostEnvironment.EnvironmentName);
            return ServiceResult<DemoDataOperationResponse>.Fail(
                WmsDemoDataErrors.ExecutionFailed());
        }
    }

    private async Task<bool> IsDatabaseReadyAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!await dbContext.Database.CanConnectAsync(cancellationToken))
            {
                return false;
            }

            IEnumerable<string> pendingMigrations = await dbContext.Database
                .GetPendingMigrationsAsync(cancellationToken);
            return !pendingMigrations.Any();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "WMS demo database readiness check failed.");
            return false;
        }
    }

    private async Task<SeedContext> SeedReferenceDataAsync(
        IDictionary<string, int> reused,
        CancellationToken cancellationToken)
    {
        string[] requiredTypeCodes =
            ["DOCK", "PALLET_RACK", "SHELF", "STAGING", "FLOOR", "INTERNAL_TRANSIT"];
        Dictionary<string, StorageLocationType> types = await dbContext.StorageLocationTypes
            .Where(x => requiredTypeCodes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, cancellationToken);
        Dictionary<string, StorageLocationStatus> statuses = await dbContext.StorageLocationStatuses
            .Where(x => x.Code == "AVAILABLE" || x.Code == "BLOCKED")
            .ToDictionaryAsync(x => x.Code, cancellationToken);

        foreach (string code in requiredTypeCodes)
        {
            if (!types.TryGetValue(code, out StorageLocationType? type) || !type.IsActive)
            {
                throw new DemoDataIdentityConflictException("storageLocationTypes", code);
            }
        }

        foreach (string code in new[] { "AVAILABLE", "BLOCKED" })
        {
            if (!statuses.TryGetValue(code, out StorageLocationStatus? status) || !status.IsActive)
            {
                throw new DemoDataIdentityConflictException("storageLocationStatuses", code);
            }
        }

        string[] unitCodes = DemoDataDefinitions.Units.Select(x => x.Code).ToArray();
        Dictionary<string, UnitOfMeasure> units = await dbContext.UnitsOfMeasure
            .Where(x => unitCodes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, cancellationToken);

        foreach (DemoDataDefinitions.UnitDefinition definition in DemoDataDefinitions.Units)
        {
            if (units.TryGetValue(definition.Code, out UnitOfMeasure? existing))
            {
                Ensure(existing.Name == definition.Name &&
                       existing.Symbol == definition.Symbol &&
                       existing.IsActive,
                    "unitsOfMeasure",
                    definition.Code);
                reused["unitsOfMeasure"]++;
                continue;
            }

            DomainValidationResult result = UnitOfMeasure.Create(
                definition.Code,
                definition.Name,
                definition.Symbol,
                out UnitOfMeasure? created);
            EnsureValid(result, "unitsOfMeasure", definition.Code);
            dbContext.UnitsOfMeasure.Add(created!);
            units[definition.Code] = created!;
        }

        string[] skuCodes = DemoDataDefinitions.Skus.Select(x => x.Code).ToArray();
        Dictionary<string, StockKeepingUnit> skus = await dbContext.StockKeepingUnits
            .Where(x => skuCodes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, cancellationToken);

        foreach (DemoDataDefinitions.SkuDefinition definition in DemoDataDefinitions.Skus)
        {
            UnitOfMeasure unit = units[definition.UnitCode];
            if (skus.TryGetValue(definition.Code, out StockKeepingUnit? existing))
            {
                Ensure(existing.Name == definition.Name &&
                       existing.Description == definition.Description &&
                       existing.BaseUnitOfMeasureId == unit.Id &&
                       existing.IsActive,
                    "stockKeepingUnits",
                    definition.Code);
                reused["stockKeepingUnits"]++;
                continue;
            }

            DomainValidationResult result = StockKeepingUnit.Create(
                definition.Code,
                definition.Name,
                definition.Description,
                unit.Id,
                out StockKeepingUnit? created);
            EnsureValid(result, "stockKeepingUnits", definition.Code);
            dbContext.StockKeepingUnits.Add(created!);
            skus[definition.Code] = created!;
        }

        Warehouse? warehouse = await dbContext.Warehouses
            .SingleOrDefaultAsync(
                x => x.Code == DemoDataDefinitions.Warehouse.Code,
                cancellationToken);

        if (warehouse is null)
        {
            DomainValidationResult result = Warehouse.Create(
                DemoDataDefinitions.Warehouse.Code,
                DemoDataDefinitions.Warehouse.Name,
                DemoDataDefinitions.Warehouse.Description,
                out warehouse);
            EnsureValid(result, "warehouses", DemoDataDefinitions.Warehouse.Code);
            dbContext.Warehouses.Add(warehouse!);
        }
        else
        {
            Ensure(warehouse.Name == DemoDataDefinitions.Warehouse.Name &&
                   warehouse.Description == DemoDataDefinitions.Warehouse.Description &&
                   warehouse.IsActive,
                "warehouses",
                DemoDataDefinitions.Warehouse.Code);
            reused["warehouses"]++;
        }

        Warehouse validWarehouse = warehouse!;

        string[] zoneCodes = DemoDataDefinitions.Zones.Select(x => x.Code).ToArray();
        Dictionary<string, Zone> zones = await dbContext.Zones
            .Where(x => x.WarehouseId == validWarehouse.Id)
            .Where(x => zoneCodes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, cancellationToken);

        foreach (DemoDataDefinitions.ZoneDefinition definition in DemoDataDefinitions.Zones)
        {
            if (zones.TryGetValue(definition.Code, out Zone? existing))
            {
                Ensure(existing.Name == definition.Name &&
                       existing.Description == definition.Description &&
                       existing.IsActive,
                    "zones",
                    definition.Code);
                reused["zones"]++;
                continue;
            }

            DomainValidationResult result = Zone.Create(
                validWarehouse.Id,
                definition.Code,
                definition.Name,
                definition.Description,
                out Zone? created);
            EnsureValid(result, "zones", definition.Code);
            dbContext.Zones.Add(created!);
            zones[definition.Code] = created!;
        }

        string[] locationCodes = DemoDataDefinitions.Locations.Select(x => x.Code).ToArray();
        Dictionary<string, StorageLocation> locations = await dbContext.StorageLocations
            .Where(x => x.WarehouseId == validWarehouse.Id)
            .Where(x => locationCodes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, cancellationToken);

        foreach (DemoDataDefinitions.LocationDefinition definition in DemoDataDefinitions.Locations)
        {
            Zone zone = zones[definition.ZoneCode];
            StorageLocationType type = types[definition.TypeCode];
            StorageLocationStatus status = statuses[definition.StatusCode];

            if (locations.TryGetValue(definition.Code, out StorageLocation? existing))
            {
                Ensure(existing.Name == definition.Name &&
                       existing.ZoneId == zone.Id &&
                       existing.StorageLocationTypeId == type.Id &&
                       existing.StorageLocationStatusId == status.Id &&
                       existing.IsPickable == definition.IsPickable &&
                       existing.IsActive,
                    "storageLocations",
                    definition.Code);
                reused["storageLocations"]++;
                continue;
            }

            DomainValidationResult result = StorageLocation.Create(
                validWarehouse.Id,
                zone.Id,
                type.Id,
                status.Id,
                definition.Code,
                definition.Name,
                description: null,
                isPickable: definition.IsPickable,
                storageLocation: out StorageLocation? created);
            EnsureValid(result, "storageLocations", definition.Code);
            dbContext.StorageLocations.Add(created!);
            locations[definition.Code] = created!;
        }

        return new SeedContext(validWarehouse, units, skus, zones, locations);
    }

    private async Task SeedOpeningsAsync(
        SeedContext context,
        IDictionary<string, int> reused,
        CancellationToken cancellationToken)
    {
        foreach (DemoDataDefinitions.OpeningDefinition definition in DemoDataDefinitions.Openings)
        {
            string reason = DemoDataDefinitions.OpeningReason(definition);
            InventoryTransaction[] matches = await dbContext.InventoryTransactions
                .Include(x => x.Entries)
                .Where(x => x.Reason == reason)
                .ToArrayAsync(cancellationToken);

            if (matches.Length > 0)
            {
                Ensure(matches.Length == 1, "inventoryTransactions", reason);
                InventoryTransaction transaction = matches[0];
                Ensure(transaction.TransactionType == InventoryTransactionType.Adjustment &&
                       transaction.Entries.Count == 1,
                    "inventoryTransactions",
                    reason);
                var entry = transaction.Entries.Single();
                Ensure(entry.StockKeepingUnitId == context.Skus[definition.SkuCode].Id &&
                       entry.StorageLocationId == context.Locations[definition.LocationCode].Id &&
                       entry.BalanceBefore == 0 &&
                       entry.BalanceAfter == definition.Quantity,
                    "inventoryTransactions",
                    reason);
                reused["inventoryTransactions"]++;
                reused["inventoryLedgerEntries"]++;
                reused["inventoryBalances"]++;
                continue;
            }

            bool balanceExists = await dbContext.InventoryBalances.AnyAsync(
                x => x.StockKeepingUnitId == context.Skus[definition.SkuCode].Id &&
                     x.StorageLocationId == context.Locations[definition.LocationCode].Id,
                cancellationToken);
            Ensure(!balanceExists, "inventoryBalances", reason);

            var command = new AdjustInventoryBalance.Command(
                context.Skus[definition.SkuCode].Id,
                context.Locations[definition.LocationCode].Id,
                definition.Quantity,
                reason,
                ExpectedBalanceVersion: null);
            ServiceResult<InventoryBalanceDetails> result = await commandDispatcher
                .DispatchAsync<AdjustInventoryBalance.Command, ServiceResult<InventoryBalanceDetails>>(
                    command,
                    cancellationToken);
            EnsureSuccess(result);
        }
    }

    private async Task SeedTransfersAsync(
        SeedContext context,
        IDictionary<string, int> reused,
        CancellationToken cancellationToken)
    {
        foreach (DemoDataDefinitions.TransferDefinition definition in DemoDataDefinitions.Transfers)
        {
            InventoryTransfer[] matches = await dbContext.InventoryTransfers
                .Include(x => x.Lines)
                .Include(x => x.Movements)
                .Where(x => x.Code == definition.Code)
                .ToArrayAsync(cancellationToken);

            InventoryTransfer transfer;
            if (matches.Length == 0)
            {
                DomainValidationResult lineResult = InventoryTransferLine.Create(
                    context.Skus[definition.SkuCode].Id,
                    context.Locations[definition.SourceCode].Id,
                    context.Locations[definition.DestinationCode].Id,
                    definition.Quantity,
                    out InventoryTransferLine? line);
                EnsureValid(lineResult, "inventoryTransferLines", definition.Code);

                DomainValidationResult transferResult = InventoryTransfer.Create(
                    definition.Code,
                    context.Warehouse.Id,
                    context.Warehouse.Id,
                    definition.TransitCode is null
                        ? null
                        : context.Locations[definition.TransitCode].Id,
                    [line!],
                    out InventoryTransfer? created);
                EnsureValid(transferResult, "inventoryTransfers", definition.Code);
                transfer = created!;
                dbContext.InventoryTransfers.Add(transfer);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                Ensure(matches.Length == 1, "inventoryTransfers", definition.Code);
                transfer = matches[0];
                EnsureTransferCompatible(transfer, definition, context);
                reused["inventoryTransfers"]++;
                reused["inventoryTransferLines"] += transfer.Lines.Count;
                reused["inventoryTransferMovements"] += transfer.Movements.Count;
                Guid[] movementTransactionIds = transfer.Movements
                    .Select(x => x.InventoryTransactionId)
                    .Distinct()
                    .ToArray();
                int transactionCount = await dbContext.InventoryTransactions
                    .CountAsync(x => movementTransactionIds.Contains(x.Id), cancellationToken);
                int ledgerEntryCount = await dbContext.InventoryLedgerEntries
                    .CountAsync(x => movementTransactionIds.Contains(x.InventoryTransactionId), cancellationToken);
                Ensure(transactionCount == movementTransactionIds.Length,
                    "inventoryTransactions", definition.Code);
                Ensure(ledgerEntryCount == movementTransactionIds.Length * 2,
                    "inventoryLedgerEntries", definition.Code);
                reused["inventoryTransactions"] += transactionCount;
                reused["inventoryLedgerEntries"] += ledgerEntryCount;
            }

            InventoryTransferLine transferLine = transfer.Lines.Single();
            decimal moved = transferLine.GetMovedQuantity(transfer.Movements);
            decimal picked = transferLine.GetPickedQuantity(transfer.Movements);
            decimal placed = transferLine.GetPlacedQuantity(transfer.Movements);

            switch (definition.Target)
            {
                case DemoDataDefinitions.TransferTarget.Created:
                    Ensure(transfer.Movements.Count == 0, "inventoryTransfers", definition.Code);
                    break;
                case DemoDataDefinitions.TransferTarget.CompletedDirect:
                    Ensure(!transfer.UsesTransit && moved <= definition.Quantity,
                        "inventoryTransfers", definition.Code);
                    if (moved < definition.Quantity)
                    {
                        ServiceResult<InventoryTransferDetails> result = await commandDispatcher
                            .DispatchAsync<MoveInventoryTransferLine.Command, ServiceResult<InventoryTransferDetails>>(
                                new MoveInventoryTransferLine.Command(
                                    transfer.Id,
                                    transferLine.Id,
                                    definition.Quantity - moved),
                                cancellationToken);
                        EnsureSuccess(result);
                    }
                    break;
                case DemoDataDefinitions.TransferTarget.PickedToTransit:
                    Ensure(transfer.UsesTransit && placed == 0 && picked <= definition.Quantity,
                        "inventoryTransfers", definition.Code);
                    if (picked < definition.Quantity)
                    {
                        ServiceResult<InventoryTransferDetails> result = await commandDispatcher
                            .DispatchAsync<PickInventoryTransferLine.Command, ServiceResult<InventoryTransferDetails>>(
                                new PickInventoryTransferLine.Command(
                                    transfer.Id,
                                    transferLine.Id,
                                    definition.Quantity - picked),
                                cancellationToken);
                        EnsureSuccess(result);
                    }
                    break;
                case DemoDataDefinitions.TransferTarget.CompletedTransit:
                    Ensure(transfer.UsesTransit && picked <= definition.Quantity && placed <= definition.Quantity,
                        "inventoryTransfers", definition.Code);
                    if (picked < definition.Quantity)
                    {
                        ServiceResult<InventoryTransferDetails> pick = await commandDispatcher
                            .DispatchAsync<PickInventoryTransferLine.Command, ServiceResult<InventoryTransferDetails>>(
                                new PickInventoryTransferLine.Command(
                                    transfer.Id,
                                    transferLine.Id,
                                    definition.Quantity - picked),
                                cancellationToken);
                        EnsureSuccess(pick);
                    }
                    if (placed < definition.Quantity)
                    {
                        ServiceResult<InventoryTransferDetails> place = await commandDispatcher
                            .DispatchAsync<PlaceInventoryTransferLine.Command, ServiceResult<InventoryTransferDetails>>(
                                new PlaceInventoryTransferLine.Command(
                                    transfer.Id,
                                    transferLine.Id,
                                    definition.Quantity - placed),
                                cancellationToken);
                        EnsureSuccess(place);
                    }
                    break;
            }
        }
    }

    private async Task SeedCountsAsync(
        SeedContext context,
        string actorId,
        IDictionary<string, int> reused,
        CancellationToken cancellationToken)
    {
        await EnsureCountAsync(
            context,
            DemoDataDefinitions.OpenCountReason,
            complete: false,
            [
                new("SKU-SCR-GVL-3.9X19", "PICK-A-01-01", 0),
                new("SKU-SCR-GVL-3.9X25", "PICK-A-01-02", -5),
                new("SKU-SCR-GVL-3.9X30", "PICK-B-01-01", 7)
            ],
            actorId,
            reused,
            cancellationToken);

        await EnsureCountAsync(
            context,
            DemoDataDefinitions.ClosedCountReason,
            complete: true,
            [
                new("SKU-SCR-GVL-3.9X19", "BULK-A-01-01", 0),
                new("SKU-SCR-GVL-3.9X25", "BULK-A-01-02", 0)
            ],
            actorId,
            reused,
            cancellationToken);
    }

    private async Task EnsureCountAsync(
        SeedContext context,
        string reason,
        bool complete,
        IReadOnlyList<CountLineDefinition> definitions,
        string actorId,
        IDictionary<string, int> reused,
        CancellationToken cancellationToken)
    {
        var matches = await dbContext.InventoryCounts
            .Where(x => x.Reason == reason)
            .Select(x => x.Id)
            .ToArrayAsync(cancellationToken);
        Ensure(matches.Length <= 1, "inventoryCounts", reason);

        InventoryCountDetails details;
        if (matches.Length == 0)
        {
            ServiceResult<InventoryCountDetails> create = await commandDispatcher
                .DispatchAsync<CreateInventoryCount.Command, ServiceResult<InventoryCountDetails>>(
                    new CreateInventoryCount.Command(
                        context.Warehouse.Id,
                        reason,
                        actorId),
                    cancellationToken);
            EnsureSuccess(create);
            details = create.Value;
        }
        else
        {
            ServiceResult<InventoryCountDetails> load =
                await CreateInventoryCount.LoadDetailsAsync(
                    dbContext,
                    matches[0],
                    cancellationToken);
            EnsureSuccess(load);
            details = load.Value;
            reused["inventoryCounts"]++;
            reused["inventoryCountLines"] += details.Lines.Count;
        }

        Ensure(details.Warehouse.Id == context.Warehouse.Id,
            "inventoryCounts", reason);

        foreach (CountLineDefinition definition in definitions)
        {
            InventoryCountLineDetails? line = details.Lines.SingleOrDefault(
                x => x.IsCurrent &&
                     x.Sku.Code == definition.SkuCode &&
                     x.StorageLocation.Code == definition.LocationCode);

            if (line is null)
            {
                ServiceResult<InventoryCountDetails> add = await commandDispatcher
                    .DispatchAsync<AddInventoryCountLine.Command, ServiceResult<InventoryCountDetails>>(
                        new AddInventoryCountLine.Command(
                            details.Id,
                            context.Skus[definition.SkuCode].Id,
                            context.Locations[definition.LocationCode].Id,
                            details.CountVersion,
                            actorId),
                        cancellationToken);
                EnsureSuccess(add);
                details = add.Value;
                line = details.Lines.Single(x =>
                    x.IsCurrent &&
                    x.Sku.Code == definition.SkuCode &&
                    x.StorageLocation.Code == definition.LocationCode);
            }

            decimal countedQuantity = line.SystemQuantity + definition.Variance;
            Ensure(countedQuantity >= 0, "inventoryCountLines", reason);

            if (line.Status == "Pending")
            {
                ServiceResult<InventoryCountDetails> record = await commandDispatcher
                    .DispatchAsync<RecordInventoryCountLine.Command, ServiceResult<InventoryCountDetails>>(
                        new RecordInventoryCountLine.Command(
                            details.Id,
                            line.Id,
                            countedQuantity,
                            "Демонстрационный результат пересчёта",
                            line.LineVersion,
                            actorId),
                        cancellationToken);
                EnsureSuccess(record);
                details = record.Value;
                line = details.Lines.Single(x => x.Id == line.Id);
            }

            Ensure(line.CountedQuantity == countedQuantity,
                "inventoryCountLines", $"{reason}:{definition.SkuCode}:{definition.LocationCode}");

            if (complete && line.Status == "Counted")
            {
                ServiceResult<InventoryCountDetails> apply = await commandDispatcher
                    .DispatchAsync<ApplyInventoryCountLine.Command, ServiceResult<InventoryCountDetails>>(
                        new ApplyInventoryCountLine.Command(
                            details.Id,
                            line.Id,
                            line.LineVersion,
                            actorId),
                        cancellationToken);
                EnsureSuccess(apply);
                details = apply.Value;
            }
            else if (!complete)
            {
                Ensure(line.Status == "Counted",
                    "inventoryCountLines", reason);
            }
        }

        Ensure(details.Lines.Count(x => x.IsCurrent) == definitions.Count,
            "inventoryCountLines", reason);

        if (complete && details.Status != "Completed")
        {
            ServiceResult<InventoryCountDetails> completeResult = await commandDispatcher
                .DispatchAsync<CompleteInventoryCount.Command, ServiceResult<InventoryCountDetails>>(
                    new CompleteInventoryCount.Command(
                        details.Id,
                        details.CountVersion,
                        actorId),
                    cancellationToken);
            EnsureSuccess(completeResult);
            details = completeResult.Value;
        }

        Ensure(details.Status == (complete ? "Completed" : "InProgress"),
            "inventoryCounts", reason);
    }

    private async Task PersistDomainChangesAsync(CancellationToken cancellationToken)
    {
        ServiceResult result = await dbContext.SaveChangesAsServiceResultAsync(
            domainEventDispatcher,
            cancellationToken);
        if (!result.IsSuccess)
        {
            throw new DemoDataCommandException(result.Error);
        }
    }

    private Task StageCompletedAsync(string stage, CancellationToken cancellationToken) =>
        stageHook.StageCompletedAsync("seed", stage, cancellationToken);

    private async Task<Dictionary<string, int>> ReadAreaCountsAsync(
        CancellationToken cancellationToken) => new()
        {
            ["unitsOfMeasure"] = await dbContext.UnitsOfMeasure.CountAsync(cancellationToken),
            ["stockKeepingUnits"] = await dbContext.StockKeepingUnits.CountAsync(cancellationToken),
            ["warehouses"] = await dbContext.Warehouses.CountAsync(cancellationToken),
            ["zones"] = await dbContext.Zones.CountAsync(cancellationToken),
            ["storageLocations"] = await dbContext.StorageLocations.CountAsync(cancellationToken),
            ["inventoryBalances"] = await dbContext.InventoryBalances.CountAsync(cancellationToken),
            ["inventoryTransactions"] = await dbContext.InventoryTransactions.CountAsync(cancellationToken),
            ["inventoryLedgerEntries"] = await dbContext.InventoryLedgerEntries.CountAsync(cancellationToken),
            ["inventoryTransfers"] = await dbContext.InventoryTransfers.CountAsync(cancellationToken),
            ["inventoryTransferLines"] = await dbContext.InventoryTransferLines.CountAsync(cancellationToken),
            ["inventoryTransferMovements"] = await dbContext.InventoryTransferMovements.CountAsync(cancellationToken),
            ["inventoryCounts"] = await dbContext.InventoryCounts.CountAsync(cancellationToken),
            ["inventoryCountLines"] = await dbContext.InventoryCountLines.CountAsync(cancellationToken),
            ["skuBarcodes"] = await dbContext.SkuBarcodes.CountAsync(cancellationToken)
        };

    private static IReadOnlyList<DemoDataAreaSummary> BuildAreas(
        IReadOnlyDictionary<string, int> before,
        IReadOnlyDictionary<string, int> after,
        IReadOnlyDictionary<string, int> reused,
        IReadOnlyDictionary<string, int> skipped) =>
        AreaOrder.Select(area => new DemoDataAreaSummary(
            area,
            Math.Max(0, after[area] - before[area]),
            reused[area],
            skipped[area],
            Deleted: 0)).ToArray();

    private static void EnsureTransferCompatible(
        InventoryTransfer transfer,
        DemoDataDefinitions.TransferDefinition definition,
        SeedContext context)
    {
        Ensure(transfer.SourceWarehouseId == context.Warehouse.Id &&
               transfer.DestinationWarehouseId == context.Warehouse.Id &&
               transfer.TransitStorageLocationId ==
                   (definition.TransitCode is null
                       ? null
                       : context.Locations[definition.TransitCode].Id) &&
               transfer.Lines.Count == 1,
            "inventoryTransfers",
            definition.Code);

        InventoryTransferLine line = transfer.Lines.Single();
        Ensure(line.StockKeepingUnitId == context.Skus[definition.SkuCode].Id &&
               line.SourceStorageLocationId == context.Locations[definition.SourceCode].Id &&
               line.DestinationStorageLocationId == context.Locations[definition.DestinationCode].Id &&
               line.RequestedQuantity == definition.Quantity,
            "inventoryTransferLines",
            definition.Code);
    }

    private static void EnsureSuccess<T>(ServiceResult<T> result)
    {
        if (!result.IsSuccess)
        {
            throw new DemoDataCommandException(result.Error);
        }
    }

    private static void EnsureValid(
        DomainValidationResult result,
        string area,
        string identity) => Ensure(result.IsValid, area, identity);

    private static void Ensure(bool condition, string area, string identity)
    {
        if (!condition)
        {
            throw new DemoDataIdentityConflictException(area, identity);
        }
    }

    private sealed record SeedContext(
        Warehouse Warehouse,
        IReadOnlyDictionary<string, UnitOfMeasure> Units,
        IReadOnlyDictionary<string, StockKeepingUnit> Skus,
        IReadOnlyDictionary<string, Zone> Zones,
        IReadOnlyDictionary<string, StorageLocation> Locations);

    private sealed record CountLineDefinition(
        string SkuCode,
        string LocationCode,
        decimal Variance);
}

internal sealed class DemoDataIdentityConflictException(
    string area,
    string identity) : Exception("A stable demo identity is incompatible.")
{
    public string Area { get; } = area;
    public string Identity { get; } = identity;
}

internal sealed class DemoDataCommandException(ServiceError error)
    : Exception(error.Message)
{
    public ServiceError Error { get; } = error;
}
