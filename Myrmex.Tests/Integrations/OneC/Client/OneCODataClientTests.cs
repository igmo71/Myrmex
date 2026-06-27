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

    [Fact]
    public async Task ReadWarehousesAsync_UsesExactProjectionOrderingAndOptionalFolderFilter()
    {
        Uri? requestUri = null;
        using HttpClient httpClient = new(new StubHttpMessageHandler(request =>
        {
            requestUri = request.RequestUri;
            return JsonResponse(new
            {
                value = new[]
                {
                    new { Ref_Key = RefKey, DeletionMark = false, IsFolder = false, Code = " WH ", Description = " Main " }
                }
            });
        }));
        OneCODataClient client = CreateClient(httpClient);

        IReadOnlyList<Catalog_Склады> records = await client.ReadWarehousesAsync(
            TestContext.Current.CancellationToken);

        string query = Uri.UnescapeDataString(requestUri!.Query);
        Assert.Contains("$select=Ref_Key,DeletionMark,IsFolder,Code,Description", query, StringComparison.Ordinal);
        Assert.Contains("$orderby=Ref_Key", query, StringComparison.Ordinal);
        Assert.Contains("$filter=IsFolder eq false", query, StringComparison.Ordinal);
        Assert.Equal(" WH ", Assert.Single(records).Code);
    }

    [Fact]
    public async Task ReadWarehousesAsync_WhenCodeAndFolderFilterDisabled_OmitsBoth()
    {
        Uri? requestUri = null;
        using HttpClient httpClient = new(new StubHttpMessageHandler(request =>
        {
            requestUri = request.RequestUri;
            return JsonResponse(new { value = Array.Empty<object>() });
        }));
        OneCODataClient client = CreateClient(httpClient, options =>
        {
            options.WarehouseCodeAvailable = false;
            options.UseFolderFilter = false;
        });

        await client.ReadWarehousesAsync(TestContext.Current.CancellationToken);

        string query = Uri.UnescapeDataString(requestUri!.Query);
        Assert.Contains("$select=Ref_Key,DeletionMark,IsFolder,Description", query, StringComparison.Ordinal);
        Assert.DoesNotContain(",Code,", query, StringComparison.Ordinal);
        Assert.DoesNotContain("$filter", query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReadUnitsOfMeasureAsync_UsesExactProjectionAndDeserializesUnicodeFields()
    {
        Uri? requestUri = null;
        using HttpClient httpClient = new(new StubHttpMessageHandler(request =>
        {
            requestUri = request.RequestUri;
            return JsonResponse(new
            {
                value = new[]
                {
                    new Dictionary<string, object?>
                    {
                        ["Ref_Key"] = RefKey,
                        ["DeletionMark"] = false,
                        ["Code"] = "796",
                        ["Description"] = "Штука",
                        ["НаименованиеПолное"] = "Штука полная",
                        ["МеждународноеСокращение"] = "PCE"
                    }
                }
            });
        }));
        OneCODataClient client = CreateClient(httpClient);

        IReadOnlyList<Catalog_УпаковкиЕдиницыИзмерения> records =
            await client.ReadUnitsOfMeasureAsync(TestContext.Current.CancellationToken);

        string query = Uri.UnescapeDataString(requestUri!.Query);
        Assert.Contains(
            "$select=Ref_Key,DeletionMark,Code,Description,НаименованиеПолное,МеждународноеСокращение",
            query,
            StringComparison.Ordinal);
        Assert.Contains("$orderby=Ref_Key", query, StringComparison.Ordinal);
        Catalog_УпаковкиЕдиницыИзмерения record = Assert.Single(records);
        Assert.Equal("Штука полная", record.НаименованиеПолное);
        Assert.Equal("PCE", record.МеждународноеСокращение);
    }

    [Fact]
    public async Task ReadWarehousesAsync_PropagatesCallerCancellation()
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
            client.ReadWarehousesAsync(cancellation.Token));
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

    private static HttpResponseMessage JsonResponse<T>(T value) => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(value)
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
