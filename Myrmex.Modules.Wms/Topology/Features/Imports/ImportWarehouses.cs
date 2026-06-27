using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Myrmex.AppDispatching.EventDispatching;
using Myrmex.Core.Application;
using Myrmex.Core.Domain;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Catalog.Features.Imports;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Topology.Domain.Warehouses;

namespace Myrmex.Modules.Wms.Topology.Features.Imports;

public static class ImportWarehouses
{
    private const string SavepointName = "ImportWarehousesBatch";

    public sealed record Command(IReadOnlyList<Item> Items)
        : ICommand<ServiceResult<ReferenceImportBatchResult>>;

    public sealed record Item(
        Guid ExternalRefKey,
        string? Code,
        string? Name,
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
                .Select(item => DomainText.NormalizeCode(item.Code))
                .Where(code => code.Length > 0)
                .ToHashSet(StringComparer.Ordinal);

            List<Warehouse> existing = await dbContext.Warehouses
                .Where(warehouse =>
                    (warehouse.ExternalRefKey.HasValue && externalRefKeys.Contains(warehouse.ExternalRefKey.Value)) ||
                    codes.Contains(warehouse.Code))
                .ToListAsync(cancellationToken);

            Dictionary<Guid, Warehouse> byExternalRefKey = existing
                .Where(warehouse => warehouse.ExternalRefKey.HasValue)
                .ToDictionary(warehouse => warehouse.ExternalRefKey!.Value);
            Dictionary<string, Warehouse> byCode = existing
                .ToDictionary(warehouse => warehouse.Code, StringComparer.Ordinal);

            int created = 0;
            int updated = 0;
            int skipped = 0;
            int failed = 0;
            List<ReferenceImportRecordError> errors = [];

            foreach (Item item in command.Items)
            {
                string normalizedCode = DomainText.NormalizeCode(item.Code);

                if (item.ExternalRefKey == Guid.Empty)
                {
                    failed++;
                    errors.Add(Error(item, ReferenceImportRecordErrorReasons.InvalidSourceRecord,
                        "ExternalRefKey is required."));
                    continue;
                }

                if (byExternalRefKey.TryGetValue(item.ExternalRefKey, out Warehouse? linked))
                {
                    if (byCode.TryGetValue(normalizedCode, out Warehouse? codeOwner) && codeOwner.Id != linked.Id)
                    {
                        skipped++;
                        errors.Add(Error(item, ReferenceImportRecordErrorReasons.CodeAlreadyUsedByAnotherRecord,
                            "Code is already used by another warehouse."));
                        continue;
                    }

                    string oldCode = linked.Code;
                    DomainValidationResult updateResult = linked.ApplyImport(
                        item.ExternalRefKey,
                        item.Code,
                        item.Name,
                        item.IsDeletionMarked,
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

                if (item.IsDeletionMarked)
                {
                    skipped++;
                    continue;
                }

                if (byCode.ContainsKey(normalizedCode))
                {
                    skipped++;
                    errors.Add(Error(item, ReferenceImportRecordErrorReasons.CodeAlreadyExistsWithoutExternalRefKey,
                        "Code already exists on a warehouse without this external identity."));
                    continue;
                }

                DomainValidationResult createResult = Warehouse.Create(
                    item.Code,
                    item.Name,
                    description: null,
                    out Warehouse? warehouse);
                if (!createResult.IsValid || warehouse is null)
                {
                    failed++;
                    errors.Add(Error(item, ReferenceImportRecordErrorReasons.InvalidSourceRecord,
                        ValidationMessage(createResult)));
                    continue;
                }

                DomainValidationResult importResult = warehouse.ApplyImport(
                    item.ExternalRefKey,
                    item.Code,
                    item.Name,
                    isDeletionMarked: false,
                    item.ImportedAtUtc);
                if (!importResult.IsValid)
                {
                    failed++;
                    errors.Add(Error(item, ReferenceImportRecordErrorReasons.InvalidSourceRecord,
                        ValidationMessage(importResult)));
                    continue;
                }

                dbContext.Warehouses.Add(warehouse);
                byExternalRefKey[item.ExternalRefKey] = warehouse;
                byCode[warehouse.Code] = warehouse;
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

        private static void RefreshCodeIndex(
            Dictionary<string, Warehouse> byCode,
            Warehouse warehouse,
            string oldCode)
        {
            if (!string.Equals(oldCode, warehouse.Code, StringComparison.Ordinal) &&
                byCode.TryGetValue(oldCode, out Warehouse? oldOwner) &&
                oldOwner.Id == warehouse.Id)
            {
                byCode.Remove(oldCode);
            }
            byCode[warehouse.Code] = warehouse;
        }

        private static ReferenceImportRecordError Error(Item item, string reason, string message) =>
            new(item.ExternalRefKey == Guid.Empty ? null : item.ExternalRefKey, item.Code?.Trim(), reason, message);

        private static string ValidationMessage(DomainValidationResult result) =>
            result.Errors.Count == 0
                ? "The warehouse source record is invalid."
                : string.Join(" ", result.Errors.Select(error => error.Message));

        private static Task RollbackAsync(
            IDbContextTransaction transaction,
            bool ownsTransaction) =>
            ownsTransaction
                ? transaction.RollbackAsync(CancellationToken.None)
                : transaction.RollbackToSavepointAsync(SavepointName, CancellationToken.None);
    }
}
