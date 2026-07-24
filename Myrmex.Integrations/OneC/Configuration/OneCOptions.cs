namespace Myrmex.Integrations.OneC.Configuration;

internal sealed class OneCOptions
{
    public const string SectionName = "Myrmex:Integrations:OneC";
    public const int DefaultBatchSize = 1000;
    public const int MaximumBatchSize = 5000;
    public const int DefaultTimeoutSeconds = 30;

    public bool Enabled { get; set; }
    public string? BaseUrl { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? WarehousesEntitySet { get; set; }
    public string? UnitsOfMeasureEntitySet { get; set; }
    public string? NomenclatureEntitySet { get; set; }
    public string? ReceivingOrdersEntitySet { get; set; }
    public bool WarehouseCodeAvailable { get; set; } = true;
    public bool UseFolderFilter { get; set; } = true;
    public int BatchSize { get; set; } = DefaultBatchSize;
    public int TimeoutSeconds { get; set; } = DefaultTimeoutSeconds;
}
