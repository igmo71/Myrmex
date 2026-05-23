using Myrmex.Core.Results;

namespace Myrmex.Modules.Wms;

internal static class WmsErrors
{
    internal static class Warehouse
    {
        public static ServiceError NotFound =>
            ServiceErrors.NotFound("Warehouse.NotFound", "Warehouse was not found.");
        public static ServiceError CodeAlreadyExists =>
            ServiceErrors.Conflict("Warehouse.CodeAlreadyExists", "Warehouse with the same code already exists.", "code");
        public static ServiceError CreateFailed =>
            ServiceErrors.Failure("Warehouse.CreateFailed", "Warehouse creation failed unexpectedly.");
    }

    internal static class Zone
    {
        public static ServiceError NotFound =>
            ServiceErrors.NotFound("Zone.NotFound", "Zone was not found.");
        public static ServiceError CodeAlreadyExists =>
            ServiceErrors.Conflict("Zone.CodeAlreadyExists", "Zone with the same code already exists in this warehouse.", "code");
        public static ServiceError CreateFailed =>
            ServiceErrors.Failure("Zone.CreateFailed", "Zone creation failed unexpectedly.");
    }
}