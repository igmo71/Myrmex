using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Myrmex.Core.Application;
using Myrmex.Core.Domain.Validation;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Infrastructure.Persistence;
using Myrmex.Modules.Wms.Inventory.Domain.InventoryCounts;
using Myrmex.Shared.Wms.Inventory;

namespace Myrmex.Modules.Wms.Inventory.Features.InventoryCounts;

internal static class CompleteInventoryCount
{
    internal sealed record Command(
        Guid? InventoryCountId,
        string? ExpectedCountVersion,
        string? ActorId) : ICommand<ServiceResult<InventoryCountDetails>>;

    internal sealed class Handler(
        WmsDbContext dbContext,
        ILogger<Handler> logger)
        : ICommandHandler<Command, ServiceResult<InventoryCountDetails>>
    {
        public async Task<ServiceResult<InventoryCountDetails>> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            List<DomainValidationFailure> errors = Validate(
                command,
                out byte[]? expectedVersion);
            if (errors.Count > 0)
            {
                return ServiceResult<InventoryCountDetails>.Invalid(errors);
            }

            InventoryCount? count = await dbContext.InventoryCounts
                .Include(x => x.Lines)
                .SingleOrDefaultAsync(
                    x => x.Id == command.InventoryCountId!.Value,
                    cancellationToken);
            if (count is null)
            {
                return ServiceResult<InventoryCountDetails>.Fail(
                    ServiceError.NotFound<InventoryCount>(
                        "InventoryCount not found",
                        nameof(Command.InventoryCountId)));
            }

            if (!count.RowVersion.SequenceEqual(expectedVersion!))
            {
                return ServiceResult<InventoryCountDetails>.Fail(
                    InventoryCountErrors.CountConcurrency(
                        nameof(Command.ExpectedCountVersion)));
            }

            DomainValidationResult completeResult = count.Complete(
                command.ActorId,
                DateTimeOffset.UtcNow);
            if (!completeResult.IsValid)
            {
                return ServiceResult<InventoryCountDetails>.Fail(
                    InventoryCountErrors.InvalidState(
                        "Inventory count completion requires at least one current line and every current line must be Applied.",
                        nameof(InventoryCount.Status)));
            }

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ServiceResult<InventoryCountDetails>.Fail(
                    InventoryCountErrors.CountConcurrency(
                        nameof(Command.ExpectedCountVersion)));
            }

            logger.LogInformation(
                "Inventory count {InventoryCountId} completed by actor {ActorId}.",
                count.Id,
                command.ActorId);
            return await CreateInventoryCount.LoadDetailsAsync(
                dbContext,
                count.Id,
                cancellationToken);
        }

        private static List<DomainValidationFailure> Validate(
            Command command,
            out byte[]? expectedVersion)
        {
            List<DomainValidationFailure> errors = [];
            if (!command.InventoryCountId.HasValue || command.InventoryCountId == Guid.Empty)
            {
                errors.Add(
                    DomainValidationFailure.Required<InventoryCount>(
                        nameof(Command.InventoryCountId)));
            }

            DomainValidationFailure? versionError = InventoryCountVersion.Parse(
                command.ExpectedCountVersion,
                nameof(Command.ExpectedCountVersion),
                out expectedVersion);
            if (versionError is not null)
            {
                errors.Add(versionError);
            }

            if (string.IsNullOrWhiteSpace(command.ActorId))
            {
                errors.Add(
                    DomainValidationFailure.Required<InventoryCount>(
                        nameof(Command.ActorId)));
            }
            else if (command.ActorId.Trim().Length > InventoryCount.ActorIdMaxLength)
            {
                errors.Add(
                    DomainValidationFailure.TooLong<InventoryCount>(
                        nameof(Command.ActorId),
                        InventoryCount.ActorIdMaxLength));
            }

            return errors;
        }
    }
}
