using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Inventory.Features.InventoryCounts;
using Myrmex.Shared.Wms.Inventory;
using Myrmex.Tests.Wms.Inventory.Testing;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Inventory.Features.InventoryCounts;

public sealed class InventoryCountLineHandlerTests
{
    [Fact]
    public async Task CreateAndAddLine_WhenBalanceExists_CapturesSnapshotAndActor()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryCountReferences references =
            await InventoryCountTestData.SeedReferencesAsync(testDbContext.DbContext);

        var createHandler = new CreateInventoryCount.Handler(testDbContext.DbContext);
        ServiceResult<InventoryCountDetails> created = await createHandler.HandleAsync(
            new CreateInventoryCount.Command(
                references.Warehouse.Id,
                "  Monthly count  ",
                InventoryCountTestData.ActorId),
            TestContext.Current.CancellationToken);

        Assert.True(created.IsSuccess);
        Assert.Equal(InventoryCountStatusDetails.Draft, created.Value.Status);
        Assert.Equal("Monthly count", created.Value.Reason);
        Assert.Equal(InventoryCountTestData.ActorId, created.Value.CreatedByActorId);

        var addHandler = new AddInventoryCountLine.Handler(testDbContext.DbContext);
        ServiceResult<InventoryCountDetails> added = await addHandler.HandleAsync(
            new AddInventoryCountLine.Command(
                created.Value.Id,
                references.StockKeepingUnit.Id,
                references.ExistingBalanceLocation.Id,
                created.Value.CountVersion,
                InventoryCountTestData.ActorId),
            TestContext.Current.CancellationToken);

