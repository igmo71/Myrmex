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
    private const string Projection = "Ref_Key,DataVersion,DeletionMark,Number,Date,Posted,Склад_Key,Статус,Товары/Ref_Key,Товары/LineNumber,Товары/Номенклатура_Key,Товары/Упаковка_Key,Товары/КоличествоУпаковок,Товары/Количество";
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
                new("$filter", $"Date ge datetime'{startDate:yyyy-MM-dd}T00:00:00' and Date lt datetime'{endExclusive:yyyy-MM-dd}T00:00:00' and Posted eq true and DeletionMark eq false and (Статус eq 'КПоступлению' or Статус eq 'ВРаботе' or Статус eq 'ТребуетсяОбработка')"),
                new("$orderby", "Date,Ref_Key")
            ],
            cancellationToken);
        return records.Where(IsEligible).ToArray();
    }

    private static bool IsEligible(ReceivingOrderSourceRecord record) =>
        record.Posted && !record.DeletionMark && record.Статус is "КПоступлению" or "ВРаботе" or "ТребуетсяОбработка";
}
