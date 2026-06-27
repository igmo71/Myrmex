namespace Myrmex.Integrations.OneC.Transport;

internal interface IOneCODataClient
{
    void ValidateConfiguration();

    Task TestConnectionAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Catalog_Склады>> ReadWarehousesAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Catalog_УпаковкиЕдиницыИзмерения>> ReadUnitsOfMeasureAsync(
        CancellationToken cancellationToken);
}
