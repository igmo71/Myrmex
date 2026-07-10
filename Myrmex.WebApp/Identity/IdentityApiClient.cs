using Myrmex.Shared.Identity;
using Myrmex.WebApp.Wms.Api;

namespace Myrmex.WebApp.Identity;

public sealed class IdentityApiClient(HttpClient httpClient)
{
    public async Task<ApiResult<IdentityUserDetails>> CreateUserAsync(
        CreateIdentityUserRequest request,
        CancellationToken cancellationToken = default)
    {
        return await httpClient.PostAsApiResultAsync<IdentityUserDetails>(
            "/api/identity/users",
            request,
            cancellationToken);
    }
}
