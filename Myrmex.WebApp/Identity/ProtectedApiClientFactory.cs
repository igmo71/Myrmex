namespace Myrmex.WebApp.Identity;

public sealed class ProtectedApiClientFactory(
    IServiceProvider scopedServices,
    IHttpMessageHandlerFactory handlerFactory)
    : IDisposable
{
    private readonly List<HttpClient> _clients = [];

    public HttpClient CreateClient(
        string name,
        Uri baseAddress,
        TimeSpan? timeout = null)
    {
        HttpMessageHandler innerHandler = handlerFactory.CreateHandler(name);
        IdentityApiAuthenticationHandler authenticationHandler =
            ActivatorUtilities.CreateInstance<IdentityApiAuthenticationHandler>(
                scopedServices);
        ProtectedApiAuthorizationHandler authorizationHandler =
            ActivatorUtilities.CreateInstance<ProtectedApiAuthorizationHandler>(
                scopedServices);
        authorizationHandler.InnerHandler = new NonDisposingHandler(innerHandler);
        authenticationHandler.InnerHandler = authorizationHandler;

        HttpClient client = new(authenticationHandler)
        {
            BaseAddress = baseAddress
        };
        if (timeout is not null)
        {
            client.Timeout = timeout.Value;
        }

        _clients.Add(client);
        return client;
    }

    public void Dispose()
    {
        foreach (HttpClient client in _clients)
        {
            client.Dispose();
        }

        _clients.Clear();
    }

    private sealed class NonDisposingHandler(HttpMessageHandler innerHandler)
        : DelegatingHandler(innerHandler)
    {
        protected override void Dispose(bool disposing)
        {
        }
    }
}
