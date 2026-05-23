namespace Myrmex.Modules.Wms.Infrastructure.Persistence;

internal static class WmsSeedIds
{
    public static readonly Guid StorageLocationTypePalletRack = Guid.Parse("018f0000-0000-7000-8000-000000000001");
    public static readonly Guid StorageLocationTypeShelf = Guid.Parse("018f0000-0000-7000-8000-000000000002");
    public static readonly Guid StorageLocationTypeFloor = Guid.Parse("018f0000-0000-7000-8000-000000000003");
    public static readonly Guid StorageLocationTypeStaging = Guid.Parse("018f0000-0000-7000-8000-000000000004");
    public static readonly Guid StorageLocationTypeDock = Guid.Parse("018f0000-0000-7000-8000-000000000005");

    public static readonly Guid StorageLocationStatusAvailable = Guid.Parse("018f0000-0000-7000-8000-000000000101");
    public static readonly Guid StorageLocationStatusBlocked = Guid.Parse("018f0000-0000-7000-8000-000000000102");
    public static readonly Guid StorageLocationStatusMaintenance = Guid.Parse("018f0000-0000-7000-8000-000000000103");
    public static readonly Guid StorageLocationStatusInventoryCheck = Guid.Parse("018f0000-0000-7000-8000-000000000104");
}