namespace Myrmex.Integrations.OneC.Transport;

internal interface IOneCODataClient
{
    void ValidateConfiguration();

    Task TestConnectionAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Catalog_Склады>> ReadWarehousesAsync(
        CancellationToken cancellationToken);

    Task<Catalog_Склады?> ReadWarehouseAsync(
        Guid externalRefKey,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Catalog_УпаковкиЕдиницыИзмерения>> ReadUnitsOfMeasureAsync(
        CancellationToken cancellationToken);

    Task<Catalog_УпаковкиЕдиницыИзмерения?> ReadUnitOfMeasureAsync(
        Guid externalRefKey,
        CancellationToken cancellationToken);

    IAsyncEnumerable<IReadOnlyList<Catalog_Номенклатура>> ReadNomenclaturePagesAsync(
        CancellationToken cancellationToken);

    Task<Catalog_Номенклатура?> ReadStockKeepingUnitAsync(
        Guid externalRefKey,
        CancellationToken cancellationToken);
}
