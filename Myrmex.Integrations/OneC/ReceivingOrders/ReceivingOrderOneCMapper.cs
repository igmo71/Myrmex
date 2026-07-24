using Myrmex.Modules.Wms.Receiving.Features.ReceivingOrders;

namespace Myrmex.Integrations.OneC.ReceivingOrders;

internal static class ReceivingOrderOneCMapper
{
    public static bool TryMap(
        ReceivingOrderSourceRecord source,
        out ImportExternalReceivingOrder.Document? document,
        out string reason,
        out string message)
    {
        document = null;
        reason = string.Empty;
        message = string.Empty;
        if (source.Ref_Key == Guid.Empty || source.Склад_Key == Guid.Empty ||
            source.DataVersion.Length == 0 || string.IsNullOrWhiteSpace(source.Number))
        {
            reason = "InvalidSourceRecord";
            message = "The source receiving document is missing a required identity or header value.";
            return false;
        }

        List<ImportExternalReceivingOrder.Line> lines = [];
        HashSet<int> lineNumbers = [];
        foreach (ReceivingOrderSourceLineRecord line in source.Товары)
        {
            if (line.LineNumber <= 0 || !lineNumbers.Add(line.LineNumber) || line.Ref_Key != source.Ref_Key ||
                line.Номенклатура_Key == Guid.Empty || line.Количество <= 0)
            {
                reason = "InvalidSourceLine";
                message = $"The source line {line.LineNumber} is invalid.";
                return false;
            }
            if (line.Упаковка_Key is Guid packageKey && packageKey != Guid.Empty)
            {
                reason = "UnsupportedPackage";
                message = $"The source line {line.LineNumber} uses a package that cannot be converted.";
                return false;
            }
            lines.Add(new($"{source.Ref_Key:D}:{line.LineNumber}", line.Номенклатура_Key, line.Количество));
        }

        if (lines.Count == 0)
        {
            reason = "EmptyPlan";
            message = "The source receiving document has no planned goods lines.";
            return false;
        }

        document = new(source.Ref_Key, source.DataVersion, source.Number.Trim(), source.Date, source.Склад_Key, lines);
        return true;
    }
}
