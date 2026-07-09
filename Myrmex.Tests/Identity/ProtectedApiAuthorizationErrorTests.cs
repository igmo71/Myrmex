using Microsoft.AspNetCore.Components;
using Myrmex.WebApp.Identity;

namespace Myrmex.Tests.Identity;

public sealed class ProtectedApiAuthorizationErrorTests
{
    [Fact]
    public async Task SendAsync_WhenApiReturns401_NavigatesToLoginAndReturnsFailure()
    {
        RecordingHandler inner = new(HttpStatusCode.Unauthorized);
        TestNavigationManager navigation = new(
            "https://web.test/",
            "https://web.test/wms/topology?warehouse=main");
        using HttpClient client = CreateClient(inner, navigation);

        using HttpResponseMessage response = await client.GetAsync(
            "/protected",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, inner.CallCount);
        Assert.Equal(
            "https://web.test/account/login?returnUrl=%2Fwms%2Ftopology%3Fwarehouse%3Dmain",
            navigation.LastUri);
    }

    [Fact]
    public async Task SendAsync_WhenApiReturns403_NavigatesToAccessDeniedAndReturnsFailure()
    {
        RecordingHandler inner = new(HttpStatusCode.Forbidden);
        TestNavigationManager navigation = new(
            "https://web.test/",
            "https://web.test/wms/topology");
        using HttpClient client = CreateClient(inner, navigation);

        using HttpResponseMessage response = await client.GetAsync(
            "/protected",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(1, inner.CallCount);
        Assert.Equal(
            "https://web.test/account/access-denied",
            navigation.LastUri);
    }

    [Fact]
    public async Task SendAsync_WhenApiReturnsSuccess_DoesNotNavigate()
    {
        RecordingHandler inner = new(HttpStatusCode.OK);
        TestNavigationManager navigation = new(
            "https://web.test/",
            "https://web.test/wms/topology");
        using HttpClient client = CreateClient(inner, navigation);

        using HttpResponseMessage response = await client.GetAsync(
            "/protected",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, inner.CallCount);
        Assert.Null(navigation.LastUri);
    }

    private static HttpClient CreateClient(
        HttpMessageHandler inner,
        NavigationManager navigation)
    {
        ProtectedApiAuthorizationHandler handler = new(navigation)
        {
            InnerHandler = inner
        };
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.test")
        };
    }

    private sealed class RecordingHandler(HttpStatusCode statusCode)
        : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string baseUri, string currentUri)
        {
            Initialize(baseUri, currentUri);
        }

        public string? LastUri { get; private set; }

        protected override void NavigateToCore(
            string uri,
            bool forceLoad)
        {
            LastUri = ToAbsoluteUri(uri).ToString();
        }

        protected override void NavigateToCore(
            string uri,
            NavigationOptions options)
        {
            LastUri = ToAbsoluteUri(uri).ToString();
        }
    }
}
