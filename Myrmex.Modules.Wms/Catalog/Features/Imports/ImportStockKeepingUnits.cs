using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Domain.StockKeepingUnits;
using Myrmex.Modules.Wms.Catalog.Domain.UnitsOfMeasure;
using Myrmex.Modules.Wms.Infrastructure.Persistence;

namespace Myrmex.Modules.Wms.Catalog.Features.Imports;

public static class ImportStockKeepingUnits
{
    private const string SavepointName = "ImportStockKeepingUnitsBatch";

    public sealed record Command(IReadOnlyList<Item> Items)
        : ICommand<ServiceResult<ReferenceImportBatchResult>>;

    public sealed record Item(
        Guid ExternalRefKey,
        string? Code,
        string? Name,
        Guid? BaseUnitOfMeasureExternalRefKey,
        bool IsDeletionMarked,
        DateTimeOffset ImportedAtUtc);

    internal sealed class Handler(
        WmsDbContext dbContext,
        IDomainEventDispatcher domainEventDispatcher)
        : ICommandHandler<Command, ServiceResult<ReferenceImportBatchResult>>
    {
        public async Task<ServiceResult<ReferenceImportBatchResult>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(command.Items);

            IDbContextTransaction? ownedTransaction = null;
            IDbContextTransaction transaction;
            if (dbContext.Database.CurrentTransaction is null)
            {
                ownedTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                transaction = ownedTransaction;
            }
            else
            {
                transaction = dbContext.Database.CurrentTransaction;
                await transaction.CreateSavepointAsync(SavepointName, cancellationToken);
            }

            try
            {
                ReferenceImportBatchResult batchResult = await ApplyBatchAsync(command, cancellationToken);
                ServiceResult saveResult = await dbContext.SaveChangesAsServiceResultAsync(
                    domainEventDispatcher,
                    cancellationToken);

                if (!saveResult.IsSuccess)
                {
                    await RollbackAsync(transaction, ownedTransaction is not null);
                    dbContext.ChangeTracker.Clear();
                    return ServiceResult<ReferenceImportBatchResult>.Fail(saveResult.Error);
                }

                if (ownedTransaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
                else
                {
                    await transaction.ReleaseSavepointAsync(SavepointName, cancellationToken);
                }

                return ServiceResult<ReferenceImportBatchResult>.Success(batchResult);
            }
            catch
            {
                await RollbackAsync(transaction, ownedTransaction is not null);
                dbContext.ChangeTracker.Clear();
                throw;
            }
            finally
            {
                if (ownedTransaction is not null)
                {
                    await ownedTransaction.DisposeAsync();
                }
            }
        }

        private async Task<ReferenceImportBatchResult> ApplyBatchAsync(
            Command command,
            CancellationToken cancellationToken)
        {
            HashSet<Guid> externalRefKeys = command.Items
                .Where(item => item.ExternalRefKey != Guid.Empty)
                .Select(item => item.ExternalRefKey)
                .ToHashSet();
            HashSet<string> codes = command.Items
                .Where(item => !item.IsDeletionMarked)
                .Select(item => DomainText.NormalizeCode(item.Code))
                .Where(code => code.Length > 0)
                .ToHashSet(StringComparer.Ordinal);
            HashSet<Guid> baseUnitExternalRefKeys = command.Items
                .Where(item => !item.IsDeletionMarked)
                .Select(item => item.BaseUnitOfMeasureExternalRefKey)
                .Where(key => key.HasValue && key.Value != Guid.Empty)
                .Select(key => key!.Value)
                .ToHashSet();

            List<StockKeepingUnit> existing = await dbContext.StockKeepingUnits
                .Where(sku =>
                    (sku.ExternalRefKey.HasValue && externalRefKeys.Contains(sku.ExternalRefKey.Value)) ||
                    codes.Contains(sku.Code))
                .ToListAsync(cancellationToken);
            List<UnitOfMeasure> baseUnits = await dbContext.UnitsOfMeasure
                .Where(unit => unit.ExternalRefKey.HasValue &&
                    baseUnitExternalRefKeys.Contains(unit.ExternalRefKey.Value))
                .ToListAsync(cancellationToken);

            Dictionary<Guid, StockKeepingUnit> byExternalRefKey = existing
                .Where(sku => sku.ExternalRefKey.HasValue)
                .ToDictionary(sku => sku.ExternalRefKey!.Value);
            Dictionary<string, StockKeepingUnit> byCode = existing
                .ToDictionary(sku => sku.Code, StringComparer.Ordinal);
            Dictionary<Guid, UnitOfMeasure> baseUnitsByExternalRefKey = baseUnits
                .ToDictionary(unit => unit.ExternalRefKey!.Value);

            int created = 0;
            int updated = 0;
            int skipped = 0;
            int failed = 0;
            List<ReferenceImportRecordError> errors = [];

            foreach (Item item in command.Items)
            {
                if (item.ExternalRefKey == Guid.Empty)
                {
                    failed++;
                    errors.Add(Error(item, ReferenceImportRecordErrorReasons.InvalidSourceRecord,
                        "ExternalRefKey is required."));
                    continue;
                }

                if (byExternalRefKey.TryGetValue(item.ExternalRefKey, out StockKeepingUnit? linked) &&
                    item.IsDeletionMarked)
                {
                    DomainValidationResult deletionResult = linked.ApplyImport(
                        item.ExternalRefKey,
                        item.Code,
                        item.Name,
                        baseUnitOfMeasureId: null,
                        isDeletionMarked: true,
                        item.ImportedAtUtc);
                    if (!deletionResult.IsValid)
                    {
                        failed++;
                        errors.Add(Error(item, ReferenceImportRecordErrorReasons.InvalidSourceRecord,
                            ValidationMessage(deletionResult)));
                        continue;
                    }

                    updated++;
                    continue;
                }

                if (item.IsDeletionMarked)
                {
                    skipped++;
                    errors.Add(Error(item, ReferenceImportRecordErrorReasons.SourceRecordDeletionMarked,
                        "The deletion-marked source SKU has no linked Myrmex record and was skipped."));
                    continue;
                }

                UnitOfMeasure? baseUnit = ResolveBaseUnit(item, baseUnitsByExternalRefKey, errors);
                if (baseUnit is null)
                {
                    failed++;
                    continue;
                }

                string normalizedCode = DomainText.NormalizeCode(item.Code);
                if (linked is not null)
                {
                    if (byCode.TryGetValue(normalizedCode, out StockKeepingUnit? codeOwner) && codeOwner.Id != linked.Id)
                    {
                        skipped++;
                        errors.Add(Error(item, ReferenceImportRecordErrorReasons.CodeAlreadyUsedByAnotherRecord,
                            "Code is already used by another SKU."));
                        continue;
                    }

                    string oldCode = linked.Code;
                    DomainValidationResult updateResult = linked.ApplyImport(
                        item.ExternalRefKey,
                        item.Code,
                        item.Name,
                        baseUnit.Id,
                        isDeletionMarked: false,
                        item.ImportedAtUtc);
                    if (!updateResult.IsValid)
                    {
                        failed++;
                        errors.Add(Error(item, ReferenceImportRecordErrorReasons.InvalidSourceRecord,
                            ValidationMessage(updateResult)));
                        continue;
                    }

                    RefreshCodeIndex(byCode, linked, oldCode);
                    updated++;
                    continue;
                }

                if (byCode.ContainsKey(normalizedCode))
                {
                    skipped++;
                    errors.Add(Error(item, ReferenceImportRecordErrorReasons.CodeAlreadyExistsWithoutExternalRefKey,
                        "Code already exists on an SKU without this external identity."));
                    continue;
                }

                DomainValidationResult createResult = StockKeepingUnit.Create(
                    item.Code,
                    item.Name,
                    description: null,
                    baseUnitOfMeasureId: baseUnit.Id,
                    stockKeepingUnit: out StockKeepingUnit? sku);
                if (!createResult.IsValid || sku is null)
                {
                    failed++;
                    errors.Add(Error(item, ReferenceImportRecordErrorReasons.InvalidSourceRecord,
                        ValidationMessage(createResult)));
                    continue;
                }

                DomainValidationResult importResult = sku.ApplyImport(
                    item.ExternalRefKey,
                    item.Code,
                    item.Name,
                    baseUnit.Id,
                    isDeletionMarked: false,
                    item.ImportedAtUtc);
                if (!importResult.IsValid)
                {
                    failed++;
                    errors.Add(Error(item, ReferenceImportRecordErrorReasons.InvalidSourceRecord,
                        ValidationMessage(importResult)));
                    continue;
                }

                dbContext.StockKeepingUnits.Add(sku);
                byExternalRefKey[item.ExternalRefKey] = sku;
                byCode[sku.Code] = sku;
                created++;
            }

            return new ReferenceImportBatchResult(
                command.Items.Count,
                created,
                updated,
                skipped,
                failed,
                errors);
        }

        private static UnitOfMeasure? ResolveBaseUnit(
            Item item,
            IReadOnlyDictionary<Guid, UnitOfMeasure> baseUnits,
            ICollection<ReferenceImportRecordError> errors)
        {
            if (!item.BaseUnitOfMeasureExternalRefKey.HasValue ||
                item.BaseUnitOfMeasureExternalRefKey.Value == Guid.Empty)
            {
                errors.Add(Error(item, ReferenceImportRecordErrorReasons.BaseUnitOfMeasureExternalRefKeyMissing,
                    "BaseUnitOfMeasureExternalRefKey is required."));
                return null;
            }

            if (!baseUnits.TryGetValue(item.BaseUnitOfMeasureExternalRefKey.Value, out UnitOfMeasure? unit))
            {
                errors.Add(Error(item, ReferenceImportRecordErrorReasons.BaseUnitOfMeasureNotImported,
                    "The referenced base unit of measure has not been imported."));
                return null;
            }

            if (!unit.IsActive)
            {
                errors.Add(Error(item, ReferenceImportRecordErrorReasons.BaseUnitOfMeasureInactive,
                    "The referenced base unit of measure is inactive."));
                return null;
            }

            return unit;
        }

        private static void RefreshCodeIndex(
            Dictionary<string, StockKeepingUnit> byCode,
            StockKeepingUnit sku,
            string oldCode)
        {
            if (!string.Equals(oldCode, sku.Code, StringComparison.Ordinal) &&
                byCode.TryGetValue(oldCode, out StockKeepingUnit? oldOwner) &&
                oldOwner.Id == sku.Id)
            {
                byCode.Remove(oldCode);
            }
            byCode[sku.Code] = sku;
        }

        private static ReferenceImportRecordError Error(Item item, string reason, string message) =>
            new(item.ExternalRefKey == Guid.Empty ? null : item.ExternalRefKey, item.Code?.Trim(), reason, message);

        private static string ValidationMessage(DomainValidationResult result) =>
            result.Errors.Count == 0
                ? "The SKU source record is invalid."
                : string.Join(" ", result.Errors.Select(error => error.Message));

        private static Task RollbackAsync(IDbContextTransaction transaction, bool ownsTransaction) =>
            ownsTransaction
                ? transaction.RollbackAsync(CancellationToken.None)
                : transaction.RollbackToSavepointAsync(SavepointName, CancellationToken.None);
    }
}
