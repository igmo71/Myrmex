using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Myrmex.Shared.Integrations.OneC;
using Myrmex.WebApp.Integrations.OneC;
using System.Net.Http.Json;

namespace Myrmex.Tests.Integrations.OneC.Web;

public sealed class OneCIntegrationApiClientTests
{
    [Fact]
    public async Task TestConnectionAsync_PostsExpectedRouteAndReadsSuccess()
    {
        HttpRequestMessage? captured = null;
        OneCConnectionTestResponse expected = new(
            DateTimeOffset.Parse("2026-06-27T12:00:00Z"),
            true,
            ["warehouses", "uoms", "skus"]);
        using HttpClient httpClient = new(new StubHttpMessageHandler(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(expected) };
        }))
        { BaseAddress = new Uri("https://api.example.test") };
        OneCIntegrationApiClient client = new(httpClient);

        var result = await client.TestConnectionAsync(TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value?.IsReady);
        Assert.Equal(HttpMethod.Post, captured?.Method);
        Assert.Equal("/api/integrations/1c/connection/test", captured?.RequestUri?.AbsolutePath);
        Assert.Null(captured?.Content);
    }

    [Fact]
    public async Task TestConnectionAsync_WhenApiReturnsProblem_PreservesSafeError()
    {
        ProblemDetails problem = new()
        {
            Status = StatusCodes.Status502BadGateway,
            Title = "1С source unavailable",
            Detail = "The 1С OData service is unavailable."
        };
        problem.Extensions["code"] = "OneC.SourceUnavailable";
        using HttpClient httpClient = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = JsonContent.Create(problem) }))
        { BaseAddress = new Uri("https://api.example.test") };
        OneCIntegrationApiClient client = new(httpClient);

        var result = await client.TestConnectionAsync(TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("The 1С OData service is unavailable.", result.Error?.Message);
        Assert.Equal("OneC.SourceUnavailable", result.Error?.Extensions["code"]);
    }

    [Fact]
    public async Task TestConnectionAsync_PropagatesCancellation()
    {
        using HttpClient httpClient = new(new AsyncStubHttpMessageHandler(async cancellationToken =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }))
        { BaseAddress = new Uri("https://api.example.test") };
        OneCIntegrationApiClient client = new(httpClient);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.TestConnectionAsync(cancellation.Token));
    }

    [Theory]
    [InlineData("warehouses", "/api/integrations/1c/warehouses/import")]
    [InlineData("uoms", "/api/integrations/1c/uoms/import")]
    public async Task ImportAsync_PostsSeparateNoBodyRouteAndParsesSharedSummary(
        string referenceType,
        string expectedRoute)
    {
        HttpRequestMessage? captured = null;
        OneCImportResponse expected = new(
            referenceType,
            IsComplete: true,
            Processed: 3,
            Created: 1,
            Updated: 1,
            Skipped: 1,
            Failed: 0,
            StartedAtUtc: DateTimeOffset.Parse("2026-06-27T12:00:00Z"),
            CompletedAtUtc: DateTimeOffset.Parse("2026-06-27T12:01:00Z"),
            OperationError: null,
            Errors: []);
        using HttpClient httpClient = new(new StubHttpMessageHandler(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(expected) };
        })) { BaseAddress = new Uri("https://api.example.test") };
        OneCIntegrationApiClient client = new(httpClient);

        var result = referenceType == "warehouses"
            ? await client.ImportWarehousesAsync(TestContext.Current.CancellationToken)
            : await client.ImportUnitsOfMeasureAsync(TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value?.Processed);
        Assert.Equal(expectedRoute, captured?.RequestUri?.AbsolutePath);
        Assert.Null(captured?.Content);
    }

    [Fact]
    public async Task ImportWarehousesAsync_WhenApiReturnsProblem_UsesExistingApiResultMapping()
    {
        ProblemDetails problem = new()
        {
            Status = StatusCodes.Status400BadRequest,
            Detail = "The 1С integration configuration is invalid."
        };
        problem.Extensions["code"] = "OneC.ConfigurationInvalid";
        using HttpClient httpClient = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = JsonContent.Create(problem) }))
        { BaseAddress = new Uri("https://api.example.test") };
        OneCIntegrationApiClient client = new(httpClient);

        var result = await client.ImportWarehousesAsync(TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("The 1С integration configuration is invalid.", result.Error?.Message);
        Assert.Equal("OneC.ConfigurationInvalid", result.Error?.Extensions["code"]);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(handler(request));
    }

    private sealed class AsyncStubHttpMessageHandler(
        Func<CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => handler(cancellationToken);
    }
}
