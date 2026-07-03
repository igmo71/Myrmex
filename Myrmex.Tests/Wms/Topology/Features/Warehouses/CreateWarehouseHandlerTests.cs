using Microsoft.EntityFrameworkCore;
using Myrmex.Core.Results;
using Myrmex.Modules.Wms.Topology.Features.Warehouses;
using Myrmex.Shared.Wms.Topology;
using Myrmex.Tests.Wms.Topology.Testing;

namespace Myrmex.Tests.Wms.Topology.Features.Warehouses;

public sealed class CreateWarehouseHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_CreatesWarehouseAndReturnsDetails()
    {
        // Arrange
        await using TestWmsDbContext testDbContext = await TestWmsDbContext.CreateAsync();
        RecordingDomainEventDispatcher domainEventDispatcher = new();

        CreateWarehouse.Handler handler = new(
            testDbContext.DbContext,
            domainEventDispatcher);

        CreateWarehouse.Command command = new(
            Code: " main ",
            Name: " Main Warehouse ",
            Description: " Primary warehouse ");

        // Act
        ServiceResult<WarehouseDetails> result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);

        WarehouseDetails details = result.Value;

        Assert.NotEqual(Guid.Empty, details.Id);
        Assert.Equal("MAIN", details.Code);
        Assert.Equal("Main Warehouse", details.Name);
        Assert.Equal("Primary warehouse", details.Description);
        Assert.True(details.IsActive);

        var warehouse = await testDbContext.DbContext.Warehouses.SingleAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(details.Id, warehouse.Id);
        Assert.Equal("MAIN", warehouse.Code);
        Assert.Equal("Main Warehouse", warehouse.Name);
        Assert.Equal("Primary warehouse", warehouse.Description);
        Assert.True(warehouse.IsActive);

        Assert.NotEmpty(domainEventDispatcher.DispatchedEvents);
    }
}
