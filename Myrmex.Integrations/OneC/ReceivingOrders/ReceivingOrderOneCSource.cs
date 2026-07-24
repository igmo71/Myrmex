using Microsoft.Extensions.Options;
using Myrmex.Integrations.OneC.Common.Transport;
using Myrmex.Integrations.OneC.Configuration;

namespace Myrmex.Integrations.OneC.ReceivingOrders;

internal interface IReceivingOrderOneCSource
{
    Task<IReadOnlyList<ReceivingOrderSourceRecord>> ReadPeriodAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken);
}

internal sealed class ReceivingOrderOneCSource(
    IOneCODataTransport transport,
    IOptions<OneCOptions> options) : IReceivingOrderOneCSource
{
    private const string Projection = "Ref_Key,DataVersion,DeletionMark,Number,Date,Posted,Склад_Key,Статус";
    private const string LineProjection = "Ref_Key,LineNumber,Номенклатура_Key,Упаковка_Key,КоличествоУпаковок,Количество";

    public async Task<IReadOnlyList<ReceivingOrderSourceRecord>> ReadPeriodAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        DateOnly endExclusive = endDate.AddDays(1);
        IReadOnlyList<ReceivingOrderSourceRecord> records = await transport.ReadCollectionAsync<ReceivingOrderSourceRecord>(
            options.Value.ReceivingOrdersEntitySet!,
            [
                new("$format", "json"),
                new("$select", Projection),
                new("$expand", $"Товары($select={LineProjection})"),
                new("$filter", $"Date ge datetime'{startDate:yyyy-MM-dd}T00:00:00' and Date lt datetime'{endExclusive:yyyy-MM-dd}T00:00:00'"),
                new("$orderby", "Date,Ref_Key")
            ],
            cancellationToken);
        return records.Where(IsEligible).ToArray();
    }

    private static bool IsEligible(ReceivingOrderSourceRecord record) =>
        record.Posted && !record.DeletionMark && record.Статус is "КПоступлению" or "ВРаботе" or "ТребуетсяОбработка";
}
