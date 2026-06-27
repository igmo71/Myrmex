using Myrmex.Shared.Integrations.OneC;
using Myrmex.WebApp.Wms.Api;

namespace Myrmex.WebApp.Integrations.OneC;

public sealed class OneCIntegrationApiClient(HttpClient httpClient)
{
    public Task<ApiResult<OneCConnectionTestResponse>> TestConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        return httpClient.PostAsApiResultAsync<OneCConnectionTestResponse>(
            "/api/integrations/1c/connection/test",
            cancellationToken);
    }
}
