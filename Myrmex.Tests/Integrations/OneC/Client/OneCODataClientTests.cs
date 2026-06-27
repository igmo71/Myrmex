using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Myrmex.Integrations.OneC.Configuration;
using Myrmex.Integrations.OneC.Transport;

namespace Myrmex.Tests.Integrations.OneC.Client;

public sealed class OneCODataClientTests
{
    private static readonly Guid RefKey = Guid.Parse("018f0000-0000-7000-8000-000000000999");

    [Fact]
    public async Task TestConnectionAsync_ProbesAllEntitySetsWithBasicAuthentication()
    {
        List<Uri> requests = [];
        AuthenticationHeaderValue? authorization = null;
        using HttpClient httpClient = new(new StubHttpMessageHandler(request =>
        {
            requests.Add(request.RequestUri!);
            authorization = request.Headers.Authorization;
            return Success();
        }));
        OneCODataClient client = CreateClient(httpClient);

        await client.TestConnectionAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, requests.Count);
        Assert.Contains(requests, x => x.AbsoluteUri.Contains("Catalog_Warehouses", StringComparison.Ordinal));
        Assert.Contains(requests, x => x.AbsoluteUri.Contains(Uri.EscapeDataString(OneCOptions.DefaultUnitsOfMeasureEntitySet), StringComparison.OrdinalIgnoreCase));
        Assert.Contains(requests, x => x.AbsoluteUri.Contains("Catalog_Nomenclature", StringComparison.Ordinal));
        Assert.All(requests, x =>
        {
            Assert.Contains("$top=1", x.Query, StringComparison.Ordinal);
            Assert.Contains("$select=Ref_Key", x.Query, StringComparison.Ordinal);
        });
        Assert.Equal("Basic", authorization?.Scheme);
        Assert.Equal(Convert.ToBase64String("operator:secret"u8.ToArray()), authorization?.Parameter);
    }

    [Fact]
    public async Task TestConnectionAsync_WhenDisabled_FailsBeforeSendingRequest()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("Must not send."));
        using HttpClient httpClient = new(handler);
        OneCODataClient client = CreateClient(httpClient, options => options.Enabled = false);

        OneCTransportException exception = await Assert.ThrowsAsync<OneCTransportException>(() =>
            client.TestConnectionAsync(TestContext.Current.CancellationToken));

        Assert.Equal(OneCTransportFailureReason.Disabled, exception.Reason);
        Assert.DoesNotContain("secret", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task TestConnectionAsync_WhenConfigurationIsIncomplete_FailsSafely()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("Must not send."));
        using HttpClient httpClient = new(handler);
        OneCODataClient client = CreateClient(httpClient, options => options.BaseUrl = null);

        OneCTransportException exception = await Assert.ThrowsAsync<OneCTransportException>(() =>
            client.TestConnectionAsync(TestContext.Current.CancellationToken));

        Assert.Equal(OneCTransportFailureReason.InvalidConfiguration, exception.Reason);
        Assert.DoesNotContain("secret", exception.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task TestConnectionAsync_WhenEnvelopeIsMalformed_ReturnsSafeFailure()
    {
        using HttpClient httpClient = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"odata.context\":\"metadata\"}")
            }));
        OneCODataClient client = CreateClient(httpClient);

        OneCTransportException exception = await Assert.ThrowsAsync<OneCTransportException>(() =>
            client.TestConnectionAsync(TestContext.Current.CancellationToken));

        Assert.Equal(OneCTransportFailureReason.MalformedResponse, exception.Reason);
        Assert.DoesNotContain("secret", exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TestConnectionAsync_WhenCallerCancels_PropagatesCancellation()
    {
        using HttpClient httpClient = new(new StubHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return Success();
        }));
        OneCODataClient client = CreateClient(httpClient);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.TestConnectionAsync(cancellation.Token));
    }

    [Fact]
    public async Task TestConnectionAsync_WhenPerRequestTimeoutExpires_ReturnsTimeoutFailure()
    {
        using HttpClient httpClient = new(new StubHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return Success();
        }));
        OneCODataClient client = CreateClient(httpClient, options => options.TimeoutSeconds = 1);

        OneCTransportException exception = await Assert.ThrowsAsync<OneCTransportException>(() =>
            client.TestConnectionAsync(CancellationToken.None));

        Assert.Equal(OneCTransportFailureReason.Timeout, exception.Reason);
    }

    private static OneCODataClient CreateClient(HttpClient httpClient, Action<OneCOptions>? configure = null)
    {
        OneCOptions options = new()
        {
            Enabled = true,
            BaseUrl = "https://onec.example.test/odata/standard.odata/",
            Username = "operator",
            Password = "secret",
            WarehousesEntitySet = "Catalog_Warehouses",
            UnitsOfMeasureEntitySet = OneCOptions.DefaultUnitsOfMeasureEntitySet,
            NomenclatureEntitySet = "Catalog_Nomenclature"
        };
        configure?.Invoke(options);
        return new OneCODataClient(httpClient, Options.Create(options));
    }

    private static HttpResponseMessage Success() => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(new { value = new[] { new { Ref_Key = RefKey } } })
    };

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            : this((request, _) => Task.FromResult(handler(request)))
        {
        }

        public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return _handler(request, cancellationToken);
        }
    }
}
