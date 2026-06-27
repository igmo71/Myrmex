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
