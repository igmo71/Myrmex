using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        RecordingLogger<OneCODataClient> logger = new();
        using HttpClient httpClient = new(new StubHttpMessageHandler(request =>
        {
            requests.Add(request.RequestUri!);
            authorization = request.Headers.Authorization;
            return Success();
        }));
        OneCODataClient client = CreateClient(httpClient, logger: logger);

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
        Assert.Contains("ReferenceType=all", logger.StructuredState, StringComparison.Ordinal);
        Assert.Contains("CheckedReferenceTypeCount=3", logger.StructuredState, StringComparison.Ordinal);
        Assert.Contains("DurationMilliseconds=", logger.StructuredState, StringComparison.Ordinal);
        Assert.DoesNotContain("operator", logger.StructuredState, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", logger.StructuredState, StringComparison.Ordinal);
        Assert.DoesNotContain("onec.example.test", logger.StructuredState, StringComparison.Ordinal);
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
    public async Task TestConnectionAsync_WhenAuthenticationFails_DoesNotLogOrExposeCredentials()
    {
        const string username = "credential-user-sentinel";
        const string password = "credential-password-sentinel";
        RecordingLogger<OneCODataClient> logger = new();
        using HttpClient httpClient = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        OneCODataClient client = CreateClient(
            httpClient,
            options =>
            {
                options.Username = username;
                options.Password = password;
            },
            logger);

        OneCTransportException exception = await Assert.ThrowsAsync<OneCTransportException>(() =>
            client.TestConnectionAsync(TestContext.Current.CancellationToken));

        Assert.Equal(OneCTransportFailureReason.AuthenticationFailed, exception.Reason);
        Assert.DoesNotContain(username, exception.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(password, exception.ToString(), StringComparison.Ordinal);
        string structuredState = logger.StructuredState;
        Assert.DoesNotContain(username, structuredState, StringComparison.Ordinal);
        Assert.DoesNotContain(password, structuredState, StringComparison.Ordinal);
        Assert.Contains("FailureCategory=AuthenticationFailed", structuredState, StringComparison.Ordinal);
        Assert.Contains("DurationMilliseconds=", structuredState, StringComparison.Ordinal);
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
                    new { Ref_Key = RefKey, DataVersion = new byte[] { 1, 2, 3 }, DeletionMark = false, IsFolder = false, Code = " WH ", Description = " Main " }
                }
            });
        }));
        OneCODataClient client = CreateClient(httpClient);

        IReadOnlyList<Catalog_Склады> records = await client.ReadWarehousesAsync(
            TestContext.Current.CancellationToken);

        string query = Uri.UnescapeDataString(requestUri!.Query);
        Assert.Contains("$select=Ref_Key,DataVersion,DeletionMark,IsFolder,Code,Description", query, StringComparison.Ordinal);
        Assert.Contains("$orderby=Ref_Key", query, StringComparison.Ordinal);
        Assert.Contains("$filter=IsFolder eq false", query, StringComparison.Ordinal);
        Catalog_Склады record = Assert.Single(records);
        Assert.Equal(" WH ", record.Code);
        Assert.Equal(new byte[] { 1, 2, 3 }, record.DataVersion);
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
        Assert.Contains("$select=Ref_Key,DataVersion,DeletionMark,IsFolder,Description", query, StringComparison.Ordinal);
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
                        ["DataVersion"] = new byte[] { 2, 3, 4 },
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
            "$select=Ref_Key,DataVersion,DeletionMark,Code,Description,НаименованиеПолное,МеждународноеСокращение",
            query,
            StringComparison.Ordinal);
        Assert.Contains("$orderby=Ref_Key", query, StringComparison.Ordinal);
        Catalog_УпаковкиЕдиницыИзмерения record = Assert.Single(records);
        Assert.Equal("Штука полная", record.НаименованиеПолное);
        Assert.Equal("PCE", record.МеждународноеСокращение);
        Assert.Equal(new byte[] { 2, 3, 4 }, record.DataVersion);
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

    [Fact]
    public async Task ReadNomenclaturePagesAsync_UsesExactStablePagingQueryAndDeserializesUomKey()
    {
        Uri? requestUri = null;
        Guid unitKey = Guid.NewGuid();
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
                        ["DataVersion"] = new byte[] { 3, 4, 5 },
                        ["DeletionMark"] = false,
                        ["IsFolder"] = false,
                        ["Code"] = "SKU-1",
                        ["Description"] = "Товар",
                        ["НаименованиеПолное"] = "Товар полный",
                        ["Артикул"] = "ART-1",
                        ["ЕдиницаИзмерения_Key"] = unitKey
                    }
                }
            });
        }));
        OneCODataClient client = CreateClient(httpClient, options => options.BatchSize = 2);

        List<IReadOnlyList<Catalog_Номенклатура>> pages = [];
        await foreach (IReadOnlyList<Catalog_Номенклатура> page in
            client.ReadNomenclaturePagesAsync(TestContext.Current.CancellationToken))
        {
            pages.Add(page);
        }

        string query = Uri.UnescapeDataString(requestUri!.Query);
        Assert.Contains(
            "$select=Ref_Key,DataVersion,DeletionMark,IsFolder,Code,Description,НаименованиеПолное,Артикул,ЕдиницаИзмерения_Key",
            query,
            StringComparison.Ordinal);
        Assert.Contains("$orderby=Ref_Key", query, StringComparison.Ordinal);
        Assert.Contains("$skip=0", query, StringComparison.Ordinal);
        Assert.Contains("$top=2", query, StringComparison.Ordinal);
        Assert.Contains("$filter=IsFolder eq false", query, StringComparison.Ordinal);
        Catalog_Номенклатура record = Assert.Single(Assert.Single(pages));
        Assert.Equal(unitKey, record.ЕдиницаИзмерения_Key);
        Assert.Equal("ART-1", record.Артикул);
        Assert.Equal(new byte[] { 3, 4, 5 }, record.DataVersion);
    }

    [Fact]
    public async Task ReadNomenclaturePagesAsync_WhenFirstPageIsEmpty_TerminatesWithoutYielding()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse(new { value = Array.Empty<object>() }));
        using HttpClient httpClient = new(handler);
        OneCODataClient client = CreateClient(httpClient, options => options.BatchSize = 2);
        int pageCount = 0;

        await foreach (IReadOnlyList<Catalog_Номенклатура> _ in
            client.ReadNomenclaturePagesAsync(TestContext.Current.CancellationToken))
        {
            pageCount++;
        }

        Assert.Equal(0, pageCount);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task ReadNomenclaturePagesAsync_AdvancesByReturnedCountAndHandlesExactAndPartialPages()
    {
        Queue<IReadOnlyList<object>> responses = new(
        [
            [NomenclatureRecord(Guid.NewGuid()), NomenclatureRecord(Guid.NewGuid())],
            [NomenclatureRecord(Guid.NewGuid()), NomenclatureRecord(Guid.NewGuid())],
            [NomenclatureRecord(Guid.NewGuid())]
        ]);
        List<string> queries = [];
        using HttpClient httpClient = new(new StubHttpMessageHandler(request =>
        {
            queries.Add(Uri.UnescapeDataString(request.RequestUri!.Query));
            return JsonResponse(new { value = responses.Dequeue() });
        }));
        OneCODataClient client = CreateClient(httpClient, options =>
        {
            options.BatchSize = 2;
            options.UseFolderFilter = false;
        });
        List<int> pageSizes = [];

        await foreach (IReadOnlyList<Catalog_Номенклатура> page in
            client.ReadNomenclaturePagesAsync(TestContext.Current.CancellationToken))
        {
            pageSizes.Add(page.Count);
        }

        Assert.Equal([2, 2, 1], pageSizes);
        Assert.Contains("$skip=0", queries[0], StringComparison.Ordinal);
        Assert.Contains("$skip=2", queries[1], StringComparison.Ordinal);
        Assert.Contains("$skip=4", queries[2], StringComparison.Ordinal);
        Assert.All(queries, query => Assert.DoesNotContain("$filter", query, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ReadNomenclaturePagesAsync_WhenTotalIsExactBatchSize_RequestsEmptyTerminalPage()
    {
        Queue<IReadOnlyList<object>> responses = new(
        [
            [NomenclatureRecord(Guid.NewGuid()), NomenclatureRecord(Guid.NewGuid())],
            []
        ]);
        var handler = new StubHttpMessageHandler(_ =>
            JsonResponse(new { value = responses.Dequeue() }));
        using HttpClient httpClient = new(handler);
        OneCODataClient client = CreateClient(httpClient, options => options.BatchSize = 2);
        List<int> pageSizes = [];

        await foreach (IReadOnlyList<Catalog_Номенклатура> page in
            client.ReadNomenclaturePagesAsync(TestContext.Current.CancellationToken))
        {
            pageSizes.Add(page.Count);
        }

        Assert.Equal([2], pageSizes);
        Assert.Equal(2, handler.CallCount);
    }

    [Theory]
    [InlineData("warehouse")]
    [InlineData("uom")]
    [InlineData("sku")]
    public async Task ReadCurrentReferenceAsync_FiltersByStableKeyAndUsesTypeSpecificShape(string referenceType)
    {
        Uri? requestUri = null;
        Guid unitKey = Guid.NewGuid();
        using HttpClient httpClient = new(new StubHttpMessageHandler(request =>
        {
            requestUri = request.RequestUri;
            object record = referenceType switch
            {
                "warehouse" => new
                {
                    Ref_Key = RefKey,
                    DataVersion = new byte[] { 1 },
                    DeletionMark = false,
                    IsFolder = true,
                    Code = "WH",
                    Description = "Warehouse"
                },
                "uom" => new
                {
                    Ref_Key = RefKey,
                    DataVersion = new byte[] { 2 },
                    DeletionMark = false,
                    Code = "EA",
                    Description = "Each",
                    НаименованиеПолное = "Each full",
                    МеждународноеСокращение = "ea"
                },
                _ => new
                {
                    Ref_Key = RefKey,
                    DataVersion = new byte[] { 3 },
                    DeletionMark = false,
                    IsFolder = true,
                    Code = "SKU",
                    Description = "Stock item",
                    НаименованиеПолное = "Stock item full",
                    Артикул = "A-1",
                    ЕдиницаИзмерения_Key = unitKey
                }
            };
            return JsonResponse(new { value = new[] { record } });
        }));
        OneCODataClient client = CreateClient(httpClient);

        object? current = referenceType switch
        {
            "warehouse" => await client.ReadWarehouseAsync(RefKey, TestContext.Current.CancellationToken),
            "uom" => await client.ReadUnitOfMeasureAsync(RefKey, TestContext.Current.CancellationToken),
            _ => await client.ReadStockKeepingUnitAsync(RefKey, TestContext.Current.CancellationToken)
        };

        Assert.NotNull(current);
        string expectedEntitySet = referenceType switch
        {
            "warehouse" => "Catalog_Warehouses",
            "uom" => OneCOptions.DefaultUnitsOfMeasureEntitySet,
            _ => "Catalog_Nomenclature"
        };
        Assert.Contains(
            Uri.EscapeDataString(expectedEntitySet),
            requestUri!.AbsoluteUri,
            StringComparison.OrdinalIgnoreCase);
        string query = Uri.UnescapeDataString(requestUri!.Query);
        Assert.Contains($"$filter=Ref_Key eq guid'{RefKey:D}'", query, StringComparison.Ordinal);
        Assert.Contains("$top=2", query, StringComparison.Ordinal);
        if (referenceType == "uom")
        {
            Assert.DoesNotContain("IsFolder", query, StringComparison.Ordinal);
            Assert.Equal("ea", Assert.IsType<Catalog_УпаковкиЕдиницыИзмерения>(current).МеждународноеСокращение);
        }
        else
        {
            bool isFolder = referenceType == "warehouse"
                ? Assert.IsType<Catalog_Склады>(current).IsFolder
                : Assert.IsType<Catalog_Номенклатура>(current).IsFolder;
            Assert.True(isFolder);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task ReadWarehouseAsync_EnforcesCurrentObjectCardinality(int count)
    {
        object[] records = Enumerable.Range(0, count)
            .Select(_ => (object)new
            {
                Ref_Key = RefKey,
                DataVersion = new byte[] { 1 },
                DeletionMark = false,
                IsFolder = false,
                Code = "WH",
                Description = "Warehouse"
            })
            .ToArray();
        using HttpClient httpClient = new(new StubHttpMessageHandler(_ =>
            JsonResponse(new { value = records })));
        OneCODataClient client = CreateClient(httpClient);

        if (count == 0)
        {
            Assert.Null(await client.ReadWarehouseAsync(RefKey, TestContext.Current.CancellationToken));
            return;
        }

        OneCTransportException exception = await Assert.ThrowsAsync<OneCTransportException>(() =>
            client.ReadWarehouseAsync(RefKey, TestContext.Current.CancellationToken));
        Assert.Equal(OneCTransportFailureReason.MalformedResponse, exception.Reason);
    }

    [Fact]
    public async Task ReadWarehouseAsync_WhenDataVersionIsEmpty_ReturnsMalformedResponse()
    {
        using HttpClient httpClient = new(new StubHttpMessageHandler(_ =>
            JsonResponse(new
            {
                value = new[]
                {
                    new { Ref_Key = RefKey, DataVersion = Array.Empty<byte>(), DeletionMark = false, IsFolder = false }
                }
            })));
        OneCODataClient client = CreateClient(httpClient);

        OneCTransportException exception = await Assert.ThrowsAsync<OneCTransportException>(() =>
            client.ReadWarehouseAsync(RefKey, TestContext.Current.CancellationToken));

        Assert.Equal(OneCTransportFailureReason.MalformedResponse, exception.Reason);
    }

    private static OneCODataClient CreateClient(
        HttpClient httpClient,
        Action<OneCOptions>? configure = null,
        ILogger<OneCODataClient>? logger = null)
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
        return new OneCODataClient(
            httpClient,
            Options.Create(options),
            logger ?? NullLogger<OneCODataClient>.Instance);
    }

    private static HttpResponseMessage Success() => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(new { value = new[] { new { Ref_Key = RefKey } } })
    };

    private static HttpResponseMessage JsonResponse<T>(T value) => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(value)
    };

    private static object NomenclatureRecord(Guid refKey) => new
    {
        Ref_Key = refKey,
        DataVersion = new byte[] { 1 },
        DeletionMark = false,
        IsFolder = false,
        Code = refKey.ToString("N"),
        Description = "Item"
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

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        private readonly List<IReadOnlyDictionary<string, object?>> _entries = [];

        public string StructuredState => string.Join(
            "|",
            _entries.SelectMany(entry => entry.Select(property => $"{property.Key}={property.Value}")));

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> properties)
            {
                _entries.Add(properties.ToDictionary(property => property.Key, property => property.Value));
            }
        }
    }
}
