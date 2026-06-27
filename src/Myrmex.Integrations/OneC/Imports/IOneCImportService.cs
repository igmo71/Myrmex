using Myrmex.Shared.Integrations.OneC;

namespace Myrmex.Integrations.OneC.Imports;

internal interface IOneCImportService
{
    Task<OneCImportResponse> ImportWarehousesAsync(CancellationToken cancellationToken);

    Task<OneCImportResponse> ImportUnitsOfMeasureAsync(CancellationToken cancellationToken);
}