        Assert.True(added.IsSuccess);
        InventoryCountLineDetails line = Assert.Single(added.Value.Lines);
        Assert.Equal(10, line.SystemQuantity);
        Assert.False(string.IsNullOrWhiteSpace(line.ExpectedBalanceVersion));
        Assert.Equal(InventoryCountLineStatusDetails.Pending, line.Status);
        Assert.Equal(InventoryCountStatusDetails.Draft, added.Value.Status);
    }

    [Fact]
    public async Task AddAndRemoveLine_WhenBalanceMissing_UsesZeroSnapshotAndDeletesPending()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryCountReferences references =
            await InventoryCountTestData.SeedReferencesAsync(testDbContext.DbContext);
        var createHandler = new CreateInventoryCount.Handler(testDbContext.DbContext);
        ServiceResult<InventoryCountDetails> created = await createHandler.HandleAsync(
            new CreateInventoryCount.Command(
                references.Warehouse.Id,
                null,
                InventoryCountTestData.ActorId),
            TestContext.Current.CancellationToken);
        var addHandler = new AddInventoryCountLine.Handler(testDbContext.DbContext);
        ServiceResult<InventoryCountDetails> added = await addHandler.HandleAsync(
            new AddInventoryCountLine.Command(
                created.Value.Id,
                references.StockKeepingUnit.Id,
                references.MissingBalanceLocation.Id,
                created.Value.CountVersion,
                InventoryCountTestData.ActorId),
            TestContext.Current.CancellationToken);

        InventoryCountLineDetails line = Assert.Single(added.Value.Lines);
        Assert.Equal(0, line.SystemQuantity);
        Assert.Null(line.ExpectedBalanceVersion);

        var removeHandler = new RemoveInventoryCountLine.Handler(testDbContext.DbContext);
        ServiceResult<InventoryCountDetails> removed = await removeHandler.HandleAsync(
            new RemoveInventoryCountLine.Command(
                added.Value.Id,
                line.Id,
                line.LineVersion,
                InventoryCountTestData.ActorId),
            TestContext.Current.CancellationToken);

        Assert.True(removed.IsSuccess);
        Assert.Empty(removed.Value.Lines);
        Assert.Equal(0, await testDbContext.DbContext.InventoryCountLines.CountAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddLine_WhenDuplicateOrStale_ReturnsConflict()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryCountReferences references =
            await InventoryCountTestData.SeedReferencesAsync(testDbContext.DbContext);
        InventoryCountDetails created = (await new CreateInventoryCount.Handler(testDbContext.DbContext)
            .HandleAsync(
                new CreateInventoryCount.Command(
                    references.Warehouse.Id,
                    null,
                    InventoryCountTestData.ActorId),
                TestContext.Current.CancellationToken)).Value;
        var handler = new AddInventoryCountLine.Handler(testDbContext.DbContext);
        ServiceResult<InventoryCountDetails> first = await handler.HandleAsync(
            new AddInventoryCountLine.Command(
                created.Id,
                references.StockKeepingUnit.Id,
                references.ExistingBalanceLocation.Id,
                created.CountVersion,
                InventoryCountTestData.ActorId),
            TestContext.Current.CancellationToken);

        ServiceResult<InventoryCountDetails> stale = await handler.HandleAsync(
            new AddInventoryCountLine.Command(
                created.Id,
                references.StockKeepingUnit.Id,
                references.MissingBalanceLocation.Id,
                created.CountVersion,
                InventoryCountTestData.ActorId),
            TestContext.Current.CancellationToken);
        ServiceResult<InventoryCountDetails> duplicate = await handler.HandleAsync(
            new AddInventoryCountLine.Command(
                first.Value.Id,
                references.StockKeepingUnit.Id,
                references.ExistingBalanceLocation.Id,
                first.Value.CountVersion,
                InventoryCountTestData.ActorId),
            TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.Conflict, stale.Error.Type);
        Assert.Equal(ServiceErrorType.Conflict, duplicate.Error.Type);
    }

    [Fact]
    public async Task CreateAndAddLine_WhenReferencesAreMissingOrInactive_ReturnExpectedErrors()
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryCountReferences references =
            await InventoryCountTestData.SeedReferencesAsync(testDbContext.DbContext);
        references.SecondWarehouse.Deactivate();
        await testDbContext.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        ServiceResult<InventoryCountDetails> inactiveWarehouse =
            await new CreateInventoryCount.Handler(testDbContext.DbContext).HandleAsync(
                new CreateInventoryCount.Command(
                    references.SecondWarehouse.Id,
                    null,
                    InventoryCountTestData.ActorId),
                TestContext.Current.CancellationToken);
        ServiceResult<InventoryCountDetails> missingWarehouse =
            await new CreateInventoryCount.Handler(testDbContext.DbContext).HandleAsync(
                new CreateInventoryCount.Command(
                    Guid.NewGuid(),
                    null,
                    InventoryCountTestData.ActorId),
                TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.Invalid, inactiveWarehouse.Error.Type);
        Assert.Equal(ServiceErrorType.NotFound, missingWarehouse.Error.Type);

        InventoryCountDetails created = (await new CreateInventoryCount.Handler(testDbContext.DbContext)
            .HandleAsync(
                new CreateInventoryCount.Command(
                    references.Warehouse.Id,
                    null,
                    InventoryCountTestData.ActorId),
                TestContext.Current.CancellationToken)).Value;
        var addHandler = new AddInventoryCountLine.Handler(testDbContext.DbContext);

        ServiceResult<InventoryCountDetails> missingSku = await addHandler.HandleAsync(
            new AddInventoryCountLine.Command(
                created.Id,
                Guid.NewGuid(),
                references.ExistingBalanceLocation.Id,
                created.CountVersion,
                InventoryCountTestData.ActorId),
            TestContext.Current.CancellationToken);
        ServiceResult<InventoryCountDetails> missingLocation = await addHandler.HandleAsync(
            new AddInventoryCountLine.Command(
                created.Id,
                references.StockKeepingUnit.Id,
                Guid.NewGuid(),
                created.CountVersion,
                InventoryCountTestData.ActorId),
            TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.NotFound, missingSku.Error.Type);
        Assert.Equal(ServiceErrorType.NotFound, missingLocation.Error.Type);
    }

    [Theory]
    [InlineData(InvalidLocationKind.InternalTransit)]
    [InlineData(InvalidLocationKind.ExternalTransit)]
    [InlineData(InvalidLocationKind.CrossWarehouse)]
    public async Task AddLine_WhenLocationIneligible_ReturnsValidation(InvalidLocationKind kind)
    {
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        SeededInventoryCountReferences references =
            await InventoryCountTestData.SeedReferencesAsync(testDbContext.DbContext);
        InventoryCountDetails created = (await new CreateInventoryCount.Handler(testDbContext.DbContext)
            .HandleAsync(
                new CreateInventoryCount.Command(
                    references.Warehouse.Id,
                    null,
                    InventoryCountTestData.ActorId),
                TestContext.Current.CancellationToken)).Value;
        Guid locationId = kind switch
        {
            InvalidLocationKind.InternalTransit => references.InternalTransitLocation.Id,
            InvalidLocationKind.ExternalTransit => references.ExternalTransitLocation.Id,
            InvalidLocationKind.CrossWarehouse => references.CrossWarehouseLocation.Id,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        ServiceResult<InventoryCountDetails> result =
            await new AddInventoryCountLine.Handler(testDbContext.DbContext).HandleAsync(
                new AddInventoryCountLine.Command(
                    created.Id,
                    references.StockKeepingUnit.Id,
                    locationId,
                    created.CountVersion,
                    InventoryCountTestData.ActorId),
                TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.Invalid, result.Error.Type);
    }

    public enum InvalidLocationKind
    {
        InternalTransit,
        ExternalTransit,
        CrossWarehouse
    }
}
