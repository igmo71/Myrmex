namespace Myrmex.WebApp.Identity;

public sealed class IdentityApiClient(HttpClient httpClient)
{
    internal HttpClient HttpClient { get; } = httpClient;
}
