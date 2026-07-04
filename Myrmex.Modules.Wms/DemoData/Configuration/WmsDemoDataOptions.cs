namespace Myrmex.Modules.Wms.DemoData.Configuration;

internal sealed class WmsDemoDataOptions
{
    public const string SectionName = "Myrmex:Wms:DemoData";

    public bool Enabled { get; set; }
    public bool AllowClear { get; set; }
    public string? ClearConfirmation { get; set; }
}
