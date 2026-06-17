using Myrmex.Shared.Common;
using System.Web;

namespace Myrmex.WebApp.Wms.Api;

internal static class WmsApiUrls
{
    public static string BuildListUrl(string path, ListRequest request)
    {
        List<string> query = [];

        query.Add($"skip={request.Skip}");
        query.Add($"take={request.Take}");

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            query.Add($"searchText={HttpUtility.UrlEncode(request.SearchText)}");
        }

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            query.Add($"sortBy={HttpUtility.UrlEncode(request.SortBy)}");
        }

        query.Add($"sortDescending={request.SortDescending.ToString().ToLowerInvariant()}");
        query.Add($"includeInactive={request.IncludeInactive.ToString().ToLowerInvariant()}");

        return $"{path}?{string.Join("&", query)}";
    }
}
