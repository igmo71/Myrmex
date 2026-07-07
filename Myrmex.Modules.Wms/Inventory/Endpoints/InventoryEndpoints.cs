using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Myrmex.AspNetCore.Security;

namespace Myrmex.Modules.Wms.Inventory.Endpoints;

internal static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/wms/inventory")
            .WithTags("Wms Inventory")
            .RequireAuthorization(MyrmexAuthorizationPolicies.WmsOperator);

        group.MapInventoryBalanceEndpoints();
        group.MapInventoryAdjustmentEndpoints();
        group.MapInventoryLedgerEndpoints();
        group.MapInventoryTransferEndpoints();
        group.MapInventoryCountEndpoints();

        return endpoints;
    }
}
