namespace Myrmex.Modules.Wms.Inventory.Domain.InventoryCounts;

internal enum InventoryCountLineStatus
{
    Pending = 0,
    Counted = 1,
    Applied = 2,
    Conflict = 3,
    Superseded = 4
}
