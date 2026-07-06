namespace Myrmex.Modules.Wms.DemoData.Features;

internal static class DemoDataDefinitions
{
    public static readonly WarehouseDefinition Warehouse = new("DEMO", "Демо склад", "Склад для демонстрации возможностей Myrmex");

    public static readonly IReadOnlyList<UnitDefinition> Units =
    [
        new("PCS", "Штука", "шт"),
        new("PACK", "Упаковка", "упак"),
        new("BOX", "Коробка", "кор"),
        new("KG", "Килограмм", "кг")
    ];

    public static readonly IReadOnlyList<SkuDefinition> Skus =
    [
        new("SKU-SCR-GVL-3.9X19", "Саморез ГВЛ 3,9×19", "Саморез для гипсоволокнистых листов 3,9×19", "PACK"),
        new("SKU-SCR-GVL-3.9X25", "Саморез ГВЛ 3,9×25", "Саморез для гипсоволокнистых листов 3,9×25", "PACK"),
        new("SKU-SCR-GVL-3.9X30", "Саморез ГВЛ 3,9×30", "Саморез для гипсоволокнистых листов 3,9×30", "PACK"),
        new("SKU-SCR-UNI-4.0X40", "Шуруп универсальный 4,0×40", "Универсальный шуруп 4,0×40", "BOX"),
        new("SKU-DWL-6X40", "Дюбель 6×40", "Нейлоновый дюбель 6×40", "PACK"),
        new("SKU-ANCH-WDG-10X100", "Анкер клиновой 10×100", "Клиновой анкер 10×100", "PCS"),
        new("SKU-BLT-M8X30", "Болт М8×30", "Болт с шестигранной головкой М8×30", "PCS"),
        new("SKU-NUT-M8", "Гайка М8", "Шестигранная гайка М8", "PCS"),
        new("SKU-WSH-M8", "Шайба М8", "Плоская шайба М8", "PCS"),
        new("SKU-THR-M10X1000", "Шпилька М10×1000", "Резьбовая шпилька М10×1000", "PCS")
    ];

    public static readonly IReadOnlyList<ZoneDefinition> Zones =
    [
        new("RCV", "Приёмка", "Зона приёмки"),
        new("BULK", "Паллетное хранение", "Зона паллетного хранения"),
        new("PICK", "Отбор", "Зона отбора"),
        new("PACK", "Упаковка", "Зона упаковки"),
        new("SHIP", "Отгрузка", "Зона отгрузки"),
        new("QRT", "Карантин", "Карантинная зона"),
        new("CART", "Тележки и транзит", "Зона тележек и внутреннего транзита")
    ];

    public static readonly IReadOnlyList<LocationDefinition> Locations =
    [
        new("RCV-DOCK-01", "Док приёмки 01", "RCV", "DOCK", "AVAILABLE", false),
        new("RCV-DOCK-02", "Док приёмки 02", "RCV", "DOCK", "AVAILABLE", false),
        new("BULK-A-01-01", "Паллетная ячейка A-01-01", "BULK", "PALLET_RACK", "AVAILABLE", false),
        new("BULK-A-01-02", "Паллетная ячейка A-01-02", "BULK", "PALLET_RACK", "AVAILABLE", false),
        new("BULK-B-01-01", "Паллетная ячейка B-01-01", "BULK", "PALLET_RACK", "AVAILABLE", false),
        new("PICK-A-01-01", "Ячейка отбора A-01-01", "PICK", "SHELF", "AVAILABLE", true),
        new("PICK-A-01-02", "Ячейка отбора A-01-02", "PICK", "SHELF", "AVAILABLE", true),
        new("PICK-B-01-01", "Ячейка отбора B-01-01", "PICK", "SHELF", "AVAILABLE", true),
        new("PACK-01", "Упаковочный стол 01", "PACK", "STAGING", "AVAILABLE", false),
        new("PACK-02", "Упаковочный стол 02", "PACK", "STAGING", "AVAILABLE", false),
        new("SHIP-STAGE-01", "Место отгрузки 01", "SHIP", "STAGING", "AVAILABLE", false),
        new("SHIP-STAGE-02", "Место отгрузки 02", "SHIP", "STAGING", "AVAILABLE", false),
        new("QRT-01", "Карантин 01", "QRT", "FLOOR", "BLOCKED", false),
        new("CART-01", "Тележка комплектовщика 01", "CART", "INTERNAL_TRANSIT", "AVAILABLE", false),
        new("CART-02", "Тележка комплектовщика 02", "CART", "INTERNAL_TRANSIT", "AVAILABLE", false)
    ];

    public static readonly IReadOnlyList<OpeningDefinition> Openings =
    [
        new("SKU-SCR-GVL-3.9X19", "BULK-A-01-01", 500),
        new("SKU-SCR-GVL-3.9X19", "PICK-A-01-01", 100),
        new("SKU-SCR-GVL-3.9X25", "BULK-A-01-02", 400),
        new("SKU-SCR-GVL-3.9X25", "PICK-A-01-02", 80),
        new("SKU-SCR-GVL-3.9X30", "BULK-B-01-01", 300),
        new("SKU-SCR-GVL-3.9X30", "PICK-B-01-01", 40),
        new("SKU-SCR-UNI-4.0X40", "BULK-B-01-01", 250),
        new("SKU-DWL-6X40", "PICK-A-01-01", 120),
        new("SKU-WSH-M8", "QRT-01", 50),
        new("SKU-THR-M10X1000", "PACK-01", 12)
    ];

    public static readonly IReadOnlyList<TransferDefinition> Transfers =
    [
        new("DEMO-TRF-DIRECT-001", "SKU-SCR-GVL-3.9X19", "BULK-A-01-01", "PICK-A-01-01", null, 20, TransferTarget.CompletedDirect),
        new("DEMO-TRF-CART-001", "SKU-SCR-GVL-3.9X30", "BULK-B-01-01", "PICK-B-01-01", "CART-01", 15, TransferTarget.CompletedTransit),
        new("DEMO-TRF-CART-002", "SKU-SCR-UNI-4.0X40", "BULK-B-01-01", "PICK-B-01-01", "CART-01", 10, TransferTarget.PickedToTransit),
        new("DEMO-TRF-DIRECT-002", "SKU-SCR-GVL-3.9X25", "BULK-A-01-02", "PICK-A-01-02", null, 25, TransferTarget.Created)
    ];

    public const string OpenCountReason = "DEMO-CNT-OPEN-001 — Инвентаризация зоны отбора";
    public const string ClosedCountReason = "DEMO-CNT-CLOSED-001 — Завершённая инвентаризация паллетной зоны";

    public static string OpeningReason(OpeningDefinition definition) =>
        $"DEMO-OPEN-{definition.SkuCode}-{definition.LocationCode}";

    internal sealed record WarehouseDefinition(string Code, string Name, string Description);
    internal sealed record UnitDefinition(string Code, string Name, string Symbol);
    internal sealed record SkuDefinition(string Code, string Name, string Description, string UnitCode);
    internal sealed record ZoneDefinition(string Code, string Name, string Description);
    internal sealed record LocationDefinition(string Code, string Name, string ZoneCode, string TypeCode, string StatusCode, bool IsPickable);
    internal sealed record OpeningDefinition(string SkuCode, string LocationCode, decimal Quantity);
    internal sealed record TransferDefinition(string Code, string SkuCode, string SourceCode, string DestinationCode, string? TransitCode, decimal Quantity, TransferTarget Target);

    internal enum TransferTarget
    {
        Created,
        CompletedDirect,
        PickedToTransit,
        CompletedTransit
    }
}
