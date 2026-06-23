using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Myrmex.Modules.Wms.Inventory.Endpoints;

internal static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/wms/inventory")
            .WithTags("Wms Inventory");

        group.MapInventoryBalanceEndpoints();
        group.MapInventoryAdjustmentEndpoints();
        group.MapInventoryLedgerEndpoints();
        group.MapInventoryTransferEndpoints();

        return endpoints;
    }
}
